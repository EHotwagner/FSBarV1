namespace FSBar.Client

module Commands =

    /// Create a move command for a unit to a position
    val MoveCommand: unitId: int -> x: float32 -> y: float32 -> z: float32 -> Highbar.AICommand

    /// Create a build command for a unit to construct a building
    val BuildCommand: unitId: int -> toBuildUnitDefId: int -> x: float32 -> y: float32 -> z: float32 -> facing: int -> Highbar.AICommand

    /// Create a patrol command for a unit to a position
    val PatrolCommand: unitId: int -> x: float32 -> y: float32 -> z: float32 -> Highbar.AICommand

    /// Create an attack command for a unit to attack a target unit
    val AttackCommand: unitId: int -> targetUnitId: int -> Highbar.AICommand

    /// Create a guard command for a unit to guard another unit
    val GuardCommand: unitId: int -> guardUnitId: int -> Highbar.AICommand

    /// Create a stop command for a unit
    val StopCommand: unitId: int -> Highbar.AICommand

    /// Create a repair command for a unit to repair another unit
    val RepairCommand: unitId: int -> repairUnitId: int -> Highbar.AICommand

    /// Create a reclaim command for a unit to reclaim another unit
    val ReclaimUnitCommand: unitId: int -> reclaimUnitId: int -> Highbar.AICommand

    /// Create a fight command for a unit to fight-move to a position
    val FightCommand: unitId: int -> x: float32 -> y: float32 -> z: float32 -> Highbar.AICommand

    /// Create a self-destruct command for a unit
    val SelfDestructCommand: unitId: int -> Highbar.AICommand

    /// Create a set wanted max speed command for a unit
    val SetWantedMaxSpeedCommand: unitId: int -> wantedMaxSpeed: float32 -> Highbar.AICommand

    /// Create a custom command for a unit
    val CustomCommand: unitId: int -> commandId: int -> ``params``: float32 list -> Highbar.AICommand

    /// Create a send text message command
    val SendTextMessageCommand: text: string -> zone: int -> Highbar.AICommand

    /// Create a give me resource command (cheat)
    val GiveMeResourceCommand: resourceId: int -> amount: float32 -> Highbar.AICommand

    /// Create a give me new unit command (cheat)
    val GiveMeNewUnitCommand: unitDefId: int -> x: float32 -> y: float32 -> z: float32 -> Highbar.AICommand

    /// Create a call Lua rules command
    val CallLuaRulesCommand: data: string -> Highbar.AICommand

    /// Create a call Lua UI command
    val CallLuaUICommand: data: string -> Highbar.AICommand
