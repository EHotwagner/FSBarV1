module FSBar.Client.Tests.EngineConfigTests

open Xunit
open FSBar.Client

[<Fact>]
let ``defaultConfig_returns_headless_mode`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal(Headless, config.Mode)

[<Fact>]
let ``defaultConfig_returns_expected_map_name`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal("Avalanche 3.4", config.MapName)

[<Fact>]
let ``defaultConfig_returns_expected_game_type`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal("Beyond All Reason test-29871-90f4bc1", config.GameType)

[<Fact>]
let ``defaultConfig_returns_expected_opponent_ai`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal("NullAI", config.OpponentAI)

[<Fact>]
let ``defaultConfig_returns_expected_sides`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal("Armada", config.OurSide)
    Assert.Equal("Cortex", config.OpponentSide)

[<Fact>]
let ``defaultConfig_returns_expected_timeout`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal(30000, config.TimeoutMs)

[<Fact>]
let ``defaultConfig_returns_expected_engine_bin`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal("spring-headless", config.EngineBin)

[<Fact>]
let ``defaultConfig_returns_none_spring_data_dir`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal(None, config.SpringDataDir)

[<Fact>]
let ``defaultConfig_returns_expected_game_speed`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal(100, config.GameSpeed)

[<Fact>]
let ``defaultConfig_generates_unique_socket_paths`` () =
    let config1 = EngineConfig.defaultConfig ()
    let config2 = EngineConfig.defaultConfig ()
    Assert.NotEqual<string>(config1.SocketPath, config2.SocketPath)

[<Fact>]
let ``defaultConfig_socket_path_starts_with_tmp`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.StartsWith("/tmp/fsbar-", config.SocketPath)
    Assert.EndsWith(".sock", config.SocketPath)

[<Fact>]
let ``custom_config_overrides_work`` () =
    let config = { EngineConfig.defaultConfig () with
                    Mode = Graphical
                    MapName = "TestMap"
                    GameSpeed = 50
                    SpringDataDir = Some "/custom/data" }
    Assert.Equal(Graphical, config.Mode)
    Assert.Equal("TestMap", config.MapName)
    Assert.Equal(50, config.GameSpeed)
    Assert.Equal(Some "/custom/data", config.SpringDataDir)

[<Fact>]
let ``headless_mode_variant_constructs`` () =
    let mode = Headless
    Assert.Equal(Headless, mode)

[<Fact>]
let ``graphical_mode_variant_constructs`` () =
    let mode = Graphical
    Assert.Equal(Graphical, mode)
