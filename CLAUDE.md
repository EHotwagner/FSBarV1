# FSBarV1 Development Guidelines

F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints).
Core stack: `FSBar.{Proto,Client,SyntheticData,Viz,Hub,Hub.App}` +
SkiaViewer 1.1.3-dev, SkiaSharp 2.88.6, FsGrpc 1.0.6,
Grpc.AspNetCore 2.67.0, BarData (local nupkg feed), xUnit 2.9.x.
**No new NuGet dependencies** without an approved spec. Per-feature
context, decisions, and changelog live under `specs/NNN-*/`.

## Project structure

```text
src/
├── FSBar.Proto/           # generated protobuf types (highbar/*, hub/scripting.proto)
├── FSBar.Client/          # connection, protocol, GameState, map analysis
├── FSBar.SyntheticData/   # scene + economy + SyntheticMapGrid builders
├── FSBar.Viz/             # GameViz, SceneBuilder, UnitGlyph, configurator
├── FSBar.Hub/             # packable core (HubStateStore, HeadlessRenderer, HubLog, …)
└── FSBar.Hub.App/         # SkiaViewer GUI binding (Tabs/*)

tests/
├── Common/                             # SurfaceAreaHelper.fs (compile-included)
├── FSBar.{Client,SyntheticData,Viz,Hub}.Tests/   # unit + Baselines/
├── FSBar.{LiveTests,Hub.LiveTests}/    # real-engine integration (Live* prefix)
└── run-all.sh                          # engine-aware wrapper
```

Top-level: `dotnet build FSBarV1.slnx`, `dotnet test FSBarV1.slnx`.
Surface-area baselines regenerate via `SURFACE_AREA_UPDATE=1`.

## Code style

- F# 9, standard conventions.
- No `private` / `internal` modifiers in non-generated source. Public
  surface is gated exclusively by `.fsi` signature files (constitution §II).

## Testing policy

- Always run tests against the live environment. No mocks, fakes, or
  in-memory substitutes.
- Tests blocked by out-of-scope issues (missing server, external dep,
  upstream feature) **must** be skipped or have assertions relaxed —
  never mark a failing test as passed.

## Key architectural conventions

**Hub state routing** (feature 041) — every Hub-GUI tab reads state
through `HubStateStore.current store` and writes via typed mutators
(`setVizConfig`, `setVizAttribute`, `setEncyclopedia`, `setActiveTab`,
`setSettings`). Mutators emit a single `HubEvent.DiagnosticsLine`
warning on `SubmitOutcome.Rejected`; callers roll back by re-reading.
Local presentation state (scroll, toasts) stays per-tab.

**Unit display pipeline** (features 028, 038, 044) —
`FSBar.Viz.UnitDisplayAdapter` is the single constructor for every
`UnitDisplay` handed to `UnitGlyph.buildUnit`
(`ofTrackedUnit` / `ofTrackedEnemy` / `ofEncyclopediaEntry`). Glyph
classifiers (`classifyShape` / `classifyTier` / `classifyFaction`) are
shared by Viewer, Units tab, and Style preview — byte-identical output
across surfaces is a contract (SC-003).

**Hub scripting (gRPC)** — `fsbar.hub.scripting.v1` exposes every
Hub-GUI action plus `StreamRenderFrames`, `StreamHubStateEvents`, and
`StreamLogs` (feature 042). Client-side overlays go through
`OverlayLayerStore` with the FR-026 cap matrix. FSI walkthroughs:
`scripts/examples/{16..24}-hub-*.fsx`.

**Full-fledged scripting client** (feature 046) — `StreamGameFrames`
now carries `GameStateFrame` + typed `GameEventEnvelope[]` per tick,
projected from `BarClient.GameState` (feature 045 single source of
truth). Enemy health uses `oneof health_info { float health;
EnemyHealthUnknown unknown; }` so radar-only / frozen-last-known is
distinguishable from `health = 0`. Unary map RPCs (`GetMapInfo`,
`GetHeightmap`, `GetCornersHeightmap`, `GetSlopeMap`, `GetLosMap`,
`GetRadarMap`, `GetResourceMap`, `ListMetalSpots`) read from the
warmup-cached `RunningSession.MapGrid` + `MetalSpots` — grid payloads
use `repeated float`/`int32` + width/height; Hub scripting channel is
configured with 64 MiB `MaxReceive/SendMessageSize`.
`GetUnitDefExtended` merges the BarData encyclopedia with the live
`UnitDefCache`. `SendCommandBatch` accepts ≤1024 `AICommand`s per
call (oversize rejected whole) and returns one `forwarded_at_frame` +
1:1 `CommandOutcome[]`. Walkthrough: `scripts/examples/24-hub-full-client.fsx`.

