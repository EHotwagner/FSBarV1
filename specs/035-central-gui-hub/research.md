# Phase 0 Research — Central GUI Hub App

**Feature**: 035-central-gui-hub
**Date**: 2026-04-17
**Status**: Complete — no remaining `NEEDS CLARIFICATION` items.

This document resolves every technical unknown in `plan.md`'s Technical
Context. Each section follows the **Decision / Rationale / Alternatives**
template prescribed by the plan template.

---

## R1 — GUI / windowing framework

**Decision**: Build the hub as a single Skia + Silk.NET window using the
existing `SkiaViewer` package. The "tabs" are scenes selected by the hub's
own input router; the embedded live viewer is the existing `FSBar.Viz.GameViz`
scene composed into the window when the Viewer tab is active.

**Rationale**:
- The constitution mandates dependency minimization (§EC). Adding Avalonia /
  Eto.Forms / WPF would introduce a 30-50 MB managed dependency stack and a
  second rendering loop alongside the SkiaSharp one we already own.
- Every existing visual surface in this repo (features 008, 027, 028, 033) is
  built on `SkiaViewer` + Silk.NET. Feature 033's style configurator panel is
  proof that non-trivial GUI controls (sliders, color swatches, scrollable
  panels, mouse routing) work fluently on this stack — the hub's tab bar /
  status bar / setup forms are smaller in scope than the configurator.
- The viewer / hub / configurator share a single GL context and a single input
  loop, eliminating the "embed BAR viewer inside Avalonia control" headache.

**Alternatives considered**:
- **Avalonia 11** — proper desktop UI toolkit with declarative XAML, F#-friendly. Rejected: large new dependency; embedding `SkiaSharp` GL surface inside `Avalonia.OpenGlControl` is fragile in this environment (the SkiaSharp GPU backend already segfaults here per `CLAUDE.md` — switching the BAR viewer's surface model just for the hub is not worth it).
- **Eto.Forms** — lightweight cross-platform UI. Rejected: same embedding problem; less F# adoption; weaker layout for the proposed sidebar / status bar / per-tab dense forms.
- **HTML over local HTTP via WebView** — rejected: introduces JS toolchain, violates §EC F#-on-.NET-exclusive in spirit, and the viewer is GL-rendered so we'd round-trip pixels through a canvas anyway.

**Implications for `FSBar.Hub.App`**:
- `Tabs/*Tab.fs` modules export a `render: tabState -> Scene` plus
  `handle: InputEvent -> tabState -> tabState` pair.
- A single `TabRouter` decides which tab's scene gets composed each frame.
- Status bar overlays the bottom 24 px regardless of active tab; tab bar
  overlays the left 56 px.

---

## R2 — gRPC server hosting (FsGrpc + ASP.NET Core)

**Decision**: Host the scripting gRPC service inside a tiny ASP.NET Core
Kestrel host bootstrapped from `FSBar.Hub.App.Program`, listening on
`http://127.0.0.1:<port>` (HTTP/2 cleartext). Default port `5021`, override in
`HubSettings`. Use FsGrpc's contract-first server pattern from the
`fsgrpc-server` skill.

**Rationale**:
- Constitution §EC mandates `fsgrpc-setup` / `fsgrpc-server` / `fsgrpc-client`.
- `Grpc.AspNetCore` is the only supported FsGrpc server host.
- HTTP/2 cleartext is fine because the endpoint is localhost-only
  (assumption); avoids the dev-cert hassle of HTTPS.
- The Kestrel host runs on a background thread; the GUI loop runs on the main
  thread. They communicate via the `SessionManager`'s `IObservable<GameFrame>`.

**Alternatives considered**:
- **Hand-rolled HTTP/2 over Kestrel** — rejected: violates §EC (must use
  FsGrpc); reinvents framing.
