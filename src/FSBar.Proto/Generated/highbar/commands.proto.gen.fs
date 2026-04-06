namespace rec Highbar
open FsGrpc.Protobuf
open Google.Protobuf
#nowarn "40"
#nowarn "1182"


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AICommand =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.OneofConverter<CommandCase>>)>]
    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    [<StructuralEquality;StructuralComparison>]
    [<RequireQualifiedAccess>]
    type CommandCase =
    | None
    /// <summary>Drawing commands (1-3)</summary>
    | [<System.Text.Json.Serialization.JsonPropertyName("drawAddPoint")>] DrawAddPoint of Highbar.DrawAddPointCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("drawAddLine")>] DrawAddLine of Highbar.DrawAddLineCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("drawRemovePoint")>] DrawRemovePoint of Highbar.DrawRemovePointCommand
    /// <summary>Chat/Resources (4-9)</summary>
    | [<System.Text.Json.Serialization.JsonPropertyName("sendTextMessage")>] SendTextMessage of Highbar.SendTextMessageCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setLastPosMessage")>] SetLastPosMessage of Highbar.SetLastPosMessageCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("sendResources")>] SendResources of Highbar.SendResourcesCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setMyIncomeShareDirect")>] SetMyIncomeShareDirect of Highbar.SetMyIncomeShareDirectCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setShareLevel")>] SetShareLevel of Highbar.SetShareLevelCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("pauseTeam")>] PauseTeam of Highbar.PauseTeamCommand
    /// <summary>Groups (12-15)</summary>
    | [<System.Text.Json.Serialization.JsonPropertyName("groupAddUnit")>] GroupAddUnit of Highbar.GroupAddUnitCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("groupRemoveUnit")>] GroupRemoveUnit of Highbar.GroupRemoveUnitCommand
    /// <summary>Pathfinding (16-19)</summary>
    | [<System.Text.Json.Serialization.JsonPropertyName("initPath")>] InitPath of Highbar.InitPathCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("getApproxLength")>] GetApproxLength of Highbar.GetApproxLengthCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("getNextWaypoint")>] GetNextWaypoint of Highbar.GetNextWaypointCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("freePath")>] FreePath of Highbar.FreePathCommand
    /// <summary>Cheats (20, 79)</summary>
    | [<System.Text.Json.Serialization.JsonPropertyName("giveMe")>] GiveMe of Highbar.GiveMeCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("giveMeNewUnit")>] GiveMeNewUnit of Highbar.GiveMeNewUnitCommand
    /// <summary>Lua (21, 96)</summary>
    | [<System.Text.Json.Serialization.JsonPropertyName("callLuaRules")>] CallLuaRules of Highbar.CallLuaRulesCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("callLuaUi")>] CallLuaUi of Highbar.CallLuaUICommand
    /// <summary>Drawer Figures (22-34)</summary>
    | [<System.Text.Json.Serialization.JsonPropertyName("createSplineFigure")>] CreateSplineFigure of Highbar.CreateSplineFigureCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("createLineFigure")>] CreateLineFigure of Highbar.CreateLineFigureCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setFigurePosition")>] SetFigurePosition of Highbar.SetFigurePositionCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setFigureColor")>] SetFigureColor of Highbar.SetFigureColorCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("removeFigure")>] RemoveFigure of Highbar.RemoveFigureCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("drawUnit")>] DrawUnit of Highbar.DrawUnitCommand
    /// <summary>Unit Commands (35-78)</summary>
    | [<System.Text.Json.Serialization.JsonPropertyName("buildUnit")>] BuildUnit of Highbar.BuildUnitCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("stop")>] Stop of Highbar.StopCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("wait")>] Wait of Highbar.WaitCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("timedWait")>] TimedWait of Highbar.TimedWaitCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("squadWait")>] SquadWait of Highbar.SquadWaitCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("deathWait")>] DeathWait of Highbar.DeathWaitCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("gatherWait")>] GatherWait of Highbar.GatherWaitCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("moveUnit")>] MoveUnit of Highbar.MoveUnitCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("patrol")>] Patrol of Highbar.PatrolCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("fight")>] Fight of Highbar.FightCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("attack")>] Attack of Highbar.AttackCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("attackArea")>] AttackArea of Highbar.AttackAreaCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("guard")>] Guard of Highbar.GuardCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("repair")>] Repair of Highbar.RepairCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("reclaimUnit")>] ReclaimUnit of Highbar.ReclaimUnitCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("reclaimArea")>] ReclaimArea of Highbar.ReclaimAreaCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("reclaimInArea")>] ReclaimInArea of Highbar.ReclaimInAreaCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("reclaimFeature")>] ReclaimFeature of Highbar.ReclaimFeatureCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("restoreArea")>] RestoreArea of Highbar.RestoreAreaCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("resurrect")>] Resurrect of Highbar.ResurrectCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("resurrectInArea")>] ResurrectInArea of Highbar.ResurrectInAreaCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("capture")>] Capture of Highbar.CaptureCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("captureArea")>] CaptureArea of Highbar.CaptureAreaCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setBase")>] SetBase of Highbar.SetBaseCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("selfDestruct")>] SelfDestruct of Highbar.SelfDestructCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("loadUnits")>] LoadUnits of Highbar.LoadUnitsCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("loadUnitsArea")>] LoadUnitsArea of Highbar.LoadUnitsAreaCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("loadOnto")>] LoadOnto of Highbar.LoadOntoCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("unloadUnit")>] UnloadUnit of Highbar.UnloadUnitCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("unloadUnitsArea")>] UnloadUnitsArea of Highbar.UnloadUnitsAreaCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setWantedMaxSpeed")>] SetWantedMaxSpeed of Highbar.SetWantedMaxSpeedCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("stockpile")>] Stockpile of Highbar.StockpileCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("dgun")>] Dgun of Highbar.DGunCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("custom")>] Custom of Highbar.CustomCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setOnOff")>] SetOnOff of Highbar.SetOnOffCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setRepeat")>] SetRepeat of Highbar.SetRepeatCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setMoveState")>] SetMoveState of Highbar.SetMoveStateCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setFireState")>] SetFireState of Highbar.SetFireStateCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setTrajectory")>] SetTrajectory of Highbar.SetTrajectoryCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setAutoRepairLevel")>] SetAutoRepairLevel of Highbar.SetAutoRepairLevelCommand
    | [<System.Text.Json.Serialization.JsonPropertyName("setIdleMode")>] SetIdleMode of Highbar.SetIdleModeCommand
    with
        static member OneofCodec : Lazy<OneofCodec<CommandCase>> = 
            lazy
            let DrawAddPoint = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.DrawAddPointCommand> (1, "drawAddPoint")
            let DrawAddLine = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.DrawAddLineCommand> (2, "drawAddLine")
            let DrawRemovePoint = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.DrawRemovePointCommand> (3, "drawRemovePoint")
            let SendTextMessage = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SendTextMessageCommand> (4, "sendTextMessage")
            let SetLastPosMessage = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetLastPosMessageCommand> (5, "setLastPosMessage")
            let SendResources = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SendResourcesCommand> (6, "sendResources")
            let SetMyIncomeShareDirect = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetMyIncomeShareDirectCommand> (7, "setMyIncomeShareDirect")
            let SetShareLevel = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetShareLevelCommand> (8, "setShareLevel")
            let PauseTeam = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.PauseTeamCommand> (9, "pauseTeam")
            let GroupAddUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GroupAddUnitCommand> (12, "groupAddUnit")
            let GroupRemoveUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GroupRemoveUnitCommand> (13, "groupRemoveUnit")
            let InitPath = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.InitPathCommand> (16, "initPath")
            let GetApproxLength = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GetApproxLengthCommand> (17, "getApproxLength")
            let GetNextWaypoint = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GetNextWaypointCommand> (18, "getNextWaypoint")
            let FreePath = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.FreePathCommand> (19, "freePath")
            let GiveMe = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GiveMeCommand> (20, "giveMe")
            let GiveMeNewUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GiveMeNewUnitCommand> (79, "giveMeNewUnit")
            let CallLuaRules = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CallLuaRulesCommand> (21, "callLuaRules")
            let CallLuaUi = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CallLuaUICommand> (96, "callLuaUi")
            let CreateSplineFigure = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CreateSplineFigureCommand> (22, "createSplineFigure")
            let CreateLineFigure = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CreateLineFigureCommand> (23, "createLineFigure")
            let SetFigurePosition = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetFigurePositionCommand> (24, "setFigurePosition")
            let SetFigureColor = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetFigureColorCommand> (25, "setFigureColor")
            let RemoveFigure = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.RemoveFigureCommand> (26, "removeFigure")
            let DrawUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.DrawUnitCommand> (27, "drawUnit")
            let BuildUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.BuildUnitCommand> (35, "buildUnit")
            let Stop = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.StopCommand> (36, "stop")
            let Wait = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.WaitCommand> (37, "wait")
            let TimedWait = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.TimedWaitCommand> (38, "timedWait")
            let SquadWait = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SquadWaitCommand> (39, "squadWait")
            let DeathWait = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.DeathWaitCommand> (40, "deathWait")
            let GatherWait = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GatherWaitCommand> (41, "gatherWait")
            let MoveUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.MoveUnitCommand> (42, "moveUnit")
            let Patrol = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.PatrolCommand> (43, "patrol")
            let Fight = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.FightCommand> (44, "fight")
            let Attack = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.AttackCommand> (45, "attack")
            let AttackArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.AttackAreaCommand> (46, "attackArea")
            let Guard = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GuardCommand> (47, "guard")
            let Repair = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.RepairCommand> (48, "repair")
            let ReclaimUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.ReclaimUnitCommand> (49, "reclaimUnit")
            let ReclaimArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.ReclaimAreaCommand> (50, "reclaimArea")
            let ReclaimInArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.ReclaimInAreaCommand> (51, "reclaimInArea")
            let ReclaimFeature = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.ReclaimFeatureCommand> (52, "reclaimFeature")
            let RestoreArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.RestoreAreaCommand> (53, "restoreArea")
            let Resurrect = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.ResurrectCommand> (54, "resurrect")
            let ResurrectInArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.ResurrectInAreaCommand> (55, "resurrectInArea")
            let Capture = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CaptureCommand> (56, "capture")
            let CaptureArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CaptureAreaCommand> (57, "captureArea")
            let SetBase = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetBaseCommand> (58, "setBase")
            let SelfDestruct = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SelfDestructCommand> (59, "selfDestruct")
            let LoadUnits = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.LoadUnitsCommand> (60, "loadUnits")
            let LoadUnitsArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.LoadUnitsAreaCommand> (61, "loadUnitsArea")
            let LoadOnto = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.LoadOntoCommand> (62, "loadOnto")
            let UnloadUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.UnloadUnitCommand> (63, "unloadUnit")
            let UnloadUnitsArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.UnloadUnitsAreaCommand> (64, "unloadUnitsArea")
            let SetWantedMaxSpeed = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetWantedMaxSpeedCommand> (65, "setWantedMaxSpeed")
            let Stockpile = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.StockpileCommand> (66, "stockpile")
            let Dgun = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.DGunCommand> (67, "dgun")
            let Custom = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CustomCommand> (68, "custom")
            let SetOnOff = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetOnOffCommand> (69, "setOnOff")
            let SetRepeat = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetRepeatCommand> (70, "setRepeat")
            let SetMoveState = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetMoveStateCommand> (71, "setMoveState")
            let SetFireState = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetFireStateCommand> (72, "setFireState")
            let SetTrajectory = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetTrajectoryCommand> (73, "setTrajectory")
            let SetAutoRepairLevel = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetAutoRepairLevelCommand> (74, "setAutoRepairLevel")
            let SetIdleMode = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetIdleModeCommand> (75, "setIdleMode")
            let Command = FieldCodec.Oneof "command" (FSharp.Collections.Map [
                ("drawAddPoint", fun node -> CommandCase.DrawAddPoint (DrawAddPoint.ReadJsonField node))
                ("drawAddLine", fun node -> CommandCase.DrawAddLine (DrawAddLine.ReadJsonField node))
                ("drawRemovePoint", fun node -> CommandCase.DrawRemovePoint (DrawRemovePoint.ReadJsonField node))
                ("sendTextMessage", fun node -> CommandCase.SendTextMessage (SendTextMessage.ReadJsonField node))
                ("setLastPosMessage", fun node -> CommandCase.SetLastPosMessage (SetLastPosMessage.ReadJsonField node))
                ("sendResources", fun node -> CommandCase.SendResources (SendResources.ReadJsonField node))
                ("setMyIncomeShareDirect", fun node -> CommandCase.SetMyIncomeShareDirect (SetMyIncomeShareDirect.ReadJsonField node))
                ("setShareLevel", fun node -> CommandCase.SetShareLevel (SetShareLevel.ReadJsonField node))
                ("pauseTeam", fun node -> CommandCase.PauseTeam (PauseTeam.ReadJsonField node))
                ("groupAddUnit", fun node -> CommandCase.GroupAddUnit (GroupAddUnit.ReadJsonField node))
                ("groupRemoveUnit", fun node -> CommandCase.GroupRemoveUnit (GroupRemoveUnit.ReadJsonField node))
                ("initPath", fun node -> CommandCase.InitPath (InitPath.ReadJsonField node))
                ("getApproxLength", fun node -> CommandCase.GetApproxLength (GetApproxLength.ReadJsonField node))
                ("getNextWaypoint", fun node -> CommandCase.GetNextWaypoint (GetNextWaypoint.ReadJsonField node))
                ("freePath", fun node -> CommandCase.FreePath (FreePath.ReadJsonField node))
                ("giveMe", fun node -> CommandCase.GiveMe (GiveMe.ReadJsonField node))
                ("giveMeNewUnit", fun node -> CommandCase.GiveMeNewUnit (GiveMeNewUnit.ReadJsonField node))
                ("callLuaRules", fun node -> CommandCase.CallLuaRules (CallLuaRules.ReadJsonField node))
                ("callLuaUi", fun node -> CommandCase.CallLuaUi (CallLuaUi.ReadJsonField node))
                ("createSplineFigure", fun node -> CommandCase.CreateSplineFigure (CreateSplineFigure.ReadJsonField node))
                ("createLineFigure", fun node -> CommandCase.CreateLineFigure (CreateLineFigure.ReadJsonField node))
                ("setFigurePosition", fun node -> CommandCase.SetFigurePosition (SetFigurePosition.ReadJsonField node))
                ("setFigureColor", fun node -> CommandCase.SetFigureColor (SetFigureColor.ReadJsonField node))
                ("removeFigure", fun node -> CommandCase.RemoveFigure (RemoveFigure.ReadJsonField node))
                ("drawUnit", fun node -> CommandCase.DrawUnit (DrawUnit.ReadJsonField node))
                ("buildUnit", fun node -> CommandCase.BuildUnit (BuildUnit.ReadJsonField node))
                ("stop", fun node -> CommandCase.Stop (Stop.ReadJsonField node))
                ("wait", fun node -> CommandCase.Wait (Wait.ReadJsonField node))
                ("timedWait", fun node -> CommandCase.TimedWait (TimedWait.ReadJsonField node))
                ("squadWait", fun node -> CommandCase.SquadWait (SquadWait.ReadJsonField node))
                ("deathWait", fun node -> CommandCase.DeathWait (DeathWait.ReadJsonField node))
                ("gatherWait", fun node -> CommandCase.GatherWait (GatherWait.ReadJsonField node))
                ("moveUnit", fun node -> CommandCase.MoveUnit (MoveUnit.ReadJsonField node))
                ("patrol", fun node -> CommandCase.Patrol (Patrol.ReadJsonField node))
                ("fight", fun node -> CommandCase.Fight (Fight.ReadJsonField node))
                ("attack", fun node -> CommandCase.Attack (Attack.ReadJsonField node))
                ("attackArea", fun node -> CommandCase.AttackArea (AttackArea.ReadJsonField node))
                ("guard", fun node -> CommandCase.Guard (Guard.ReadJsonField node))
                ("repair", fun node -> CommandCase.Repair (Repair.ReadJsonField node))
                ("reclaimUnit", fun node -> CommandCase.ReclaimUnit (ReclaimUnit.ReadJsonField node))
                ("reclaimArea", fun node -> CommandCase.ReclaimArea (ReclaimArea.ReadJsonField node))
                ("reclaimInArea", fun node -> CommandCase.ReclaimInArea (ReclaimInArea.ReadJsonField node))
                ("reclaimFeature", fun node -> CommandCase.ReclaimFeature (ReclaimFeature.ReadJsonField node))
                ("restoreArea", fun node -> CommandCase.RestoreArea (RestoreArea.ReadJsonField node))
                ("resurrect", fun node -> CommandCase.Resurrect (Resurrect.ReadJsonField node))
                ("resurrectInArea", fun node -> CommandCase.ResurrectInArea (ResurrectInArea.ReadJsonField node))
                ("capture", fun node -> CommandCase.Capture (Capture.ReadJsonField node))
                ("captureArea", fun node -> CommandCase.CaptureArea (CaptureArea.ReadJsonField node))
                ("setBase", fun node -> CommandCase.SetBase (SetBase.ReadJsonField node))
                ("selfDestruct", fun node -> CommandCase.SelfDestruct (SelfDestruct.ReadJsonField node))
                ("loadUnits", fun node -> CommandCase.LoadUnits (LoadUnits.ReadJsonField node))
                ("loadUnitsArea", fun node -> CommandCase.LoadUnitsArea (LoadUnitsArea.ReadJsonField node))
                ("loadOnto", fun node -> CommandCase.LoadOnto (LoadOnto.ReadJsonField node))
                ("unloadUnit", fun node -> CommandCase.UnloadUnit (UnloadUnit.ReadJsonField node))
                ("unloadUnitsArea", fun node -> CommandCase.UnloadUnitsArea (UnloadUnitsArea.ReadJsonField node))
                ("setWantedMaxSpeed", fun node -> CommandCase.SetWantedMaxSpeed (SetWantedMaxSpeed.ReadJsonField node))
                ("stockpile", fun node -> CommandCase.Stockpile (Stockpile.ReadJsonField node))
                ("dgun", fun node -> CommandCase.Dgun (Dgun.ReadJsonField node))
                ("custom", fun node -> CommandCase.Custom (Custom.ReadJsonField node))
                ("setOnOff", fun node -> CommandCase.SetOnOff (SetOnOff.ReadJsonField node))
                ("setRepeat", fun node -> CommandCase.SetRepeat (SetRepeat.ReadJsonField node))
                ("setMoveState", fun node -> CommandCase.SetMoveState (SetMoveState.ReadJsonField node))
                ("setFireState", fun node -> CommandCase.SetFireState (SetFireState.ReadJsonField node))
                ("setTrajectory", fun node -> CommandCase.SetTrajectory (SetTrajectory.ReadJsonField node))
                ("setAutoRepairLevel", fun node -> CommandCase.SetAutoRepairLevel (SetAutoRepairLevel.ReadJsonField node))
                ("setIdleMode", fun node -> CommandCase.SetIdleMode (SetIdleMode.ReadJsonField node))
                ])
            Command

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Command: OptionBuilder<Highbar.AICommand.CommandCase>
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Command.Set (CommandCase.DrawAddPoint (ValueCodec.Message<Highbar.DrawAddPointCommand>.ReadValue reader))
            | 2 -> x.Command.Set (CommandCase.DrawAddLine (ValueCodec.Message<Highbar.DrawAddLineCommand>.ReadValue reader))
            | 3 -> x.Command.Set (CommandCase.DrawRemovePoint (ValueCodec.Message<Highbar.DrawRemovePointCommand>.ReadValue reader))
            | 4 -> x.Command.Set (CommandCase.SendTextMessage (ValueCodec.Message<Highbar.SendTextMessageCommand>.ReadValue reader))
            | 5 -> x.Command.Set (CommandCase.SetLastPosMessage (ValueCodec.Message<Highbar.SetLastPosMessageCommand>.ReadValue reader))
            | 6 -> x.Command.Set (CommandCase.SendResources (ValueCodec.Message<Highbar.SendResourcesCommand>.ReadValue reader))
            | 7 -> x.Command.Set (CommandCase.SetMyIncomeShareDirect (ValueCodec.Message<Highbar.SetMyIncomeShareDirectCommand>.ReadValue reader))
            | 8 -> x.Command.Set (CommandCase.SetShareLevel (ValueCodec.Message<Highbar.SetShareLevelCommand>.ReadValue reader))
            | 9 -> x.Command.Set (CommandCase.PauseTeam (ValueCodec.Message<Highbar.PauseTeamCommand>.ReadValue reader))
            | 12 -> x.Command.Set (CommandCase.GroupAddUnit (ValueCodec.Message<Highbar.GroupAddUnitCommand>.ReadValue reader))
            | 13 -> x.Command.Set (CommandCase.GroupRemoveUnit (ValueCodec.Message<Highbar.GroupRemoveUnitCommand>.ReadValue reader))
            | 16 -> x.Command.Set (CommandCase.InitPath (ValueCodec.Message<Highbar.InitPathCommand>.ReadValue reader))
            | 17 -> x.Command.Set (CommandCase.GetApproxLength (ValueCodec.Message<Highbar.GetApproxLengthCommand>.ReadValue reader))
            | 18 -> x.Command.Set (CommandCase.GetNextWaypoint (ValueCodec.Message<Highbar.GetNextWaypointCommand>.ReadValue reader))
            | 19 -> x.Command.Set (CommandCase.FreePath (ValueCodec.Message<Highbar.FreePathCommand>.ReadValue reader))
            | 20 -> x.Command.Set (CommandCase.GiveMe (ValueCodec.Message<Highbar.GiveMeCommand>.ReadValue reader))
            | 79 -> x.Command.Set (CommandCase.GiveMeNewUnit (ValueCodec.Message<Highbar.GiveMeNewUnitCommand>.ReadValue reader))
            | 21 -> x.Command.Set (CommandCase.CallLuaRules (ValueCodec.Message<Highbar.CallLuaRulesCommand>.ReadValue reader))
            | 96 -> x.Command.Set (CommandCase.CallLuaUi (ValueCodec.Message<Highbar.CallLuaUICommand>.ReadValue reader))
            | 22 -> x.Command.Set (CommandCase.CreateSplineFigure (ValueCodec.Message<Highbar.CreateSplineFigureCommand>.ReadValue reader))
            | 23 -> x.Command.Set (CommandCase.CreateLineFigure (ValueCodec.Message<Highbar.CreateLineFigureCommand>.ReadValue reader))
            | 24 -> x.Command.Set (CommandCase.SetFigurePosition (ValueCodec.Message<Highbar.SetFigurePositionCommand>.ReadValue reader))
            | 25 -> x.Command.Set (CommandCase.SetFigureColor (ValueCodec.Message<Highbar.SetFigureColorCommand>.ReadValue reader))
            | 26 -> x.Command.Set (CommandCase.RemoveFigure (ValueCodec.Message<Highbar.RemoveFigureCommand>.ReadValue reader))
            | 27 -> x.Command.Set (CommandCase.DrawUnit (ValueCodec.Message<Highbar.DrawUnitCommand>.ReadValue reader))
            | 35 -> x.Command.Set (CommandCase.BuildUnit (ValueCodec.Message<Highbar.BuildUnitCommand>.ReadValue reader))
            | 36 -> x.Command.Set (CommandCase.Stop (ValueCodec.Message<Highbar.StopCommand>.ReadValue reader))
            | 37 -> x.Command.Set (CommandCase.Wait (ValueCodec.Message<Highbar.WaitCommand>.ReadValue reader))
            | 38 -> x.Command.Set (CommandCase.TimedWait (ValueCodec.Message<Highbar.TimedWaitCommand>.ReadValue reader))
            | 39 -> x.Command.Set (CommandCase.SquadWait (ValueCodec.Message<Highbar.SquadWaitCommand>.ReadValue reader))
            | 40 -> x.Command.Set (CommandCase.DeathWait (ValueCodec.Message<Highbar.DeathWaitCommand>.ReadValue reader))
            | 41 -> x.Command.Set (CommandCase.GatherWait (ValueCodec.Message<Highbar.GatherWaitCommand>.ReadValue reader))
            | 42 -> x.Command.Set (CommandCase.MoveUnit (ValueCodec.Message<Highbar.MoveUnitCommand>.ReadValue reader))
            | 43 -> x.Command.Set (CommandCase.Patrol (ValueCodec.Message<Highbar.PatrolCommand>.ReadValue reader))
            | 44 -> x.Command.Set (CommandCase.Fight (ValueCodec.Message<Highbar.FightCommand>.ReadValue reader))
            | 45 -> x.Command.Set (CommandCase.Attack (ValueCodec.Message<Highbar.AttackCommand>.ReadValue reader))
            | 46 -> x.Command.Set (CommandCase.AttackArea (ValueCodec.Message<Highbar.AttackAreaCommand>.ReadValue reader))
            | 47 -> x.Command.Set (CommandCase.Guard (ValueCodec.Message<Highbar.GuardCommand>.ReadValue reader))
            | 48 -> x.Command.Set (CommandCase.Repair (ValueCodec.Message<Highbar.RepairCommand>.ReadValue reader))
            | 49 -> x.Command.Set (CommandCase.ReclaimUnit (ValueCodec.Message<Highbar.ReclaimUnitCommand>.ReadValue reader))
            | 50 -> x.Command.Set (CommandCase.ReclaimArea (ValueCodec.Message<Highbar.ReclaimAreaCommand>.ReadValue reader))
            | 51 -> x.Command.Set (CommandCase.ReclaimInArea (ValueCodec.Message<Highbar.ReclaimInAreaCommand>.ReadValue reader))
            | 52 -> x.Command.Set (CommandCase.ReclaimFeature (ValueCodec.Message<Highbar.ReclaimFeatureCommand>.ReadValue reader))
            | 53 -> x.Command.Set (CommandCase.RestoreArea (ValueCodec.Message<Highbar.RestoreAreaCommand>.ReadValue reader))
            | 54 -> x.Command.Set (CommandCase.Resurrect (ValueCodec.Message<Highbar.ResurrectCommand>.ReadValue reader))
            | 55 -> x.Command.Set (CommandCase.ResurrectInArea (ValueCodec.Message<Highbar.ResurrectInAreaCommand>.ReadValue reader))
            | 56 -> x.Command.Set (CommandCase.Capture (ValueCodec.Message<Highbar.CaptureCommand>.ReadValue reader))
            | 57 -> x.Command.Set (CommandCase.CaptureArea (ValueCodec.Message<Highbar.CaptureAreaCommand>.ReadValue reader))
            | 58 -> x.Command.Set (CommandCase.SetBase (ValueCodec.Message<Highbar.SetBaseCommand>.ReadValue reader))
            | 59 -> x.Command.Set (CommandCase.SelfDestruct (ValueCodec.Message<Highbar.SelfDestructCommand>.ReadValue reader))
            | 60 -> x.Command.Set (CommandCase.LoadUnits (ValueCodec.Message<Highbar.LoadUnitsCommand>.ReadValue reader))
            | 61 -> x.Command.Set (CommandCase.LoadUnitsArea (ValueCodec.Message<Highbar.LoadUnitsAreaCommand>.ReadValue reader))
            | 62 -> x.Command.Set (CommandCase.LoadOnto (ValueCodec.Message<Highbar.LoadOntoCommand>.ReadValue reader))
            | 63 -> x.Command.Set (CommandCase.UnloadUnit (ValueCodec.Message<Highbar.UnloadUnitCommand>.ReadValue reader))
            | 64 -> x.Command.Set (CommandCase.UnloadUnitsArea (ValueCodec.Message<Highbar.UnloadUnitsAreaCommand>.ReadValue reader))
            | 65 -> x.Command.Set (CommandCase.SetWantedMaxSpeed (ValueCodec.Message<Highbar.SetWantedMaxSpeedCommand>.ReadValue reader))
            | 66 -> x.Command.Set (CommandCase.Stockpile (ValueCodec.Message<Highbar.StockpileCommand>.ReadValue reader))
            | 67 -> x.Command.Set (CommandCase.Dgun (ValueCodec.Message<Highbar.DGunCommand>.ReadValue reader))
            | 68 -> x.Command.Set (CommandCase.Custom (ValueCodec.Message<Highbar.CustomCommand>.ReadValue reader))
            | 69 -> x.Command.Set (CommandCase.SetOnOff (ValueCodec.Message<Highbar.SetOnOffCommand>.ReadValue reader))
            | 70 -> x.Command.Set (CommandCase.SetRepeat (ValueCodec.Message<Highbar.SetRepeatCommand>.ReadValue reader))
            | 71 -> x.Command.Set (CommandCase.SetMoveState (ValueCodec.Message<Highbar.SetMoveStateCommand>.ReadValue reader))
            | 72 -> x.Command.Set (CommandCase.SetFireState (ValueCodec.Message<Highbar.SetFireStateCommand>.ReadValue reader))
            | 73 -> x.Command.Set (CommandCase.SetTrajectory (ValueCodec.Message<Highbar.SetTrajectoryCommand>.ReadValue reader))
            | 74 -> x.Command.Set (CommandCase.SetAutoRepairLevel (ValueCodec.Message<Highbar.SetAutoRepairLevelCommand>.ReadValue reader))
            | 75 -> x.Command.Set (CommandCase.SetIdleMode (ValueCodec.Message<Highbar.SetIdleModeCommand>.ReadValue reader))
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.AICommand = {
            Command = x.Command.Build |> (Option.defaultValue CommandCase.None)
            }

