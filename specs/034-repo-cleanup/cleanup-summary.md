# 034 Cleanup Summary

One-page reference for features that previously depended on modules/files moved or removed in this cleanup. Use this when rebasing old branches onto master.

## Modules / files deleted and where the logic lives now

| Deleted | Replaced by |
|---|---|
| `tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs` (367 lines, reflection-based) | `tests/FSBar.Viz.Tests/VizSurfaceAreaTests.fs` (thin wrapper) + `tests/Common/SurfaceAreaHelper.fs` (shared compare-to-baseline) |
| `tests/FSBar.Client.Tests/SurfaceAreaTests.fs` legacy body | `tests/FSBar.Client.Tests/ClientSurfaceAreaTests.fs` (thin wrapper) + `tests/Common/SurfaceAreaHelper.fs` |
| `tests/FSBar.SyntheticData.Tests/SurfaceAreaTests.fs` placeholder | `tests/FSBar.SyntheticData.Tests/SyntheticDataSurfaceAreaTests.fs` (thin wrapper) |
| `tests/FSBar.Client.Tests/SyntheticMapGrid.fs` | `src/FSBar.SyntheticData/SyntheticMapGrid.{fs,fsi}` — now a proper library module |
| `tests/FSBar.Viz.Tests/VizEngineFixture.fs:testMapGrid` | `FSBar.SyntheticData.SyntheticMapGrid.build {| width; height; seed |}` |
| ~482 `private` + 14 `internal` keywords across 60 files | Nothing — `.fsi` files are the authoritative public-surface gate per constitution §II |

## Projects relocated

| Before | After |
|---|---|
| `src/FSBar.Client.Tests/` | `tests/FSBar.Client.Tests/` |
| `src/FSBar.Client.Tests/Baselines/` | `tests/FSBar.Client.Tests/Baselines/` (content byte-equal) |
| `src/FSBar.SyntheticData.Tests/` | `tests/FSBar.SyntheticData.Tests/` |

## Test files renamed (Live prefix)

| Before | After |
|---|---|
| `tests/FSBar.LiveTests/ConnectionTests.fs` | `LiveConnectionTests.fs` |
| `tests/FSBar.LiveTests/CommandTests.fs` | `LiveCommandsTests.fs` (also normalized to plural) |
| `tests/FSBar.LiveTests/EventTests.fs` | `LiveEventsTests.fs` (also normalized to plural) |
| `tests/FSBar.LiveTests/MapQueryTests.fs` | `LiveMapQueryTests.fs` |
| `tests/FSBar.LiveTests/MapGridTests.fs` | `LiveMapGridTests.fs` |

Per-project SurfaceArea wrappers were also renamed to disambiguate:

| Before | After |
|---|---|
| `tests/FSBar.Client.Tests/SurfaceAreaTests.fs` | `ClientSurfaceAreaTests.fs` |
| `tests/FSBar.SyntheticData.Tests/SurfaceAreaTests.fs` | `SyntheticDataSurfaceAreaTests.fs` |
| `tests/FSBar.Viz.Tests/SurfaceAreaTests.fs` (would-be) | `VizSurfaceAreaTests.fs` |

## Call-site migrations

| Old call | New call | Affected projects |
|---|---|---|
| `SyntheticMapGrid.flat w h` (from `FSBar.Client.Tests` module) | Same name; now `open FSBar.SyntheticData` then use `SyntheticMapGrid.flat w h` | `tests/FSBar.Client.Tests/` (4 files: BasePlanTests, ChokepointsTests, PathingTests, WallInTests) |
| `VizEngineFixture.testMapGrid w h` | `SyntheticMapGrid.build {\| width = w; height = h; seed = None \|}` | 44 call sites across `tests/FSBar.Viz.Tests/` |

## Baselines

- **New**: `tests/FSBar.SyntheticData.Tests/Baselines/SyntheticMapGrid.baseline` (first commit).
- **Path-renamed (content byte-equal)**: 21 Client baselines moved with their project.
- **Content-regenerated**: 8 pre-existing stale Viz baselines brought in sync with their `.fsi` (GameViz, LiveSession, MapData, MockSnapshot, SceneBuilder, UnitLabels.generated, UnitLabelsGenerator, VizTypes). These had drifted because `FSBar.Viz.Tests` used a reflection-based smoke test rather than baseline-compare. Covered by `contracts/baseline-invariant.md` §3.

## Script references updated

| File | Change |
|---|---|
| `.gitignore` | `src/FSBar.Client.Tests/TestResults/` → `tests/FSBar.Client.Tests/TestResults/` |
| `tests/run-all.sh` | Unit-tier path points at `tests/FSBar.Client.Tests/` |
| `bots/trainer/run.sh` | `dotnet build` path updated |
| `bots/trainer/helpers/prelude.fsx` | `#r` paths updated |
| `bots/trainer/README.md` / `PLAYBOOK.md` | Documentation updated |
| `scripts/examples/15-queued-move.fsx` | `#r` paths updated |
| `docs/tests.fsx` | Path references updated |
| `tests/FSBar.Client.Tests/MapCacheFileIntegrationTests.fs` | Comment updated |

## Summary numbers

- 8 projects now in `FSBarV1.slnx` (was 3).
- 0 `private`/`internal` in non-generated F# (was ~482 / ~14).
- Net lines: ~21,535 → ~21,150 (removed SurfaceBaselineTests.fs reflection block, centralized SyntheticMapGrid; keyword removal is lexical-only, no line delta).
- Unit-test pass count unchanged: 242 + 31 + 209 (delta comes from consolidating reflection-based Viz checks into baseline-compare — same coverage, fewer test rows).
