module FSBar.Client.Tests.ScriptGeneratorTests

open Xunit
open FSBar.Client

[<Fact>]
let ``generate_headless_produces_valid_script`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("[GAME]", script)
    Assert.Contains("}", script)

[<Fact>]
let ``generate_contains_map_name`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("Avalanche 3.4", script)

[<Fact>]
let ``generate_contains_game_type`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("Beyond All Reason test-29871-90f4bc1", script)

[<Fact>]
let ``generate_contains_socket_path`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains(config.SocketPath, script)

[<Fact>]
let ``generate_contains_our_side`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("Side=Armada;", script)

[<Fact>]
let ``generate_contains_opponent_side`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("Side=Cortex;", script)

[<Fact>]
let ``generate_contains_opponent_ai`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("NullAI", script)

[<Fact>]
let ``generate_contains_game_speed`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("MinSpeed=100;", script)
    Assert.Contains("MaxSpeed=100;", script)

[<Fact>]
let ``generate_contains_faction_modoptions`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("teamfaction_0=armada;", script)
    Assert.Contains("teamfaction_1=cortex;", script)

[<Fact>]
let ``generate_with_graphical_config`` () =
    let config = { EngineConfig.defaultConfig () with Mode = Graphical }
    let script = ScriptGenerator.generate config
    Assert.Contains("[GAME]", script)
    Assert.Contains(config.MapName, script)

[<Fact>]
let ``generate_with_custom_config`` () =
    let config = { EngineConfig.defaultConfig () with
                    MapName = "Custom Map"
                    GameType = "Custom Game"
                    OurSide = "TestSide"
                    OpponentSide = "EnemySide"
                    GameSpeed = 200 }
    let script = ScriptGenerator.generate config
    Assert.Contains("Custom Map", script)
    Assert.Contains("Custom Game", script)
    Assert.Contains("Side=TestSide;", script)
    Assert.Contains("Side=EnemySide;", script)
    Assert.Contains("MinSpeed=200;", script)

[<Fact>]
let ``generate_contains_required_sections`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("[MODOPTIONS]", script)
    Assert.Contains("[MAPOPTIONS]", script)
    Assert.Contains("[PLAYER0]", script)
    Assert.Contains("[AI0]", script)
    Assert.Contains("[AI1]", script)
    Assert.Contains("[TEAM0]", script)
    Assert.Contains("[TEAM1]", script)
    Assert.Contains("[ALLYTEAM0]", script)
    Assert.Contains("[ALLYTEAM1]", script)

[<Fact>]
let ``generate_contains_highbar_ai_config`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("Name=HighBarV2;", script)
    Assert.Contains("ShortName=HighBarV2;", script)
