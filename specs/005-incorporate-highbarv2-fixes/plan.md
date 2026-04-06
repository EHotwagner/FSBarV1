# Implementation Plan: Incorporate HighBarV2 Client and Test Fixes

**Branch**: `005-incorporate-highbarv2-fixes` | **Date**: 2026-04-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-incorporate-highbarv2-fixes/spec.md`

## Summary

Port three targeted fixes from HighBarV2 to FSBarV1: (1) a typed `EngineDisconnectedException` replacing generic `failwith` for socket disconnections, (2) configurable read timeouts on `NetworkStream` via config/env var/default chain, and (3) resilient error handling in map test helpers to prevent cascade failures from proxy disconnects.

## Technical Context

**Language/Version**: F# / .NET 10.0  
**Primary Dependencies**: FsGrpc 1.0.6 (protobuf), BarData (unit definitions), xUnit 2.9.x  
**Storage**: Filesystem (socket files, session dirs)  
**Testing**: xUnit 2.9.x with live engine integration tests  
**Target Platform**: Linux (Unix domain sockets)  
**Project Type**: Library (FSBar.Client) + integration test suite  
**Performance Goals**: Map tests complete within 30s each; no indefinite hangs  
**Constraints**: Must maintain backward compatibility with existing BarClient API consumers  
**Scale/Scope**: 6 files modified (4 client, 2 test)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
| ---- | ------ | ----- |
| I. Spec-First Delivery | PASS | Spec and plan created before implementation |
| II. Compiler-Enforced Contracts | PASS | `.fsi` updates planned for Connection.fsi and EngineConfig.fsi |
| III. Test Evidence | PASS | Existing map tests serve as verification; currently 11/12 fail, should pass/skip after fix |
| IV. Observability / Safe Failure | PASS | Core purpose: replace silent failures with typed exceptions |
| V. Scripting Accessibility | N/A | No new public API surface for scripting |

**Post-design re-check**: All gates still pass. No new modules created, only existing signatures updated.

## Project Structure

### Documentation (this feature)

```text
specs/005-incorporate-highbarv2-fixes/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/FSBar.Client/
├── EngineConfig.fsi     # MODIFY: add ReadTimeoutMs field
├── EngineConfig.fs      # MODIFY: add ReadTimeoutMs field + timeout resolution
├── Connection.fsi       # MODIFY: add EngineDisconnectedException type
├── Connection.fs        # MODIFY: add exception type, wrap readExact, apply timeout
├── ...                  # (remaining files unchanged)

tests/FSBar.LiveTests/
├── MapGridTests.fs      # MODIFY: expand tryLoadGrid() error handling
├── MapQueryTests.fs     # MODIFY: expand tryLoadGrid() error handling
├── ...                  # (remaining files unchanged)
```

**Structure Decision**: No new files or projects. All changes are modifications to existing files within the established structure.

## Implementation Phases

### Phase 1: EngineDisconnectedException + Read Timeout (P1 + P2)

**Files**: `Connection.fsi`, `Connection.fs`, `EngineConfig.fsi`, `EngineConfig.fs`

1. Add `EngineDisconnectedException` type to `Connection.fs` (before module declaration):
   - Inherits `System.IO.IOException`
   - Constructor: `(message: string, ?lastFrameNumber: uint32, ?innerException: exn)`
   - Member: `LastFrameNumber: uint32 option`

2. Update `Connection.fsi` to declare the exception type publicly.

3. Add `ReadTimeoutMs: int option` to `EngineConfig` record and `.fsi`.
   - Default: `None` in `defaultConfig()`
   - Add resolution helper: `resolveReadTimeout: EngineConfig -> int`

4. Modify `Connection.readExact`:
   - Wrap `stream.Read` call in try/catch for `IOException`
   - On `IOException`: raise `EngineDisconnectedException` wrapping the original
   - On zero-byte read: raise `EngineDisconnectedException` instead of `failwith`

5. Modify `Connection.acceptConnection`:
   - Accept optional `readTimeoutMs: int` parameter
   - Set `stream.ReadTimeout <- readTimeoutMs` before returning
   - Update `.fsi` signature

6. Update `BarClient.Start()` to pass resolved timeout to `acceptConnection`.

### Phase 2: Map Test Error Recovery (P3)

**Files**: `MapGridTests.fs`, `MapQueryTests.fs`

1. In both files, update `tryLoadGrid()`:
   ```
   Current:  with ex when ex.Message.Contains("empty array") -> ...
   Updated:  with
             | :? EngineDisconnectedException as ex -> ... log + None
             | :? IOException as ex -> ... log + None
             | ex when ex.Message.Contains("empty array") -> ... None
   ```

2. Verify all 12 map tests either pass or skip cleanly.

### Phase 3: Verification

1. `dotnet build src/FSBar.Client/` — compiler checks .fsi conformance
2. `dotnet test tests/FSBar.LiveTests/ --filter "Category=MapGrid|Category=MapQuery"` — map tests pass/skip
3. `dotnet test tests/FSBar.LiveTests/` — full suite still works

## Complexity Tracking

No constitution violations. No complexity justifications needed.
