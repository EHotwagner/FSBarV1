module FSBar.Hub.Tests.ScriptingServiceUnaryTests

// Feature 040 T020 — unit tests for US1 session-orchestration RPCs.
// Each test drives one override directly (ServerCallContext defaults to
// null since none of the US1 implementations touch it).

open System
open System.IO
open System.Threading.Tasks
open Xunit
open FSBar.Hub
open Fsbar.Hub.Scripting.V1

/// Fixture: a temp BAR install with one map + one AI registered under
/// the active engine. Enough for BarInstall.detect + LobbyConfig.validate
/// + ListMaps to succeed without touching the user's real BAR install.
type private BarFixture() =
    let tempDir =
        let p = Path.Combine(Path.GetTempPath(), "fsbar-usunary-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(p) |> ignore
        p

    do
        let mapsDir = Path.Combine(tempDir, "maps")
        Directory.CreateDirectory(mapsDir) |> ignore
        // Create a dummy .sd7 file so ListMaps returns at least one entry.
        File.WriteAllBytes(Path.Combine(mapsDir, "testmap_1.0.sd7"), Array.empty)
        let engDir = Path.Combine(tempDir, "engine", "recoil_2026.03.14")
        Directory.CreateDirectory(engDir) |> ignore
        // Create both spring-headless + spring + an AI skirmish dir so
        // BarInstall.detect succeeds and LobbyConfig.validate can find
        // "HighBarV2" + "BARb" in LobbyConfig.defaults.
        let hb = Path.Combine(engDir, "spring-headless")
        File.WriteAllText(hb, "#!/bin/sh\nexit 0")
        File.SetUnixFileMode(
            hb,
            UnixFileMode.UserRead ||| UnixFileMode.UserWrite ||| UnixFileMode.UserExecute)
        let sp = Path.Combine(engDir, "spring")
        File.WriteAllText(sp, "#!/bin/sh\nexit 0")
        File.SetUnixFileMode(
            sp,
            UnixFileMode.UserRead ||| UnixFileMode.UserWrite ||| UnixFileMode.UserExecute)
        // AI skirmish dirs — one for HighBarV2, one for BARb (LobbyConfig.defaults).
        let aiDir name =
            let d = Path.Combine(engDir, "AI", "Skirmish", name)
            Directory.CreateDirectory(d) |> ignore
        aiDir "HighBarV2"
        aiDir "BARb"

    member _.DataDir = tempDir

    member this.Resolve() =
        let settings = { HubSettings.defaults with BarDataDirOverride = Some this.DataDir }
        match BarInstall.detect settings with
        | Ok install -> install
        | Result.Error e -> failwith (BarInstall.formatError e)

    interface IDisposable with
        member _.Dispose() =
            try Directory.Delete(tempDir, recursive = true) with _ -> ()

let private makeBundled () : BundledProxy.BundledProxyInfo =
    { Version = "0.1.17"
      BundleRoot = "/tmp/stub"
      LibSkirmishAiPath = "/tmp/stub/libSkirmishAI.so"
      AiInfoLuaPath = "/tmp/stub/AIInfo.lua"
      AiOptionsLuaPath = "/tmp/stub/AIOptions.lua" }

let private makeService () =
    let fixture = new BarFixture()
    let install = fixture.Resolve()
    let bus = HubEvents.create ()
    let sessions = SessionManager.create install bus.Sink
    let unitDefs () = FSBar.Client.UnitDefCache.empty
    let initialState : HubState =
        { ActiveTab = FSBar.Hub.HubTab.Setup
          VizConfig = FSBar.Viz.VizDefaults.defaultConfig
          Camera = ViewerCamera.defaults
          Lobby = LobbyConfig.defaults
          Encyclopedia = { FactionFilter = Set.empty; SelectedDefId = None }
          PresetList = []
          Settings = HubSettings.defaults }
    let store = HubStateStore.create bus.Sink initialState
    let overlays = OverlayLayerStore.create bus.Sink
    let renderer =
        HeadlessRenderer.create sessions store overlays bus.Sink (fun () -> HubSettings.defaults)
    let hubLog = HubLog.create bus.Sink (fun () -> HubSettings.defaults)
    let service =
        new ScriptingHub.ScriptingService(
            sessions, bus.Sink, bus.Events, unitDefs, install, makeBundled (), 5099,
            store, renderer, overlays, hubLog, ScriptingHub.defaults)
    service, bus, sessions, store, fixture

let private disposeAll
        (svc: ScriptingHub.ScriptingService)
        (bus: HubEvents.HubEventBus)
        (sm: SessionManager.SessionManager)
        (fx: BarFixture) =
    (svc :> IDisposable).Dispose()
    (sm :> IDisposable).Dispose()
    (bus :> IDisposable).Dispose()
    (fx :> IDisposable).Dispose()

let private nullContext : Grpc.Core.ServerCallContext =
    Unchecked.defaultof<Grpc.Core.ServerCallContext>

let private lobbyWireFromDefaults (mapName: string) : LobbyConfigWire =
    { MapName = mapName
      Mode = LobbyMode.Skirmish
      EngineSpeed = 1.0f
      LaunchGraphicalViewer = false
      Teams =
          [ { AllyTeamId = 0
              Seats =
                  [ { Kind = SeatKind.Ai
                      Side = "Armada"
                      Handicap = 0.0f
                      AiName = "HighBarV2"
                      HumanName = "" } ] }
            { AllyTeamId = 1
              Seats =
                  [ { Kind = SeatKind.Ai
                      Side = "Cortex"
                      Handicap = 0.0f
                      AiName = "BARb"
                      HumanName = "" } ] } ]
      Spectators = [] }

[<Fact>]
let ``T020a — ListMaps returns the seeded .sd7 entry`` () =
    let svc, bus, sm, _store, fx = makeService ()
    try
        let resp : ListMapsResponse =
            (svc.ListMaps ListMapsRequest.empty nullContext).Result
        Assert.NotEmpty(resp.Maps)
        Assert.Contains(resp.Maps, fun m -> m.Name = "testmap_1.0")
    finally
        disposeAll svc bus sm fx

[<Fact>]
let ``T020b — ConfigureLobby rejected when lobby missing`` () =
    let svc, bus, sm, _store, fx = makeService ()
    try
        let req : ConfigureLobbyRequest = { Lobby = None }
        let resp : ConfigureLobbyResponse =
            (svc.ConfigureLobby req nullContext).Result
        match resp.Result with
        | Some r ->
            Assert.Equal(SubmitOutcome.Rejected, r.Outcome)
            Assert.NotEqual<string>("", r.Reason)
        | None -> Assert.Fail("expected MutationResult")
    finally
        disposeAll svc bus sm fx

[<Fact>]
let ``T020c — ConfigureLobby validation returns error list`` () =
    let svc, bus, sm, _store, fx = makeService ()
    try
        // Map name does not match the seeded map.
        let lobby = lobbyWireFromDefaults "nonexistent_map"
        let req : ConfigureLobbyRequest = { Lobby = Some lobby }
        let resp : ConfigureLobbyResponse =
            (svc.ConfigureLobby req nullContext).Result
        match resp.Result with
        | Some r ->
            Assert.Equal(SubmitOutcome.Rejected, r.Outcome)
            Assert.Equal<string>("validation failed", r.Reason)
        | None -> Assert.Fail("expected MutationResult")
        Assert.NotEmpty(resp.ValidationErrors)
        Assert.Contains(
            resp.ValidationErrors,
            (fun (e: string) -> e.Contains("nonexistent_map")))
    finally
        disposeAll svc bus sm fx

[<Fact>]
let ``T020d — ConfigureLobby succeeds and mutates HubStateStore`` () =
    let svc, bus, sm, store, fx = makeService ()
    try
        let lobby = lobbyWireFromDefaults "testmap_1.0"
        let req : ConfigureLobbyRequest = { Lobby = Some lobby }
        let resp : ConfigureLobbyResponse =
            (svc.ConfigureLobby req nullContext).Result
        match resp.Result with
        | Some r -> Assert.Equal(SubmitOutcome.Sent, r.Outcome)
        | None -> Assert.Fail("expected MutationResult")
        Assert.Empty(resp.ValidationErrors)
        // Store should now carry the updated lobby.
        let s = HubStateStore.current store
        Assert.Equal("testmap_1.0", s.Lobby.MapName)
    finally
        disposeAll svc bus sm fx

[<Fact>]
let ``T020e — ValidateLobby returns errors without mutating store`` () =
    let svc, bus, sm, store, fx = makeService ()
    try
        let lobby = lobbyWireFromDefaults "nonexistent_map"
        let req : ValidateLobbyRequest = { Lobby = Some lobby }
        let resp : ValidateLobbyResponse =
            (svc.ValidateLobby req nullContext).Result
        Assert.NotEmpty(resp.Errors)
        // Store MapName should still be the initial empty default.
        let s = HubStateStore.current store
        Assert.Equal("", s.Lobby.MapName)
    finally
        disposeAll svc bus sm fx

[<Fact>]
let ``T020f — StopSession rejects when no active session`` () =
    let svc, bus, sm, _store, fx = makeService ()
    try
        let resp : StopSessionResponse =
            (svc.StopSession StopSessionRequest.empty nullContext).Result
        match resp.Result with
        | Some r ->
            Assert.Equal(SubmitOutcome.Rejected, r.Outcome)
            Assert.Equal<string>("no active session", r.Reason)
        | None -> Assert.Fail("expected MutationResult")
    finally
        disposeAll svc bus sm fx

// ---------------------------------------------------------------------
// US3 unit tests — SetVizConfig / SetVizAttribute / ToggleOverlay /
// SetCamera / SetActiveTab.
// ---------------------------------------------------------------------

[<Fact>]
let ``T045a — SetVizAttribute unknown key rejected`` () =
    let svc, bus, sm, _store, fx = makeService ()
    try
        let req : SetVizAttributeRequest = {
            Key = "nonexistent_key"
            Value =
                Some ({ Value = VizAttributeValue.ValueCase.BoolValue true } : VizAttributeValue)
        }
        let resp : SetVizAttributeResponse =
            (svc.SetVizAttribute req nullContext).Result
        match resp.Result with
        | Some r -> Assert.Equal(SubmitOutcome.Rejected, r.Outcome)
        | None -> Assert.Fail("expected MutationResult")
    finally
        disposeAll svc bus sm fx

[<Fact>]
let ``T045b — SetVizAttribute valid key mutates the store`` () =
    let svc, bus, sm, store, fx = makeService ()
    try
        // "overlays.showGridLines" is a valid bool descriptor key.
        let before = (HubStateStore.current store).VizConfig.ShowGridLines
        let req : SetVizAttributeRequest = {
            Key = "overlays.showGridLines"
            Value =
                Some ({ Value = VizAttributeValue.ValueCase.BoolValue (not before) }
                      : VizAttributeValue)
        }
        let resp : SetVizAttributeResponse =
            (svc.SetVizAttribute req nullContext).Result
        match resp.Result with
        | Some r -> Assert.Equal(SubmitOutcome.Sent, r.Outcome)
        | None -> Assert.Fail("expected MutationResult")
        let after = (HubStateStore.current store).VizConfig.ShowGridLines
        Assert.NotEqual<bool>(before, after)
    finally
        disposeAll svc bus sm fx

[<Fact>]
let ``T045c — SetVizConfig aggregates unknown + invalid keys`` () =
    let svc, bus, sm, _store, fx = makeService ()
    try
        let req : SetVizConfigRequest = {
            VizConfig =
                Some
                    ({ Attributes =
                        Map.ofList
                            [ "unknown_one",
                              ({ Value = VizAttributeValue.ValueCase.BoolValue true }
                               : VizAttributeValue)
                              "unknown_two",
                              ({ Value = VizAttributeValue.ValueCase.IntValue 1 }
                               : VizAttributeValue) ] } : VizConfigWire)
        }
        let resp : SetVizConfigResponse =
            (svc.SetVizConfig req nullContext).Result
        match resp.Result with
        | Some r -> Assert.Equal(SubmitOutcome.Rejected, r.Outcome)
        | None -> Assert.Fail("expected MutationResult")
        Assert.Equal(2, resp.UnknownKeys.Length)
    finally
        disposeAll svc bus sm fx

[<Fact>]
let ``T045d — SetCamera rejects NaN scale`` () =
    let svc, bus, sm, _store, fx = makeService ()
    try
        let req : SetCameraRequest = {
            Camera =
                Some ({ Scale = nanf; OriginX = 0.0f; OriginY = 0.0f; AutoFit = false }
                      : ViewerCameraWire)
        }
        let resp : SetCameraResponse = (svc.SetCamera req nullContext).Result
        match resp.Result with
        | Some r -> Assert.Equal(SubmitOutcome.Rejected, r.Outcome)
        | None -> Assert.Fail("expected MutationResult")
    finally
        disposeAll svc bus sm fx

[<Fact>]
let ``T045e — ToggleOverlay flips WeaponRanges`` () =
    let svc, bus, sm, store, fx = makeService ()
    try
        let before =
            (HubStateStore.current store).VizConfig.ActiveOverlays
            |> Set.contains FSBar.Viz.OverlayKind.WeaponRanges
        let req : ToggleOverlayRequest = {
            Overlay = OverlayKey.WeaponRanges
            Target = OverlayTargetState.Toggle
        }
        let resp : ToggleOverlayResponse = (svc.ToggleOverlay req nullContext).Result
        match resp.Result with
        | Some r -> Assert.Equal(SubmitOutcome.Sent, r.Outcome)
        | None -> Assert.Fail("expected MutationResult")
        Assert.Equal(not before, resp.NewState)
    finally
        disposeAll svc bus sm fx

[<Fact>]
let ``T045f — SetActiveTab updates store`` () =
    let svc, bus, sm, store, fx = makeService ()
    try
        let req : SetActiveTabRequest = { Tab = Fsbar.Hub.Scripting.V1.HubTab.Viewer }
        let resp : SetActiveTabResponse = (svc.SetActiveTab req nullContext).Result
        match resp.Result with
        | Some r -> Assert.Equal(SubmitOutcome.Sent, r.Outcome)
        | None -> Assert.Fail("expected MutationResult")
        Assert.Equal(FSBar.Hub.HubTab.Viewer, (HubStateStore.current store).ActiveTab)
    finally
        disposeAll svc bus sm fx

[<Fact>]
let ``T020g — LaunchSession rejected when lobby invalid in store`` () =
    // Store seed has MapName = "" which will fail validation at launch
    // time. SessionManager.Launch returns Error because LobbyConfig.validate
    // fails, so the RPC surfaces Rejected.
    let svc, bus, sm, _store, fx = makeService ()
    try
        let req : LaunchSessionRequest = {
            StartPaused = false
            LaunchGraphicalViewer = false
        }
        let resp : LaunchSessionResponse =
            (svc.LaunchSession req nullContext).Result
        match resp.Result with
        | Some r -> Assert.Equal(SubmitOutcome.Rejected, r.Outcome)
        | None -> Assert.Fail("expected MutationResult")
        Assert.Equal<string option>(None, resp.SessionId)
    finally
        disposeAll svc bus sm fx
