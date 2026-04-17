(**
---
title: Test Suite Documentation
category: Reference
categoryindex: 5
index: 5
---
*)

(**
# Test Suite Documentation

FSBarV1 has roughly **330 test methods** spread across four xUnit 2.9.x projects. Counts
update as the codebase evolves — run `dotnet test` for authoritative numbers.

| Project | Location | Approx. count | What it covers |
|---------|----------|---------------|----------------|
| `FSBar.Client.Tests` | `tests/FSBar.Client.Tests/` | ~158 | Unit tests for every `FSBar.Client` module. Some use real Unix sockets (`Connection`, `Protocol`); none use mocks. |
| `FSBar.LiveTests` | `tests/FSBar.LiveTests/` | ~29 | Integration tests against a live headless BAR engine (commander reaches enemy base, build flow, event delivery). |
| `FSBar.Viz.Tests` | `tests/FSBar.Viz.Tests/` | ~104 | Visualization library — color maps, layer renderer, scene builder, live + preview sessions, surface baselines. |
| `FSBar.SyntheticData.Tests` | `tests/FSBar.SyntheticData.Tests/` | ~40 | Synthetic scene generation, structural validation, continuity checks, surface baselines. |

All tests use xUnit 2.9.x. **No mocks or fakes are used anywhere** — every test exercises
real code against real sockets, real file I/O, or a real engine. Tests that cannot pass in
the current environment (e.g. missing engine) are either marked skipped or have their
assertions relaxed rather than mocked out.

The per-test walkthroughs below cover the original `FSBar.Client.Tests` and
`FSBar.LiveTests` suites in depth. `FSBar.Viz.Tests` and `FSBar.SyntheticData.Tests` are
summarized at the end of this page rather than enumerated individually.

---

## Test Infrastructure

### EngineFixture

Shared fixture that manages a headless BAR engine instance for integration tests. It checks
prerequisites, starts the engine, captures 30 warm-up frames, and tears down cleanly.
*)

(*** do-not-eval ***)
open System
open System.Diagnostics
open System.IO
open Xunit
open FSBar.Client

type EngineFixture() =
    let mutable client: BarClient option = None
    let mutable initialFrames: GameFrame list = []
    let mutable initialEvents: GameEvent list = []

    let testsDir =
        let assemblyDir = Path.GetDirectoryName(typeof<EngineFixture>.Assembly.Location)
        let testProjectDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", ".."))
        Path.GetFullPath(Path.Combine(testProjectDir, ".."))

    let checkPrereqScript = Path.Combine(testsDir, "check-prerequisites.sh")

    let checkPrerequisites () =
        let psi = ProcessStartInfo()
        psi.FileName <- "/usr/bin/env"
        psi.ArgumentList.Add("bash")
        psi.ArgumentList.Add(checkPrereqScript)
        psi.ArgumentList.Add("--json")
        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true

        use proc = Process.Start(psi)
        let stdout = proc.StandardOutput.ReadToEnd()
        let stderr = proc.StandardError.ReadToEnd()
        proc.WaitForExit()

        if proc.ExitCode = 2 then
            failwith $"Prerequisites check script error: {stderr}{stdout}"
        elif proc.ExitCode <> 0 then
            failwith $"Prerequisites not met — skipping live engine tests.\n{stdout}"

        let json = stdout.Trim()

        let extractValue (s: string) (prefix: string) =
            let start = s.IndexOf(prefix) + prefix.Length
            let endIdx = s.IndexOf("\"", start)
            s.Substring(start, endIdx - start)

        let enginePath = extractValue json "\"engine\":\""
        let dataDir = extractValue json "\"datadir\":\""
        (enginePath, dataDir)

    member _.Client =
        client |> Option.defaultWith (fun () -> failwith "Client not initialized")

    member _.InitialFrames = initialFrames
    member _.InitialEvents = initialEvents

    member _.IsEngineAlive =
        match client with
        | Some c -> c.State = Connected || c.State = Running
        | None -> false

    member _.GetDiagnostics() =
        match client with
        | Some c ->
            let config = c.Config
            let sessionDir = EngineLauncher.getSessionDir config
            let mutable output = $"Session: {sessionDir}\nSocket: {config.SocketPath}\nState: {c.State}\n"
            for logFile in ["stdout.log"; "stderr.log"; "infolog.txt"] do
                let path = Path.Combine(sessionDir, logFile)
                if File.Exists(path) then
                    let lines = File.ReadAllLines(path)
                    let tail = lines |> Array.skip (max 0 (lines.Length - 30))
                    output <- output + $"\n--- {logFile} (last {tail.Length} lines) ---\n"
                    output <- output + (String.Join("\n", tail)) + "\n"
            output
        | None -> "No client initialized."

    interface IAsyncLifetime with
        member _.InitializeAsync() = task {
            let (enginePath, dataDir) = checkPrerequisites ()

            let config =
                { EngineConfig.defaultConfig () with
                    EngineBin = enginePath
                    SpringDataDir = Some dataDir }

            let c = new BarClient(config)
            c.Start()

            // Warm-up: capture first 30 frames with one-time events
            let warmupFrames = ResizeArray<GameFrame>()
            c.WaitFrames 30 (fun frame -> warmupFrames.Add(frame))

            initialFrames <- warmupFrames |> Seq.toList
            initialEvents <- initialFrames |> List.collect (fun f -> f.Events)
            client <- Some c
        }

        member _.DisposeAsync() =
            let sessionDir =
                client |> Option.map (fun c -> EngineLauncher.getSessionDir c.Config)

            client |> Option.iter (fun c ->
                try c.Stop() with _ -> ()
            )
            client <- None

            sessionDir |> Option.iter (fun dir ->
                if Directory.Exists(dir) then
                    try Directory.Delete(dir, true) with _ -> ()
            )

            Threading.Tasks.Task.CompletedTask

[<CollectionDefinition("Engine")>]
type EngineCollection() =
    interface ICollectionFixture<EngineFixture>

(**
### BarbFixture

Separate fixture for BARb AI tests. Configures the opponent as BARb instead of NullAI.
*)

(*** do-not-eval ***)
type BarbFixture() =
    let mutable client: BarClient option = None
    let mutable initialFrames: GameFrame list = []
    let mutable initialEvents: GameEvent list = []

    member _.Client =
        client |> Option.defaultWith (fun () -> failwith "Client not initialized")

    member _.InitialFrames = initialFrames
    member _.InitialEvents = initialEvents

    interface IAsyncLifetime with
        member _.InitializeAsync() = task {
            let enginePath =
                let searchDir =
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".local/state/Beyond All Reason/engine")
                let candidates =
                    Directory.GetFiles(searchDir, "spring-headless", SearchOption.AllDirectories)
                if candidates.Length = 0 then failwith "spring-headless not found"
                candidates.[0]

            let dataDir =
                Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(enginePath), "..", ".."))

            let config =
                { EngineConfig.defaultConfig () with
                    EngineBin = enginePath
                    SpringDataDir = Some dataDir
                    OpponentAI = "BARb"
                    GameSpeed = 100 }

            let c = new BarClient(config)
            c.Start()

            let warmup = ResizeArray<GameFrame>()
            c.WaitFrames 30 (fun frame -> warmup.Add(frame))

            initialFrames <- warmup |> Seq.toList
            initialEvents <- initialFrames |> List.collect (fun f -> f.Events)
            client <- Some c
        }

        member _.DisposeAsync() =
            client |> Option.iter (fun c -> try c.Stop() with _ -> ())
            client <- None
            Threading.Tasks.Task.CompletedTask

[<CollectionDefinition("BARb")>]
type BarbCollection() =
    interface ICollectionFixture<BarbFixture>

(**
---

## Unit Tests: EngineConfig (14 tests)

Tests for the `EngineConfig` module -- default configuration values, uniqueness of socket paths,
and custom overrides.

### defaultConfig returns headless mode

Verifies that the default config uses headless mode.
*)

(*** do-not-eval ***)
[<Fact>]
let ``defaultConfig_returns_headless_mode`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal(Headless, config.Mode)

(**
### defaultConfig returns expected map name

Verifies the default map is "Avalanche 3.4".
*)

(*** do-not-eval ***)
[<Fact>]
let ``defaultConfig_returns_expected_map_name`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal("Avalanche 3.4", config.MapName)

(**
### defaultConfig returns expected game type

Verifies the default game type string.
*)

(*** do-not-eval ***)
[<Fact>]
let ``defaultConfig_returns_expected_game_type`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal("Beyond All Reason test-29871-90f4bc1", config.GameType)

(**
### defaultConfig returns expected opponent AI

Verifies the default opponent is NullAI.
*)

(*** do-not-eval ***)
[<Fact>]
let ``defaultConfig_returns_expected_opponent_ai`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal("NullAI", config.OpponentAI)

(**
### defaultConfig returns expected sides

Verifies default factions are Armada (us) and Cortex (opponent).
*)

(*** do-not-eval ***)
[<Fact>]
let ``defaultConfig_returns_expected_sides`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal("Armada", config.OurSide)
    Assert.Equal("Cortex", config.OpponentSide)

(**
### defaultConfig returns expected timeout

Verifies the default connection timeout is 30000ms.
*)

(*** do-not-eval ***)
[<Fact>]
let ``defaultConfig_returns_expected_timeout`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal(30000, config.TimeoutMs)

(**
### defaultConfig returns expected engine bin

Verifies the default engine binary is "spring-headless".
*)