/// <summary>Container for all AI command types (97 variants)</summary>
type private _AICommand = AICommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type AICommand = {
    // Field Declarations
    Command: Highbar.AICommand.CommandCase
    }
    with
    static member Proto : Lazy<ProtoDef<AICommand>> =
        lazy
        // Field Definitions
        let DrawAddPoint = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.DrawAddPointCommand> (1, "drawAddPoint")
        let DrawAddLine = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.DrawAddLineCommand> (2, "drawAddLine")
        let DrawRemovePoint = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.DrawRemovePointCommand> (3, "drawRemovePoint")
        let SendTextMessage = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SendTextMessageCommand> (4, "sendTextMessage")
        let SetLastPosMessage = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetLastPosMessageCommand> (5, "setLastPosMessage")
        let SendResources = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SendResourcesCommand> (6, "sendResources")
        let SetMyIncomeShareDirect = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetMyIncomeShareDirectCommand> (7, "setMyIncomeShareDirect")
        let SetShareLevel = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetShareLevelCommand> (8, "setShareLevel")
        let PauseTeam = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.PauseTeamCommand> (9, "pauseTeam")
        let GroupAddUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GroupAddUnitCommand> (12, "groupAddUnit")
        let GroupRemoveUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GroupRemoveUnitCommand> (13, "groupRemoveUnit")
        let InitPath = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.InitPathCommand> (16, "initPath")
        let GetApproxLength = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GetApproxLengthCommand> (17, "getApproxLength")
        let GetNextWaypoint = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GetNextWaypointCommand> (18, "getNextWaypoint")
        let FreePath = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.FreePathCommand> (19, "freePath")
        let GiveMe = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GiveMeCommand> (20, "giveMe")
        let GiveMeNewUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GiveMeNewUnitCommand> (79, "giveMeNewUnit")
        let CallLuaRules = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CallLuaRulesCommand> (21, "callLuaRules")
        let CallLuaUi = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CallLuaUICommand> (96, "callLuaUi")
        let CreateSplineFigure = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CreateSplineFigureCommand> (22, "createSplineFigure")
        let CreateLineFigure = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CreateLineFigureCommand> (23, "createLineFigure")
        let SetFigurePosition = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetFigurePositionCommand> (24, "setFigurePosition")
        let SetFigureColor = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetFigureColorCommand> (25, "setFigureColor")
        let RemoveFigure = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.RemoveFigureCommand> (26, "removeFigure")
        let DrawUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.DrawUnitCommand> (27, "drawUnit")
        let BuildUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.BuildUnitCommand> (35, "buildUnit")
        let Stop = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.StopCommand> (36, "stop")
        let Wait = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.WaitCommand> (37, "wait")
        let TimedWait = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.TimedWaitCommand> (38, "timedWait")
        let SquadWait = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SquadWaitCommand> (39, "squadWait")
        let DeathWait = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.DeathWaitCommand> (40, "deathWait")
        let GatherWait = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GatherWaitCommand> (41, "gatherWait")
        let MoveUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.MoveUnitCommand> (42, "moveUnit")
        let Patrol = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.PatrolCommand> (43, "patrol")
        let Fight = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.FightCommand> (44, "fight")
        let Attack = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.AttackCommand> (45, "attack")
        let AttackArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.AttackAreaCommand> (46, "attackArea")
        let Guard = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.GuardCommand> (47, "guard")
        let Repair = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.RepairCommand> (48, "repair")
        let ReclaimUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.ReclaimUnitCommand> (49, "reclaimUnit")
        let ReclaimArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.ReclaimAreaCommand> (50, "reclaimArea")
        let ReclaimInArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.ReclaimInAreaCommand> (51, "reclaimInArea")
        let ReclaimFeature = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.ReclaimFeatureCommand> (52, "reclaimFeature")
        let RestoreArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.RestoreAreaCommand> (53, "restoreArea")
        let Resurrect = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.ResurrectCommand> (54, "resurrect")
        let ResurrectInArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.ResurrectInAreaCommand> (55, "resurrectInArea")
        let Capture = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CaptureCommand> (56, "capture")
        let CaptureArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CaptureAreaCommand> (57, "captureArea")
        let SetBase = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetBaseCommand> (58, "setBase")
        let SelfDestruct = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SelfDestructCommand> (59, "selfDestruct")
        let LoadUnits = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.LoadUnitsCommand> (60, "loadUnits")
        let LoadUnitsArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.LoadUnitsAreaCommand> (61, "loadUnitsArea")
        let LoadOnto = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.LoadOntoCommand> (62, "loadOnto")
        let UnloadUnit = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.UnloadUnitCommand> (63, "unloadUnit")
        let UnloadUnitsArea = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.UnloadUnitsAreaCommand> (64, "unloadUnitsArea")
        let SetWantedMaxSpeed = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetWantedMaxSpeedCommand> (65, "setWantedMaxSpeed")
        let Stockpile = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.StockpileCommand> (66, "stockpile")
        let Dgun = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.DGunCommand> (67, "dgun")
        let Custom = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.CustomCommand> (68, "custom")
        let SetOnOff = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetOnOffCommand> (69, "setOnOff")
        let SetRepeat = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetRepeatCommand> (70, "setRepeat")
        let SetMoveState = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetMoveStateCommand> (71, "setMoveState")
        let SetFireState = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetFireStateCommand> (72, "setFireState")
        let SetTrajectory = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetTrajectoryCommand> (73, "setTrajectory")
        let SetAutoRepairLevel = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetAutoRepairLevelCommand> (74, "setAutoRepairLevel")
        let SetIdleMode = FieldCodec.OneofCase "command" ValueCodec.Message<Highbar.SetIdleModeCommand> (75, "setIdleMode")
        let Command = FieldCodec.Oneof "command" (FSharp.Collections.Map [
            ("drawAddPoint", fun node -> Highbar.AICommand.CommandCase.DrawAddPoint (DrawAddPoint.ReadJsonField node))
            ("drawAddLine", fun node -> Highbar.AICommand.CommandCase.DrawAddLine (DrawAddLine.ReadJsonField node))
            ("drawRemovePoint", fun node -> Highbar.AICommand.CommandCase.DrawRemovePoint (DrawRemovePoint.ReadJsonField node))
            ("sendTextMessage", fun node -> Highbar.AICommand.CommandCase.SendTextMessage (SendTextMessage.ReadJsonField node))
            ("setLastPosMessage", fun node -> Highbar.AICommand.CommandCase.SetLastPosMessage (SetLastPosMessage.ReadJsonField node))
            ("sendResources", fun node -> Highbar.AICommand.CommandCase.SendResources (SendResources.ReadJsonField node))
            ("setMyIncomeShareDirect", fun node -> Highbar.AICommand.CommandCase.SetMyIncomeShareDirect (SetMyIncomeShareDirect.ReadJsonField node))
            ("setShareLevel", fun node -> Highbar.AICommand.CommandCase.SetShareLevel (SetShareLevel.ReadJsonField node))
            ("pauseTeam", fun node -> Highbar.AICommand.CommandCase.PauseTeam (PauseTeam.ReadJsonField node))
            ("groupAddUnit", fun node -> Highbar.AICommand.CommandCase.GroupAddUnit (GroupAddUnit.ReadJsonField node))
            ("groupRemoveUnit", fun node -> Highbar.AICommand.CommandCase.GroupRemoveUnit (GroupRemoveUnit.ReadJsonField node))
            ("initPath", fun node -> Highbar.AICommand.CommandCase.InitPath (InitPath.ReadJsonField node))
            ("getApproxLength", fun node -> Highbar.AICommand.CommandCase.GetApproxLength (GetApproxLength.ReadJsonField node))
            ("getNextWaypoint", fun node -> Highbar.AICommand.CommandCase.GetNextWaypoint (GetNextWaypoint.ReadJsonField node))
            ("freePath", fun node -> Highbar.AICommand.CommandCase.FreePath (FreePath.ReadJsonField node))
            ("giveMe", fun node -> Highbar.AICommand.CommandCase.GiveMe (GiveMe.ReadJsonField node))
            ("giveMeNewUnit", fun node -> Highbar.AICommand.CommandCase.GiveMeNewUnit (GiveMeNewUnit.ReadJsonField node))
            ("callLuaRules", fun node -> Highbar.AICommand.CommandCase.CallLuaRules (CallLuaRules.ReadJsonField node))
            ("callLuaUi", fun node -> Highbar.AICommand.CommandCase.CallLuaUi (CallLuaUi.ReadJsonField node))
            ("createSplineFigure", fun node -> Highbar.AICommand.CommandCase.CreateSplineFigure (CreateSplineFigure.ReadJsonField node))
            ("createLineFigure", fun node -> Highbar.AICommand.CommandCase.CreateLineFigure (CreateLineFigure.ReadJsonField node))
            ("setFigurePosition", fun node -> Highbar.AICommand.CommandCase.SetFigurePosition (SetFigurePosition.ReadJsonField node))
            ("setFigureColor", fun node -> Highbar.AICommand.CommandCase.SetFigureColor (SetFigureColor.ReadJsonField node))
            ("removeFigure", fun node -> Highbar.AICommand.CommandCase.RemoveFigure (RemoveFigure.ReadJsonField node))
            ("drawUnit", fun node -> Highbar.AICommand.CommandCase.DrawUnit (DrawUnit.ReadJsonField node))
            ("buildUnit", fun node -> Highbar.AICommand.CommandCase.BuildUnit (BuildUnit.ReadJsonField node))
            ("stop", fun node -> Highbar.AICommand.CommandCase.Stop (Stop.ReadJsonField node))
            ("wait", fun node -> Highbar.AICommand.CommandCase.Wait (Wait.ReadJsonField node))
            ("timedWait", fun node -> Highbar.AICommand.CommandCase.TimedWait (TimedWait.ReadJsonField node))
            ("squadWait", fun node -> Highbar.AICommand.CommandCase.SquadWait (SquadWait.ReadJsonField node))
            ("deathWait", fun node -> Highbar.AICommand.CommandCase.DeathWait (DeathWait.ReadJsonField node))
            ("gatherWait", fun node -> Highbar.AICommand.CommandCase.GatherWait (GatherWait.ReadJsonField node))
            ("moveUnit", fun node -> Highbar.AICommand.CommandCase.MoveUnit (MoveUnit.ReadJsonField node))
            ("patrol", fun node -> Highbar.AICommand.CommandCase.Patrol (Patrol.ReadJsonField node))
            ("fight", fun node -> Highbar.AICommand.CommandCase.Fight (Fight.ReadJsonField node))
            ("attack", fun node -> Highbar.AICommand.CommandCase.Attack (Attack.ReadJsonField node))
            ("attackArea", fun node -> Highbar.AICommand.CommandCase.AttackArea (AttackArea.ReadJsonField node))
            ("guard", fun node -> Highbar.AICommand.CommandCase.Guard (Guard.ReadJsonField node))
            ("repair", fun node -> Highbar.AICommand.CommandCase.Repair (Repair.ReadJsonField node))
            ("reclaimUnit", fun node -> Highbar.AICommand.CommandCase.ReclaimUnit (ReclaimUnit.ReadJsonField node))
            ("reclaimArea", fun node -> Highbar.AICommand.CommandCase.ReclaimArea (ReclaimArea.ReadJsonField node))
            ("reclaimInArea", fun node -> Highbar.AICommand.CommandCase.ReclaimInArea (ReclaimInArea.ReadJsonField node))
            ("reclaimFeature", fun node -> Highbar.AICommand.CommandCase.ReclaimFeature (ReclaimFeature.ReadJsonField node))
            ("restoreArea", fun node -> Highbar.AICommand.CommandCase.RestoreArea (RestoreArea.ReadJsonField node))
            ("resurrect", fun node -> Highbar.AICommand.CommandCase.Resurrect (Resurrect.ReadJsonField node))
            ("resurrectInArea", fun node -> Highbar.AICommand.CommandCase.ResurrectInArea (ResurrectInArea.ReadJsonField node))
            ("capture", fun node -> Highbar.AICommand.CommandCase.Capture (Capture.ReadJsonField node))
            ("captureArea", fun node -> Highbar.AICommand.CommandCase.CaptureArea (CaptureArea.ReadJsonField node))
            ("setBase", fun node -> Highbar.AICommand.CommandCase.SetBase (SetBase.ReadJsonField node))
            ("selfDestruct", fun node -> Highbar.AICommand.CommandCase.SelfDestruct (SelfDestruct.ReadJsonField node))
            ("loadUnits", fun node -> Highbar.AICommand.CommandCase.LoadUnits (LoadUnits.ReadJsonField node))
            ("loadUnitsArea", fun node -> Highbar.AICommand.CommandCase.LoadUnitsArea (LoadUnitsArea.ReadJsonField node))
            ("loadOnto", fun node -> Highbar.AICommand.CommandCase.LoadOnto (LoadOnto.ReadJsonField node))
            ("unloadUnit", fun node -> Highbar.AICommand.CommandCase.UnloadUnit (UnloadUnit.ReadJsonField node))
            ("unloadUnitsArea", fun node -> Highbar.AICommand.CommandCase.UnloadUnitsArea (UnloadUnitsArea.ReadJsonField node))
            ("setWantedMaxSpeed", fun node -> Highbar.AICommand.CommandCase.SetWantedMaxSpeed (SetWantedMaxSpeed.ReadJsonField node))
            ("stockpile", fun node -> Highbar.AICommand.CommandCase.Stockpile (Stockpile.ReadJsonField node))
            ("dgun", fun node -> Highbar.AICommand.CommandCase.Dgun (Dgun.ReadJsonField node))
            ("custom", fun node -> Highbar.AICommand.CommandCase.Custom (Custom.ReadJsonField node))
            ("setOnOff", fun node -> Highbar.AICommand.CommandCase.SetOnOff (SetOnOff.ReadJsonField node))
            ("setRepeat", fun node -> Highbar.AICommand.CommandCase.SetRepeat (SetRepeat.ReadJsonField node))
            ("setMoveState", fun node -> Highbar.AICommand.CommandCase.SetMoveState (SetMoveState.ReadJsonField node))
            ("setFireState", fun node -> Highbar.AICommand.CommandCase.SetFireState (SetFireState.ReadJsonField node))
            ("setTrajectory", fun node -> Highbar.AICommand.CommandCase.SetTrajectory (SetTrajectory.ReadJsonField node))
            ("setAutoRepairLevel", fun node -> Highbar.AICommand.CommandCase.SetAutoRepairLevel (SetAutoRepairLevel.ReadJsonField node))
            ("setIdleMode", fun node -> Highbar.AICommand.CommandCase.SetIdleMode (SetIdleMode.ReadJsonField node))
            ])
        // Proto Definition Implementation
        { // ProtoDef<AICommand>
            Name = "AICommand"
            Empty = {
                Command = Highbar.AICommand.CommandCase.None
                }
            Size = fun (m: AICommand) ->
                0
                + match m.Command with
                    | Highbar.AICommand.CommandCase.None -> 0
                    | Highbar.AICommand.CommandCase.DrawAddPoint v -> DrawAddPoint.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.DrawAddLine v -> DrawAddLine.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.DrawRemovePoint v -> DrawRemovePoint.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SendTextMessage v -> SendTextMessage.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetLastPosMessage v -> SetLastPosMessage.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SendResources v -> SendResources.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetMyIncomeShareDirect v -> SetMyIncomeShareDirect.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetShareLevel v -> SetShareLevel.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.PauseTeam v -> PauseTeam.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.GroupAddUnit v -> GroupAddUnit.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.GroupRemoveUnit v -> GroupRemoveUnit.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.InitPath v -> InitPath.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.GetApproxLength v -> GetApproxLength.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.GetNextWaypoint v -> GetNextWaypoint.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.FreePath v -> FreePath.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.GiveMe v -> GiveMe.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.GiveMeNewUnit v -> GiveMeNewUnit.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.CallLuaRules v -> CallLuaRules.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.CallLuaUi v -> CallLuaUi.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.CreateSplineFigure v -> CreateSplineFigure.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.CreateLineFigure v -> CreateLineFigure.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetFigurePosition v -> SetFigurePosition.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetFigureColor v -> SetFigureColor.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.RemoveFigure v -> RemoveFigure.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.DrawUnit v -> DrawUnit.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.BuildUnit v -> BuildUnit.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.Stop v -> Stop.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.Wait v -> Wait.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.TimedWait v -> TimedWait.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SquadWait v -> SquadWait.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.DeathWait v -> DeathWait.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.GatherWait v -> GatherWait.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.MoveUnit v -> MoveUnit.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.Patrol v -> Patrol.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.Fight v -> Fight.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.Attack v -> Attack.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.AttackArea v -> AttackArea.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.Guard v -> Guard.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.Repair v -> Repair.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.ReclaimUnit v -> ReclaimUnit.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.ReclaimArea v -> ReclaimArea.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.ReclaimInArea v -> ReclaimInArea.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.ReclaimFeature v -> ReclaimFeature.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.RestoreArea v -> RestoreArea.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.Resurrect v -> Resurrect.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.ResurrectInArea v -> ResurrectInArea.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.Capture v -> Capture.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.CaptureArea v -> CaptureArea.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetBase v -> SetBase.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SelfDestruct v -> SelfDestruct.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.LoadUnits v -> LoadUnits.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.LoadUnitsArea v -> LoadUnitsArea.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.LoadOnto v -> LoadOnto.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.UnloadUnit v -> UnloadUnit.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.UnloadUnitsArea v -> UnloadUnitsArea.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetWantedMaxSpeed v -> SetWantedMaxSpeed.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.Stockpile v -> Stockpile.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.Dgun v -> Dgun.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.Custom v -> Custom.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetOnOff v -> SetOnOff.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetRepeat v -> SetRepeat.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetMoveState v -> SetMoveState.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetFireState v -> SetFireState.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetTrajectory v -> SetTrajectory.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetAutoRepairLevel v -> SetAutoRepairLevel.CalcFieldSize v
                    | Highbar.AICommand.CommandCase.SetIdleMode v -> SetIdleMode.CalcFieldSize v
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: AICommand) ->
                (match m.Command with
                | Highbar.AICommand.CommandCase.None -> ()
                | Highbar.AICommand.CommandCase.DrawAddPoint v -> DrawAddPoint.WriteField w v
                | Highbar.AICommand.CommandCase.DrawAddLine v -> DrawAddLine.WriteField w v
                | Highbar.AICommand.CommandCase.DrawRemovePoint v -> DrawRemovePoint.WriteField w v
                | Highbar.AICommand.CommandCase.SendTextMessage v -> SendTextMessage.WriteField w v
                | Highbar.AICommand.CommandCase.SetLastPosMessage v -> SetLastPosMessage.WriteField w v
                | Highbar.AICommand.CommandCase.SendResources v -> SendResources.WriteField w v
                | Highbar.AICommand.CommandCase.SetMyIncomeShareDirect v -> SetMyIncomeShareDirect.WriteField w v
                | Highbar.AICommand.CommandCase.SetShareLevel v -> SetShareLevel.WriteField w v
                | Highbar.AICommand.CommandCase.PauseTeam v -> PauseTeam.WriteField w v
                | Highbar.AICommand.CommandCase.GroupAddUnit v -> GroupAddUnit.WriteField w v
                | Highbar.AICommand.CommandCase.GroupRemoveUnit v -> GroupRemoveUnit.WriteField w v
                | Highbar.AICommand.CommandCase.InitPath v -> InitPath.WriteField w v
                | Highbar.AICommand.CommandCase.GetApproxLength v -> GetApproxLength.WriteField w v
                | Highbar.AICommand.CommandCase.GetNextWaypoint v -> GetNextWaypoint.WriteField w v
                | Highbar.AICommand.CommandCase.FreePath v -> FreePath.WriteField w v
                | Highbar.AICommand.CommandCase.GiveMe v -> GiveMe.WriteField w v
                | Highbar.AICommand.CommandCase.GiveMeNewUnit v -> GiveMeNewUnit.WriteField w v
                | Highbar.AICommand.CommandCase.CallLuaRules v -> CallLuaRules.WriteField w v
                | Highbar.AICommand.CommandCase.CallLuaUi v -> CallLuaUi.WriteField w v
                | Highbar.AICommand.CommandCase.CreateSplineFigure v -> CreateSplineFigure.WriteField w v
                | Highbar.AICommand.CommandCase.CreateLineFigure v -> CreateLineFigure.WriteField w v
                | Highbar.AICommand.CommandCase.SetFigurePosition v -> SetFigurePosition.WriteField w v
                | Highbar.AICommand.CommandCase.SetFigureColor v -> SetFigureColor.WriteField w v
                | Highbar.AICommand.CommandCase.RemoveFigure v -> RemoveFigure.WriteField w v
                | Highbar.AICommand.CommandCase.DrawUnit v -> DrawUnit.WriteField w v
                | Highbar.AICommand.CommandCase.BuildUnit v -> BuildUnit.WriteField w v
                | Highbar.AICommand.CommandCase.Stop v -> Stop.WriteField w v
                | Highbar.AICommand.CommandCase.Wait v -> Wait.WriteField w v
                | Highbar.AICommand.CommandCase.TimedWait v -> TimedWait.WriteField w v
                | Highbar.AICommand.CommandCase.SquadWait v -> SquadWait.WriteField w v
                | Highbar.AICommand.CommandCase.DeathWait v -> DeathWait.WriteField w v
                | Highbar.AICommand.CommandCase.GatherWait v -> GatherWait.WriteField w v
                | Highbar.AICommand.CommandCase.MoveUnit v -> MoveUnit.WriteField w v
                | Highbar.AICommand.CommandCase.Patrol v -> Patrol.WriteField w v
                | Highbar.AICommand.CommandCase.Fight v -> Fight.WriteField w v
                | Highbar.AICommand.CommandCase.Attack v -> Attack.WriteField w v
                | Highbar.AICommand.CommandCase.AttackArea v -> AttackArea.WriteField w v
                | Highbar.AICommand.CommandCase.Guard v -> Guard.WriteField w v
                | Highbar.AICommand.CommandCase.Repair v -> Repair.WriteField w v
                | Highbar.AICommand.CommandCase.ReclaimUnit v -> ReclaimUnit.WriteField w v
                | Highbar.AICommand.CommandCase.ReclaimArea v -> ReclaimArea.WriteField w v
                | Highbar.AICommand.CommandCase.ReclaimInArea v -> ReclaimInArea.WriteField w v
                | Highbar.AICommand.CommandCase.ReclaimFeature v -> ReclaimFeature.WriteField w v
                | Highbar.AICommand.CommandCase.RestoreArea v -> RestoreArea.WriteField w v
                | Highbar.AICommand.CommandCase.Resurrect v -> Resurrect.WriteField w v
                | Highbar.AICommand.CommandCase.ResurrectInArea v -> ResurrectInArea.WriteField w v
                | Highbar.AICommand.CommandCase.Capture v -> Capture.WriteField w v
                | Highbar.AICommand.CommandCase.CaptureArea v -> CaptureArea.WriteField w v
                | Highbar.AICommand.CommandCase.SetBase v -> SetBase.WriteField w v
                | Highbar.AICommand.CommandCase.SelfDestruct v -> SelfDestruct.WriteField w v
                | Highbar.AICommand.CommandCase.LoadUnits v -> LoadUnits.WriteField w v
                | Highbar.AICommand.CommandCase.LoadUnitsArea v -> LoadUnitsArea.WriteField w v
                | Highbar.AICommand.CommandCase.LoadOnto v -> LoadOnto.WriteField w v
                | Highbar.AICommand.CommandCase.UnloadUnit v -> UnloadUnit.WriteField w v
                | Highbar.AICommand.CommandCase.UnloadUnitsArea v -> UnloadUnitsArea.WriteField w v
                | Highbar.AICommand.CommandCase.SetWantedMaxSpeed v -> SetWantedMaxSpeed.WriteField w v
                | Highbar.AICommand.CommandCase.Stockpile v -> Stockpile.WriteField w v
                | Highbar.AICommand.CommandCase.Dgun v -> Dgun.WriteField w v
                | Highbar.AICommand.CommandCase.Custom v -> Custom.WriteField w v
                | Highbar.AICommand.CommandCase.SetOnOff v -> SetOnOff.WriteField w v
                | Highbar.AICommand.CommandCase.SetRepeat v -> SetRepeat.WriteField w v
                | Highbar.AICommand.CommandCase.SetMoveState v -> SetMoveState.WriteField w v
                | Highbar.AICommand.CommandCase.SetFireState v -> SetFireState.WriteField w v
                | Highbar.AICommand.CommandCase.SetTrajectory v -> SetTrajectory.WriteField w v
                | Highbar.AICommand.CommandCase.SetAutoRepairLevel v -> SetAutoRepairLevel.WriteField w v
                | Highbar.AICommand.CommandCase.SetIdleMode v -> SetIdleMode.WriteField w v
                )
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.AICommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeCommandNone = Command.WriteJsonNoneCase o
                let writeDrawAddPoint = DrawAddPoint.WriteJsonField o
                let writeDrawAddLine = DrawAddLine.WriteJsonField o
                let writeDrawRemovePoint = DrawRemovePoint.WriteJsonField o
                let writeSendTextMessage = SendTextMessage.WriteJsonField o
                let writeSetLastPosMessage = SetLastPosMessage.WriteJsonField o
                let writeSendResources = SendResources.WriteJsonField o
                let writeSetMyIncomeShareDirect = SetMyIncomeShareDirect.WriteJsonField o
                let writeSetShareLevel = SetShareLevel.WriteJsonField o
                let writePauseTeam = PauseTeam.WriteJsonField o
                let writeGroupAddUnit = GroupAddUnit.WriteJsonField o
                let writeGroupRemoveUnit = GroupRemoveUnit.WriteJsonField o
                let writeInitPath = InitPath.WriteJsonField o
                let writeGetApproxLength = GetApproxLength.WriteJsonField o
                let writeGetNextWaypoint = GetNextWaypoint.WriteJsonField o
                let writeFreePath = FreePath.WriteJsonField o
                let writeGiveMe = GiveMe.WriteJsonField o
                let writeGiveMeNewUnit = GiveMeNewUnit.WriteJsonField o
                let writeCallLuaRules = CallLuaRules.WriteJsonField o
                let writeCallLuaUi = CallLuaUi.WriteJsonField o
                let writeCreateSplineFigure = CreateSplineFigure.WriteJsonField o
                let writeCreateLineFigure = CreateLineFigure.WriteJsonField o
                let writeSetFigurePosition = SetFigurePosition.WriteJsonField o
                let writeSetFigureColor = SetFigureColor.WriteJsonField o
                let writeRemoveFigure = RemoveFigure.WriteJsonField o
                let writeDrawUnit = DrawUnit.WriteJsonField o
                let writeBuildUnit = BuildUnit.WriteJsonField o
                let writeStop = Stop.WriteJsonField o
                let writeWait = Wait.WriteJsonField o
                let writeTimedWait = TimedWait.WriteJsonField o
                let writeSquadWait = SquadWait.WriteJsonField o
                let writeDeathWait = DeathWait.WriteJsonField o
                let writeGatherWait = GatherWait.WriteJsonField o
                let writeMoveUnit = MoveUnit.WriteJsonField o
                let writePatrol = Patrol.WriteJsonField o
                let writeFight = Fight.WriteJsonField o
                let writeAttack = Attack.WriteJsonField o
                let writeAttackArea = AttackArea.WriteJsonField o
                let writeGuard = Guard.WriteJsonField o
                let writeRepair = Repair.WriteJsonField o
                let writeReclaimUnit = ReclaimUnit.WriteJsonField o
                let writeReclaimArea = ReclaimArea.WriteJsonField o
                let writeReclaimInArea = ReclaimInArea.WriteJsonField o
                let writeReclaimFeature = ReclaimFeature.WriteJsonField o
                let writeRestoreArea = RestoreArea.WriteJsonField o
                let writeResurrect = Resurrect.WriteJsonField o
                let writeResurrectInArea = ResurrectInArea.WriteJsonField o
                let writeCapture = Capture.WriteJsonField o
                let writeCaptureArea = CaptureArea.WriteJsonField o
                let writeSetBase = SetBase.WriteJsonField o
                let writeSelfDestruct = SelfDestruct.WriteJsonField o
                let writeLoadUnits = LoadUnits.WriteJsonField o
                let writeLoadUnitsArea = LoadUnitsArea.WriteJsonField o
                let writeLoadOnto = LoadOnto.WriteJsonField o
                let writeUnloadUnit = UnloadUnit.WriteJsonField o
                let writeUnloadUnitsArea = UnloadUnitsArea.WriteJsonField o
                let writeSetWantedMaxSpeed = SetWantedMaxSpeed.WriteJsonField o
                let writeStockpile = Stockpile.WriteJsonField o
                let writeDgun = Dgun.WriteJsonField o
                let writeCustom = Custom.WriteJsonField o
                let writeSetOnOff = SetOnOff.WriteJsonField o
                let writeSetRepeat = SetRepeat.WriteJsonField o
                let writeSetMoveState = SetMoveState.WriteJsonField o
                let writeSetFireState = SetFireState.WriteJsonField o
                let writeSetTrajectory = SetTrajectory.WriteJsonField o
                let writeSetAutoRepairLevel = SetAutoRepairLevel.WriteJsonField o
                let writeSetIdleMode = SetIdleMode.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: AICommand) =
                    (match m.Command with
                    | Highbar.AICommand.CommandCase.None -> writeCommandNone w
                    | Highbar.AICommand.CommandCase.DrawAddPoint v -> writeDrawAddPoint w v
                    | Highbar.AICommand.CommandCase.DrawAddLine v -> writeDrawAddLine w v
                    | Highbar.AICommand.CommandCase.DrawRemovePoint v -> writeDrawRemovePoint w v
                    | Highbar.AICommand.CommandCase.SendTextMessage v -> writeSendTextMessage w v
                    | Highbar.AICommand.CommandCase.SetLastPosMessage v -> writeSetLastPosMessage w v
                    | Highbar.AICommand.CommandCase.SendResources v -> writeSendResources w v
                    | Highbar.AICommand.CommandCase.SetMyIncomeShareDirect v -> writeSetMyIncomeShareDirect w v
                    | Highbar.AICommand.CommandCase.SetShareLevel v -> writeSetShareLevel w v
                    | Highbar.AICommand.CommandCase.PauseTeam v -> writePauseTeam w v
                    | Highbar.AICommand.CommandCase.GroupAddUnit v -> writeGroupAddUnit w v
                    | Highbar.AICommand.CommandCase.GroupRemoveUnit v -> writeGroupRemoveUnit w v
                    | Highbar.AICommand.CommandCase.InitPath v -> writeInitPath w v
                    | Highbar.AICommand.CommandCase.GetApproxLength v -> writeGetApproxLength w v
                    | Highbar.AICommand.CommandCase.GetNextWaypoint v -> writeGetNextWaypoint w v
                    | Highbar.AICommand.CommandCase.FreePath v -> writeFreePath w v
                    | Highbar.AICommand.CommandCase.GiveMe v -> writeGiveMe w v
                    | Highbar.AICommand.CommandCase.GiveMeNewUnit v -> writeGiveMeNewUnit w v
                    | Highbar.AICommand.CommandCase.CallLuaRules v -> writeCallLuaRules w v
                    | Highbar.AICommand.CommandCase.CallLuaUi v -> writeCallLuaUi w v
                    | Highbar.AICommand.CommandCase.CreateSplineFigure v -> writeCreateSplineFigure w v
                    | Highbar.AICommand.CommandCase.CreateLineFigure v -> writeCreateLineFigure w v
                    | Highbar.AICommand.CommandCase.SetFigurePosition v -> writeSetFigurePosition w v
                    | Highbar.AICommand.CommandCase.SetFigureColor v -> writeSetFigureColor w v
                    | Highbar.AICommand.CommandCase.RemoveFigure v -> writeRemoveFigure w v
                    | Highbar.AICommand.CommandCase.DrawUnit v -> writeDrawUnit w v
                    | Highbar.AICommand.CommandCase.BuildUnit v -> writeBuildUnit w v
                    | Highbar.AICommand.CommandCase.Stop v -> writeStop w v
                    | Highbar.AICommand.CommandCase.Wait v -> writeWait w v
                    | Highbar.AICommand.CommandCase.TimedWait v -> writeTimedWait w v
                    | Highbar.AICommand.CommandCase.SquadWait v -> writeSquadWait w v
                    | Highbar.AICommand.CommandCase.DeathWait v -> writeDeathWait w v
                    | Highbar.AICommand.CommandCase.GatherWait v -> writeGatherWait w v
                    | Highbar.AICommand.CommandCase.MoveUnit v -> writeMoveUnit w v
                    | Highbar.AICommand.CommandCase.Patrol v -> writePatrol w v
                    | Highbar.AICommand.CommandCase.Fight v -> writeFight w v
                    | Highbar.AICommand.CommandCase.Attack v -> writeAttack w v
                    | Highbar.AICommand.CommandCase.AttackArea v -> writeAttackArea w v
                    | Highbar.AICommand.CommandCase.Guard v -> writeGuard w v
                    | Highbar.AICommand.CommandCase.Repair v -> writeRepair w v
                    | Highbar.AICommand.CommandCase.ReclaimUnit v -> writeReclaimUnit w v
                    | Highbar.AICommand.CommandCase.ReclaimArea v -> writeReclaimArea w v
                    | Highbar.AICommand.CommandCase.ReclaimInArea v -> writeReclaimInArea w v
                    | Highbar.AICommand.CommandCase.ReclaimFeature v -> writeReclaimFeature w v
                    | Highbar.AICommand.CommandCase.RestoreArea v -> writeRestoreArea w v
                    | Highbar.AICommand.CommandCase.Resurrect v -> writeResurrect w v
                    | Highbar.AICommand.CommandCase.ResurrectInArea v -> writeResurrectInArea w v
                    | Highbar.AICommand.CommandCase.Capture v -> writeCapture w v
                    | Highbar.AICommand.CommandCase.CaptureArea v -> writeCaptureArea w v
                    | Highbar.AICommand.CommandCase.SetBase v -> writeSetBase w v
                    | Highbar.AICommand.CommandCase.SelfDestruct v -> writeSelfDestruct w v
                    | Highbar.AICommand.CommandCase.LoadUnits v -> writeLoadUnits w v
                    | Highbar.AICommand.CommandCase.LoadUnitsArea v -> writeLoadUnitsArea w v
                    | Highbar.AICommand.CommandCase.LoadOnto v -> writeLoadOnto w v
                    | Highbar.AICommand.CommandCase.UnloadUnit v -> writeUnloadUnit w v
                    | Highbar.AICommand.CommandCase.UnloadUnitsArea v -> writeUnloadUnitsArea w v
                    | Highbar.AICommand.CommandCase.SetWantedMaxSpeed v -> writeSetWantedMaxSpeed w v
                    | Highbar.AICommand.CommandCase.Stockpile v -> writeStockpile w v
                    | Highbar.AICommand.CommandCase.Dgun v -> writeDgun w v
                    | Highbar.AICommand.CommandCase.Custom v -> writeCustom w v
                    | Highbar.AICommand.CommandCase.SetOnOff v -> writeSetOnOff w v
                    | Highbar.AICommand.CommandCase.SetRepeat v -> writeSetRepeat w v
                    | Highbar.AICommand.CommandCase.SetMoveState v -> writeSetMoveState w v
                    | Highbar.AICommand.CommandCase.SetFireState v -> writeSetFireState w v
                    | Highbar.AICommand.CommandCase.SetTrajectory v -> writeSetTrajectory w v
                    | Highbar.AICommand.CommandCase.SetAutoRepairLevel v -> writeSetAutoRepairLevel w v
                    | Highbar.AICommand.CommandCase.SetIdleMode v -> writeSetIdleMode w v
                    )
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : AICommand =
                    match kvPair.Key with
                    | "drawAddPoint" -> { value with Command = Highbar.AICommand.CommandCase.DrawAddPoint (DrawAddPoint.ReadJsonField kvPair.Value) }
                    | "drawAddLine" -> { value with Command = Highbar.AICommand.CommandCase.DrawAddLine (DrawAddLine.ReadJsonField kvPair.Value) }
                    | "drawRemovePoint" -> { value with Command = Highbar.AICommand.CommandCase.DrawRemovePoint (DrawRemovePoint.ReadJsonField kvPair.Value) }
                    | "sendTextMessage" -> { value with Command = Highbar.AICommand.CommandCase.SendTextMessage (SendTextMessage.ReadJsonField kvPair.Value) }
                    | "setLastPosMessage" -> { value with Command = Highbar.AICommand.CommandCase.SetLastPosMessage (SetLastPosMessage.ReadJsonField kvPair.Value) }
                    | "sendResources" -> { value with Command = Highbar.AICommand.CommandCase.SendResources (SendResources.ReadJsonField kvPair.Value) }
                    | "setMyIncomeShareDirect" -> { value with Command = Highbar.AICommand.CommandCase.SetMyIncomeShareDirect (SetMyIncomeShareDirect.ReadJsonField kvPair.Value) }
                    | "setShareLevel" -> { value with Command = Highbar.AICommand.CommandCase.SetShareLevel (SetShareLevel.ReadJsonField kvPair.Value) }
                    | "pauseTeam" -> { value with Command = Highbar.AICommand.CommandCase.PauseTeam (PauseTeam.ReadJsonField kvPair.Value) }
                    | "groupAddUnit" -> { value with Command = Highbar.AICommand.CommandCase.GroupAddUnit (GroupAddUnit.ReadJsonField kvPair.Value) }
                    | "groupRemoveUnit" -> { value with Command = Highbar.AICommand.CommandCase.GroupRemoveUnit (GroupRemoveUnit.ReadJsonField kvPair.Value) }
                    | "initPath" -> { value with Command = Highbar.AICommand.CommandCase.InitPath (InitPath.ReadJsonField kvPair.Value) }
                    | "getApproxLength" -> { value with Command = Highbar.AICommand.CommandCase.GetApproxLength (GetApproxLength.ReadJsonField kvPair.Value) }
                    | "getNextWaypoint" -> { value with Command = Highbar.AICommand.CommandCase.GetNextWaypoint (GetNextWaypoint.ReadJsonField kvPair.Value) }
                    | "freePath" -> { value with Command = Highbar.AICommand.CommandCase.FreePath (FreePath.ReadJsonField kvPair.Value) }
                    | "giveMe" -> { value with Command = Highbar.AICommand.CommandCase.GiveMe (GiveMe.ReadJsonField kvPair.Value) }
                    | "giveMeNewUnit" -> { value with Command = Highbar.AICommand.CommandCase.GiveMeNewUnit (GiveMeNewUnit.ReadJsonField kvPair.Value) }
                    | "callLuaRules" -> { value with Command = Highbar.AICommand.CommandCase.CallLuaRules (CallLuaRules.ReadJsonField kvPair.Value) }
                    | "callLuaUi" -> { value with Command = Highbar.AICommand.CommandCase.CallLuaUi (CallLuaUi.ReadJsonField kvPair.Value) }
                    | "createSplineFigure" -> { value with Command = Highbar.AICommand.CommandCase.CreateSplineFigure (CreateSplineFigure.ReadJsonField kvPair.Value) }
                    | "createLineFigure" -> { value with Command = Highbar.AICommand.CommandCase.CreateLineFigure (CreateLineFigure.ReadJsonField kvPair.Value) }
                    | "setFigurePosition" -> { value with Command = Highbar.AICommand.CommandCase.SetFigurePosition (SetFigurePosition.ReadJsonField kvPair.Value) }
                    | "setFigureColor" -> { value with Command = Highbar.AICommand.CommandCase.SetFigureColor (SetFigureColor.ReadJsonField kvPair.Value) }
                    | "removeFigure" -> { value with Command = Highbar.AICommand.CommandCase.RemoveFigure (RemoveFigure.ReadJsonField kvPair.Value) }
                    | "drawUnit" -> { value with Command = Highbar.AICommand.CommandCase.DrawUnit (DrawUnit.ReadJsonField kvPair.Value) }
                    | "buildUnit" -> { value with Command = Highbar.AICommand.CommandCase.BuildUnit (BuildUnit.ReadJsonField kvPair.Value) }
                    | "stop" -> { value with Command = Highbar.AICommand.CommandCase.Stop (Stop.ReadJsonField kvPair.Value) }
                    | "wait" -> { value with Command = Highbar.AICommand.CommandCase.Wait (Wait.ReadJsonField kvPair.Value) }
                    | "timedWait" -> { value with Command = Highbar.AICommand.CommandCase.TimedWait (TimedWait.ReadJsonField kvPair.Value) }
                    | "squadWait" -> { value with Command = Highbar.AICommand.CommandCase.SquadWait (SquadWait.ReadJsonField kvPair.Value) }
                    | "deathWait" -> { value with Command = Highbar.AICommand.CommandCase.DeathWait (DeathWait.ReadJsonField kvPair.Value) }
                    | "gatherWait" -> { value with Command = Highbar.AICommand.CommandCase.GatherWait (GatherWait.ReadJsonField kvPair.Value) }
                    | "moveUnit" -> { value with Command = Highbar.AICommand.CommandCase.MoveUnit (MoveUnit.ReadJsonField kvPair.Value) }
                    | "patrol" -> { value with Command = Highbar.AICommand.CommandCase.Patrol (Patrol.ReadJsonField kvPair.Value) }
                    | "fight" -> { value with Command = Highbar.AICommand.CommandCase.Fight (Fight.ReadJsonField kvPair.Value) }
                    | "attack" -> { value with Command = Highbar.AICommand.CommandCase.Attack (Attack.ReadJsonField kvPair.Value) }
                    | "attackArea" -> { value with Command = Highbar.AICommand.CommandCase.AttackArea (AttackArea.ReadJsonField kvPair.Value) }
                    | "guard" -> { value with Command = Highbar.AICommand.CommandCase.Guard (Guard.ReadJsonField kvPair.Value) }
                    | "repair" -> { value with Command = Highbar.AICommand.CommandCase.Repair (Repair.ReadJsonField kvPair.Value) }
                    | "reclaimUnit" -> { value with Command = Highbar.AICommand.CommandCase.ReclaimUnit (ReclaimUnit.ReadJsonField kvPair.Value) }
                    | "reclaimArea" -> { value with Command = Highbar.AICommand.CommandCase.ReclaimArea (ReclaimArea.ReadJsonField kvPair.Value) }
                    | "reclaimInArea" -> { value with Command = Highbar.AICommand.CommandCase.ReclaimInArea (ReclaimInArea.ReadJsonField kvPair.Value) }
                    | "reclaimFeature" -> { value with Command = Highbar.AICommand.CommandCase.ReclaimFeature (ReclaimFeature.ReadJsonField kvPair.Value) }
                    | "restoreArea" -> { value with Command = Highbar.AICommand.CommandCase.RestoreArea (RestoreArea.ReadJsonField kvPair.Value) }
                    | "resurrect" -> { value with Command = Highbar.AICommand.CommandCase.Resurrect (Resurrect.ReadJsonField kvPair.Value) }
                    | "resurrectInArea" -> { value with Command = Highbar.AICommand.CommandCase.ResurrectInArea (ResurrectInArea.ReadJsonField kvPair.Value) }
                    | "capture" -> { value with Command = Highbar.AICommand.CommandCase.Capture (Capture.ReadJsonField kvPair.Value) }
                    | "captureArea" -> { value with Command = Highbar.AICommand.CommandCase.CaptureArea (CaptureArea.ReadJsonField kvPair.Value) }
                    | "setBase" -> { value with Command = Highbar.AICommand.CommandCase.SetBase (SetBase.ReadJsonField kvPair.Value) }
                    | "selfDestruct" -> { value with Command = Highbar.AICommand.CommandCase.SelfDestruct (SelfDestruct.ReadJsonField kvPair.Value) }
                    | "loadUnits" -> { value with Command = Highbar.AICommand.CommandCase.LoadUnits (LoadUnits.ReadJsonField kvPair.Value) }
                    | "loadUnitsArea" -> { value with Command = Highbar.AICommand.CommandCase.LoadUnitsArea (LoadUnitsArea.ReadJsonField kvPair.Value) }
                    | "loadOnto" -> { value with Command = Highbar.AICommand.CommandCase.LoadOnto (LoadOnto.ReadJsonField kvPair.Value) }
                    | "unloadUnit" -> { value with Command = Highbar.AICommand.CommandCase.UnloadUnit (UnloadUnit.ReadJsonField kvPair.Value) }
                    | "unloadUnitsArea" -> { value with Command = Highbar.AICommand.CommandCase.UnloadUnitsArea (UnloadUnitsArea.ReadJsonField kvPair.Value) }
                    | "setWantedMaxSpeed" -> { value with Command = Highbar.AICommand.CommandCase.SetWantedMaxSpeed (SetWantedMaxSpeed.ReadJsonField kvPair.Value) }
                    | "stockpile" -> { value with Command = Highbar.AICommand.CommandCase.Stockpile (Stockpile.ReadJsonField kvPair.Value) }
                    | "dgun" -> { value with Command = Highbar.AICommand.CommandCase.Dgun (Dgun.ReadJsonField kvPair.Value) }
                    | "custom" -> { value with Command = Highbar.AICommand.CommandCase.Custom (Custom.ReadJsonField kvPair.Value) }
                    | "setOnOff" -> { value with Command = Highbar.AICommand.CommandCase.SetOnOff (SetOnOff.ReadJsonField kvPair.Value) }
                    | "setRepeat" -> { value with Command = Highbar.AICommand.CommandCase.SetRepeat (SetRepeat.ReadJsonField kvPair.Value) }
                    | "setMoveState" -> { value with Command = Highbar.AICommand.CommandCase.SetMoveState (SetMoveState.ReadJsonField kvPair.Value) }
                    | "setFireState" -> { value with Command = Highbar.AICommand.CommandCase.SetFireState (SetFireState.ReadJsonField kvPair.Value) }
                    | "setTrajectory" -> { value with Command = Highbar.AICommand.CommandCase.SetTrajectory (SetTrajectory.ReadJsonField kvPair.Value) }
                    | "setAutoRepairLevel" -> { value with Command = Highbar.AICommand.CommandCase.SetAutoRepairLevel (SetAutoRepairLevel.ReadJsonField kvPair.Value) }
                    | "setIdleMode" -> { value with Command = Highbar.AICommand.CommandCase.SetIdleMode (SetIdleMode.ReadJsonField kvPair.Value) }
                    | "command" -> { value with Command = Command.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _AICommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._AICommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module DrawAddPointCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Position: OptionBuilder<Highbar.Vector3> // (1)
            val mutable Label: string // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 2 -> x.Label <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.DrawAddPointCommand = {
            Position = x.Position.Build
            Label = x.Label |> orEmptyString
            }