**Hub admin channel** (feature 039) — loopback UDP autohost channel to
the engine for pause / resume / speed / force-end / admin message.
`SessionManager` exposes each as `Pause` / `Resume` / `SetEngineSpeed` /
`ForceEnd` / `SendAdminMessage`, returning `SubmitOutcome`. Wire
contract: `specs/039-hub-admin-channel/contracts/autohost-wire.md`.

**Hub log stream** (feature 042) — `FSBar.Hub.HubLog` is the canonical
emit surface. Emit is O(1) when no subscriber is attached. Categories
mirror the proto enum 1:1; default floor is `Info`. Correlation IDs
flow via `FSBar.Hub.CorrelationId.ServerInterceptor`
(`x-fsbar-correlation-id` request-metadata header, echoed on trailer).

**Map analysis caching** — static per-map chokepoints + MapGrid
committed under `bots/trainer/map-cache/*.json`. Contract in
`src/FSBar.Client/MapCacheFile.fsi`. Trainer warmup hard-aborts on any
mismatch (FR-006).

**Style configurator** (feature 033) — press `P` in the live viewer.
Every editable attribute lives in `ConfigDescriptors.all`; extend that
single registry to expose a new knob — panel + presets pick it up
automatically.

## Engine paths

Auto-detected at runtime by `EngineDiscovery` (scans
`~/.local/state/Beyond All Reason/engine/recoil_*/`, picks latest).
Override via `FSBAR_TEST_ENGINE` (preferred), `HIGHBAR_TEST_ENGINE`
(legacy alias — if both set, `FSBAR_TEST_ENGINE` wins and a warning is
emitted), or `tests/engine-version.json`.

- Data dir: `~/.local/state/Beyond All Reason`
- Engine dir: `…/engine/recoil_<YYYY.MM.DD>/`
- Binaries: `spring-headless` (headless) / `spring` (graphical)

## How-to skills

Procedural workflows live as `.claude/skills/` — invoke via `/skill`:

| Task | Skill |
|------|-------|
| Launch the Hub GUI + env-var matrix | `hub-run` |
| Regenerate `FSBar.Proto` from `.proto` | `proto-regen` |
| Load FSBar into FSI (dlopen + `#r`) | `fsi-fsbar-load` |
| Bump SkiaViewer / BarData from sibling repo | `upstream-pack` |
| Regenerate `UnitLabels.generated.fs` | `unit-labels-regen` |
| Regenerate committed map-cache JSON | `map-cache-refresh` |
| Headless BAR engine REPL with viz | `repl` / `repl-graphical` |

## Active Technologies
- F# 9 on .NET 10.0 (Constitution §Engineering Constraints) + FsGrpc 1.0.6, Grpc.AspNetCore 2.67.0, SkiaSharp 2.88.6, BarData (local nupkg) (046-scripting-full-client)
- N/A — live session state is in-process (`BarClient.GameState`) (046-scripting-full-client)
- Markdown for the docs page; Python 3.10+ for + `grpcio` (≥1.60), `grpcio-tools` (for (047-polyglot-client-guide)
- N/A — the Python client is stateless; live session state (047-polyglot-client-guide)

## Recent Changes
- 047-polyglot-client-guide: Python scripting-client on-ramp — new `docs/scripting-polyglot.md` + runnable `scripts/examples/python/hub_full_client.py` (docs/code only; no F# / proto changes)
- 046-scripting-full-client: Added F# 9 on .NET 10.0 (Constitution §Engineering Constraints) + FsGrpc 1.0.6, Grpc.AspNetCore 2.67.0, SkiaSharp 2.88.6, BarData (local nupkg)
