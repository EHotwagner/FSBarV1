# FSBarV1 Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-16

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

- F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf generation), FsGrpc.Tools 1.0.6 (build-time), BarData (NuGet from local store) (001-fsharp-repl-client)

## Project Structure

```text
src/
tests/
```

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
`N` full names (sticky toggles). `UnitGlyph.statusLine` projects the
active overlays to a `WLCN`-ordered string for the status-line widget.

Regenerate the label table whenever `nupkg/BarData.*.nupkg` changes.
Keep the `.fsi` for `UnitLabels.generated` stable — the generator only
rewrites the `.fs`.

## Commands

# Add commands for F# / .NET 10.0

## Testing

Always run tests against the live environment. Do not use mocks, fakes, or in-memory substitutes.

Tests that cannot pass due to out-of-scope issues (e.g., missing server, external dependency unavailable, unimplemented upstream feature) MUST be marked as skipped or have their assertions relaxed. Never mark a failing test as passed.

## Code Style

F# / .NET 10.0: Follow standard conventions

## Recent Changes
- 029-trainer-viewer-options: Added Bash (run.sh CLI) + F# 9 on .NET 10.0 (bot scripts, helpers) + FSBar.Client (BarClient, EngineConfig), FSBar.Viz (GameViz, SceneBuilder, UnitGlyph), SkiaViewer (window management), SkiaSharp 2.88.6
- 028-unit-viz-language: Added F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints). + Existing in-repo — `FSBar.Viz` (`SceneBuilder`, `VizTypes`, `ColorMaps`), `FSBar.Client` (`GameState`, `UnitDefCache`), `FSBar.SyntheticData` (`Scenes`, `SceneTypes`), `SkiaViewer` 1.1.3-dev (declarative `Scene`), `SkiaSharp` 2.88.6, `BarData` (local nupkg feed), `xUnit 2.9.x`. **No new NuGet dependencies.**
- 027-map-terrain-viz: Added F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints). + Existing in-repo only — `FSBar.Client` (`MapCacheFile`, `MapGrid`, `MapQuery`), `FSBar.Viz` (`LayerRenderer`, `SceneBuilder`, `PreviewSession`, `GameViz`, `ColorMaps`, `VizTypes`), `SkiaViewer` 1.1.3-dev (`InputEvent.FrameTick`, `Scene`, `Shader.Image`), `SkiaSharp` 2.88.6, `xUnit 2.9.x`. **No new NuGet dependencies.**


<!-- MANUAL ADDITIONS START -->

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
