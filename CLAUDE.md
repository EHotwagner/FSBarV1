# FSBarV1 Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-18

## Active Technologies
- F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf), BarData (unit definitions), xUnit 2.9.x (002-test-suite-report)
- N/A (file-based report output only) (002-test-suite-report)
- F# / .NET 10.0 + FSBar.Client (in-repo), FSBar.Proto (in-repo), BarData (NuGet), xUnit 2.9.x, Microsoft.NET.Test.Sdk (003-live-game-tests)
- Filesystem only (temp dirs, socket files, log files, Markdown reports) (003-live-game-tests)
- F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf), FSBar.Proto (generated types), BarData (unit definitions) (004-array-map-layers)
- In-memory Array2D grids + ConcurrentDictionary caching (004-array-map-layers)
- Filesystem (socket files, session dirs) (005-incorporate-highbarv2-fixes)
- F# / .NET 10.0 + xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x (existing in FSBar.Client.Tests) (007-fix-surface-baselines)
- Filesystem — `.baseline` text files committed to git (007-fix-surface-baselines)
- F# / .NET 10.0 + Silk.NET.Windowing 2.22.0, Silk.NET.OpenGL 2.22.0, Silk.NET.Input 2.22.0, SkiaSharp 2.88.6, FSBar.Client (in-repo), FSBar.Proto (in-repo) (008-game-viz)
- N/A (in-memory only) (008-game-viz)
- F# / .NET 10.0 + SkiaViewer 1.0.0, SkiaSharp 2.88.6, FSBar.Client (in-repo), FSBar.Viz (in-repo) (010-map-gamestate-preview)
- Binary files on disk for MapGrid serialization (010-map-gamestate-preview)
- F# / .NET 10.0 + FSBar.Client (in-repo), FSBar.Viz (in-repo), SkiaViewer 1.0.0, SkiaSharp 2.88.6, Silk.NET 2.22.0 (011-live-map-viz)
- N/A (in-memory only, no persistence needed) (011-live-map-viz)
- Containerfile (OCI/Docker format), Bash (entrypoint), Markdown (documentation) + Arch Linux base image, .NET 10.0 SDK, Node.js, GitHub CLI, FSI MCP server (012-minimal-container-setup)
- N/A (container image layers + host bind mounts at runtime) (012-minimal-container-setup)
- F# / .NET 10.0 + FSBar.Client (in-repo), System.IO, System.IO.Compression (for gzip) (013-auto-engine-version)
- Filesystem scanning (read-only) (013-auto-engine-version)
- F# / .NET 10.0 + SkiaViewer (local nupkg), BarData (local nupkg), NuGet CLI tooling (015-fix-stale-dll-cache)
- Filesystem (nupkg files, NuGet global cache) (015-fix-stale-dll-cache)
- In-memory (Map, ConcurrentDictionary caches) (016-gamestate-api)
- N/A (in-memory session state + Unix domain sockets) (016-idiomatic-fsharp-streams)
- F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf), BarData (NuGet local feed), System.IObservable (BCL — no external Rx needed) (017-observable-gamestate-api)
- In-memory (Map, ConcurrentDictionary caches, Array2D grids) (017-observable-gamestate-api)
- F# / .NET 10.0 + FSBar.Client (in-repo, for types only — GameState, TrackedUnit, TrackedEnemy, EconomySnapshot, UnitDefCache, GameEvent, GameFrame) (018-synthetic-viz-data)
- N/A (in-memory only, pure functions) (018-synthetic-viz-data)
- F# / .NET 10.0 + SkiaViewer (latest prerelease, declarative Scene API), SkiaSharp 2.88.6, FSBar.Client (in-repo), FSBar.SyntheticData (in-repo) (019-revamp-viz-library)
- Filesystem (MapData binary format for save/load, screenshots) (019-revamp-viz-library)
- F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints). Trainer bot scripts are `.fsx` loaded by `dotnet fsi`. + existing in-repo `FSBar.Client`, `FSBar.Proto`, `FsGrpc 1.0.6`, `BarData` (NuGet from local store); `System.Text.Json` (BCL) for run artifacts; bash for the runner. (020-bot-iterative-trainer)
- filesystem only — JSONL frame logs, JSON metadata/result files, plain-text stdout/infolog captures under `bots/runs/` (gitignored); in-repo `bots/trainer/` tree for bot + helpers + ladder + playbook. No database. (020-bot-iterative-trainer)
- F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints). Trainer bot scripts are `.fsx` loaded by `dotnet fsi`. + existing in-repo `FSBar.Client`, `FSBar.Proto`, `FsGrpc 1.0.6`, `BarData` (NuGet from local store); rebuilt HighBarV2 `libSkirmishAI.so` from sibling `../HighBarV2` checkout (post `029-fix-trainer-issues` squash-merge). `System.Text.Json` (BCL). Bash for the runner. (021-rerun-trainer-highbar)
- filesystem only — JSONL frame logs, JSON metadata/result files, plain-text stdout/infolog captures under `bots/runs/` (gitignored, unchanged from 020); in-repo `bots/trainer/` tree edited in place; `Mailbox/` for inbound and new outbound reports. (021-rerun-trainer-highbar)
- F# / .NET 10.0 (no F# changes in this feature — references-only) plus Bash for the trainer runner edit + existing in-repo `FSBar.Client`, `FSBar.Proto`, `BarData` (NuGet from local store). No new dependencies. (022-incorporate-highbar-030)
- filesystem only — JSONL frame logs and JSON metadata under `bots/runs/` (gitignored, unchanged from 020/021); `Mailbox/` for outbound report; `specs/022-incorporate-highbar-030/` for closure note. (022-incorporate-highbar-030)
- F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints). Trainer bot scripts are `.fsx` loaded by `dotnet fsi`. + existing in-repo `FSBar.Client`, `FSBar.Proto`, `FsGrpc 1.0.6`, `BarData` (NuGet from local store); `System.Text.Json` (BCL) for run artifacts; bash for the runner. **No new dependencies.** (023-trainer-builder-economy)
- filesystem only — JSONL frame logs, JSON metadata/result/phase-transition files, plain-text stdout/infolog captures under `bots/runs/` (gitignored, unchanged from 020/021/022); in-repo `bots/trainer/` tree edited in place. No database. (023-trainer-builder-economy)
- F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints). + existing in-repo `FSBar.Client` (`MapGrid`, `MapQuery`, `Callbacks`, `GameState`), `BarData` (NuGet local feed, unit definitions), `xUnit 2.9.x` for tests. **No new NuGet dependencies.** `bsdtar` (system tool, present on dev image via libarchive) is shelled out to extract `.sd7` → `.smf` at SMF-parser runtime. (024-tactical-map-primitives)
- Filesystem only. Test fixtures split: synthetic `MapGrid` values constructed in-memory in `tests/FSBar.Client.Tests/SyntheticMapGrid.fs`; SMF fixtures are read on-demand from `~/.local/state/Beyond All Reason/maps/*.sd7` (no binaries committed to the repo). Bot run artifacts continue to land under `bots/runs/` (gitignored, unchanged from 020/023). (024-tactical-map-primitives)
- F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints). Trainer bot script is `.fsx` loaded by `dotnet fsi`. + existing in-repo `FSBar.Client` (all 024 primitives: `Pathing`, `Chokepoints`, `BasePlan`, `WallIn`, `SmfParser`, plus pre-024 `Commands`, `Callbacks`, `GameState`, `MapGrid`, `UnitDefCache`, `Protocol`), `FSBar.Proto` (generated types incl. `Highbar.AICommand`), `BarData` (NuGet local feed, unit definitions), `xUnit 2.9.x` for Commands unit test. **No new NuGet dependencies.** (025-macro-primitive-driven)
- Filesystem only. Extended `bots/trainer/map-cache/<map>.json` carries a compressed MapGrid blob (heightmap + slope map + resource map + dimensions) alongside the existing chokepoint list — OR the bot inline re-parses `.sd7` via `SmfParser` at warmup, pending the Phase-0 R1 measurement decision. Bot run artifacts land under `bots/runs/` (gitignored, unchanged from 020/023/024). (025-macro-primitive-driven)
- F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints) + Existing in-repo only — `FSBar.Client` (`MapGrid`, `SmfParser`, `Chokepoints`, `BasePlan`, `MapQuery`), BCL `System.IO.Compression` (already used for gzipped blobs), BCL `System.Text.Json` (already used by `14-cache-map-analysis.fsx`). **No new NuGet dependencies.** (026-permanent-map-cache)
- Filesystem. Committed JSON files under `bots/trainer/map-cache/<safe-name>.json`, one per supported map. Each file is a self-describing record containing schema version, `codeVersion`, analysis parameters, source map identity, and gzip+base64 blobs for heightmap / slope map / resource map. Typical size 500 KB – 1 MB per map per the feature 025 notes; capped at ~1.5 MB/map and ~15 MB total by SC-005. (026-permanent-map-cache)
- F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints). + Existing in-repo only — `FSBar.Client` (`MapCacheFile`, `MapGrid`, `MapQuery`), `FSBar.Viz` (`LayerRenderer`, `SceneBuilder`, `PreviewSession`, `GameViz`, `ColorMaps`, `VizTypes`), `SkiaViewer` 1.1.3-dev (`InputEvent.FrameTick`, `Scene`, `Shader.Image`), `SkiaSharp` 2.88.6, `xUnit 2.9.x`. **No new NuGet dependencies.** (027-map-terrain-viz)
- Filesystem read-only — reads cached `bots/trainer/map-cache/<map>.json` files via `MapCacheFile.read`. No new on-disk formats. (027-map-terrain-viz)
- F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints). + Existing in-repo — `FSBar.Viz` (`SceneBuilder`, `VizTypes`, `ColorMaps`), `FSBar.Client` (`GameState`, `UnitDefCache`), `FSBar.SyntheticData` (`Scenes`, `SceneTypes`), `SkiaViewer` 1.1.3-dev (declarative `Scene`), `SkiaSharp` 2.88.6, `BarData` (local nupkg feed), `xUnit 2.9.x`. **No new NuGet dependencies.** (028-unit-viz-language)
- Filesystem. One build-time artifact committed to the repo: `src/FSBar.Viz/UnitLabels.generated.fs` — byte-stable mapping from `BarData` unit internal name to 2- or 3-char code. No runtime storage. (028-unit-viz-language)
- Bash (run.sh CLI) + F# 9 on .NET 10.0 (bot scripts, helpers) + FSBar.Client (BarClient, EngineConfig), FSBar.Viz (GameViz, SceneBuilder, UnitGlyph), SkiaViewer (window management), SkiaSharp 2.88.6 (029-trainer-viewer-options)
- Filesystem only — run artifacts under `bots/runs/` (gitignored) (029-trainer-viewer-options)
- F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints) + Existing in-repo — `FSBar.Viz` (`GameViz`, `SceneBuilder`, `VizTypes`, `UnitGlyph`, `UnitLabels`), `FSBar.Client` (`GameState`, `MapGrid`, `UnitDefCache`, `MapCacheFile`, `BarClient`), `SkiaViewer` 1.1.3-dev, `SkiaSharp` 2.88.6, `BarData` (NuGet from local store), `xUnit 2.9.x`. **No new NuGet dependencies.** (030-gameviz-state-api)
- N/A (in-memory only, no persistence changes) (030-gameviz-state-api)
- Bash (run.sh) + F# 9 on .NET 10.0 (bot scripts) + FSBar.Client (BarClient, GameState), FSBar.Viz (GameViz, VizConfig, OverlayKind, LayerKind, VizDefaults) (031-full-trainer-viz-run)
- F# 9 on .NET 10.0 + FSBar.Client (GameState, MapGrid, UnitDefCache), FSBar.Viz (GameViz, SceneBuilder, VizTypes), SkiaViewer 1.1.3-dev, SkiaSharp 2.88.6 (032-lockfree-viewer-dataflow)
- F# 9 on .NET 10.0 + FSBar.Viz (in-repo), SkiaViewer 1.1.3-dev, SkiaSharp 2.88.6, System.Text.Json (BCL) (033-viz-style-configurator)
- JSON files on disk (`viz-presets/` directory) for presets (033-viz-style-configurator)
- F# 9 on .NET 10.0 (exclusive per constitution §Engineering Constraints) + FsGrpc 1.0.6 (protobuf), BarData (NuGet local feed), SkiaViewer 1.1.3-dev (local nupkg), SkiaSharp 2.88.6, Silk.NET 2.22.0, xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x. No new dependencies introduced. (034-repo-cleanup)
- Filesystem only — committed `.baseline` text files, `viz-presets/*.json`, `bots/trainer/map-cache/*.json`. No persistence format changes. (034-repo-cleanup)
- F# 9 on .NET 10.0 (exclusive per constitution §Engineering Constraints) + FSBar.Client, FSBar.Viz, FSBar.SyntheticData (in-repo), FSBar.Proto (in-repo, extended with `proto/hub/scripting.proto`), SkiaViewer 1.1.3-dev, Grpc.AspNetCore 2.67.0, Grpc.Core.Api 2.67.0, FsGrpc 1.0.6, xUnit 2.9.x. Bundled HighBarV2 proxy under `proxy/bundled/<version>/`. (035-central-gui-hub)
- F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints) + Existing in-repo only — `FSBar.Hub`, (038-hub-viewer-fixes)
- `$XDG_CONFIG_HOME/fsbar-hub/settings.json` — one additive (038-hub-viewer-fixes)
- F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints) + Existing in-repo — `FSBar.Client`, `FSBar.Hub`, `FSBar.Viz`, `FSBar.Proto`. BCL `System.Net.Sockets.UdpClient` + `System.Threading.Channels` for the autohost socket. `Grpc.AspNetCore 2.67.0` / `Grpc.Core.Api 2.67.0` already in the graph for scripting. `SkiaViewer` 1.1.3-dev for UI. `xUnit 2.9.x` for tests. **No new NuGet dependencies.** (039-hub-admin-channel)
- In-memory only — `AdminChannelStatus` lives for the session's lifetime; no persistence. `HubSettings` is not extended (engine speed does NOT persist across launches per Session 2026-04-17 Q5). (039-hub-admin-channel)
- F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints). + Existing in-repo only — `FSBar.Proto`, `FSBar.Client`, `FSBar.Viz`, `FSBar.Hub`, `FSBar.Hub.App`. NuGet: `Grpc.AspNetCore 2.67.0`, `Grpc.Core.Api 2.67.0`, `FsGrpc 1.0.6`, `SkiaSharp 2.88.6`, `SkiaViewer 1.1.3-dev` (local feed), `BarData` (local feed), `xUnit 2.9.x`, `Microsoft.NET.Test.Sdk 17.x`. **No new NuGet dependencies.** (040-grpc-full-hub-ui)
- Filesystem only — unchanged from pre-feature. `$XDG_CONFIG_HOME/fsbar-hub/settings.json` (HubSettings), `viz-presets/*.json` (style presets), `bots/trainer/map-cache/*.json` (map analysis). `HubStateStore` and `HeadlessRenderer` are in-memory only. (040-grpc-full-hub-ui)
- F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints). + Existing in-repo only — `FSBar.Proto`, `FSBar.Client`, `FSBar.Viz`, `FSBar.Hub`, `FSBar.Hub.App`. NuGet: `Grpc.AspNetCore 2.67.0`, `Grpc.Core.Api 2.67.0`, `FsGrpc 1.0.6`, `SkiaSharp 2.88.6`, `SkiaViewer 1.1.3-dev` (local feed), `BarData` (local feed), `xUnit 2.9.x`, `Microsoft.NET.Test.Sdk 17.x`. **No new NuGet dependencies.** (040-grpc-full-hub-ui)
- Filesystem only — unchanged from pre-feature. `$XDG_CONFIG_HOME/fsbar-hub/settings.json` (HubSettings), `viz-presets/*.json` (style presets), `bots/trainer/map-cache/*.json` (map analysis). `HubStateStore` and `HeadlessRenderer` are in-memory only. (040-grpc-full-hub-ui)
- F# 9 on .NET 10.0 (exclusive per Constitution + Existing in-repo only — `FSBar.Proto`, (041-hub-040-followups)
- Filesystem only — unchanged from feature 040. Overlay (041-hub-040-followups)
- F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints). + Existing in-repo only — `FSBar.Proto`, `FSBar.Client`, `FSBar.Viz`, `FSBar.Hub`, `FSBar.Hub.App`. NuGet: `Grpc.AspNetCore 2.67.0`, `Grpc.Core.Api 2.67.0`, `FsGrpc 1.0.6`, `SkiaSharp 2.88.6`, `SkiaViewer 1.1.3-dev` (local feed), `BarData` (local feed), `xUnit 2.9.x`, `Microsoft.NET.Test.Sdk 17.x`. **No new NuGet dependencies.** (042-grpc-log-stream)
- Filesystem only — unchanged from feature 041. `HubSettings.MaxLogStreamSubscribers` persists in `$XDG_CONFIG_HOME/fsbar-hub/settings.json` (schema v3, one-field bump from v2). `HubLog` subscriber state is in-memory only, released within 1 s of gRPC channel close (FR-013). (042-grpc-log-stream)
- F# 9 / .NET 10.0 + `FSBar.Hub`, `FSBar.Hub.App`, `FSBar.Proto`, `FSBar.Client` (in-repo); `Grpc.AspNetCore 2.67.0`, `FsGrpc 1.0.6` (transitive via Hub); `Grpc.Net.Client 2.67.*` (new, explicit — needed for `GrpcChannel.ForAddress` in the fixture); `xUnit 2.9.x`, `Xunit.SkippableFact 1.4.13`, `Microsoft.NET.Test.Sdk 17.x` (043-grpc-hub-testsuite)
- N/A — test-only; preset cleanup uses `test-043-*` prefix naming convention (043-grpc-hub-testsuite)

