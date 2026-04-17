namespace FSBar.Client

/// Parses the engine-generated `ArchiveCache20.lua` to recover the
/// engine-registered map name for each installed `.sd7` map archive.
///
/// The engine does NOT accept archive filename stems as the `MapName`
/// in start scripts — it requires the map's *declared* name (e.g.
/// "Avalanche 3.4"), which is typically `name_pure + " " + version`
/// but can include bespoke formatting inside `mapinfo.lua`. The
/// engine's archive cache is the authoritative source for this
/// string; this module mirrors the exact value the engine will match
/// against at start time.
module ArchiveCache =

    /// A single map archive entry as the engine registered it.
    type MapEntry = {
        /// Archive filename relative to `<dataDir>/maps/`,
        /// e.g. `avalanche_3.4.sd7`.
        ArchiveFileName: string
        /// Filename without the `.sd7` extension, used to match rows
        /// returned by a plain filesystem scan of `<dataDir>/maps/`.
        FileStem: string
        /// The `archivedata.name` string the engine expects in the
        /// `MapName=` field of a start script (e.g. "Avalanche 3.4").
        EngineName: string
        /// The `archivedata.name_pure` string — the raw `name`
        /// field from `mapinfo.lua`.
        NamePure: string
        /// The `archivedata.version` string, when present.
        Version: string option
    }

    /// Default cache path relative to a BAR data dir —
    /// `<dataDir>/cache/ArchiveCache20.lua`.
    val defaultCachePath: dataDir: string -> string

    /// Reads and parses the archive cache. Returns `[]` when the file
    /// does not exist or cannot be read. Emits only map entries
    /// (`modtype = 3`).
    val loadMaps: cachePath: string -> MapEntry list

    /// Convenience: `loadMaps` against the default cache path for the
    /// given data dir.
    val loadMapsForDataDir: dataDir: string -> MapEntry list

    /// Pure parser exposed for unit tests. Accepts the raw Lua source
    /// of `ArchiveCache20.lua` and returns the map entries.
    val parse: content: string -> MapEntry list
