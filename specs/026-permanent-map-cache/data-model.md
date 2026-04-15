# Phase 1 Data Model: Permanent, Committed Map Cache

**Feature**: 026-permanent-map-cache
**Date**: 2026-04-15

This document is the authoritative description of the on-disk and in-memory data shapes introduced by this feature. The types here are the exact declarations that will appear in `src/FSBar.Client/MapCacheFile.fsi`; the data model and the signature file are one-to-one. The data model drives the contracts (contracts/) and the tasks (`/speckit.tasks`).

---

## Constants

### `MapCacheFile.schemaVersion : int`

- **Value**: `2`
- **Semantics**: Identifies the on-disk file format. Changes only when the serialization layout changes (fields added/removed/renamed, nesting restructured, encoding of a blob changed). Comparisons are exact; mismatches are fatal.
- **Versioning rule**: Every code change that breaks compatibility with a previously written cache file bumps this integer by 1.

### `MapCacheFile.codeVersion : int`

- **Initial value**: `1` (first version of the new cache format)
- **Semantics**: Identifies the semantic version of the analysis code that produced the file. A cache file written at `codeVersion = k` is only valid at `codeVersion = k` — not before, not after.
- **Versioning rule**: Bumped by +1 in the same PR as any change to the semantics of `Chokepoints.fs`, `BasePlan.fs`, `WallIn.fs`, `MapGrid.fs`, `SmfParser.fs`, `MapQuery.fs`, or `MapCacheFile.fs` itself. The doc comment on the constant enumerates this set explicitly.
- **Compared against**: Itself — the integer baked into the cache file's `codeVersion` field must equal the integer declared in the currently loaded `MapCacheFile` module, exactly.

---

## Core types

### `SupportedMap`

The declarative per-map inputs that determine what gets analyzed and how. One instance per supported map. Defined once in `MapCacheFile.fs`, exposed via `MapCacheFile.supportedMaps : SupportedMap list`.

```fsharp
type SupportedMap = {
    /// Canonical human-readable map name, e.g. "Avalanche 3.4".
    /// Used as the cache-file key (after sanitisation) and as the bot-side
    /// map-name comparison value.
    MapName: string

    /// Filename stem of the .sd7 archive to read, e.g. "avalanche_3.4".
    /// Combined with the BAR maps directory at refresh time.
    /// Lookup is case-insensitive and tolerant of underscores/spaces;
    /// the sanitisation rules match those used by the trainer bot.
    Sd7FileStem: string

    /// Fixed base-centre elmo coordinate used as the chokepoint search
    /// origin and as the BasePlan resolve context's base centre. Conventionally
    /// the top-left start-area centroid for ladder maps.
    BaseCentre: float32 * float32 * float32

    /// Exact chokepoint query parameters (MoveType, width threshold, search
    /// radius, etc.) used to produce this map's cached chokepoint list.
    /// Recorded verbatim inside the cache file so the loader can reject any
    /// file generated with different parameters.
    ChokepointQuery: Chokepoints.ChokepointQuery
}
```

**Validation rules**:
- `MapName` must be non-empty and must match exactly the `mapName` field of any cache file intended to satisfy this entry.
- `Sd7FileStem` must be resolvable to exactly one `.sd7` file under the BAR maps directory at refresh time; ambiguous matches abort the refresh for that map with a clear error.
- `BaseCentre` coordinates are in elmo space; `y` is unused for the base-plan algorithm today but preserved for future use and for the `(x, y, z)` convention used across `FSBar.Client`.
- `ChokepointQuery` is stored as-is — any change to its defaults or any per-map override is a semantic change that (via the codec re-reading it from the list) will cause a regeneration-parameter mismatch if an old cache is still around.

### `MapCacheFile.Contents`

The top-level record that gets JSON-serialized. Field declaration order is the serialization order (R2 from research); do not reorder.

```fsharp
type Contents = {
    /// Exact integer match against MapCacheFile.schemaVersion. Drives
    /// SchemaVersionMismatch errors on load.
    SchemaVersion: int

    /// Exact integer match against MapCacheFile.codeVersion. Drives
    /// CodeVersionMismatch errors on load.
    CodeVersion: int

    /// Canonical map name, e.g. "Avalanche 3.4". Compared against
    /// SupportedMap.MapName on load. Drives MapNameMismatch errors.
    MapName: string

    /// Elmo dimensions of the map (width * 8 / height * 8 of the heightmap
    /// squares). Recorded for self-consistency checks against the blobs below.
    WidthElmos: int
    HeightElmos: int

    /// Heightmap-square dimensions of the map. Used by the loader to reshape
    /// the flat gzipped blobs into 2D arrays.
    WidthHeightmap: int
    HeightHeightmap: int

    /// Base centre in elmo coordinates (x, y, z). Self-describing; MUST equal
    /// the SupportedMap.BaseCentre for the matching MapName.
    BaseCentre: Vec3

    /// Chokepoint query parameters that produced the chokepoints list below.
    /// Self-describing; MUST equal the SupportedMap.ChokepointQuery for the
    /// matching MapName.
    ChokepointQuery: ChokepointQuerySnapshot

    /// Static chokepoint list for this map, in deterministic
    /// findChokepoints output order.
    Chokepoints: ChokepointEntry list

    /// Gzipped heightmap / slope map / resource map blobs.
    Heightmap: GzipBlob        // float32 values
    SlopeMap: GzipBlob         // float32 values
    ResourceMap: GzipBlob      // int32 values
}
```

