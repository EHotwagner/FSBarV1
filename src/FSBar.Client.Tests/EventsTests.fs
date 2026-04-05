module FSBar.Client.Tests.EventsTests

open Xunit
open FSBar.Client
open Highbar

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
let ``fromProto_EnemyCreated_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.EnemyCreated { EnemyId = 55 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.EnemyCreated 55, result)

[<Fact>]
let ``fromProto_EnemyFinished_maps_correctly`` () =
    let evt : EngineEvent = { Event = EngineEvent.EventCase.EnemyFinished { EnemyId = 44 } }
    let result = Events.fromProto evt
    Assert.Equal(GameEvent.EnemyFinished 44, result)

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
