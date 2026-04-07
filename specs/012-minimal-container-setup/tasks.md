# Tasks: Minimal Container Setup for BAR Development

**Input**: Design documents from `/specs/012-minimal-container-setup/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, quickstart.md

**Tests**: No automated tests requested. Verification is manual (build + run).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the container/ directory and base file structure

- [x] T001 Create container/ directory at repository root
- [x] T002 [P] Create container/Containerfile with Arch Linux base image and system setup (mirror config, pacman update) — stripped package list per FR-002/FR-009
- [x] T003 [P] Create container/entrypoint.sh with XDG_RUNTIME_DIR creation and exec passthrough (per research.md R4)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core Containerfile sections that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Add user setup section to container/Containerfile (developer user, sudo, PATH, DOTNET_ROOT)
- [x] T005 Add AUR freeimage build section to container/Containerfile (per FR-009)
- [x] T006 Add dev tools section to container/Containerfile (uv installer only — do NOT include Claude Code CLI; document installation steps in container/README.md instead per FR-012)
- [x] T007 Add .NET global tools section to container/Containerfile (fsautocomplete, fantomas only — per FR-013)
- [x] T008 Add Python tools section to container/Containerfile (specify-cli via uv — per FR-014)
- [x] T009 Add Constitutions/upcons build-and-install section to container/Containerfile (per FR-014)
- [x] T010 Add git identity and auth section to container/Containerfile (GH_TOKEN build arg, parameterized user)
- [x] T011 Add workspace directory creation to container/Containerfile (projects/, tools/)
- [x] T011b Create empty container/README.md with feature title header

**Checkpoint**: Foundation ready — Containerfile has base system, user, tools. User story implementation can now begin.

---

## Phase 3: User Story 1 - Build and Run Minimal Container (Priority: P1) 🎯 MVP

**Goal**: Produce a buildable Containerfile that creates a working container with .NET 10.0, essential tools, FSI MCP server, and three project repos. Container starts and works for headless/CLI workflows.

**Independent Test**: Build the image with `podman build`, run with BAR mount, verify `dotnet --version` works and FSI MCP server starts on port 5020.

### Implementation for User Story 1

- [x] T012 [US1] Add FSI MCP server clone-and-build section to container/Containerfile (clone repo, patch to net10.0, dotnet build — per FR-008)
- [x] T013 [US1] Add project repos clone section to container/Containerfile (FSBarV1, HighBarV2, SkiaViewer only — per FR-010). Unset GH_TOKEN insteadOf after cloning.
- [x] T014 [US1] Add EXPOSE directive to container/Containerfile (ports 5020, 8080 only — per research.md R6)
- [x] T015 [US1] Add ENTRYPOINT and CMD to container/Containerfile pointing to container/entrypoint.sh
- [ ] T016 [US1] Verify complete Containerfile builds successfully with `podman build` — fix any build errors
- [ ] T017 [US1] Verify container runs and `dotnet --version`, `git --version`, `fsautocomplete --version`, `specify --help` all succeed inside the container

**Checkpoint**: User Story 1 complete — container builds and runs for headless/CLI use.

---

## Phase 4: User Story 2 - GPU Passthrough for Graphical Engine (Priority: P2)

**Goal**: Document and verify GPU passthrough so graphical applications (SkiaViewer, game engine) render on the host display via hardware-accelerated OpenGL.

**Independent Test**: Run container with GPU flags, execute `glxinfo | grep renderer` and confirm host GPU is visible (not software rendering).

### Implementation for User Story 2

- [x] T018 [US2] Verify container/Containerfile retains required native packages for GPU rendering (openal, sdl2, alsa-lib, nss, ttf-liberation, fontconfig, mesa-utils — per FR-009; mesa-utils provides glxinfo for GPU verification)
- [x] T019 [US2] Add GPU passthrough and X11 forwarding run commands to container/README.md — Mesa/AMD (`--device /dev/dri`) and NVIDIA (`--gpus all`) variants per research.md R1/R2
- [x] T020 [US2] Verify entrypoint.sh sets XDG_RUNTIME_DIR=/tmp/runtime-developer and creates the directory if missing
- [ ] T021 [US2] Test graphical mode: run container with GPU flags, verify `glxinfo` shows host GPU renderer

**Checkpoint**: User Story 2 complete — GPU passthrough documented and verified.

---

## Phase 5: User Story 3 - FSI MCP Server Integration (Priority: P2)

**Goal**: Ensure the FSI MCP server is pre-built and starts immediately inside the container, able to execute F# code against mounted BAR assemblies.

**Independent Test**: Start the FSI MCP server inside the container, confirm it binds to port 5020 and accepts connections.

### Implementation for User Story 3

- [x] T022 [US3] Verify FSI MCP server binary is pre-built in image at /home/developer/tools/fsi-mcp-server/server/ — `dotnet run --no-build` should start without compilation
- [x] T023 [US3] Document FSI MCP server start command in container/README.md (including required env vars XDG_RUNTIME_DIR, DISPLAY)
- [x] T024 [US3] Document how to load FSBar assemblies in FSI from the cloned project repos (reference CLAUDE.md prelude pattern)

**Checkpoint**: User Story 3 complete — FSI MCP server starts and runs F# code.

---

## Phase 6: User Story 4 - Clear Setup Documentation (Priority: P3)

**Goal**: Provide complete consumer-facing documentation covering build, run, verification, and optional Claude Code agent setup.

**Independent Test**: A developer unfamiliar with the project can follow the documentation to a working environment without additional guidance.

### Implementation for User Story 4

- [x] T025 [US4] Add Prerequisites section to container/README.md (Linux, Podman/Docker, BAR installation, GPU drivers optional)
- [x] T026 [US4] Add Build section to container/README.md with Podman and Docker commands (including GH_TOKEN build arg)
- [x] T027 [US4] Add Run section to container/README.md with headless and graphical mode variants (per quickstart.md)
- [x] T028 [US4] Add Verification section to container/README.md (dotnet, git, fsautocomplete, BAR mount check, FSI MCP server start, GPU check)
- [x] T029 [US4] Add Optional Claude Code Agent Setup section to container/README.md (per FR-012 — FSAgents clone, skills setup, settings.json generation, MCP server config)
- [x] T030 [US4] Add Troubleshooting section to container/README.md (no GPU, display not forwarding, port conflict, BAR folder missing)
- [x] T031 [US4] Add Port Reference table to container/README.md (5020 FSI MCP, 8080 general)

**Checkpoint**: All user stories complete — container builds, runs, GPU works, FSI MCP works, docs are comprehensive.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup across all deliverables

- [x] T032 [P] Review Containerfile for unnecessary layers — merge RUN commands where possible to reduce image size
- [ ] T033 [P] Verify image size is at least 30% smaller than original (compare with `podman images` / `docker images`)
- [ ] T034 Validate all container/README.md commands by executing them end-to-end
- [x] T035 Remove any commented-out or dead code from Containerfile

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - US1 must complete before US2/US3 (they verify what US1 builds)
  - US4 (documentation) can start after Phase 2 but references US2/US3 content
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) — no other story dependencies
- **User Story 2 (P2)**: Depends on US1 (needs a working container to test GPU passthrough)
- **User Story 3 (P2)**: Depends on US1 (needs a working container with FSI MCP server built)
- **User Story 4 (P3)**: Can start after Foundational but references outputs from US1–US3

### Within Each User Story

- Containerfile edits before verification steps
- Verification before documentation of that feature
- Story complete before moving to next priority

### Parallel Opportunities

- T002 and T003 can run in parallel (different files)
- T032 and T033 can run in parallel (independent checks)
- US4 documentation tasks (T025–T031) can all be written in a single pass

---

## Parallel Example: User Story 1

```bash
# Phase 1 — parallel file creation:
Task: "Create container/Containerfile with base image setup"
Task: "Create container/entrypoint.sh with XDG_RUNTIME_DIR creation"

# Phase 2 — sequential Containerfile sections (same file):
Task: "Add user setup section"
Task: "Add AUR freeimage section"
Task: "Add dev tools section"
# ... (sequential because all modify container/Containerfile)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (create directory + base files)
2. Complete Phase 2: Foundational (core Containerfile sections)
3. Complete Phase 3: User Story 1 (FSI MCP, repos, ports, build verification)
4. **STOP and VALIDATE**: Build the image, run it, verify core tools work
5. Deliver if ready — container works for headless/CLI development

### Incremental Delivery

1. Complete Setup + Foundational → Base image ready
2. Add User Story 1 → Test build/run → **MVP delivered**
3. Add User Story 2 → Test GPU passthrough → Graphical mode documented
4. Add User Story 3 → Test FSI MCP → Interactive F# ready
5. Add User Story 4 → Full documentation → Consumer-ready
6. Polish → Size validation, cleanup → Final delivery

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- All Containerfile modifications in Phase 2 are sequential (same file)
- Verification tasks (T016, T017, T021) require actually building/running the container
- Commit after each phase completion
