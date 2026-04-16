namespace FSBar.Viz

/// Describes the type of input control rendered for an attribute.
[<RequireQualifiedAccess>]
type InputKind =
    | ColorPicker
    | Slider of min: float32 * max: float32
    | IntSlider of min: int * max: int
    | Toggle
    | EnumChoice of labels: string list

/// Categories for grouping attributes in the configurator panel.
[<RequireQualifiedAccess>]
type AttributeCategory =
    | Colors
    | Sizes
    | Strokes
    | Overlays
    | HealthDamage
    | Effects

/// A single configurable visual attribute with metadata for panel rendering.
type AttributeDescriptor =
    { Key: string
      Label: string
      Category: AttributeCategory
      InputKind: InputKind
      Get: VizConfig -> obj
      Set: obj -> VizConfig -> VizConfig
      Default: obj
      Range: (float32 * float32) option }

/// Static attribute descriptor registry — the single source of truth for
/// what's configurable in the Viz style configurator.
module ConfigDescriptors =
    /// All configurable attributes, ordered by category then display order.
    val all: AttributeDescriptor list

    /// Display name for a category.
    val categoryLabel: AttributeCategory -> string

    /// Category order for panel rendering.
    val categoryOrder: AttributeCategory list

    /// Lookup a descriptor by key. Returns None if key is unknown.
    val tryFind: key: string -> AttributeDescriptor option

    /// Apply a map of key→value pairs to a VizConfig, returning updated config.
    /// Unknown keys are silently skipped.
    val applyValues: values: Map<string, obj> -> config: VizConfig -> VizConfig

    /// Extract all current values from a VizConfig as a key→value map.
    val extractValues: config: VizConfig -> Map<string, obj>

    /// Check whether two configs differ on any descriptor.
    val isDirty: current: VizConfig -> reference: VizConfig -> bool