type private _DrawAddPointCommand = DrawAddPointCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type DrawAddPointCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("label")>] Label: string // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<DrawAddPointCommand>> =
        lazy
        // Field Definitions
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (1, "position")
        let Label = FieldCodec.Primitive ValueCodec.String (2, "label")
        // Proto Definition Implementation
        { // ProtoDef<DrawAddPointCommand>
            Name = "DrawAddPointCommand"
            Empty = {
                Position = Position.GetDefault()
                Label = Label.GetDefault()
                }
            Size = fun (m: DrawAddPointCommand) ->
                0
                + Position.CalcFieldSize m.Position
                + Label.CalcFieldSize m.Label
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: DrawAddPointCommand) ->
                Position.WriteField w m.Position
                Label.WriteField w m.Label
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.DrawAddPointCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writePosition = Position.WriteJsonField o
                let writeLabel = Label.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: DrawAddPointCommand) =
                    writePosition w m.Position
                    writeLabel w m.Label
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : DrawAddPointCommand =
                    match kvPair.Key with
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | "label" -> { value with Label = Label.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _DrawAddPointCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._DrawAddPointCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module DrawAddLineCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable FromPosition: OptionBuilder<Highbar.Vector3> // (1)
            val mutable ToPosition: OptionBuilder<Highbar.Vector3> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.FromPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 2 -> x.ToPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.DrawAddLineCommand = {
            FromPosition = x.FromPosition.Build
            ToPosition = x.ToPosition.Build
            }

type private _DrawAddLineCommand = DrawAddLineCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type DrawAddLineCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("fromPosition")>] FromPosition: Highbar.Vector3 option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("toPosition")>] ToPosition: Highbar.Vector3 option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<DrawAddLineCommand>> =
        lazy
        // Field Definitions
        let FromPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (1, "fromPosition")
        let ToPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (2, "toPosition")
        // Proto Definition Implementation
        { // ProtoDef<DrawAddLineCommand>
            Name = "DrawAddLineCommand"
            Empty = {
                FromPosition = FromPosition.GetDefault()
                ToPosition = ToPosition.GetDefault()
                }
            Size = fun (m: DrawAddLineCommand) ->
                0
                + FromPosition.CalcFieldSize m.FromPosition
                + ToPosition.CalcFieldSize m.ToPosition
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: DrawAddLineCommand) ->
                FromPosition.WriteField w m.FromPosition
                ToPosition.WriteField w m.ToPosition
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.DrawAddLineCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFromPosition = FromPosition.WriteJsonField o
                let writeToPosition = ToPosition.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: DrawAddLineCommand) =
                    writeFromPosition w m.FromPosition
                    writeToPosition w m.ToPosition
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : DrawAddLineCommand =
                    match kvPair.Key with
                    | "fromPosition" -> { value with FromPosition = FromPosition.ReadJsonField kvPair.Value }
                    | "toPosition" -> { value with ToPosition = ToPosition.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _DrawAddLineCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._DrawAddLineCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module DrawRemovePointCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Position: OptionBuilder<Highbar.Vector3> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.DrawRemovePointCommand = {
            Position = x.Position.Build
            }

type private _DrawRemovePointCommand = DrawRemovePointCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type DrawRemovePointCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<DrawRemovePointCommand>> =
        lazy
        // Field Definitions
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (1, "position")
        // Proto Definition Implementation
        { // ProtoDef<DrawRemovePointCommand>
            Name = "DrawRemovePointCommand"
            Empty = {
                Position = Position.GetDefault()
                }
            Size = fun (m: DrawRemovePointCommand) ->
                0
                + Position.CalcFieldSize m.Position
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: DrawRemovePointCommand) ->
                Position.WriteField w m.Position
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.DrawRemovePointCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writePosition = Position.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: DrawRemovePointCommand) =
                    writePosition w m.Position
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : DrawRemovePointCommand =
                    match kvPair.Key with
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _DrawRemovePointCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._DrawRemovePointCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SendTextMessageCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Text: string // (1)
            val mutable Zone: int // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Text <- ValueCodec.String.ReadValue reader
            | 2 -> x.Zone <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SendTextMessageCommand = {
            Text = x.Text |> orEmptyString
            Zone = x.Zone
            }