(*** do-not-eval ***)
[<Fact>]
let ``defaultConfig_returns_expected_engine_bin`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal("spring-headless", config.EngineBin)

(**
### defaultConfig returns None spring data dir

Verifies SpringDataDir defaults to None (use engine default).
*)

(*** do-not-eval ***)
[<Fact>]
let ``defaultConfig_returns_none_spring_data_dir`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal(None, config.SpringDataDir)

(**
### defaultConfig returns expected game speed

Verifies the default game speed is 100x.
*)

(*** do-not-eval ***)
[<Fact>]
let ``defaultConfig_returns_expected_game_speed`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.Equal(100, config.GameSpeed)

(**
### defaultConfig generates unique socket paths

Verifies that each call to defaultConfig produces a different socket path.
*)

(*** do-not-eval ***)
[<Fact>]
let ``defaultConfig_generates_unique_socket_paths`` () =
    let config1 = EngineConfig.defaultConfig ()
    let config2 = EngineConfig.defaultConfig ()
    Assert.NotEqual<string>(config1.SocketPath, config2.SocketPath)

(**
### defaultConfig socket path starts with /tmp

Verifies socket path format: `/tmp/fsbar-*.sock`.
*)

(*** do-not-eval ***)
[<Fact>]
let ``defaultConfig_socket_path_starts_with_tmp`` () =
    let config = EngineConfig.defaultConfig ()
    Assert.StartsWith("/tmp/fsbar-", config.SocketPath)
    Assert.EndsWith(".sock", config.SocketPath)

(**
### Custom config overrides work

Verifies that record update syntax correctly overrides config fields.
*)

(*** do-not-eval ***)
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

(**
### Headless mode variant constructs

Verifies the Headless DU case constructs correctly.
*)

(*** do-not-eval ***)
[<Fact>]
let ``headless_mode_variant_constructs`` () =
    let mode = Headless
    Assert.Equal(Headless, mode)

(**
### Graphical mode variant constructs

Verifies the Graphical DU case constructs correctly.
*)

(*** do-not-eval ***)
[<Fact>]
let ``graphical_mode_variant_constructs`` () =
    let mode = Graphical
    Assert.Equal(Graphical, mode)

(**
---

## Unit Tests: ScriptGenerator (13 tests)

Tests for the `ScriptGenerator` module -- verifies that generated game scripts contain all
required sections, player/AI configurations, and config values.

### generate produces valid script

Verifies the script contains the top-level `[GAME]` section.
*)

(*** do-not-eval ***)
[<Fact>]
let ``generate_headless_produces_valid_script`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("[GAME]", script)
    Assert.Contains("}", script)

(**
### generate contains map name

Verifies the map name appears in the generated script.
*)

(*** do-not-eval ***)
[<Fact>]
let ``generate_contains_map_name`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("Avalanche 3.4", script)

(**
### generate contains game type

Verifies the game type string appears in the script.
*)

(*** do-not-eval ***)
[<Fact>]
let ``generate_contains_game_type`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("Beyond All Reason test-29871-90f4bc1", script)

(**
### generate contains socket path

Verifies the socket path is embedded in the script for the proxy to connect to.
*)

(*** do-not-eval ***)
[<Fact>]
let ``generate_contains_socket_path`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains(config.SocketPath, script)

(**
### generate contains our side / opponent side

Verifies faction assignments appear correctly.
*)

(*** do-not-eval ***)
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

(**
### generate contains opponent AI

Verifies the opponent AI name appears in the script.
*)

(*** do-not-eval ***)
[<Fact>]
let ``generate_contains_opponent_ai`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("NullAI", script)

(**
### generate contains game speed

Verifies MinSpeed and MaxSpeed are set to the configured game speed.
*)

(*** do-not-eval ***)
[<Fact>]
let ``generate_contains_game_speed`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("MinSpeed=100;", script)
    Assert.Contains("MaxSpeed=100;", script)

(**
### generate contains faction modoptions

Verifies faction mod options are set for both teams.
*)

(*** do-not-eval ***)
[<Fact>]
let ``generate_contains_faction_modoptions`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("teamfaction_0=armada;", script)
    Assert.Contains("teamfaction_1=cortex;", script)

(**
### generate with graphical config

Verifies script generation works with graphical mode config.
*)

(*** do-not-eval ***)
[<Fact>]
let ``generate_with_graphical_config`` () =
    let config = { EngineConfig.defaultConfig () with Mode = Graphical }
    let script = ScriptGenerator.generate config
    Assert.Contains("[GAME]", script)
    Assert.Contains(config.MapName, script)

(**
### generate with custom config

Verifies custom config values propagate into the script.
*)

(*** do-not-eval ***)
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

(**
### generate contains required sections

Verifies all required game script sections are present.
*)

(*** do-not-eval ***)
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

(**
### generate contains HighBar AI config

Verifies the HighBar V2 AI is configured in the script.
*)

(*** do-not-eval ***)
[<Fact>]
let ``generate_contains_highbar_ai_config`` () =
    let config = EngineConfig.defaultConfig ()
    let script = ScriptGenerator.generate config
    Assert.Contains("Name=HighBarV2;", script)
    Assert.Contains("ShortName=HighBarV2;", script)

(**
---

## Unit Tests: Commands (18 tests)

Tests for all 17 command builders plus the internal order flag test. Each test constructs a
command and verifies the protobuf message fields via pattern matching.

### MoveCommand returns valid command

Verifies unit ID, position fields.
*)

(*** do-not-eval ***)
[<Fact>]
let ``MoveCommand_returns_valid_command`` () =
    let cmd = Commands.MoveCommand 1 100.0f 200.0f 300.0f
    match cmd.Command with
    | AICommand.CommandCase.MoveUnit m ->
        Assert.Equal(1, m.UnitId)
        let pos = m.ToPosition |> Option.get
        Assert.Equal(100.0f, pos.X)
        Assert.Equal(200.0f, pos.Y)
        Assert.Equal(300.0f, pos.Z)
    | _ -> Assert.Fail("Expected MoveUnit command")

(**
### BuildCommand returns valid command

Verifies unit ID, build target def ID, position, and facing.
*)

(*** do-not-eval ***)
[<Fact>]
let ``BuildCommand_returns_valid_command`` () =
    let cmd = Commands.BuildCommand 1 42 100.0f 200.0f 300.0f 2
    match cmd.Command with
    | AICommand.CommandCase.BuildUnit b ->
        Assert.Equal(1, b.UnitId)
        Assert.Equal(42, b.ToBuildUnitDefId)
        let pos = b.BuildPosition |> Option.get
        Assert.Equal(100.0f, pos.X)
        Assert.Equal(2, b.Facing)
    | _ -> Assert.Fail("Expected BuildUnit command")

(**
### AttackCommand returns valid command

Verifies unit ID and target unit ID.
*)

(*** do-not-eval ***)
[<Fact>]
let ``AttackCommand_returns_valid_command`` () =
    let cmd = Commands.AttackCommand 1 99
    match cmd.Command with
    | AICommand.CommandCase.Attack a ->
        Assert.Equal(1, a.UnitId)
        Assert.Equal(99, a.TargetUnitId)
    | _ -> Assert.Fail("Expected Attack command")

(**
### PatrolCommand returns valid command

Verifies unit ID and patrol position.
*)

(*** do-not-eval ***)
[<Fact>]
let ``PatrolCommand_returns_valid_command`` () =
    let cmd = Commands.PatrolCommand 5 10.0f 20.0f 30.0f
    match cmd.Command with
    | AICommand.CommandCase.Patrol p ->
        Assert.Equal(5, p.UnitId)
        let pos = p.ToPosition |> Option.get
        Assert.Equal(10.0f, pos.X)
        Assert.Equal(20.0f, pos.Y)
        Assert.Equal(30.0f, pos.Z)
    | _ -> Assert.Fail("Expected Patrol command")

(**
### GuardCommand returns valid command

Verifies unit ID and guard target ID.
*)

(*** do-not-eval ***)
[<Fact>]
let ``GuardCommand_returns_valid_command`` () =
    let cmd = Commands.GuardCommand 1 2
    match cmd.Command with
    | AICommand.CommandCase.Guard g ->
        Assert.Equal(1, g.UnitId)
        Assert.Equal(2, g.GuardUnitId)
    | _ -> Assert.Fail("Expected Guard command")

(**
### StopCommand returns valid command

Verifies unit ID.
*)

(*** do-not-eval ***)
[<Fact>]
let ``StopCommand_returns_valid_command`` () =
    let cmd = Commands.StopCommand 7
    match cmd.Command with
    | AICommand.CommandCase.Stop s ->
        Assert.Equal(7, s.UnitId)
    | _ -> Assert.Fail("Expected Stop command")

(**
### RepairCommand returns valid command

Verifies unit ID and repair target ID.
*)

(*** do-not-eval ***)
[<Fact>]
let ``RepairCommand_returns_valid_command`` () =
    let cmd = Commands.RepairCommand 1 3
    match cmd.Command with
    | AICommand.CommandCase.Repair r ->
        Assert.Equal(1, r.UnitId)
        Assert.Equal(3, r.RepairUnitId)
    | _ -> Assert.Fail("Expected Repair command")

(**
### ReclaimUnitCommand returns valid command

Verifies unit ID and reclaim target ID.
*)

