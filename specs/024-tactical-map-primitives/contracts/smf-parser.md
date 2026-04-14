# Contract: `FSBar.Client.SmfParser`

**FR link**: FR-024, FR-025, FR-026, FR-027, FR-028
**Tier**: 1 — compiled module, curated `.fsi`, surface-area baseline required.
**Spec source**: [../spec.md](../spec.md) US "SMF parser" block.

## Purpose

Parse a BAR `.sd7` archive from the local BAR installation (e.g. `~/.local/state/Beyond All Reason/maps/avalanche_3.4.sd7`) and return a fully-populated `SmfMap` value that downstream primitives can consume without a running engine.

## Public API surface (`SmfParser.fsi`)

```fsharp
namespace FSBar.Client

/// A parsed Spring Map File — see data-model.md §1 for full record shape.
type SmfMap = {
    WidthHeightmap: int
    HeightHeightmap: int
    WidthElmos: int
    HeightElmos: int
    HeightMap: float32[,]
    SlopeMap: float32[,]
    MetalMap: uint8[,]
    TypeMap: uint8[,]
    SourceArchive: string
}

/// Errors surfaced by the parser. Callers pattern-match for diagnostics.
type SmfParseError =
    /// The .sd7 file is missing from the filesystem.
    | ArchiveNotFound of path:string
    /// bsdtar (or equivalent extractor) failed. stderr attached.
    | ExtractionFailed of archive:string * stderr:string
    /// The .sd7 extracted successfully but contained no .smf entry.
    | NoSmfInArchive of archive:string
    /// The .smf bytes did not start with the SMF magic header.
    | InvalidMagic of actualHex:string
    /// The .smf format version is newer than the parser supports.
    | UnsupportedVersion of version:int
    /// A size-field in the header pointed outside the byte buffer.
    | Truncated of atOffset:int * expectedBytes:int * availableBytes:int

/// Functions for loading Spring Map Files from disk.
module SmfParser =

    /// Parse a `.sd7` archive, extracting the embedded `.smf` via
    /// `bsdtar` and decoding the resulting bytes into an `SmfMap`.
    /// Returns `Error` with a descriptive payload on any failure.
    /// Thread-safe: each call uses its own temp directory and cleans up
    /// afterward.
    val parseSd7 : sd7Path:string -> Result<SmfMap, SmfParseError>

    /// Parse raw SMF bytes directly (no 7-zip extraction step). Used by
    /// unit tests that want to inject a minimal synthetic SMF payload
    /// without going through the filesystem.
    val parseBytes : sourceName:string -> bytes:byte[] -> Result<SmfMap, SmfParseError>

    /// Convert an `SmfMap` to a `MapGrid` value for consumption by
    /// Pathing / Chokepoints / WallIn / BasePlan. LOS and radar
    /// layers are zero-initialised (SMF is a static snapshot).
    val toMapGrid : smf:SmfMap -> MapGrid

    /// Return the list of `.sd7` archives currently installed at the
    /// standard BAR path (`~/.local/state/Beyond All Reason/maps/`).
    /// Returns an empty list if BAR is not installed — callers decide
    /// whether that's an error or a skip-tests signal.
    val listInstalledMaps : unit -> string list
```

## Semantics

### `parseSd7`

1. Verify `sd7Path` exists. Return `Error ArchiveNotFound` if not.
2. Create a unique temp directory.
3. Run `bsdtar -xf <sd7Path> -C <tempDir> 'maps/*.smf'` via `Process.Start`.
4. If exit code ≠ 0, return `Error (ExtractionFailed (archive, stderr))`.
5. Search `<tempDir>` recursively for a `*.smf` file. If none, return `Error (NoSmfInArchive archive)`.
6. Read the `.smf` bytes into memory.
7. Delete `<tempDir>` (best-effort, swallow errors — the OS cleans up temp anyway).
8. Delegate to `parseBytes` with the original `sd7Path` as `sourceName`.
9. On success, the returned `SmfMap.SourceArchive` is set to the absolute `sd7Path`.

### `parseBytes`

1. Verify the first 16 bytes match the SMF magic `"spring map file"` + null terminator. Return `Error (InvalidMagic ...)` otherwise.
2. Read the SMF header (version, map width/height, heightmap offset, metal map offset, type map offset).
3. Version ≠ 1 → `Error (UnsupportedVersion v)`.
4. Any header offset + expected size exceeds `bytes.Length` → `Error (Truncated ...)`.
5. Decode the heightmap: int16 array at `heightMapOffset`, length `(widthHeightmap+1) × (heightHeightmap+1)`, reshaped to a `float32[,]` with the Spring formula (int16 value → world-space height).
6. Decode the metal map: uint8 array at `metalMapOffset`, length `(widthHeightmap/2) × (heightHeightmap/2)`, reshaped to `uint8[,]`.
7. Decode the type map identically at `typeMapOffset`.
8. Compute the slope map locally via the formula in research R3. Output shape `(widthHeightmap/2) × (heightHeightmap/2)` float32.
9. Assemble the `SmfMap` record. Return `Ok`.

### `toMapGrid`

Copies the SMF layers into a `MapGrid` record (existing type from `MapGrid.fsi`). Zero-initialises `LosMap` and `RadarMap` (neither is present in an offline SMF). `ResourceMap` = `MetalMap` converted to `int[,]` (existing MapGrid field type).

### `listInstalledMaps`

Looks at `Environment.ExpandEnvironmentVariables("%HOME%/.local/state/Beyond All Reason/maps")` (or the Linux equivalent), filters for `*.sd7`, returns absolute paths. Never throws — returns `[]` if the directory is missing.

## Error paths

All failure modes return `Error` variants; none throw exceptions. The single exception is unrecoverable infrastructure failure (out-of-memory during byte decoding, system `Directory.CreateDirectory` failure for temp dir) which bubbles as a standard `.NET` exception — these are operator-environment problems, not contract failures.

## Test strategy

**Unit tests** (`SmfParserTests.fs`, xUnit, no filesystem dependencies):

- `parseBytes` with a hand-crafted minimal valid SMF blob (8×8 map) → `Ok SmfMap` with correct dimensions and heightmap values.
- `parseBytes` with bad magic → `Error InvalidMagic`.
- `parseBytes` with version=2 → `Error UnsupportedVersion`.
- `parseBytes` with truncated heightmap → `Error Truncated`.
- `toMapGrid` round-trips an SmfMap through to a MapGrid with matching dimensions.

**Integration tests** (skipped if BAR not installed):

- `parseSd7` on `~/.local/state/Beyond All Reason/maps/avalanche_3.4.sd7` → `Ok SmfMap` with dimensions matching a live-engine `getMapWidth`/`getMapHeight` call (512×512).
- SC-010: heightmap min/max values within ±1 elmo of a live-engine `getCornersHeightMap` snapshot for the same map.
- `listInstalledMaps` returns at least the three maps confirmed in the Q2 investigation.

## Surface-area baseline

`tests/FSBar.Client.Tests/Baselines/SmfParser.baseline` is generated from the `.fsi` on first pass and thereafter validated by the existing baseline test harness. Any addition to the public surface fails the test until the baseline is explicitly updated.
