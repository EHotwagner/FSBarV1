namespace FSBar.Hub.App.Tabs

open SkiaViewer
open FSBar.Hub

/// Settings tab (feature 035-central-gui-hub T040) — always-accessible
/// counterpart to the First-Run Wizard. Shows the detected BAR
/// install, the bundled-proxy version, and the proxy's current
/// installation status on the active engine. Provides a one-click
/// "Install / Upgrade" button that invokes `ProxyInstaller.install`
/// and renders the per-step outcome.
module SettingsTab =

    /// Actions the Settings tab surfaces on mouse input.
    [<RequireQualifiedAccess>]
    type SettingsTabAction =
        /// User clicked "Install / Upgrade". Caller should run
        /// `ProxyInstaller.install` and fold the result back into
        /// the tab state via `applyInstallResult`.
        | InstallProxy
        /// User clicked "Force reinstall" — same as `InstallProxy`
        /// but with `force = true` (overwrites a newer local
        /// libSkirmishAI.so per spec.md Edge Cases).
        | ForceReinstallProxy
        /// User clicked "Refresh status" — caller re-runs
        /// `ProxyInstaller.checkStatus` and applies it to the tab.
        | RefreshStatus

    /// Per-tab render state.
    type SettingsTabState = {
        /// Last-observed proxy status for the active engine. `None`
        /// when BAR detection or bundled-proxy resolution failed
        /// (in which case the tab shows a warning banner instead).
        Status: ProxyInstaller.ProxyInstallStatus option
        Health: ProxyInstaller.ProxyHealth option
        /// Result of the last Install / Upgrade click. `None` when
        /// the user hasn't clicked yet; `Some (Ok _)` on success
        /// (with a summary string); `Some (Error _)` with the
        /// failure reason list joined.
        LastInstallResult: Result<string, string> option
        /// True while a background install is in flight. The UI
        /// disables buttons during this window.
        InstallInFlight: bool
    }

    /// Build initial tab state from an install + bundled info. The
    /// caller evaluates `checkStatus` synchronously at construction
    /// time so the first render has real data.
    val init:
        install: BarInstall.BarInstall ->
        bundled: BundledProxy.BundledProxyInfo ->
            SettingsTabState

    /// Re-apply a fresh `checkStatus` result to the tab (e.g. after
    /// the user clicked RefreshStatus).
    val applyStatus:
        state: SettingsTabState ->
        status: ProxyInstaller.ProxyInstallStatus ->
            SettingsTabState

    /// Update the tab with the outcome of an `install` call.
    val applyInstallResult:
        state: SettingsTabState ->
        result: Result<ProxyInstaller.ProxyInstallStatus, string list> ->
            SettingsTabState

    val render:
        state: SettingsTabState ->
        install: BarInstall.BarInstall option ->
        bundled: BundledProxy.BundledProxyInfo option ->
        settings: HubSettings.HubSettings ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            Element list

    val handleMouse:
        state: SettingsTabState ->
        x: float32 ->
        y: float32 ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            SettingsTabAction option
