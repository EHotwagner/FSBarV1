namespace FSBar.LiveTests

open Xunit
open FSBar.Client

/// Event delivery integration tests.
/// Validates that real engine events arrive correctly in the F# client.
[<Collection("Engine")>]
type EventTests(engine: EngineFixture) =

    [<Fact>]
    [<Trait("Category", "Events")>]
    member _.``Init event received with valid team ID``() =
        let initEvents =
            engine.InitialEvents
            |> List.choose (function GameEvent.Init teamId -> Some teamId | _ -> None)

        Assert.True(initEvents.Length >= 1, "Should receive at least one Init event")
        let teamId = initEvents.[0]
        Assert.True(teamId >= 0, $"Init teamId should be >= 0, got {teamId}")

    [<Fact>]
    [<Trait("Category", "Events")>]
    member _.``Update events received with matching frame numbers``() =
        let frames = engine.Client.Run(5, fun _ -> [])

        Assert.True(frames.Length >= 5, $"Should have at least 5 frames, got {frames.Length}")

        for frame in frames do
            let updateFrameNums =
                frame.Events
                |> List.choose (function GameEvent.Update f -> Some f | _ -> None)
            if frame.FrameNumber > 0u then
                Assert.True(updateFrameNums.Length >= 1,
                    $"Frame {frame.FrameNumber} should contain at least one Update event")

    [<Fact>]
    [<Trait("Category", "Events")>]
    member _.``UnitCreated event received for builder unit``() =
        let unitCreatedEvents =
            engine.InitialEvents
            |> List.choose (function
                | GameEvent.UnitCreated(unitId, builderId) -> Some(unitId, builderId)
                | _ -> None)

        Assert.True(unitCreatedEvents.Length >= 1,
            "Should receive at least one UnitCreated event in initial frames")

        let (unitId, _) = unitCreatedEvents.[0]
        Assert.True(unitId > 0, $"UnitCreated unitId should be > 0, got {unitId}")

    [<Fact>]
    [<Trait("Category", "Events")>]
    member _.``UnitFinished event received for commander``() =
        let createdUnitIds =
            engine.InitialEvents
            |> List.choose (function
                | GameEvent.UnitCreated(unitId, _) -> Some unitId
                | _ -> None)
            |> Set.ofList

        let finishedUnitIds =
            engine.InitialEvents
            |> List.choose (function
                | GameEvent.UnitFinished unitId -> Some unitId
                | _ -> None)

        Assert.True(finishedUnitIds.Length >= 1,
            "Should receive at least one UnitFinished event in initial frames")

        let finishedUnit = finishedUnitIds.[0]
        Assert.True(createdUnitIds.Contains(finishedUnit),
            $"UnitFinished unitId {finishedUnit} should match a previously created unit")

    [<Fact>]
    [<Trait("Category", "Events")>]
    member _.``Unknown events do not crash the frame loop``() =
        let frames = engine.Client.Run(10, fun _ -> [])

        Assert.True(frames.Length >= 10,
            $"Should have processed 10 frames without crashing, got {frames.Length}")
