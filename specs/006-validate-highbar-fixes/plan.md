# Implementation Plan: Validate HighBar Fixes

**Branch**: `006-validate-highbar-fixes` | **Date**: 2026-04-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-validate-highbar-fixes/spec.md`

## Summary

Fix the map grid dimension mismatch (11/12 map tests failing) by switching to the corners heightmap callback (ID 59) and correcting slope map dimensions to half-resolution. Validate that all three HighBar V2 fixes (typed exceptions, configurable timeouts, resilient test execution) work correctly end-to-end.

## Technical Context

**Language/Version**: F# / .NET 10.0  
**Primary Dependencies**: FsGrpc 1.0.6 (protobuf), BarData (unit definitions), xUnit 2.9.x  
**Storage**: Filesystem (socket files, session dirs)  
**Testing**: xUnit 2.9.x + live engine integration tests  
**Target Platform**: Linux (spring-headless engine)  
**Project Type**: Library (FSBar.Client) + integration test suite  
**Performance Goals**: Map grid loads in < 5s on standard hardware  
**Constraints**: Requires HighBar V2 proxy (commit 026+) and spring-headless binary  
**Scale/Scope**: 3 source files modified, 2 test files updated, 1 proto file extended

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| I. Spec-First Delivery | PASS | Spec and plan exist; changes modify public API (proto + `.fsi`) — Tier 1 |
| II. Compiler-Enforced Contracts | PASS | `Callbacks.fsi` will be updated with new `getCornersHeightMap` signature; `MapGrid.fsi` and `MapQuery.fsi` signatures unchanged |
| III. Test Evidence | PASS | 11 currently-failing integration tests become the test evidence; no new tests needed — existing tests validate the fix |
| IV. Observability | PASS | Dimension mismatch errors already emit structured diagnostics via `failwith` messages |
| V. Scripting Accessibility | PASS | `scripts/examples/05-map-layers.fsx` may need update if it uses heightmap loading; check during implementation |

**Post-Phase 1 Re-check**: All gates pass. No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/006-validate-highbar-fixes/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: dimension analysis + HighBar commit research
├── data-model.md        # Phase 1: entity dimension changes
├── quickstart.md        # Phase 1: verification guide
└── contracts/
    ├── callbacks-fsi-delta.md   # New getCornersHeightMap signature
    ├── mapquery-fsi-delta.md    # slopeAtElmo behavioral change
    └── proto-delta.md           # New CALLBACK_MAP_GET_CORNERS_HEIGHT_MAP = 59
```

### Source Code (repository root)

```text
src/
├── FSBar.Client/
│   ├── Callbacks.fs       # Add getCornersHeightMap (callback 59)
│   ├── Callbacks.fsi      # Add getCornersHeightMap signature
│   ├── MapGrid.fs         # Use corners heightmap; fix slope dimensions
│   ├── MapGrid.fsi        # No signature changes
│   ├── MapQuery.fs        # Fix slopeAtElmo coordinate mapping
│   └── MapQuery.fsi       # No signature changes
├── FSBar.Proto/           # Regenerated from proto
└── FSBar.Client.Tests/    # Unit tests (unaffected)

proto/
└── highbar/
    └── callbacks.proto    # Add CALLBACK_MAP_GET_CORNERS_HEIGHT_MAP = 59

tests/
└── FSBar.LiveTests/
    ├── MapGridTests.fs    # Slope dimension assertions
    └── MapQueryTests.fs   # Slope query test expectations
```

**Structure Decision**: Existing single-project structure. No new projects or directories needed.

## Implementation Phases

### Phase 1: Proto + Callback (FR-003, FR-001)

**Goal**: Add corners heightmap callback to proto and F# client.

1. **Proto update**: Add `CALLBACK_MAP_GET_CORNERS_HEIGHT_MAP = 59` to `proto/highbar/callbacks.proto` after line 104 (after `CALLBACK_MAP_GET_METAL_SPOTS = 58`)
2. **Regenerate bindings**: `dotnet build src/FSBar.Proto/`
3. **Callbacks.fs**: Add `getCornersHeightMap` function using `CallbackId.CallbackMapGetCornersHeightMap`
4. **Callbacks.fsi**: Add `val getCornersHeightMap: stream: NetworkStream -> float32 list`

**Verification**: `dotnet build src/FSBar.Client/` compiles cleanly.

