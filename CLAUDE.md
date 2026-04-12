# FSBarV1 Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-12

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

- F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf generation), FsGrpc.Tools 1.0.6 (build-time), BarData (NuGet from local store) (001-fsharp-repl-client)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for F# / .NET 10.0

## Testing

Always run tests against the live environment. Do not use mocks, fakes, or in-memory substitutes.

Tests that cannot pass due to out-of-scope issues (e.g., missing server, external dependency unavailable, unimplemented upstream feature) MUST be marked as skipped or have their assertions relaxed. Never mark a failing test as passed.

## Code Style

F# / .NET 10.0: Follow standard conventions

## Recent Changes
- 021-rerun-trainer-highbar: Added F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints). Trainer bot scripts are `.fsx` loaded by `dotnet fsi`. + existing in-repo `FSBar.Client`, `FSBar.Proto`, `FsGrpc 1.0.6`, `BarData` (NuGet from local store); rebuilt HighBarV2 `libSkirmishAI.so` from sibling `../HighBarV2` checkout (post `029-fix-trainer-issues` squash-merge). `System.Text.Json` (BCL). Bash for the runner.
- 020-bot-iterative-trainer: Added F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints). Trainer bot scripts are `.fsx` loaded by `dotnet fsi`. + existing in-repo `FSBar.Client`, `FSBar.Proto`, `FsGrpc 1.0.6`, `BarData` (NuGet from local store); `System.Text.Json` (BCL) for run artifacts; bash for the runner.
- 019-revamp-viz-library: Added F# / .NET 10.0 + SkiaViewer (latest prerelease, declarative Scene API), SkiaSharp 2.88.6, FSBar.Client (in-repo), FSBar.SyntheticData (in-repo)


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
- The engine proxy does not support `getCornersHeightMap` — heightmap data is empty. GameViz retries loading on each `onFrame` until data is available.
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
