# Quickstart: 006-validate-highbar-fixes

**Date**: 2026-04-06

## What This Feature Does

Fixes the map grid dimension mismatch that causes 11/12 map integration tests to fail by:
1. Switching from center heightmap (callback 52) to corners heightmap (callback 59)
2. Fixing slope map dimensions from `(w+1)*(h+1)` to `(w/2)*(h/2)`
3. Updating `MapQuery.slopeAtElmo` to use correct slope-map coordinates

## Prerequisites

- HighBar V2 proxy built from commit 026+ (supports `CALLBACK_MAP_GET_CORNERS_HEIGHT_MAP`)
- `spring-headless` binary and BAR game data installed
- Run `tests/check-prerequisites.sh` to verify

## Files Changed

| File | Change |
|------|--------|
| `proto/highbar/callbacks.proto` | Add `CALLBACK_MAP_GET_CORNERS_HEIGHT_MAP = 59` |
| `src/FSBar.Client/Callbacks.fs` | Add `getCornersHeightMap` function |
| `src/FSBar.Client/Callbacks.fsi` | Add `getCornersHeightMap` signature |
| `src/FSBar.Client/MapGrid.fs` | Use corners heightmap; fix slope dimensions |
| `src/FSBar.Client/MapQuery.fs` | Fix `slopeAtElmo` coordinate mapping |
| `src/FSBar.Client/MapQuery.fsi` | No signature changes (behavioral fix only) |
| `tests/FSBar.LiveTests/MapGridTests.fs` | Update dimension assertions for slope map |
| `tests/FSBar.LiveTests/MapQueryTests.fs` | Adjust slope-related test expectations |

## How to Verify

```bash
# Build
dotnet build

# Run unit tests
dotnet test src/FSBar.Client.Tests/

# Run integration tests (requires live engine)
dotnet test tests/FSBar.LiveTests/ --filter "Category=MapGrid|Category=MapQuery"
```

Expected: All 12 map tests pass (or skip if proxy unavailable).
