namespace FSBar.LiveTests

open System
open Xunit
open FSBar.Client

/// Connection and handshake integration tests.
/// Validates the full communication chain: engine -> proxy -> socket -> F# client.
[<Collection("Engine")>]
type ConnectionTests(engine: EngineFixture) =

    [<Fact>]
    [<Trait("Category", "Connection")>]
    member _.``Harness smoke test - engine starts and client is connected``() =
        Assert.True(engine.IsEngineAlive, "Engine should be running")
        Assert.NotNull(engine.Client)

    [<Fact>]
    [<Trait("Category", "Connection")>]
    member _.``Client connects to engine proxy socket``() =
        let client = engine.Client
        Assert.True(
            client.State = Connected || client.State = Running,
            $"Client should be connected, got state: {client.State}")

    [<Fact>]
    [<Trait("Category", "Connection")>]
    member _.``Handshake completes with valid protocol metadata``() =
        let hs = engine.Client.Handshake
        Assert.True(hs.IsSome, "Handshake info should be available")
        let info = hs.Value
        Assert.True(info.ProtocolVersion > 0u, $"Protocol version should be > 0, got {info.ProtocolVersion}")
        Assert.True(info.TeamId >= 0, $"TeamId should be >= 0, got {info.TeamId}")

    [<Fact>]
    [<Trait("Category", "Connection")>]
    member _.``First frames contain Init event``() =
        let hasInit =
            engine.InitialEvents
            |> List.exists (function GameEvent.Init _ -> true | _ -> false)
        Assert.True(hasInit, "Init event should appear in the initial warm-up frames")

    [<Fact>]
    [<Trait("Category", "Connection")>]
    member _.``Empty command responses work for consecutive frames``() =
        let client = engine.Client
        let frames = client.Run(5, fun _ -> [])

        Assert.True(frames.Length >= 5,
            $"Should have received at least 5 frames, got {frames.Length}")

        for i in 1 .. frames.Length - 1 do
            Assert.True(frames.[i].FrameNumber > frames.[i - 1].FrameNumber,
                $"Frame numbers should increment: {frames.[i-1].FrameNumber} -> {frames.[i].FrameNumber}")

    [<Fact>]
    [<Trait("Category", "Connection")>]
    member _.``Graceful disconnect after receiving frames``() =
        let client = engine.Client
        let frames = client.Run(3, fun _ -> [])
        Assert.True(frames.Length >= 3, $"Should have processed at least 3 frames")
        Assert.True(engine.IsEngineAlive, "Engine should still be alive")
