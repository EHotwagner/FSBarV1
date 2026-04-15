# Loader Error-Case Contract

**Feature**: 026-permanent-map-cache
**Module**: `FSBar.Client.MapCacheFile`
**Primary operation**: `read : SupportedMap -> path: string -> Result<LoadedMap, LoadError>`

This contract enumerates every expected failure mode of the loader with its triggering condition and its observable diagnostic output. Every row corresponds to a test case in `src/FSBar.Client.Tests/MapCacheFileVersionTests.fs` / `MapCacheFileCorruptionTests.fs` (the existing pure-unit-test project, feature-007 convention). Tests and this table MUST stay in sync — if a test changes, the row changes; if a row changes, the test changes.

All error-path diagnostics are produced by `MapCacheFile.formatLoadError`. The test suite asserts both that the returned `LoadError` constructor is the expected one AND that `formatLoadError` produces a string that contains the four anchor tokens noted in the "Message anchors" column (so wording can evolve without rewriting tests).

| # | Trigger | `LoadError` constructor | Message anchors (all must appear) | User story / FR |
|---|---|---|---|---|
| E1 | File does not exist at `path` | `FileMissing path` | `path` · `cache file not found` · `codeVersion` · `refresh-all.sh` | US1 acc-3 / FR-003, FR-006 |
| E2 | File exists but is not valid JSON (truncated, malformed, binary garbage) | `ParseFailure(path, detail)` | `path` · `failed to parse` · `detail` · `refresh-all.sh` | Edge case "corrupted or truncated" / FR-006 |
| E3 | File's `schemaVersion` ≠ `MapCacheFile.schemaVersion` | `SchemaVersionMismatch(path, expected, found)` | `path` · `schemaVersion` · `expected <N>` · `refresh-all.sh` | Edge case "matches schema but different source code" / FR-006 |
| E4 | File's `codeVersion` ≠ `MapCacheFile.codeVersion` | `CodeVersionMismatch(path, expected, found)` | `path` · `codeVersion` · `expected <N>` · `refresh-all.sh` | US2 acc-2 / FR-005, FR-006 |
| E5 | File's `mapName` ≠ `SupportedMap.MapName` | `MapNameMismatch(path, expected, found)` | `path` · `mapName` · `expected <name>` · `refresh-all.sh` | Edge case "wrong file at wrong path" / FR-006 |
| E6 | File's `baseCentre` or `chokepointQuery` snapshot ≠ current `SupportedMap` values | `ParametersMismatch(path, detail)` | `path` · `parameters changed` · `detail` · `refresh-all.sh` | Edge case "semantics changed without bump" / FR-005, FR-006 |
| E7 | Blob field declares `(rows × cols × 4) ≠ decompressed byte count` | `BlobCorrupted(path, field, "size mismatch")` | `path` · `field` · `size mismatch` · `refresh-all.sh` | Edge case "corrupted or truncated" / FR-006 |
| E8 | Blob's gzip payload fails to decompress | `BlobCorrupted(path, field, "gzip decode failure: …")` | `path` · `field` · `gzip decode failure` · `refresh-all.sh` | Edge case "corrupted or truncated" / FR-006 |

> **Note**: Unsupported-map lookup (a caller asking for a `SupportedMap` name that is not in `MapCacheFile.supportedMaps`) is intentionally **not** a `LoadError`. It is the caller's responsibility to handle `tryFindSupportedMap : mapName -> SupportedMap option`, preserving the FR-010 fallback path for unsupported maps. `read` only runs when a valid `SupportedMap` already exists — by construction it cannot see an unsupported map.

## Non-error cases (positive acceptance)

| # | Trigger | Result | User story |
|---|---|---|---|
| P1 | All fields match and all blobs decompress cleanly | `Ok LoadedMap` with `Grid.HeightMap`, `Grid.SlopeMap`, `Grid.ResourceMap` equal to the originals used at `write` time, `Chokepoints` in the same order as `findChokepoints` produced, and `LosMap`/`RadarMap` zero-initialized to the correct shape. | US1 acc-1, acc-3 |
| P2 | `write` followed immediately by `read` on a temp path round-trips the `(MapGrid, Chokepoints)` pair exactly. | Byte-for-byte equal on the blobs; structural equality on the chokepoint list. | US2 acc-3 (determinism precondition) |
| P3 | `write` called twice with identical inputs to two temp paths produces byte-identical files. | `File.ReadAllBytes path1 = File.ReadAllBytes path2`. | SC-004 |

## Behavioral invariants

1. **No caching inside the loader itself.** Every `read` invocation does the full validation pipeline. (Session-level memoisation is the caller's concern; today that's the trainer bot, and this feature does not change its memoisation behavior.)
2. **No network / no engine calls.** The entire load path is filesystem → JSON → blob decompression → `Array2D` reshaping. Zero dependencies on a running BAR engine.
3. **Loader never falls back.** If validation fails at any step, the loader returns `Error`. It does not attempt to rebuild from `.sd7`, does not zero-out a malformed blob, does not return a partially-loaded `LoadedMap`. FR-006 is the normative statement of this rule.
4. **`formatLoadError` is exhaustive.** The function handles every `LoadError` constructor; removing a constructor without updating the formatter is a compile error.
5. **`supportedMaps` is the single source of truth.** The trainer warmup and the refresh script consult `supportedMaps` — they do not maintain their own lists. FR-008 is the normative statement.
