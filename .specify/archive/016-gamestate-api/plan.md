# Implementation Plan: GameState API, Unit Debugging, and Permanent Map

**Branch**: `016-gamestate-api` | **Date**: 2026-04-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/016-gamestate-api/spec.md`

## Summary

Add a centralized `GameState` module to FSBar.Client that eliminates duplicated unit/event tracking across consumers, caches unit definitions for instant lookup, provides a unit watch/debugging system, and enhances MapCache with permanent queryable map functions. Existing BarClient API remains unchanged; new `StepTracked` methods are opt-in additions.

## Technical Context

**Language/Version**: F# / .NET 10.0
**Primary Dependencies**: FsGrpc 1.0.6 (protobuf), FSBar.Proto (generated types), BarData (unit definitions)
**Storage**: In-memory (Map, ConcurrentDictionary caches)
**Testing**: xUnit 2.9.x, Microsoft.NET.Test.Sdk — live engine tests (no mocks per CLAUDE.md)
**Target Platform**: Linux (headless engine + FSI REPL)
**Project Type**: Library (FSBar.Client) + REPL script (Repl.fsx) + Visualization (FSBar.Viz)
**Performance Goals**: Unit def lookup <1ms, metal spot query <1ms, per-frame state update within engine tick budget
**Constraints**: Fixed callback API (27 callbacks), synchronous protocol over Unix socket, ~2500 round-trips for init
**Scale/Scope**: Typical game has 1-200 units per team, ~500 unit definitions, map up to 512x512 heightmap

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Evidence |
|------|--------|----------|
| Spec-First (I) | PASS | Spec at `specs/016-gamestate-api/spec.md` with 12 FRs, 7 SCs, 5 user stories |
| Compiler-Enforced Contracts (II) | PASS | Plan includes `.fsi` files for all new modules (GameState, UnitWatch) |
| Test Evidence (III) | PASS | Surface area baselines + live integration tests planned |
| Observability (IV) | PASS | Unit watch system provides structured diagnostics; errors fail fast |
| Scripting Accessibility (V) | PASS | Repl.fsx updated to expose all new functionality via FSI helpers |
| F#-only stack | PASS | All code is F# |
| .fsi for every public module | PASS | GameState.fsi, UnitWatch.fsi, updated MapCache.fsi, BarClient.fsi |
| Surface area baselines | PASS | New baselines for GameState, UnitWatch; updated for MapCache, BarClient |

No violations. No complexity tracking needed.

## Project Structure

### Documentation (this feature)

```text
specs/016-gamestate-api/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/FSBar.Client/
├── GameState.fsi        # NEW — GameState types and module signature
├── GameState.fs         # NEW — GameState implementation
├── UnitWatch.fsi        # NEW — Unit watch/debug signature
├── UnitWatch.fs         # NEW — Unit watch/debug implementation
├── MapCache.fsi         # MODIFIED — add refreshDynamic, metalSpots, nearestMetalSpot, isPassable
├── MapCache.fs          # MODIFIED — implement new functions
├── BarClient.fsi        # MODIFIED — add StepTracked, StepTrackedWith, GameState property
├── BarClient.fs         # MODIFIED — implement tracked stepping
└── FSBar.Client.fsproj  # MODIFIED — add new files to compile order

src/FSBar.Viz/
├── GameViz.fsi          # MODIFIED — add onGameState
└── GameViz.fs           # MODIFIED — implement onGameState

scripts/examples/
└── Repl.fsx             # MODIFIED — use GameState, add watch/unwatch helpers

tests/FSBar.Client.Tests/             # NEW — created during implementation
├── FSBar.Client.Tests.fsproj         # NEW
├── SurfaceBaselineTests.fs           # NEW — mirrors FSBar.Viz.Tests pattern
├── GameStateTests.fs                 # NEW — live integration tests
├── Baselines/
│   ├── GameState.baseline            # NEW
│   ├── UnitWatch.baseline            # NEW
│   ├── MapCache.baseline             # NEW
│   └── BarClient.baseline            # NEW
```

**Structure Decision**: Single project extension — all new code goes into existing `FSBar.Client` project. No new projects needed. Compile order in `.fsproj`: GameState and UnitWatch are inserted after MapCache, before BarClient.
