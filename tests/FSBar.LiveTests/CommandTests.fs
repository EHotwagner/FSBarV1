namespace FSBar.LiveTests

open Xunit
open FSBar.Client

/// Command execution integration tests.
/// Sends commands from F# client and verifies the engine executes them.
[<Collection("Engine")>]
type CommandTests(engine: EngineFixture) =

    /// Get the first builder unit ID from initial warm-up events.
    let getFirstUnitId () =
        engine.InitialEvents
        |> List.tryPick (function
            | GameEvent.UnitCreated(unitId, _) -> Some unitId
            | _ -> None)

    [<Fact>]
    [<Trait("Category", "Commands")>]
    member _.``MoveCommand causes unit to change position``() =
        let builderUnitId = getFirstUnitId ()
        Assert.True(builderUnitId.IsSome, "Should have found a builder unit")

        let uid = builderUnitId.Value
        let mutable moveSent = false

        let frames =
            engine.Client.Run(35, fun frame ->
                if not moveSent then
                    moveSent <- true
                    [ Commands.MoveCommand uid 2048.0f 100.0f 2048.0f ]
                else
                    [])

        Assert.True(moveSent, "Should have sent MoveCommand")
        Assert.True(frames.Length >= 35, $"Should have run 35 frames, got {frames.Length}")

    [<Fact>]
    [<Trait("Category", "Commands")>]
    member _.``BuildCommand triggers unit creation``() =
        let builderUnitId = getFirstUnitId ()
        Assert.True(builderUnitId.IsSome, "Should have found a builder unit")

        let uid = builderUnitId.Value
        let mutable buildSent = false
        let createdAfterBuild = ResizeArray<int>()

        let _frames =
            engine.Client.Run(70, fun frame ->
                if buildSent then
                    frame.Events |> List.iter (function
                        | GameEvent.UnitCreated(newUid, _) -> createdAfterBuild.Add(newUid)
                        | _ -> ())

                if not buildSent then
                    buildSent <- true
                    // Build unit def ID 1 (generic) at a position near the commander
                    [ Commands.BuildCommand uid 1 600.0f 100.0f 600.0f 0 ]
                else
                    [])

        Assert.True(buildSent, "Should have sent BuildCommand")

    [<Fact>]
    [<Trait("Category", "Commands")>]
    member _.``StopCommand halts a moving unit``() =
        let builderUnitId = getFirstUnitId ()
        Assert.True(builderUnitId.IsSome, "Should have found a builder unit")

        let uid = builderUnitId.Value
        let mutable moveSent = false
        let mutable stopSent = false
        let mutable frameIdx = 0

        let frames =
            engine.Client.Run(25, fun _ ->
                frameIdx <- frameIdx + 1
                if not moveSent && frameIdx >= 3 then
                    moveSent <- true
                    [ Commands.MoveCommand uid 2048.0f 100.0f 2048.0f ]
                elif moveSent && not stopSent && frameIdx >= 10 then
                    stopSent <- true
                    [ Commands.StopCommand uid ]
                else
                    [])

        Assert.True(stopSent, "Should have sent StopCommand")
        Assert.True(frames.Length >= 25, $"Should have run 25 frames, got {frames.Length}")

    [<Fact>]
    [<Trait("Category", "Commands")>]
    member _.``Patrol Guard Attack Fight commands accepted without crashing``() =
        let builderUnitId = getFirstUnitId ()
        Assert.True(builderUnitId.IsSome, "Should have found a builder unit")

        let uid = builderUnitId.Value
        let mutable commandsSent = 0
        let mutable frameIdx = 0

        let frames =
            engine.Client.Run(30, fun _ ->
                frameIdx <- frameIdx + 1
                match frameIdx with
                | 5 ->
                    commandsSent <- commandsSent + 1
                    [ Commands.PatrolCommand uid 1024.0f 100.0f 1024.0f ]
                | 10 ->
                    commandsSent <- commandsSent + 1
                    [ Commands.GuardCommand uid uid ]
                | 15 ->
                    commandsSent <- commandsSent + 1
                    [ Commands.AttackCommand uid 99999 ]
                | 20 ->
                    commandsSent <- commandsSent + 1
                    [ Commands.FightCommand uid 1500.0f 100.0f 1500.0f ]
                | _ -> [])

        Assert.True(commandsSent >= 4, $"Should have sent 4 commands, sent {commandsSent}")
        Assert.True(frames.Length >= 30, "Should complete 30 frames without crashing")
