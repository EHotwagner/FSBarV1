# Implementation Plan: Fix Missing Baseline Surface FSI Coverage

**Branch**: `007-fix-surface-baselines` | **Date**: 2026-04-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-fix-surface-baselines/spec.md`

## Summary

Implement surface-area baseline tests for all 12 public FSBar.Client modules to fulfill Constitution Section II. Each `.fsi` signature file gets a stored `.baseline` snapshot; an xUnit test verifies content matches at build time. Divergence produces a clear diff. An environment variable (`UPDATE_BASELINES=true`) allows regeneration after intentional API changes.

## Technical Context

**Language/Version**: F# / .NET 10.0  
**Primary Dependencies**: xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x (existing in FSBar.Client.Tests)  
**Storage**: Filesystem — `.baseline` text files committed to git  
**Testing**: xUnit (existing `FSBar.Client.Tests` project, `dotnet test`)  
**Target Platform**: .NET 10.0 (cross-platform)  
**Project Type**: Library test infrastructure  
**Performance Goals**: N/A (test-time only, sub-second execution)  
**Constraints**: No new dependencies; no live server required; pure file I/O  
**Scale/Scope**: 12 `.fsi` files → 12 `.baseline` files + 1 test file

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec-First Delivery | PASS | Spec 007 exists with acceptance criteria and scope boundaries |
| II. Compiler-Enforced Structural Contracts | PASS | This feature IS the implementation of §II baseline requirements |
| III. Test Evidence Is Mandatory | PASS | The feature itself produces tests; test evidence = baseline tests passing |
| IV. Observability and Safe Failure | PASS | Test failures produce human-readable diffs with actionable context |
| V. Scripting Accessibility | N/A | Test infrastructure, not a public API module |
| .fsi for every public module | N/A | No new public modules created |
| Surface-area baselines | PASS | This feature creates all missing baselines |
| Dependency minimization | PASS | Zero new dependencies — uses only System.IO and xUnit (already present) |
| Packable libraries | N/A | Test project, not a library |

**Post-Phase 1 Re-check**: All gates remain PASS. Design uses existing project, no new dependencies, no new public API surface.

## Project Structure

### Documentation (this feature)

```text
specs/007-fix-surface-baselines/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research decisions
├── data-model.md        # Baseline entity model
├── quickstart.md        # Developer workflow guide
├── contracts/
│   └── baseline-format.md  # Baseline file format contract
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/FSBar.Client.Tests/
├── FSBar.Client.Tests.fsproj   # Updated: add SurfaceAreaTests.fs + Baselines content
├── SurfaceAreaTests.fs          # NEW: baseline comparison tests
├── EngineConfigTests.fs         # Existing (unchanged)
├── CommandsTests.fs             # Existing (unchanged)
├── ... (other existing tests)
└── Baselines/                   # NEW: stored baseline snapshots
    ├── BarClient.baseline
    ├── Callbacks.baseline
    ├── Commands.baseline
    ├── Connection.baseline
    ├── EngineConfig.baseline
    ├── EngineLauncher.baseline
    ├── Events.baseline
    ├── MapCache.baseline
    ├── MapGrid.baseline
    ├── MapQuery.baseline
    ├── Protocol.baseline
    └── ScriptGenerator.baseline
```

**Structure Decision**: Baselines and test file go in the existing `FSBar.Client.Tests` project. No new projects. The `Baselines/` directory is co-located with the test source. Baseline files are included as `Content` items (CopyToOutputDirectory) so tests can locate them relative to the test assembly.

## Design

### Approach

The implementation is deliberately simple: baseline files are verbatim copies of `.fsi` content. The test reads both files as strings and compares them. This avoids any parsing infrastructure while capturing the complete public API surface (types, signatures, attributes, doc comments).

### Test Structure

`SurfaceAreaTests.fs` contains:

1. **Per-module baseline test** (parameterized via `[<Theory>]` + `[<InlineData>]`): For each of the 12 modules, read `src/FSBar.Client/{Module}.fsi` and `Baselines/{Module}.baseline`, compare content. If `UPDATE_BASELINES` env var is set, overwrite the baseline instead of asserting.

2. **Missing baseline detection test**: Enumerate all `.fsi` files in the FSBar.Client project directory, verify each has a corresponding `.baseline` file. Fail with the list of uncovered modules.

3. **Orphaned baseline detection test**: Enumerate all `.baseline` files, verify each has a corresponding `.fsi` file. Fail if baselines exist for removed modules.

### File Discovery

Tests resolve the FSBar.Client source directory relative to the test project's source location using `[<CallerFilePath>]` or a known relative path (`../../FSBar.Client/`). This avoids hardcoded absolute paths.

### Diff Output

On mismatch, the test failure message includes:
- Module name
- Expected content (first ~50 lines of baseline)
- Actual content (first ~50 lines of current `.fsi`)
- Line-by-line diff showing additions/removals

### Regeneration Flow

```
Developer changes .fsi file
  → runs `dotnet test` → baseline test FAILS with diff
  → reviews diff, confirms intentional
  → runs `UPDATE_BASELINES=true dotnet test` → baselines updated
  → runs `dotnet test` again → all PASS
  → `git diff` shows baseline changes for review
  → commits .fsi + .baseline changes together
```

## Complexity Tracking

No constitution violations. No complexity justifications needed.

| Item | Decision | Rationale |
|------|----------|-----------|
| Raw text comparison (not parsed) | Simplest approach | .fsi IS the surface; parsing adds complexity without value |
| Single test file, not one per module | Reduces boilerplate | `[<Theory>]` parameterization covers all 12 modules cleanly |
| Env var for regeneration | Standard .NET pattern | No custom tooling needed; discoverable via test output messages |
