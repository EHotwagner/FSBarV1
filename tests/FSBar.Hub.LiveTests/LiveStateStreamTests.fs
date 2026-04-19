namespace FSBar.Hub.LiveTests

// Feature 046 — US1 (per-tick GameState readout) + US2 (typed game
// events) live tests. Exercises the projection wired into
// ScriptingHub.publishFrame against a real BAR engine session.
//
// Skips when BAR install / HighBarV2 / BARb / Avalanche are missing,
// matching the LiveHeadlessOrchestrationTests pattern (the test reuses
// HeadlessOrchestrationFixtures from that file).

open System
open System.Diagnostics
open System.IO
open System.Threading
open Xunit
open FSBar.Client
open FSBar.Hub
open Fsbar.Hub.Scripting.V1

module StateStreamFixtures =
    open HeadlessOrchestrationFixtures

    /// Drive the orchestration into Running on the given map and return
    /// (svc, sm, bus, store) along with a started session-id from the
    /// LaunchSession response. Caller MUST dispose svc / sm / bus.
    let launchAndWait (mapName: string) (mapStem: string) =
        let install = requireBarInstall ()
        requireMapInstalled install mapStem
        let svc, sm, bus, store = makeService install

        let wire = happyLobbyWire mapName
        let cfgReq : ConfigureLobbyRequest = { Lobby = Some wire }
        let cfgResp = (svc.ConfigureLobby cfgReq nullContext).Result
        match cfgResp.Result with
        | Some r -> Assert.Equal(SubmitOutcome.Sent, r.Outcome)
        | None -> Assert.Fail(sprintf "ConfigureLobby errors: %A" cfgResp.ValidationErrors)

        let launchReq : LaunchSessionRequest = {
            StartPaused = false
            LaunchGraphicalViewer = false
        }
        let launchResp = (svc.LaunchSession launchReq nullContext).Result
        match launchResp.Result with
        | Some r -> Assert.Equal(SubmitOutcome.Sent, r.Outcome)
        | None -> Assert.Fail("LaunchSession returned no MutationResult")

        let running =
            waitUntil 40000 (fun () ->
                match sm.State with SessionManager.Running _ -> true | _ -> false)
        Assert.True(running, sprintf "session did not reach Running on %s" mapName)
        svc, sm, bus

    let stopSession (svc: ScriptingHub.ScriptingService) (sm: SessionManager.SessionManager) =
        try
            let _ = (svc.StopSession StopSessionRequest.empty nullContext).Result
            waitUntil 15000 (fun () -> sm.State = SessionManager.Idle) |> ignore
        with _ -> ()