- **Pure-managed gRPC over named pipes / Unix domain socket** — rejected for
  v1: scripting clients (F# `.fsx`, Python) overwhelmingly know `localhost:port`
  better than UDS; this is also how the trainer connects to the engine today.

**Implementation note**: Packages added to `FSBar.Hub.App.fsproj` (NOT to the
library `FSBar.Hub`):

```xml
<PackageReference Include="Grpc.AspNetCore" Version="2.67.0" />
```

`<Project Sdk="Microsoft.NET.Sdk.Web">` so we get the framework reference to
`Microsoft.AspNetCore.App` automatically.

The `ScriptingHub` *implementation* lives in `FSBar.Hub` (no ASP.NET reference),
exposing `type ScriptingService(deps) = interface IHubScriptingService`. Only
the App project pulls Grpc.AspNetCore + does `app.MapGrpcService<...>()`.

---

## R3 — Multi-client gRPC fan-out (slow-client isolation)

**Decision**: Each connected `StreamGameFrames` RPC call gets its own
`System.Threading.Channels.BoundedChannel<GameFrame>` of capacity 16, with
`BoundedChannelFullMode.DropOldest`. A single internal subscriber to
`SessionManager.Frames` enqueues each frame to every active client channel;
the per-client `IAsyncEnumerable` loop drains its own channel into the gRPC
response stream. If a client's `WriteAsync` fails or its channel has dropped
≥ 32 frames cumulatively, the hub closes that client's stream and emits a
structured `client.detached` event — other clients are unaffected.

**Rationale**:
- Directly mirrors the lock-free dataflow approach validated in feature 032
  (`032-lockfree-viewer-dataflow`): single producer, multiple independent
  consumers, no shared lock.
- Bounded channel + drop-oldest provides natural back-pressure that protects
  the hub's memory without blocking the producer (FR-028, SC-005, SC-006).
- A drop counter that escalates to disconnect prevents zombie clients from
  silently lagging forever.
- 16-frame buffer at ~60 fps = ~270 ms of buffer — plenty for a healthy
  client and small enough to surface lag quickly.

**Alternatives considered**:
- **Unbounded channel** — rejected: a stuck client would grow memory
  unboundedly until OOM.
- **Shared SPMC ring with per-consumer cursor** — rejected: more complex,
  marginal performance benefit at hub-scale (≤ tens of clients), and harder
  to reason about for slow-client cleanup.
- **Drop newest** — rejected: stale frames on the recent edge are worse for
  scripting clients than missing one a few ticks back; "oldest dropped" keeps
  the most recent gamestate flowing.

---

## R4 — Where the hub-side `.proto` lives

**Decision**: Add `src/FSBar.Proto/proto/hub/scripting.proto` to the existing
`FSBar.Proto` project. Keep the existing `highbar/*.proto` engine-protocol
files untouched. The `FSBar.Proto` project description is widened from
"protobuf bindings for HighBar V2 protocol" to "protobuf bindings for the
HighBar V2 engine protocol and the FSBar hub scripting service".

**Rationale**:
- The hub scripting messages reuse engine types (`Highbar.GameFrame`,
  `Highbar.AICommand`, `Highbar.UnitDef`) verbatim — a separate project would
  force a transitive `FSBar.Proto` reference + a second `FsGrpc.Tools` build
  pass, doubling generation overhead without any encapsulation gain.
- Buf workspaces are per-project; one workspace is simpler than two.
- The package consumers (scripting `.fsx` clients) already depend on
  `FSBar.Proto` for engine types. Adding hub messages there keeps "one
  package = all FSBar protobuf types".

**Alternatives considered**:
- **New `FSBar.Hub.Proto` project** — rejected per above; would also force
  duplicate `PackageId` discipline and a second NuGet artifact.
- **Inline the proto under `FSBar.Hub` itself** — rejected: keeps the
  `.proto` on the same compile pass as the impl, blurring the FsGrpc
  contract-first boundary.

---

## R5 — `IGL_data.lua` targeted edit strategy

**Decision**: Implement `ProxyInstaller.setSimpleAiList` as a regex-driven
**single-line replace** that locates the existing `simpleAiList` key and
rewrites only the value token. If the key is absent, append a new line in the
correct table block (after the last existing key, before the closing brace).
Idempotent: re-running with the value already correct is a no-op that does
not touch the file's mtime. Validated by golden fixture in tests.

**Rationale**:
- AS-2.5 explicitly forbids reformatting the file or reordering keys.
- Lua syntax for the relevant lines is regular ("`<key> = <bool|number|string>,?`"),
  so a regex anchored on the key name is sufficient and avoids pulling in a
  full Lua parser (would violate §EC dependency minimization).
- Idempotency by content-equality check (read, compute new, compare bytes,
  skip write) prevents spurious mtime changes that would confuse
  Chobby's settings-watcher.

**Regex** (F#-flavored):
```
^(\s*)simpleAiList(\s*)=(\s*)(true|false)(\s*,?\s*)$
```
Match group 4 is replaced with the literal `false`. Anchored multiline.

**Alternatives considered**:
- **Full Lua AST parse + emit (e.g., NLua)** — rejected: heavy dep, breaks
  §EC dependency minimization, harder to preserve original formatting
  byte-for-byte.
- **Append-only ("simpleAiList = false" at end of file)** — rejected: Lua
  would error if the surrounding `return { ... }` wrapper isn't accounted for;
  also creates duplicate keys which is fragile.

---

## R6 — Bundled-proxy versioning + refresh script

**Decision**: The on-disk layout is:
```
proxy/
├── bundled/
│   └── <version>/                # e.g. 0.1.17/
│       ├── libSkirmishAI.so
│       ├── AIInfo.lua
│       └── AIOptions.lua
├── BUNDLED_VERSION               # single line plain text: "0.1.17\n"
└── README.md
```
The hub reads `proxy/BUNDLED_VERSION` (resolved relative to its own assembly
location, falling back to `$FSBAR_HUB_BUNDLED_PROXY_DIR` for dev runs) and
expects exactly one matching `bundled/<version>/` subdir. The maintainer
script `scripts/refresh-bundled-proxy.sh` takes a version arg, copies the
three files from `${HIGHBARV2_REPO:-../HighBarV2}/build/`, and rewrites
`BUNDLED_VERSION`.

**Rationale**:
- The version string in a separate file (vs. embedded in directory name only)
  lets the hub display it in the Settings tab via a single read, and lets the
  refresh script do an atomic swap (write new dir, move BUNDLED_VERSION last).
- Committing the binary `libSkirmishAI.so` is acceptable for this repo
  (already permits binaries via `nupkg/` and `bots/trainer/map-cache/`).
- Per FR-006a, users must NOT need a HighBarV2 checkout — bundling in-repo
  satisfies that.

**Alternatives considered**:
- **Submodule pointing at HighBarV2** — rejected: FR-006a explicitly says
  "self-contained checkout"; submodules force users to `git submodule init`.
- **Download from GitHub release on first run** — rejected: requires network
  on first run, complicates offline dev, and FR-006a says "bundled binary
  committed to this repo".

---

## R7 — Settings storage location

**Decision**: `$XDG_CONFIG_HOME/fsbar-hub/settings.json` (default
`$HOME/.config/fsbar-hub/settings.json`). Single JSON file containing:
`barDataDir`, `engineDirOverride`, `lastLobby`, `grpcPort`,
`launchGraphicalViewerDefault`, `gridLines`, etc.

**Rationale**:
- XDG Base Directory Specification is the Linux desktop convention; matches
  where Chobby itself stores `IGL_data.lua` (`$XDG_DATA_HOME` for state).
- Single file = single atomic rename on save; no migration ceremony.
- JSON via `System.Text.Json` (BCL) — no new dependency.

**Alternatives considered**:
- **`~/.fsbar-hub/`** — rejected: ignores XDG; clutters $HOME.
- **In the BAR data directory** — rejected: pollutes user data dir with hub
  state; also makes "hub knows where BAR is" circular on first run.

---

## R8 — Embedding `GameViz` inside the Hub window

**Decision**: The hub does **not** call `GameViz.start` (which opens its own
window). Instead, the hub composes the viewer's scene into its own
`SkiaViewer.Window` by calling `SceneBuilder.buildScene` directly with the
hub's `VizConfig`. Hub chrome (tab bar, status bar) is a separate scene
overlaid on top.

This requires a small additive change to `FSBar.Viz`: expose a "headless
scene builder" entrypoint that accepts the live `GameState` + `MapGrid` and
returns a `Scene` without owning a window. Most of the plumbing already
exists in `SceneBuilder.buildScene` and `LayerRenderer.*`.

**Rationale**:
- One window for the entire app is required for the tab UX (FR-002a).
- Reusing `SceneBuilder.buildScene` keeps glyph parity with the standalone
  viewer (SC-003 byte-match requirement is satisfied for free).
- The change to `FSBar.Viz` is additive (a new public function or a new
  module), so existing consumers (`run.sh`-style standalone use) are
  unaffected. This is recorded as a Tier 1 surface change to `FSBar.Viz` and
  must ship with `.fsi` updates and a baseline bump.

**Alternatives considered**:
- **Open `GameViz` in its own window alongside the hub** — rejected: violates
  the "single GUI app" intent and FR-002a (tabs cannot be siblings of a
  separate window).
- **Render BAR viewer to an off-screen texture and blit** — rejected:
  duplicate render path, performance concern at 60 fps.

---

## R9 — Hub-owned engine process lifecycle

**Decision**: When the hub launches an engine via `EngineLauncher`, it places
the new process in its own process group (Linux `setsid()` via
`ProcessStartInfo.CreateNewProcessGroup` equivalent — actually accomplished by
`Process.Start` and then `prctl(PR_SET_PDEATHSIG, SIGTERM)` from a shell
wrapper). On hub exit (window-close, SIGTERM, or `Stop`), the hub iterates
its tracked PIDs and sends SIGTERM, then SIGKILL after a 3s grace.

**Rationale**:
- FR-001 mandates teardown.
- `prctl(PR_SET_PDEATHSIG)` is the only Linux mechanism that survives a hub
  crash — pure Process.Kill on SIGTERM works for clean exit but leaks engines
  if the hub itself segfaults.
- The hub spawns engines via a thin shell wrapper script
  (`scripts/hub-spawn-engine.sh`) that does the prctl call and then `exec`s
  `spring-headless`. Same pattern the trainer uses.

**Alternatives considered**:
- **Pure managed Process.Kill** — rejected: doesn't survive hub crashes.
- **Linux cgroup v2 freeze on hub exit** — rejected: requires elevated perms;
  overkill for a single-user desktop app.

---

## R10 — Encyclopedia glyph parity

**Decision**: The encyclopedia tab renders each unit by constructing a
synthetic `UnitDisplay` per `BarData.UnitDef` and calling
`UnitGlyph.buildUnit` directly with a one-element list. This is the same code
path the live viewer uses, satisfying SC-003's byte-match requirement by
construction.

The synthetic UnitDisplay carries a fixed position / heading / hp / build
progress so the entry's glyph is deterministic across hub runs.

**Rationale**:
- `UnitGlyph` already classifies shape / tier / faction / label from
  `BarData` fields — encyclopedia and viewer will draw bit-identical glyphs
  as long as we pass equivalent `UnitDisplay` records.
- `FSBar.SyntheticData.SyntheticDataAdapter` is the existing converter
  `UnitDef → UnitDisplay`; encyclopedia reuses it.

**Alternatives considered**:
- **Render encyclopedia glyphs through a separate code path** — rejected:
  guaranteed drift; violates SC-003.
- **Generate static PNG sprites at build time** — rejected: ties hub releases
  to BarData releases; defeats FR-022 dynamic updates.

---

## Summary of new public surface (informs `.fsi` work in Phase 1)

| Project | Module | Notes |
|---------|--------|-------|
| `FSBar.Proto` (existing) | new generated `Hub.Scripting.*` | from `proto/hub/scripting.proto`, generated by FsGrpc.Tools — no hand-written `.fsi` (generated code is exempt per existing convention with `Generated/highbar/*.gen.fs`) |
| `FSBar.Hub` (new) | `HubSettings`, `BarInstall`, `BundledProxy`, `ProxyInstaller`, `LobbyConfig`, `SessionManager`, `ScriptingHub`, `HubEvents` | every `.fs` ships with curated `.fsi`; surface baselines under `tests/FSBar.Hub.Tests/Baselines/` |
| `FSBar.Hub.App` (new) | `Tabs/*`, `Chrome/*`, `FirstRun`, `ProcessLifetime` | `.fsi` for reusable bits; `Program.fs` is unsignatured glue |
| `FSBar.Viz` (existing, EXTENDED) | `SceneBuilder` — add headless scene-build entrypoint per R8 | `.fsi` updated, baseline bumped |

All open `NEEDS CLARIFICATION` items from the Technical Context are now
resolved.
