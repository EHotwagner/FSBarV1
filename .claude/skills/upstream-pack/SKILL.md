---
name: "upstream-pack"
description: "Update a locally-developed NuGet dependency (SkiaViewer, BarData) into FSBarV1 via the upstream pack-dev.sh timestamp-versioned prerelease workflow. Use when user edits a sibling repo and needs the change picked up in FSBarV1."
user-invocable: true
---

## Workflow

In the upstream repo:
```bash
./pack-dev.sh ~/projects/FSBarV1/nupkg
```

This produces a timestamp-versioned prerelease (e.g., `1.0.0-dev.20260408T115727`), which sidesteps NuGet's global-cache staleness.

In FSBarV1:
```bash
dotnet build
```

PackageReferences use `Version="*-*"` to pick up the latest prerelease automatically. **Do not** pin exact versions for local-feed packages.

## Verify freshness

```bash
./scripts/check-deps.sh
```

## If FSI has the old DLL loaded

FSI locks `#r` DLLs — restart it via `mcp__fsi-server__restart_fsi` after the rebuild (see `fsi-fsbar-load` skill).