[<Collection("HubSession")>]
type LiveStateStreamTests() =

    /// US1 — within `timeoutMs` of session-start, the state stream
    /// delivers a frame with `GameState = Some {...}`, ≥1 friendly
    /// (commander), populated 8-field economy, and consecutive frame
    /// numbers are non-decreasing across collected snapshots.
    [<SkippableFact>]
    [<Trait("Category", "Feature046")>]
    member _.``US1 — state stream delivers populated GameStateFrame within 30s on Avalanche``() =
        task {
            let svc, sm, bus =
                StateStreamFixtures.launchAndWait "Avalanche 3.4" "avalanche_3.4"
            try
                let _clientId, reader = svc.AttachTestClient("us1-state")

                // Collect frames for up to 30s, looking for the first
                // populated GameState. Then validate AS1 invariants on
                // the next ≤10 messages.
                let collected = ResizeArray<GameFrameMessage>()
                let sw = Stopwatch.StartNew()
                let mutable populated = false
                while not populated && sw.ElapsedMilliseconds < 30000L do
                    let mutable msg = Unchecked.defaultof<GameFrameMessage>
                    if reader.TryRead(&msg) then
                        collected.Add(msg)
                        if msg.GameState.IsSome
                           && (msg.GameState.Value.Friendlies |> List.isEmpty |> not)
                        then populated <- true
                    else
                        do! Async.Sleep(100)

                Assert.True(
                    populated,
                    sprintf "no populated GameStateFrame within 30s; collected %d empty messages"
                        collected.Count)

                // Drain another batch so we can check monotonicity.
                let extra = ResizeArray<GameFrameMessage>()
                let sw2 = Stopwatch.StartNew()
                while extra.Count < 5 && sw2.ElapsedMilliseconds < 15000L do
                    let mutable msg = Unchecked.defaultof<GameFrameMessage>
                    if reader.TryRead(&msg) then extra.Add(msg)
                    else do! Async.Sleep(100)

                let withState =
                    Seq.append collected extra
                    |> Seq.choose (fun m -> m.GameState)
                    |> Seq.toList

                // AS1 — at least one populated snapshot has friendlies +
                // an economy record.
                let firstPop =
                    withState
                    |> List.find (fun gs -> not (List.isEmpty gs.Friendlies))
                Assert.NotEmpty(firstPop.Friendlies)
                Assert.True(firstPop.Economy.IsSome, "Economy must be populated")

                let econ = firstPop.Economy.Value
                // 8-field economy — at least one of metal/energy storage
                // is positive on a real engine session (commander has a
                // base storage > 0).
                Assert.True(
                    econ.MetalStorage > 0.0f || econ.EnergyStorage > 0.0f,
                    sprintf "economy storage not populated: %A" econ)

                // Non-decreasing frame numbers.
                let frames =
                    withState
                    |> List.map (fun gs -> int gs.FrameNumber)
                let isMonotone =
                    frames
                    |> List.pairwise
                    |> List.forall (fun (a, b) -> a <= b)
                Assert.True(isMonotone, sprintf "frames not non-decreasing: %A" frames)
            finally
                StateStreamFixtures.stopSession svc sm
                try (svc :> IDisposable).Dispose() with _ -> ()
                try (sm :> IDisposable).Dispose() with _ -> ()
                try (bus :> IDisposable).Dispose() with _ -> ()
        }

    /// FR-003 totality — every enemy on a delivered snapshot must set
    /// exactly one arm of `health_info` (Health or Unknown — never None).
    /// Run on the same session shape; if no enemies are visible during
    /// the collection window, the assertion is vacuously satisfied
    /// (US1 AS2/AS3 still hold by construction since the projection
    /// is the only writer of the oneof).
    [<SkippableFact>]
    [<Trait("Category", "Feature046")>]
    member _.``US1 — every projected EnemyUnitState sets exactly one health_info arm``() =
        task {
            let svc, sm, bus =
                StateStreamFixtures.launchAndWait "Avalanche 3.4" "avalanche_3.4"
            try
                let _clientId, reader = svc.AttachTestClient("us1-health")

                // Collect 20s of frames so a few may include enemies.
                let enemies = ResizeArray<EnemyUnitState>()
                let sw = Stopwatch.StartNew()
                while sw.ElapsedMilliseconds < 20000L do
                    let mutable msg = Unchecked.defaultof<GameFrameMessage>
                    if reader.TryRead(&msg) then
                        match msg.GameState with
                        | Some gs -> enemies.AddRange(gs.Enemies)
                        | None -> ()
                    else do! Async.Sleep(100)

                for e in enemies do
                    match e.HealthInfo with
                    | EnemyUnitState.HealthInfoCase.Health _ -> ()
                    | EnemyUnitState.HealthInfoCase.Unknown _ -> ()
                    | EnemyUnitState.HealthInfoCase.None ->
                        Assert.Fail(
                            sprintf "EnemyUnitState id=%d has no health_info arm set" e.UnitId)
            finally
                StateStreamFixtures.stopSession svc sm
                try (svc :> IDisposable).Dispose() with _ -> ()
                try (sm :> IDisposable).Dispose() with _ -> ()
                try (bus :> IDisposable).Dispose() with _ -> ()
        }

    /// US2 — within the first 30s of streaming, the projected
    /// `game_events` carries at least one lifecycle event (UnitCreated,
    /// UnitFinished, UnitIdle, EnemyEnterLOS, EnemyEnterRadar,
    /// UnitDestroyed, …) — any payload that is NOT the per-tick
    /// `Update` filler. Init fires during session handshake before the
    /// scripting client can subscribe, so it is not a reliable gate.
    /// Validates that GameEvent → wire mapping runs end-to-end against
    /// the live engine.
    [<SkippableFact>]
    [<Trait("Category", "Feature046")>]
    member _.``US2 — game_events delivers a lifecycle event within 30s on Avalanche``() =
        task {
            let svc, sm, bus =
                StateStreamFixtures.launchAndWait "Avalanche 3.4" "avalanche_3.4"
            try
                let _clientId, reader = svc.AttachTestClient("us2-events")

                let lifecycleCount = ref 0
                let totalEvents = ref 0
                let sw = Stopwatch.StartNew()
                let mutable found = false
                while not found && sw.ElapsedMilliseconds < 30000L do
                    let mutable msg = Unchecked.defaultof<GameFrameMessage>
                    if reader.TryRead(&msg) then
                        for ev in msg.GameEvents do
                            incr totalEvents
                            match ev.Payload with
                            | GameEventEnvelope.PayloadCase.Update _
                            | GameEventEnvelope.PayloadCase.None -> ()
                            | _ ->
                                incr lifecycleCount
                                found <- true
                    else do! Async.Sleep(100)

                Assert.True(
                    found,
                    sprintf "no lifecycle event in first 30s; saw %d total events (%d lifecycle)"
                        totalEvents.Value lifecycleCount.Value)
            finally
                StateStreamFixtures.stopSession svc sm
                try (svc :> IDisposable).Dispose() with _ -> ()
                try (sm :> IDisposable).Dispose() with _ -> ()
                try (bus :> IDisposable).Dispose() with _ -> ()
        }
