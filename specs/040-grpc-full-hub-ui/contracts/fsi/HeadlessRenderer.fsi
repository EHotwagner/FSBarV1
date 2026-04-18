// Sketch for src/FSBar.Hub/HeadlessRenderer.fsi
// Feature 040 — off-screen Viewer-tab render pipeline for the render-frame RPC.

namespace FSBar.Hub

open System
open System.Threading.Channels

type ImageFormat =
    | Png
    | Jpeg

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

type RenderSubscriptionRequest =
    { ClientLabel: string
      TargetHz: int
      Format: ImageFormat
      ViewportWidth: int
      ViewportHeight: int
      JpegQuality: int
      CloseOnSessionEnd: bool
      EmitNoSessionPlaceholder: bool }

type RenderSubscription = {
    Id: Guid
    Channel: ChannelReader<RenderFrameMessage>
    /// Cancel the subscription's worker and free resources.
    Dispose: unit -> unit
}

type SubscribeOutcome =
    | Subscribed of RenderSubscription
    | Rejected of reason: string           // e.g. "max subscribers reached"

module HeadlessRenderer =
    type T

    val create:
        sessions: SessionManager ->
        store:    HubStateStore.T ->
        overlays: OverlayLayerStore.T ->
        settings: HubSettings ->
        T

    /// Subscribe a new client. Spawns a per-subscriber worker; may Reject when
    /// the cap in HubSettings.MaxRenderFrameSubscribers is reached.
    val subscribe: T -> RenderSubscriptionRequest -> SubscribeOutcome

    /// Synchronously render and encode one frame at the caller's requested
    /// viewport + format. Returns the placeholder frame when no session is
    /// active. Used by the GetRenderFrame unary RPC.
    val renderOnce:
        T ->
        format: ImageFormat ->
        viewportWidth: int ->
        viewportHeight: int ->
        jpegQuality: int ->
        RenderFrameMessage
