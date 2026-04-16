namespace FSBar.Viz

/// A serialized attribute value for preset storage.
[<RequireQualifiedAccess>]
type PresetValue =
    | ColorVal of argb: uint32
    | FloatVal of v: float32
    | IntVal of v: int
    | BoolVal of v: bool
    | StringVal of v: string
    | StringSetVal of vs: Set<string>

/// A named collection of visual attribute values.
type StylePreset =
    { Name: string
      CreatedAt: System.DateTimeOffset
      Values: Map<string, PresetValue> }

/// Preset file I/O (JSON format).
module StylePreset =
    /// Directory where presets are stored (`<repo-root>/viz-presets/`).
    val presetDirectory: string

    /// Returns true if `name` contains only filesystem-safe characters
    /// (alphanumeric, spaces, hyphens, underscores) and is non-empty.
    val isValidName: name: string -> bool

    /// Save a preset to disk. Overwrites if a preset with the same name exists.
    /// Returns Ok with the full file path, or Error with a descriptive message.
    val save: preset: StylePreset -> Result<string, string>

    /// Load a preset by name. Returns Error if file missing or malformed.
    val load: name: string -> Result<StylePreset, string>

    /// List all available preset names (derived from filenames in preset directory).
    val listNames: unit -> string list

    /// Delete a preset by name. Returns Error if file does not exist.
    val delete: name: string -> Result<unit, string>

    /// Convert current VizConfig to a StylePreset with the given name.
    val fromConfig: name: string -> config: VizConfig -> StylePreset

    /// Apply a StylePreset to a VizConfig, returning updated config.
    /// Unknown keys are silently skipped; missing keys retain current values.
    val applyToConfig: preset: StylePreset -> config: VizConfig -> VizConfig
