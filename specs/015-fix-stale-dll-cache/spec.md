# Feature Specification: Fix Stale DLL Cache Problem

**Feature Branch**: `015-fix-stale-dll-cache`
**Created**: 2026-04-08
**Status**: Draft
**Input**: User description: "this project uses two other projects, highbar and skiaviewer. stale cached dlls are a constant problem. what are options to fix that?"

## Clarifications

### Session 2026-04-08

- Q: Should the solution work within NuGet's versioning model (auto-bump versions) or around it (bypass/invalidate cache)? → A: Auto-increment version on each upstream rebuild (e.g., `1.0.0-dev.20260408T1130`), so NuGet always sees a new version.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Rebuilding After Upstream Changes (Priority: P1)

A developer updates a dependency project (HighBar or SkiaViewer), rebuilds it, and expects the consuming project (FSBarV1) to immediately use the new version without manual cache-clearing steps.

Today, after rebuilding SkiaViewer or BarData and updating the local `.nupkg` file, the developer must manually clear the NuGet global cache, force-restore, and sometimes hand-copy DLLs into bin directories. This wastes significant time and causes hard-to-diagnose bugs when stale DLLs silently persist.

**Why this priority**: This is the core pain point — every upstream change triggers a multi-step manual cache invalidation process.

**Independent Test**: Rebuild an upstream dependency, update the nupkg, run a single build command, and verify the new DLL is used — without any manual cache-clearing.

**Acceptance Scenarios**:

1. **Given** an upstream dependency has been rebuilt and its package file updated, **When** the developer builds FSBarV1, **Then** the build uses the updated dependency without manual intervention.
2. **Given** a developer has not cleared any caches, **When** the upstream package is updated in the local feed, **Then** subsequent builds pick up the new version automatically.
3. **Given** the developer builds FSBarV1 from a clean checkout, **When** all dependency packages are present in the local feed, **Then** the build succeeds with the correct dependency versions on the first attempt.

---

### User Story 2 - FSI REPL Sessions Use Current DLLs (Priority: P2)

A developer starts an FSI session (via the MCP server or manually) and expects it to load the most recently built DLLs from dependency projects. Today, FSI loads DLLs from hard-coded paths in the Debug bin directory, and these can be stale if the NuGet restore didn't propagate updated dependencies.

**Why this priority**: The REPL is the primary interactive development tool. Stale DLLs in FSI cause confusing runtime errors and wasted debugging time.

**Independent Test**: Update an upstream dependency, build FSBarV1, restart FSI, and verify the new dependency version is loaded.

**Acceptance Scenarios**:

1. **Given** FSBarV1 has been rebuilt with an updated dependency, **When** the developer restarts FSI and loads the project, **Then** the FSI session uses the updated dependency DLL.
2. **Given** a dependency has been rebuilt, **When** the developer runs the standard build command before starting FSI, **Then** the DLLs in the bin directories match the current dependency versions.

---

### User Story 3 - Clear Feedback When Dependencies Are Out of Date (Priority: P3)

When a dependency package is updated but the build output still contains an older version, the developer receives a clear warning or error rather than silently using stale code.

**Why this priority**: Even with automation, edge cases can cause staleness. Clear feedback prevents silent bugs.

**Independent Test**: Introduce a version mismatch between the local feed and build output, run a verification command, and confirm a warning is emitted.

**Acceptance Scenarios**:

1. **Given** the local feed contains a newer package than what is in the build output, **When** the developer runs a verification command, **Then** a clear message identifies which dependencies are stale.

---

### Edge Cases

- What happens when the upstream package version number stays the same but the contents change? → Resolved: each upstream rebuild auto-increments the version, so same-version updates no longer occur.
- How does the system handle network-unavailable scenarios where only local feeds are accessible?
- What happens when multiple developers have different local cache states?
- What happens when a dependency's transitive dependencies change but the top-level version stays the same? → Resolved: version increment forces full re-resolve of transitive dependencies.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The build process MUST detect when a local feed package has been updated and propagate the new binary to all build outputs.
- **FR-002**: The system MUST NOT require manual cache-clearing commands as part of the normal development workflow after updating a dependency.
- **FR-003**: Each upstream rebuild MUST produce a unique version identifier (e.g., timestamp-based prerelease suffix) so that NuGet's cache correctly distinguishes between builds.
- **FR-004**: The build MUST ensure all downstream projects (tests, FSI prelude paths) receive the updated dependency after a single build invocation.
- **FR-005**: The system MUST provide a way to verify that build outputs match the current dependency packages.
- **FR-006**: The solution MUST work for both BarData and SkiaViewer dependency packages.
- **FR-007**: The solution MUST NOT break existing CI or local development workflows.
- **FR-008**: The consuming project (FSBarV1) MUST accept prerelease/dev versions of local-feed dependencies via version range or wildcard.

### Key Entities

- **Local Feed Package**: A `.nupkg` file in the repository's `nupkg/` directory serving as the dependency source. Each rebuild produces a unique version (e.g., `1.0.0-dev.20260408T1130`).
- **NuGet Global Cache**: The system-wide cache at `~/.nuget/packages/` where NuGet unpacks and caches package contents. Auto-incrementing versions ensure cache entries are never stale.
- **Build Output**: The `bin/Debug/net10.0/` directories where compiled DLLs are placed for runtime use.
- **Dependency Chain**: The transitive path from an upstream project (HighBar, SkiaViewer) through packaging, caching, restore, and build to the final consumer output.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After updating an upstream dependency, the developer can use the new version in FSBarV1 with a single build command (zero manual cache-clearing steps).
- **SC-002**: FSI sessions always load DLLs that match the most recently built dependency versions after a build + FSI restart.
- **SC-003**: Each upstream rebuild produces a unique version, eliminating same-version cache collisions entirely.
- **SC-004**: The total time from "upstream dependency rebuilt" to "FSBarV1 using new version" does not increase compared to today's workflow (excluding the manual cache-clearing steps that are eliminated).
- **SC-005**: A verification command can confirm all dependency DLLs match their source packages in under 5 seconds.

## Assumptions

- HighBar and SkiaViewer will continue to be consumed as NuGet packages from a local feed, not as ProjectReferences (they are separate repositories).
- The local feed directory (`nupkg/`) is the single source of truth for dependency package contents.
- Developers rebuild upstream projects and update the local feed nupkg files as part of their existing workflow.
- The solution should be compatible with the existing `nuget.config` dual-source setup (local feed + nuget.org).
- Upstream projects will adopt auto-incrementing version numbers for dev builds (e.g., timestamp-based prerelease suffixes).
- FSBarV1 PackageReferences will use version ranges or wildcards to accept the latest prerelease version from the local feed.