type private _SendTextMessageCommand = SendTextMessageCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SendTextMessageCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("text")>] Text: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("zone")>] Zone: int // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<SendTextMessageCommand>> =
        lazy
        // Field Definitions
        let Text = FieldCodec.Primitive ValueCodec.String (1, "text")
        let Zone = FieldCodec.Primitive ValueCodec.Int32 (2, "zone")
        // Proto Definition Implementation
        { // ProtoDef<SendTextMessageCommand>
            Name = "SendTextMessageCommand"
            Empty = {
                Text = Text.GetDefault()
                Zone = Zone.GetDefault()
                }
            Size = fun (m: SendTextMessageCommand) ->
                0
                + Text.CalcFieldSize m.Text
                + Zone.CalcFieldSize m.Zone
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SendTextMessageCommand) ->
                Text.WriteField w m.Text
                Zone.WriteField w m.Zone
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SendTextMessageCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeText = Text.WriteJsonField o
                let writeZone = Zone.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SendTextMessageCommand) =
                    writeText w m.Text
                    writeZone w m.Zone
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SendTextMessageCommand =
                    match kvPair.Key with
                    | "text" -> { value with Text = Text.ReadJsonField kvPair.Value }
                    | "zone" -> { value with Zone = Zone.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SendTextMessageCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SendTextMessageCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetLastPosMessageCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Position: OptionBuilder<Highbar.Vector3> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetLastPosMessageCommand = {
            Position = x.Position.Build
            }

type private _SetLastPosMessageCommand = SetLastPosMessageCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetLastPosMessageCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SetLastPosMessageCommand>> =
        lazy
        // Field Definitions
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (1, "position")
        // Proto Definition Implementation
        { // ProtoDef<SetLastPosMessageCommand>
            Name = "SetLastPosMessageCommand"
            Empty = {
                Position = Position.GetDefault()
                }
            Size = fun (m: SetLastPosMessageCommand) ->
                0
                + Position.CalcFieldSize m.Position
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetLastPosMessageCommand) ->
                Position.WriteField w m.Position
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetLastPosMessageCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writePosition = Position.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetLastPosMessageCommand) =
                    writePosition w m.Position
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetLastPosMessageCommand =
                    match kvPair.Key with
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetLastPosMessageCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetLastPosMessageCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SendResourcesCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ResourceId: int // (1)
            val mutable Amount: float32 // (2)
            val mutable ReceivingTeamId: int // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ResourceId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.Amount <- ValueCodec.Float.ReadValue reader
            | 3 -> x.ReceivingTeamId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SendResourcesCommand = {
            ResourceId = x.ResourceId
            Amount = x.Amount
            ReceivingTeamId = x.ReceivingTeamId
            }

type private _SendResourcesCommand = SendResourcesCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SendResourcesCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("resourceId")>] ResourceId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("amount")>] Amount: float32 // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("receivingTeamId")>] ReceivingTeamId: int // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<SendResourcesCommand>> =
        lazy
        // Field Definitions
        let ResourceId = FieldCodec.Primitive ValueCodec.Int32 (1, "resourceId")
        let Amount = FieldCodec.Primitive ValueCodec.Float (2, "amount")
        let ReceivingTeamId = FieldCodec.Primitive ValueCodec.Int32 (3, "receivingTeamId")
        // Proto Definition Implementation
        { // ProtoDef<SendResourcesCommand>
            Name = "SendResourcesCommand"
            Empty = {
                ResourceId = ResourceId.GetDefault()
                Amount = Amount.GetDefault()
                ReceivingTeamId = ReceivingTeamId.GetDefault()
                }
            Size = fun (m: SendResourcesCommand) ->
                0
                + ResourceId.CalcFieldSize m.ResourceId
                + Amount.CalcFieldSize m.Amount
                + ReceivingTeamId.CalcFieldSize m.ReceivingTeamId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SendResourcesCommand) ->
                ResourceId.WriteField w m.ResourceId
                Amount.WriteField w m.Amount
                ReceivingTeamId.WriteField w m.ReceivingTeamId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SendResourcesCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResourceId = ResourceId.WriteJsonField o
                let writeAmount = Amount.WriteJsonField o
                let writeReceivingTeamId = ReceivingTeamId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SendResourcesCommand) =
                    writeResourceId w m.ResourceId
                    writeAmount w m.Amount
                    writeReceivingTeamId w m.ReceivingTeamId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SendResourcesCommand =
                    match kvPair.Key with
                    | "resourceId" -> { value with ResourceId = ResourceId.ReadJsonField kvPair.Value }
                    | "amount" -> { value with Amount = Amount.ReadJsonField kvPair.Value }
                    | "receivingTeamId" -> { value with ReceivingTeamId = ReceivingTeamId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SendResourcesCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SendResourcesCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetMyIncomeShareDirectCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ResourceId: int // (1)
            val mutable Share: float32 // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ResourceId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.Share <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetMyIncomeShareDirectCommand = {
            ResourceId = x.ResourceId
            Share = x.Share
            }

