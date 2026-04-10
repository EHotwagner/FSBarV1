# Implementation Plan: Synthetic Visualization Test Data

**Branch**: `018-synthetic-viz-data` | **Date**: 2026-04-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/018-synthetic-viz-data/spec.md`

## Summary

Create a pure F# data generator that produces realistic sequences of 300 GameState/GameFrame snapshots using the real FSBar.Client types. Three distinct scenes (early-game buildup, mid-game skirmish, late-game siege) on different map sizes provide diverse test data for visualization development without requiring a live engine connection.

## Technical Context

**Language/Version**: F# / .NET 10.0
**Primary Dependencies**: FSBar.Client (in-repo, for types only — GameState, TrackedUnit, TrackedEnemy, EconomySnapshot, UnitDefCache, GameEvent, GameFrame)
**Storage**: N/A (in-memory only, pure functions)
**Testing**: xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x
**Target Platform**: Linux (same as FSBar.Client)
**Project Type**: Library
**Performance Goals**: Each scene generated in < 1 second
**Constraints**: No engine dependency, no network calls, deterministic output
**Scale/Scope**: 3 scenes x 300 frames = 900 GameState records total

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| I. Spec-First Delivery | PASS | Spec and plan created before implementation |
| II. Compiler-Enforced Structural Contracts | PASS | New modules will have `.fsi` signature files. Surface-area baselines will be added. |
| III. Test Evidence Is Mandatory | PASS | Validation tests will verify all scene invariants |
| IV. Observability and Safe Failure | PASS | Pure functions — no failure modes beyond invalid scene definitions |
| V. Scripting Accessibility | PASS | Quickstart and FSI prelude planned |
| F# exclusive stack | PASS | F# only |
| `.fsi` for public modules | PASS | Planned for Scenes.fsi and Validation.fsi |
| Surface-area baselines | PASS | Will be added for public modules |
| Packable via dotnet pack | PASS | Standard library project |

No violations. No complexity tracking needed.

## Project Structure

### Documentation (this feature)

```text
specs/018-synthetic-viz-data/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── public-api.md
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── FSBar.SyntheticData/
│   ├── FSBar.SyntheticData.fsproj
│   ├── SceneTypes.fsi
│   ├── SceneTypes.fs
│   ├── UnitDefs.fsi
│   ├── UnitDefs.fs
│   ├── EconomySim.fsi
│   ├── EconomySim.fs
│   ├── UnitSim.fsi
│   ├── UnitSim.fs
│   ├── EnemySim.fsi
│   ├── EnemySim.fs
│   ├── Scenes.fsi
│   ├── Scenes.fs
│   ├── Validation.fsi
│   └── Validation.fs
├── FSBar.SyntheticData.Tests/
│   ├── FSBar.SyntheticData.Tests.fsproj
│   ├── ValidationTests.fs
│   ├── SceneATests.fs
│   ├── SceneBTests.fs
│   ├── SceneCTests.fs
│   ├── ContinuityTests.fs
│   ├── SurfaceAreaTests.fs
│   └── Baselines/
│       ├── Scenes.baseline
│       └── Validation.baseline
└── FSBar.SyntheticData/scripts/
    ├── prelude.fsx
    └── examples/
        └── 01-generate-scene.fsx
```

**Structure Decision**: New `FSBar.SyntheticData` library project alongside existing `FSBar.Client`. References FSBar.Client for types only. Test project follows the same pattern as `FSBar.Client.Tests`.

## Post-Design Constitution Re-Check

| Gate | Status | Notes |
|------|--------|-------|
| II. `.fsi` signatures | PASS | All 6 public modules have planned `.fsi` files |
| II. Surface-area baselines | PASS | Baselines directory planned in test project |
| V. Scripting accessibility | PASS | `prelude.fsx` and example script planned |
| Packable | PASS | Standard `.fsproj` with PackageId |