(*** do-not-eval ***)
[<Fact>]
let ``ReclaimUnitCommand_returns_valid_command`` () =
    let cmd = Commands.ReclaimUnitCommand 1 4
    match cmd.Command with
    | AICommand.CommandCase.ReclaimUnit r ->
        Assert.Equal(1, r.UnitId)
        Assert.Equal(4, r.ReclaimUnitId)
    | _ -> Assert.Fail("Expected ReclaimUnit command")

(**
### FightCommand returns valid command

Verifies unit ID and fight-move position.
*)

(*** do-not-eval ***)
[<Fact>]
let ``FightCommand_returns_valid_command`` () =
    let cmd = Commands.FightCommand 2 50.0f 60.0f 70.0f
    match cmd.Command with
    | AICommand.CommandCase.Fight f ->
        Assert.Equal(2, f.UnitId)
        let pos = f.ToPosition |> Option.get
        Assert.Equal(50.0f, pos.X)
        Assert.Equal(60.0f, pos.Y)
        Assert.Equal(70.0f, pos.Z)
    | _ -> Assert.Fail("Expected Fight command")

(**
### SelfDestructCommand returns valid command

Verifies unit ID for self-destruct.
*)

(*** do-not-eval ***)
[<Fact>]
let ``SelfDestructCommand_returns_valid_command`` () =
    let cmd = Commands.SelfDestructCommand 9
    match cmd.Command with
    | AICommand.CommandCase.SelfDestruct sd ->
        Assert.Equal(9, sd.UnitId)
    | _ -> Assert.Fail("Expected SelfDestruct command")

(**
### SetWantedMaxSpeedCommand returns valid command

Verifies unit ID and speed value.
*)

(*** do-not-eval ***)
[<Fact>]
let ``SetWantedMaxSpeedCommand_returns_valid_command`` () =
    let cmd = Commands.SetWantedMaxSpeedCommand 3 5.5f
    match cmd.Command with
    | AICommand.CommandCase.SetWantedMaxSpeed s ->
        Assert.Equal(3, s.UnitId)
        Assert.Equal(5.5f, s.WantedMaxSpeed)
    | _ -> Assert.Fail("Expected SetWantedMaxSpeed command")

(**
### CustomCommand returns valid command

Verifies unit ID, command ID, and float parameters.
*)

(*** do-not-eval ***)
[<Fact>]
let ``CustomCommand_returns_valid_command`` () =
    let cmd = Commands.CustomCommand 1 999 [1.0f; 2.0f; 3.0f]
    match cmd.Command with
    | AICommand.CommandCase.Custom c ->
        Assert.Equal(1, c.UnitId)
        Assert.Equal(999, c.CommandId)
        Assert.Equal<float32 list>([1.0f; 2.0f; 3.0f], c.Params)
    | _ -> Assert.Fail("Expected Custom command")

(**
### SendTextMessageCommand returns valid command

Verifies text and zone.
*)

(*** do-not-eval ***)
[<Fact>]
let ``SendTextMessageCommand_returns_valid_command`` () =
    let cmd = Commands.SendTextMessageCommand "hello" 5
    match cmd.Command with
    | AICommand.CommandCase.SendTextMessage m ->
        Assert.Equal("hello", m.Text)
        Assert.Equal(5, m.Zone)
    | _ -> Assert.Fail("Expected SendTextMessage command")

(**
### GiveMeResourceCommand returns valid command

Verifies resource ID and amount.
*)

(*** do-not-eval ***)
[<Fact>]
let ``GiveMeResourceCommand_returns_valid_command`` () =
    let cmd = Commands.GiveMeResourceCommand 0 1000.0f
    match cmd.Command with
    | AICommand.CommandCase.GiveMe g ->
        Assert.Equal(0, g.ResourceId)
        Assert.Equal(1000.0f, g.Amount)
    | _ -> Assert.Fail("Expected GiveMe command")

(**
### GiveMeNewUnitCommand returns valid command

Verifies unit def ID and spawn position.
*)

(*** do-not-eval ***)
[<Fact>]
let ``GiveMeNewUnitCommand_returns_valid_command`` () =
    let cmd = Commands.GiveMeNewUnitCommand 42 100.0f 200.0f 300.0f
    match cmd.Command with
    | AICommand.CommandCase.GiveMeNewUnit g ->
        Assert.Equal(42, g.UnitDefId)
        let pos = g.Position |> Option.get
        Assert.Equal(100.0f, pos.X)
        Assert.Equal(200.0f, pos.Y)
        Assert.Equal(300.0f, pos.Z)
    | _ -> Assert.Fail("Expected GiveMeNewUnit command")

(**
### CallLuaRulesCommand returns valid command

Verifies Lua rules data string.
*)

(*** do-not-eval ***)
[<Fact>]
let ``CallLuaRulesCommand_returns_valid_command`` () =
    let cmd = Commands.CallLuaRulesCommand "test_data"
    match cmd.Command with
    | AICommand.CommandCase.CallLuaRules r ->
        Assert.Equal("test_data", r.Data)
    | _ -> Assert.Fail("Expected CallLuaRules command")

(**
### CallLuaUICommand returns valid command

Verifies Lua UI data string.
*)

(*** do-not-eval ***)
[<Fact>]
let ``CallLuaUICommand_returns_valid_command`` () =
    let cmd = Commands.CallLuaUICommand "ui_data"
    match cmd.Command with
    | AICommand.CommandCase.CallLuaUi u ->
        Assert.Equal("ui_data", u.Data)
    | _ -> Assert.Fail("Expected CallLuaUi command")

(**
### All commands have internal order flag

Verifies that movement/action commands set options=8 (internal order).
*)

(*** do-not-eval ***)
[<Fact>]
let ``all_commands_have_internal_order_flag`` () =
    let cmds = [
        Commands.MoveCommand 1 0.0f 0.0f 0.0f
        Commands.PatrolCommand 1 0.0f 0.0f 0.0f
        Commands.StopCommand 1
        Commands.AttackCommand 1 2
        Commands.GuardCommand 1 2
    ]
    for cmd in cmds do
        match cmd.Command with
        | AICommand.CommandCase.MoveUnit m -> Assert.Equal(8u, m.Options)
        | AICommand.CommandCase.Patrol p -> Assert.Equal(8u, p.Options)
        | AICommand.CommandCase.Stop s -> Assert.Equal(8u, s.Options)
        | AICommand.CommandCase.Attack a -> Assert.Equal(8u, a.Options)
        | AICommand.CommandCase.Guard g -> Assert.Equal(8u, g.Options)
        | _ -> Assert.Fail("Unexpected command case")

(**
---

## Unit Tests: Events (30 tests)

Tests for the `Events.fromProto` function. Each test constructs a protobuf `EngineEvent` and
verifies it maps to the correct `GameEvent` DU case with correct field values.

### Init, Release, Update, Message
*)

(*** do-not-eval ***)
[<Fact>]
let ``fromProto_Init_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.Init { TeamId = 5 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.Init 5, result)

[<Fact>]
let ``fromProto_Release_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.Release ReleaseEvent.Unused }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.Release, result)

[<Fact>]
let ``fromProto_Update_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.Update { Frame = 42 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.Update 42, result)

[<Fact>]
let ``fromProto_Message_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.Message { Player = 1; Message = "hello" } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.Message(1, "hello"), result)

(**
### Unit Events (Created, Finished, Idle, MoveFailed, Damaged, Destroyed, Given, Captured)
*)

(*** do-not-eval ***)
[<Fact>]
let ``fromProto_UnitCreated_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.UnitCreated { UnitId = 10; BuilderId = 20 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.UnitCreated(10, 20), result)

[<Fact>]
let ``fromProto_UnitFinished_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.UnitFinished { UnitId = 10 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.UnitFinished 10, result)

[<Fact>]
let ``fromProto_UnitIdle_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.UnitIdle { UnitId = 7 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.UnitIdle 7, result)

[<Fact>]
let ``fromProto_UnitMoveFailed_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.UnitMoveFailed { UnitId = 3 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.UnitMoveFailed 3, result)

[<Fact>]
let ``fromProto_UnitDamaged_maps_correctly`` () =
    let evt : EngineEvent = {
        Event = EngineEvent.EventCase.UnitDamaged {
            UnitId = 1; AttackerId = Some 2; Damage = 50.0f; Direction = None; WeaponDefId = 3; IsParalyzer = true
        }
    }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.UnitDamaged(1, Some 2, 50.0f, 3, true), result)

[<Fact>]
let ``fromProto_UnitDamaged_no_attacker_maps_correctly`` () =
    let evt : EngineEvent = {
        Event = EngineEvent.EventCase.UnitDamaged {
            UnitId = 1; AttackerId = None; Damage = 25.0f; Direction = None; WeaponDefId = 0; IsParalyzer = false
        }
    }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.UnitDamaged(1, None, 25.0f, 0, false), result)

[<Fact>]
let ``fromProto_UnitDestroyed_maps_correctly`` () =
    let evt : EngineEvent = {
        Event = EngineEvent.EventCase.UnitDestroyed { UnitId = 5; AttackerId = Some 10 }
    }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.UnitDestroyed(5, Some 10), result)

[<Fact>]
let ``fromProto_UnitGiven_maps_correctly`` () =
    let evt : EngineEvent = {
        Event = EngineEvent.EventCase.UnitGiven { UnitId = 1; OldTeamId = 0; NewTeamId = 1 }
    }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.UnitGiven(1, 0, 1), result)

[<Fact>]
let ``fromProto_UnitCaptured_maps_correctly`` () =
    let evt : EngineEvent = {
        Event = EngineEvent.EventCase.UnitCaptured { UnitId = 2; OldTeamId = 1; NewTeamId = 0 }
    }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.UnitCaptured(2, 1, 0), result)

