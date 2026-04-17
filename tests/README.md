# Test Suite Layout

All xUnit test projects live under this directory. To run the whole suite at once:

```bash
dotnet test FSBarV1.slnx          # from repo root
```

For live/engine-dependent tests that check prerequisites first:

```bash
./tests/run-all.sh                # wraps dotnet test, adds engine detection
```

## Test taxonomy

| Project | Kind | What it covers |
|---|---|---|
| `FSBar.Client.Tests` | Unit | Pure-F# logic in `FSBar.Client` (protocol, commands, game state, pathing, map analysis, surface-area). |
| `FSBar.SyntheticData.Tests` | Unit | Pure-F# logic in `FSBar.SyntheticData` (scene generation, economy sim, validation, surface-area). |
| `FSBar.Viz.Tests` | Unit (headless) | `FSBar.Viz` rendering and wiring — uses SkiaSharp raster surfaces. Some tests require `DISPLAY` and are skipped when it's absent (see `hasDisplay` in `VizEngineFixture.fs`). Surface-area check covers the 14 baseline-backed modules. |
| `FSBar.LiveTests` | Integration | End-to-end against a real Beyond All Reason engine. Auto-skipped when engine is missing; file-names prefixed `Live*` to disambiguate from unit-side counterparts. |

## Where a new test goes

- Testing a pure F# function in `FSBar.X`? Add `<FunctionName>Tests.fs` under `tests/FSBar.X.Tests/`.
- Testing behaviour that requires a running engine? Add `Live<Area>Tests.fs` under `tests/FSBar.LiveTests/`.
- Comparing `.fsi` public surface? Extend the appropriate project's `Baselines/*.baseline` file — the thin wrapper test (e.g. `ClientSurfaceAreaTests.fs`) picks it up automatically via `MemberData`.

## Shared helpers

`tests/Common/` holds loose `.fs` files that are compile-included by test projects via `<Compile Include="..\Common\..." />`. It is not a `.fsproj`.

- `SurfaceAreaHelper.fs` — compare-to-baseline helper used by all three per-project `*SurfaceAreaTests.fs` wrappers. Supports `SURFACE_AREA_UPDATE=1` (or legacy `UPDATE_BASELINES=true`) to regenerate baselines from their `.fsi` sources.

## Ownership table

| Source module | Owning test project |
|---|---|
| `FSBar.Client.*` (Connection, Protocol, Commands, Events, GameState, MapGrid, MapQuery, MapCache, MapCacheFile, Pathing, BasePlan, Chokepoints, WallIn, SmfParser, UnitDefCache, BarClient, ScriptGenerator, EngineConfig, EngineDiscovery, EngineLauncher, Callbacks) | `FSBar.Client.Tests` (unit) + `FSBar.LiveTests` (engine-side) |
| `FSBar.SyntheticData.*` (Scenes, SceneTypes, UnitSim, EnemySim, EconomySim, Validation, UnitDefs, SyntheticMapGrid) | `FSBar.SyntheticData.Tests` |
| `FSBar.Viz.*` (GameViz, LiveSession, PreviewSession, SceneBuilder, LayerRenderer, UnitGlyph, UnitGlyphPalettes, UnitLabels*, ColorMaps, ConfigPanel, ConfigDescriptors, StylePreset, MockSnapshot, MapData, SyntheticDataAdapter, VizTypes, VizDefaults) | `FSBar.Viz.Tests` |

## Running subsets

```bash
# Unit tests only (skip live engine):
dotnet test tests/FSBar.Client.Tests \
            tests/FSBar.SyntheticData.Tests \
            tests/FSBar.Viz.Tests

# Live/engine-dependent tests (requires BAR engine):
dotnet test tests/FSBar.LiveTests

# One project:
dotnet test tests/FSBar.Viz.Tests
```

## Baseline regeneration

Run this from the repo root if `.fsi` surface changes are intentional:

```bash
SURFACE_AREA_UPDATE=1 dotnet test FSBarV1.slnx --filter "FullyQualifiedName~SurfaceArea"
```

Review the resulting diff in `tests/**/Baselines/*.baseline`, commit alongside the `.fsi` change.
