# Research: Fix Stale DLL Cache Problem

## Decision 1: Version Format for Auto-Incrementing Dev Builds

**Decision**: Use `<BaseVersion>-dev.<timestamp>` format (e.g., `1.0.0-dev.20260408T113045`) for upstream dev builds.

**Rationale**: NuGet prerelease versions sort lexicographically after the `-` delimiter. Timestamp-based suffixes are monotonically increasing, unique per build, and human-readable. The format `dev.YYYYMMDDTHHmmss` avoids NuGet's 20-character prerelease label limit while remaining unambiguous.

**Alternatives considered**:
- Git commit hash suffix (`1.0.0-dev.abc1234`): Not monotonically sortable; NuGet would not reliably select the latest.
- Incrementing build number (`1.0.0-dev.42`): Requires persistent state to track the counter; fragile across machines.
- Date-only (`1.0.0-dev.20260408`): Not unique within a day; multiple rebuilds would collide.

## Decision 2: How FSBarV1 Accepts Prerelease Versions

**Decision**: Use floating version `*-*` or version range syntax in PackageReference to accept latest prerelease.

**Rationale**: 
- BarData already uses `Version="*"` which accepts any version including prereleases.
- SkiaViewer uses `Version="1.0.0"` (exact), which must be changed to `Version="1.0.0-*"` or `Version="*-*"` to accept dev builds.
- NuGet's `*-*` wildcard matches the latest version including prereleases from all configured sources.

**Alternatives considered**:
- Central Package Management (Directory.Packages.props): Adds indirection; overkill for 2 local packages.
- Pinning exact dev version in .fsproj: Defeats the purpose — would require updating .fsproj on every rebuild.

## Decision 3: Where Versioning Logic Lives

**Decision**: Add a build/pack script to each upstream project (SkiaViewer, HighBarV2/BarData) that generates the timestamp version and packs to the FSBarV1 local feed directory.

**Rationale**: The version increment must happen at the upstream project's pack step, not at the consumer's restore step. A simple shell script (e.g., `pack-dev.sh`) in each upstream project calls `dotnet pack --version-suffix dev.$(date +%Y%m%dT%H%M%S)` and copies the result to the consumer's `nupkg/` directory.

**Alternatives considered**:
- MSBuild property in Directory.Build.props: More implicit; harder to understand and debug.
- Manual version bump: Current workflow — the problem we're solving.

## Decision 4: Staleness Verification Command

**Decision**: Add a shell script (`scripts/check-deps.sh`) to FSBarV1 that compares the `.nupkg` file hashes in `nupkg/` against the DLLs in build output directories.

**Rationale**: Simple, fast (checksums only), no build system integration needed. Can be run independently or wired into a pre-build step.

**Alternatives considered**:
- MSBuild target: More integrated but harder to debug; script is more transparent.
- Assembly version metadata comparison: Requires loading assemblies; slower and more complex.

## Decision 5: FSI Prelude DLL Freshness

**Decision**: No change needed to prelude.fsx. The prelude loads DLLs from `bin/Debug/net10.0/` which are populated by `dotnet build`. Once NuGet restore correctly pulls the latest dev version (via auto-incrementing), the build output directories will always contain current DLLs.

**Rationale**: The prelude's hard-coded paths are correct — they point to build output, which is the authoritative location after a build. The staleness problem was upstream of the prelude (NuGet cache → restore → build), not in the prelude itself.

**Alternatives considered**:
- Dynamic DLL discovery in prelude: Over-engineered; the build system should produce correct output.