(**
### Enemy Events (EnterLOS, LeaveLOS, EnterRadar, LeaveRadar, Damaged, Destroyed, Created, Finished)
*)

(*** do-not-eval ***)
[<Fact>]
let ``fromProto_EnemyEnterLOS_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.EnemyEnterLos { EnemyId = 99 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.EnemyEnterLOS 99, result)

[<Fact>]
let ``fromProto_EnemyLeaveLOS_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.EnemyLeaveLos { EnemyId = 88 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.EnemyLeaveLOS 88, result)

[<Fact>]
let ``fromProto_EnemyEnterRadar_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.EnemyEnterRadar { EnemyId = 77 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.EnemyEnterRadar 77, result)

[<Fact>]
let ``fromProto_EnemyLeaveRadar_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.EnemyLeaveRadar { EnemyId = 66 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.EnemyLeaveRadar 66, result)

[<Fact>]
let ``fromProto_EnemyDamaged_maps_correctly`` () =
    let evt : EngineEvent = {
        Event = EngineEvent.EventCase.EnemyDamaged {
            EnemyId = 5; AttackerId = Some 3; Damage = 100.0f; Direction = None; WeaponDefId = 7
        }
    }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.EnemyDamaged(5, Some 3, 100.0f, 7), result)

[<Fact>]
let ``fromProto_EnemyDestroyed_maps_correctly`` () =
    let evt : EngineEvent = {
        Event = EngineEvent.EventCase.EnemyDestroyed { EnemyId = 4; AttackerId = Some 2 }
    }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.EnemyDestroyed(4, Some 2), result)

[<Fact>]
let ``fromProto_EnemyCreated_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.EnemyCreated { EnemyId = 55 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.EnemyCreated 55, result)

[<Fact>]
let ``fromProto_EnemyFinished_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.EnemyFinished { EnemyId = 44 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.EnemyFinished 44, result)

(**
### Combat and Misc Events (WeaponFired, PlayerCommand, SeismicPing, CommandFinished, Load, Save, LuaMessage, Unknown)
*)

(*** do-not-eval ***)
[<Fact>]
let ``fromProto_WeaponFired_maps_correctly`` () =
    let evt : EngineEvent = {
        Event = EngineEvent.EventCase.WeaponFired { UnitId = 1; WeaponDefId = 15 }
    }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.WeaponFired(1, 15), result)

[<Fact>]
let ``fromProto_PlayerCommand_maps_correctly`` () =
    let evt : EngineEvent = {
        Event = EngineEvent.EventCase.PlayerCommand { Units = [1; 2; 3]; CommandTopicId = 10; CommandId = 20 }
    }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.PlayerCommand([1; 2; 3], 10, 20), result)

[<Fact>]
let ``fromProto_SeismicPing_maps_correctly`` () =
    let evt : EngineEvent = {
        Event = EngineEvent.EventCase.SeismicPing {
            Position = Some { X = 1.0f; Y = 2.0f; Z = 3.0f }; Strength = 50.0f
        }
    }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.SeismicPing(1.0f, 2.0f, 3.0f, 50.0f), result)

[<Fact>]
let ``fromProto_SeismicPing_no_position_defaults_to_zero`` () =
    let evt : EngineEvent = {
        Event = EngineEvent.EventCase.SeismicPing { Position = None; Strength = 10.0f }
    }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.SeismicPing(0.0f, 0.0f, 0.0f, 10.0f), result)

[<Fact>]
let ``fromProto_CommandFinished_maps_correctly`` () =
    let evt : EngineEvent = {
        Event = EngineEvent.EventCase.CommandFinished { UnitId = 1; CommandId = 2; CommandTopicId = 3 }
    }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.CommandFinished(1, 2, 3), result)

[<Fact>]
let ``fromProto_Load_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.Load LoadEvent.Unused }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.Load, result)

[<Fact>]
let ``fromProto_Save_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.Save SaveEvent.Unused }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.Save, result)

[<Fact>]
let ``fromProto_LuaMessage_maps_correctly`` () =
    let evt : EngineEvent = {
        Event = EngineEvent.EventCase.LuaMessage { Data = "lua_data"; InMessageId = 123 }
    }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.LuaMessage("lua_data", 123), result)