type private _SetMyIncomeShareDirectCommand = SetMyIncomeShareDirectCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetMyIncomeShareDirectCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("resourceId")>] ResourceId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("share")>] Share: float32 // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<SetMyIncomeShareDirectCommand>> =
        lazy
        // Field Definitions
        let ResourceId = FieldCodec.Primitive ValueCodec.Int32 (1, "resourceId")
        let Share = FieldCodec.Primitive ValueCodec.Float (2, "share")
        // Proto Definition Implementation
        { // ProtoDef<SetMyIncomeShareDirectCommand>
            Name = "SetMyIncomeShareDirectCommand"
            Empty = {
                ResourceId = ResourceId.GetDefault()
                Share = Share.GetDefault()
                }
            Size = fun (m: SetMyIncomeShareDirectCommand) ->
                0
                + ResourceId.CalcFieldSize m.ResourceId
                + Share.CalcFieldSize m.Share
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetMyIncomeShareDirectCommand) ->
                ResourceId.WriteField w m.ResourceId
                Share.WriteField w m.Share
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetMyIncomeShareDirectCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResourceId = ResourceId.WriteJsonField o
                let writeShare = Share.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetMyIncomeShareDirectCommand) =
                    writeResourceId w m.ResourceId
                    writeShare w m.Share
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetMyIncomeShareDirectCommand =
                    match kvPair.Key with
                    | "resourceId" -> { value with ResourceId = ResourceId.ReadJsonField kvPair.Value }
                    | "share" -> { value with Share = Share.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetMyIncomeShareDirectCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetMyIncomeShareDirectCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetShareLevelCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ResourceId: int // (1)
            val mutable ShareLevel: float32 // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ResourceId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.ShareLevel <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetShareLevelCommand = {
            ResourceId = x.ResourceId
            ShareLevel = x.ShareLevel
            }

type private _SetShareLevelCommand = SetShareLevelCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetShareLevelCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("resourceId")>] ResourceId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("shareLevel")>] ShareLevel: float32 // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<SetShareLevelCommand>> =
        lazy
        // Field Definitions
        let ResourceId = FieldCodec.Primitive ValueCodec.Int32 (1, "resourceId")
        let ShareLevel = FieldCodec.Primitive ValueCodec.Float (2, "shareLevel")
        // Proto Definition Implementation
        { // ProtoDef<SetShareLevelCommand>
            Name = "SetShareLevelCommand"
            Empty = {
                ResourceId = ResourceId.GetDefault()
                ShareLevel = ShareLevel.GetDefault()
                }
            Size = fun (m: SetShareLevelCommand) ->
                0
                + ResourceId.CalcFieldSize m.ResourceId
                + ShareLevel.CalcFieldSize m.ShareLevel
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetShareLevelCommand) ->
                ResourceId.WriteField w m.ResourceId
                ShareLevel.WriteField w m.ShareLevel
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetShareLevelCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResourceId = ResourceId.WriteJsonField o
                let writeShareLevel = ShareLevel.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetShareLevelCommand) =
                    writeResourceId w m.ResourceId
                    writeShareLevel w m.ShareLevel
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetShareLevelCommand =
                    match kvPair.Key with
                    | "resourceId" -> { value with ResourceId = ResourceId.ReadJsonField kvPair.Value }
                    | "shareLevel" -> { value with ShareLevel = ShareLevel.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetShareLevelCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetShareLevelCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PauseTeamCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Enable: bool // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Enable <- ValueCodec.Bool.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.PauseTeamCommand = {
            Enable = x.Enable
            }

type private _PauseTeamCommand = PauseTeamCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type PauseTeamCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("enable")>] Enable: bool // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<PauseTeamCommand>> =
        lazy
        // Field Definitions
        let Enable = FieldCodec.Primitive ValueCodec.Bool (1, "enable")
        // Proto Definition Implementation
        { // ProtoDef<PauseTeamCommand>
            Name = "PauseTeamCommand"
            Empty = {
                Enable = Enable.GetDefault()
                }
            Size = fun (m: PauseTeamCommand) ->
                0
                + Enable.CalcFieldSize m.Enable
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: PauseTeamCommand) ->
                Enable.WriteField w m.Enable
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.PauseTeamCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeEnable = Enable.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: PauseTeamCommand) =
                    writeEnable w m.Enable
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : PauseTeamCommand =
                    match kvPair.Key with
                    | "enable" -> { value with Enable = Enable.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _PauseTeamCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._PauseTeamCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GroupAddUnitCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.GroupAddUnitCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            }

type private _GroupAddUnitCommand = GroupAddUnitCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GroupAddUnitCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<GroupAddUnitCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        // Proto Definition Implementation
        { // ProtoDef<GroupAddUnitCommand>
            Name = "GroupAddUnitCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                }
            Size = fun (m: GroupAddUnitCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GroupAddUnitCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.GroupAddUnitCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GroupAddUnitCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GroupAddUnitCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GroupAddUnitCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._GroupAddUnitCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GroupRemoveUnitCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.GroupRemoveUnitCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            }

type private _GroupRemoveUnitCommand = GroupRemoveUnitCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GroupRemoveUnitCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<GroupRemoveUnitCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        // Proto Definition Implementation
        { // ProtoDef<GroupRemoveUnitCommand>
            Name = "GroupRemoveUnitCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                }
            Size = fun (m: GroupRemoveUnitCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GroupRemoveUnitCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.GroupRemoveUnitCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GroupRemoveUnitCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GroupRemoveUnitCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GroupRemoveUnitCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._GroupRemoveUnitCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module InitPathCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable StartPosition: OptionBuilder<Highbar.Vector3> // (1)
            val mutable EndPosition: OptionBuilder<Highbar.Vector3> // (2)
            val mutable PathType: int // (3)
            val mutable GoalRadius: float32 // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.StartPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 2 -> x.EndPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 3 -> x.PathType <- ValueCodec.Int32.ReadValue reader
            | 4 -> x.GoalRadius <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.InitPathCommand = {
            StartPosition = x.StartPosition.Build
            EndPosition = x.EndPosition.Build
            PathType = x.PathType
            GoalRadius = x.GoalRadius
            }

type private _InitPathCommand = InitPathCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type InitPathCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("startPosition")>] StartPosition: Highbar.Vector3 option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("endPosition")>] EndPosition: Highbar.Vector3 option // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("pathType")>] PathType: int // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("goalRadius")>] GoalRadius: float32 // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<InitPathCommand>> =
        lazy
        // Field Definitions
        let StartPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (1, "startPosition")
        let EndPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (2, "endPosition")
        let PathType = FieldCodec.Primitive ValueCodec.Int32 (3, "pathType")
        let GoalRadius = FieldCodec.Primitive ValueCodec.Float (4, "goalRadius")
        // Proto Definition Implementation
        { // ProtoDef<InitPathCommand>
            Name = "InitPathCommand"
            Empty = {
                StartPosition = StartPosition.GetDefault()
                EndPosition = EndPosition.GetDefault()
                PathType = PathType.GetDefault()
                GoalRadius = GoalRadius.GetDefault()
                }
            Size = fun (m: InitPathCommand) ->
                0
                + StartPosition.CalcFieldSize m.StartPosition
                + EndPosition.CalcFieldSize m.EndPosition
                + PathType.CalcFieldSize m.PathType
                + GoalRadius.CalcFieldSize m.GoalRadius
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: InitPathCommand) ->
                StartPosition.WriteField w m.StartPosition
                EndPosition.WriteField w m.EndPosition
                PathType.WriteField w m.PathType
                GoalRadius.WriteField w m.GoalRadius
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.InitPathCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeStartPosition = StartPosition.WriteJsonField o
                let writeEndPosition = EndPosition.WriteJsonField o
                let writePathType = PathType.WriteJsonField o
                let writeGoalRadius = GoalRadius.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: InitPathCommand) =
                    writeStartPosition w m.StartPosition
                    writeEndPosition w m.EndPosition
                    writePathType w m.PathType
                    writeGoalRadius w m.GoalRadius
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : InitPathCommand =
                    match kvPair.Key with
                    | "startPosition" -> { value with StartPosition = StartPosition.ReadJsonField kvPair.Value }
                    | "endPosition" -> { value with EndPosition = EndPosition.ReadJsonField kvPair.Value }
                    | "pathType" -> { value with PathType = PathType.ReadJsonField kvPair.Value }
                    | "goalRadius" -> { value with GoalRadius = GoalRadius.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _InitPathCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._InitPathCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GetApproxLengthCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable StartPosition: OptionBuilder<Highbar.Vector3> // (1)
            val mutable EndPosition: OptionBuilder<Highbar.Vector3> // (2)
            val mutable PathType: int // (3)
            val mutable GoalRadius: float32 // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.StartPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 2 -> x.EndPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 3 -> x.PathType <- ValueCodec.Int32.ReadValue reader
            | 4 -> x.GoalRadius <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.GetApproxLengthCommand = {
            StartPosition = x.StartPosition.Build
            EndPosition = x.EndPosition.Build
            PathType = x.PathType
            GoalRadius = x.GoalRadius
            }

type private _GetApproxLengthCommand = GetApproxLengthCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GetApproxLengthCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("startPosition")>] StartPosition: Highbar.Vector3 option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("endPosition")>] EndPosition: Highbar.Vector3 option // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("pathType")>] PathType: int // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("goalRadius")>] GoalRadius: float32 // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<GetApproxLengthCommand>> =
        lazy
        // Field Definitions
        let StartPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (1, "startPosition")
        let EndPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (2, "endPosition")
        let PathType = FieldCodec.Primitive ValueCodec.Int32 (3, "pathType")
        let GoalRadius = FieldCodec.Primitive ValueCodec.Float (4, "goalRadius")
        // Proto Definition Implementation
        { // ProtoDef<GetApproxLengthCommand>
            Name = "GetApproxLengthCommand"
            Empty = {
                StartPosition = StartPosition.GetDefault()
                EndPosition = EndPosition.GetDefault()
                PathType = PathType.GetDefault()
                GoalRadius = GoalRadius.GetDefault()
                }
            Size = fun (m: GetApproxLengthCommand) ->
                0
                + StartPosition.CalcFieldSize m.StartPosition
                + EndPosition.CalcFieldSize m.EndPosition
                + PathType.CalcFieldSize m.PathType
                + GoalRadius.CalcFieldSize m.GoalRadius
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GetApproxLengthCommand) ->
                StartPosition.WriteField w m.StartPosition
                EndPosition.WriteField w m.EndPosition
                PathType.WriteField w m.PathType
                GoalRadius.WriteField w m.GoalRadius
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.GetApproxLengthCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeStartPosition = StartPosition.WriteJsonField o
                let writeEndPosition = EndPosition.WriteJsonField o
                let writePathType = PathType.WriteJsonField o
                let writeGoalRadius = GoalRadius.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GetApproxLengthCommand) =
                    writeStartPosition w m.StartPosition
                    writeEndPosition w m.EndPosition
                    writePathType w m.PathType
                    writeGoalRadius w m.GoalRadius
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GetApproxLengthCommand =
                    match kvPair.Key with
                    | "startPosition" -> { value with StartPosition = StartPosition.ReadJsonField kvPair.Value }
                    | "endPosition" -> { value with EndPosition = EndPosition.ReadJsonField kvPair.Value }
                    | "pathType" -> { value with PathType = PathType.ReadJsonField kvPair.Value }
                    | "goalRadius" -> { value with GoalRadius = GoalRadius.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GetApproxLengthCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._GetApproxLengthCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GetNextWaypointCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable PathId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.PathId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.GetNextWaypointCommand = {
            PathId = x.PathId
            }

type private _GetNextWaypointCommand = GetNextWaypointCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GetNextWaypointCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("pathId")>] PathId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<GetNextWaypointCommand>> =
        lazy
        // Field Definitions
        let PathId = FieldCodec.Primitive ValueCodec.Int32 (1, "pathId")
        // Proto Definition Implementation
        { // ProtoDef<GetNextWaypointCommand>
            Name = "GetNextWaypointCommand"
            Empty = {
                PathId = PathId.GetDefault()
                }
            Size = fun (m: GetNextWaypointCommand) ->
                0
                + PathId.CalcFieldSize m.PathId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GetNextWaypointCommand) ->
                PathId.WriteField w m.PathId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.GetNextWaypointCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writePathId = PathId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GetNextWaypointCommand) =
                    writePathId w m.PathId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GetNextWaypointCommand =
                    match kvPair.Key with
                    | "pathId" -> { value with PathId = PathId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GetNextWaypointCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._GetNextWaypointCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FreePathCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable PathId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.PathId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.FreePathCommand = {
            PathId = x.PathId
            }

type private _FreePathCommand = FreePathCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type FreePathCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("pathId")>] PathId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<FreePathCommand>> =
        lazy
        // Field Definitions
        let PathId = FieldCodec.Primitive ValueCodec.Int32 (1, "pathId")
        // Proto Definition Implementation
        { // ProtoDef<FreePathCommand>
            Name = "FreePathCommand"
            Empty = {
                PathId = PathId.GetDefault()
                }
            Size = fun (m: FreePathCommand) ->
                0
                + PathId.CalcFieldSize m.PathId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: FreePathCommand) ->
                PathId.WriteField w m.PathId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.FreePathCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writePathId = PathId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: FreePathCommand) =
                    writePathId w m.PathId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : FreePathCommand =
                    match kvPair.Key with
                    | "pathId" -> { value with PathId = PathId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _FreePathCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._FreePathCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GiveMeCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ResourceId: int // (1)
            val mutable Amount: float32 // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ResourceId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.Amount <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.GiveMeCommand = {
            ResourceId = x.ResourceId
            Amount = x.Amount
            }

type private _GiveMeCommand = GiveMeCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GiveMeCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("resourceId")>] ResourceId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("amount")>] Amount: float32 // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<GiveMeCommand>> =
        lazy
        // Field Definitions
        let ResourceId = FieldCodec.Primitive ValueCodec.Int32 (1, "resourceId")
        let Amount = FieldCodec.Primitive ValueCodec.Float (2, "amount")
        // Proto Definition Implementation
        { // ProtoDef<GiveMeCommand>
            Name = "GiveMeCommand"
            Empty = {
                ResourceId = ResourceId.GetDefault()
                Amount = Amount.GetDefault()
                }
            Size = fun (m: GiveMeCommand) ->
                0
                + ResourceId.CalcFieldSize m.ResourceId
                + Amount.CalcFieldSize m.Amount
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GiveMeCommand) ->
                ResourceId.WriteField w m.ResourceId
                Amount.WriteField w m.Amount
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.GiveMeCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResourceId = ResourceId.WriteJsonField o
                let writeAmount = Amount.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GiveMeCommand) =
                    writeResourceId w m.ResourceId
                    writeAmount w m.Amount
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GiveMeCommand =
                    match kvPair.Key with
                    | "resourceId" -> { value with ResourceId = ResourceId.ReadJsonField kvPair.Value }
                    | "amount" -> { value with Amount = Amount.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GiveMeCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._GiveMeCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GiveMeNewUnitCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitDefId: int // (1)
            val mutable Position: OptionBuilder<Highbar.Vector3> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitDefId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.GiveMeNewUnitCommand = {
            UnitDefId = x.UnitDefId
            Position = x.Position.Build
            }

type private _GiveMeNewUnitCommand = GiveMeNewUnitCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GiveMeNewUnitCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitDefId")>] UnitDefId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<GiveMeNewUnitCommand>> =
        lazy
        // Field Definitions
        let UnitDefId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitDefId")
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (2, "position")
        // Proto Definition Implementation
        { // ProtoDef<GiveMeNewUnitCommand>
            Name = "GiveMeNewUnitCommand"
            Empty = {
                UnitDefId = UnitDefId.GetDefault()
                Position = Position.GetDefault()
                }
            Size = fun (m: GiveMeNewUnitCommand) ->
                0
                + UnitDefId.CalcFieldSize m.UnitDefId
                + Position.CalcFieldSize m.Position
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GiveMeNewUnitCommand) ->
                UnitDefId.WriteField w m.UnitDefId
                Position.WriteField w m.Position
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.GiveMeNewUnitCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitDefId = UnitDefId.WriteJsonField o
                let writePosition = Position.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GiveMeNewUnitCommand) =
                    writeUnitDefId w m.UnitDefId
                    writePosition w m.Position
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GiveMeNewUnitCommand =
                    match kvPair.Key with
                    | "unitDefId" -> { value with UnitDefId = UnitDefId.ReadJsonField kvPair.Value }
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GiveMeNewUnitCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._GiveMeNewUnitCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CallLuaRulesCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Data: string // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Data <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.CallLuaRulesCommand = {
            Data = x.Data |> orEmptyString
            }

type private _CallLuaRulesCommand = CallLuaRulesCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CallLuaRulesCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("data")>] Data: string // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<CallLuaRulesCommand>> =
        lazy
        // Field Definitions
        let Data = FieldCodec.Primitive ValueCodec.String (1, "data")
        // Proto Definition Implementation
        { // ProtoDef<CallLuaRulesCommand>
            Name = "CallLuaRulesCommand"
            Empty = {
                Data = Data.GetDefault()
                }
            Size = fun (m: CallLuaRulesCommand) ->
                0
                + Data.CalcFieldSize m.Data
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CallLuaRulesCommand) ->
                Data.WriteField w m.Data
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.CallLuaRulesCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeData = Data.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CallLuaRulesCommand) =
                    writeData w m.Data
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CallLuaRulesCommand =
                    match kvPair.Key with
                    | "data" -> { value with Data = Data.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CallLuaRulesCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CallLuaRulesCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CallLuaUICommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Data: string // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Data <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.CallLuaUICommand = {
            Data = x.Data |> orEmptyString
            }