### Phase 2: MapGrid Dimension Fix (FR-001, FR-002, FR-007)

**Goal**: Use corners heightmap and fix slope map dimensions.

1. **MapGrid.fs line 70**: Change `Callbacks.getHeightMap` → `Callbacks.getCornersHeightMap`
2. **MapGrid.fs lines 73-75**: Change slope map dimensions from `hmW hmH` to `w/2` and `h/2`:
   ```
   let slopeW = w / 2
   let slopeH = h / 2
   Callbacks.getSlopeMap stream |> toFloat32Array2D slopeW slopeH "SlopeMap"
   ```
3. HeightMap dimensions (`hmW`, `hmH` = `w+1`, `h+1`) remain correct — now matched by corners heightmap data.

**Verification**: `dotnet build` succeeds; `dotnet test tests/FSBar.LiveTests/ --filter "Category=MapGrid"` — heightmap and slope tests pass.

### Phase 3: MapQuery Slope Fix (FR-001, FR-002)

**Goal**: Fix `slopeAtElmo` to use half-resolution grid coordinates.

1. **MapQuery.fs `slopeAtElmo`**: Change bounds check to use slope map dimensions and grid conversion to `x / 16, z / 16`
2. **MapGrid.fs `terrainAt`**: The slope access `grid.SlopeMap.[x, z]` currently uses heightmap grid indices. With slope at half-resolution, need to convert: `grid.SlopeMap.[x / 2, z / 2]` (since `x, z` are already in heightmap-grid space, and slope grid is half that).
3. **MapGrid.fs `passability`**: Same issue — slope access needs `x / 2, z / 2` with bounds clamping.

**Verification**: `dotnet test tests/FSBar.LiveTests/ --filter "Category=MapQuery"` — slope and terrain queries pass.

### Phase 4: Test Updates + Validation (FR-004, FR-006, FR-008)

**Goal**: Update test assertions and run full validation.

1. **MapGridTests.fs**: Slope-related assertions may need dimension updates if any tests check slope map dimensions directly.
2. **MapQueryTests.fs**: Slope query tests should validate against half-resolution grid.
3. **tryLoadGrid()**: Add catch for dimension mismatch exceptions (in case proxy returns unexpected sizes) — skip with diagnostic.
4. Run full test suite: `dotnet test tests/FSBar.LiveTests/`

**Verification**: All 12 map tests pass or skip with diagnostics. Zero cascade failures.

### Phase 5: `.fsi` + Script Updates

**Goal**: Ensure contracts and scripting accessibility are up to date.

1. **Callbacks.fsi**: Already updated in Phase 1.
2. **MapGrid.fsi**: No signature changes needed.
3. **MapQuery.fsi**: No signature changes needed.
4. **scripts/examples/05-map-layers.fsx**: Check if it references heightmap loading; update if needed.
5. `dotnet pack src/FSBar.Client/` — verify NuGet package builds.

**Verification**: `dotnet build` clean; scripts load without errors.

## .fsi Signature Contracts

### Callbacks.fsi — New Addition

```fsharp
/// Get the corners heightmap as a flat float32 list (row-major order).
/// Returns (mapWidth+1)*(mapHeight+1) vertex-resolution height values.
val getCornersHeightMap: stream: NetworkStream -> float32 list
```

### MapGrid.fsi — No Changes

Existing signatures remain. `loadFromEngine` return type unchanged; internal dimension changes are not visible in the signature.

### MapQuery.fsi — No Changes

`slopeAtElmo` signature unchanged. Behavioral fix only (grid coordinate mapping).

## Complexity Tracking

> **Constitution violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Surface-area baselines absent (§II) | Pre-existing project-wide gap — no baselines exist for any module | Creating baselines for all modules is out of scope for a validation feature — defer to dedicated tech-debt feature |

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Proxy doesn't support callback 59 | Low (confirmed in HighBar 026) | High — blocks P1 | `tryLoadGrid()` catches and skips |
| Slope map dimensions wrong | Low (confirmed via engine docs) | Medium — slope queries incorrect | Integration tests validate |
| `terrainAt`/`passability` slope index out of bounds | Medium | High — runtime crash | Bounds clamping in slope access |
| Other map callbacks affected | Low | Low | LOS/radar/resource remain at `w*h` — unchanged |
