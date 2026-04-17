namespace FSBar.Hub.App.Tabs

open SkiaViewer
open FSBar.Hub

/// gRPC tab (feature 035-central-gui-hub T065) — shows the scripting
/// service's bind address + live connected-client roster. Read-only
/// pane; no input handling beyond the caller routing clicks.
module GrpcTab =

    /// Render the tab content.
    ///
    /// When `service = None` the tab surfaces a banner explaining why
    /// the scripting service is not running (most common: BAR install
    /// detection failed so there's no `SessionManager` to wire it
    /// to). When `service = Some s`, the tab draws:
    ///   * "Endpoint: http://127.0.0.1:<port>" heading
    ///   * Overflow-detach counter + roster table.
    val render:
        service: ScriptingHub.ScriptingService option ->
        endpointUrl: string ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            Element list
