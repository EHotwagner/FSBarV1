# Implementation Plan: Array2D Map Data Layers

**Branch**: `004-array-map-layers` | **Date**: 2026-04-05 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-array-map-layers/spec.md`

## Summary

Expose the five unwrapped engine map callbacks (heightmap, slope, LOS, radar, resource) as Array2D grid layers, bundle them into a unified `MapGrid` record type, add derived passability layers with caching, and provide coordinate-aware point/region query functions with terrain classification via discriminated unions and active patterns.

## Technical Context

**Language/Version**: F# / .NET 10.0  
**Primary Dependencies**: FsGrpc 1.0.6 (protobuf), FSBar.Proto (generated types), BarData (unit definitions)  
**Storage**: In-memory Array2D grids + ConcurrentDictionary caching  
**Testing**: xUnit 2.9.x + live engine integration tests (no mocks)  
**Target Platform**: Linux (F# Interactive / REPL)  
**Project Type**: Library (FSBar.Client) + FSI scripting  
**Performance Goals**: <5s per layer load; O(1) point queries; cached derived layers return instantly  
**Constraints**: All map layers must fit in memory (~tens of MB for 32x32 SMU maps)  
**Scale/Scope**: Single game session; maps up to 32x32 SMU (heightmap up to 4097x4097)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Evidence |
|------|--------|----------|
| I. Spec-First Delivery | PASS | Spec exists at `specs/004-array-map-layers/spec.md` with clarifications |
| II. Compiler-Enforced Structural Contracts | PLANNED | New modules (MapGrid, MapQuery, MapCache) will each have `.fsi` signature files; surface-area baselines will be created |
| III. Test Evidence Is Mandatory | PLANNED | Live integration tests per user story; xUnit tests against running engine |
| IV. Observability and Safe Failure Handling | PLANNED | Structured errors for empty arrays, out-of-bounds, connection failures; no silent fallbacks |
| V. Scripting Accessibility | PLANNED | prelude.fsx will expose MapGrid loading; new example script for map queries |
| F# exclusive stack | PASS | All F# |
| .fsi for public modules | PLANNED | MapGrid.fsi, MapQuery.fsi, MapCache.fsi |
| Packable via dotnet pack | PASS | FSBar.Client already packable; new modules added to same project |
| gRPC via fsgrpc | PASS | Existing FsGrpc infrastructure; callbacks.proto already defines IDs 52-56 |

No violations. No complexity tracking needed.

### Post-Phase 1 Re-Check

| Gate | Status | Evidence |
|------|--------|----------|
| II. Structural Contracts | DESIGNED | `.fsi` signatures defined in `contracts/public-api.md` for MapGrid, MapQuery, MapCache modules |
| III. Test Evidence | DESIGNED | Live integration tests planned: MapGridTests.fs (layer loading, dimensions, refresh), MapQueryTests.fs (point queries, bounds, terrain) |
| IV. Observability | DESIGNED | `Result<'T, string>` for coordinate queries; descriptive errors for empty arrays, dimension mismatches, connection failures |
| V. Scripting | DESIGNED | prelude.fsx additions + `05-map-layers.fsx` example script covering all major operations |

All gates pass. Ready for `/speckit.tasks`.

## Project Structure

### Documentation (this feature)

```text
specs/004-array-map-layers/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/FSBar.Client/
├── Callbacks.fsi        # MODIFY — add 5 new map callback signatures
├── Callbacks.fs         # MODIFY — add 5 new map callback implementations
├── MapGrid.fsi          # NEW — MapGrid record, Terrain DU, MoveType DU, active patterns
├── MapGrid.fs           # NEW — types, Array2D reshaping, terrain classification
├── MapQuery.fsi         # NEW — point queries, region extraction, coordinate conversion
├── MapQuery.fs          # NEW — query implementations
├── MapCache.fsi         # NEW — cached map loading, dynamic layer refresh
├── MapCache.fs          # NEW — ConcurrentDictionary + Lazy caching
├── FSBar.Client.fsproj  # MODIFY — add new .fsi/.fs pairs to compile order
├── ...                  # existing files unchanged

tests/FSBar.LiveTests/
├── MapGridTests.fs      # NEW — live integration tests for map data loading
├── MapQueryTests.fs     # NEW — live integration tests for queries
├── ...                  # existing test files unchanged

scripts/
├── prelude.fsx          # MODIFY — expose MapGrid, MapQuery, MapCache
├── examples/
│   └── 05-map-layers.fsx  # NEW — example script for map data exploration
```

**Structure Decision**: New modules are added within the existing `FSBar.Client` project. No new projects needed — this is a feature extension of the existing library. Three new module pairs (MapGrid, MapQuery, MapCache) keep concerns separated per the existing pattern.
