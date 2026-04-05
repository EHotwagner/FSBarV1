namespace FSBar.Client

module Commands =

    /// INTERNAL_ORDER flag (bit 3) used for AI-issued commands
    [<Literal>]
    let private INTERNAL_ORDER = 8u

    /// Maximum timeout value
    [<Literal>]
    let private MAX_TIMEOUT = 2147483647

    /// Create a move command for a unit to a position
    let MoveCommand (unitId: int) (x: float32) (y: float32) (z: float32) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.MoveUnit {
            UnitId = unitId
            GroupId = 0
            Options = INTERNAL_ORDER
            Timeout = MAX_TIMEOUT
            ToPosition = Some { X = x; Y = y; Z = z }
        }}

    /// Create a build command for a unit to construct a building
    let BuildCommand (unitId: int) (toBuildUnitDefId: int) (x: float32) (y: float32) (z: float32) (facing: int) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.BuildUnit {
            UnitId = unitId
            GroupId = 0
            Options = INTERNAL_ORDER
            Timeout = MAX_TIMEOUT
            ToBuildUnitDefId = toBuildUnitDefId
            BuildPosition = Some { X = x; Y = y; Z = z }
            Facing = facing
        }}

    /// Create a patrol command for a unit to a position
    let PatrolCommand (unitId: int) (x: float32) (y: float32) (z: float32) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.Patrol {
            UnitId = unitId
            GroupId = 0
            Options = INTERNAL_ORDER
            Timeout = MAX_TIMEOUT
            ToPosition = Some { X = x; Y = y; Z = z }
        }}

    /// Create an attack command for a unit to attack a target unit
    let AttackCommand (unitId: int) (targetUnitId: int) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.Attack {
            UnitId = unitId
            GroupId = 0
            Options = INTERNAL_ORDER
            Timeout = MAX_TIMEOUT
            TargetUnitId = targetUnitId
        }}

    /// Create a guard command for a unit to guard another unit
    let GuardCommand (unitId: int) (guardUnitId: int) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.Guard {
            UnitId = unitId
            GroupId = 0
            Options = INTERNAL_ORDER
            Timeout = MAX_TIMEOUT
            GuardUnitId = guardUnitId
        }}

    /// Create a stop command for a unit
    let StopCommand (unitId: int) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.Stop {
            UnitId = unitId
            GroupId = 0
            Options = INTERNAL_ORDER
            Timeout = MAX_TIMEOUT
        }}

    /// Create a repair command for a unit to repair another unit
    let RepairCommand (unitId: int) (repairUnitId: int) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.Repair {
            UnitId = unitId
            GroupId = 0
            Options = INTERNAL_ORDER
            Timeout = MAX_TIMEOUT
            RepairUnitId = repairUnitId
        }}

    /// Create a reclaim command for a unit to reclaim another unit
    let ReclaimUnitCommand (unitId: int) (reclaimUnitId: int) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.ReclaimUnit {
            UnitId = unitId
            GroupId = 0
            Options = INTERNAL_ORDER
            Timeout = MAX_TIMEOUT
            ReclaimUnitId = reclaimUnitId
        }}

    /// Create a fight command for a unit to fight-move to a position
    let FightCommand (unitId: int) (x: float32) (y: float32) (z: float32) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.Fight {
            UnitId = unitId
            GroupId = 0
            Options = INTERNAL_ORDER
            Timeout = MAX_TIMEOUT
            ToPosition = Some { X = x; Y = y; Z = z }
        }}

    /// Create a self-destruct command for a unit
    let SelfDestructCommand (unitId: int) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.SelfDestruct {
            UnitId = unitId
            GroupId = 0
            Options = INTERNAL_ORDER
            Timeout = MAX_TIMEOUT
        }}

    /// Create a set wanted max speed command for a unit
    let SetWantedMaxSpeedCommand (unitId: int) (wantedMaxSpeed: float32) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.SetWantedMaxSpeed {
            UnitId = unitId
            GroupId = 0
            Options = INTERNAL_ORDER
            Timeout = MAX_TIMEOUT
            WantedMaxSpeed = wantedMaxSpeed
        }}

    /// Create a custom command for a unit
    let CustomCommand (unitId: int) (commandId: int) (``params``: float32 list) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.Custom {
            UnitId = unitId
            GroupId = 0
            Options = INTERNAL_ORDER
            Timeout = MAX_TIMEOUT
            CommandId = commandId
            Params = ``params``
        }}

    /// Create a send text message command
    let SendTextMessageCommand (text: string) (zone: int) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.SendTextMessage {
            Text = text
            Zone = zone
        }}

    /// Create a give me resource command (cheat)
    let GiveMeResourceCommand (resourceId: int) (amount: float32) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.GiveMe {
            ResourceId = resourceId
            Amount = amount
        }}

    /// Create a give me new unit command (cheat)
    let GiveMeNewUnitCommand (unitDefId: int) (x: float32) (y: float32) (z: float32) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.GiveMeNewUnit {
            UnitDefId = unitDefId
            Position = Some { X = x; Y = y; Z = z }
        }}

    /// Create a call Lua rules command
    let CallLuaRulesCommand (data: string) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.CallLuaRules {
            Data = data
        }}

    /// Create a call Lua UI command
    let CallLuaUICommand (data: string) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.CallLuaUi {
            Data = data
        }}
