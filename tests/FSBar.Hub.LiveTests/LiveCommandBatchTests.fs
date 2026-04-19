namespace FSBar.Hub.LiveTests

// Feature 046 — US4 live tests. Exercises SendCommandBatch against a
// real BAR engine session: oversize rejection, empty batch, and a
// real multi-command dispatch.

open System
open Xunit
open FSBar.Client
open FSBar.Hub
open Fsbar.Hub.Scripting.V1

[<Collection("HubSession")>]
type LiveCommandBatchTests() =

    /// US4 oversize gate — a batch with 1025 entries MUST be rejected
    /// whole (zero outcomes, non-empty diagnostic naming the cap).
    /// This is pure input validation — no session required.
    [<Fact>]
    [<Trait("Category", "Feature046")>]
    member _.``US4 — oversize batch (1025) rejected whole without session``() =
        task {
            let install = HeadlessOrchestrationFixtures.requireBarInstall ()
            let svc, sm, bus, _ = HeadlessOrchestrationFixtures.makeService install
            try
                let moveCmd =
                    ({ Command =
                        Highbar.AICommand.CommandCase.MoveUnit
                            ({ UnitId = 1
                               GroupId = 0
                               Options = 0u
                               Timeout = 0
                               ToPosition = Some ({ X = 0.0f; Y = 0.0f; Z = 0.0f } : Highbar.Vector3) }
                             : Highbar.MoveUnitCommand) }
                     : Highbar.AICommand)
                let commands = List.replicate 1025 moveCmd
                let req : SendCommandBatchRequest = { Commands = commands }
                let resp = (svc.SendCommandBatch req HeadlessOrchestrationFixtures.nullContext).Result
                Assert.Equal(0, resp.ForwardedAtFrame)
                Assert.Empty(resp.Outcomes)
                Assert.Contains("1025", resp.BatchDiagnostic)
                Assert.Contains("1024", resp.BatchDiagnostic)
            finally
                try (svc :> IDisposable).Dispose() with _ -> ()
                try (sm :> IDisposable).Dispose() with _ -> ()
                try (bus :> IDisposable).Dispose() with _ -> ()
        }

    /// US4 — a batch of valid commands on a live session returns one
    /// forwarded_at_frame and 1:1 CommandOutcomes, all accepted.
    [<SkippableFact>]
    [<Trait("Category", "Feature046")>]
    member _.``US4 — batch of 3 move commands forwarded on one frame``() =
        task {
            let svc, sm, bus =
                StateStreamFixtures.launchAndWait "Avalanche 3.4" "avalanche_3.4"
            try
                let mk unitId =
                    ({ Command =
                        Highbar.AICommand.CommandCase.MoveUnit
                            ({ UnitId = unitId
                               GroupId = 0
                               Options = 0u
                               Timeout = 0
                               ToPosition = Some ({ X = 100.0f; Y = 0.0f; Z = 100.0f } : Highbar.Vector3) }
                             : Highbar.MoveUnitCommand) }
                     : Highbar.AICommand)
                let req : SendCommandBatchRequest = {
                    Commands = [ mk 1; mk 2; mk 3 ]
                }
                let resp = (svc.SendCommandBatch req HeadlessOrchestrationFixtures.nullContext).Result
                Assert.Equal(3, List.length resp.Outcomes)
                for o in resp.Outcomes do
                    Assert.True(o.Accepted,
                        sprintf "outcome %d rejected: %s" o.Index o.Diagnostic)
                Assert.Equal("", resp.BatchDiagnostic)
            finally
                StateStreamFixtures.stopSession svc sm
                try (svc :> IDisposable).Dispose() with _ -> ()
                try (sm :> IDisposable).Dispose() with _ -> ()
                try (bus :> IDisposable).Dispose() with _ -> ()
        }

    /// US4 — a batch entry with no payload (command = None) is
    /// rejected per-entry while accepted peers still forward.
    [<SkippableFact>]
    [<Trait("Category", "Feature046")>]
    member _.``US4 — invalid (empty) command in batch rejected per-entry``() =
        task {
            let svc, sm, bus =
                StateStreamFixtures.launchAndWait "Avalanche 3.4" "avalanche_3.4"
            try
                let valid =
                    ({ Command =
                        Highbar.AICommand.CommandCase.MoveUnit
                            ({ UnitId = 1
                               GroupId = 0
                               Options = 0u
                               Timeout = 0
                               ToPosition = Some ({ X = 0.0f; Y = 0.0f; Z = 0.0f } : Highbar.Vector3) }
                             : Highbar.MoveUnitCommand) }
                     : Highbar.AICommand)
                let empty = ({ Command = Highbar.AICommand.CommandCase.None } : Highbar.AICommand)
                let req : SendCommandBatchRequest = { Commands = [ valid; empty; valid ] }
                let resp = (svc.SendCommandBatch req HeadlessOrchestrationFixtures.nullContext).Result
                Assert.Equal(3, List.length resp.Outcomes)
                Assert.True(resp.Outcomes.[0].Accepted, "entry 0 should be accepted")
                Assert.False(resp.Outcomes.[1].Accepted, "entry 1 (empty) should be rejected")
                Assert.True(resp.Outcomes.[2].Accepted, "entry 2 should be accepted")
            finally
                StateStreamFixtures.stopSession svc sm
                try (svc :> IDisposable).Dispose() with _ -> ()
                try (sm :> IDisposable).Dispose() with _ -> ()
                try (bus :> IDisposable).Dispose() with _ -> ()
        }
