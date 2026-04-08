# Data Model: Fix Stale DLL Cache Problem

## Entities

### Dev Package Version

A version identifier generated at pack time for upstream development builds.

- **Format**: `<Major>.<Minor>.<Patch>-dev.<Timestamp>`
- **Example**: `1.0.0-dev.20260408T113045`
- **Uniqueness**: Guaranteed unique per second (timestamp-based)
- **Ordering**: Lexicographic sort on the prerelease suffix produces chronological ordering
- **Lifecycle**: Created at `dotnet pack` time; consumed by `dotnet restore` in FSBarV1

### Local Feed Package

A `.nupkg` file in the FSBarV1 `nupkg/` directory.

- **Attributes**: Package ID, Version, File hash (SHA256), File modification time
- **Relationships**: One-to-many with Build Output DLLs (a single nupkg feeds multiple project outputs)
- **State transitions**: Created → Superseded (when a newer dev version is packed)

### Build Output DLL

A DLL in a project's `bin/Debug/net10.0/` directory.

- **Attributes**: File name, Assembly version, File hash, Source package version
- **Relationships**: Many-to-one with Local Feed Package
- **Freshness**: Considered fresh when its file hash matches the DLL inside the latest nupkg for its package ID

## Validation Rules

- Package version MUST match the regex `^\d+\.\d+\.\d+(-dev\.\d{8}T\d{6})?$`
- PackageReference version ranges MUST accept prerelease versions (e.g., `*-*` or `1.0.0-*`)
- The `nupkg/` directory MUST contain at most one version per package ID (superseded versions are removed)