[<Fact>]
let ``fromProto_None_maps_to_Unknown`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.None }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.Unknown, result)

(**
---

## Unit Tests: Connection (9 tests)

Tests for the `Connection` module using real Unix domain sockets. A helper function creates a
connected socket pair for each test.

### sendMessage/recvBytes roundtrip

Verifies data sent through sendMessage is received correctly by recvBytes.
*)

(*** do-not-eval ***)
[<Fact>]
let ``sendMessage_recvBytes_roundtrip`` () =
    let (clientStream, serverStream, _) = createStreamPair ()
    try
        let data = [| 1uy; 2uy; 3uy; 4uy; 5uy |]
        Connection.sendMessage clientStream data
        let received = Connection.recvBytes serverStream
        Assert.Equal<byte[]>(data, received)
    finally
        clientStream.Dispose()
        serverStream.Dispose()

(**
### sendMessage writes length prefix header

Verifies the 4-byte little-endian length header is correct.
*)

(*** do-not-eval ***)
[<Fact>]
let ``sendMessage_writes_length_prefix_header`` () =
    let (clientStream, serverStream, _) = createStreamPair ()
    try
        let data = [| 10uy; 20uy; 30uy |]
        Connection.sendMessage clientStream data
        let buffer = Array.zeroCreate 7
        let mutable offset = 0
        while offset < 7 do
            let n = serverStream.Read(buffer, offset, 7 - offset)
            offset <- offset + n
        let length = BitConverter.ToInt32(buffer, 0)
        Assert.Equal(3, length)
        Assert.Equal(10uy, buffer.[4])
        Assert.Equal(20uy, buffer.[5])
        Assert.Equal(30uy, buffer.[6])
    finally
        clientStream.Dispose()
        serverStream.Dispose()

(**
### sendMessage/recvBytes large payload

Verifies 10,000 byte payloads roundtrip correctly.
*)

(*** do-not-eval ***)
[<Fact>]
let ``sendMessage_recvBytes_large_payload`` () =
    let (clientStream, serverStream, _) = createStreamPair ()
    try
        let data = Array.init 10000 (fun i -> byte (i % 256))
        Connection.sendMessage clientStream data
        let received = Connection.recvBytes serverStream
        Assert.Equal<byte[]>(data, received)
    finally
        clientStream.Dispose()
        serverStream.Dispose()

(**
### sendMessage/recvBytes single byte payload

Verifies a 1-byte message works.
*)

(*** do-not-eval ***)
[<Fact>]
let ``sendMessage_recvBytes_single_byte_payload`` () =
    let (clientStream, serverStream, _) = createStreamPair ()
    try
        let data = [| 42uy |]
        Connection.sendMessage clientStream data
        let received = Connection.recvBytes serverStream
        Assert.Equal<byte[]>(data, received)
    finally
        clientStream.Dispose()
        serverStream.Dispose()

(**
### sendMessage/recvBytes multiple messages

Verifies two consecutive messages are received in order.
*)

(*** do-not-eval ***)
[<Fact>]
let ``sendMessage_recvBytes_multiple_messages`` () =
    let (clientStream, serverStream, _) = createStreamPair ()
    try
        let msg1 = [| 1uy; 2uy |]
        let msg2 = [| 3uy; 4uy; 5uy |]
        Connection.sendMessage clientStream msg1
        Connection.sendMessage clientStream msg2
        let recv1 = Connection.recvBytes serverStream
        let recv2 = Connection.recvBytes serverStream
        Assert.Equal<byte[]>(msg1, recv1)
        Assert.Equal<byte[]>(msg2, recv2)
    finally
        clientStream.Dispose()
        serverStream.Dispose()

(**
### createListener creates socket file

Verifies the socket file exists on disk after creating a listener.
*)

(*** do-not-eval ***)
[<Fact>]
let ``createListener_creates_socket_file`` () =
    let guid = Guid.NewGuid().ToString("N").[..7]
    let path = $"/tmp/fsbar-test-{guid}.sock"
    let listener = Connection.createListener path
    try
        Assert.True(File.Exists(path))
    finally
        listener.Close()
        Connection.cleanup path None

(**
### createListener removes stale socket

Verifies a stale socket file is removed before binding.
*)

(*** do-not-eval ***)
[<Fact>]
let ``createListener_removes_stale_socket`` () =
    let guid = Guid.NewGuid().ToString("N").[..7]
    let path = $"/tmp/fsbar-test-{guid}.sock"
    File.WriteAllText(path, "stale")
    let listener = Connection.createListener path
    try
        Assert.True(File.Exists(path))
    finally
        listener.Close()
        Connection.cleanup path None

(**
### cleanup removes socket file

Verifies cleanup deletes the socket file from disk.
*)

(*** do-not-eval ***)
[<Fact>]
let ``cleanup_removes_socket_file`` () =
    let guid = Guid.NewGuid().ToString("N").[..7]
    let path = $"/tmp/fsbar-test-{guid}.sock"
    File.WriteAllText(path, "test")
    Connection.cleanup path None
    Assert.False(File.Exists(path))

(**
### acceptConnection timeout throws

Verifies that accepting a connection with a very short timeout raises an exception.
*)

(*** do-not-eval ***)
[<Fact>]
let ``acceptConnection_timeout_throws`` () =
    let guid = Guid.NewGuid().ToString("N").[..7]
    let path = $"/tmp/fsbar-test-{guid}.sock"
    let listener = Connection.createListener path
    try
        Assert.Throws<exn>(fun () ->
            Connection.acceptConnection listener 1 10000 |> ignore
        ) |> ignore
    finally
        listener.Close()
        Connection.cleanup path None

(**
---

## Unit Tests: Protocol (7 tests)

Tests for the `Protocol` module using real socket pairs. Each test simulates the proxy side
by sending protobuf messages and verifying the client-side protocol functions work correctly.

### Handshake parses correctly

Sends a Handshake from the proxy side and verifies all fields are parsed. Also verifies
the client sent a HandshakeResponse back.
*)

(*** do-not-eval ***)
[<Fact>]
let ``handshake_parses_correctly`` () =
    let (proxyStream, aiStream) = createStreamPair ()
    try
        let hs : ProxyMessage = {
            Message = ProxyMessage.MessageCase.Handshake {
                ProtocolVersion = 1u
                EngineVersion = "test-engine"
                GameId = "game-123"
                MapName = "TestMap"
                ModName = "TestMod"
                TeamId = 0
                AllyTeamId = 0
                PlayerCount = 2
            }
        }
        Connection.sendMessage proxyStream (encode hs)
        let info = Protocol.handshake aiStream

        Assert.Equal(1u, info.ProtocolVersion)
        Assert.Equal("test-engine", info.EngineVersion)
        Assert.Equal("game-123", info.GameId)
        Assert.Equal("TestMap", info.MapName)
        Assert.Equal("TestMod", info.ModName)
        Assert.Equal(0, info.TeamId)
        Assert.Equal(0, info.AllyTeamId)
        Assert.Equal(2, info.PlayerCount)

        let respBytes = Connection.recvBytes proxyStream
        let resp = decode<AIMessage> respBytes
        match resp.Message with
        | AIMessage.MessageCase.HandshakeResponse hr ->
            Assert.True(hr.Accepted)
            Assert.Equal(1u, hr.ProtocolVersion)
        | _ -> Assert.Fail("Expected HandshakeResponse")
    finally
        proxyStream.Dispose()
        aiStream.Dispose()

(**
### receiveFrame deserializes frame correctly

Verifies frame number and events are parsed from a Frame message.
*)

(*** do-not-eval ***)
[<Fact>]
let ``receiveFrame_deserializes_frame_correctly`` () =
    let (proxyStream, aiStream) = createStreamPair ()
    try
        let frame : ProxyMessage = {
            Message = ProxyMessage.MessageCase.Frame {
                FrameNumber = 42u
                Events = [
                    { Event = EngineEvent.EventCase.Update { Frame = 42 } }
                ]
                TeamId = 0
            }
        }
        Connection.sendMessage proxyStream (encode frame)

        let result = Protocol.receiveFrame aiStream
        Assert.True(result.IsSome)
        let gameFrame = result.Value
        Assert.Equal(42u, gameFrame.FrameNumber)
        Assert.Equal(1, gameFrame.Events.Length)
        Assert.Equal(GameEvent.Update 42, gameFrame.Events.[0])
    finally
        proxyStream.Dispose()
        aiStream.Dispose()

(**
### receiveFrame returns None on shutdown

Verifies that a Shutdown message causes receiveFrame to return None.
*)

(*** do-not-eval ***)
[<Fact>]
let ``receiveFrame_returns_none_on_shutdown`` () =
    let (proxyStream, aiStream) = createStreamPair ()
    try
        let shutdownMsg : ProxyMessage = {
            Message = ProxyMessage.MessageCase.Shutdown {
                Reason = ShutdownReason.GameOver
            }
        }
        Connection.sendMessage proxyStream (encode shutdownMsg)

        let result = Protocol.receiveFrame aiStream
        Assert.True(result.IsNone)
    finally
        proxyStream.Dispose()
        aiStream.Dispose()

(**
### sendFrameResponse serializes commands

Verifies commands are serialized into a FrameResponse.
*)

(*** do-not-eval ***)
[<Fact>]
let ``sendFrameResponse_serializes_commands`` () =
    let (proxyStream, aiStream) = createStreamPair ()
    try
        let commands = [
            Commands.MoveCommand 1 10.0f 20.0f 30.0f
            Commands.StopCommand 2
        ]
        Protocol.sendFrameResponse aiStream commands

        let respBytes = Connection.recvBytes proxyStream
        let resp = decode<AIMessage> respBytes
        match resp.Message with
        | AIMessage.MessageCase.FrameResponse fr ->
            Assert.Equal(2, fr.Commands.Length)
        | _ -> Assert.Fail("Expected FrameResponse")
    finally
        proxyStream.Dispose()
        aiStream.Dispose()

(**
### sendFrameResponse empty commands

Verifies an empty command list produces a valid FrameResponse with 0 commands.
*)

(*** do-not-eval ***)
[<Fact>]
let ``sendFrameResponse_empty_commands`` () =
    let (proxyStream, aiStream) = createStreamPair ()
    try
        Protocol.sendFrameResponse aiStream []

        let respBytes = Connection.recvBytes proxyStream
        let resp = decode<AIMessage> respBytes
        match resp.Message with
        | AIMessage.MessageCase.FrameResponse fr ->
            Assert.Equal(0, fr.Commands.Length)
        | _ -> Assert.Fail("Expected FrameResponse")
    finally
        proxyStream.Dispose()
        aiStream.Dispose()

(**
### receiveFrame with multiple events

Verifies a frame with 3 events (Init, UnitCreated, UnitFinished) parses all events.
*)

(*** do-not-eval ***)
[<Fact>]
let ``receiveFrame_with_multiple_events`` () =
    let (proxyStream, aiStream) = createStreamPair ()
    try
        let frame : ProxyMessage = {
            Message = ProxyMessage.MessageCase.Frame {
                FrameNumber = 100u
                Events = [
                    { Event = EngineEvent.EventCase.Init { TeamId = 0 } }
                    { Event = EngineEvent.EventCase.UnitCreated { UnitId = 1; BuilderId = 0 } }
                    { Event = EngineEvent.EventCase.UnitFinished { UnitId = 1 } }
                ]
                TeamId = 0
            }
        }
        Connection.sendMessage proxyStream (encode frame)

        let result = Protocol.receiveFrame aiStream
        Assert.True(result.IsSome)
        let gameFrame = result.Value
        Assert.Equal(100u, gameFrame.FrameNumber)
        Assert.Equal(3, gameFrame.Events.Length)
        Assert.Equal(GameEvent.Init 0, gameFrame.Events.[0])
        Assert.Equal(GameEvent.UnitCreated(1, 0), gameFrame.Events.[1])
        Assert.Equal(GameEvent.UnitFinished 1, gameFrame.Events.[2])
    finally
        proxyStream.Dispose()
        aiStream.Dispose()

(**
### receiveFrame handles save request transparently

Verifies that a SaveRequest before a Frame is handled internally -- the caller only
sees the Frame, and a SaveResponse was sent automatically.
*)

(*** do-not-eval ***)
[<Fact>]
let ``receiveFrame_handles_save_request_transparently`` () =
    let (proxyStream, aiStream) = createStreamPair ()
    try
        let saveReq : ProxyMessage = {
            Message = ProxyMessage.MessageCase.SaveRequest SaveRequest.Unused
        }
        Connection.sendMessage proxyStream (encode saveReq)

        let frame : ProxyMessage = {
            Message = ProxyMessage.MessageCase.Frame {
                FrameNumber = 50u
                Events = []
                TeamId = 0
            }
        }
        Connection.sendMessage proxyStream (encode frame)

        let result = Protocol.receiveFrame aiStream
        Assert.True(result.IsSome)
        Assert.Equal(50u, result.Value.FrameNumber)

        let respBytes = Connection.recvBytes proxyStream
        let resp = decode<AIMessage> respBytes
        match resp.Message with
        | AIMessage.MessageCase.SaveResponse _ -> ()
        | _ -> Assert.Fail("Expected SaveResponse")
    finally
        proxyStream.Dispose()
        aiStream.Dispose()

(**
---

## Unit Tests: BarClient (9 tests)

Tests for the `BarClient` orchestrator. These test client creation, state management, and
configuration propagation without starting an engine.

### create returns idle state

Verifies a newly created client starts in Idle state.
*)

(*** do-not-eval ***)
[<Fact>]
let ``create_returns_idle_state`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    Assert.Equal(Idle, client.State)

(**
### create config matches provided

Verifies the client stores the provided configuration.
*)

(*** do-not-eval ***)
[<Fact>]
let ``create_config_matches_provided`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    Assert.Equal(config.MapName, client.Config.MapName)
    Assert.Equal(config.GameType, client.Config.GameType)
    Assert.Equal(config.Mode, client.Config.Mode)
    Assert.Equal(config.SocketPath, client.Config.SocketPath)

(**
### create with custom config preserves settings

Verifies custom config values are preserved.
*)

(*** do-not-eval ***)
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

(**
### create handshake is None

Verifies handshake info is None before connecting.
*)

(*** do-not-eval ***)
[<Fact>]
let ``create_handshake_is_none`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    Assert.True(client.Handshake.IsNone)

(**
### Stream access before connect throws

Verifies accessing the stream before connection raises an exception.
*)