- F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf generation), FsGrpc.Tools 1.0.6 (build-time), BarData (NuGet from local store) (001-fsharp-repl-client)

## Project Structure

```text
src/
├── FSBar.Proto/            # generated protobuf types
├── FSBar.Client/           # core client (connection, protocol, game state, map analysis)
├── FSBar.SyntheticData/    # scene + economy + SyntheticMapGrid builders
└── FSBar.Viz/              # visualization (GameViz, SceneBuilder, LayerRenderer, configurator)

tests/
├── Common/                          # shared compile-included helpers (SurfaceAreaHelper.fs)
├── FSBar.Client.Tests/              # unit tests for FSBar.Client (+ Baselines/)
├── FSBar.SyntheticData.Tests/       # unit tests for FSBar.SyntheticData (+ Baselines/)
├── FSBar.Viz.Tests/                 # unit tests for FSBar.Viz (+ Baselines/)
├── FSBar.LiveTests/                 # integration tests against a real BAR engine (Live* prefix)
├── engine-version.json / ENGINE-VERSION.md
├── run-all.sh                       # engine-aware wrapper around `dotnet test`
└── README.md                        # test taxonomy + ownership table
```

`FSBarV1.slnx` lists all 8 projects. Top-level commands: `dotnet build FSBarV1.slnx`
and `dotnet test FSBarV1.slnx`. Surface-area checks are a thin per-project wrapper
over `tests/Common/SurfaceAreaHelper.fs` — set `SURFACE_AREA_UPDATE=1` to regenerate
baselines after an intentional `.fsi` change.