type private _CallLuaUICommand = CallLuaUICommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CallLuaUICommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("data")>] Data: string // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<CallLuaUICommand>> =
        lazy
        // Field Definitions
        let Data = FieldCodec.Primitive ValueCodec.String (1, "data")
        // Proto Definition Implementation
        { // ProtoDef<CallLuaUICommand>
            Name = "CallLuaUICommand"
            Empty = {
                Data = Data.GetDefault()
                }
            Size = fun (m: CallLuaUICommand) ->
                0
                + Data.CalcFieldSize m.Data
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CallLuaUICommand) ->
                Data.WriteField w m.Data
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.CallLuaUICommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeData = Data.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CallLuaUICommand) =
                    writeData w m.Data
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CallLuaUICommand =
                    match kvPair.Key with
                    | "data" -> { value with Data = Data.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CallLuaUICommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CallLuaUICommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CreateSplineFigureCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Position1: OptionBuilder<Highbar.Vector3> // (1)
            val mutable Position2: OptionBuilder<Highbar.Vector3> // (2)
            val mutable Position3: OptionBuilder<Highbar.Vector3> // (3)
            val mutable Position4: OptionBuilder<Highbar.Vector3> // (4)
            val mutable Width: float32 // (5)
            val mutable Arrow: bool // (6)
            val mutable Lifespan: int // (7)
            val mutable FigureGroupId: int // (8)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Position1.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 2 -> x.Position2.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 3 -> x.Position3.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 4 -> x.Position4.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 5 -> x.Width <- ValueCodec.Float.ReadValue reader
            | 6 -> x.Arrow <- ValueCodec.Bool.ReadValue reader
            | 7 -> x.Lifespan <- ValueCodec.Int32.ReadValue reader
            | 8 -> x.FigureGroupId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.CreateSplineFigureCommand = {
            Position1 = x.Position1.Build
            Position2 = x.Position2.Build
            Position3 = x.Position3.Build
            Position4 = x.Position4.Build
            Width = x.Width
            Arrow = x.Arrow
            Lifespan = x.Lifespan
            FigureGroupId = x.FigureGroupId
            }

type private _CreateSplineFigureCommand = CreateSplineFigureCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CreateSplineFigureCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("position1")>] Position1: Highbar.Vector3 option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("position2")>] Position2: Highbar.Vector3 option // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("position3")>] Position3: Highbar.Vector3 option // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("position4")>] Position4: Highbar.Vector3 option // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("width")>] Width: float32 // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("arrow")>] Arrow: bool // (6)
    [<System.Text.Json.Serialization.JsonPropertyName("lifespan")>] Lifespan: int // (7)
    [<System.Text.Json.Serialization.JsonPropertyName("figureGroupId")>] FigureGroupId: int // (8)
    }
    with
    static member Proto : Lazy<ProtoDef<CreateSplineFigureCommand>> =
        lazy
        // Field Definitions
        let Position1 = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (1, "position1")
        let Position2 = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (2, "position2")
        let Position3 = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (3, "position3")
        let Position4 = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (4, "position4")
        let Width = FieldCodec.Primitive ValueCodec.Float (5, "width")
        let Arrow = FieldCodec.Primitive ValueCodec.Bool (6, "arrow")
        let Lifespan = FieldCodec.Primitive ValueCodec.Int32 (7, "lifespan")
        let FigureGroupId = FieldCodec.Primitive ValueCodec.Int32 (8, "figureGroupId")
        // Proto Definition Implementation
        { // ProtoDef<CreateSplineFigureCommand>
            Name = "CreateSplineFigureCommand"
            Empty = {
                Position1 = Position1.GetDefault()
                Position2 = Position2.GetDefault()
                Position3 = Position3.GetDefault()
                Position4 = Position4.GetDefault()
                Width = Width.GetDefault()
                Arrow = Arrow.GetDefault()
                Lifespan = Lifespan.GetDefault()
                FigureGroupId = FigureGroupId.GetDefault()
                }
            Size = fun (m: CreateSplineFigureCommand) ->
                0
                + Position1.CalcFieldSize m.Position1
                + Position2.CalcFieldSize m.Position2
                + Position3.CalcFieldSize m.Position3
                + Position4.CalcFieldSize m.Position4
                + Width.CalcFieldSize m.Width
                + Arrow.CalcFieldSize m.Arrow
                + Lifespan.CalcFieldSize m.Lifespan
                + FigureGroupId.CalcFieldSize m.FigureGroupId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CreateSplineFigureCommand) ->
                Position1.WriteField w m.Position1
                Position2.WriteField w m.Position2
                Position3.WriteField w m.Position3
                Position4.WriteField w m.Position4
                Width.WriteField w m.Width
                Arrow.WriteField w m.Arrow
                Lifespan.WriteField w m.Lifespan
                FigureGroupId.WriteField w m.FigureGroupId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.CreateSplineFigureCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writePosition1 = Position1.WriteJsonField o
                let writePosition2 = Position2.WriteJsonField o
                let writePosition3 = Position3.WriteJsonField o
                let writePosition4 = Position4.WriteJsonField o
                let writeWidth = Width.WriteJsonField o
                let writeArrow = Arrow.WriteJsonField o
                let writeLifespan = Lifespan.WriteJsonField o
                let writeFigureGroupId = FigureGroupId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CreateSplineFigureCommand) =
                    writePosition1 w m.Position1
                    writePosition2 w m.Position2
                    writePosition3 w m.Position3
                    writePosition4 w m.Position4
                    writeWidth w m.Width
                    writeArrow w m.Arrow
                    writeLifespan w m.Lifespan
                    writeFigureGroupId w m.FigureGroupId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CreateSplineFigureCommand =
                    match kvPair.Key with
                    | "position1" -> { value with Position1 = Position1.ReadJsonField kvPair.Value }
                    | "position2" -> { value with Position2 = Position2.ReadJsonField kvPair.Value }
                    | "position3" -> { value with Position3 = Position3.ReadJsonField kvPair.Value }
                    | "position4" -> { value with Position4 = Position4.ReadJsonField kvPair.Value }
                    | "width" -> { value with Width = Width.ReadJsonField kvPair.Value }
                    | "arrow" -> { value with Arrow = Arrow.ReadJsonField kvPair.Value }
                    | "lifespan" -> { value with Lifespan = Lifespan.ReadJsonField kvPair.Value }
                    | "figureGroupId" -> { value with FigureGroupId = FigureGroupId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CreateSplineFigureCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CreateSplineFigureCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CreateLineFigureCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable FromPosition: OptionBuilder<Highbar.Vector3> // (1)
            val mutable ToPosition: OptionBuilder<Highbar.Vector3> // (2)
            val mutable Width: float32 // (3)
            val mutable Arrow: bool // (4)
            val mutable Lifespan: int // (5)
            val mutable FigureGroupId: int // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.FromPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 2 -> x.ToPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 3 -> x.Width <- ValueCodec.Float.ReadValue reader
            | 4 -> x.Arrow <- ValueCodec.Bool.ReadValue reader
            | 5 -> x.Lifespan <- ValueCodec.Int32.ReadValue reader
            | 6 -> x.FigureGroupId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.CreateLineFigureCommand = {
            FromPosition = x.FromPosition.Build
            ToPosition = x.ToPosition.Build
            Width = x.Width
            Arrow = x.Arrow
            Lifespan = x.Lifespan
            FigureGroupId = x.FigureGroupId
            }

type private _CreateLineFigureCommand = CreateLineFigureCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CreateLineFigureCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("fromPosition")>] FromPosition: Highbar.Vector3 option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("toPosition")>] ToPosition: Highbar.Vector3 option // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("width")>] Width: float32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("arrow")>] Arrow: bool // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("lifespan")>] Lifespan: int // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("figureGroupId")>] FigureGroupId: int // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<CreateLineFigureCommand>> =
        lazy
        // Field Definitions
        let FromPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (1, "fromPosition")
        let ToPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (2, "toPosition")
        let Width = FieldCodec.Primitive ValueCodec.Float (3, "width")
        let Arrow = FieldCodec.Primitive ValueCodec.Bool (4, "arrow")
        let Lifespan = FieldCodec.Primitive ValueCodec.Int32 (5, "lifespan")
        let FigureGroupId = FieldCodec.Primitive ValueCodec.Int32 (6, "figureGroupId")
        // Proto Definition Implementation
        { // ProtoDef<CreateLineFigureCommand>
            Name = "CreateLineFigureCommand"
            Empty = {
                FromPosition = FromPosition.GetDefault()
                ToPosition = ToPosition.GetDefault()
                Width = Width.GetDefault()
                Arrow = Arrow.GetDefault()
                Lifespan = Lifespan.GetDefault()
                FigureGroupId = FigureGroupId.GetDefault()
                }
            Size = fun (m: CreateLineFigureCommand) ->
                0
                + FromPosition.CalcFieldSize m.FromPosition
                + ToPosition.CalcFieldSize m.ToPosition
                + Width.CalcFieldSize m.Width
                + Arrow.CalcFieldSize m.Arrow
                + Lifespan.CalcFieldSize m.Lifespan
                + FigureGroupId.CalcFieldSize m.FigureGroupId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CreateLineFigureCommand) ->
                FromPosition.WriteField w m.FromPosition
                ToPosition.WriteField w m.ToPosition
                Width.WriteField w m.Width
                Arrow.WriteField w m.Arrow
                Lifespan.WriteField w m.Lifespan
                FigureGroupId.WriteField w m.FigureGroupId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.CreateLineFigureCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFromPosition = FromPosition.WriteJsonField o
                let writeToPosition = ToPosition.WriteJsonField o
                let writeWidth = Width.WriteJsonField o
                let writeArrow = Arrow.WriteJsonField o
                let writeLifespan = Lifespan.WriteJsonField o
                let writeFigureGroupId = FigureGroupId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CreateLineFigureCommand) =
                    writeFromPosition w m.FromPosition
                    writeToPosition w m.ToPosition
                    writeWidth w m.Width
                    writeArrow w m.Arrow
                    writeLifespan w m.Lifespan
                    writeFigureGroupId w m.FigureGroupId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CreateLineFigureCommand =
                    match kvPair.Key with
                    | "fromPosition" -> { value with FromPosition = FromPosition.ReadJsonField kvPair.Value }
                    | "toPosition" -> { value with ToPosition = ToPosition.ReadJsonField kvPair.Value }
                    | "width" -> { value with Width = Width.ReadJsonField kvPair.Value }
                    | "arrow" -> { value with Arrow = Arrow.ReadJsonField kvPair.Value }
                    | "lifespan" -> { value with Lifespan = Lifespan.ReadJsonField kvPair.Value }
                    | "figureGroupId" -> { value with FigureGroupId = FigureGroupId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CreateLineFigureCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CreateLineFigureCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetFigurePositionCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable FigureId: int // (1)
            val mutable Position: OptionBuilder<Highbar.Vector3> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.FigureId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetFigurePositionCommand = {
            FigureId = x.FigureId
            Position = x.Position.Build
            }

type private _SetFigurePositionCommand = SetFigurePositionCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetFigurePositionCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("figureId")>] FigureId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<SetFigurePositionCommand>> =
        lazy
        // Field Definitions
        let FigureId = FieldCodec.Primitive ValueCodec.Int32 (1, "figureId")
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (2, "position")
        // Proto Definition Implementation
        { // ProtoDef<SetFigurePositionCommand>
            Name = "SetFigurePositionCommand"
            Empty = {
                FigureId = FigureId.GetDefault()
                Position = Position.GetDefault()
                }
            Size = fun (m: SetFigurePositionCommand) ->
                0
                + FigureId.CalcFieldSize m.FigureId
                + Position.CalcFieldSize m.Position
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetFigurePositionCommand) ->
                FigureId.WriteField w m.FigureId
                Position.WriteField w m.Position
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetFigurePositionCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFigureId = FigureId.WriteJsonField o
                let writePosition = Position.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetFigurePositionCommand) =
                    writeFigureId w m.FigureId
                    writePosition w m.Position
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetFigurePositionCommand =
                    match kvPair.Key with
                    | "figureId" -> { value with FigureId = FigureId.ReadJsonField kvPair.Value }
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetFigurePositionCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetFigurePositionCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetFigureColorCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable FigureId: int // (1)
            val mutable R: float32 // (2)
            val mutable G: float32 // (3)
            val mutable B: float32 // (4)
            val mutable A: float32 // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.FigureId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.R <- ValueCodec.Float.ReadValue reader
            | 3 -> x.G <- ValueCodec.Float.ReadValue reader
            | 4 -> x.B <- ValueCodec.Float.ReadValue reader
            | 5 -> x.A <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetFigureColorCommand = {
            FigureId = x.FigureId
            R = x.R
            G = x.G
            B = x.B
            A = x.A
            }

type private _SetFigureColorCommand = SetFigureColorCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetFigureColorCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("figureId")>] FigureId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("r")>] R: float32 // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("g")>] G: float32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("b")>] B: float32 // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("a")>] A: float32 // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<SetFigureColorCommand>> =
        lazy
        // Field Definitions
        let FigureId = FieldCodec.Primitive ValueCodec.Int32 (1, "figureId")
        let R = FieldCodec.Primitive ValueCodec.Float (2, "r")
        let G = FieldCodec.Primitive ValueCodec.Float (3, "g")
        let B = FieldCodec.Primitive ValueCodec.Float (4, "b")
        let A = FieldCodec.Primitive ValueCodec.Float (5, "a")
        // Proto Definition Implementation
        { // ProtoDef<SetFigureColorCommand>
            Name = "SetFigureColorCommand"
            Empty = {
                FigureId = FigureId.GetDefault()
                R = R.GetDefault()
                G = G.GetDefault()
                B = B.GetDefault()
                A = A.GetDefault()
                }
            Size = fun (m: SetFigureColorCommand) ->
                0
                + FigureId.CalcFieldSize m.FigureId
                + R.CalcFieldSize m.R
                + G.CalcFieldSize m.G
                + B.CalcFieldSize m.B
                + A.CalcFieldSize m.A
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetFigureColorCommand) ->
                FigureId.WriteField w m.FigureId
                R.WriteField w m.R
                G.WriteField w m.G
                B.WriteField w m.B
                A.WriteField w m.A
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetFigureColorCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFigureId = FigureId.WriteJsonField o
                let writeR = R.WriteJsonField o
                let writeG = G.WriteJsonField o
                let writeB = B.WriteJsonField o
                let writeA = A.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetFigureColorCommand) =
                    writeFigureId w m.FigureId
                    writeR w m.R
                    writeG w m.G
                    writeB w m.B
                    writeA w m.A
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetFigureColorCommand =
                    match kvPair.Key with
                    | "figureId" -> { value with FigureId = FigureId.ReadJsonField kvPair.Value }
                    | "r" -> { value with R = R.ReadJsonField kvPair.Value }
                    | "g" -> { value with G = G.ReadJsonField kvPair.Value }
                    | "b" -> { value with B = B.ReadJsonField kvPair.Value }
                    | "a" -> { value with A = A.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetFigureColorCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetFigureColorCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module RemoveFigureCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable FigureId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.FigureId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.RemoveFigureCommand = {
            FigureId = x.FigureId
            }

type private _RemoveFigureCommand = RemoveFigureCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type RemoveFigureCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("figureId")>] FigureId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<RemoveFigureCommand>> =
        lazy
        // Field Definitions
        let FigureId = FieldCodec.Primitive ValueCodec.Int32 (1, "figureId")
        // Proto Definition Implementation
        { // ProtoDef<RemoveFigureCommand>
            Name = "RemoveFigureCommand"
            Empty = {
                FigureId = FigureId.GetDefault()
                }
            Size = fun (m: RemoveFigureCommand) ->
                0
                + FigureId.CalcFieldSize m.FigureId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: RemoveFigureCommand) ->
                FigureId.WriteField w m.FigureId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.RemoveFigureCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFigureId = FigureId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: RemoveFigureCommand) =
                    writeFigureId w m.FigureId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : RemoveFigureCommand =
                    match kvPair.Key with
                    | "figureId" -> { value with FigureId = FigureId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _RemoveFigureCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._RemoveFigureCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module DrawUnitCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitDefId: int // (1)
            val mutable Position: OptionBuilder<Highbar.Vector3> // (2)
            val mutable Rotation: float32 // (3)
            val mutable Lifespan: int // (4)
            val mutable TeamId: int // (5)
            val mutable Transparent: bool // (6)
            val mutable DrawBorder: bool // (7)
            val mutable Facing: int // (8)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitDefId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 3 -> x.Rotation <- ValueCodec.Float.ReadValue reader
            | 4 -> x.Lifespan <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.TeamId <- ValueCodec.Int32.ReadValue reader
            | 6 -> x.Transparent <- ValueCodec.Bool.ReadValue reader
            | 7 -> x.DrawBorder <- ValueCodec.Bool.ReadValue reader
            | 8 -> x.Facing <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.DrawUnitCommand = {
            UnitDefId = x.UnitDefId
            Position = x.Position.Build
            Rotation = x.Rotation
            Lifespan = x.Lifespan
            TeamId = x.TeamId
            Transparent = x.Transparent
            DrawBorder = x.DrawBorder
            Facing = x.Facing
            }

type private _DrawUnitCommand = DrawUnitCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type DrawUnitCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitDefId")>] UnitDefId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("rotation")>] Rotation: float32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("lifespan")>] Lifespan: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("teamId")>] TeamId: int // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("transparent")>] Transparent: bool // (6)
    [<System.Text.Json.Serialization.JsonPropertyName("drawBorder")>] DrawBorder: bool // (7)
    [<System.Text.Json.Serialization.JsonPropertyName("facing")>] Facing: int // (8)
    }
    with
    static member Proto : Lazy<ProtoDef<DrawUnitCommand>> =
        lazy
        // Field Definitions
        let UnitDefId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitDefId")
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (2, "position")
        let Rotation = FieldCodec.Primitive ValueCodec.Float (3, "rotation")
        let Lifespan = FieldCodec.Primitive ValueCodec.Int32 (4, "lifespan")
        let TeamId = FieldCodec.Primitive ValueCodec.Int32 (5, "teamId")
        let Transparent = FieldCodec.Primitive ValueCodec.Bool (6, "transparent")
        let DrawBorder = FieldCodec.Primitive ValueCodec.Bool (7, "drawBorder")
        let Facing = FieldCodec.Primitive ValueCodec.Int32 (8, "facing")
        // Proto Definition Implementation
        { // ProtoDef<DrawUnitCommand>
            Name = "DrawUnitCommand"
            Empty = {
                UnitDefId = UnitDefId.GetDefault()
                Position = Position.GetDefault()
                Rotation = Rotation.GetDefault()
                Lifespan = Lifespan.GetDefault()
                TeamId = TeamId.GetDefault()
                Transparent = Transparent.GetDefault()
                DrawBorder = DrawBorder.GetDefault()
                Facing = Facing.GetDefault()
                }
            Size = fun (m: DrawUnitCommand) ->
                0
                + UnitDefId.CalcFieldSize m.UnitDefId
                + Position.CalcFieldSize m.Position
                + Rotation.CalcFieldSize m.Rotation
                + Lifespan.CalcFieldSize m.Lifespan
                + TeamId.CalcFieldSize m.TeamId
                + Transparent.CalcFieldSize m.Transparent
                + DrawBorder.CalcFieldSize m.DrawBorder
                + Facing.CalcFieldSize m.Facing
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: DrawUnitCommand) ->
                UnitDefId.WriteField w m.UnitDefId
                Position.WriteField w m.Position
                Rotation.WriteField w m.Rotation
                Lifespan.WriteField w m.Lifespan
                TeamId.WriteField w m.TeamId
                Transparent.WriteField w m.Transparent
                DrawBorder.WriteField w m.DrawBorder
                Facing.WriteField w m.Facing
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.DrawUnitCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitDefId = UnitDefId.WriteJsonField o
                let writePosition = Position.WriteJsonField o
                let writeRotation = Rotation.WriteJsonField o
                let writeLifespan = Lifespan.WriteJsonField o
                let writeTeamId = TeamId.WriteJsonField o
                let writeTransparent = Transparent.WriteJsonField o
                let writeDrawBorder = DrawBorder.WriteJsonField o
                let writeFacing = Facing.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: DrawUnitCommand) =
                    writeUnitDefId w m.UnitDefId
                    writePosition w m.Position
                    writeRotation w m.Rotation
                    writeLifespan w m.Lifespan
                    writeTeamId w m.TeamId
                    writeTransparent w m.Transparent
                    writeDrawBorder w m.DrawBorder
                    writeFacing w m.Facing
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : DrawUnitCommand =
                    match kvPair.Key with
                    | "unitDefId" -> { value with UnitDefId = UnitDefId.ReadJsonField kvPair.Value }
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | "rotation" -> { value with Rotation = Rotation.ReadJsonField kvPair.Value }
                    | "lifespan" -> { value with Lifespan = Lifespan.ReadJsonField kvPair.Value }
                    | "teamId" -> { value with TeamId = TeamId.ReadJsonField kvPair.Value }
                    | "transparent" -> { value with Transparent = Transparent.ReadJsonField kvPair.Value }
                    | "drawBorder" -> { value with DrawBorder = DrawBorder.ReadJsonField kvPair.Value }
                    | "facing" -> { value with Facing = Facing.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _DrawUnitCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._DrawUnitCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module BuildUnitCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable ToBuildUnitDefId: int // (5)
            val mutable BuildPosition: OptionBuilder<Highbar.Vector3> // (6)
            val mutable Facing: int // (7)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.ToBuildUnitDefId <- ValueCodec.Int32.ReadValue reader
            | 6 -> x.BuildPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 7 -> x.Facing <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.BuildUnitCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            ToBuildUnitDefId = x.ToBuildUnitDefId
            BuildPosition = x.BuildPosition.Build
            Facing = x.Facing
            }

