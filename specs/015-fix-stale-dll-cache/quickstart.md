# Quickstart: Fix Stale DLL Cache Problem

## Developer Workflow After Implementation

### Updating SkiaViewer

```bash
# In the SkiaViewer repo, after making changes:
cd ~/projects/SkiaViewer
./pack-dev.sh ~/projects/FSBarV1/nupkg

# In FSBarV1, just build — NuGet picks up the new version automatically:
cd ~/projects/FSBarV1
dotnet build
```

### Updating BarData (HighBarV2)

```bash
# In the HighBarV2 repo:
cd ~/projects/HighBarV2
./pack-dev.sh ~/projects/FSBarV1/nupkg

# In FSBarV1:
cd ~/projects/FSBarV1
dotnet build
```

### Verifying Dependencies Are Fresh

```bash
cd ~/projects/FSBarV1
./scripts/check-deps.sh
# Output: ✓ BarData 1.0.0-dev.20260408T113045 — fresh
#         ✓ SkiaViewer 1.0.0-dev.20260408T114530 — fresh
```

### FSI Session

```bash
# After building, restart FSI to pick up new DLLs:
# (FSI MCP tool: restart_fsi)
# Then load prelude as normal — DLLs in bin/ are current.
```

## What Changed

1. **Upstream projects** (SkiaViewer, HighBarV2): Added `pack-dev.sh` script that packs with timestamp-based prerelease version.
2. **FSBarV1 PackageReferences**: Changed from exact versions to wildcard ranges that accept prerelease versions.
3. **FSBarV1**: Added `scripts/check-deps.sh` for dependency freshness verification.
