# Feature Specification: Repository Cleanup and Test Consolidation

**Feature Branch**: `034-repo-cleanup`
**Created**: 2026-04-17
**Status**: Draft
**Input**: User description: "aggressively cleanup the repo. search for duplicate funcationality and refactor. make the code simpler and more fsharp idiomatic if not performance relevant. no private modifiers, fsi already handles that. consolidate the testing suite."

## Clarifications

### Session 2026-04-17

- Q: Where should the consolidated test projects live — top-level `tests/`, co-located under `src/`, or a formalized mixed layout? → A: All test projects under top-level `tests/` (move `FSBar.Client.Tests` and `FSBar.SyntheticData.Tests` out of `src/`).
- Q: Should the cleanup permit any `private`/`internal` modifiers on non-generated F#, or enforce a hard zero? → A: Hard zero on non-generated F#. Only excluded paths: `src/FSBar.Proto/Generated/**` and committed `*.generated.fs`.
- Q: How aggressive should the duplicate-hunt be? → A: Literal duplicates + near-duplicates that collapse to a single parameterized implementation without new abstractions. No speculative unification.
- Q: What's the trainer behavioral-equivalence acceptance gate? → A: Smoke check only — trainer starts, emits non-empty JSONL, exits cleanly on a supported map. Byte-equivalence is not required.
- Q: Is a specific line-count reduction target the right acceptance criterion, or should we measure cleanup by structural outcomes alone? → A: Drop the percentage target. Use structural outcomes (SC-002, SC-005, FR-002) as the acceptance for "duplication removed."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Eliminate Duplicated Functionality (Priority: P1)

A contributor (human or agent) reading a module wants to trust that there is exactly one place where any given piece of logic lives. Today the repo has grown organically across 33 feature branches, and multiple modules contain near-identical helpers, parallel test files with the same name under different projects (for example `SurfaceAreaTests.fs` appears in both `FSBar.Client.Tests` and `FSBar.SyntheticData.Tests`), and overlapping helpers for the same underlying operation. A contributor should be able to change behavior in one place and have the change take effect everywhere it is needed, without grepping for silent copies.

**Why this priority**: Duplication is the single largest driver of defects and churn in this repo. Left unchecked, it causes silent behavioral drift, doubles the surface area of tests that have to be maintained, and makes every subsequent feature more expensive. Removing it is the highest-value cleanup pass.

**Independent Test**: After the cleanup, a contributor can pick any externally-observable helper (e.g. "compute resource spot coordinates", "surface-area summary of a module") and find exactly one authoritative implementation, with every caller routing through it. This can be verified by searching the repo for the operation and confirming a single definition site; the full build and test suite still passes against that single definition.

**Acceptance Scenarios**:

1. **Given** two files that encode the same logic under different names, **When** the cleanup completes, **Then** only one of them remains and all call sites route through it.
2. **Given** a pair of tests covering the same assertion under different projects, **When** the cleanup completes, **Then** exactly one of them remains, in the project that owns the subject under test.
3. **Given** a helper function that is reimplemented inline in multiple modules, **When** the cleanup completes, **Then** the helper is defined once in the owning module and reused.

---

### User Story 2 - Consolidate the Test Suite into a Coherent Layout (Priority: P2)

A contributor running the tests today has to know that some tests live under `src/FSBar.Client.Tests/` and `src/FSBar.SyntheticData.Tests/` (co-located with source) while others live under `tests/FSBar.LiveTests/` and `tests/FSBar.Viz.Tests/` (in the top-level tests directory). Test helpers and fixtures are scattered across both trees. The solution file `FSBarV1.slnx` only lists three of the seven-plus F# projects, so IDE navigation and `dotnet build` behavior diverge. After the cleanup, every test project lives under top-level `tests/`, one documented command runs the full suite, and the solution file lists every project the repo ships.

**Why this priority**: Test-suite friction compounds across every feature. Inconsistent layout makes contributors reluctant to add tests where they belong, encourages ad-hoc one-off scripts, and causes CI/local drift. Consolidation is high-value but lower-risk than code-level refactoring, so it sits just below P1.

**Independent Test**: After the cleanup, a contributor who has never seen this repo can (a) open the solution file and see every F# project the repo ships; (b) run a single documented command from the repo root that executes the entire test suite; (c) locate the tests for any given source module in a single predictable location.

