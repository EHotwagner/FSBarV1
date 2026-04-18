namespace FSBar.Hub

open System
open System.Threading.Channels

/// Encoded-image format the render-frame pipeline produces.
type ImageFormat =
    | Png
    | Jpeg

/// One rendered + encoded frame of the Viewer tab as image bytes.
/// `RenderedAtUnixMs` stamps the instant the scene was rasterized;
/// `EncodedAtUnixMs` stamps the instant the encode completed.
/// Both are populated by `HeadlessRenderer` on the server side and
/// forwarded in the gRPC `RenderFrameMessage` envelope.
type RenderFrameMessage =
    { ImageBytes: byte[]
      Format: ImageFormat
      RenderedAtUnixMs: int64
      EncodedAtUnixMs: int64
      ClientSequence: uint64
      ViewportWidth: int
      ViewportHeight: int
      Quality: int
      IsPlaceholder: bool }

/// Caller-visible subscription parameters.
type RenderSubscriptionRequest =
    { ClientLabel: string
      TargetHz: int
      Format: ImageFormat
      ViewportWidth: int
      ViewportHeight: int
      JpegQuality: int
      CloseOnSessionEnd: bool
      EmitNoSessionPlaceholder: bool }

/// One active subscription's reader handle. The worker writes every
/// rendered+encoded frame into `Channel`; the caller reads and forwards
/// to its gRPC stream. `Dispose` stops the worker and closes the channel.
type RenderSubscription = {
    Id: Guid
    Channel: ChannelReader<RenderFrameMessage>
    Dispose: unit -> unit
}

/// Outcome of a `subscribe` call.
type SubscribeOutcome =
    | Subscribed of RenderSubscription
    | SubscribeRejected of reason: string

/// Off-screen render pipeline for the Viewer tab.
///
/// Reads the current `SessionManager` game state + `HubStateStore` camera
/// + `HubStateStore.VizConfig` every tick, calls
/// `SceneBuilder.buildSceneHeadlessView`, rasterizes into an off-screen
/// SKSurface (raster backend — the GPU backend segfaults in this
/// environment per CLAUDE.md), encodes PNG/JPEG, and fans out to
/// per-subscriber bounded channels (capacity 16, DropOldest).
module HeadlessRenderer =
    type T

    /// Allocate a new renderer. Retains the session manager, state
    /// store, and overlay store (the latter queried each frame for
    /// US6 layer composition; a skeleton `OverlayLayerStore` produces
    /// no primitives, so US2 renders base-scene only).
    val create:
        sessions: SessionManager.SessionManager ->
        store:    HubStateStore.T ->
        overlays: OverlayLayerStore.T ->
        settings: (unit -> HubSettings.HubSettings) ->
            T

    /// Subscribe a new client. Returns `Subscribed` with a
    /// `RenderSubscription` whose `Channel` receives one
    /// `RenderFrameMessage` per render tick. When
    /// `HubSettings.MaxRenderFrameSubscribers` is reached returns
    /// `SubscribeRejected "max subscribers reached"`.
    val subscribe: T -> RenderSubscriptionRequest -> SubscribeOutcome

    /// Synchronously render + encode a single frame at the caller's
    /// requested viewport. Used by the `GetRenderFrame` unary RPC.
    /// When no session is active, returns a placeholder frame with
    /// `IsPlaceholder = true`.
    val renderOnce:
        T ->
        format: ImageFormat ->
        viewportWidth: int ->
        viewportHeight: int ->
        jpegQuality: int ->
            RenderFrameMessage

    /// Current active-subscription count. Exposed for tests + diagnostics.
    val subscriberCount: T -> int