type private _BuildUnitCommand = BuildUnitCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type BuildUnitCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("toBuildUnitDefId")>] ToBuildUnitDefId: int // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("buildPosition")>] BuildPosition: Highbar.Vector3 option // (6)
    [<System.Text.Json.Serialization.JsonPropertyName("facing")>] Facing: int // (7)
    }
    with
    static member Proto : Lazy<ProtoDef<BuildUnitCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let ToBuildUnitDefId = FieldCodec.Primitive ValueCodec.Int32 (5, "toBuildUnitDefId")
        let BuildPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (6, "buildPosition")
        let Facing = FieldCodec.Primitive ValueCodec.Int32 (7, "facing")
        // Proto Definition Implementation
        { // ProtoDef<BuildUnitCommand>
            Name = "BuildUnitCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                ToBuildUnitDefId = ToBuildUnitDefId.GetDefault()
                BuildPosition = BuildPosition.GetDefault()
                Facing = Facing.GetDefault()
                }
            Size = fun (m: BuildUnitCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + ToBuildUnitDefId.CalcFieldSize m.ToBuildUnitDefId
                + BuildPosition.CalcFieldSize m.BuildPosition
                + Facing.CalcFieldSize m.Facing
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: BuildUnitCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                ToBuildUnitDefId.WriteField w m.ToBuildUnitDefId
                BuildPosition.WriteField w m.BuildPosition
                Facing.WriteField w m.Facing
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.BuildUnitCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeToBuildUnitDefId = ToBuildUnitDefId.WriteJsonField o
                let writeBuildPosition = BuildPosition.WriteJsonField o
                let writeFacing = Facing.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: BuildUnitCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeToBuildUnitDefId w m.ToBuildUnitDefId
                    writeBuildPosition w m.BuildPosition
                    writeFacing w m.Facing
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : BuildUnitCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "toBuildUnitDefId" -> { value with ToBuildUnitDefId = ToBuildUnitDefId.ReadJsonField kvPair.Value }
                    | "buildPosition" -> { value with BuildPosition = BuildPosition.ReadJsonField kvPair.Value }
                    | "facing" -> { value with Facing = Facing.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _BuildUnitCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._BuildUnitCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StopCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.StopCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            }

type private _StopCommand = StopCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type StopCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<StopCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        // Proto Definition Implementation
        { // ProtoDef<StopCommand>
            Name = "StopCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                }
            Size = fun (m: StopCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: StopCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.StopCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: StopCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : StopCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _StopCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._StopCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module WaitCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.WaitCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            }

type private _WaitCommand = WaitCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type WaitCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<WaitCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        // Proto Definition Implementation
        { // ProtoDef<WaitCommand>
            Name = "WaitCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                }
            Size = fun (m: WaitCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: WaitCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.WaitCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: WaitCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : WaitCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _WaitCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._WaitCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TimedWaitCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable WaitTime: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.WaitTime <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.TimedWaitCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            WaitTime = x.WaitTime
            }

type private _TimedWaitCommand = TimedWaitCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type TimedWaitCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("waitTime")>] WaitTime: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<TimedWaitCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let WaitTime = FieldCodec.Primitive ValueCodec.Int32 (5, "waitTime")
        // Proto Definition Implementation
        { // ProtoDef<TimedWaitCommand>
            Name = "TimedWaitCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                WaitTime = WaitTime.GetDefault()
                }
            Size = fun (m: TimedWaitCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + WaitTime.CalcFieldSize m.WaitTime
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: TimedWaitCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                WaitTime.WriteField w m.WaitTime
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.TimedWaitCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeWaitTime = WaitTime.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: TimedWaitCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeWaitTime w m.WaitTime
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : TimedWaitCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "waitTime" -> { value with WaitTime = WaitTime.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _TimedWaitCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._TimedWaitCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SquadWaitCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable NumUnits: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.NumUnits <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SquadWaitCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            NumUnits = x.NumUnits
            }

type private _SquadWaitCommand = SquadWaitCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SquadWaitCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("numUnits")>] NumUnits: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<SquadWaitCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let NumUnits = FieldCodec.Primitive ValueCodec.Int32 (5, "numUnits")
        // Proto Definition Implementation
        { // ProtoDef<SquadWaitCommand>
            Name = "SquadWaitCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                NumUnits = NumUnits.GetDefault()
                }
            Size = fun (m: SquadWaitCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + NumUnits.CalcFieldSize m.NumUnits
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SquadWaitCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                NumUnits.WriteField w m.NumUnits
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SquadWaitCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeNumUnits = NumUnits.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SquadWaitCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeNumUnits w m.NumUnits
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SquadWaitCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "numUnits" -> { value with NumUnits = NumUnits.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SquadWaitCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SquadWaitCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module DeathWaitCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable DeathUnitId: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.DeathUnitId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.DeathWaitCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            DeathUnitId = x.DeathUnitId
            }

type private _DeathWaitCommand = DeathWaitCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type DeathWaitCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("deathUnitId")>] DeathUnitId: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<DeathWaitCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let DeathUnitId = FieldCodec.Primitive ValueCodec.Int32 (5, "deathUnitId")
        // Proto Definition Implementation
        { // ProtoDef<DeathWaitCommand>
            Name = "DeathWaitCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                DeathUnitId = DeathUnitId.GetDefault()
                }
            Size = fun (m: DeathWaitCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + DeathUnitId.CalcFieldSize m.DeathUnitId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: DeathWaitCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                DeathUnitId.WriteField w m.DeathUnitId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.DeathWaitCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeDeathUnitId = DeathUnitId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: DeathWaitCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeDeathUnitId w m.DeathUnitId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : DeathWaitCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "deathUnitId" -> { value with DeathUnitId = DeathUnitId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _DeathWaitCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._DeathWaitCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GatherWaitCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.GatherWaitCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            }

type private _GatherWaitCommand = GatherWaitCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GatherWaitCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<GatherWaitCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        // Proto Definition Implementation
        { // ProtoDef<GatherWaitCommand>
            Name = "GatherWaitCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                }
            Size = fun (m: GatherWaitCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GatherWaitCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.GatherWaitCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GatherWaitCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GatherWaitCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GatherWaitCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._GatherWaitCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MoveUnitCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable ToPosition: OptionBuilder<Highbar.Vector3> // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.ToPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.MoveUnitCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            ToPosition = x.ToPosition.Build
            }

type private _MoveUnitCommand = MoveUnitCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type MoveUnitCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("toPosition")>] ToPosition: Highbar.Vector3 option // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<MoveUnitCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let ToPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (5, "toPosition")
        // Proto Definition Implementation
        { // ProtoDef<MoveUnitCommand>
            Name = "MoveUnitCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                ToPosition = ToPosition.GetDefault()
                }
            Size = fun (m: MoveUnitCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + ToPosition.CalcFieldSize m.ToPosition
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: MoveUnitCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                ToPosition.WriteField w m.ToPosition
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.MoveUnitCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeToPosition = ToPosition.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: MoveUnitCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeToPosition w m.ToPosition
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : MoveUnitCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "toPosition" -> { value with ToPosition = ToPosition.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _MoveUnitCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._MoveUnitCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PatrolCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable ToPosition: OptionBuilder<Highbar.Vector3> // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.ToPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.PatrolCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            ToPosition = x.ToPosition.Build
            }

type private _PatrolCommand = PatrolCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type PatrolCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("toPosition")>] ToPosition: Highbar.Vector3 option // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<PatrolCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let ToPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (5, "toPosition")
        // Proto Definition Implementation
        { // ProtoDef<PatrolCommand>
            Name = "PatrolCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                ToPosition = ToPosition.GetDefault()
                }
            Size = fun (m: PatrolCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + ToPosition.CalcFieldSize m.ToPosition
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: PatrolCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                ToPosition.WriteField w m.ToPosition
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.PatrolCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeToPosition = ToPosition.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: PatrolCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeToPosition w m.ToPosition
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : PatrolCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "toPosition" -> { value with ToPosition = ToPosition.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _PatrolCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._PatrolCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FightCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable ToPosition: OptionBuilder<Highbar.Vector3> // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.ToPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.FightCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            ToPosition = x.ToPosition.Build
            }

type private _FightCommand = FightCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type FightCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("toPosition")>] ToPosition: Highbar.Vector3 option // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<FightCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let ToPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (5, "toPosition")
        // Proto Definition Implementation
        { // ProtoDef<FightCommand>
            Name = "FightCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                ToPosition = ToPosition.GetDefault()
                }
            Size = fun (m: FightCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + ToPosition.CalcFieldSize m.ToPosition
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: FightCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                ToPosition.WriteField w m.ToPosition
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.FightCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeToPosition = ToPosition.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: FightCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeToPosition w m.ToPosition
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : FightCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "toPosition" -> { value with ToPosition = ToPosition.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _FightCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._FightCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AttackCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable TargetUnitId: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.TargetUnitId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.AttackCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            TargetUnitId = x.TargetUnitId
            }

type private _AttackCommand = AttackCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type AttackCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("targetUnitId")>] TargetUnitId: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<AttackCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let TargetUnitId = FieldCodec.Primitive ValueCodec.Int32 (5, "targetUnitId")
        // Proto Definition Implementation
        { // ProtoDef<AttackCommand>
            Name = "AttackCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                TargetUnitId = TargetUnitId.GetDefault()
                }
            Size = fun (m: AttackCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + TargetUnitId.CalcFieldSize m.TargetUnitId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: AttackCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                TargetUnitId.WriteField w m.TargetUnitId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.AttackCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeTargetUnitId = TargetUnitId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: AttackCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeTargetUnitId w m.TargetUnitId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : AttackCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "targetUnitId" -> { value with TargetUnitId = TargetUnitId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _AttackCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._AttackCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AttackAreaCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable AttackPosition: OptionBuilder<Highbar.Vector3> // (5)
            val mutable Radius: float32 // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.AttackPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 6 -> x.Radius <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.AttackAreaCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            AttackPosition = x.AttackPosition.Build
            Radius = x.Radius
            }

type private _AttackAreaCommand = AttackAreaCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type AttackAreaCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("attackPosition")>] AttackPosition: Highbar.Vector3 option // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("radius")>] Radius: float32 // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<AttackAreaCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let AttackPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (5, "attackPosition")
        let Radius = FieldCodec.Primitive ValueCodec.Float (6, "radius")
        // Proto Definition Implementation
        { // ProtoDef<AttackAreaCommand>
            Name = "AttackAreaCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                AttackPosition = AttackPosition.GetDefault()
                Radius = Radius.GetDefault()
                }
            Size = fun (m: AttackAreaCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + AttackPosition.CalcFieldSize m.AttackPosition
                + Radius.CalcFieldSize m.Radius
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: AttackAreaCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                AttackPosition.WriteField w m.AttackPosition
                Radius.WriteField w m.Radius
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.AttackAreaCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeAttackPosition = AttackPosition.WriteJsonField o
                let writeRadius = Radius.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: AttackAreaCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeAttackPosition w m.AttackPosition
                    writeRadius w m.Radius
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : AttackAreaCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "attackPosition" -> { value with AttackPosition = AttackPosition.ReadJsonField kvPair.Value }
                    | "radius" -> { value with Radius = Radius.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _AttackAreaCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._AttackAreaCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GuardCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable GuardUnitId: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.GuardUnitId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.GuardCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            GuardUnitId = x.GuardUnitId
            }

type private _GuardCommand = GuardCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GuardCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("guardUnitId")>] GuardUnitId: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<GuardCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let GuardUnitId = FieldCodec.Primitive ValueCodec.Int32 (5, "guardUnitId")
        // Proto Definition Implementation
        { // ProtoDef<GuardCommand>
            Name = "GuardCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                GuardUnitId = GuardUnitId.GetDefault()
                }
            Size = fun (m: GuardCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + GuardUnitId.CalcFieldSize m.GuardUnitId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GuardCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                GuardUnitId.WriteField w m.GuardUnitId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.GuardCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeGuardUnitId = GuardUnitId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GuardCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeGuardUnitId w m.GuardUnitId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GuardCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "guardUnitId" -> { value with GuardUnitId = GuardUnitId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GuardCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._GuardCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module RepairCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable RepairUnitId: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.RepairUnitId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.RepairCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            RepairUnitId = x.RepairUnitId
            }

type private _RepairCommand = RepairCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type RepairCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("repairUnitId")>] RepairUnitId: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<RepairCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let RepairUnitId = FieldCodec.Primitive ValueCodec.Int32 (5, "repairUnitId")
        // Proto Definition Implementation
        { // ProtoDef<RepairCommand>
            Name = "RepairCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                RepairUnitId = RepairUnitId.GetDefault()
                }
            Size = fun (m: RepairCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + RepairUnitId.CalcFieldSize m.RepairUnitId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: RepairCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                RepairUnitId.WriteField w m.RepairUnitId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.RepairCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeRepairUnitId = RepairUnitId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: RepairCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeRepairUnitId w m.RepairUnitId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : RepairCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "repairUnitId" -> { value with RepairUnitId = RepairUnitId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _RepairCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._RepairCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ReclaimUnitCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable ReclaimUnitId: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.ReclaimUnitId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.ReclaimUnitCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            ReclaimUnitId = x.ReclaimUnitId
            }

type private _ReclaimUnitCommand = ReclaimUnitCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ReclaimUnitCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("reclaimUnitId")>] ReclaimUnitId: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<ReclaimUnitCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let ReclaimUnitId = FieldCodec.Primitive ValueCodec.Int32 (5, "reclaimUnitId")
        // Proto Definition Implementation
        { // ProtoDef<ReclaimUnitCommand>
            Name = "ReclaimUnitCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                ReclaimUnitId = ReclaimUnitId.GetDefault()
                }
            Size = fun (m: ReclaimUnitCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + ReclaimUnitId.CalcFieldSize m.ReclaimUnitId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ReclaimUnitCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                ReclaimUnitId.WriteField w m.ReclaimUnitId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.ReclaimUnitCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeReclaimUnitId = ReclaimUnitId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ReclaimUnitCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeReclaimUnitId w m.ReclaimUnitId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ReclaimUnitCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "reclaimUnitId" -> { value with ReclaimUnitId = ReclaimUnitId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ReclaimUnitCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._ReclaimUnitCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ReclaimAreaCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable Position: OptionBuilder<Highbar.Vector3> // (5)
            val mutable Radius: float32 // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 6 -> x.Radius <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.ReclaimAreaCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            Position = x.Position.Build
            Radius = x.Radius
            }

type private _ReclaimAreaCommand = ReclaimAreaCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ReclaimAreaCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("radius")>] Radius: float32 // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<ReclaimAreaCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (5, "position")
        let Radius = FieldCodec.Primitive ValueCodec.Float (6, "radius")
        // Proto Definition Implementation
        { // ProtoDef<ReclaimAreaCommand>
            Name = "ReclaimAreaCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                Position = Position.GetDefault()
                Radius = Radius.GetDefault()
                }
            Size = fun (m: ReclaimAreaCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + Position.CalcFieldSize m.Position
                + Radius.CalcFieldSize m.Radius
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ReclaimAreaCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                Position.WriteField w m.Position
                Radius.WriteField w m.Radius
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.ReclaimAreaCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writePosition = Position.WriteJsonField o
                let writeRadius = Radius.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ReclaimAreaCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writePosition w m.Position
                    writeRadius w m.Radius
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ReclaimAreaCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | "radius" -> { value with Radius = Radius.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ReclaimAreaCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._ReclaimAreaCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ReclaimInAreaCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable Position: OptionBuilder<Highbar.Vector3> // (5)
            val mutable Radius: float32 // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 6 -> x.Radius <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.ReclaimInAreaCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            Position = x.Position.Build
            Radius = x.Radius
            }

type private _ReclaimInAreaCommand = ReclaimInAreaCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ReclaimInAreaCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("radius")>] Radius: float32 // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<ReclaimInAreaCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (5, "position")
        let Radius = FieldCodec.Primitive ValueCodec.Float (6, "radius")
        // Proto Definition Implementation
        { // ProtoDef<ReclaimInAreaCommand>
            Name = "ReclaimInAreaCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                Position = Position.GetDefault()
                Radius = Radius.GetDefault()
                }
            Size = fun (m: ReclaimInAreaCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + Position.CalcFieldSize m.Position
                + Radius.CalcFieldSize m.Radius
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ReclaimInAreaCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                Position.WriteField w m.Position
                Radius.WriteField w m.Radius
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.ReclaimInAreaCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writePosition = Position.WriteJsonField o
                let writeRadius = Radius.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ReclaimInAreaCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writePosition w m.Position
                    writeRadius w m.Radius
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ReclaimInAreaCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | "radius" -> { value with Radius = Radius.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ReclaimInAreaCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._ReclaimInAreaCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ReclaimFeatureCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable FeatureId: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.FeatureId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.ReclaimFeatureCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            FeatureId = x.FeatureId
            }

type private _ReclaimFeatureCommand = ReclaimFeatureCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ReclaimFeatureCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("featureId")>] FeatureId: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<ReclaimFeatureCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let FeatureId = FieldCodec.Primitive ValueCodec.Int32 (5, "featureId")
        // Proto Definition Implementation
        { // ProtoDef<ReclaimFeatureCommand>
            Name = "ReclaimFeatureCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                FeatureId = FeatureId.GetDefault()
                }
            Size = fun (m: ReclaimFeatureCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + FeatureId.CalcFieldSize m.FeatureId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ReclaimFeatureCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                FeatureId.WriteField w m.FeatureId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.ReclaimFeatureCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeFeatureId = FeatureId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ReclaimFeatureCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeFeatureId w m.FeatureId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ReclaimFeatureCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "featureId" -> { value with FeatureId = FeatureId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ReclaimFeatureCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._ReclaimFeatureCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module RestoreAreaCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable Position: OptionBuilder<Highbar.Vector3> // (5)
            val mutable Radius: float32 // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 6 -> x.Radius <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.RestoreAreaCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            Position = x.Position.Build
            Radius = x.Radius
            }

type private _RestoreAreaCommand = RestoreAreaCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type RestoreAreaCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("radius")>] Radius: float32 // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<RestoreAreaCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (5, "position")
        let Radius = FieldCodec.Primitive ValueCodec.Float (6, "radius")
        // Proto Definition Implementation
        { // ProtoDef<RestoreAreaCommand>
            Name = "RestoreAreaCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                Position = Position.GetDefault()
                Radius = Radius.GetDefault()
                }
            Size = fun (m: RestoreAreaCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + Position.CalcFieldSize m.Position
                + Radius.CalcFieldSize m.Radius
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: RestoreAreaCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                Position.WriteField w m.Position
                Radius.WriteField w m.Radius
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.RestoreAreaCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writePosition = Position.WriteJsonField o
                let writeRadius = Radius.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: RestoreAreaCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writePosition w m.Position
                    writeRadius w m.Radius
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : RestoreAreaCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | "radius" -> { value with Radius = Radius.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _RestoreAreaCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._RestoreAreaCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ResurrectCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable FeatureId: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.FeatureId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.ResurrectCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            FeatureId = x.FeatureId
            }

type private _ResurrectCommand = ResurrectCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ResurrectCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("featureId")>] FeatureId: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<ResurrectCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let FeatureId = FieldCodec.Primitive ValueCodec.Int32 (5, "featureId")
        // Proto Definition Implementation
        { // ProtoDef<ResurrectCommand>
            Name = "ResurrectCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                FeatureId = FeatureId.GetDefault()
                }
            Size = fun (m: ResurrectCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + FeatureId.CalcFieldSize m.FeatureId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ResurrectCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                FeatureId.WriteField w m.FeatureId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.ResurrectCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeFeatureId = FeatureId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ResurrectCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeFeatureId w m.FeatureId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ResurrectCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "featureId" -> { value with FeatureId = FeatureId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ResurrectCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._ResurrectCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ResurrectInAreaCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable Position: OptionBuilder<Highbar.Vector3> // (5)
            val mutable Radius: float32 // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 6 -> x.Radius <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.ResurrectInAreaCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            Position = x.Position.Build
            Radius = x.Radius
            }

type private _ResurrectInAreaCommand = ResurrectInAreaCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ResurrectInAreaCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("radius")>] Radius: float32 // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<ResurrectInAreaCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (5, "position")
        let Radius = FieldCodec.Primitive ValueCodec.Float (6, "radius")
        // Proto Definition Implementation
        { // ProtoDef<ResurrectInAreaCommand>
            Name = "ResurrectInAreaCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                Position = Position.GetDefault()
                Radius = Radius.GetDefault()
                }
            Size = fun (m: ResurrectInAreaCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + Position.CalcFieldSize m.Position
                + Radius.CalcFieldSize m.Radius
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ResurrectInAreaCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                Position.WriteField w m.Position
                Radius.WriteField w m.Radius
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.ResurrectInAreaCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writePosition = Position.WriteJsonField o
                let writeRadius = Radius.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ResurrectInAreaCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writePosition w m.Position
                    writeRadius w m.Radius
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ResurrectInAreaCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | "radius" -> { value with Radius = Radius.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ResurrectInAreaCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._ResurrectInAreaCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CaptureCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable TargetUnitId: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.TargetUnitId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.CaptureCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            TargetUnitId = x.TargetUnitId
            }

type private _CaptureCommand = CaptureCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CaptureCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("targetUnitId")>] TargetUnitId: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<CaptureCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let TargetUnitId = FieldCodec.Primitive ValueCodec.Int32 (5, "targetUnitId")
        // Proto Definition Implementation
        { // ProtoDef<CaptureCommand>
            Name = "CaptureCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                TargetUnitId = TargetUnitId.GetDefault()
                }
            Size = fun (m: CaptureCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + TargetUnitId.CalcFieldSize m.TargetUnitId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CaptureCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                TargetUnitId.WriteField w m.TargetUnitId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.CaptureCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeTargetUnitId = TargetUnitId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CaptureCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeTargetUnitId w m.TargetUnitId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CaptureCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "targetUnitId" -> { value with TargetUnitId = TargetUnitId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CaptureCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CaptureCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CaptureAreaCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable Position: OptionBuilder<Highbar.Vector3> // (5)
            val mutable Radius: float32 // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 6 -> x.Radius <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.CaptureAreaCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            Position = x.Position.Build
            Radius = x.Radius
            }

type private _CaptureAreaCommand = CaptureAreaCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CaptureAreaCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("radius")>] Radius: float32 // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<CaptureAreaCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (5, "position")
        let Radius = FieldCodec.Primitive ValueCodec.Float (6, "radius")
        // Proto Definition Implementation
        { // ProtoDef<CaptureAreaCommand>
            Name = "CaptureAreaCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                Position = Position.GetDefault()
                Radius = Radius.GetDefault()
                }
            Size = fun (m: CaptureAreaCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + Position.CalcFieldSize m.Position
                + Radius.CalcFieldSize m.Radius
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CaptureAreaCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                Position.WriteField w m.Position
                Radius.WriteField w m.Radius
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.CaptureAreaCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writePosition = Position.WriteJsonField o
                let writeRadius = Radius.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CaptureAreaCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writePosition w m.Position
                    writeRadius w m.Radius
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CaptureAreaCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | "radius" -> { value with Radius = Radius.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CaptureAreaCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CaptureAreaCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetBaseCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable BasePosition: OptionBuilder<Highbar.Vector3> // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.BasePosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetBaseCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            BasePosition = x.BasePosition.Build
            }

type private _SetBaseCommand = SetBaseCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetBaseCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("basePosition")>] BasePosition: Highbar.Vector3 option // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<SetBaseCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let BasePosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (5, "basePosition")
        // Proto Definition Implementation
        { // ProtoDef<SetBaseCommand>
            Name = "SetBaseCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                BasePosition = BasePosition.GetDefault()
                }
            Size = fun (m: SetBaseCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + BasePosition.CalcFieldSize m.BasePosition
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetBaseCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                BasePosition.WriteField w m.BasePosition
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetBaseCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeBasePosition = BasePosition.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetBaseCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeBasePosition w m.BasePosition
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetBaseCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "basePosition" -> { value with BasePosition = BasePosition.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetBaseCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetBaseCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SelfDestructCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SelfDestructCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            }

type private _SelfDestructCommand = SelfDestructCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SelfDestructCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<SelfDestructCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        // Proto Definition Implementation
        { // ProtoDef<SelfDestructCommand>
            Name = "SelfDestructCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                }
            Size = fun (m: SelfDestructCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SelfDestructCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SelfDestructCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SelfDestructCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SelfDestructCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SelfDestructCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SelfDestructCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LoadUnitsCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable ToLoadUnitIds: RepeatedBuilder<int> // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.ToLoadUnitIds.AddRange ((ValueCodec.Packed ValueCodec.Int32).ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.LoadUnitsCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            ToLoadUnitIds = x.ToLoadUnitIds.Build
            }

type private _LoadUnitsCommand = LoadUnitsCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LoadUnitsCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("toLoadUnitIds")>] ToLoadUnitIds: int list // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<LoadUnitsCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let ToLoadUnitIds = FieldCodec.Primitive (ValueCodec.Packed ValueCodec.Int32) (5, "toLoadUnitIds")
        // Proto Definition Implementation
        { // ProtoDef<LoadUnitsCommand>
            Name = "LoadUnitsCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                ToLoadUnitIds = ToLoadUnitIds.GetDefault()
                }
            Size = fun (m: LoadUnitsCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + ToLoadUnitIds.CalcFieldSize m.ToLoadUnitIds
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LoadUnitsCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                ToLoadUnitIds.WriteField w m.ToLoadUnitIds
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.LoadUnitsCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeToLoadUnitIds = ToLoadUnitIds.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LoadUnitsCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeToLoadUnitIds w m.ToLoadUnitIds
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LoadUnitsCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "toLoadUnitIds" -> { value with ToLoadUnitIds = ToLoadUnitIds.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LoadUnitsCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._LoadUnitsCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LoadUnitsAreaCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable Position: OptionBuilder<Highbar.Vector3> // (5)
            val mutable Radius: float32 // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 6 -> x.Radius <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.LoadUnitsAreaCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            Position = x.Position.Build
            Radius = x.Radius
            }

type private _LoadUnitsAreaCommand = LoadUnitsAreaCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LoadUnitsAreaCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("radius")>] Radius: float32 // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<LoadUnitsAreaCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (5, "position")
        let Radius = FieldCodec.Primitive ValueCodec.Float (6, "radius")
        // Proto Definition Implementation
        { // ProtoDef<LoadUnitsAreaCommand>
            Name = "LoadUnitsAreaCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                Position = Position.GetDefault()
                Radius = Radius.GetDefault()
                }
            Size = fun (m: LoadUnitsAreaCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + Position.CalcFieldSize m.Position
                + Radius.CalcFieldSize m.Radius
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LoadUnitsAreaCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                Position.WriteField w m.Position
                Radius.WriteField w m.Radius
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.LoadUnitsAreaCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writePosition = Position.WriteJsonField o
                let writeRadius = Radius.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LoadUnitsAreaCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writePosition w m.Position
                    writeRadius w m.Radius
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LoadUnitsAreaCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | "radius" -> { value with Radius = Radius.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LoadUnitsAreaCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._LoadUnitsAreaCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LoadOntoCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable TransportUnitId: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.TransportUnitId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.LoadOntoCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            TransportUnitId = x.TransportUnitId
            }

type private _LoadOntoCommand = LoadOntoCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LoadOntoCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("transportUnitId")>] TransportUnitId: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<LoadOntoCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let TransportUnitId = FieldCodec.Primitive ValueCodec.Int32 (5, "transportUnitId")
        // Proto Definition Implementation
        { // ProtoDef<LoadOntoCommand>
            Name = "LoadOntoCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                TransportUnitId = TransportUnitId.GetDefault()
                }
            Size = fun (m: LoadOntoCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + TransportUnitId.CalcFieldSize m.TransportUnitId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LoadOntoCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                TransportUnitId.WriteField w m.TransportUnitId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.LoadOntoCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeTransportUnitId = TransportUnitId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LoadOntoCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeTransportUnitId w m.TransportUnitId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LoadOntoCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "transportUnitId" -> { value with TransportUnitId = TransportUnitId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LoadOntoCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._LoadOntoCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UnloadUnitCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable ToPosition: OptionBuilder<Highbar.Vector3> // (5)
            val mutable ToUnloadUnitId: int // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.ToPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 6 -> x.ToUnloadUnitId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.UnloadUnitCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            ToPosition = x.ToPosition.Build
            ToUnloadUnitId = x.ToUnloadUnitId
            }

type private _UnloadUnitCommand = UnloadUnitCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type UnloadUnitCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("toPosition")>] ToPosition: Highbar.Vector3 option // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("toUnloadUnitId")>] ToUnloadUnitId: int // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<UnloadUnitCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let ToPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (5, "toPosition")
        let ToUnloadUnitId = FieldCodec.Primitive ValueCodec.Int32 (6, "toUnloadUnitId")
        // Proto Definition Implementation
        { // ProtoDef<UnloadUnitCommand>
            Name = "UnloadUnitCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                ToPosition = ToPosition.GetDefault()
                ToUnloadUnitId = ToUnloadUnitId.GetDefault()
                }
            Size = fun (m: UnloadUnitCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + ToPosition.CalcFieldSize m.ToPosition
                + ToUnloadUnitId.CalcFieldSize m.ToUnloadUnitId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: UnloadUnitCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                ToPosition.WriteField w m.ToPosition
                ToUnloadUnitId.WriteField w m.ToUnloadUnitId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.UnloadUnitCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeToPosition = ToPosition.WriteJsonField o
                let writeToUnloadUnitId = ToUnloadUnitId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: UnloadUnitCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeToPosition w m.ToPosition
                    writeToUnloadUnitId w m.ToUnloadUnitId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : UnloadUnitCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "toPosition" -> { value with ToPosition = ToPosition.ReadJsonField kvPair.Value }
                    | "toUnloadUnitId" -> { value with ToUnloadUnitId = ToUnloadUnitId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _UnloadUnitCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._UnloadUnitCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UnloadUnitsAreaCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable ToPosition: OptionBuilder<Highbar.Vector3> // (5)
            val mutable Radius: float32 // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.ToPosition.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 6 -> x.Radius <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.UnloadUnitsAreaCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            ToPosition = x.ToPosition.Build
            Radius = x.Radius
            }

type private _UnloadUnitsAreaCommand = UnloadUnitsAreaCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type UnloadUnitsAreaCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("toPosition")>] ToPosition: Highbar.Vector3 option // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("radius")>] Radius: float32 // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<UnloadUnitsAreaCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let ToPosition = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (5, "toPosition")
        let Radius = FieldCodec.Primitive ValueCodec.Float (6, "radius")
        // Proto Definition Implementation
        { // ProtoDef<UnloadUnitsAreaCommand>
            Name = "UnloadUnitsAreaCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                ToPosition = ToPosition.GetDefault()
                Radius = Radius.GetDefault()
                }
            Size = fun (m: UnloadUnitsAreaCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + ToPosition.CalcFieldSize m.ToPosition
                + Radius.CalcFieldSize m.Radius
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: UnloadUnitsAreaCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                ToPosition.WriteField w m.ToPosition
                Radius.WriteField w m.Radius
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.UnloadUnitsAreaCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeToPosition = ToPosition.WriteJsonField o
                let writeRadius = Radius.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: UnloadUnitsAreaCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeToPosition w m.ToPosition
                    writeRadius w m.Radius
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : UnloadUnitsAreaCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "toPosition" -> { value with ToPosition = ToPosition.ReadJsonField kvPair.Value }
                    | "radius" -> { value with Radius = Radius.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _UnloadUnitsAreaCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._UnloadUnitsAreaCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetWantedMaxSpeedCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable WantedMaxSpeed: float32 // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.WantedMaxSpeed <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetWantedMaxSpeedCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            WantedMaxSpeed = x.WantedMaxSpeed
            }

type private _SetWantedMaxSpeedCommand = SetWantedMaxSpeedCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetWantedMaxSpeedCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("wantedMaxSpeed")>] WantedMaxSpeed: float32 // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<SetWantedMaxSpeedCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let WantedMaxSpeed = FieldCodec.Primitive ValueCodec.Float (5, "wantedMaxSpeed")
        // Proto Definition Implementation
        { // ProtoDef<SetWantedMaxSpeedCommand>
            Name = "SetWantedMaxSpeedCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                WantedMaxSpeed = WantedMaxSpeed.GetDefault()
                }
            Size = fun (m: SetWantedMaxSpeedCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + WantedMaxSpeed.CalcFieldSize m.WantedMaxSpeed
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetWantedMaxSpeedCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                WantedMaxSpeed.WriteField w m.WantedMaxSpeed
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetWantedMaxSpeedCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeWantedMaxSpeed = WantedMaxSpeed.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetWantedMaxSpeedCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeWantedMaxSpeed w m.WantedMaxSpeed
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetWantedMaxSpeedCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "wantedMaxSpeed" -> { value with WantedMaxSpeed = WantedMaxSpeed.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetWantedMaxSpeedCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetWantedMaxSpeedCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StockpileCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.StockpileCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            }

type private _StockpileCommand = StockpileCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type StockpileCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<StockpileCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        // Proto Definition Implementation
        { // ProtoDef<StockpileCommand>
            Name = "StockpileCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                }
            Size = fun (m: StockpileCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: StockpileCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.StockpileCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: StockpileCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : StockpileCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _StockpileCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._StockpileCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module DGunCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable TargetUnitId: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.TargetUnitId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.DGunCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            TargetUnitId = x.TargetUnitId
            }

type private _DGunCommand = DGunCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type DGunCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("targetUnitId")>] TargetUnitId: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<DGunCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let TargetUnitId = FieldCodec.Primitive ValueCodec.Int32 (5, "targetUnitId")
        // Proto Definition Implementation
        { // ProtoDef<DGunCommand>
            Name = "DGunCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                TargetUnitId = TargetUnitId.GetDefault()
                }
            Size = fun (m: DGunCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + TargetUnitId.CalcFieldSize m.TargetUnitId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: DGunCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                TargetUnitId.WriteField w m.TargetUnitId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.DGunCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeTargetUnitId = TargetUnitId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: DGunCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeTargetUnitId w m.TargetUnitId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : DGunCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "targetUnitId" -> { value with TargetUnitId = TargetUnitId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _DGunCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._DGunCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CustomCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable CommandId: int // (5)
            val mutable Params: RepeatedBuilder<float32> // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.CommandId <- ValueCodec.Int32.ReadValue reader
            | 6 -> x.Params.AddRange ((ValueCodec.Packed ValueCodec.Float).ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.CustomCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            CommandId = x.CommandId
            Params = x.Params.Build
            }

type private _CustomCommand = CustomCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CustomCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("commandId")>] CommandId: int // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("params")>] Params: float32 list // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<CustomCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let CommandId = FieldCodec.Primitive ValueCodec.Int32 (5, "commandId")
        let Params = FieldCodec.Primitive (ValueCodec.Packed ValueCodec.Float) (6, "params")
        // Proto Definition Implementation
        { // ProtoDef<CustomCommand>
            Name = "CustomCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                CommandId = CommandId.GetDefault()
                Params = Params.GetDefault()
                }
            Size = fun (m: CustomCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + CommandId.CalcFieldSize m.CommandId
                + Params.CalcFieldSize m.Params
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CustomCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                CommandId.WriteField w m.CommandId
                Params.WriteField w m.Params
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.CustomCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeCommandId = CommandId.WriteJsonField o
                let writeParams = Params.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CustomCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeCommandId w m.CommandId
                    writeParams w m.Params
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CustomCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "commandId" -> { value with CommandId = CommandId.ReadJsonField kvPair.Value }
                    | "params" -> { value with Params = Params.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CustomCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CustomCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetOnOffCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable On: bool // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.On <- ValueCodec.Bool.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetOnOffCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            On = x.On
            }

type private _SetOnOffCommand = SetOnOffCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetOnOffCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("on")>] On: bool // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<SetOnOffCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let On = FieldCodec.Primitive ValueCodec.Bool (5, "on")
        // Proto Definition Implementation
        { // ProtoDef<SetOnOffCommand>
            Name = "SetOnOffCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                On = On.GetDefault()
                }
            Size = fun (m: SetOnOffCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + On.CalcFieldSize m.On
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetOnOffCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                On.WriteField w m.On
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetOnOffCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeOn = On.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetOnOffCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeOn w m.On
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetOnOffCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "on" -> { value with On = On.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetOnOffCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetOnOffCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetRepeatCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable Repeat: bool // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.Repeat <- ValueCodec.Bool.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetRepeatCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            Repeat = x.Repeat
            }

type private _SetRepeatCommand = SetRepeatCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetRepeatCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("repeat")>] Repeat: bool // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<SetRepeatCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let Repeat = FieldCodec.Primitive ValueCodec.Bool (5, "repeat")
        // Proto Definition Implementation
        { // ProtoDef<SetRepeatCommand>
            Name = "SetRepeatCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                Repeat = Repeat.GetDefault()
                }
            Size = fun (m: SetRepeatCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + Repeat.CalcFieldSize m.Repeat
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetRepeatCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                Repeat.WriteField w m.Repeat
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetRepeatCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeRepeat = Repeat.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetRepeatCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeRepeat w m.Repeat
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetRepeatCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "repeat" -> { value with Repeat = Repeat.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetRepeatCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetRepeatCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetMoveStateCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable MoveState: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.MoveState <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetMoveStateCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            MoveState = x.MoveState
            }

type private _SetMoveStateCommand = SetMoveStateCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetMoveStateCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("moveState")>] MoveState: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<SetMoveStateCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let MoveState = FieldCodec.Primitive ValueCodec.Int32 (5, "moveState")
        // Proto Definition Implementation
        { // ProtoDef<SetMoveStateCommand>
            Name = "SetMoveStateCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                MoveState = MoveState.GetDefault()
                }
            Size = fun (m: SetMoveStateCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + MoveState.CalcFieldSize m.MoveState
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetMoveStateCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                MoveState.WriteField w m.MoveState
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetMoveStateCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeMoveState = MoveState.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetMoveStateCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeMoveState w m.MoveState
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetMoveStateCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "moveState" -> { value with MoveState = MoveState.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetMoveStateCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetMoveStateCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetFireStateCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable FireState: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.FireState <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetFireStateCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            FireState = x.FireState
            }

type private _SetFireStateCommand = SetFireStateCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetFireStateCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("fireState")>] FireState: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<SetFireStateCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let FireState = FieldCodec.Primitive ValueCodec.Int32 (5, "fireState")
        // Proto Definition Implementation
        { // ProtoDef<SetFireStateCommand>
            Name = "SetFireStateCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                FireState = FireState.GetDefault()
                }
            Size = fun (m: SetFireStateCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + FireState.CalcFieldSize m.FireState
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetFireStateCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                FireState.WriteField w m.FireState
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetFireStateCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeFireState = FireState.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetFireStateCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeFireState w m.FireState
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetFireStateCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "fireState" -> { value with FireState = FireState.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetFireStateCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetFireStateCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetTrajectoryCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable Trajectory: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.Trajectory <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetTrajectoryCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            Trajectory = x.Trajectory
            }

type private _SetTrajectoryCommand = SetTrajectoryCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetTrajectoryCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("trajectory")>] Trajectory: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<SetTrajectoryCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let Trajectory = FieldCodec.Primitive ValueCodec.Int32 (5, "trajectory")
        // Proto Definition Implementation
        { // ProtoDef<SetTrajectoryCommand>
            Name = "SetTrajectoryCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                Trajectory = Trajectory.GetDefault()
                }
            Size = fun (m: SetTrajectoryCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + Trajectory.CalcFieldSize m.Trajectory
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetTrajectoryCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                Trajectory.WriteField w m.Trajectory
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetTrajectoryCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeTrajectory = Trajectory.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetTrajectoryCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeTrajectory w m.Trajectory
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetTrajectoryCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "trajectory" -> { value with Trajectory = Trajectory.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetTrajectoryCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetTrajectoryCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetAutoRepairLevelCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable AutoRepairLevel: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.AutoRepairLevel <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetAutoRepairLevelCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            AutoRepairLevel = x.AutoRepairLevel
            }

type private _SetAutoRepairLevelCommand = SetAutoRepairLevelCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetAutoRepairLevelCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("autoRepairLevel")>] AutoRepairLevel: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<SetAutoRepairLevelCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let AutoRepairLevel = FieldCodec.Primitive ValueCodec.Int32 (5, "autoRepairLevel")
        // Proto Definition Implementation
        { // ProtoDef<SetAutoRepairLevelCommand>
            Name = "SetAutoRepairLevelCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                AutoRepairLevel = AutoRepairLevel.GetDefault()
                }
            Size = fun (m: SetAutoRepairLevelCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + AutoRepairLevel.CalcFieldSize m.AutoRepairLevel
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetAutoRepairLevelCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                AutoRepairLevel.WriteField w m.AutoRepairLevel
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetAutoRepairLevelCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeAutoRepairLevel = AutoRepairLevel.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetAutoRepairLevelCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeAutoRepairLevel w m.AutoRepairLevel
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetAutoRepairLevelCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "autoRepairLevel" -> { value with AutoRepairLevel = AutoRepairLevel.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetAutoRepairLevelCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetAutoRepairLevelCommand.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetIdleModeCommand =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable GroupId: int // (2)
            val mutable Options: uint32 // (3)
            val mutable Timeout: int // (4)
            val mutable IdleMode: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.GroupId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Options <- ValueCodec.UInt32.ReadValue reader
            | 4 -> x.Timeout <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.IdleMode <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SetIdleModeCommand = {
            UnitId = x.UnitId
            GroupId = x.GroupId
            Options = x.Options
            Timeout = x.Timeout
            IdleMode = x.IdleMode
            }

type private _SetIdleModeCommand = SetIdleModeCommand
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetIdleModeCommand = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("groupId")>] GroupId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("options")>] Options: uint32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("timeout")>] Timeout: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("idleMode")>] IdleMode: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<SetIdleModeCommand>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let GroupId = FieldCodec.Primitive ValueCodec.Int32 (2, "groupId")
        let Options = FieldCodec.Primitive ValueCodec.UInt32 (3, "options")
        let Timeout = FieldCodec.Primitive ValueCodec.Int32 (4, "timeout")
        let IdleMode = FieldCodec.Primitive ValueCodec.Int32 (5, "idleMode")
        // Proto Definition Implementation
        { // ProtoDef<SetIdleModeCommand>
            Name = "SetIdleModeCommand"
            Empty = {
                UnitId = UnitId.GetDefault()
                GroupId = GroupId.GetDefault()
                Options = Options.GetDefault()
                Timeout = Timeout.GetDefault()
                IdleMode = IdleMode.GetDefault()
                }
            Size = fun (m: SetIdleModeCommand) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + GroupId.CalcFieldSize m.GroupId
                + Options.CalcFieldSize m.Options
                + Timeout.CalcFieldSize m.Timeout
                + IdleMode.CalcFieldSize m.IdleMode
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetIdleModeCommand) ->
                UnitId.WriteField w m.UnitId
                GroupId.WriteField w m.GroupId
                Options.WriteField w m.Options
                Timeout.WriteField w m.Timeout
                IdleMode.WriteField w m.IdleMode
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SetIdleModeCommand.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeGroupId = GroupId.WriteJsonField o
                let writeOptions = Options.WriteJsonField o
                let writeTimeout = Timeout.WriteJsonField o
                let writeIdleMode = IdleMode.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetIdleModeCommand) =
                    writeUnitId w m.UnitId
                    writeGroupId w m.GroupId
                    writeOptions w m.Options
                    writeTimeout w m.Timeout
                    writeIdleMode w m.IdleMode
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetIdleModeCommand =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "groupId" -> { value with GroupId = GroupId.ReadJsonField kvPair.Value }
                    | "options" -> { value with Options = Options.ReadJsonField kvPair.Value }
                    | "timeout" -> { value with Timeout = Timeout.ReadJsonField kvPair.Value }
                    | "idleMode" -> { value with IdleMode = IdleMode.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetIdleModeCommand.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SetIdleModeCommand.Proto.Value.Empty