F# style: no `private` / `internal` modifiers in non-generated source. Public
surface is gated exclusively by `.fsi` signature files per constitution §II.

## Map analysis caching

Static per-map analysis (chokepoints + MapGrid) is committed under
`bots/trainer/map-cache/*.json`. The authoritative contract is
`src/FSBar.Client/MapCacheFile.fsi` (`schemaVersion`, `codeVersion`,
`SupportedMap`, `write`/`read`/`formatLoadError`). Regenerate via
`bots/trainer/map-cache/refresh-all.sh` after bumping `codeVersion`.
The trainer warmup reads these via `MapCacheFile.read` and hard-aborts
on any mismatch per FR-006.

## Unit glyph renderer (feature 028-unit-viz-language)

The information-dense unit renderer lives in `FSBar.Viz.UnitGlyph`
behind the `VizConfig.UseGlyphRenderer` flag (default `true`). Key
modules:

- `src/FSBar.Viz/UnitGlyph.fsi` — public renderer API (`classifyShape`,
  `classifyTier`, `classifyFaction`, `buildUnit`, `buildOverlayLayer`,
  `buildUnitsGlyph`, `advanceEffects`, `statusLine`, `resetSession`).
- `src/FSBar.Viz/UnitGlyphPalettes.fsi` — faction + team palettes and
  the default `UnitGlyphStyle`.
