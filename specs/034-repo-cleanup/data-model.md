# Data Model — Post-Cleanup Project Graph

**Feature**: 034-repo-cleanup
**Phase**: 1 (Design)

This feature has no runtime data entities — it is a repository refactor. The "data model" is the post-cleanup project graph: what files live where, which projects reference which, and which test project owns which baselines. This document is the authoritative target state for the tasks phase.

## Production projects (under `src/`)

### `FSBar.Proto`

- **Path**: `src/FSBar.Proto/FSBar.Proto.fsproj`
- **Purpose**: Generated protobuf types consumed by `FSBar.Client`.
- **Project references**: none.
- **Package references**: `FsGrpc` 1.0.6, `FsGrpc.Tools` 1.0.6.
- **Changes in this feature**: none (generated code excluded from cleanup).

### `FSBar.Client`

- **Path**: `src/FSBar.Client/FSBar.Client.fsproj`
- **Purpose**: Core client library — connection, protocol, game state, map analysis.
- **Project references**: `FSBar.Proto`.
- **Package references**: `BarData` (local nupkg, wildcard `*-*`).
- **Changes in this feature**:
  - `private`/`internal` keywords removed from all `.fs` files (not `.fsi`).
  - No `.fsi` edits (baselines stay byte-stable).
  - Consumers unchanged.

### `FSBar.SyntheticData`

- **Path**: `src/FSBar.SyntheticData/FSBar.SyntheticData.fsproj`
- **Purpose**: Synthetic game data builders for tests and demos.
- **Project references**: `FSBar.Client`.
- **Package references**: `BarData`.
- **Changes in this feature**:
  - **New module**: `SyntheticMapGrid.fs` + `SyntheticMapGrid.fsi`. Lifted from `src/FSBar.Client.Tests/SyntheticMapGrid.fs`. Public API: `SyntheticMapGrid.build : {width:int; height:int; seed:int option} -> FSBar.Client.MapGrid.MapGrid`.
  - **New baseline**: `tests/FSBar.SyntheticData.Tests/Baselines/SyntheticMapGrid.baseline` (created on first test run).
  - `private`/`internal` keyword removal in existing `.fs` files.

### `FSBar.Viz`

- **Path**: `src/FSBar.Viz/FSBar.Viz.fsproj`
- **Purpose**: Visualization library (GameViz, SceneBuilder, LayerRenderer, configurator).
- **Project references**: `FSBar.Client`, `FSBar.SyntheticData`.
- **Package references**: `SkiaViewer` (local, wildcard), `SkiaSharp` 2.88.6, `Silk.NET.Windowing/OpenGL/Input` 2.22.0.
- **Changes in this feature**:
  - `private`/`internal` keyword removal across `.fs` files (both hot and non-hot modules — the pass is keyword-only on hot paths).
  - Idiomatic style pass on cold modules only (`ConfigPanel.fs`, `ConfigDescriptors.fs`, `PreviewSession.fs`, `StylePreset.fs`) — explicit allowlist from research §R8.1. Hot-path modules (`GameViz.fs`, `SceneBuilder.fs`, `LayerRenderer.fs`, `UnitGlyph.fs`) get keyword removal only.
  - No `.fsi` edits.

## Test projects (under `tests/`)

### `tests/Common/` (new — shared helpers)

- **Path**: `tests/Common/` (directory, no `.fsproj`).
- **Purpose**: Shared test helpers compile-included by multiple test projects.
- **Contents**:
  - `SurfaceAreaHelper.fs` — canonical compare-to-baseline logic, lifted from `src/FSBar.Client.Tests/SurfaceAreaTests.fs`. Parameters: baseline directory path, assembly under test.
- **Inclusion pattern**: each consuming `.fsproj` adds `<Compile Include="..\Common\SurfaceAreaHelper.fs" />` before its own test files.

### `FSBar.Client.Tests` (moved from `src/`)

- **Path**: `tests/FSBar.Client.Tests/FSBar.Client.Tests.fsproj`.
- **Project references**: `FSBar.Client`, `FSBar.Proto`.
- **Package references**: xunit 2.9.*, Microsoft.NET.Test.Sdk 17.*.
- **Included from Common**: `..\Common\SurfaceAreaHelper.fs`.
- **Baselines**: `tests/FSBar.Client.Tests/Baselines/` — 21 files moved from `src/FSBar.Client.Tests/Baselines/`.
- **Files deleted in this feature**:
  - `SyntheticMapGrid.fs` — lifted to `FSBar.SyntheticData`.
  - `SurfaceAreaTests.fs` legacy body — replaced by a 5-line wrapper invoking the shared helper.

### `FSBar.SyntheticData.Tests` (moved from `src/`)

- **Path**: `tests/FSBar.SyntheticData.Tests/FSBar.SyntheticData.Tests.fsproj`.
- **Project references**: `FSBar.SyntheticData`, `FSBar.Client`.
- **Package references**: xunit, Test.Sdk.
- **Included from Common**: `..\Common\SurfaceAreaHelper.fs`.
- **Baselines**: `tests/FSBar.SyntheticData.Tests/Baselines/` — newly created; at minimum `SyntheticMapGrid.baseline` (for the lifted module).

### `FSBar.LiveTests` (renamed files; path unchanged)

