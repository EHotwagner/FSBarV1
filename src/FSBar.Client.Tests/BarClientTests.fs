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
    Assert.Equal("Red Rock Desert v2", config.MapName)

[<Fact>]
let ``multiple_create_dispose_cycles`` () =
    for _ in 1..3 do
        let config = EngineConfig.defaultConfig ()
        use client = BarClient.create config
        Assert.Equal(Idle, client.State)
