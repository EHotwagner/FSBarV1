# Implementation Plan: Fix Stale DLL Cache Problem

**Branch**: `015-fix-stale-dll-cache` | **Date**: 2026-04-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/015-fix-stale-dll-cache/spec.md`

## Summary

Eliminate stale DLL cache problems by auto-incrementing package versions on each upstream rebuild. Each `dotnet pack` of SkiaViewer or BarData produces a unique timestamp-based prerelease version (e.g., `1.0.0-dev.20260408T113045`). FSBarV1's PackageReferences use wildcard ranges to accept the latest dev version. NuGet's caching works correctly since every rebuild is a distinct version. A verification script confirms dependency freshness.

## Technical Context

**Language/Version**: F# / .NET 10.0
**Primary Dependencies**: SkiaViewer (local nupkg), BarData (local nupkg), NuGet CLI tooling
**Storage**: Filesystem (nupkg files, NuGet global cache)
**Testing**: Manual verification via shell scripts; xUnit for any F# code
**Target Platform**: Linux (development container)
**Project Type**: Build tooling / developer workflow improvement
**Performance Goals**: Pack + restore + build cycle must not add measurable overhead
**Constraints**: Must work offline (local feeds only); must not require version edits in .fsproj files
**Scale/Scope**: 2 upstream projects, 1 consumer project, ~6 .fsproj files affected

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| Spec-First Delivery | ✅ Pass | Spec and plan created before implementation |
| Compiler-Enforced Contracts | N/A | No new public F# modules; this is build tooling (shell scripts) |
| Test Evidence | ✅ Pass | Verification script serves as automated check; manual workflow test defined |
| Observability | ✅ Pass | Pack script outputs version; check-deps.sh reports freshness |
| Scripting Accessibility | ✅ Pass | FSI prelude unchanged; build output freshness ensures prelude correctness |
| F# Exclusive Stack | ⚠️ Partial | Pack scripts are Bash (not F#), but they are build tooling outside the F# project scope per constitution ("Multi-language needs MUST be addressed by separate projects") |
| dotnet pack to local store | ✅ Pass | Pack-dev scripts produce nupkg to local feed |

**Post-Phase 1 Re-check**: No new violations. Bash scripts are build infrastructure, not application code.

## Project Structure

### Documentation (this feature)

```text
specs/015-fix-stale-dll-cache/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
# FSBarV1 changes
scripts/
└── check-deps.sh        # New: dependency freshness verification
src/
├── FSBar.Client/
│   └── FSBar.Client.fsproj  # Update: BarData version range
└── FSBar.Viz/
    └── FSBar.Viz.fsproj     # Update: SkiaViewer version range

# Upstream project changes (separate repos)
~/projects/SkiaViewer/
├── pack-dev.sh              # New: timestamp-versioned pack script
└── src/SkiaViewer/
    └── SkiaViewer.fsproj    # Update: VersionPrefix property

~/projects/HighBarV2/
├── pack-dev.sh              # New: timestamp-versioned pack script
└── data/bar/
    └── BarData.fsproj       # Update: VersionPrefix property
```

**Structure Decision**: No new F# projects. Changes are limited to .fsproj version properties, shell scripts for packaging, and a verification script.

## Implementation Phases

### Phase 1: Upstream Pack Scripts (SkiaViewer + HighBarV2)

Add `pack-dev.sh` to each upstream project:
- Generates timestamp suffix: `dev.$(date +%Y%m%dT%H%M%S)`
- Runs `dotnet pack --version-suffix $SUFFIX -o $TARGET_DIR`
- Removes old nupkg for same package ID from target dir
- Accepts target directory as argument (required — typically the consumer project's `nupkg/` directory)

Update upstream .fsproj files:
- Set `<VersionPrefix>1.0.0</VersionPrefix>` (so `--version-suffix` works correctly)
- Remove any hard-coded `<Version>` that conflicts

### Phase 2: FSBarV1 PackageReference Updates

Update consumer .fsproj files to accept prerelease versions:
- `FSBar.Client.fsproj`: BarData already uses `Version="*"` — verify it accepts prereleases (may need `*-*`)
- `FSBar.Viz.fsproj`: Change SkiaViewer from `Version="1.0.0"` to `Version="*-*"` or `1.0.0-*`

### Phase 3: Verification Script

Create `scripts/check-deps.sh`:
- Lists each local-feed package in `nupkg/`
- Extracts the DLL from the nupkg (ZIP)
- Compares hash against the corresponding DLL in build output directories
- Reports fresh/stale status per package

### Phase 4: Documentation & CLAUDE.md

- Update CLAUDE.md with new workflow instructions
- Update quickstart with pack-dev usage
- Remove any stale-cache workaround documentation
