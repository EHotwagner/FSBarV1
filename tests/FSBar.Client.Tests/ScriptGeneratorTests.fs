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
    Assert.Contains(config.GameType, script)

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
    Assert.Contains($"MinSpeed={config.GameSpeed};", script)
    Assert.Contains($"MaxSpeed={config.GameSpeed};", script)

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

[<Fact>]
let ``generate_default_death_mode_is_com`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("deathmode=com;", script)
    Assert.DoesNotContain("deathmode=neverend;", script)

[<Fact>]
let ``generate_custom_death_mode_is_rendered`` () =
    let config = { EngineConfig.defaultConfig () with DeathMode = "neverend" }
    let script = ScriptGenerator.generate config
    Assert.Contains("deathmode=neverend;", script)

[<Fact>]
let ``generate_with_opponent_ai_options_emits_options_block`` () =
    let config =
        { EngineConfig.defaultConfig () with
            OpponentAIOptions = Map.ofList [ "profile", "easy"; "difficulty", "hard" ] }
    let script = ScriptGenerator.generate config
    let ai1Index = script.IndexOf("[AI1]")
    Assert.True(ai1Index >= 0, "Expected [AI1] section")
    let ai1Section = script.Substring(ai1Index)
    Assert.Contains("[OPTIONS]", ai1Section)
    Assert.Contains("profile=easy;", ai1Section)
    Assert.Contains("difficulty=hard;", ai1Section)

[<Fact>]
let ``generate_with_empty_opponent_ai_options_omits_options_block`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.True(Map.isEmpty config.OpponentAIOptions)
    let script = ScriptGenerator.generate config
    let ai1Index = script.IndexOf("[AI1]")
    let team0Index = script.IndexOf("[TEAM0]")
    Assert.True(ai1Index >= 0 && team0Index > ai1Index, "Expected [AI1] before [TEAM0]")
    let ai1Section = script.Substring(ai1Index, team0Index - ai1Index)
    Assert.DoesNotContain("[OPTIONS]", ai1Section)