- `src/FSBar.Viz/UnitLabels.generated.fs(i)` — committed 2- or 3-char
  label table for every unit in `BarData.AllUnitDefs`. Regenerate via
  `dotnet fsi src/FSBar.Viz/scripts/gen-unit-labels.fsx [--clean]`. The
  script exits non-zero if an existing label would change without a
  genuine collision (SC-006 tripwire).
- `src/FSBar.Viz/UnitLabelsGenerator.fsi` — the pure two-pass label
  generator (research.md R3: name-derived letter pairs, then an
  alphabetical pool sweep for overflow).
- `src/FSBar.Viz/SyntheticDataAdapter.fsi` — adapter from
  `FSBar.SyntheticData.Scene + GameState` to `UnitDisplay seq`. Live-
  game wiring via `FSBar.Client.TrackedUnit` is a follow-up feature.

Hotkeys in `GameViz`: `W` weapon ranges, `L` sight, `C` command queue,
`N` full names (sticky toggles), `P` style configurator panel.
`UnitGlyph.statusLine` projects the active overlays to a `WLCN`-ordered
string for the status-line widget.

## Unit display adapter + encyclopedia data (feature 038)

`FSBar.Viz.UnitDisplayAdapter` is the single source-of-truth
constructor for `UnitDisplay` values across every Hub surface (Viewer
tab, Units-tab encyclopedia, Style-tab preview). Every caller that
hands a `UnitDisplay` to `UnitGlyph.buildUnit` must go through one of
its three constructors (FR-002):

- `ofTrackedUnit: defCache -> teamId -> unitId -> TrackedUnit -> UnitDisplay`
- `ofTrackedEnemy: defCache -> enemyId -> TrackedEnemy -> UnitDisplay`
- `ofEncyclopediaEntry: EncyclopediaEntry -> pinnedFootprint:float32 -> UnitDisplay`

The adapter classifies via the same `UnitGlyph.classifyShape` /
`classifyTier` / `classifyFaction` helpers the encyclopedia uses, with
`canMove = m.canFly || m.movementClass <> None`. `GameViz.resolveDefPropsFromBarData`
also unifies on this derivation so standalone GameViz and
Hub-Viewer-via-SceneBuilder produce byte-identical glyphs.

`FSBar.Viz.EncyclopediaData` holds the `EncyclopediaEntry` record and
`buildFromBarData` materialiser lifted out of
`FSBar.Hub.App.Tabs.EncyclopediaTab`, so the adapter and the Units-tab
consume one type.

`SceneBuilder.buildSceneHeadlessSized` / `buildSceneHeadlessView`
gained a `defCache: UnitDefCache option` parameter; `None` keeps the
legacy placeholder path. The hub's `ViewerTab` threads
`Some state.UnitDefs` through so on-field glyphs are classified
identically to the encyclopedia.

### Hub pause + Start-paused (feature 038)

`SessionManager.Launch(config, startPaused)` issues a single `/pause`
chat command on the first `Running` transition when `startPaused = true`.
`SessionManager.IsPaused` reports the hub-known pause state;
`TogglePause()` backs the Viewer-tab ⏸/▶ button. Note the known drift:
if the user types `/pause` in BAR's native UI, the hub's flag doesn't
reconcile — click the button twice to recover.

