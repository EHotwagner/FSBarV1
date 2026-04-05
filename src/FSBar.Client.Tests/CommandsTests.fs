module FSBar.Client.Tests.CommandsTests

open Xunit
open FSBar.Client
open Highbar

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

[<Fact>]
let ``AttackCommand_returns_valid_command`` () =
    let cmd = Commands.AttackCommand 1 99
    match cmd.Command with
    | AICommand.CommandCase.Attack a ->
        Assert.Equal(1, a.UnitId)
        Assert.Equal(99, a.TargetUnitId)
    | _ -> Assert.Fail("Expected Attack command")

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

[<Fact>]
let ``GuardCommand_returns_valid_command`` () =
    let cmd = Commands.GuardCommand 1 2
    match cmd.Command with
    | AICommand.CommandCase.Guard g ->
        Assert.Equal(1, g.UnitId)
        Assert.Equal(2, g.GuardUnitId)
    | _ -> Assert.Fail("Expected Guard command")

[<Fact>]
let ``StopCommand_returns_valid_command`` () =
    let cmd = Commands.StopCommand 7
    match cmd.Command with
    | AICommand.CommandCase.Stop s ->
        Assert.Equal(7, s.UnitId)
    | _ -> Assert.Fail("Expected Stop command")

[<Fact>]
let ``RepairCommand_returns_valid_command`` () =
    let cmd = Commands.RepairCommand 1 3
    match cmd.Command with
    | AICommand.CommandCase.Repair r ->
        Assert.Equal(1, r.UnitId)
        Assert.Equal(3, r.RepairUnitId)
    | _ -> Assert.Fail("Expected Repair command")

[<Fact>]
let ``ReclaimUnitCommand_returns_valid_command`` () =
    let cmd = Commands.ReclaimUnitCommand 1 4
    match cmd.Command with
    | AICommand.CommandCase.ReclaimUnit r ->
        Assert.Equal(1, r.UnitId)
        Assert.Equal(4, r.ReclaimUnitId)
    | _ -> Assert.Fail("Expected ReclaimUnit command")

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

[<Fact>]
let ``SelfDestructCommand_returns_valid_command`` () =
    let cmd = Commands.SelfDestructCommand 9
    match cmd.Command with
    | AICommand.CommandCase.SelfDestruct sd ->
        Assert.Equal(9, sd.UnitId)
    | _ -> Assert.Fail("Expected SelfDestruct command")

[<Fact>]
let ``SetWantedMaxSpeedCommand_returns_valid_command`` () =
    let cmd = Commands.SetWantedMaxSpeedCommand 3 5.5f
    match cmd.Command with
    | AICommand.CommandCase.SetWantedMaxSpeed s ->
        Assert.Equal(3, s.UnitId)
        Assert.Equal(5.5f, s.WantedMaxSpeed)
    | _ -> Assert.Fail("Expected SetWantedMaxSpeed command")

[<Fact>]
let ``CustomCommand_returns_valid_command`` () =
    let cmd = Commands.CustomCommand 1 999 [1.0f; 2.0f; 3.0f]
    match cmd.Command with
    | AICommand.CommandCase.Custom c ->
        Assert.Equal(1, c.UnitId)
        Assert.Equal(999, c.CommandId)
        Assert.Equal<float32 list>([1.0f; 2.0f; 3.0f], c.Params)
    | _ -> Assert.Fail("Expected Custom command")

[<Fact>]
let ``SendTextMessageCommand_returns_valid_command`` () =
    let cmd = Commands.SendTextMessageCommand "hello" 5
    match cmd.Command with
    | AICommand.CommandCase.SendTextMessage m ->
        Assert.Equal("hello", m.Text)
        Assert.Equal(5, m.Zone)
    | _ -> Assert.Fail("Expected SendTextMessage command")

[<Fact>]
let ``GiveMeResourceCommand_returns_valid_command`` () =
    let cmd = Commands.GiveMeResourceCommand 0 1000.0f
    match cmd.Command with
    | AICommand.CommandCase.GiveMe g ->
        Assert.Equal(0, g.ResourceId)
        Assert.Equal(1000.0f, g.Amount)
    | _ -> Assert.Fail("Expected GiveMe command")

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

[<Fact>]
let ``CallLuaRulesCommand_returns_valid_command`` () =
    let cmd = Commands.CallLuaRulesCommand "test_data"
    match cmd.Command with
    | AICommand.CommandCase.CallLuaRules r ->
        Assert.Equal("test_data", r.Data)
    | _ -> Assert.Fail("Expected CallLuaRules command")

[<Fact>]
let ``CallLuaUICommand_returns_valid_command`` () =
    let cmd = Commands.CallLuaUICommand "ui_data"
    match cmd.Command with
    | AICommand.CommandCase.CallLuaUi u ->
        Assert.Equal("ui_data", u.Data)
    | _ -> Assert.Fail("Expected CallLuaUi command")

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