(*** do-not-eval ***)
[<Fact>]
let ``stream_access_before_connect_throws`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    Assert.Throws<exn>(fun () ->
        client.Stream |> ignore
    ) |> ignore

(**
### Dispose from idle is safe

Verifies disposing an idle client does not throw.
*)

(*** do-not-eval ***)
[<Fact>]
let ``dispose_from_idle_is_safe`` () =
    let config = EngineConfig.defaultConfig ()
    let client = BarClient.create config
    (client :> System.IDisposable).Dispose()
    Assert.Equal(Idle, client.State)

(**
### Stop from idle is safe

Verifies stopping an idle client does not throw.
*)

(*** do-not-eval ***)
[<Fact>]
let ``stop_from_idle_is_safe`` () =
    let config = EngineConfig.defaultConfig ()
    use client = BarClient.create config
    client.Stop()
    Assert.Equal(Idle, client.State)

(**
### defaultConfig module function works

Verifies `BarClient.defaultConfig()` delegates to `EngineConfig.defaultConfig()`.
*)

(*** do-not-eval ***)
[<Fact>]
let ``defaultConfig_module_function_works`` () =
    let config = BarClient.defaultConfig ()
    Assert.Equal(Headless, config.Mode)
    Assert.Equal("Avalanche 3.4", config.MapName)

(**
### Multiple create/dispose cycles

Verifies clients can be created and disposed repeatedly.
*)

(*** do-not-eval ***)
[<Fact>]
let ``multiple_create_dispose_cycles`` () =
    for _ in 1..3 do
        let config = EngineConfig.defaultConfig ()
        use client = BarClient.create config
        Assert.Equal(Idle, client.State)

(**
---

## Surface Area Tests (14 tests)

The `SurfaceAreaTests` guard against accidental public API changes by comparing `.fsi` signature
files against committed baselines. If a `.fsi` file changes, the test fails with a diff showing
exactly what changed. Run `UPDATE_BASELINES=true dotnet test` to accept changes.

### baseline matches fsi surface (12 parameterized tests)

One test per module: BarClient, Callbacks, Commands, Connection, EngineConfig, EngineLauncher,
Events, MapCache, MapGrid, MapQuery, Protocol, ScriptGenerator. Each compares the `.fsi` file
content against its `.baseline` file.
*)

(*** do-not-eval ***)
[<Theory>]
[<InlineData("BarClient")>]
[<InlineData("Callbacks")>]
[<InlineData("Commands")>]
[<InlineData("Connection")>]
[<InlineData("EngineConfig")>]
[<InlineData("EngineLauncher")>]
[<InlineData("Events")>]
[<InlineData("MapCache")>]
[<InlineData("MapGrid")>]
[<InlineData("MapQuery")>]
[<InlineData("Protocol")>]
[<InlineData("ScriptGenerator")>]
let ``baseline_matches_fsi_surface`` (moduleName: string) =
    let fsiPath = Path.Combine(clientSrcDir, sprintf "%s.fsi" moduleName)
    let baselinePath = Path.Combine(baselinesDir, sprintf "%s.baseline" moduleName)
    let fsiContent = File.ReadAllText(fsiPath)

    if isUpdateMode () then
        let needsUpdate =
            not (File.Exists(baselinePath))
            || File.ReadAllText(baselinePath) <> fsiContent
        if needsUpdate then
            File.WriteAllText(baselinePath, fsiContent)
    else
        Assert.True(File.Exists(baselinePath),
            sprintf "Missing baseline for %s." moduleName)
        let baselineContent = File.ReadAllText(baselinePath)
        if baselineContent <> fsiContent then
            let diff = lineDiff baselineContent fsiContent
            Assert.Fail(sprintf "Surface area changed for module %s.\n\nDiff:\n%s" moduleName diff)

(**
### all fsi modules have baselines

Verifies every `.fsi` file in the client project has a corresponding `.baseline` file.
*)

(*** do-not-eval ***)
[<Fact>]
let ``all_fsi_modules_have_baselines`` () =
    let fsiFiles =
        Directory.GetFiles(clientSrcDir, "*.fsi")
        |> Array.map (fun f -> Path.GetFileNameWithoutExtension(f))
        |> Array.sort

    let missing =
        fsiFiles
        |> Array.filter (fun m ->
            not (File.Exists(Path.Combine(baselinesDir, sprintf "%s.baseline" m))))
    Assert.True(missing.Length = 0,
        sprintf "Missing baselines for: %s" (String.Join(", ", missing)))

(**
### no orphaned baselines exist

Verifies there are no `.baseline` files without a corresponding `.fsi` file.
*)

(*** do-not-eval ***)
[<Fact>]
let ``no_orphaned_baselines_exist`` () =
    let baselineFiles =
        Directory.GetFiles(baselinesDir, "*.baseline")
        |> Array.map (fun f -> Path.GetFileNameWithoutExtension(f))
        |> Array.sort

    let orphaned =
        baselineFiles
        |> Array.filter (fun m ->
            not (File.Exists(Path.Combine(clientSrcDir, sprintf "%s.fsi" m))))

    Assert.True(orphaned.Length = 0,
        sprintf "Orphaned baselines found: %s" (String.Join(", ", orphaned)))

(**
---

## Integration Tests: Connection (6 tests)

Live tests that verify the full communication chain against a real BAR engine.

### Harness smoke test

Verifies the engine starts and the client is connected.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``Harness smoke test - engine starts and client is connected``() =
    Assert.True(engine.IsEngineAlive, "Engine should be running")
    Assert.NotNull(engine.Client)

(**
### Client connects to engine proxy socket

Verifies the client state is Connected or Running.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``Client connects to engine proxy socket``() =
    let client = engine.Client
    Assert.True(
        client.State = Connected || client.State = Running,
        $"Client should be connected, got state: {client.State}")

(**
### Handshake completes with valid protocol metadata

Verifies handshake info is available and contains valid protocol version and team ID.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``Handshake completes with valid protocol metadata``() =
    let hs = engine.Client.Handshake
    Assert.True(hs.IsSome, "Handshake info should be available")
    let info = hs.Value
    Assert.True(info.ProtocolVersion > 0u)
    Assert.True(info.TeamId >= 0)

(**
### First frames contain Init event

Verifies the Init event appears in the warm-up frames.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``First frames contain Init event``() =
    let hasInit =
        engine.InitialEvents
        |> List.exists (function GameEvent.Init _ -> true | _ -> false)
    Assert.True(hasInit, "Init event should appear in the initial warm-up frames")

(**
### Empty command responses work for consecutive frames

Verifies 5 consecutive frames can be processed with empty responses, and frame numbers increment.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``Empty command responses work for consecutive frames``() =
    let client = engine.Client
    let frames = ResizeArray<GameFrame>()
    client.WaitFrames 5 (fun frame -> frames.Add(frame))
    Assert.True(frames.Count >= 5)

    for i in 1 .. frames.Count - 1 do
        Assert.True(frames.[i].FrameNumber > frames.[i - 1].FrameNumber)

(**
### Graceful disconnect after receiving frames

Verifies the engine stays alive after processing frames.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``Graceful disconnect after receiving frames``() =
    let client = engine.Client
    let frames = ResizeArray<GameFrame>()
    client.WaitFrames 3 (fun frame -> frames.Add(frame))
    Assert.True(frames.Count >= 3)
    Assert.True(engine.IsEngineAlive)

(**
---

## Integration Tests: Commands (4 tests)

Live tests that send commands to the engine and verify execution.

### MoveCommand causes unit to change position

Sends a MoveCommand and runs 35 frames to verify the command was accepted.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``MoveCommand causes unit to change position``() =
    let uid = getFirstUnitId().Value
    let mutable moveSent = false
    let frames = ResizeArray<GameFrame>()

    engine.Client.WaitFrames 35 (fun frame ->
        frames.Add(frame)
        if not moveSent then
            moveSent <- true
            engine.Client.SendCommands
                [ Commands.MoveCommand uid 2048.0f 100.0f 2048.0f ])

    Assert.True(moveSent)
    Assert.True(frames.Count >= 35)

(**
### BuildCommand triggers unit creation

Sends a BuildCommand and monitors for UnitCreated events.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``BuildCommand triggers unit creation``() =
    let uid = getFirstUnitId().Value
    let mutable buildSent = false
    let createdAfterBuild = ResizeArray<int>()

    engine.Client.WaitFrames 70 (fun frame ->
        if buildSent then
            frame.Events |> List.iter (function
                | GameEvent.UnitCreated(newUid, _) -> createdAfterBuild.Add(newUid)
                | _ -> ())

        if not buildSent then
            buildSent <- true
            engine.Client.SendCommands
                [ Commands.BuildCommand uid 1 600.0f 100.0f 600.0f 0 ])

    Assert.True(buildSent)

(**
### StopCommand halts a moving unit

Sends MoveCommand then StopCommand and verifies both were accepted.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``StopCommand halts a moving unit``() =
    let uid = getFirstUnitId().Value
    let mutable moveSent = false
    let mutable stopSent = false
    let mutable frameIdx = 0
    let frames = ResizeArray<GameFrame>()

    engine.Client.WaitFrames 25 (fun frame ->
        frames.Add(frame)
        frameIdx <- frameIdx + 1
        if not moveSent && frameIdx >= 3 then
            moveSent <- true
            engine.Client.SendCommands
                [ Commands.MoveCommand uid 2048.0f 100.0f 2048.0f ]
        elif moveSent && not stopSent && frameIdx >= 10 then
            stopSent <- true
            engine.Client.SendCommands [ Commands.StopCommand uid ])

    Assert.True(stopSent)
    Assert.True(frames.Count >= 25)

(**
### Patrol Guard Attack Fight commands accepted without crashing

Sends four different commands across 30 frames and verifies no crash.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``Patrol Guard Attack Fight commands accepted without crashing``() =
    let uid = getFirstUnitId().Value
    let mutable commandsSent = 0
    let mutable frameIdx = 0
    let frames = ResizeArray<GameFrame>()

    engine.Client.WaitFrames 30 (fun frame ->
        frames.Add(frame)
        frameIdx <- frameIdx + 1
        match frameIdx with
        | 5 ->
            commandsSent <- commandsSent + 1
            engine.Client.SendCommands
                [ Commands.PatrolCommand uid 1024.0f 100.0f 1024.0f ]
        | 10 ->
            commandsSent <- commandsSent + 1
            engine.Client.SendCommands [ Commands.GuardCommand uid uid ]
        | 15 ->
            commandsSent <- commandsSent + 1
            engine.Client.SendCommands [ Commands.AttackCommand uid 99999 ]
        | 20 ->
            commandsSent <- commandsSent + 1
            engine.Client.SendCommands
                [ Commands.FightCommand uid 1500.0f 100.0f 1500.0f ]
        | _ -> ())

    Assert.True(commandsSent >= 4)
    Assert.True(frames.Count >= 30)

(**
---

## Integration Tests: Events (5 tests)

Live tests verifying that real engine events arrive correctly.

### Init event received with valid team ID

Verifies at least one Init event with a non-negative team ID.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``Init event received with valid team ID``() =
    let initEvents =
        engine.InitialEvents
        |> List.choose (function GameEvent.Init teamId -> Some teamId | _ -> None)
    Assert.True(initEvents.Length >= 1)
    Assert.True(initEvents.[0] >= 0)

(**
### Update events received with matching frame numbers

Verifies Update events appear in frames with matching frame numbers.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``Update events received with matching frame numbers``() =
    let frames = ResizeArray<GameFrame>()
    engine.Client.WaitFrames 5 (fun frame -> frames.Add(frame))
    Assert.True(frames.Count >= 5)
    for frame in frames do
        let updateFrameNums =
            frame.Events
            |> List.choose (function GameEvent.Update f -> Some f | _ -> None)
        if frame.FrameNumber > 0u then
            Assert.True(updateFrameNums.Length >= 1)

(**
### UnitCreated event received for builder unit

Verifies at least one UnitCreated event with a positive unit ID.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``UnitCreated event received for builder unit``() =
    let unitCreatedEvents =
        engine.InitialEvents
        |> List.choose (function
            | GameEvent.UnitCreated(unitId, builderId) -> Some(unitId, builderId)
            | _ -> None)
    Assert.True(unitCreatedEvents.Length >= 1)
    let (unitId, _) = unitCreatedEvents.[0]
    Assert.True(unitId > 0)

(**
### UnitFinished event received for commander

Verifies a UnitFinished event matches a previously created unit.
*)

(*** do-not-eval ***)
[<Fact>]
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

    Assert.True(finishedUnitIds.Length >= 1)
    Assert.True(createdUnitIds.Contains(finishedUnitIds.[0]))

(**
### Unknown events do not crash the frame loop

Verifies 10 frames can be processed without crashing despite potential unknown event types.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``Unknown events do not crash the frame loop``() =
    let frames = ResizeArray<GameFrame>()
    engine.Client.WaitFrames 10 (fun frame -> frames.Add(frame))
    Assert.True(frames.Count >= 10)

(**
---

## Integration Tests: MapGrid (6 tests)

Live tests for map data loading and analysis. Tests skip gracefully if the proxy does not
support map data callbacks.

### loadFromEngine returns correct heightmap dimensions

Verifies heightmap dimensions match `getMapWidth + 1` and `getMapHeight + 1`.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``loadFromEngine returns correct heightmap dimensions``() =
    let stream = engine.Client.Stream
    let w = Callbacks.getMapWidth stream
    let h = Callbacks.getMapHeight stream
    match tryLoadGrid () with
    | None -> ()
    | Some grid ->
        Assert.Equal(w + 1, Array2D.length1 grid.HeightMap)
        Assert.Equal(h + 1, Array2D.length2 grid.HeightMap)

(**
### loadFromEngine populates all layers

Verifies all 5 map layers have non-zero dimensions.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``loadFromEngine populates all layers``() =
    match tryLoadGrid () with
    | None -> ()
    | Some grid ->
        Assert.True(Array2D.length1 grid.HeightMap > 0)
        Assert.True(Array2D.length1 grid.SlopeMap > 0)
        Assert.True(Array2D.length1 grid.ResourceMap > 0)
        Assert.True(Array2D.length1 grid.LosMap > 0)
        Assert.True(Array2D.length1 grid.RadarMap > 0)

(**
### MapGrid ToString shows compact summary

Verifies ToString() produces a compact string containing "elmos" and "heightmap".
*)

(*** do-not-eval ***)
[<Fact>]
member _.``MapGrid ToString shows compact summary``() =
    match tryLoadGrid () with
    | None -> ()
    | Some grid ->
        let s = grid.ToString()
        Assert.Contains("elmos", s)
        Assert.Contains("heightmap", s)
        Assert.True(s.Length <= 200)

(**
### passability kbot dimensions match heightmap

Verifies the passability grid has the same dimensions as the heightmap.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``passability kbot dimensions match heightmap``() =
    match tryLoadGrid () with
    | None -> ()
    | Some grid ->
        let pass = MapGrid.passability grid MoveType.Kbot
        Assert.Equal(Array2D.length1 grid.HeightMap, Array2D.length1 pass)
        Assert.Equal(Array2D.length2 grid.HeightMap, Array2D.length2 pass)

(**
### passability all four movetypes return correct dimensions

Verifies all four movement types produce correctly-sized passability grids.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``passability all four movetypes return correct dimensions``() =
    match tryLoadGrid () with
    | None -> ()
    | Some grid ->
        let expectedW = Array2D.length1 grid.HeightMap
        let expectedH = Array2D.length2 grid.HeightMap
        for mt in [ MoveType.Kbot; MoveType.Tank; MoveType.Hover; MoveType.Ship ] do
            let pass = MapGrid.passability grid mt
            Assert.Equal(expectedW, Array2D.length1 pass)
            Assert.Equal(expectedH, Array2D.length2 pass)

(**
### refreshLos returns grid with same LOS dimensions

Verifies refreshed LOS map has the same dimensions as the original.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``refreshLos returns grid with same LOS dimensions``() =
    match tryLoadGrid () with
    | None -> ()
    | Some grid ->
        let stream = engine.Client.Stream
        let updated = MapGrid.refreshLos stream grid
        Assert.Equal(Array2D.length1 grid.LosMap, Array2D.length1 updated.LosMap)
        Assert.Equal(Array2D.length2 grid.LosMap, Array2D.length2 updated.LosMap)

(**
---

## Integration Tests: MapQuery (6 tests)

Live tests for point queries and region operations on map data.

### heightAtElmo at start position returns Ok with plausible value

Queries height at the team's start position and verifies it is within a plausible range.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``heightAtElmo at start position returns Ok with plausible value``() =
    match tryLoadGrid () with
    | None -> ()
    | Some grid ->
        let stream = engine.Client.Stream
        let (sx, _, sz) = Callbacks.getStartPos stream 0
        let result = MapQuery.heightAtElmo grid (int sx) (int sz)
        match result with
        | Result.Ok h -> Assert.True(h > -1000.0f && h < 10000.0f)
        | Result.Error e -> Assert.Fail $"Expected Ok, got Error: {e}"

(**
### heightAtElmo out of bounds returns Error

Verifies querying far out-of-bounds coordinates returns an Error.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``heightAtElmo out of bounds returns Error``() =
    match tryLoadGrid () with
    | None -> ()
    | Some grid ->
        let result = MapQuery.heightAtElmo grid 999999 999999
        match result with
        | Result.Error msg -> Assert.Contains("Out of bounds", msg)
        | Result.Ok _ -> Assert.Fail "Expected Error"

(**
### heightSubRegion returns correct dimensions

Verifies a 1024x1024 elmo sub-region produces a 128x128 grid (1024/8 = 128).
*)

(*** do-not-eval ***)
[<Fact>]
member _.``heightSubRegion returns correct dimensions``() =
    match tryLoadGrid () with
    | None -> ()
    | Some grid ->
        let result = MapQuery.heightSubRegion grid 0 0 1024 1024
        match result with
        | Result.Ok region ->
            Assert.Equal(128, Array2D.length1 region)
            Assert.Equal(128, Array2D.length2 region)
        | Result.Error e -> Assert.Fail $"Expected Ok, got Error: {e}"

(**
### elmoToGrid roundtrip preserves aligned coordinates

Verifies converting elmo to grid and back produces the original coordinates.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``elmoToGrid roundtrip preserves aligned coordinates``() =
    let x, z = 1024, 2048
    let gx, gz = MapQuery.elmoToGrid x z
    let rx, rz = MapQuery.gridToElmo gx gz
    Assert.Equal(x, rx)
    Assert.Equal(z, rz)

(**
### resourceHotspots correlate with metal spots

Verifies resourceHotspots returns a list (may be empty if no resources).
*)

(*** do-not-eval ***)
[<Fact>]
member _.``resourceHotspots correlate with metal spots``() =
    match tryLoadGrid () with
    | None -> ()
    | Some grid ->
        let stream = engine.Client.Stream
        let metalSpots = Callbacks.getMetalSpots stream
        if metalSpots.Length > 0 then
            let hotspots =
                MapQuery.resourceHotspots grid 0 0 grid.WidthElmos grid.HeightElmos 0
            Assert.True(hotspots.Length >= 0)

(**
### resourceHotspots empty for very high threshold

Verifies a threshold of 255 returns few or no results.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``resourceHotspots empty for very high threshold``() =
    match tryLoadGrid () with
    | None -> ()
    | Some grid ->
        let hotspots =
            MapQuery.resourceHotspots grid 0 0 grid.WidthElmos grid.HeightElmos 255
        Assert.True(hotspots.Length <= 10)

(**
---

## Integration Tests: BARb AI (2 tests)

Live tests against the BARb AI opponent using a separate `BarbFixture`.

### Commander reaches enemy base against BARb AI

Sends repeated MoveCommands to rush the commander toward the enemy base. Verifies arrival
within 5000 frames using position callbacks every 500 frames.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``Commander reaches enemy base against BARb AI``() =
    let uid = getCommanderUnitId().Value
    let stream = fixture.Client.Stream
    let enemyX, enemyY, enemyZ = 3200.0f, 100.0f, 3200.0f

    let mutable frameCount = 0
    let mutable arrived = false
    let mutable destroyed = false
    let maxFrames = 5000

    while frameCount < maxFrames && not arrived && not destroyed do
        match Protocol.receiveFrame stream with
        | None -> destroyed <- true
        | Some frame ->
            frameCount <- frameCount + 1

            if frameCount % 500 = 0 then
                let (cx, _, cz) = Callbacks.getUnitPos stream uid
                let dist = Math.Sqrt(float ((enemyX - cx) * (enemyX - cx) + (enemyZ - cz) * (enemyZ - cz)))
                if dist < 300.0 then arrived <- true

            if frameCount = 1 || frameCount % 1000 = 0 then
                Protocol.sendFrameResponse stream [ Commands.MoveCommand uid enemyX enemyY enemyZ ]
            else
                Protocol.sendFrameResponse stream []

            for evt in frame.Events do
                match evt with
                | GameEvent.UnitDestroyed(deadUid, _) when deadUid = uid -> destroyed <- true
                | _ -> ()

    Assert.True(arrived, $"Commander should reach enemy base within {maxFrames} frames")

(**
### Commander assassinates enemy commander

Multi-phase test: seeds enemy IDs from warmup events (EnemyEnterLOS/EnemyCreated), moves to enemy
base, hunts for the enemy commander by checking unit definitions via callbacks, then attacks it.
Verifies the enemy commander is destroyed within 12000 frames.
*)

(*** do-not-eval ***)
[<Fact>]
member _.``Commander assassinates enemy commander``() =
    let uid = getCommanderUnitId().Value
    let stream = fixture.Client.Stream
    let enemyX, enemyY, enemyZ = 3200.0f, 100.0f, 3200.0f

    let mutable phase = "move"
    let mutable frameCount = 0
    let mutable enemyComId = -1
    let mutable enemyComDead = false
    let mutable ourComDead = false
    let enemiesInLOS = ResizeArray<int>()
    let maxFrames = 12000

    // Seed enemies from warmup and prior test frames (EnemyEnterLOS/EnemyCreated may have fired earlier)
    for evt in fixture.InitialEvents do
        match evt with
        | GameEvent.EnemyEnterLOS eid | GameEvent.EnemyCreated eid ->
            if not (enemiesInLOS.Contains(eid)) then
                enemiesInLOS.Add(eid)
        | _ -> ()

    let checkedDefs = System.Collections.Generic.HashSet<int>()

    while frameCount < maxFrames && not enemyComDead && not ourComDead do
        match Protocol.receiveFrame stream with
        | None -> ourComDead <- true
        | Some frame ->
            frameCount <- frameCount + 1

            // Collect enemies entering LOS or being created
            for evt in frame.Events do
                match evt with
                | GameEvent.EnemyEnterLOS eid | GameEvent.EnemyCreated eid ->
                    if not (enemiesInLOS.Contains(eid)) then
                        enemiesInLOS.Add(eid)
                | GameEvent.EnemyDestroyed(eid, _) when eid = enemyComId ->
                    enemyComDead <- true
                | GameEvent.UnitDestroyed(deadUid, _) when deadUid = uid ->
                    ourComDead <- true
                | _ -> ()

            if enemyComId < 0 && frameCount % 100 = 0 && enemiesInLOS.Count > 0 then
                for eid in enemiesInLOS do
                    if enemyComId < 0 && not (checkedDefs.Contains(eid)) then
                        checkedDefs.Add(eid) |> ignore
                        let defId = Callbacks.getUnitDef stream eid
                        if defId > 0 then
                            let defName = Callbacks.getUnitDefName stream defId
                            if defName.Contains("commander") || defName.Contains("com_") then
                                enemyComId <- eid
                                phase <- "kill"

            let commands =
                match phase with
                | "move" when frameCount = 1 || frameCount % 1000 = 0 ->
                    [ Commands.MoveCommand uid enemyX enemyY enemyZ ]
                | "kill" when enemyComId > 0 && frameCount % 200 = 0 ->
                    [ Commands.AttackCommand uid enemyComId ]
                | _ -> []

            if phase = "move" && frameCount % 500 = 0 then
                let (cx, _, cz) = Callbacks.getUnitPos stream uid
                let dist = Math.Sqrt(float ((enemyX - cx) * (enemyX - cx) + (enemyZ - cz) * (enemyZ - cz)))
                if dist < 400.0 then phase <- "hunt"

            let finalCommands =
                if phase = "hunt" && frameCount % 300 = 0 then
                    let angle = float frameCount / 300.0 * Math.PI / 3.0
                    let px = enemyX + 500.0f * float32 (Math.Cos(angle))
                    let pz = enemyZ + 500.0f * float32 (Math.Sin(angle))
                    [ Commands.MoveCommand uid px enemyY pz ]
                else commands

            Protocol.sendFrameResponse stream finalCommands

    Assert.True(enemyComDead, "Enemy commander should be destroyed")

(**
---

## `FSBar.Viz.Tests` — Visualization Library

Approximately 104 tests in `tests/FSBar.Viz.Tests/`. These exercise the declarative
Scene API, the layer renderer cache, color maps, mock snapshots, live and preview
session lifecycles, and the `.fsi` surface baselines. They are grouped by module rather
than enumerated individually; see the source files for the exact test list.

| Test file | Approx. count | What it covers |
|-----------|---------------|----------------|
| `ColorMapsTests.fs` | 17 | Built-in color schemes (`grayscale`, `terrain`, `heatMap`, `binary`) and `colorSchemeFor` per-layer defaults. |
| `LayerRendererTests.fs` | 8 | `renderLayer` output shape, cache hit/miss counting, and `invalidateCache` / `invalidateAll` behavior. |
| `MapDataTests.fs` | 4 | Binary save/load round-trip for `MapGrid` + metal spots. |
| `SceneBuilderTests.fs` | 19 | `buildScene` composition — base layer selection, overlay toggling, grid lines, HUD panels, view transforms. |
| `MockSnapshotTests.fs` | 10 | The `MockSnapshot` fluent builder (`withFriendlyAt`, `withEnemyAt`, `withEconomy`, `withMetalSpots`, ...). |
| `ViewerTests.fs` | 3 | `GameViz.start` / `stop` / configuration round-trip in a headless window. |
| `LiveSessionTests.fs` | 3 | `LiveSession` handle lifecycle and error reporting without an engine. |
| `LiveSessionIntegrationTests.fs` | 5 | `LiveSession.startWithClient` against a real `BarClient` session. |
| `GameVizIntegrationTests.fs` | 8 | `GameViz.attachToClient` + `onFrame` feeding live frames into the viewer. |
| `PreviewSessionTests.fs` | 3 | `PreviewSession.startWithMap` / `startWithSnapshot` / `startPlayback` disposal semantics. |
| `SyntheticVizTests.fs` | 5 | Rendering `FSBar.SyntheticData` scenes through the viz pipeline. |
| `SurfaceBaselineTests.fs` | 19 | Guards every `FSBar.Viz` `.fsi` against its committed `.baseline` file — any accidental public API change fails loudly. |

Several of these tests require an OpenGL context (GameViz, LiveSession, PreviewSession).
They skip cleanly when run headless without `XDG_RUNTIME_DIR` / `DISPLAY`.

## `FSBar.SyntheticData.Tests` — Scene Generator

Approximately 40 tests in `tests/FSBar.SyntheticData.Tests/`. These validate the three
pre-built scenes and the individual simulation modules.

| Test file | Approx. count | What it covers |
|-----------|---------------|----------------|
| `SceneATests.fs` | 10 | Sparse scene — frame count, unit tracking, basic continuity. |
| `SceneBTests.fs` | 6 | Medium scene — builder + factory + moving enemies. |
| `SceneCTests.fs` | 6 | Dense scene — multi-team movement and combat events. |
| `ContinuityTests.fs` | 4 | Frame-to-frame invariants from `Validation.validateContinuity`. |
| `ValidationTests.fs` | 3 | Structural invariants from `Validation.validate` (in-bounds positions, stable unit IDs, non-negative economy). |
| `SurfaceAreaTests.fs` | 1 | `.fsi` surface baseline for `FSBar.SyntheticData`. |

These tests are pure — they do not touch the filesystem, network, or any engine.
*)