There is no `sd7Path` field. There is no `generatedAtUtc` field. Both are intentionally absent (see research R1).

### `Vec3`

```fsharp
type Vec3 = { X: float32; Y: float32; Z: float32 }
```

Serializes as `{"x": ..., "y": ..., "z": ...}`. Used for `BaseCentre` and chokepoint positions.

### `ChokepointQuerySnapshot`

A value-typed mirror of `Chokepoints.ChokepointQuery`. We avoid serializing `Chokepoints.ChokepointQuery` directly because that type is part of a different module's surface and may evolve for reasons unrelated to the cache schema; a snapshot copy is a stable serialization boundary. The loader compares the snapshot equal-wise against the `ChokepointQuery` derived from the current `SupportedMap`; any drift is a hard abort.

```fsharp
type ChokepointQuerySnapshot = {
    MoveType: string                      // MoveType.ToString() — e.g. "Kbot"
    MaxWidthElmos: float32
    SearchRadiusElmos: float32
    // ... every other field of Chokepoints.ChokepointQuery
}
```

### `ChokepointEntry`

A value-typed mirror of `Chokepoints.Chokepoint`. Same rationale as above: stable schema boundary.

```fsharp
type ChokepointEntry = {
    Id: uint32                             // unwrapped from ChokepointId
    Position: Vec3
    WidthElmos: float32
    OutwardDirX: float32                   // OutwardDir is a 2D (x,z) vector; we keep it flat
    OutwardDirZ: float32
    DistanceFromBase: float32
}
```

### `GzipBlob`

```fsharp
type GzipBlob = {
    Rows: int
    Cols: int
    GzipB64: string                        // base64-encoded gzip-compressed little-endian bytes
}
```

Blob byte layout:
- For float32 blobs (heightmap, slope map): `rows * cols * 4` bytes, row-major, each cell encoded as little-endian IEEE 754 single-precision.
- For int32 blobs (resource map): `rows * cols * 4` bytes, row-major, each cell encoded as little-endian two's-complement.

Compression is gzip at `CompressionLevel.Optimal`.

### `LoadError`

The discriminated union returned by `MapCacheFile.read` when loading fails. Each constructor carries enough context for the loader's caller to render a one-line, actionable diagnostic per FR-006.

```fsharp
type LoadError =
    /// The cache file does not exist at the expected path.
    | FileMissing of path: string

    /// The file exists but could not be parsed as JSON or did not match
    /// the expected top-level record shape.
    | ParseFailure of path: string * detail: string

    /// The file's schemaVersion does not match the currently expected value.
    | SchemaVersionMismatch of path: string * expected: int * found: int

    /// The file's codeVersion does not match the currently expected value.
    /// This is the FR-006 "stale cache" case.
    | CodeVersionMismatch of path: string * expected: int * found: int

    /// The file's mapName does not match the caller's expected map.
    | MapNameMismatch of path: string * expected: string * found: string

    /// The file's BaseCentre or ChokepointQuery does not match the
    /// SupportedMap declaration for this map. Indicates the per-map
    /// parameters changed in source after the cache was last baked.
    | ParametersMismatch of path: string * detail: string

    /// A blob's declared (Rows, Cols) does not multiply to the actual
    /// decompressed byte length / 4, or decompression itself failed.
    | BlobCorrupted of path: string * field: string * detail: string
```

All constructors include the cache file path so error messages always identify the offending file. Note that there is **no** `UnsupportedMap` constructor: unsupported-map lookup is the caller's responsibility via `tryFindSupportedMap : _ -> _ option`, which preserves FR-010's fallback path for non-permanent-cache maps. `read` only runs when the caller has already resolved a valid `SupportedMap`, so by construction it never sees an unsupported map.

### `LoadedMap`

The successful return value of `MapCacheFile.read`. Holds the fully materialized `MapGrid` and chokepoint list ready for the trainer warmup to consume.

```fsharp
type LoadedMap = {
    MapName: string
    Grid: MapGrid
    Chokepoints: FSBar.Client.Chokepoints.Chokepoint list
    BaseCentre: float32 * float32 * float32
}
```

Note that `LoadedMap.Grid` has `LosMap` and `RadarMap` initialized to zero arrays of the correct shape. These are dynamic per-frame layers and are refreshed by the existing `FSBar.Client.MapCache.refreshDynamic` callsite during gameplay — the persistent cache file deliberately does not store them.

---

## Operations

