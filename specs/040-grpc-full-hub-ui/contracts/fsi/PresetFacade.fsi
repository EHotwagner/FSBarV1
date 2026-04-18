// Sketch for src/FSBar.Hub/PresetFacade.fsi
// Feature 040 — hub-side wrapper over FSBar.Viz.StylePreset that adds name
// validation and HubEventBus emission, plus invalidates HubStateStore's cache.

namespace FSBar.Hub

open FSBar.Viz

type PresetError =
    | InvalidName of reason: string
    | NotFound of name: string
    | IoError of message: string

type PresetDescriptor =
    { Name: string
      ModifiedAtUnixMs: int64 }

module PresetFacade =
    type T

    val create:
        store: HubStateStore.T ->
        events: HubEvents.IHubEventSink ->
        presetDirectory: string ->
        T

    val listNames: T -> Result<PresetDescriptor list, PresetError>
    val save:      T -> name: string -> VizConfig -> Result<unit, PresetError>
    val load:      T -> name: string -> Result<VizConfig, PresetError>
    val delete:    T -> name: string -> Result<unit, PresetError>

    /// Reject names that are empty, contain path separators, traverse (..),
    /// exceed 64 UTF-8 code points, or contain control characters.
    val validateName: name: string -> Result<unit, PresetError>
