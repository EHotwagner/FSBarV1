# Implementation Plan: Repository Cleanup and Test Consolidation

**Branch**: `034-repo-cleanup` | **Date**: 2026-04-17 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/home/developer/projects/FSBarV1/specs/034-repo-cleanup/spec.md`

## Summary

Aggressive cleanup of a ~33k-line F# mono-repo that has grown across 33 feature branches: remove duplicate and near-duplicate code, consolidate four test projects into a single top-level `tests/` layout, strip all `private`/`internal` access modifiers from non-generated F# (the `.fsi` signature files are the real access gate per constitution §II), and apply idiomatic F# style to non-hot-path code. The refactor is behavior-preserving — baselines stay byte-stable, the trainer still runs, and every in-repo project appears in the solution file.

Technical approach:

1. **Move** `FSBar.Client.Tests` and `FSBar.SyntheticData.Tests` out of `src/` into `tests/`, along with their baselines.
2. **Collapse** three parallel SurfaceArea test implementations into one shared helper under `tests/Common/`.
3. **Lift** synthetic MapGrid construction into `FSBar.SyntheticData` so both test projects reuse it.
4. **Rename** colliding test basenames (`ConnectionTests.fs`, `CommandsTests.fs`/`CommandTests.fs`, `EventsTests.fs`/`EventTests.fs`, `MapQueryTests.fs`) with a `Live` prefix on the integration-test variants.
5. **Strip** all `private`/`internal` access modifiers from non-generated F#, leaving the `.fsi` surface untouched so baselines stay byte-stable.
6. **Apply** F# idiomatic style (pipelines, `Result`/`Option`, pattern-match preference) only on cold, non-hot-path modules (explicit allowlist from research §R8.1).
7. **Add** every project to `FSBarV1.slnx` so IDE and command-line builds agree.
8. **Document** the new layout in `tests/README.md` and the update in `CLAUDE.md`.

## Technical Context

**Language/Version**: F# 9 on .NET 10.0 (exclusive per constitution §Engineering Constraints)
**Primary Dependencies**: FsGrpc 1.0.6 (protobuf), BarData (NuGet local feed), SkiaViewer 1.1.3-dev (local nupkg), SkiaSharp 2.88.6, Silk.NET 2.22.0, xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x. No new dependencies introduced.
**Storage**: Filesystem only — committed `.baseline` text files, `viz-presets/*.json`, `bots/trainer/map-cache/*.json`. No persistence format changes.
**Testing**: xUnit 2.9.x across four test projects (two unit, two live/integration). Live tests launch a real BAR engine per fixture.
**Target Platform**: Linux developer container (per CLAUDE.md reference environment).
**Project Type**: Multi-project F# library ecosystem — 4 production projects (`FSBar.Proto`, `FSBar.Client`, `FSBar.SyntheticData`, `FSBar.Viz`) + 4 test projects, with `bots/trainer/` scripts consuming `FSBar.Client` as an external consumer.
**Performance Goals**: No regression — per-frame viz render stays at ~60fps, per-tick BarClient frame loop stays at engine rate. Hot-path modules (enumerated in `research.md` §R2) keep their existing mutable/imperative style verbatim.
**Constraints**: Must preserve `.fsi` signature files and surface-area baselines byte-stable; must not change protobuf wire format; must keep `Version="*-*"` wildcard references to the local nupkg feed; must not break trainer smoke run (FR-015, SC-009); must not touch generated code (`src/FSBar.Proto/Generated/**`, `*.generated.fs(i)`).
**Scale/Scope**: ~33,000 lines non-generated F# across 202 files; 7 production+test projects; 35 committed surface-area baselines; ~482 `private`/`internal` keyword removals across 53 non-generated files.

No NEEDS CLARIFICATION markers — the spec's clarification session (5 Qs, 2026-04-17) resolved scope, modifier policy, duplicate threshold, trainer gate, and line-count target.

## Constitution Check

Checked against `.specify/memory/constitution.md` v2.2.1.

| Principle | Status | Notes |
|---|---|---|
| **I. Spec-First Delivery** | ✅ PASS | Spec + clarifications + plan in place before implementation. All requirements have acceptance criteria. |
| **II. Compiler-Enforced Structural Contracts** | ✅ PASS | `.fsi` files preserved byte-stable. Baselines preserved byte-stable (research §R3.2). Removing `private` keywords does NOT change `.fsi`-gated public surface — `.fsi` is authoritative. |
| **III. Test Evidence Is Mandatory** | ✅ PASS | Existing test suite continues to pass (SC-008). Trainer smoke run validates consumer-side (SC-009). No new behavioral code; no new tests required. Renames are fixture-only. |
| **IV. Observability and Safe Failure Handling** | ✅ PASS | No change to diagnostics or error paths. Hot paths left alone. Idiomatic pass explicitly prohibits introducing silent exception-swallowing (research §R8.3). |
| **V. Scripting Accessibility** | ✅ PASS | `FSBar.Client`, `FSBar.Viz`, `FSBar.SyntheticData` already ship `scripts/prelude.fsx` + numbered examples. Cleanup does not affect this. `FSBar.Proto` is generated-types-only (exempt). |

**No violations — no justification required in Complexity Tracking.**

### Re-check hooks

Post-Phase-1 re-check applies at the bottom of this document after artifact generation.

## Project Structure

### Documentation (this feature)

```text
specs/034-repo-cleanup/
├── spec.md                 # /speckit.specify output
├── plan.md                 # This file (/speckit.plan output)
├── research.md             # Phase 0 output
├── data-model.md           # Phase 1 output — new project layout as structural model
├── quickstart.md           # Phase 1 output — how to build/test after cleanup
├── contracts/
│   └── baseline-invariant.md    # Phase 1 — baseline-invariance contract for this feature
├── checklists/
│   └── requirements.md     # from /speckit.specify
└── tasks.md                # /speckit.tasks output (NOT created by /speckit.plan)
```

### Source Code (repository root — post-cleanup target)

```text
src/
├── FSBar.Proto/            # generated types (unchanged)
├── FSBar.Client/           # core client library
├── FSBar.SyntheticData/    # synthetic game data (+ new SyntheticMapGrid module)
└── FSBar.Viz/              # visualization library

tests/
├── Common/                 # shared helpers (new) — SurfaceAreaHelper.fs, etc.
├── FSBar.Client.Tests/     # moved from src/
│   └── Baselines/          # 21 baselines moved with the project
├── FSBar.SyntheticData.Tests/   # moved from src/
├── FSBar.LiveTests/        # unchanged path; basenames normalized (Live* prefix)
│   ├── LiveConnectionTests.fs
│   ├── LiveCommandsTests.fs
│   ├── LiveEventsTests.fs
│   ├── LiveMapQueryTests.fs
│   └── EngineFixture.fs
├── FSBar.Viz.Tests/        # unchanged path
│   └── Baselines/          # 14 Viz baselines (unchanged)
├── engine-version.json     # unchanged
├── ENGINE-VERSION.md       # unchanged
├── run-all.sh              # updated to reference new test paths
└── README.md               # new — test taxonomy (unit vs live vs viz)

FSBarV1.slnx                # now lists all 8 projects
```

**Structure Decision**: Option 1-variant ("Single project" morphed for an F# multi-library ecosystem). All test projects live under top-level `tests/`; production projects stay under `src/`. Shared test helpers live in `tests/Common/` as loose compile-included files (research §R4.2), not a separate `.fsproj`. This matches the clarified user choice (Q1: A) and the existing F# ecosystem conventions visible in the repo.

### Files that move (summary)

| From | To |
|---|---|
| `src/FSBar.Client.Tests/` | `tests/FSBar.Client.Tests/` (+ baselines) |
| `src/FSBar.SyntheticData.Tests/` | `tests/FSBar.SyntheticData.Tests/` |
| `tests/FSBar.LiveTests/ConnectionTests.fs` | `tests/FSBar.LiveTests/LiveConnectionTests.fs` (rename) |
| `tests/FSBar.LiveTests/CommandTests.fs` | `tests/FSBar.LiveTests/LiveCommandsTests.fs` (rename + plural) |
| `tests/FSBar.LiveTests/EventTests.fs` | `tests/FSBar.LiveTests/LiveEventsTests.fs` (rename + plural) |
| `tests/FSBar.LiveTests/MapQueryTests.fs` | `tests/FSBar.LiveTests/LiveMapQueryTests.fs` (rename) |
| `tests/FSBar.Viz.Tests/VizEngineFixture.fs:testMapGrid` | deleted — use `FSBar.SyntheticData.SyntheticMapGrid.build` |
| `src/FSBar.Client.Tests/SyntheticMapGrid.fs` | `src/FSBar.SyntheticData/SyntheticMapGrid.fs` (+ new `.fsi`) |
| `tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs` | deleted — replaced by thin wrapper over shared helper |
| `src/FSBar.SyntheticData.Tests/SurfaceAreaTests.fs` (placeholder) | `tests/FSBar.SyntheticData.Tests/SurfaceAreaTests.fs` (thin wrapper) |

### Files that disappear (net deletions)

- `tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs` (367 lines, replaced by shared helper).
- `src/FSBar.Client.Tests/SurfaceAreaTests.fs` (138 lines of bespoke logic; core lifted into `tests/Common/SurfaceAreaHelper.fs`, thin wrapper remains).
- Ad-hoc `testMapGrid` in `VizEngineFixture.fs` (~20 lines).
- ~482 `private`/`internal` keywords across the tree.

Net: a meaningful reduction in size + one consolidated surface-area testing approach.

## Phase 0: Outline & Research — ✅ complete

See `research.md`. All spec clarifications resolved. No remaining unknowns blocking Phase 1.

## Phase 1: Design & Contracts

### 1.1 Data model — new project layout

See `data-model.md`. For a refactoring feature the "data model" is the post-cleanup project graph: which `.fsproj` references which, where each source file lives, and which baselines belong to which test project.

### 1.2 Contracts — baseline-invariance contract

See `contracts/baseline-invariant.md`. The external contract for this feature is: "`.fsi` signature files and `.baseline` files are byte-stable across the cleanup unless a deliberate surface change is flagged." This is the primary acceptance guard for constitution §II compliance.

### 1.3 Quickstart — how to build and test after the cleanup

See `quickstart.md`. The single top-level commands: `dotnet build FSBarV1.slnx` and `dotnet test FSBarV1.slnx`.

### 1.4 Agent context update

Run after plan is committed:

```bash
.specify/scripts/bash/update-agent-context.sh claude
```

This updates `CLAUDE.md` with the new project layout (test projects under `tests/`) and removes references to deleted modules.

## Constitution Re-check (post-Phase 1)

After Phase 1 artifacts (data-model, contracts, quickstart) are generated, re-evaluate:

| Principle | Status | Notes |
|---|---|---|
| **I. Spec-First Delivery** | ✅ PASS | Plan references spec requirements; tasks phase will emit task-level traceability. |
| **II. Compiler-Enforced Structural Contracts** | ✅ PASS | Baseline-invariance contract in `contracts/baseline-invariant.md` makes `.fsi`/`.baseline` stability an explicit acceptance gate. The new `FSBar.SyntheticData.SyntheticMapGrid` module requires a new `.fsi` + baseline — tasks phase must schedule both. |
| **III. Test Evidence Is Mandatory** | ✅ PASS | Test-layout changes are fixture-only; existing behavioral coverage unchanged. Quickstart documents how to verify. |
| **IV. Observability** | ✅ PASS | Unchanged. |
| **V. Scripting Accessibility** | ✅ PASS | No new public modules on the shipped libraries that require prelude coverage (the new `SyntheticMapGrid` module inside `FSBar.SyntheticData` is test-facing; existing `FSBar.SyntheticData/scripts/prelude.fsx` updates would be ergonomic but not required). Tasks phase flags this as optional. |

**Post-design gate: PASS. Ready for `/speckit.tasks`.**

## Complexity Tracking

*No constitution violations — table omitted.*
