# Tasks: Automatic Engine Version Detection and Update

**Input**: Design documents from `/specs/013-auto-engine-version/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Register the new EngineDiscovery module in the project and create file stubs

- [x] T001 Add EngineDiscovery.fsi and EngineDiscovery.fs entries to src/FSBar.Client/FSBar.Client.fsproj immediately before EngineConfig.fsi (EngineConfig depends on EngineDiscovery)
- [x] T002 Add EngineDiscoveryTests.fs entry to src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj after EngineConfigTests.fs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core types and discovery infrastructure that all user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T003 Define DiscoveredEngine, DiscoveredGame, ResolutionSource, and EngineResolution record types in src/FSBar.Client/EngineDiscovery.fsi — public signature with discoverEngines, discoverGameVersion, resolveEngine, and defaultDataDir function signatures
- [x] T004 Implement defaultDataDir in src/FSBar.Client/EngineDiscovery.fs — resolve standard BAR data directory at ~/.local/state/Beyond All Reason, validate maps/ and packages/ subdirectories exist
- [x] T005 Implement discoverEngines in src/FSBar.Client/EngineDiscovery.fs — scan <datadir>/engine/recoil_*/ directories, check for spring-headless and spring binaries, return DiscoveredEngine list sorted by version string descending
- [x] T006 Implement discoverGameVersion in src/FSBar.Client/EngineDiscovery.fs — decompress <datadir>/rapid/repos-cdn.beyondallreason.dev/byar/versions.gz using System.IO.Compression.GZipStream, find byar:test tag line, parse game name and hash into DiscoveredGame record
- [x] T007 Implement validateEngine in src/FSBar.Client/EngineDiscovery.fs — verify binary path exists as regular file and is executable; fail with actionable error identifying the corrupted version and what is missing

**Checkpoint**: Core discovery functions ready — resolution chain can now be built

---

## Phase 3: User Story 1 — Automatic Engine Version Detection (Priority: P1) MVP

**Goal**: System auto-detects latest installed engine and game version without manual config changes

**Independent Test**: Run test suite after engine update without modifying engine-version.json — system uses latest version automatically

### Implementation for User Story 1

- [x] T008 [US1] Implement resolveEngine in src/FSBar.Client/EngineDiscovery.fs — priority chain: check HIGHBAR_TEST_ENGINE env var → check engine-version.json path (if provided) → auto-detect via discoverEngines taking latest → error with searched locations. Log resolved version and source via printfn
- [x] T009 [US1] Update EngineConfig.defaultConfig in src/FSBar.Client/EngineConfig.fs — call EngineDiscovery.resolveEngine to populate EngineBin (headless path), AppImagePath (graphical path), and GameType (game version string). Wrap the call in try/with: on success use resolved values; on failure log the error via eprintfn and fall back to previous hardcoded defaults ("spring-headless", etc.) so that config construction never throws. The hard error from FR-010/FR-004 surfaces at engine launch time (EngineLauncher validates the binary), not at config creation time
- [x] T010 [US1] Update EngineConfig.fsi in src/FSBar.Client/EngineConfig.fsi — if defaultConfig signature changes (it shouldn't, since it returns the same record type, but verify)
- [x] T011 [US1] Update check-prerequisites.sh in tests/check-prerequisites.sh — make engine-version.json optional: if config file is absent, auto-detect engine by scanning ~/.local/state/Beyond All Reason/engine/recoil_*/ for latest version, auto-detect game version by parsing rapid versions.gz for byar:test tag. Preserve all existing checks (game archive, map files, data directory)
- [x] T012 [US1] Create EngineDiscoveryTests in src/FSBar.Client.Tests/EngineDiscoveryTests.fs — test discoverEngines finds installed engine(s), test discoverGameVersion parses rapid versions, test resolveEngine auto-detect path returns valid result, test version sorting returns newest first
- [x] T013 [US1] Update EngineConfigTests in src/FSBar.Client.Tests/EngineConfigTests.fs — change Assert.Equal("spring-headless", ...) to Assert.False(String.IsNullOrEmpty(config.EngineBin)), change Assert.Equal("Beyond All Reason test-29876-f8bb848", ...) to Assert.StartsWith("Beyond All Reason", config.GameType), update any hardcoded AppImagePath assertions
- [x] T014 [US1] Update ScriptGeneratorTests in src/FSBar.Client.Tests/ScriptGeneratorTests.fs — change Assert.Contains("Beyond All Reason test-29876-f8bb848", script) to Assert.Contains(config.GameType, script) using the config instance, update generate_with_custom_config test if needed
- [x] T015 [US1] Create EngineDiscovery.baseline in src/FSBar.Client.Tests/Baselines/EngineDiscovery.baseline — generate surface-area baseline from EngineDiscovery.fsi public API, add to SurfaceAreaTests
- [x] T016 [US1] Build and run all tests — dotnet build src/FSBar.Client/ && dotnet test src/FSBar.Client.Tests/ — verify no regressions, all new tests pass

**Checkpoint**: Auto-detection works end-to-end. Tests and check-prerequisites.sh work without engine-version.json present.

---

## Phase 4: User Story 2 — Version Override for Reproducibility (Priority: P2)

**Goal**: Developers can pin a specific engine version via env var or config file, overriding auto-detection

**Independent Test**: Set HIGHBAR_TEST_ENGINE or engine-version.json to a specific version, verify system uses that exact version even when a newer one is available

### Implementation for User Story 2

- [x] T017 [US2] Add resolveEngine tests for env var override path in src/FSBar.Client.Tests/EngineDiscoveryTests.fs — test that HIGHBAR_TEST_ENGINE env var takes precedence over auto-detect, test that invalid env var path produces actionable error
- [x] T018 [US2] Add resolveEngine tests for config file override path in src/FSBar.Client.Tests/EngineDiscoveryTests.fs — test that engine-version.json pin takes precedence over auto-detect, test that pinned version not found produces error identifying the missing version
- [x] T019 [US2] Add resolveEngine test for missing engine in src/FSBar.Client.Tests/EngineDiscoveryTests.fs — test that when no engine is found at any source, error message lists at least 2 searched locations
- [x] T020 [US2] Verify check-prerequisites.sh respects HIGHBAR_TEST_ENGINE override in tests/check-prerequisites.sh — confirm existing env var override behavior is preserved with new auto-detect fallback

**Checkpoint**: Override mechanisms verified — env var and config file both take precedence over auto-detection

---

## Phase 5: User Story 3 — Version Change Notification (Priority: P3)

**Goal**: System logs resolved engine version at startup so developers can correlate behavior with engine updates

**Independent Test**: Switch installed engine version and observe startup output for version information

### Implementation for User Story 3

- [x] T021 [US3] Verify resolveEngine logging output in src/FSBar.Client/EngineDiscovery.fs — ensure printfn outputs include resolved engine version string, binary path, game version, and resolution source (env var / config / auto-detected)
- [x] T022 [US3] Add test verifying version info is logged in src/FSBar.Client.Tests/EngineDiscoveryTests.fs — capture console output during resolveEngine call, assert it contains version string and source indicator

**Checkpoint**: Version visibility confirmed — developers see which engine version is active on every run

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, baselines, and cleanup across all stories

- [x] T023 [P] Create tests/ENGINE-VERSION.md documenting that engine-version.json is optional and only needed for pinning a specific version; reference it from the Prerequisites section in README.md
- [x] T024 [P] Update CLAUDE.md engine paths section — note auto-detection capability, update hardcoded version references
- [x] T024b [P] Update scripts/prelude.fsx to expose EngineDiscovery.resolveEngine and EngineDiscovery.discoverEngines as interactive helpers — verify prelude loads with a single #load directive per Constitution V
- [x] T025 Repack FSBar.Client — dotnet pack src/FSBar.Client/ -o ~/.local/share/nuget-local/ to publish updated NuGet package
- [x] T026 Run full test suite — dotnet test src/FSBar.Client.Tests/ and ./tests/check-prerequisites.sh to verify everything passes end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational — core auto-detection
- **User Story 2 (Phase 4)**: Depends on Phase 3 (resolveEngine must exist before testing overrides)
- **User Story 3 (Phase 5)**: Depends on Phase 3 (logging built into resolveEngine)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Foundational (Phase 2) only — no cross-story dependencies
- **User Story 2 (P2)**: Depends on US1 (override testing requires the resolution chain to exist)
- **User Story 3 (P3)**: Depends on US1 (logging is part of resolveEngine implementation)

### Within Each User Story

- Types/signatures before implementation
- Implementation before tests
- Tests before baseline/surface-area

### Parallel Opportunities

- T003–T007 in Phase 2: T004, T005, T006, T007 can be parallelized once T003 defines the types
- T012, T013, T014, T015 in Phase 3: test updates are parallel (different files)
- T017, T018, T019 in Phase 4: test additions are parallel (same file but independent test functions)
- T023, T024 in Phase 6: doc updates are parallel (different files)

---

## Parallel Example: User Story 1

```bash
# After T008-T011 implementation, launch test updates in parallel:
Task: "T012 Create EngineDiscoveryTests in src/FSBar.Client.Tests/EngineDiscoveryTests.fs"
Task: "T013 Update EngineConfigTests in src/FSBar.Client.Tests/EngineConfigTests.fs"
Task: "T014 Update ScriptGeneratorTests in src/FSBar.Client.Tests/ScriptGeneratorTests.fs"
Task: "T015 Create EngineDiscovery.baseline in src/FSBar.Client.Tests/Baselines/EngineDiscovery.baseline"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (register files in fsproj)
2. Complete Phase 2: Foundational (types + discovery functions)
3. Complete Phase 3: User Story 1 (wire up + tests)
4. **STOP and VALIDATE**: `dotnet test` passes, `./tests/check-prerequisites.sh` works without engine-version.json
5. Ship MVP — auto-detection works

### Incremental Delivery

1. Setup + Foundational → Discovery infrastructure ready
2. User Story 1 → Auto-detection works end-to-end (MVP!)
3. User Story 2 → Override mechanisms verified
4. User Story 3 → Version logging confirmed
5. Polish → Docs updated, package repacked

---

## Notes

- EngineDiscovery.fsi/.fs must appear BEFORE EngineConfig.fsi/.fs in the fsproj compile order (F# requires forward declaration)
- The EngineConfig record type does NOT change — only the defaults populated by defaultConfig()
- Tests run against the live environment per CLAUDE.md — no mocks for filesystem scanning
- engine-version.json becomes optional but remains functional for version pinning