`HubSettings` carries `StartPausedDefault: bool` (default `true`) and
`LaunchGraphicalViewerDefault: bool` (default `false`). Both are
user-togglable via checkboxes on the Setup tab and persist across Hub
restarts in `$XDG_CONFIG_HOME/fsbar-hub/settings.json`.

### Direction triangle (feature 038 US4)

`UnitGlyph.buildUnit`'s facing pip is a 4-command triangle path whose
apex tracks `UnitDisplay.HeadingRadians`. Suppressed for
`MovementShape.Building` (FR-010). Static previews pass
`HeadingRadians = 0.0f` and inherit the canonical east-facing
orientation of the shape outline (FR-010a).

## Style configurator (feature 033-viz-style-configurator)

Press `P` in the live viewer to toggle a 280-pixel side panel on the
right edge. Every `VizConfig` / `UnitGlyphStyle` attribute is exposed
via typed descriptors with appropriate controls (color swatch cycling,
sliders, toggles, enum cycling). Changes apply within one frame.

Modules:

- `src/FSBar.Viz/ConfigDescriptors.fsi` — static registry of
  `AttributeDescriptor` records (key, label, category, input kind,
  get/set, default, range). Single source of truth for what's editable.
- `src/FSBar.Viz/ConfigPanel.fsi` — declarative panel rendering +
  mouse/scroll input handling. Returns `ConfigPanelInputResult` with
  optional updated `VizConfig` and optional `ConfigPanelAction`.
- `src/FSBar.Viz/StylePreset.fsi` — JSON preset persistence under
  `viz-presets/<name>.json` (gitignored, user-local). `fromConfig` /
  `applyToConfig` convert via `ConfigDescriptors`.

Add a new visual attribute by extending `ConfigDescriptors.all` (one
entry) — the panel picks it up automatically and presets roundtrip it.

Regenerate the label table whenever `nupkg/BarData.*.nupkg` changes.
Keep the `.fsi` for `UnitLabels.generated` stable — the generator only
rewrites the `.fs`.

## Hub state-store routing convention (feature 041)

Every Hub-GUI tab reads its authoritative state through
`HubStateStore.current store` and writes back via the typed
mutators (`setVizConfig`, `setVizAttribute`, `setEncyclopedia`,
`setSettings`, `setActiveTab`). The `Program.fs` entrypoint exposes
two thunks — `getActiveTab ()` and `getVizConfig ()` — plus a
`getSettings ()` reader. Tab call sites that previously held a
`let mutable vizConfig` / `let mutable activeTab` mirror were
removed in feature 041 (FR-017..FR-022).

`HubStateStore.set*` mutators emit a single
`HubEvent.DiagnosticsLine Warning` when they return
`SubmitOutcome.Rejected reason` (FR-023a / R7). Format:
`HubStateStore.<mutator> rejected: <reason>`. Callers silently
roll back by reading `HubStateStore.current` again — they do NOT
need to add their own warning emit.

Adding a new tab that needs to participate in this routing:
1. Take `store: HubStateStore.T` as a `render` / `handleInput`
   parameter (not the cached value).
2. Read every HubState-owned field through `(HubStateStore.current store).X`.
3. Write back via the typed mutator; ignore the `SubmitOutcome` —
   the next render reads the authoritative value anyway.
4. Local presentation state (scroll positions, toasts, drag
   anchors) stays in the per-tab `let mutable <Tab>State` record.

## gRPC parity for Hub UI (feature 040)

Feature 040 exposes every Hub-GUI action through the
`fsbar.hub.scripting.v1` gRPC service and adds a server-streaming RPC
that delivers the Viewer-tab's rendered Skia output to remote clients
as PNG/JPEG image bytes.

Three new `FSBar.Hub` modules:

- `HubStateStore` — central atomic-LWW store for active tab, VizConfig,
  Viewer camera, lobby, encyclopedia selection, preset cache, settings.
  Tabs and gRPC handlers read/write through it; every mutation emits an
  event on the `HubEventBus`.
- `HeadlessRenderer` — off-screen Viewer render pipeline with a
  per-subscriber `BoundedChannel<RenderFrameMessage>` (capacity 16,
  DropOldest). Rasterises via `Scene.recordPicture` →
  `SKCanvas.DrawPicture` on a raster `SKSurface` (GPU GRContext
  segfaults in this environment). Cap:
  `HubSettings.MaxRenderFrameSubscribers` (default 8, range 1–32).
- `OverlayLayerStore` — per-client, name-keyed overlay layers with
  declarative `OverlayPrimitive` DU (Line/Polyline/Polygon/Rectangle/
  Circle/Path/Text/Image) and `CoordinateSpace = World | Screen`.
  FR-026 cap matrix enforced in `putLayer` (16 layers/client, 500
  primitives/layer, 1 MiB/push, 256 KiB/image, 2048² dims, 4 KiB text).
  Auto-clean on `HubEvent.ScriptingClientDetached` via
  `wireDisconnectCleanup`.

Feature 040 does **not** introduce `PresetFacade` / `EncyclopediaFacade`;
the preset + encyclopedia RPCs call `FSBar.Viz.StylePreset` and
`FSBar.Viz.EncyclopediaData` directly from `ScriptingHub`.

New RPCs grouped by user story:

| Story | RPCs |
|-------|------|
| US1 (session orchestration) | `ConfigureLobby`, `ListMaps`, `ValidateLobby`, `LaunchSession`, `StopSession` |
| US2 (render frames) | `StreamRenderFrames`, `GetRenderFrame` |
| US3 (viz + camera) | `SetVizConfig`, `SetVizAttribute`, `ToggleOverlay`, `SetCamera`, `SetActiveTab` |
| US4 (preset / encyc / settings / proxy) | `ListPresets`, `SavePreset`, `LoadPreset`, `DeletePreset`, `ListUnits`, `SelectUnit`, `GetHubSettings`, `SetHubSettings`, `InstallProxy`, `RefreshProxyStatus` |
| US5 (state observation) | `GetHubState`, `StreamHubStateEvents` |
| US6 (client overlays) | `PutLayer`, `DeleteLayer`, `ListLayers`, `ClearLayers` |

