// Sketch for src/FSBar.Hub/OverlayLayerStore.fsi
// Feature 040 US6 — per-client, name-keyed overlay layers. Renders on top of
// every built-in overlay (FR-024, FR-027). Auto-cleaned on client disconnect
// via ScriptingClientDetached (FR-025, SC-010). Caps enforced per FR-026.

namespace FSBar.Hub

open System

type Point = { X: float32; Y: float32 }

type CoordinateSpace =
    | World
    | Screen

type TextAlign =
    | Left
    | Center
    | Right

type PathVerb =
    | MoveTo of Point
    | LineTo of Point
    | CubicTo of c1: Point * c2: Point * p: Point
    | Close

type OverlayStyle =
    { StrokeColorRgba: uint32
      StrokeWidth: float32
      FillColorRgba: uint32 option
      Opacity: float32
      Dash: float32[] option }

type OverlayPrimitive =
    | Line of from: Point * ``to``: Point * style: OverlayStyle * space: CoordinateSpace
    | Polyline of points: Point list * style: OverlayStyle * space: CoordinateSpace
    | Polygon of points: Point list * style: OverlayStyle * space: CoordinateSpace
    | Rectangle of
        x: float32 *
        y: float32 *
        w: float32 *
        h: float32 *
        cornerRadius: float32 *
        style: OverlayStyle *
        space: CoordinateSpace
    | Circle of
        center: Point *
        radius: float32 *
        style: OverlayStyle *
        space: CoordinateSpace
    | Path of
        verbs: PathVerb list *
        style: OverlayStyle *
        space: CoordinateSpace
    | Text of
        anchor: Point *
        text: string *
        fontSize: float32 *
        fontFamily: string *
        align: TextAlign *
        style: OverlayStyle *
        space: CoordinateSpace
    | Image of
        anchor: Point *
        width: int *
        height: int *
        bytes: byte[] *
        space: CoordinateSpace

type OverlayLayer =
    { Name: string
      ZHint: int
      UploadedAtUnixMs: int64
      Primitives: OverlayPrimitive list }

type OverlayLayerDescriptor =
    { Name: string
      ZHint: int
      UploadedAtUnixMs: int64
      PrimitiveCount: int }

type CapKind =
    | LayersPerClient
    | PrimitivesPerLayer
    | BytesPerPush
    | ImageBytes
    | ImageDimensions

type PutLayerError =
    | InvalidName of reason: string
    | ValidationFailed of errors: string list
    | CapExceeded of cap: CapKind

type OverlayLayerSnapshot =
    { Entries: (Guid * OverlayLayer)[] }   // pre-sorted by (owner, zHint, uploadedAt)

module OverlayLayerStore =
    type T

    val create:
        events: HubEvents.IHubEventSink ->
        T

    /// Subscribe to ScriptingClientDetached to drop client layers on disconnect.
    val wireDisconnectCleanup: T -> HubEvents.IHubEventSource -> unit

    val putLayer:
        T -> clientId: Guid -> OverlayLayer -> Result<unit, PutLayerError>

    val deleteLayer:
        T -> clientId: Guid -> layerName: string -> HubStateStore.SubmitOutcome

    val listLayers:
        T -> clientId: Guid -> OverlayLayerDescriptor list

    val clearLayers:
        T -> clientId: Guid -> int

    /// Called internally on ScriptingClientDetached (also exposed for tests).
    val removeClient: T -> clientId: Guid -> unit

    /// Immutable per-frame projection for HeadlessRenderer. O(n) copy.
    val snapshot: T -> OverlayLayerSnapshot
