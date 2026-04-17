namespace FSBar.Client

/// <summary>
/// A parsed Spring Map File (SMF). Structurally compatible with
/// <see cref="T:FSBar.Client.MapGrid"/> so downstream primitives can consume either
/// source without branching.
/// </summary>
type SmfMap =
    { /// <summary>Map dimensions in heightmap squares (1 square = 8 elmos).</summary>
      WidthHeightmap: int
      /// <summary>Map dimensions in heightmap squares (1 square = 8 elmos).</summary>
      HeightHeightmap: int
      /// <summary>World-space dimensions in elmos (= heightmap × 8).</summary>
      WidthElmos: int
      /// <summary>World-space dimensions in elmos (= heightmap × 8).</summary>
      HeightElmos: int
      /// <summary>Corners heightmap: (WidthHeightmap+1) × (HeightHeightmap+1) float32 world heights.</summary>
      HeightMap: float32[,]
      /// <summary>Slope map at half heightmap resolution: (WidthHeightmap/2) × (HeightHeightmap/2).</summary>
      SlopeMap: float32[,]
      /// <summary>Metal map (raw SMF byte values, 0-255) at half heightmap resolution.</summary>
      MetalMap: uint8[,]
      /// <summary>Type map (terrain type indices, 0-255) at half heightmap resolution.</summary>
      TypeMap: uint8[,]
      /// <summary>Absolute path to the .sd7 archive the map was parsed from (diagnostics only).</summary>
      SourceArchive: string }

/// <summary>Errors surfaced by the SMF parser. Callers pattern-match for diagnostics.</summary>
type SmfParseError =
    /// <summary>The .sd7 file is missing from the filesystem.</summary>
    | ArchiveNotFound of path: string
    /// <summary>bsdtar (or equivalent extractor) failed. stderr is attached.</summary>
    | ExtractionFailed of archive: string * stderr: string
    /// <summary>The .sd7 extracted successfully but contained no .smf entry.</summary>
    | NoSmfInArchive of archive: string
    /// <summary>The .smf bytes did not start with the SMF magic header.</summary>
    | InvalidMagic of actualHex: string
    /// <summary>The .smf format version is newer than the parser supports.</summary>
    | UnsupportedVersion of version: int
    /// <summary>A size-field in the header pointed outside the byte buffer.</summary>
    | Truncated of atOffset: int * expectedBytes: int * availableBytes: int

/// <summary>
/// Parses BAR Spring Map Files (<c>.sd7</c> / <c>.smf</c>) from disk into an
/// <see cref="T:FSBar.Client.SmfMap"/> value that downstream primitives can consume without
/// a running engine.
/// </summary>
module SmfParser =

    /// <summary>
    /// Parse a <c>.sd7</c> archive by extracting its embedded <c>.smf</c> via <c>bsdtar</c>
    /// and decoding the resulting bytes. Thread-safe: each call uses its own temp directory
    /// and cleans up afterwards.
    /// </summary>
    /// <param name="sd7Path">Absolute path to the <c>.sd7</c> archive.</param>
    /// <returns><c>Ok SmfMap</c> on success, or <c>Error</c> with a descriptive payload.</returns>
    val parseSd7: sd7Path: string -> Result<SmfMap, SmfParseError>

    /// <summary>
    /// Parse raw SMF bytes directly (no 7-zip extraction step). Used by unit tests that
    /// inject a minimal synthetic SMF payload without going through the filesystem.
    /// </summary>
    /// <param name="sourceName">Label recorded in <c>SourceArchive</c> for diagnostics.</param>
    /// <param name="bytes">Raw SMF byte buffer.</param>
    val parseBytes: sourceName: string -> bytes: byte[] -> Result<SmfMap, SmfParseError>

    /// <summary>
    /// Convert an <see cref="T:FSBar.Client.SmfMap"/> to a <see cref="T:FSBar.Client.MapGrid"/>
    /// for consumption by Pathing / Chokepoints / WallIn / BasePlan. LOS and radar layers are
    /// zero-initialised (an offline SMF has no runtime visibility data).
    /// </summary>
    val toMapGrid: smf: SmfMap -> MapGrid

    /// <summary>
    /// Return absolute paths to every <c>.sd7</c> archive currently installed at the
    /// standard BAR path (<c>~/.local/state/Beyond All Reason/maps/</c>). Returns an empty
    /// list when BAR is not installed — callers decide whether that is an error or a skip
    /// signal.
    /// </summary>
    val listInstalledMaps: unit -> string list