**Acceptance Scenarios**:

1. **Given** the consolidated repository, **When** a contributor opens `FSBarV1.slnx`, **Then** every F# project in `src/` and `tests/` is listed.
2. **Given** the consolidated repository, **When** a contributor runs the documented top-level test command, **Then** every test project runs and reports pass/fail.
3. **Given** a source module `Foo` in project `FSBar.X`, **When** a contributor looks for its tests, **Then** they are found at one predictable path and nowhere else.
4. **Given** fixtures and helpers currently duplicated across test projects, **When** the cleanup completes, **Then** shared fixtures live in exactly one shared location and are referenced from test projects that need them.

---

### User Story 3 - F# Idiomatic Style Pass and Removal of `private` Modifiers (Priority: P3)

A contributor reading F# code in this repo should see idiomatic F# — pipelines, pattern matching, immutable data, `Result`/`Option` rather than nulls or exception-for-control-flow — in any code path where performance is not the overriding constraint. In particular, because FSI-based development is the workflow (see CLAUDE.md), `private` and `internal` modifiers on modules, types, and members impede REPL exploration and add no enforcement value inside a single repository. These should be removed wherever they appear outside generated code, unless the code path is demonstrably performance-sensitive and relies on some specific access pattern.

**Why this priority**: Style consistency improves readability and onboarding but does not block features. It is lowest-priority because the return is diffuse. It is still in scope because the user explicitly asked for it and because it aligns the codebase with how the team actually develops (FSI-first).

**Independent Test**: After the cleanup, a contributor can grep for `\bprivate\b` or `\binternal\b` in non-generated source and get zero hits (excluding `src/FSBar.Proto/Generated/**` and committed `*.generated.fs(i)`). A contributor reviewing a random non-performance-sensitive module finds idiomatic F# style (pipelines, `Result`/`Option`, pattern matching) rather than imperative or OO translations.

**Acceptance Scenarios**:

1. **Given** any non-generated `.fs` or `.fsi` file, **When** a contributor searches for `private` or `internal` access modifiers, **Then** there are zero uses — no allowlist.
2. **Given** a non-performance-sensitive module, **When** a contributor reviews it, **Then** it uses F# idioms (pipelines, discriminated unions, pattern matching) rather than imperative equivalents.
3. **Given** a performance-sensitive hot path (e.g. per-frame rendering, per-tick game state mapping), **When** the cleanup touches it, **Then** the existing performance-oriented style (mutable arrays, loops, in-place updates) is preserved.

---

### Edge Cases