- **Path**: `tests/FSBar.LiveTests/FSBar.LiveTests.fsproj`.
- **Project references**: `FSBar.Client`.
- **Package references**: xunit, Test.Sdk.
- **Files renamed** (basename normalization for SC-005):
  - `ConnectionTests.fs` → `LiveConnectionTests.fs`
  - `CommandTests.fs` → `LiveCommandsTests.fs` (also plural-normalized)
  - `EventTests.fs` → `LiveEventsTests.fs` (also plural-normalized)
  - `MapQueryTests.fs` → `LiveMapQueryTests.fs`
  - `MapGridTests.fs` → `LiveMapGridTests.fs` (consistency)
- **Unchanged**: `EngineFixture.fs`, `BarbRushTests.fs`, `.fsx` demo scripts.

### `FSBar.Viz.Tests` (path unchanged)

- **Path**: `tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj`.
- **Project references**: `FSBar.Viz`, `FSBar.Client`, `FSBar.SyntheticData`.
- **Package references**: xunit 2.9.3, Test.Sdk 17.14.1, coverlet.
- **Included from Common**: `..\Common\SurfaceAreaHelper.fs`.
- **Baselines**: `tests/FSBar.Viz.Tests/Baselines/` — 14 files unchanged.
- **Files deleted**:
  - `SurfaceBaselineTests.fs` (367 lines) — replaced by 5-line wrapper around shared helper.
  - `VizEngineFixture.fs:testMapGrid` — deleted; callers switch to `FSBar.SyntheticData.SyntheticMapGrid.build`.

## Solution file — `FSBarV1.slnx`

After cleanup:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/FSBar.Proto/FSBar.Proto.fsproj" />
    <Project Path="src/FSBar.Client/FSBar.Client.fsproj" />
    <Project Path="src/FSBar.SyntheticData/FSBar.SyntheticData.fsproj" />
    <Project Path="src/FSBar.Viz/FSBar.Viz.fsproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/FSBar.Client.Tests/FSBar.Client.Tests.fsproj" />
    <Project Path="tests/FSBar.SyntheticData.Tests/FSBar.SyntheticData.Tests.fsproj" />
    <Project Path="tests/FSBar.LiveTests/FSBar.LiveTests.fsproj" />
    <Project Path="tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj" />
  </Folder>
</Solution>
```

## Project reference graph

```
FSBar.Proto  ←  FSBar.Client  ←  FSBar.SyntheticData  ←  FSBar.Viz
                     ↑                 ↑                      ↑
                     |                 |                      |
                     |                 |      +---------------+
                     |                 |      |
                     +-----------------+------+----- (transitive via FSBar.Viz)
                     |                 |      |
                 Client.Tests   SyntheticData.Tests   Viz.Tests
                     ↑
                  LiveTests
```

Test projects (not shown as arrows into production projects for readability):

- `FSBar.Client.Tests` → `FSBar.Client`, `FSBar.Proto`
- `FSBar.SyntheticData.Tests` → `FSBar.SyntheticData`, `FSBar.Client`
- `FSBar.LiveTests` → `FSBar.Client`
- `FSBar.Viz.Tests` → `FSBar.Viz`, `FSBar.Client`, `FSBar.SyntheticData`

## Baseline ownership

| Baseline file | Owning project | Subject module |
|---|---|---|
| 21 files | `tests/FSBar.Client.Tests/Baselines/` | `FSBar.Client.*` (BarClient, BasePlan, Callbacks, Chokepoints, Commands, Connection, EngineConfig, EngineDiscovery, EngineLauncher, Events, GameState, MapCache, MapCacheFile, MapGrid, MapQuery, Pathing, Protocol, ScriptGenerator, SmfParser, UnitDefCache, WallIn) |
| 14 files | `tests/FSBar.Viz.Tests/Baselines/` | `FSBar.Viz.*` (ColorMaps, GameViz, LayerRenderer, LiveSession, MapData, MockSnapshot, PreviewSession, SceneBuilder, SyntheticDataAdapter, UnitGlyph, UnitGlyphPalettes, UnitLabels.generated, UnitLabelsGenerator, VizDefaults, VizTypes) |
| 1 file (new) | `tests/FSBar.SyntheticData.Tests/Baselines/` | `FSBar.SyntheticData.SyntheticMapGrid` |

Total after cleanup: 36 committed baselines. Only one new baseline (`SyntheticMapGrid.baseline`) is added — all others stay byte-stable.

## Access-modifier state

| State | Count |
|---|---|
| Before cleanup | ~482 `private` + ~14 `internal` across ~53 non-generated files |
| After cleanup | 0 `private` + 0 `internal` in non-generated paths |

Exempt paths (grep exclusions for SC-002 acceptance):

- `src/FSBar.Proto/Generated/**`
- Any `*.generated.fs` / `*.generated.fsi` (currently: `src/FSBar.Viz/UnitLabels.generated.fs(i)`)

## Scripts touched

| Script | Change |
|---|---|
| `tests/run-all.sh` | Update hardcoded paths to point at `tests/FSBar.Client.Tests/` (new) instead of `src/FSBar.Client.Tests/`. |
| `pack-dev.sh` | No change — references only production projects. |
| `CLAUDE.md` | Update "Project Structure" and "Recent Changes" sections; remove any references to deleted modules. |
| `tests/README.md` | New — test taxonomy (unit / viz / live) and ownership table. |
