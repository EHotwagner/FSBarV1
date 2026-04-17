namespace FSBar.Client

/// Persistent, file-based cache for static per-map analysis primitives.
///
/// Distinct from <see cref="T:FSBar.Client.MapCache"/> — that module is a
/// thread-safe in-memory session cache for engine-callback round-trips.
/// This module serialises <see cref="T:FSBar.Client.MapGrid"/> plus the
/// chokepoint list to a deterministic JSON file on disk so a fresh clone
/// of the repository can load a precomputed analysis in under 25 ms
/// instead of spending ~250 ms re-parsing the .sd7 at every trainer start.
///
/// Feature: specs/026-permanent-map-cache/
/// Clarifications:
///   - Staleness detection: manual integer codeVersion, exact match.
///   - Mismatch policy: hard abort via LoadError (no fallback).
///   - CI enforcement: out of scope; runtime abort is the backstop.
module MapCacheFile =

    /// The on-disk file format version. Bumped only when the serialisation
    /// layout itself changes (fields added/removed, nesting restructured,
    /// blob encoding altered). Compared exactly on load.
    val schemaVersion: int

    /// The current code version for map-analysis cache compatibility.
    ///
    /// BUMP THIS INTEGER (by +1) in the same PR whenever you change any of:
    ///   - Chokepoints.fs / BasePlan.fs / WallIn.fs — analysis semantics
    ///   - MapGrid.fs / SmfParser.fs / MapQuery.fs — primitive extraction
    ///   - MapCacheFile.fs — the codec itself (write/read/blob format)
    ///
    /// After bumping, run bots/trainer/map-cache/refresh-all.sh and commit
    /// the regenerated cache files in the same PR.
    val codeVersion: int

    /// Declarative per-map inputs that determine what gets analysed and how.
    /// One instance per supported map; the full list is exposed via
    /// <see cref="F:FSBar.Client.MapCacheFile.supportedMaps"/>.
    type SupportedMap = {
        MapName: string
        Sd7FileStem: string
        BaseCentre: float32 * float32 * float32
        ChokepointQuery: ChokepointQuery
    }

    /// The canonical list of supported maps for which a committed cache file
    /// is required. Adding a map to the permanent cache is exactly one new
    /// element in this list plus the committed cache file produced by the
    /// refresh script.
    val supportedMaps: SupportedMap list

    /// Look up a supported map by its canonical name. Returns None for any
    /// map outside the supported set, preserving the unsupported-map
    /// fallback path for non-permanent-cache consumers (FR-010).
    val tryFindSupportedMap: mapName: string -> SupportedMap option

    /// Structured failure mode returned by <see cref="M:FSBar.Client.MapCacheFile.read"/>.
    /// Every constructor carries the file path (when known) plus the
    /// context needed to render an actionable diagnostic.
    type LoadError =
        | FileMissing of path: string
        | ParseFailure of path: string * detail: string
        | SchemaVersionMismatch of path: string * expected: int * found: int
        | CodeVersionMismatch of path: string * expected: int * found: int
        | MapNameMismatch of path: string * expected: string * found: string
        | ParametersMismatch of path: string * detail: string
        | BlobCorrupted of path: string * field: string * detail: string

    /// Successful read result. Contains the fully materialised MapGrid
    /// (with LosMap/RadarMap initialised to zero arrays of the correct
    /// shape — dynamic layers are refreshed per frame by the caller) and
    /// the chokepoint list in findChokepoints output order.
    type LoadedMap = {
        MapName: string
        Grid: MapGrid
        Chokepoints: Chokepoint list
        BaseCentre: float32 * float32 * float32
    }

    /// Deterministic write path. Two invocations with identical inputs
    /// MUST produce byte-identical output files (SC-004).
    ///
    /// Throws on IO failure; does not catch. Does not validate that the
    /// caller's (grid, chokepoints) values are consistent with the
    /// SupportedMap declaration — the caller is expected to produce them
    /// from the same SupportedMap it passes here.
    val write:
        supported: SupportedMap
        -> grid: MapGrid
        -> chokepoints: Chokepoint list
        -> path: string
        -> unit

    /// Validation pipeline and load path. Returns a structured Error for
    /// all expected failure modes — callers map those into hard-abort
    /// exceptions per FR-006 using formatLoadError for the message text.
    /// Does not throw except for truly exceptional IO failures.
    val read:
        supported: SupportedMap
        -> path: string
        -> Result<LoadedMap, LoadError>

    /// Canonical committed-cache path for a supported map. Built as
    /// {repoRoot}/bots/trainer/map-cache/{sanitised-name}.json. Both the
    /// trainer warmup and the refresh script consult this function so the
    /// sanitisation rule lives in exactly one place.
    val cachePathFor: repoRoot: string -> supported: SupportedMap -> string

    /// Format a LoadError as a multi-line human-readable error message.
    /// Always includes: the offending file path, the mismatch kind,
    /// expected vs. found values (where applicable), and the refresh
    /// command the contributor should run next.
    val formatLoadError: error: LoadError -> string
