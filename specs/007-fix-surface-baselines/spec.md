# Feature Specification: Fix Missing Baseline Surface FSI Coverage

**Feature Branch**: `007-fix-surface-baselines`  
**Created**: 2026-04-06  
**Status**: Draft  
**Input**: User description: "fix the missing baseline surface fsi coverage"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Surface-Area Baseline Validation on Build (Priority: P1)

A developer makes changes to FSBar.Client and runs the test suite. The system automatically compares the current public API surface of each module (as declared in `.fsi` signature files) against stored baseline snapshots. If any public type, member, or function signature has changed, the test fails with a clear diff showing exactly what changed, forcing the developer to explicitly review and update the baseline before merging.

**Why this priority**: This is the core purpose of the feature — catching accidental or undocumented public API changes. Without this, breaking changes can slip through unnoticed. This directly fulfills Constitution Section II ("Compiler-Enforced Structural Contracts").

**Independent Test**: Can be fully tested by modifying a single `.fsi` file (e.g., adding a new public function to `Commands.fsi`) and verifying the baseline test fails with a meaningful diff. Delivers immediate value as a regression safety net.

**Acceptance Scenarios**:

1. **Given** all 12 FSBar.Client modules have baseline files matching their current `.fsi` signatures, **When** a developer runs the test suite without any API changes, **Then** all surface-area baseline tests pass.
2. **Given** a baseline file exists for `Commands.fsi`, **When** a developer adds a new public function to `Commands.fsi`, **Then** the baseline test for Commands fails and reports the specific addition.
3. **Given** a baseline file exists for `Events.fsi`, **When** a developer removes a variant from the `GameEvent` DU in `Events.fsi`, **Then** the baseline test for Events fails and reports the specific removal.

---

### User Story 2 - Baseline Update Workflow (Priority: P2)

A developer intentionally changes a public API (e.g., adding a new command function). After reviewing the test failure diff and confirming the change is intentional, the developer runs a straightforward process to regenerate the baseline file for the affected module. On the next test run, the updated baseline passes.

**Why this priority**: Without a clear update workflow, developers would have no way to move forward after an intentional API change. This is essential for day-to-day usability but secondary to detection itself.

**Independent Test**: Can be tested by intentionally changing a `.fsi` file, observing the test failure, regenerating the baseline, and confirming tests pass again. Delivers value as a complete developer workflow.

**Acceptance Scenarios**:

1. **Given** a baseline test is failing due to an intentional API addition, **When** the developer regenerates the baseline for that module, **Then** the updated baseline file reflects the new API surface.
2. **Given** a baseline has been regenerated after an intentional change, **When** the developer re-runs the test suite, **Then** all surface-area baseline tests pass.

---

### User Story 3 - Initial Baseline Generation for All Modules (Priority: P1)

A developer sets up the baseline system for the first time across all 12 existing FSBar.Client public modules. Each module's current `.fsi` signature is captured as the canonical baseline snapshot. After generation, all baseline tests pass, establishing the starting point for future change detection.

**Why this priority**: Equal to P1 because without initial baselines for all modules, the detection system (Story 1) has nothing to compare against. These are co-dependent.

**Independent Test**: Can be tested by running the baseline generation process and verifying that 12 baseline files are created (one per public module) and all corresponding tests pass.

**Acceptance Scenarios**:

1. **Given** no baseline files exist, **When** the developer runs the initial baseline generation, **Then** a baseline file is created for each of the 12 FSBar.Client public modules: BarClient, Callbacks, Commands, Connection, EngineConfig, EngineLauncher, Events, MapCache, MapGrid, MapQuery, Protocol, ScriptGenerator.
2. **Given** all 12 baseline files have been generated, **When** the developer runs the full test suite, **Then** all 12 surface-area baseline tests pass.

---

### Edge Cases

- What happens when a new public module is added to FSBar.Client without a corresponding baseline? The test suite should detect the missing baseline and fail with guidance to generate one.
- What happens when a `.fsi` file contains only internal/private members? The baseline should still be generated (possibly empty) to track that the module has no public surface.
- What happens when the `.fsi` file has syntax errors or cannot be parsed? The baseline test should fail with a clear error message rather than silently passing.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST maintain a serialized baseline snapshot file for each of the 12 public FSBar.Client modules (BarClient, Callbacks, Commands, Connection, EngineConfig, EngineLauncher, Events, MapCache, MapGrid, MapQuery, Protocol, ScriptGenerator).
- **FR-002**: System MUST compare the current public API surface of each module against its stored baseline during test execution.
- **FR-003**: System MUST fail the test with a human-readable diff when the current API surface diverges from the stored baseline.
- **FR-004**: System MUST provide a mechanism to regenerate baseline files after intentional API changes.
- **FR-005**: System MUST detect when a public module exists without a corresponding baseline file and report this as a test failure.
- **FR-006**: Baseline files MUST capture all public types, discriminated union variants, record fields, class members, and function signatures declared in `.fsi` files.
- **FR-007**: Baseline comparison MUST treat declaration order as significant, since F# `.fsi` declaration order affects compilation semantics (types must be declared before use). Reordering declarations is a structural change that should be detected.

### Key Entities

- **Baseline Snapshot**: A serialized representation of a module's public API surface at a point in time, stored as a file alongside the test project.
- **Public API Surface**: The complete set of public types, members, functions, and their signatures as declared in a module's `.fsi` signature file.
- **Surface Diff**: A human-readable comparison showing additions, removals, and modifications between the current API surface and the stored baseline.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 12 FSBar.Client public modules have baseline snapshot files that pass validation against their current `.fsi` signatures.
- **SC-002**: Any undocumented change to a public API surface causes an immediate, clear test failure within the standard test run.
- **SC-003**: A developer can update a baseline after an intentional API change in under 1 minute.
- **SC-004**: The baseline test output clearly identifies which module changed and what specifically was added, removed, or modified.
- **SC-005**: Constitution Section II compliance is fully satisfied — no remaining gaps in surface-area baseline coverage for FSBar.Client.

## Assumptions

- The 12 existing `.fsi` signature files in FSBar.Client accurately represent the intended public API surface and can serve as the source of truth for initial baseline generation.
- The existing test infrastructure (xUnit in `tests/FSBar.LiveTests/`) is the appropriate location for baseline tests, or a new test project can be created alongside it.
- Baseline files are checked into version control so that all developers share the same reference point.
- The FSBar.Proto project (generated protobuf bindings) is excluded from baseline coverage since its surface is auto-generated and governed by `.proto` schema files rather than hand-authored `.fsi` files.
