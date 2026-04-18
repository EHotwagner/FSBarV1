namespace FSBar.Hub

open System

/// 2D point in either world or screen coordinates (disambiguated by the
/// owning `OverlayPrimitive`'s `CoordinateSpace`).
type OverlayPoint = { X: float32; Y: float32 }

/// Coordinate space for an overlay primitive. `World` is transformed by
/// the current `ViewerCamera`; `Screen` is viewport pixels and bypasses
/// the camera.
type CoordinateSpace =
    | World
    | Screen

/// Horizontal alignment for a `Text` primitive.
type TextAlign =
    | Left
    | Center
    | Right

/// One verb in a path primitive. `MoveTo` MUST be the first verb in any
/// path; downstream validation (US6 T060) enforces this.
type PathVerb =
    | MoveTo of OverlayPoint
    | LineTo of OverlayPoint
    | CubicTo of c1: OverlayPoint * c2: OverlayPoint * p: OverlayPoint
    | Close

/// Stroke + fill + opacity description shared by every overlay primitive.
type OverlayStyle =
    { StrokeColorRgba: uint32
      StrokeWidth: float32
      FillColorRgba: uint32 option
      Opacity: float32
      Dash: float32 array option }

/// Declarative overlay primitive. One case per supported Skia draw call.
type OverlayPrimitive =
    | Line of ``from``: OverlayPoint * ``to``: OverlayPoint * style: OverlayStyle * space: CoordinateSpace
    | Polyline of points: OverlayPoint list * style: OverlayStyle * space: CoordinateSpace
    | Polygon of points: OverlayPoint list * style: OverlayStyle * space: CoordinateSpace
    | Rectangle of
        x: float32 *
        y: float32 *
        w: float32 *
        h: float32 *
        cornerRadius: float32 *
        style: OverlayStyle *
        space: CoordinateSpace
    | Circle of
        center: OverlayPoint *
        radius: float32 *
        style: OverlayStyle *
        space: CoordinateSpace
    | Path of
        verbs: PathVerb list *
        style: OverlayStyle *
        space: CoordinateSpace
    | Text of
        anchor: OverlayPoint *
        text: string *
        fontSize: float32 *
        fontFamily: string *
        align: TextAlign *
        style: OverlayStyle *
        space: CoordinateSpace
    | Image of
        anchor: OverlayPoint *
        width: int *
        height: int *
        bytes: byte array *
        space: CoordinateSpace

/// A named layer owned by one scripting client. Layers are atomic: a
/// `PutLayer` with an existing name replaces the whole primitive list.
type OverlayLayer =
    { Name: string
      ZHint: int
      UploadedAtUnixMs: int64
      Primitives: OverlayPrimitive list }

/// Summary of an overlay layer — everything but the primitive payload.
/// Returned by `listLayers` and the `ListLayers` RPC.
type OverlayLayerDescriptor =
    { Name: string
      ZHint: int
      UploadedAtUnixMs: int64
      PrimitiveCount: int }

/// Which cap in FR-026 was exceeded. Mapped to the wire
/// `PutLayerResponse.exceeded_cap` string in `ScriptingHub`.
type CapKind =
    | LayersPerClient
    | PrimitivesPerLayer
    | BytesPerPush
    | ImageBytes
    | ImageDimensions

/// Typed error from `putLayer`. Validation failures and cap violations
/// produce distinct gRPC status codes upstream.
type PutLayerError =
    | InvalidName of reason: string
    | ValidationFailed of errors: string list
    | CapExceeded of cap: CapKind

/// Immutable per-frame projection used by `HeadlessRenderer` — entries
/// are pre-sorted by `(ownerId, zHint ascending, uploadedAt ascending)`
/// so the renderer can composite without touching the store state.
type OverlayLayerSnapshot =
    { Entries: (Guid * OverlayLayer) array }

/// Per-client, name-keyed overlay layer store (feature 040 US6).
///
/// Phase 2 ships a skeleton — no validation, no cap enforcement — so
/// downstream wiring can take shape. US6 task T060 adds the FR-026 cap
/// matrix + primitive validation; US6 T061 wires the disconnect-cleanup
/// subscription.
///
/// Every mutator is keyed on the caller's `clientId`. Clients cannot
/// read or mutate each other's layers; a client disconnect drops all
/// of its layers atomically (wired via `wireDisconnectCleanup`).
module OverlayLayerStore =

    type T

    val create: events: HubEvents.IHubEventSink -> T

    /// Subscribe the store to `HubEvent.ScriptingClientDetached` on the
    /// supplied event source so client disconnect drops every layer they
    /// own in a single atomic store operation. Idempotent — safe to call
    /// multiple times (later calls no-op).
    val wireDisconnectCleanup: T -> System.IObservable<HubEvents.HubEvent> -> unit

    /// Atomically create or replace a named layer owned by `clientId`.
    /// Phase 2 skeleton: every call succeeds (no validation, no caps).
    /// US6 T060 adds the validation and cap checks.
    val putLayer:
        T -> clientId: Guid -> layer: OverlayLayer -> Result<unit, PutLayerError>

    /// Delete the layer called `layerName` owned by `clientId`.
    /// Idempotent: returns `Sent` even when the name isn't present.
    val deleteLayer:
        T -> clientId: Guid -> layerName: string -> SubmitOutcome

    /// List every layer owned by `clientId`. Returns an empty list when
    /// the client has never put a layer (no distinction between absent
    /// and empty).
    val listLayers: T -> clientId: Guid -> OverlayLayerDescriptor list

    /// Drop every layer owned by `clientId`; returns the number dropped.
    val clearLayers: T -> clientId: Guid -> int

    /// Remove every layer owned by `clientId`. Called via
    /// `wireDisconnectCleanup`; exposed here for tests.
    val removeClient: T -> clientId: Guid -> unit

    /// O(total-layers) copy of every active layer across every client,
    /// pre-sorted for the `HeadlessRenderer` composite pass.
    val snapshot: T -> OverlayLayerSnapshot