- **Generated code** (`src/FSBar.Proto/Generated/**`) must not be touched by the style pass; regenerating protobuf would undo manual edits.
- **Cached artifacts** committed under `bots/trainer/map-cache/*.json` are products of `MapCacheFile` with a pinned `codeVersion`. If the cleanup changes `MapCacheFile` serialization semantics, the cache must be regenerated and re-committed, per CLAUDE.md.
- **Committed generated F#** such as `src/FSBar.Viz/UnitLabels.generated.fs(i)` is regenerated by a script; the `.fsi` must stay stable and the `.fs` must match a regeneration run.
- **Hot paths** (per-frame renderer, per-tick game-state update, per-frame MapGrid reshape) must preserve their mutable/imperative style; only non-hot code gets the idiomatic pass.
- **Upstream dependencies** consumed via local NuGet feed (SkiaViewer, BarData) must not be broken by the cleanup — version wildcards (`*-*`) must be preserved.
- **Containerfile / CI scripts** that invoke `dotnet build` or specific project paths must keep working after project moves or renames.
- **Surface-area baselines** (the `.baseline` files in `tests/FSBar.Viz.Tests/Baselines/` and `tests/FSBar.Client.Tests/Baselines/`) are gated by `.fsi` signature files, not by `private` keywords in `.fs` files. Removing `private` from a `.fs` file does NOT change the public surface and therefore does NOT regenerate the baseline. Removing `private` from an `.fsi` file DOES expose a symbol and MUST regenerate the corresponding baseline; those per-file deltas are enumerated in `contracts/baseline-invariant.md` before implementation runs. The default expectation is that 34 of 35 baselines stay byte-stable; only the new `SyntheticMapGrid.baseline` and any explicit `.fsi`-driven exposures change.
- **In-flight work** in other branches will conflict with large refactors. The cleanup should happen on a single branch and land as one bundled merge to minimize downstream rebase pain.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The cleanup MUST identify and remove duplicate functionality across the repo. In-scope duplicates are: (a) literal duplicates — two or more pieces of code that compute the same thing with the same inputs and outputs; and (b) near-duplicates that would collapse to a single parameterized implementation without introducing a new shared abstraction. Speculative unification — inventing a new common module to merge code that happens to look similar — is explicitly out of scope.
- **FR-002**: After the cleanup, every remaining externally-observable operation MUST have exactly one authoritative definition site, and every caller MUST route through that site (no inlined re-implementations).
- **FR-003**: The cleanup MUST consolidate all test projects under the top-level `tests/` directory. `FSBar.Client.Tests` and `FSBar.SyntheticData.Tests` MUST be moved out of `src/` into `tests/`, joining the existing `tests/FSBar.LiveTests` and `tests/FSBar.Viz.Tests`. No test projects remain under `src/` after the cleanup.
- **FR-004**: The solution file (`FSBarV1.slnx`) MUST list every F# project the repo ships (source and test), so that IDE and command-line builds agree on project membership.
- **FR-005**: A single documented top-level command MUST run the full test suite across every test project, with clear pass/fail reporting.
- **FR-006**: Shared test fixtures, helpers, and synthetic data builders MUST live in exactly one location and be referenced from every test project that needs them (no copy-paste fixtures).
- **FR-007**: All `private` and `internal` access modifiers in non-generated F# source (`.fs` and `.fsi`) MUST be removed. There is no allowlist. The only excluded paths are `src/FSBar.Proto/Generated/**` and any committed `*.generated.fs`/`*.generated.fsi` (regenerated by their scripts). If removal of a modifier breaks compilation or a test, the fix is to resolve the underlying coupling — not to keep the modifier.
- **FR-008**: Non-performance-sensitive code MUST be rewritten in idiomatic F# style (pipelines, `Result`/`Option`, pattern matching, immutable data) where the existing code is notably imperative or OO-translated.
- **FR-009**: Performance-sensitive hot paths MUST NOT be rewritten for style. These include per-frame rendering, per-tick game-state updates, and per-frame MapGrid reshape. Any change to a hot path must preserve its existing allocation and control-flow characteristics.
- **FR-010**: Generated code (`src/FSBar.Proto/Generated/**`, `src/FSBar.Viz/UnitLabels.generated.fs`) MUST NOT be modified by hand. Regeneration, if needed, MUST go through the existing scripts.
- **FR-011**: The committed map-cache files under `bots/trainer/map-cache/` MUST remain loadable after the cleanup. If `MapCacheFile` serialization changes, the cache files MUST be regenerated via `bots/trainer/map-cache/refresh-all.sh` and re-committed.
- **FR-012**: Surface-area `.baseline` files MUST be regenerated and re-committed if the public API surface changes as a side-effect of removing access modifiers or deleting duplicate modules.
- **FR-013**: After the cleanup, a fresh `dotnet build` of the solution from a clean working copy MUST succeed without warnings newly introduced by this feature.
- **FR-014**: After the cleanup, the full test suite MUST pass on the developer container environment with the engine versions currently supported.
- **FR-015**: The cleanup MUST NOT break external consumers of `FSBar.Client` (the bot trainer scripts under `bots/trainer/`). Acceptance is a trainer smoke run on a supported map: the trainer starts, reaches a steady game frame, emits non-empty JSONL frame logs, and exits cleanly. Byte-equivalence of JSONL or final-result JSON across runs is NOT required (engine non-determinism makes that unrealistic).
- **FR-016**: The cleanup MUST preserve the `Version="*-*"` wildcard pattern for `PackageReference`s pointing at the local `nupkg/` feed (SkiaViewer, BarData), since upstream pack-dev workflows depend on it.
- **FR-017**: Any deletion of a module, project, or file MUST also remove stale references in `.fsproj` compile ordering, `CLAUDE.md` pointers, spec notes, and any scripts or bash entrypoints that reference it.
- **FR-018**: The cleanup MUST produce a short written summary of what was removed and what was merged, so that future features referencing old module names can find their replacements.

### Key Entities