`ScriptingService` constructor accepts:

```fsharp
new:
    sessions * events * busEvents * unitDefs * install * bundled *
    port * state * renderer * overlays * opts -> ScriptingService
```

where `state: HubStateStore.T`, `renderer: HeadlessRenderer.T`,
`overlays: OverlayLayerStore.T`, and
`busEvents: IObservable<HubEvent>` is used by `StreamHubStateEvents`.

FSI walkthroughs for each user story live under `scripts/examples/`:

- `17-hub-lobby-launch.fsx` — US1
- `18-hub-render-frames.fsx` — US2
- `19-hub-vizconfig-drive.fsx` — US3
- `20-hub-state-observer.fsx` — US5
- `21-hub-overlay-layers.fsx` — US6

The feature-039 `16-hub-admin.fsx` keeps working unchanged (SC-007
additive-only wire-contract guard — verified via `buf breaking`).

Env-var mapping to `SetActiveTab` / `SelectUnit`:

| Env var | Equivalent RPC |
|---------|----------------|
| `FSBAR_HUB_INITIAL_TAB=Viewer` | `SetActiveTab(HubTab.Viewer)` |
| `FSBAR_HUB_ENCYCLOPEDIA_SELECT=armcom` | `SelectUnit(InternalName="armcom")` |

## Commands

# Add commands for F# / .NET 10.0

## Testing

Always run tests against the live environment. Do not use mocks, fakes, or in-memory substitutes.

Tests that cannot pass due to out-of-scope issues (e.g., missing server, external dependency unavailable, unimplemented upstream feature) MUST be marked as skipped or have their assertions relaxed. Never mark a failing test as passed.

## Code Style

F# / .NET 10.0: Follow standard conventions

## Recent Changes
- 043-grpc-hub-testsuite: Added F# 9 / .NET 10.0 + `FSBar.Hub`, `FSBar.Hub.App`, `FSBar.Proto`, `FSBar.Client` (in-repo); `Grpc.AspNetCore 2.67.0`, `FsGrpc 1.0.6` (transitive via Hub); `Grpc.Net.Client 2.67.*` (new, explicit — needed for `GrpcChannel.ForAddress` in the fixture); `xUnit 2.9.x`, `Xunit.SkippableFact 1.4.13`, `Microsoft.NET.Test.Sdk 17.x`
- 042-grpc-log-stream: Added F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints). + Existing in-repo only — `FSBar.Proto`, `FSBar.Client`, `FSBar.Viz`, `FSBar.Hub`, `FSBar.Hub.App`. NuGet: `Grpc.AspNetCore 2.67.0`, `Grpc.Core.Api 2.67.0`, `FsGrpc 1.0.6`, `SkiaSharp 2.88.6`, `SkiaViewer 1.1.3-dev` (local feed), `BarData` (local feed), `xUnit 2.9.x`, `Microsoft.NET.Test.Sdk 17.x`. **No new NuGet dependencies.**
- 041-hub-040-followups: Added F# 9 on .NET 10.0 (exclusive per Constitution + Existing in-repo only — `FSBar.Proto`,


<!-- MANUAL ADDITIONS START -->

## Central GUI hub (feature 035-central-gui-hub)

`src/FSBar.Hub/` is the packable core library (`HubSettings`,
`BarInstall`, `BundledProxy`, `ProxyInstaller`, `LobbyConfig`,
`SessionManager`, `ScriptingHub`, `HubEvents`) and
`src/FSBar.Hub.App/` is the GUI executable that binds those into a
SkiaViewer window. The two projects keep GUI deps out of the
packable lib so downstream scripting tooling can consume just
`FSBar.Hub` (or even `FSBar.Proto` alone for the wire contract).

