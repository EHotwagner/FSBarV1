# Tasks: Fix Missing Baseline Surface FSI Coverage

**Input**: Design documents from `/specs/007-fix-surface-baselines/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Baselines directory and update the test project to include new files

- [x] T001 Create `src/FSBar.Client.Tests/Baselines/` directory for baseline snapshot storage
- [x] T002 Update `src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj` — add `<Compile Include="SurfaceAreaTests.fs" />` entry (after existing test files) and add `<Content Include="Baselines/**" CopyToOutputDirectory="PreserveNewest" />` item group for baseline files

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core test infrastructure that all user stories depend on — path resolution and module discovery

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T003 Create `src/FSBar.Client.Tests/SurfaceAreaTests.fs` with module declaration (`module FSBar.Client.Tests.SurfaceAreaTests`), open statements (`Xunit`, `System`, `System.IO`), and path resolution helper that locates the FSBar.Client source directory relative to the test file using `__SOURCE_DIRECTORY__` (e.g., `let clientSrcDir = Path.Combine(__SOURCE_DIRECTORY__, "..", "FSBar.Client") |> Path.GetFullPath`) and the Baselines directory (`let baselinesDir = Path.Combine(__SOURCE_DIRECTORY__, "Baselines") |> Path.GetFullPath`)

**Checkpoint**: Foundation ready — SurfaceAreaTests.fs compiles with path helpers, user story implementation can begin

---

## Phase 3: User Story 1 — Surface-Area Baseline Validation on Build (Priority: P1) + User Story 3 — Initial Baseline Generation (Priority: P1) MVP

**Goal**: Detect API surface changes by comparing `.fsi` files against stored baselines, and generate the initial set of baselines for all 12 modules

**Independent Test**: Modify a `.fsi` file (e.g., add a function to `Commands.fsi`), run `dotnet test src/FSBar.Client.Tests/ --filter "SurfaceArea"`, verify test fails with a diff. Revert change, verify tests pass.

### Implementation for User Stories 1 + 3

- [x] T004 [US1] Implement per-module baseline comparison test in `src/FSBar.Client.Tests/SurfaceAreaTests.fs` — add `[<Theory>]` with `[<InlineData("BarClient")>]`, `[<InlineData("Callbacks")>]`, `[<InlineData("Commands")>]`, `[<InlineData("Connection")>]`, `[<InlineData("EngineConfig")>]`, `[<InlineData("EngineLauncher")>]`, `[<InlineData("Events")>]`, `[<InlineData("MapCache")>]`, `[<InlineData("MapGrid")>]`, `[<InlineData("MapQuery")>]`, `[<InlineData("Protocol")>]`, `[<InlineData("ScriptGenerator")>]` for a test function ``baseline_matches_fsi_surface`` that reads `{clientSrcDir}/{moduleName}.fsi` and `{baselinesDir}/{moduleName}.baseline`, compares with string equality, and on mismatch produces a failure message showing the module name and a line-by-line diff (compare expected vs actual lines, prefix removals with `-` and additions with `+`)
- [x] T005 [US1] Implement missing baseline detection test in `src/FSBar.Client.Tests/SurfaceAreaTests.fs` — add `[<Fact>]` test ``all_fsi_modules_have_baselines`` that enumerates all `*.fsi` files in `clientSrcDir`, extracts module names, checks each has a corresponding `.baseline` file in `baselinesDir`, and fails listing any missing modules with a message: "Missing baselines for: {modules}. Run UPDATE_BASELINES=true dotnet test to generate."
- [x] T006 [US1] Implement orphaned baseline detection test in `src/FSBar.Client.Tests/SurfaceAreaTests.fs` — add `[<Fact>]` test ``no_orphaned_baselines_exist`` that enumerates all `*.baseline` files in `baselinesDir`, extracts module names, checks each has a corresponding `.fsi` file in `clientSrcDir`, and fails listing any orphaned baselines with a message: "Orphaned baselines found: {modules}. Remove baselines for deleted modules."
- [x] T007 [US3] Generate initial baseline files for all 12 modules — create each `src/FSBar.Client.Tests/Baselines/{ModuleName}.baseline` by copying the content of the corresponding `src/FSBar.Client/{ModuleName}.fsi` file. Files: BarClient.baseline, Callbacks.baseline, Commands.baseline, Connection.baseline, EngineConfig.baseline, EngineLauncher.baseline, Events.baseline, MapCache.baseline, MapGrid.baseline, MapQuery.baseline, Protocol.baseline, ScriptGenerator.baseline
- [x] T008 [US1] [US3] Verify all surface-area tests pass by running `dotnet test src/FSBar.Client.Tests/ --filter "SurfaceArea"` — all 12 parameterized baseline tests, the missing baseline test, and the orphaned baseline test must pass

**Checkpoint**: All 12 modules have baselines. Tests detect any `.fsi` drift, missing baselines, and orphaned baselines.

---

## Phase 4: User Story 2 — Baseline Update Workflow (Priority: P2)

**Goal**: Provide a mechanism to regenerate baselines after intentional API changes via `UPDATE_BASELINES=true`

**Independent Test**: Temporarily add a function to `Commands.fsi`, run `UPDATE_BASELINES=true dotnet test src/FSBar.Client.Tests/ --filter "SurfaceArea"`, verify `Commands.baseline` is updated. Revert `.fsi` change. Run `dotnet test` — test fails (baseline now has extra function). Run `UPDATE_BASELINES=true dotnet test` again to restore.

### Implementation for User Story 2

- [x] T009 [US2] Add `UPDATE_BASELINES` environment variable support to the ``baseline_matches_fsi_surface`` test in `src/FSBar.Client.Tests/SurfaceAreaTests.fs` — at the start of the test, check `Environment.GetEnvironmentVariable("UPDATE_BASELINES")`. If set to `"true"` (case-insensitive) and the `.fsi` content differs from the `.baseline` content (or the baseline file doesn't exist), overwrite the `.baseline` file with the current `.fsi` content and skip assertion (return early or use `Assert.True(true)` with a message "Baseline updated for {moduleName}"). If not set, assert equality as before.
- [x] T010 [US2] Add `UPDATE_BASELINES` support to the ``all_fsi_modules_have_baselines`` test — when `UPDATE_BASELINES=true`, instead of failing on missing baselines, generate them by copying `.fsi` content to new `.baseline` files, then pass with a message listing generated files
- [x] T011 [US2] Verify regeneration workflow — temporarily modify a `.fsi` file (e.g., add a comment to `src/FSBar.Client/ScriptGenerator.fsi`), run `UPDATE_BASELINES=true dotnet test src/FSBar.Client.Tests/ --filter "SurfaceArea"`, verify the corresponding `.baseline` is updated, then revert both files

**Checkpoint**: Complete regeneration workflow operational. Developers can update baselines with a single command.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Validation and cleanup

- [x] T012 [P] Run full test suite `dotnet test src/FSBar.Client.Tests/` to verify surface-area tests don't interfere with existing unit tests
- [x] T013 [P] Validate quickstart.md workflow — follow the steps in `specs/007-fix-surface-baselines/quickstart.md` end-to-end to confirm documentation accuracy

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (needs .fsproj updated and Baselines dir)
- **US1+US3 (Phase 3)**: Depends on Phase 2 (needs SurfaceAreaTests.fs with path helpers)
- **US2 (Phase 4)**: Depends on Phase 3 (extends existing tests with env var support)
- **Polish (Phase 5)**: Depends on Phase 4

### User Story Dependencies

- **US1 + US3 (P1)**: Co-dependent — comparison tests (US1) need baselines (US3) to exist; baselines need tests to validate them. Implemented together in Phase 3.
- **US2 (P2)**: Depends on US1 — extends the comparison test with regeneration capability.

### Within Phase 3

- T004, T005, T006 can be written in sequence within the same file (SurfaceAreaTests.fs)
- T007 (baseline generation) depends on T004 being written but can run before T005/T006
- T008 (verification) depends on all of T004–T007

### Parallel Opportunities

- T001 and T002 can run in parallel (different files)
- T012 and T013 can run in parallel (independent validation tasks)
- Within Phase 3, T004/T005/T006 are in the same file so must be sequential

---

## Parallel Example: Phase 5

```bash
# Launch both validation tasks together:
Task: "Run full test suite to verify no interference"
Task: "Validate quickstart.md workflow end-to-end"
```

---

## Implementation Strategy

### MVP First (Phase 3 = US1 + US3)

1. Complete Phase 1: Setup (Baselines dir, .fsproj)
2. Complete Phase 2: Foundational (path helpers in SurfaceAreaTests.fs)
3. Complete Phase 3: US1 + US3 (comparison tests + initial baselines)
4. **STOP and VALIDATE**: Run `dotnet test src/FSBar.Client.Tests/ --filter "SurfaceArea"` — all 14 tests pass (12 parameterized + 2 detection)
5. Constitution §II is now satisfied for all 12 modules

### Incremental Delivery

1. Setup + Foundational → Infrastructure ready
2. US1 + US3 → Baseline detection operational (MVP!)
3. US2 → Regeneration workflow added
4. Polish → Full validation complete

---

## Notes

- All implementation is in a single file: `src/FSBar.Client.Tests/SurfaceAreaTests.fs`
- 12 modules covered: BarClient, Callbacks, Commands, Connection, EngineConfig, EngineLauncher, Events, MapCache, MapGrid, MapQuery, Protocol, ScriptGenerator
- No new dependencies — uses System.IO (BCL) + xUnit (existing)
- Baseline files are verbatim `.fsi` copies — no parsing or normalization
- Tests resolve paths via `__SOURCE_DIRECTORY__` to avoid hardcoded absolute paths
