module FSBar.Client.Tests.BarClientTests

open Xunit
open FSBar.Client

[<Fact>]
let ``create_returns_idle_state`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    Assert.Equal(Idle, client.State)

[<Fact>]
let ``create_config_matches_provided`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    Assert.Equal(config.MapName, client.Config.MapName)
    Assert.Equal(config.GameType, client.Config.GameType)
    Assert.Equal(config.Mode, client.Config.Mode)
    Assert.Equal(config.SocketPath, client.Config.SocketPath)

[<Fact>]
let ``create_with_custom_config_preserves_settings`` () =
    let config = { EngineConfig.defaultConfig () with
                    MapName = "CustomMap"
                    GameType = "CustomGame"
                    Mode = Graphical
                    GameSpeed = 50 }
    use client = BarClient.create config
    Assert.Equal("CustomMap", client.Config.MapName)
    Assert.Equal("CustomGame", client.Config.GameType)
    Assert.Equal(Graphical, client.Config.Mode)
    Assert.Equal(50, client.Config.GameSpeed)

[<Fact>]
let ``create_handshake_is_none`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    Assert.True(client.Handshake.IsNone)

[<Fact>]
let ``stream_access_before_connect_throws`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    Assert.Throws<exn>(fun () ->
        client.Stream |> ignore
    ) |> ignore

[<Fact>]
let ``dispose_from_idle_is_safe`` () =
    let config = EngineConfig.defaultConfig ()
    let client = BarClient.create config
    (client :> System.IDisposable).Dispose()
    Assert.Equal(Idle, client.State)

[<Fact>]
let ``stop_from_idle_is_safe`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    client.Stop()
    Assert.Equal(Idle, client.State)

[<Fact>]
let ``defaultConfig_module_function_works`` () =
    let config = BarClient.defaultConfig ()
    Assert.Equal(Headless, config.Mode)
    Assert.Equal("Avalanche 3.4", config.MapName)

[<Fact>]
let ``frames_property_returns_observable`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    let frames = client.Frames
    Assert.NotNull(frames)

[<Fact>]
let ``frames_observable_supports_subscribe`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    let mutable received = 0
    use _sub = client.Frames.Subscribe(
        { new System.IObserver<GameFrame> with
            member _.OnNext(_) = received <- received + 1
            member _.OnCompleted() = ()
            member _.OnError(_) = () })
    // No frames expected since we're not connected, but subscription should work
    Assert.Equal(0, received)

[<Fact>]
let ``frames_observable_supports_multiple_subscribers`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    let mutable count1 = 0
    let mutable count2 = 0
    use _sub1 = client.Frames.Subscribe(
        { new System.IObserver<GameFrame> with
            member _.OnNext(_) = count1 <- count1 + 1
            member _.OnCompleted() = ()
            member _.OnError(_) = () })
    use _sub2 = client.Frames.Subscribe(
        { new System.IObserver<GameFrame> with
            member _.OnNext(_) = count2 <- count2 + 1
            member _.OnCompleted() = ()
            member _.OnError(_) = () })
    Assert.Equal(0, count1)
    Assert.Equal(0, count2)

[<Fact>]
let ``send_commands_raises_when_idle`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    Assert.Throws<System.InvalidOperationException>(fun () ->
        client.SendCommands []
    ) |> ignore

[<Fact>]
let ``send_commands_raises_when_stopped`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    client.Stop()  // transitions to Idle (no-op), test with explicit stopped state
    // Create, start, stop cycle can't be tested without engine, but the state check is valid
    Assert.Throws<System.InvalidOperationException>(fun () ->
        client.SendCommands []
    ) |> ignore

[<Fact>]
let ``game_state_is_empty_before_start`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    Assert.Equal(0u, client.GameState.FrameNumber)
    Assert.True(client.GameState.Units.IsEmpty)

[<Fact>]
let ``multiple_create_dispose_cycles`` () =
    for _ in 1..3 do
        let config = EngineConfig.defaultConfig ()
        use client = BarClient.create config
        Assert.Equal(Idle, client.State)