- **Duplicate Code Group**: Two or more pieces of code (functions, modules, tests, fixtures) that produce the same effect for the same inputs, OR that would collapse to a single parameterized implementation without inventing a new shared abstraction. The group is replaced by a single authoritative version.
- **Test Project**: A `.fsproj` that contains only test code and references its subject project(s). The consolidated layout has a single rule for where these live.
- **Shared Fixture**: A test helper or synthetic-data builder used by more than one test project, lifted into one shared location.
- **Hot Path**: A code path whose performance characteristics are load-bearing for a real-time feature (per-frame rendering, per-tick updates). These are explicitly excluded from the idiomatic-style pass.
- **Surface Baseline**: A committed `.baseline` text file listing the public API of a project, used to detect accidental surface changes. Must be regenerated when the public surface legitimately changes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After cleanup, every externally-observable operation in the repo has exactly one authoritative definition site, verifiable by inspection of a short written summary (per FR-018) that lists each merged duplicate group and its surviving implementation. No percentage-based line-count target is imposed; structural outcomes (SC-002, SC-005, FR-002) are the acceptance criteria for "duplication removed."
- **SC-002**: After cleanup, every non-generated F# file contains zero occurrences of `private` and `internal` as access modifiers on modules, types, or members. Acceptance is a single grep across `src/` and `tests/` excluding `src/FSBar.Proto/Generated/**` and `*.generated.fs(i)`; the result is empty.
- **SC-003**: After cleanup, the solution file (`FSBarV1.slnx`) lists 100% of the F# projects the repo ships; a contributor opening the solution sees every source and test project.
- **SC-004**: After cleanup, a single documented top-level command runs the entire test suite end-to-end in one invocation, with a single aggregated pass/fail summary.
- **SC-005**: After cleanup, the number of test projects with duplicated test-file names (same basename in multiple projects covering overlapping behavior) is zero.
- **SC-006**: After cleanup, a contributor can locate the tests for any given source module in under 30 seconds using only the directory tree and a predictable naming rule — no grep across multiple test projects required.
- **SC-007**: After cleanup, the full `dotnet build` of the solution completes with no new warnings attributable to this feature, on the developer container environment.
- **SC-008**: After cleanup, the full test suite passes on the developer container environment with the currently-supported engine versions.
- **SC-009**: After cleanup, a trainer smoke run on a supported map starts successfully, reaches a steady game frame, produces non-empty JSONL frame logs, and exits cleanly. No byte-equivalence requirement across runs.
- **SC-010**: After cleanup, `CLAUDE.md` and any module-pointer notes in it reflect the new module layout — no references to deleted or moved modules remain.

## Assumptions

- The cleanup happens on a single feature branch (`034-repo-cleanup`) and lands as one bundled merge, since large refactors conflict badly with parallel feature work. In-flight branches will rebase onto the cleaned tree.
- The developer container environment is the reference environment for all build and test validation (per CLAUDE.md). CI behavior is not separately tracked.
- "F# idiomatic" means the style already used in the cleaner parts of this codebase — pipelines, discriminated unions, `Result`/`Option`, pattern matching — rather than an external style guide.
- "Performance-sensitive" is determined by whether a function is on a per-frame or per-tick hot path reachable from the live viewer (`FSBar.Viz.GameViz`), the game loop (`FSBar.Client.BarClient`), or the trainer's per-frame logging. When in doubt, leave the existing style alone.
- Committed generated artifacts (`src/FSBar.Viz/UnitLabels.generated.fs`, `src/FSBar.Proto/Generated/**`) are regenerated only by their existing scripts and are not edited by hand as part of this cleanup.
- Upstream NuGet packages (SkiaViewer, BarData) are not modified by this feature; only the consumer-side wildcard version references and any accidentally-drifted references are normalized.
- The map-cache JSON files under `bots/trainer/map-cache/` are regenerated via the existing `refresh-all.sh` script if and only if the `MapCacheFile` serialization or `codeVersion` changes as part of this cleanup.
- Surface-area `.baseline` files are regenerated whenever the public API surface legitimately changes; the regeneration mechanism already exists (feature 007 established it).
- The user's instruction to drop `private` modifiers is scoped to F# access modifiers; it is not a directive to change visibility of `.fsproj` compile order, `InternalsVisibleTo`, or anything else outside the language-level access-modifier list.
