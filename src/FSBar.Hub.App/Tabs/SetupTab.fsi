namespace FSBar.Hub.App.Tabs

open SkiaSharp
open SkiaViewer
open FSBar.Hub

/// Setup tab: lobby builder that drives `SessionManager.Launch` (feature
/// 035-central-gui-hub T029).
///
/// Phase-3 scope: map picker (scrollable list from `<dataDir>/maps/*.sd7`)
/// + a read-only summary of the currently-selected lobby + a Launch
/// button that surfaces the full `LobbyError` list on failure. Team
/// editing, spectator management, mode switching, and engine-speed
/// controls are deferred — `LobbyConfig.defaults` gives a working
/// HighBarV2 vs BARb / Armada vs Cortex Skirmish lobby that reaches
/// Running against the real engine, proven by the live test suite.
module SetupTab =

    /// Actions the Setup tab surfaces on mouse input. The entrypoint
    /// translates each into a hub-wide state mutation.
    [<RequireQualifiedAccess>]
    type SetupTabAction =
        /// User clicked a map in the list. Payload is the engine-
        /// registered map name recovered from `ArchiveCache20.lua`
        /// (the exact string a start script's `MapName=` must carry);
        /// falls back to the archive filename stem when the cache
        /// has no entry for this archive.
        | SelectMap of mapName: string
        /// User scrolled the map list. Payload is the new scroll
        /// offset clamped to `[0, maxScroll]`.
        | ScrollMapList of offset: float32
        /// User clicked Launch. Caller validates + calls
        /// `SessionManager.Launch`.
        | Launch

    /// One row in the map picker — pairs an on-disk archive stem
    /// with the engine-registered name from `ArchiveCache20.lua`.
    type MapRow = {
        /// Engine-registered map name (e.g. "Avalanche 3.4"). Falls
        /// back to `FileStem` when the cache has no entry — the UI
        /// flags those rows as unresolved.
        EngineName: string
        /// Filename without the `.sd7` extension, used for
        /// disambiguation in the list and as the cache-miss fallback.
        FileStem: string
    }

    /// Internal render state.
    type SetupTabState = {
        /// Offset into the map list for scrolling.
        MapListScroll: float32
        /// Full list of installed map archives, sorted by engine-
        /// registered name. Computed once at tab construction.
        Maps: MapRow list
        /// Current lobby (map name + teams + mode + speed).
        Lobby: LobbyConfig.LobbyConfig
        /// Validation result of `Lobby` against the active BarInstall.
        /// Empty list means valid; otherwise the GUI renders the
        /// full set per AS-1.3.
        Errors: LobbyConfig.LobbyError list
        /// Most recent Launch attempt's outcome — `None` when never
        /// launched, `Some error` when the last attempt reported
        /// back from `SessionManager.Launch`.
        LastLaunchError: string option
    }

    /// Build the initial tab state from a BarInstall.
    val init: install: BarInstall.BarInstall -> SetupTabState

    /// Re-run validation against the current lobby.
    val validate:
        install: BarInstall.BarInstall ->
        state: SetupTabState ->
            SetupTabState

    /// Render the tab content into the area to the right of the
    /// TabBar and above the StatusBar. Caller provides the content
    /// rectangle `(x, y, w, h)`.
    val render:
        state: SetupTabState ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            Element list

    /// Hit-test a mouse click. Returns the action that should fire.
    val handleMouse:
        state: SetupTabState ->
        x: float32 ->
        y: float32 ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            SetupTabAction option

    /// Hit-test a scroll wheel event. Returns a `ScrollMapList`
    /// action when the scroll landed inside the map list.
    val handleScroll:
        state: SetupTabState ->
        delta: float32 ->
        x: float32 ->
        y: float32 ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            SetupTabAction option