Run with:

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet run --project src/FSBar.Hub.App
```

Environment variables useful for CI smoke tests:

| Var | Effect |
|-----|--------|
| `FSBAR_HUB_SCREENSHOT_DIR` | Take a screenshot after a settle delay and exit cleanly |
| `FSBAR_HUB_AUTO_LAUNCH=1` | Fire SetupTab.Launch immediately (needs `FSBAR_HUB_SCREENSHOT_DIR`) |
| `FSBAR_HUB_SCREENSHOT_WAIT_MS=N` | Extra delay before the screenshot so more units are visible |
| `FSBAR_HUB_INITIAL_TAB=Setup|Viewer|Units|Style|Settings|Grpc` | Land on a specific tab |
| `FSBAR_HUB_ENCYCLOPEDIA_SELECT=<name>` | Pre-select a unit in the Units tab |
| `FSBAR_HUB_BUNDLED_PROXY_DIR=/path/to/proxy` | Override bundled-proxy root for dev runs |

Core library modules live under `src/FSBar.Hub/` with `.fsi`
signatures and surface-area baselines in
`tests/FSBar.Hub.Tests/Baselines/`. The app-layer tabs at
`src/FSBar.Hub.App/Tabs/*.{fsi,fs}` compose `FSBar.Viz.ConfigPanel`
/ `FSBar.Viz.SceneBuilder.buildSceneHeadlessSized` /
`FSBar.Viz.UnitGlyph.buildUnit` directly — so Units-tab glyphs
byte-match Viewer-tab glyphs (SC-003).

## Hub admin channel (feature 039)

The Hub opens a loopback UDP autohost channel to the engine at every
session launch so pause / resume / engine-speed / force-end / admin
message work as real engine operations rather than cosmetic hub-side
flags. Replaces the feature-038 chat-based path entirely.

Wire-level client: `FSBar.Client.AdminChannel` binds `127.0.0.1:0` via
`AdminChannel.bind()` before spawning the engine; `SessionManager.Launch`
captures the OS-assigned port and threads it into `EngineConfig.AutohostPort`
which `ScriptGenerator.generateSpringSettings` emits into the per-session
`springsettings.cfg`. Wire format is documented in
`specs/039-hub-admin-channel/contracts/autohost-wire.md`
(outbound 4 SETGAMESPEED / 5 PAUSE / 8 SAYMESSAGE / 0 KILLSERVER;
inbound 0 SERVER_STARTED / 1 SERVER_QUIT / 2 SERVER_STARTPLAYING /
11 GAME_WARNING).

Hub-level orchestrator: `FSBar.Hub.AdminChannelHost` wraps one
`AdminChannel`, coalesces rapid same-kind submits within a 100 ms
quiet window (research.md §R5), tracks `AdminChannelStatus`
(Attached / Unavailable(reason) / Lost(reason)), and publishes every
status transition as `HubEvent.AdminChannelStatusChanged`.

`SessionManager` exposes five new admin members — each returns an
`AdminChannelHost.SubmitOutcome` (`Sent` / `Coalesced n` / `Rejected r`):

- `Pause: unit -> SubmitOutcome`
- `Resume: unit -> SubmitOutcome`
- `SetEngineSpeed: float32 -> SubmitOutcome`
- `ForceEnd: unit -> SubmitOutcome`
- `SendAdminMessage: string -> SubmitOutcome`

`TogglePause()` survives as a convenience (dispatches to `Pause`/`Resume`
based on `IsPaused`). `SetPaused(bool)` from feature 038 is gone.

`startPaused = true` launches defer their initial `Pause true` until
the autohost `ServerStartPlaying` event arrives so the sim is truly
frozen at engine frame zero (research.md §R9).

Viewer-tab toolbar (`src/FSBar.Hub.App/Tabs/ViewerTab.fs`) grows a
top-right admin toolbar: `⏸ ⏹ [0.5x 1x 2x 5x 10x] [admin message]`.
Clicks dispatch via the module-private `AdminToolbarAction` + the
public `ViewerTab.handleMouse` entry point. Controls render
disabled (dimmed) when `SessionManager.AdminStatus <> Some Attached`
and an inline status line shows the reason (FR-009).

Scripting service (`proto/hub/scripting.proto`) gained five unary RPCs
— `Pause` / `Resume` / `SetEngineSpeed` / `ForceEndMatch` /
`SendAdminMessage` — each returning an `AdminSubmitResult` that echoes
the resulting `AdminChannelStatusInfo`. `ActiveSession` gained an
optional `admin_channel_status` field. See
`scripts/examples/16-hub-admin.fsx` for an end-to-end gRPC walkthrough.

Unit test coverage: `tests/FSBar.Client.Tests/AdminChannelCodecTests.fs`
(wire format round-trip), `tests/FSBar.Hub.Tests/AdminChannelHostTests.fs`
(host rejection semantics, status transitions). Live coverage:
`tests/FSBar.Hub.LiveTests/LiveAdminPauseTests.fs` +
`LiveAdminSpeedTests.fs` + `LiveAdminForceEndTests.fs` +
`LiveAdminMessageTests.fs` + `LiveScriptingAdminPauseTests.fs` +
`LiveAdminChannelLossTests.fs` — all marked `[<Trait("Category", "AdminChannel")>]`
so `dotnet test --filter "Category=AdminChannel"` runs the full live matrix.

## Hub log stream (feature 042)

`FSBar.Hub.HubLog` is the canonical in-process log-emit surface for
the Hub. Every subsystem that wants to surface diagnostics beyond the
`HubEventBus` (session lifecycle, admin-channel wire trace, RPC
dispatch, proxy install steps, preset persistence, lobby validation,
settings load/save, state-store mutations, headless-renderer ticks)
calls `HubLog.emit` / `HubLog.emitSimple` / `HubLog.emitFromDiagnosticsLine`.

The bus is constructed once per hub process in `Program.fs`:

```fsharp
let hubLog = HubLog.create bus.Sink getSettings
sessions |> Option.iter (fun sm -> sm.AttachLog hubLog)
```

Emit is O(1) when no subscriber is attached — a volatile-read gate on
subscriber count short-circuits before any `LogEntry` allocation or
message formatting (R1 / FR-016).

Log categories (closed F# DU, mirrored 1:1 by the proto enum):
`SessionManager`, `AdminChannel`, `ScriptingHub`, `ProxyInstall`,
`HeadlessRenderer`, `HubStateStore`, `PresetPersistence`, `Lobby`,
`Settings`.

Default filter (empty `LogFilterWire`): all categories, `Info` floor.
`Debug` entries are only delivered to subscribers that explicitly
lower the floor (FR-005a / Clarifications Q2).

Shipped presets:

| Preset name | Categories | MinSeverity |
|-------------|------------|-------------|
| `session-lifecycle` | SessionManager + AdminChannel + ProxyInstall | Info |
| `admin-channel` | AdminChannel | Debug |
| `scripting-wire` | ScriptingHub | Debug |

Correlation IDs flow via `FSBar.Hub.CorrelationId.ServerInterceptor`
— registered in `Program.fs` on the Kestrel gRPC pipeline — which
reads the request-metadata header `x-fsbar-correlation-id` (optional,
≤ 64 UTF-8 bytes), assigns a fresh `Guid.NewGuid().ToString("N")`
when absent, stores the effective ID in `AsyncLocal<_>`, and echoes
it on the response trailer. Background tasks in RPC handlers should
capture `CorrelationId.current ()` outside the task and re-scope with
`use _ = CorrelationId.withScope cid` inside.

`FSBar.Hub.DispatchTracer.DebugDispatchInterceptor` runs after the
correlation interceptor and emits one `ScriptingHub`/`Debug` entry
on RPC entry and another on completion (with elapsed ms).

Subscriber cap: `HubSettings.MaxLogStreamSubscribers` (default `8`,
range `[1, 32]`, schema v3). Overflow attempts receive gRPC
`ResourceExhausted` with the cap named in the reason (FR-015a).

Per-subscriber buffer: `BoundedChannel<LogEntry>` capacity 256 with
`BoundedChannelFullMode.DropOldest`. Drop counts are carried on the
next delivered entry's `dropped_since_last` field (FR-012).

Per-entry message cap: 8 KiB UTF-8 including the trailing marker
` …[truncated N bytes]` (FR-012a / R6). Truncation happens once
per emit (before fan-out) so every subscriber sees byte-identical
content.

`FSBar.Client.SessionManager.AttachLog` and
`FSBar.Hub.AdminChannelHost.AdminChannelHost.AttachLog` plug the log
bus into the session and admin-channel host so state transitions and
wire-level events surface on the stream. `SessionManager` propagates
the bus down to its per-session `AdminChannelHost` on every Launch.

Example walkthrough: `scripts/examples/22-hub-log-stream.fsx`.
Live coverage: `tests/FSBar.Hub.LiveTests/LiveAdminChannelLogStreamTests.fs`
tagged `[<Trait("Category", "LogStream")>]`; run the full live matrix
via `dotnet test --filter "Category=LogStream"`.

## Hub scripting proto regeneration

`FSBar.Proto` generates F# code from `proto/highbar/*.proto` and
`proto/hub/scripting.proto` via `cd proto && buf generate`. Generated
files are committed under `src/FSBar.Proto/Generated/` so a plain
`dotnet build` works without the plugin installed.

Regenerating requires the `protoc-gen-fsgrpc` plugin on PATH. **No
prebuilt binary is distributed**, and `FsGrpc.Tools 1.0.6` is not on
nuget.org. Install from source via the helper script in the sibling
`fsGRPCSkills` repo:

```bash
~/tools/fsGRPCSkills/fsgrpc-setup/scripts/install-protoc-gen-fsgrpc.sh
```

The script clones `dmgtech/fsgrpc@a52b8a7`, patches it to skip optics
emission (so generated code compiles against `FsGrpc 1.0.6`), publishes
for the current TFM, and drops a wrapper at `~/.local/bin/protoc-gen-fsgrpc`.
See the script's `--help` for why the patch is necessary.

After regeneration, verify `dotnet build FSBarV1.slnx` succeeds and that
the committed `highbar/*.gen.fs` files weren't gratuitously rewritten.

## FSI MCP Server

The FSI MCP server (`fsi-server`) runs at `http://127.0.0.1:5020/sse` and provides an F# Interactive session via MCP tools.

### Critical: DLL references are locked

FSI locks DLLs loaded via `#r`. After rebuilding a project, you **must restart FSI** to pick up the new DLLs. Use the `restart_fsi` MCP tool to do this without restarting the entire MCP server.

### Starting the MCP server

The server binary is at `/home/developer/tools/fsi-mcp-server/server/`. Start it with:
```
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 dotnet run --no-build
```
- `XDG_RUNTIME_DIR` is required for GLFW windowing (Silk.NET viz)
- `DISPLAY=:0` is required for graphical windows

### Loading FSBar assemblies in FSI

Before loading `#r` references, preload native libraries with `dlopen`:
```fsharp
open System.Runtime.InteropServices
[<DllImport("libdl.so.2")>]
extern nativeint dlopen(string filename, int flags)
let np = "/home/developer/projects/FSBarV1/tests/FSBar.Viz.Tests/bin/Debug/net10.0/runtimes/linux-x64/native"
let _ = dlopen(np + "/libglfw.so.3", 0x2 ||| 0x100)
let _ = dlopen(np + "/libSkiaSharp.so", 0x2 ||| 0x100)
```

Load DLLs from the test output directory (has all transitive dependencies):
```
#r ".../tests/FSBar.Viz.Tests/bin/Debug/net10.0/<DllName>.dll"
```

### GameViz notes

- The SkiaSharp GPU backend (GRContext) segfaults in this environment. The Viewer uses a raster SKSurface + GL texture upload instead.
- `Silk.NET.Windowing.Glfw.GlfwWindowing.RegisterPlatform()` must be called before `Window.Create` (done in Viewer.fs).
- `getCornersHeightMap` is live (HighBar commit `c70559a` / feature 026-corners-heightmap-callback, consumed by FSBarV1 via feature 006). Verified 2026-04-13 live probe on Avalanche 3.4: returns 263169 = (513 × 513) float32 corner vertices with min 130.0 / max 700.0. If GameViz sees an empty heightmap, the cause is elsewhere (e.g., query happened pre-Start, or MapGrid reshape bug) — not a missing proxy callback.
- Throttle viz updates to ~60fps when running the game loop. Calling `onFrame` on every `Step()` at high game speed will consume 100% CPU.

### Graphical mode

- Always run the graphical engine in windowed mode (never fullscreen). The `EngineLauncher` writes `Fullscreen=0` to `springsettings.cfg` in each session directory automatically.

### Engine paths

Engine versions are **auto-detected** at runtime by the `EngineDiscovery` module. It scans `~/.local/state/Beyond All Reason/engine/recoil_*/` and selects the latest version. Override with `HIGHBAR_TEST_ENGINE` env var or `tests/engine-version.json`.

- Standard data dir: `~/.local/state/Beyond All Reason`
- Engine dir pattern: `~/.local/state/Beyond All Reason/engine/recoil_<YYYY.MM.DD>/`
- Headless binary: `spring-headless` (within engine version dir)
- Graphical binary: `spring` (within engine version dir)

### Upstream dependency workflow (SkiaViewer, BarData)

Both SkiaViewer and BarData are consumed as NuGet packages from the local `nupkg/` feed. Each upstream project has a `pack-dev.sh` script that produces a timestamp-versioned prerelease package (e.g., `1.0.0-dev.20260408T115727`), eliminating stale cache issues.

**Updating an upstream dependency:**
```bash
# In the upstream repo (e.g., SkiaViewer):
./pack-dev.sh ~/projects/FSBarV1/nupkg

# In FSBarV1 — just build, NuGet picks up the new version:
dotnet build
```

**Verifying dependency freshness:**
```bash
./scripts/check-deps.sh
```

PackageReferences use `Version="*-*"` wildcard to accept the latest prerelease version automatically. Do **not** use exact version pinning for local-feed packages.

<!-- MANUAL ADDITIONS END -->
