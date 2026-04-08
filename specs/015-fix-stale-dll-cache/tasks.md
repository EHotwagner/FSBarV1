# Tasks: Fix Stale DLL Cache Problem

**Input**: Design documents from `/specs/015-fix-stale-dll-cache/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Tests**: No test tasks included (this is build tooling; verification is via the check-deps script itself).

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No new project setup needed. Existing repos are the targets.

- [x] T001 Verify SkiaViewer repo is cloned at ~/projects/SkiaViewer and builds successfully
- [x] T002 [P] Verify HighBarV2 repo is cloned at ~/projects/HighBarV2 and BarData builds successfully

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Update upstream project files to support timestamp-based versioning

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T003 Update ~/projects/SkiaViewer/src/SkiaViewer/SkiaViewer.fsproj — replace `<Version>1.0.0</Version>` with `<VersionPrefix>1.0.0</VersionPrefix>`
- [x] T004 [P] Update ~/projects/HighBarV2/data/bar/BarData.fsproj — add `<VersionPrefix>1.0.0</VersionPrefix>` if no Version property exists, or replace existing `<Version>` with `<VersionPrefix>`

**Checkpoint**: Both upstream projects accept `--version-suffix` during `dotnet pack`

---

## Phase 3: User Story 1 - Rebuilding After Upstream Changes (Priority: P1) 🎯 MVP

**Goal**: After updating an upstream dependency, a single build command in FSBarV1 uses the new version — zero manual cache clearing.

**Independent Test**: Rebuild SkiaViewer with a code change, run `pack-dev.sh`, run `dotnet build` in FSBarV1, verify the new DLL is in build output.

### Implementation for User Story 1

- [x] T005 [US1] Create ~/projects/SkiaViewer/pack-dev.sh — script that runs `dotnet pack --version-suffix dev.$(date +%Y%m%dT%H%M%S)`, removes old nupkg from target dir, copies new nupkg to target dir (accepts target dir as $1, required — no default to avoid ambiguity between local feed locations)
- [x] T006 [P] [US1] Create ~/projects/HighBarV2/pack-dev.sh — same script adapted for BarData project path (data/bar/BarData.fsproj), target dir as $1 (required)
- [x] T007 [US1] Update /home/developer/projects/FSBarV1/src/FSBar.Viz/FSBar.Viz.fsproj — change SkiaViewer PackageReference from `Version="1.0.0"` to `Version="*-*"` to accept prerelease versions
- [x] T008 [P] [US1] Update /home/developer/projects/FSBarV1/src/FSBar.Client/FSBar.Client.fsproj — verify BarData `Version="*"` accepts prereleases; if not, change to `Version="*-*"`
- [x] T009 [US1] End-to-end validation: run pack-dev.sh for SkiaViewer targeting FSBarV1/nupkg/, run `dotnet build` in FSBarV1, confirm SkiaViewer.dll in bin/Debug/net10.0/ has the new version (use `dotnet restore --force` only for this one-time validation to confirm NuGet resolves the new version; normal workflow needs only `dotnet build`)

**Checkpoint**: Single-command dependency update workflow works for both SkiaViewer and BarData

---

## Phase 4: User Story 2 - FSI REPL Sessions Use Current DLLs (Priority: P2)

**Goal**: After building FSBarV1 with updated dependencies, FSI sessions load the current DLLs.

**Independent Test**: Update SkiaViewer, pack-dev, build FSBarV1, restart FSI, load prelude, verify the new SkiaViewer is loaded.

### Implementation for User Story 2

- [x] T010 [US2] Verify /home/developer/projects/FSBarV1/scripts/prelude.fsx loads DLLs from bin/Debug/net10.0/ paths that are populated by the build — no changes expected (prelude already uses correct paths)
- [x] T011 [US2] End-to-end validation: pack-dev SkiaViewer, build FSBarV1, restart FSI (via restart_fsi MCP tool), load prelude, confirm SkiaViewer version matches

**Checkpoint**: FSI sessions always use current DLLs after a build + restart

---

## Phase 5: User Story 3 - Clear Feedback When Dependencies Are Out of Date (Priority: P3)

**Goal**: A verification command reports which dependency DLLs are fresh or stale.

**Independent Test**: Run check-deps.sh with a fresh build (all green), then manually replace a DLL with an older copy, run again (shows stale).

### Implementation for User Story 3

- [x] T012 [US3] Create /home/developer/projects/FSBarV1/scripts/check-deps.sh — for each .nupkg in nupkg/, extract the DLL, compute SHA256 hash, compare against corresponding DLL in src/*/bin/Debug/net10.0/ and tests/*/bin/Debug/net10.0/, report fresh/stale per package
- [x] T013 [US3] End-to-end validation: build FSBarV1, run check-deps.sh (all fresh), manually overwrite one DLL, run check-deps.sh (shows stale)

**Checkpoint**: Developers can verify dependency freshness in under 5 seconds

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and cleanup

- [x] T014 [P] Update /home/developer/projects/FSBarV1/CLAUDE.md — add section documenting the pack-dev workflow for upstream dependencies, remove any stale-cache workaround instructions
- [x] T015 [P] Update /home/developer/projects/FSBarV1/specs/015-fix-stale-dll-cache/quickstart.md — verify all commands work as documented
- [ ] T016 Commit and push changes to all three repos (SkiaViewer, HighBarV2, FSBarV1)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — verify repos exist
- **Foundational (Phase 2)**: Depends on Setup — update .fsproj VersionPrefix
- **User Story 1 (Phase 3)**: Depends on Foundational — pack scripts + version ranges
- **User Story 2 (Phase 4)**: Depends on US1 — FSI validation requires working pack-dev flow
- **User Story 3 (Phase 5)**: Can start after Foundational — independent of US1/US2
- **Polish (Phase 6)**: Depends on all user stories complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Foundational (Phase 2) only — no other story dependencies
- **User Story 2 (P2)**: Depends on US1 (needs working pack-dev flow to validate FSI)
- **User Story 3 (P3)**: Depends on Foundational only — can run in parallel with US1

### Parallel Opportunities

- T001 and T002 can run in parallel (different repos)
- T003 and T004 can run in parallel (different repos)
- T005 and T006 can run in parallel (different repos)
- T007 and T008 can run in parallel (different files)
- T012 can start as soon as Foundational completes (parallel with US1)
- T014 and T015 can run in parallel (different files)

---

## Parallel Example: User Story 1

```bash
# Launch pack-dev scripts for both upstream projects together:
Task: "Create ~/projects/SkiaViewer/pack-dev.sh"
Task: "Create ~/projects/HighBarV2/pack-dev.sh"

# Update both .fsproj version ranges together:
Task: "Update FSBar.Viz.fsproj SkiaViewer version range"
Task: "Update FSBar.Client.fsproj BarData version range"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (verify repos)
2. Complete Phase 2: Foundational (VersionPrefix in upstream .fsproj)
3. Complete Phase 3: User Story 1 (pack-dev scripts + version ranges)
4. **STOP and VALIDATE**: Pack SkiaViewer, build FSBarV1, confirm new DLL — no cache clearing
5. If working, proceed to US2/US3

### Incremental Delivery

1. Setup + Foundational → Upstream projects ready
2. User Story 1 → Single-command updates work (MVP!)
3. User Story 2 → FSI freshness validated
4. User Story 3 → Verification script available
5. Polish → Documentation updated

---

## Notes

- [P] tasks = different files/repos, no dependencies
- [Story] label maps task to specific user story for traceability
- pack-dev.sh scripts are Bash (build tooling, not F# application code)
- The `*-*` version wildcard in PackageReferences accepts the latest version including prereleases
- Old nupkg files in nupkg/ should be removed by pack-dev.sh to avoid version resolution ambiguity