### `MapCacheFile.write : SupportedMap -> MapGrid -> Chokepoints.Chokepoint list -> path: string -> unit`

Writes a cache file to the given path for the given supported map, using the provided `MapGrid` and chokepoint list. Must be deterministic: two invocations with identical inputs produce byte-identical output files. Throws on IO failure; does not catch.

### `MapCacheFile.read : SupportedMap -> path: string -> Result<LoadedMap, LoadError>`

Reads a cache file from the given path, validates it against the given supported-map declaration and the module's current `schemaVersion` and `codeVersion`, and returns either the fully materialized `LoadedMap` or a structured `LoadError`. Does not throw for expected failure modes (missing file, corruption, mismatches) — those return `Error`. Exceptions are reserved for truly exceptional conditions (out of memory, disk fault). The trainer warmup translates any `Error` into an exception with a formatted message via a helper below.

### `MapCacheFile.cachePathFor : repoRoot: string -> SupportedMap -> string`

Returns the canonical committed-cache path for a supported map, under `bots/trainer/map-cache/<sanitised-name>.json`. Used by both the trainer warmup and the refresh script to agree on file locations without duplicating the sanitiser.

### `MapCacheFile.formatLoadError : LoadError -> string`

Formats a `LoadError` as a single multi-line human-readable error message that names the file, the mismatch kind, expected vs. found values, and the refresh command. Used by both the trainer warmup and the test suite to keep diagnostic text consistent.

### `MapCacheFile.supportedMaps : SupportedMap list`

The canonical list of supported maps. Adding a map to the permanent cache is exactly one new element in this list plus the committed cache file.

### `MapCacheFile.tryFindSupportedMap : mapName: string -> SupportedMap option`

Convenience lookup by canonical map name. Returns `None` for maps outside the supported set so callers can fall back to pre-existing unsupported-map behavior (FR-010).

---

## Entity relationships

```text
                  ┌──────────────────────┐
                  │    SupportedMap      │  (declared in source)
                  │  - MapName           │
                  │  - Sd7FileStem       │
                  │  - BaseCentre        │
                  │  - ChokepointQuery   │
                  └──────────┬───────────┘
                             │
                             ▼
              ┌────────────────────────────┐
              │      Refresh command       │
              │   reads .sd7 → MapGrid     │
              │   runs findChokepoints      │
              └──────────┬─────────────────┘
                         │
                         ▼
            ┌────────────────────────────────┐
            │     MapCacheFile.Contents      │  (written to disk)
            │  - SchemaVersion               │
            │  - CodeVersion                 │
            │  - MapName (must match)        │
            │  - BaseCentre (must match)     │
            │  - ChokepointQuery (must match)│
            │  - Chokepoints list            │
            │  - Heightmap / Slope / Resource│
            └──────────┬─────────────────────┘
                       │
                       ▼
              ┌─────────────────────┐
              │ MapCacheFile.read   │
              │  validates against  │
              │  SupportedMap +     │
              │  current constants  │
              └──────────┬──────────┘
                         │
               ┌─────────┴─────────┐
               │                   │
               ▼                   ▼
         ┌──────────┐        ┌────────────────┐
         │ LoadedMap│        │   LoadError    │
         │ (MapGrid │        │ (FR-006 abort) │
         │  + CPs)  │        └────────────────┘
         └──────────┘
```

The validation pipeline on `read` enforces, in this order:
1. File exists → else `FileMissing`.
2. JSON parses into `Contents` → else `ParseFailure`.
3. `SchemaVersion` equals `MapCacheFile.schemaVersion` → else `SchemaVersionMismatch`.
4. `CodeVersion` equals `MapCacheFile.codeVersion` → else `CodeVersionMismatch`.
5. `MapName` equals the provided `SupportedMap.MapName` → else `MapNameMismatch`.
6. `BaseCentre` and `ChokepointQuery` match the provided `SupportedMap` → else `ParametersMismatch`.
7. Each blob decompresses, matches its declared `(Rows, Cols)`, and reshapes into the expected `Array2D` shape → else `BlobCorrupted`.

Steps 3 and 4 could be swapped; the chosen order surfaces schema-format issues (likely to affect *every* file simultaneously during a version bump) before codeVersion issues (likely to affect files that a specific contributor forgot to regenerate).

---

## State transitions

None of the entities above are stateful. The cache file is write-once (via `write`) and read-many (via `read`). There is no in-place update path — refreshing a cache is a full rewrite. This is intentional: partial updates would complicate determinism (SC-004) for no benefit.

---

## Out of scope for this feature

- **LOS / radar caching** — dynamic, already handled by the existing in-memory `FSBar.Client.MapCache` module.
- **Pathfinding result caching** — query-dependent, not a static per-map artifact.
- **`FSBar.Client.Pathing` precomputation** — same rationale.
- **Partial / incremental cache updates** — always a full rewrite via the refresh command.
- **Automatic `codeVersion` bump detection** — decided against in the clarify phase.
- **CI enforcement of cache freshness** — decided against in the clarify phase.
