namespace rec Highbar
open FsGrpc.Protobuf
open Google.Protobuf
#nowarn "40"
#nowarn "1182"


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module EngineEvent =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.OneofConverter<EventCase>>)>]
    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    [<StructuralEquality;StructuralComparison>]
    [<RequireQualifiedAccess>]
    type EventCase =
    | None
    | [<System.Text.Json.Serialization.JsonPropertyName("init")>] Init of Highbar.InitEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("release")>] Release of Highbar.ReleaseEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("update")>] Update of Highbar.UpdateEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("message")>] Message of Highbar.MessageEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("unitCreated")>] UnitCreated of Highbar.UnitCreatedEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("unitFinished")>] UnitFinished of Highbar.UnitFinishedEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("unitIdle")>] UnitIdle of Highbar.UnitIdleEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("unitMoveFailed")>] UnitMoveFailed of Highbar.UnitMoveFailedEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("unitDamaged")>] UnitDamaged of Highbar.UnitDamagedEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("unitDestroyed")>] UnitDestroyed of Highbar.UnitDestroyedEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("unitGiven")>] UnitGiven of Highbar.UnitGivenEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("unitCaptured")>] UnitCaptured of Highbar.UnitCapturedEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("enemyEnterLos")>] EnemyEnterLos of Highbar.EnemyEnterLOSEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("enemyLeaveLos")>] EnemyLeaveLos of Highbar.EnemyLeaveLOSEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("enemyEnterRadar")>] EnemyEnterRadar of Highbar.EnemyEnterRadarEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("enemyLeaveRadar")>] EnemyLeaveRadar of Highbar.EnemyLeaveRadarEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("enemyDamaged")>] EnemyDamaged of Highbar.EnemyDamagedEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("enemyDestroyed")>] EnemyDestroyed of Highbar.EnemyDestroyedEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("weaponFired")>] WeaponFired of Highbar.WeaponFiredEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("playerCommand")>] PlayerCommand of Highbar.PlayerCommandEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("seismicPing")>] SeismicPing of Highbar.SeismicPingEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("commandFinished")>] CommandFinished of Highbar.CommandFinishedEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("load")>] Load of Highbar.LoadEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("save")>] Save of Highbar.SaveEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("enemyCreated")>] EnemyCreated of Highbar.EnemyCreatedEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("enemyFinished")>] EnemyFinished of Highbar.EnemyFinishedEvent
    | [<System.Text.Json.Serialization.JsonPropertyName("luaMessage")>] LuaMessage of Highbar.LuaMessageEvent
    with
        static member OneofCodec : Lazy<OneofCodec<EventCase>> = 
            lazy
            let Init = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.InitEvent> (1, "init")
            let Release = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.ReleaseEvent> (2, "release")
            let Update = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UpdateEvent> (3, "update")
            let Message = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.MessageEvent> (4, "message")
            let UnitCreated = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitCreatedEvent> (5, "unitCreated")
            let UnitFinished = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitFinishedEvent> (6, "unitFinished")
            let UnitIdle = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitIdleEvent> (7, "unitIdle")
            let UnitMoveFailed = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitMoveFailedEvent> (8, "unitMoveFailed")
            let UnitDamaged = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitDamagedEvent> (9, "unitDamaged")
            let UnitDestroyed = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitDestroyedEvent> (10, "unitDestroyed")
            let UnitGiven = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitGivenEvent> (11, "unitGiven")
            let UnitCaptured = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitCapturedEvent> (12, "unitCaptured")
            let EnemyEnterLos = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyEnterLOSEvent> (13, "enemyEnterLos")
            let EnemyLeaveLos = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyLeaveLOSEvent> (14, "enemyLeaveLos")
            let EnemyEnterRadar = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyEnterRadarEvent> (15, "enemyEnterRadar")
            let EnemyLeaveRadar = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyLeaveRadarEvent> (16, "enemyLeaveRadar")
            let EnemyDamaged = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyDamagedEvent> (17, "enemyDamaged")
            let EnemyDestroyed = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyDestroyedEvent> (18, "enemyDestroyed")
            let WeaponFired = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.WeaponFiredEvent> (19, "weaponFired")
            let PlayerCommand = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.PlayerCommandEvent> (20, "playerCommand")
            let SeismicPing = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.SeismicPingEvent> (21, "seismicPing")
            let CommandFinished = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.CommandFinishedEvent> (22, "commandFinished")
            let Load = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.LoadEvent> (23, "load")
            let Save = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.SaveEvent> (24, "save")
            let EnemyCreated = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyCreatedEvent> (25, "enemyCreated")
            let EnemyFinished = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyFinishedEvent> (26, "enemyFinished")
            let LuaMessage = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.LuaMessageEvent> (27, "luaMessage")
            let Event = FieldCodec.Oneof "event" (FSharp.Collections.Map [
                ("init", fun node -> EventCase.Init (Init.ReadJsonField node))
                ("release", fun node -> EventCase.Release (Release.ReadJsonField node))
                ("update", fun node -> EventCase.Update (Update.ReadJsonField node))
                ("message", fun node -> EventCase.Message (Message.ReadJsonField node))
                ("unitCreated", fun node -> EventCase.UnitCreated (UnitCreated.ReadJsonField node))
                ("unitFinished", fun node -> EventCase.UnitFinished (UnitFinished.ReadJsonField node))
                ("unitIdle", fun node -> EventCase.UnitIdle (UnitIdle.ReadJsonField node))
                ("unitMoveFailed", fun node -> EventCase.UnitMoveFailed (UnitMoveFailed.ReadJsonField node))
                ("unitDamaged", fun node -> EventCase.UnitDamaged (UnitDamaged.ReadJsonField node))
                ("unitDestroyed", fun node -> EventCase.UnitDestroyed (UnitDestroyed.ReadJsonField node))
                ("unitGiven", fun node -> EventCase.UnitGiven (UnitGiven.ReadJsonField node))
                ("unitCaptured", fun node -> EventCase.UnitCaptured (UnitCaptured.ReadJsonField node))
                ("enemyEnterLos", fun node -> EventCase.EnemyEnterLos (EnemyEnterLos.ReadJsonField node))
                ("enemyLeaveLos", fun node -> EventCase.EnemyLeaveLos (EnemyLeaveLos.ReadJsonField node))
                ("enemyEnterRadar", fun node -> EventCase.EnemyEnterRadar (EnemyEnterRadar.ReadJsonField node))
                ("enemyLeaveRadar", fun node -> EventCase.EnemyLeaveRadar (EnemyLeaveRadar.ReadJsonField node))
                ("enemyDamaged", fun node -> EventCase.EnemyDamaged (EnemyDamaged.ReadJsonField node))
                ("enemyDestroyed", fun node -> EventCase.EnemyDestroyed (EnemyDestroyed.ReadJsonField node))
                ("weaponFired", fun node -> EventCase.WeaponFired (WeaponFired.ReadJsonField node))
                ("playerCommand", fun node -> EventCase.PlayerCommand (PlayerCommand.ReadJsonField node))
                ("seismicPing", fun node -> EventCase.SeismicPing (SeismicPing.ReadJsonField node))
                ("commandFinished", fun node -> EventCase.CommandFinished (CommandFinished.ReadJsonField node))
                ("load", fun node -> EventCase.Load (Load.ReadJsonField node))
                ("save", fun node -> EventCase.Save (Save.ReadJsonField node))
                ("enemyCreated", fun node -> EventCase.EnemyCreated (EnemyCreated.ReadJsonField node))
                ("enemyFinished", fun node -> EventCase.EnemyFinished (EnemyFinished.ReadJsonField node))
                ("luaMessage", fun node -> EventCase.LuaMessage (LuaMessage.ReadJsonField node))
                ])
            Event

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Event: OptionBuilder<Highbar.EngineEvent.EventCase>
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Event.Set (EventCase.Init (ValueCodec.Message<Highbar.InitEvent>.ReadValue reader))
            | 2 -> x.Event.Set (EventCase.Release (ValueCodec.Message<Highbar.ReleaseEvent>.ReadValue reader))
            | 3 -> x.Event.Set (EventCase.Update (ValueCodec.Message<Highbar.UpdateEvent>.ReadValue reader))
            | 4 -> x.Event.Set (EventCase.Message (ValueCodec.Message<Highbar.MessageEvent>.ReadValue reader))
            | 5 -> x.Event.Set (EventCase.UnitCreated (ValueCodec.Message<Highbar.UnitCreatedEvent>.ReadValue reader))
            | 6 -> x.Event.Set (EventCase.UnitFinished (ValueCodec.Message<Highbar.UnitFinishedEvent>.ReadValue reader))
            | 7 -> x.Event.Set (EventCase.UnitIdle (ValueCodec.Message<Highbar.UnitIdleEvent>.ReadValue reader))
            | 8 -> x.Event.Set (EventCase.UnitMoveFailed (ValueCodec.Message<Highbar.UnitMoveFailedEvent>.ReadValue reader))
            | 9 -> x.Event.Set (EventCase.UnitDamaged (ValueCodec.Message<Highbar.UnitDamagedEvent>.ReadValue reader))
            | 10 -> x.Event.Set (EventCase.UnitDestroyed (ValueCodec.Message<Highbar.UnitDestroyedEvent>.ReadValue reader))
            | 11 -> x.Event.Set (EventCase.UnitGiven (ValueCodec.Message<Highbar.UnitGivenEvent>.ReadValue reader))
            | 12 -> x.Event.Set (EventCase.UnitCaptured (ValueCodec.Message<Highbar.UnitCapturedEvent>.ReadValue reader))
            | 13 -> x.Event.Set (EventCase.EnemyEnterLos (ValueCodec.Message<Highbar.EnemyEnterLOSEvent>.ReadValue reader))
            | 14 -> x.Event.Set (EventCase.EnemyLeaveLos (ValueCodec.Message<Highbar.EnemyLeaveLOSEvent>.ReadValue reader))
            | 15 -> x.Event.Set (EventCase.EnemyEnterRadar (ValueCodec.Message<Highbar.EnemyEnterRadarEvent>.ReadValue reader))
            | 16 -> x.Event.Set (EventCase.EnemyLeaveRadar (ValueCodec.Message<Highbar.EnemyLeaveRadarEvent>.ReadValue reader))
            | 17 -> x.Event.Set (EventCase.EnemyDamaged (ValueCodec.Message<Highbar.EnemyDamagedEvent>.ReadValue reader))
            | 18 -> x.Event.Set (EventCase.EnemyDestroyed (ValueCodec.Message<Highbar.EnemyDestroyedEvent>.ReadValue reader))
            | 19 -> x.Event.Set (EventCase.WeaponFired (ValueCodec.Message<Highbar.WeaponFiredEvent>.ReadValue reader))
            | 20 -> x.Event.Set (EventCase.PlayerCommand (ValueCodec.Message<Highbar.PlayerCommandEvent>.ReadValue reader))
            | 21 -> x.Event.Set (EventCase.SeismicPing (ValueCodec.Message<Highbar.SeismicPingEvent>.ReadValue reader))
            | 22 -> x.Event.Set (EventCase.CommandFinished (ValueCodec.Message<Highbar.CommandFinishedEvent>.ReadValue reader))
            | 23 -> x.Event.Set (EventCase.Load (ValueCodec.Message<Highbar.LoadEvent>.ReadValue reader))
            | 24 -> x.Event.Set (EventCase.Save (ValueCodec.Message<Highbar.SaveEvent>.ReadValue reader))
            | 25 -> x.Event.Set (EventCase.EnemyCreated (ValueCodec.Message<Highbar.EnemyCreatedEvent>.ReadValue reader))
            | 26 -> x.Event.Set (EventCase.EnemyFinished (ValueCodec.Message<Highbar.EnemyFinishedEvent>.ReadValue reader))
            | 27 -> x.Event.Set (EventCase.LuaMessage (ValueCodec.Message<Highbar.LuaMessageEvent>.ReadValue reader))
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.EngineEvent = {
            Event = x.Event.Build |> (Option.defaultValue EventCase.None)
            }

/// <summary>Container for all engine event types (28 variants)</summary>
type private _EngineEvent = EngineEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type EngineEvent = {
    // Field Declarations
    Event: Highbar.EngineEvent.EventCase
    }
    with
    static member Proto : Lazy<ProtoDef<EngineEvent>> =
        lazy
        // Field Definitions
        let Init = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.InitEvent> (1, "init")
        let Release = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.ReleaseEvent> (2, "release")
        let Update = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UpdateEvent> (3, "update")
        let Message = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.MessageEvent> (4, "message")
        let UnitCreated = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitCreatedEvent> (5, "unitCreated")
        let UnitFinished = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitFinishedEvent> (6, "unitFinished")
        let UnitIdle = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitIdleEvent> (7, "unitIdle")
        let UnitMoveFailed = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitMoveFailedEvent> (8, "unitMoveFailed")
        let UnitDamaged = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitDamagedEvent> (9, "unitDamaged")
        let UnitDestroyed = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitDestroyedEvent> (10, "unitDestroyed")
        let UnitGiven = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitGivenEvent> (11, "unitGiven")
        let UnitCaptured = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.UnitCapturedEvent> (12, "unitCaptured")
        let EnemyEnterLos = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyEnterLOSEvent> (13, "enemyEnterLos")
        let EnemyLeaveLos = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyLeaveLOSEvent> (14, "enemyLeaveLos")
        let EnemyEnterRadar = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyEnterRadarEvent> (15, "enemyEnterRadar")
        let EnemyLeaveRadar = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyLeaveRadarEvent> (16, "enemyLeaveRadar")
        let EnemyDamaged = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyDamagedEvent> (17, "enemyDamaged")
        let EnemyDestroyed = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyDestroyedEvent> (18, "enemyDestroyed")
        let WeaponFired = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.WeaponFiredEvent> (19, "weaponFired")
        let PlayerCommand = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.PlayerCommandEvent> (20, "playerCommand")
        let SeismicPing = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.SeismicPingEvent> (21, "seismicPing")
        let CommandFinished = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.CommandFinishedEvent> (22, "commandFinished")
        let Load = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.LoadEvent> (23, "load")
        let Save = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.SaveEvent> (24, "save")
        let EnemyCreated = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyCreatedEvent> (25, "enemyCreated")
        let EnemyFinished = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.EnemyFinishedEvent> (26, "enemyFinished")
        let LuaMessage = FieldCodec.OneofCase "event" ValueCodec.Message<Highbar.LuaMessageEvent> (27, "luaMessage")
        let Event = FieldCodec.Oneof "event" (FSharp.Collections.Map [
            ("init", fun node -> Highbar.EngineEvent.EventCase.Init (Init.ReadJsonField node))
            ("release", fun node -> Highbar.EngineEvent.EventCase.Release (Release.ReadJsonField node))
            ("update", fun node -> Highbar.EngineEvent.EventCase.Update (Update.ReadJsonField node))
            ("message", fun node -> Highbar.EngineEvent.EventCase.Message (Message.ReadJsonField node))
            ("unitCreated", fun node -> Highbar.EngineEvent.EventCase.UnitCreated (UnitCreated.ReadJsonField node))
            ("unitFinished", fun node -> Highbar.EngineEvent.EventCase.UnitFinished (UnitFinished.ReadJsonField node))
            ("unitIdle", fun node -> Highbar.EngineEvent.EventCase.UnitIdle (UnitIdle.ReadJsonField node))
            ("unitMoveFailed", fun node -> Highbar.EngineEvent.EventCase.UnitMoveFailed (UnitMoveFailed.ReadJsonField node))
            ("unitDamaged", fun node -> Highbar.EngineEvent.EventCase.UnitDamaged (UnitDamaged.ReadJsonField node))
            ("unitDestroyed", fun node -> Highbar.EngineEvent.EventCase.UnitDestroyed (UnitDestroyed.ReadJsonField node))
            ("unitGiven", fun node -> Highbar.EngineEvent.EventCase.UnitGiven (UnitGiven.ReadJsonField node))
            ("unitCaptured", fun node -> Highbar.EngineEvent.EventCase.UnitCaptured (UnitCaptured.ReadJsonField node))
            ("enemyEnterLos", fun node -> Highbar.EngineEvent.EventCase.EnemyEnterLos (EnemyEnterLos.ReadJsonField node))
            ("enemyLeaveLos", fun node -> Highbar.EngineEvent.EventCase.EnemyLeaveLos (EnemyLeaveLos.ReadJsonField node))
            ("enemyEnterRadar", fun node -> Highbar.EngineEvent.EventCase.EnemyEnterRadar (EnemyEnterRadar.ReadJsonField node))
            ("enemyLeaveRadar", fun node -> Highbar.EngineEvent.EventCase.EnemyLeaveRadar (EnemyLeaveRadar.ReadJsonField node))
            ("enemyDamaged", fun node -> Highbar.EngineEvent.EventCase.EnemyDamaged (EnemyDamaged.ReadJsonField node))
            ("enemyDestroyed", fun node -> Highbar.EngineEvent.EventCase.EnemyDestroyed (EnemyDestroyed.ReadJsonField node))
            ("weaponFired", fun node -> Highbar.EngineEvent.EventCase.WeaponFired (WeaponFired.ReadJsonField node))
            ("playerCommand", fun node -> Highbar.EngineEvent.EventCase.PlayerCommand (PlayerCommand.ReadJsonField node))
            ("seismicPing", fun node -> Highbar.EngineEvent.EventCase.SeismicPing (SeismicPing.ReadJsonField node))
            ("commandFinished", fun node -> Highbar.EngineEvent.EventCase.CommandFinished (CommandFinished.ReadJsonField node))
            ("load", fun node -> Highbar.EngineEvent.EventCase.Load (Load.ReadJsonField node))
            ("save", fun node -> Highbar.EngineEvent.EventCase.Save (Save.ReadJsonField node))
            ("enemyCreated", fun node -> Highbar.EngineEvent.EventCase.EnemyCreated (EnemyCreated.ReadJsonField node))
            ("enemyFinished", fun node -> Highbar.EngineEvent.EventCase.EnemyFinished (EnemyFinished.ReadJsonField node))
            ("luaMessage", fun node -> Highbar.EngineEvent.EventCase.LuaMessage (LuaMessage.ReadJsonField node))
            ])
        // Proto Definition Implementation
        { // ProtoDef<EngineEvent>
            Name = "EngineEvent"
            Empty = {
                Event = Highbar.EngineEvent.EventCase.None
                }
            Size = fun (m: EngineEvent) ->
                0
                + match m.Event with
                    | Highbar.EngineEvent.EventCase.None -> 0
                    | Highbar.EngineEvent.EventCase.Init v -> Init.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.Release v -> Release.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.Update v -> Update.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.Message v -> Message.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.UnitCreated v -> UnitCreated.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.UnitFinished v -> UnitFinished.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.UnitIdle v -> UnitIdle.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.UnitMoveFailed v -> UnitMoveFailed.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.UnitDamaged v -> UnitDamaged.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.UnitDestroyed v -> UnitDestroyed.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.UnitGiven v -> UnitGiven.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.UnitCaptured v -> UnitCaptured.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.EnemyEnterLos v -> EnemyEnterLos.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.EnemyLeaveLos v -> EnemyLeaveLos.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.EnemyEnterRadar v -> EnemyEnterRadar.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.EnemyLeaveRadar v -> EnemyLeaveRadar.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.EnemyDamaged v -> EnemyDamaged.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.EnemyDestroyed v -> EnemyDestroyed.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.WeaponFired v -> WeaponFired.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.PlayerCommand v -> PlayerCommand.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.SeismicPing v -> SeismicPing.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.CommandFinished v -> CommandFinished.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.Load v -> Load.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.Save v -> Save.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.EnemyCreated v -> EnemyCreated.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.EnemyFinished v -> EnemyFinished.CalcFieldSize v
                    | Highbar.EngineEvent.EventCase.LuaMessage v -> LuaMessage.CalcFieldSize v
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: EngineEvent) ->
                (match m.Event with
                | Highbar.EngineEvent.EventCase.None -> ()
                | Highbar.EngineEvent.EventCase.Init v -> Init.WriteField w v
                | Highbar.EngineEvent.EventCase.Release v -> Release.WriteField w v
                | Highbar.EngineEvent.EventCase.Update v -> Update.WriteField w v
                | Highbar.EngineEvent.EventCase.Message v -> Message.WriteField w v
                | Highbar.EngineEvent.EventCase.UnitCreated v -> UnitCreated.WriteField w v
                | Highbar.EngineEvent.EventCase.UnitFinished v -> UnitFinished.WriteField w v
                | Highbar.EngineEvent.EventCase.UnitIdle v -> UnitIdle.WriteField w v
                | Highbar.EngineEvent.EventCase.UnitMoveFailed v -> UnitMoveFailed.WriteField w v
                | Highbar.EngineEvent.EventCase.UnitDamaged v -> UnitDamaged.WriteField w v
                | Highbar.EngineEvent.EventCase.UnitDestroyed v -> UnitDestroyed.WriteField w v
                | Highbar.EngineEvent.EventCase.UnitGiven v -> UnitGiven.WriteField w v
                | Highbar.EngineEvent.EventCase.UnitCaptured v -> UnitCaptured.WriteField w v
                | Highbar.EngineEvent.EventCase.EnemyEnterLos v -> EnemyEnterLos.WriteField w v
                | Highbar.EngineEvent.EventCase.EnemyLeaveLos v -> EnemyLeaveLos.WriteField w v
                | Highbar.EngineEvent.EventCase.EnemyEnterRadar v -> EnemyEnterRadar.WriteField w v
                | Highbar.EngineEvent.EventCase.EnemyLeaveRadar v -> EnemyLeaveRadar.WriteField w v
                | Highbar.EngineEvent.EventCase.EnemyDamaged v -> EnemyDamaged.WriteField w v
                | Highbar.EngineEvent.EventCase.EnemyDestroyed v -> EnemyDestroyed.WriteField w v
                | Highbar.EngineEvent.EventCase.WeaponFired v -> WeaponFired.WriteField w v
                | Highbar.EngineEvent.EventCase.PlayerCommand v -> PlayerCommand.WriteField w v
                | Highbar.EngineEvent.EventCase.SeismicPing v -> SeismicPing.WriteField w v
                | Highbar.EngineEvent.EventCase.CommandFinished v -> CommandFinished.WriteField w v
                | Highbar.EngineEvent.EventCase.Load v -> Load.WriteField w v
                | Highbar.EngineEvent.EventCase.Save v -> Save.WriteField w v
                | Highbar.EngineEvent.EventCase.EnemyCreated v -> EnemyCreated.WriteField w v
                | Highbar.EngineEvent.EventCase.EnemyFinished v -> EnemyFinished.WriteField w v
                | Highbar.EngineEvent.EventCase.LuaMessage v -> LuaMessage.WriteField w v
                )
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.EngineEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeEventNone = Event.WriteJsonNoneCase o
                let writeInit = Init.WriteJsonField o
                let writeRelease = Release.WriteJsonField o
                let writeUpdate = Update.WriteJsonField o
                let writeMessage = Message.WriteJsonField o
                let writeUnitCreated = UnitCreated.WriteJsonField o
                let writeUnitFinished = UnitFinished.WriteJsonField o
                let writeUnitIdle = UnitIdle.WriteJsonField o
                let writeUnitMoveFailed = UnitMoveFailed.WriteJsonField o
                let writeUnitDamaged = UnitDamaged.WriteJsonField o
                let writeUnitDestroyed = UnitDestroyed.WriteJsonField o
                let writeUnitGiven = UnitGiven.WriteJsonField o
                let writeUnitCaptured = UnitCaptured.WriteJsonField o
                let writeEnemyEnterLos = EnemyEnterLos.WriteJsonField o
                let writeEnemyLeaveLos = EnemyLeaveLos.WriteJsonField o
                let writeEnemyEnterRadar = EnemyEnterRadar.WriteJsonField o
                let writeEnemyLeaveRadar = EnemyLeaveRadar.WriteJsonField o
                let writeEnemyDamaged = EnemyDamaged.WriteJsonField o
                let writeEnemyDestroyed = EnemyDestroyed.WriteJsonField o
                let writeWeaponFired = WeaponFired.WriteJsonField o
                let writePlayerCommand = PlayerCommand.WriteJsonField o
                let writeSeismicPing = SeismicPing.WriteJsonField o
                let writeCommandFinished = CommandFinished.WriteJsonField o
                let writeLoad = Load.WriteJsonField o
                let writeSave = Save.WriteJsonField o
                let writeEnemyCreated = EnemyCreated.WriteJsonField o
                let writeEnemyFinished = EnemyFinished.WriteJsonField o
                let writeLuaMessage = LuaMessage.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: EngineEvent) =
                    (match m.Event with
                    | Highbar.EngineEvent.EventCase.None -> writeEventNone w
                    | Highbar.EngineEvent.EventCase.Init v -> writeInit w v
                    | Highbar.EngineEvent.EventCase.Release v -> writeRelease w v
                    | Highbar.EngineEvent.EventCase.Update v -> writeUpdate w v
                    | Highbar.EngineEvent.EventCase.Message v -> writeMessage w v
                    | Highbar.EngineEvent.EventCase.UnitCreated v -> writeUnitCreated w v
                    | Highbar.EngineEvent.EventCase.UnitFinished v -> writeUnitFinished w v
                    | Highbar.EngineEvent.EventCase.UnitIdle v -> writeUnitIdle w v
                    | Highbar.EngineEvent.EventCase.UnitMoveFailed v -> writeUnitMoveFailed w v
                    | Highbar.EngineEvent.EventCase.UnitDamaged v -> writeUnitDamaged w v
                    | Highbar.EngineEvent.EventCase.UnitDestroyed v -> writeUnitDestroyed w v
                    | Highbar.EngineEvent.EventCase.UnitGiven v -> writeUnitGiven w v
                    | Highbar.EngineEvent.EventCase.UnitCaptured v -> writeUnitCaptured w v
                    | Highbar.EngineEvent.EventCase.EnemyEnterLos v -> writeEnemyEnterLos w v
                    | Highbar.EngineEvent.EventCase.EnemyLeaveLos v -> writeEnemyLeaveLos w v
                    | Highbar.EngineEvent.EventCase.EnemyEnterRadar v -> writeEnemyEnterRadar w v
                    | Highbar.EngineEvent.EventCase.EnemyLeaveRadar v -> writeEnemyLeaveRadar w v
                    | Highbar.EngineEvent.EventCase.EnemyDamaged v -> writeEnemyDamaged w v
                    | Highbar.EngineEvent.EventCase.EnemyDestroyed v -> writeEnemyDestroyed w v
                    | Highbar.EngineEvent.EventCase.WeaponFired v -> writeWeaponFired w v
                    | Highbar.EngineEvent.EventCase.PlayerCommand v -> writePlayerCommand w v
                    | Highbar.EngineEvent.EventCase.SeismicPing v -> writeSeismicPing w v
                    | Highbar.EngineEvent.EventCase.CommandFinished v -> writeCommandFinished w v
                    | Highbar.EngineEvent.EventCase.Load v -> writeLoad w v
                    | Highbar.EngineEvent.EventCase.Save v -> writeSave w v
                    | Highbar.EngineEvent.EventCase.EnemyCreated v -> writeEnemyCreated w v
                    | Highbar.EngineEvent.EventCase.EnemyFinished v -> writeEnemyFinished w v
                    | Highbar.EngineEvent.EventCase.LuaMessage v -> writeLuaMessage w v
                    )
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : EngineEvent =
                    match kvPair.Key with
                    | "init" -> { value with Event = Highbar.EngineEvent.EventCase.Init (Init.ReadJsonField kvPair.Value) }
                    | "release" -> { value with Event = Highbar.EngineEvent.EventCase.Release (Release.ReadJsonField kvPair.Value) }
                    | "update" -> { value with Event = Highbar.EngineEvent.EventCase.Update (Update.ReadJsonField kvPair.Value) }
                    | "message" -> { value with Event = Highbar.EngineEvent.EventCase.Message (Message.ReadJsonField kvPair.Value) }
                    | "unitCreated" -> { value with Event = Highbar.EngineEvent.EventCase.UnitCreated (UnitCreated.ReadJsonField kvPair.Value) }
                    | "unitFinished" -> { value with Event = Highbar.EngineEvent.EventCase.UnitFinished (UnitFinished.ReadJsonField kvPair.Value) }
                    | "unitIdle" -> { value with Event = Highbar.EngineEvent.EventCase.UnitIdle (UnitIdle.ReadJsonField kvPair.Value) }
                    | "unitMoveFailed" -> { value with Event = Highbar.EngineEvent.EventCase.UnitMoveFailed (UnitMoveFailed.ReadJsonField kvPair.Value) }
                    | "unitDamaged" -> { value with Event = Highbar.EngineEvent.EventCase.UnitDamaged (UnitDamaged.ReadJsonField kvPair.Value) }
                    | "unitDestroyed" -> { value with Event = Highbar.EngineEvent.EventCase.UnitDestroyed (UnitDestroyed.ReadJsonField kvPair.Value) }
                    | "unitGiven" -> { value with Event = Highbar.EngineEvent.EventCase.UnitGiven (UnitGiven.ReadJsonField kvPair.Value) }
                    | "unitCaptured" -> { value with Event = Highbar.EngineEvent.EventCase.UnitCaptured (UnitCaptured.ReadJsonField kvPair.Value) }
                    | "enemyEnterLos" -> { value with Event = Highbar.EngineEvent.EventCase.EnemyEnterLos (EnemyEnterLos.ReadJsonField kvPair.Value) }
                    | "enemyLeaveLos" -> { value with Event = Highbar.EngineEvent.EventCase.EnemyLeaveLos (EnemyLeaveLos.ReadJsonField kvPair.Value) }
                    | "enemyEnterRadar" -> { value with Event = Highbar.EngineEvent.EventCase.EnemyEnterRadar (EnemyEnterRadar.ReadJsonField kvPair.Value) }
                    | "enemyLeaveRadar" -> { value with Event = Highbar.EngineEvent.EventCase.EnemyLeaveRadar (EnemyLeaveRadar.ReadJsonField kvPair.Value) }
                    | "enemyDamaged" -> { value with Event = Highbar.EngineEvent.EventCase.EnemyDamaged (EnemyDamaged.ReadJsonField kvPair.Value) }
                    | "enemyDestroyed" -> { value with Event = Highbar.EngineEvent.EventCase.EnemyDestroyed (EnemyDestroyed.ReadJsonField kvPair.Value) }
                    | "weaponFired" -> { value with Event = Highbar.EngineEvent.EventCase.WeaponFired (WeaponFired.ReadJsonField kvPair.Value) }
                    | "playerCommand" -> { value with Event = Highbar.EngineEvent.EventCase.PlayerCommand (PlayerCommand.ReadJsonField kvPair.Value) }
                    | "seismicPing" -> { value with Event = Highbar.EngineEvent.EventCase.SeismicPing (SeismicPing.ReadJsonField kvPair.Value) }
                    | "commandFinished" -> { value with Event = Highbar.EngineEvent.EventCase.CommandFinished (CommandFinished.ReadJsonField kvPair.Value) }
                    | "load" -> { value with Event = Highbar.EngineEvent.EventCase.Load (Load.ReadJsonField kvPair.Value) }
                    | "save" -> { value with Event = Highbar.EngineEvent.EventCase.Save (Save.ReadJsonField kvPair.Value) }
                    | "enemyCreated" -> { value with Event = Highbar.EngineEvent.EventCase.EnemyCreated (EnemyCreated.ReadJsonField kvPair.Value) }
                    | "enemyFinished" -> { value with Event = Highbar.EngineEvent.EventCase.EnemyFinished (EnemyFinished.ReadJsonField kvPair.Value) }
                    | "luaMessage" -> { value with Event = Highbar.EngineEvent.EventCase.LuaMessage (LuaMessage.ReadJsonField kvPair.Value) }
                    | "event" -> { value with Event = Event.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _EngineEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._EngineEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module InitEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable TeamId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.TeamId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.InitEvent = {
            TeamId = x.TeamId
            }

type private _InitEvent = InitEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type InitEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("teamId")>] TeamId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<InitEvent>> =
        lazy
        // Field Definitions
        let TeamId = FieldCodec.Primitive ValueCodec.Int32 (1, "teamId")
        // Proto Definition Implementation
        { // ProtoDef<InitEvent>
            Name = "InitEvent"
            Empty = {
                TeamId = TeamId.GetDefault()
                }
            Size = fun (m: InitEvent) ->
                0
                + TeamId.CalcFieldSize m.TeamId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: InitEvent) ->
                TeamId.WriteField w m.TeamId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.InitEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeTeamId = TeamId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: InitEvent) =
                    writeTeamId w m.TeamId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : InitEvent =
                    match kvPair.Key with
                    | "teamId" -> { value with TeamId = TeamId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _InitEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._InitEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ReleaseEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = ReleaseEvent.empty

[<StructuralEquality;StructuralComparison>]
type ReleaseEvent = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<ReleaseEvent>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<ReleaseEvent>
            Name = "ReleaseEvent"
            Empty = ReleaseEvent.empty
            Size = fun (m: ReleaseEvent) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ReleaseEvent) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                ReleaseEvent.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> ReleaseEvent.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UpdateEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Frame: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Frame <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.UpdateEvent = {
            Frame = x.Frame
            }

type private _UpdateEvent = UpdateEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type UpdateEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("frame")>] Frame: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<UpdateEvent>> =
        lazy
        // Field Definitions
        let Frame = FieldCodec.Primitive ValueCodec.Int32 (1, "frame")
        // Proto Definition Implementation
        { // ProtoDef<UpdateEvent>
            Name = "UpdateEvent"
            Empty = {
                Frame = Frame.GetDefault()
                }
            Size = fun (m: UpdateEvent) ->
                0
                + Frame.CalcFieldSize m.Frame
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: UpdateEvent) ->
                Frame.WriteField w m.Frame
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.UpdateEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFrame = Frame.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: UpdateEvent) =
                    writeFrame w m.Frame
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : UpdateEvent =
                    match kvPair.Key with
                    | "frame" -> { value with Frame = Frame.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _UpdateEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._UpdateEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MessageEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Player: int // (1)
            val mutable Message: string // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Player <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.Message <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.MessageEvent = {
            Player = x.Player
            Message = x.Message |> orEmptyString
            }

type private _MessageEvent = MessageEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type MessageEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("player")>] Player: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("message")>] Message: string // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<MessageEvent>> =
        lazy
        // Field Definitions
        let Player = FieldCodec.Primitive ValueCodec.Int32 (1, "player")
        let Message = FieldCodec.Primitive ValueCodec.String (2, "message")
        // Proto Definition Implementation
        { // ProtoDef<MessageEvent>
            Name = "MessageEvent"
            Empty = {
                Player = Player.GetDefault()
                Message = Message.GetDefault()
                }
            Size = fun (m: MessageEvent) ->
                0
                + Player.CalcFieldSize m.Player
                + Message.CalcFieldSize m.Message
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: MessageEvent) ->
                Player.WriteField w m.Player
                Message.WriteField w m.Message
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.MessageEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writePlayer = Player.WriteJsonField o
                let writeMessage = Message.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: MessageEvent) =
                    writePlayer w m.Player
                    writeMessage w m.Message
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : MessageEvent =
                    match kvPair.Key with
                    | "player" -> { value with Player = Player.ReadJsonField kvPair.Value }
                    | "message" -> { value with Message = Message.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _MessageEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._MessageEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UnitCreatedEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable BuilderId: int // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.BuilderId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.UnitCreatedEvent = {
            UnitId = x.UnitId
            BuilderId = x.BuilderId
            }

type private _UnitCreatedEvent = UnitCreatedEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type UnitCreatedEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("builderId")>] BuilderId: int // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<UnitCreatedEvent>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let BuilderId = FieldCodec.Primitive ValueCodec.Int32 (2, "builderId")
        // Proto Definition Implementation
        { // ProtoDef<UnitCreatedEvent>
            Name = "UnitCreatedEvent"
            Empty = {
                UnitId = UnitId.GetDefault()
                BuilderId = BuilderId.GetDefault()
                }
            Size = fun (m: UnitCreatedEvent) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + BuilderId.CalcFieldSize m.BuilderId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: UnitCreatedEvent) ->
                UnitId.WriteField w m.UnitId
                BuilderId.WriteField w m.BuilderId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.UnitCreatedEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeBuilderId = BuilderId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: UnitCreatedEvent) =
                    writeUnitId w m.UnitId
                    writeBuilderId w m.BuilderId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : UnitCreatedEvent =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "builderId" -> { value with BuilderId = BuilderId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _UnitCreatedEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._UnitCreatedEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UnitFinishedEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.UnitFinishedEvent = {
            UnitId = x.UnitId
            }

type private _UnitFinishedEvent = UnitFinishedEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type UnitFinishedEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<UnitFinishedEvent>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        // Proto Definition Implementation
        { // ProtoDef<UnitFinishedEvent>
            Name = "UnitFinishedEvent"
            Empty = {
                UnitId = UnitId.GetDefault()
                }
            Size = fun (m: UnitFinishedEvent) ->
                0
                + UnitId.CalcFieldSize m.UnitId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: UnitFinishedEvent) ->
                UnitId.WriteField w m.UnitId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.UnitFinishedEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: UnitFinishedEvent) =
                    writeUnitId w m.UnitId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : UnitFinishedEvent =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _UnitFinishedEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._UnitFinishedEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UnitIdleEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.UnitIdleEvent = {
            UnitId = x.UnitId
            }

type private _UnitIdleEvent = UnitIdleEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type UnitIdleEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<UnitIdleEvent>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        // Proto Definition Implementation
        { // ProtoDef<UnitIdleEvent>
            Name = "UnitIdleEvent"
            Empty = {
                UnitId = UnitId.GetDefault()
                }
            Size = fun (m: UnitIdleEvent) ->
                0
                + UnitId.CalcFieldSize m.UnitId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: UnitIdleEvent) ->
                UnitId.WriteField w m.UnitId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.UnitIdleEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: UnitIdleEvent) =
                    writeUnitId w m.UnitId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : UnitIdleEvent =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _UnitIdleEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._UnitIdleEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UnitMoveFailedEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.UnitMoveFailedEvent = {
            UnitId = x.UnitId
            }

type private _UnitMoveFailedEvent = UnitMoveFailedEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type UnitMoveFailedEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<UnitMoveFailedEvent>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        // Proto Definition Implementation
        { // ProtoDef<UnitMoveFailedEvent>
            Name = "UnitMoveFailedEvent"
            Empty = {
                UnitId = UnitId.GetDefault()
                }
            Size = fun (m: UnitMoveFailedEvent) ->
                0
                + UnitId.CalcFieldSize m.UnitId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: UnitMoveFailedEvent) ->
                UnitId.WriteField w m.UnitId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.UnitMoveFailedEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: UnitMoveFailedEvent) =
                    writeUnitId w m.UnitId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : UnitMoveFailedEvent =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _UnitMoveFailedEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._UnitMoveFailedEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UnitDamagedEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable AttackerId: OptionBuilder<int> // (2)
            val mutable Damage: float32 // (3)
            val mutable Direction: OptionBuilder<Highbar.Vector3> // (4)
            val mutable WeaponDefId: int // (5)
            val mutable IsParalyzer: bool // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.AttackerId.Set (ValueCodec.Int32.ReadValue reader)
            | 3 -> x.Damage <- ValueCodec.Float.ReadValue reader
            | 4 -> x.Direction.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 5 -> x.WeaponDefId <- ValueCodec.Int32.ReadValue reader
            | 6 -> x.IsParalyzer <- ValueCodec.Bool.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.UnitDamagedEvent = {
            UnitId = x.UnitId
            AttackerId = x.AttackerId.Build
            Damage = x.Damage
            Direction = x.Direction.Build
            WeaponDefId = x.WeaponDefId
            IsParalyzer = x.IsParalyzer
            }

type private _UnitDamagedEvent = UnitDamagedEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type UnitDamagedEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("attackerId")>] AttackerId: int option // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("damage")>] Damage: float32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("direction")>] Direction: Highbar.Vector3 option // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("weaponDefId")>] WeaponDefId: int // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("isParalyzer")>] IsParalyzer: bool // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<UnitDamagedEvent>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let AttackerId = FieldCodec.Optional ValueCodec.Int32 (2, "attackerId")
        let Damage = FieldCodec.Primitive ValueCodec.Float (3, "damage")
        let Direction = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (4, "direction")
        let WeaponDefId = FieldCodec.Primitive ValueCodec.Int32 (5, "weaponDefId")
        let IsParalyzer = FieldCodec.Primitive ValueCodec.Bool (6, "isParalyzer")
        // Proto Definition Implementation
        { // ProtoDef<UnitDamagedEvent>
            Name = "UnitDamagedEvent"
            Empty = {
                UnitId = UnitId.GetDefault()
                AttackerId = AttackerId.GetDefault()
                Damage = Damage.GetDefault()
                Direction = Direction.GetDefault()
                WeaponDefId = WeaponDefId.GetDefault()
                IsParalyzer = IsParalyzer.GetDefault()
                }
            Size = fun (m: UnitDamagedEvent) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + AttackerId.CalcFieldSize m.AttackerId
                + Damage.CalcFieldSize m.Damage
                + Direction.CalcFieldSize m.Direction
                + WeaponDefId.CalcFieldSize m.WeaponDefId
                + IsParalyzer.CalcFieldSize m.IsParalyzer
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: UnitDamagedEvent) ->
                UnitId.WriteField w m.UnitId
                AttackerId.WriteField w m.AttackerId
                Damage.WriteField w m.Damage
                Direction.WriteField w m.Direction
                WeaponDefId.WriteField w m.WeaponDefId
                IsParalyzer.WriteField w m.IsParalyzer
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.UnitDamagedEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeAttackerId = AttackerId.WriteJsonField o
                let writeDamage = Damage.WriteJsonField o
                let writeDirection = Direction.WriteJsonField o
                let writeWeaponDefId = WeaponDefId.WriteJsonField o
                let writeIsParalyzer = IsParalyzer.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: UnitDamagedEvent) =
                    writeUnitId w m.UnitId
                    writeAttackerId w m.AttackerId
                    writeDamage w m.Damage
                    writeDirection w m.Direction
                    writeWeaponDefId w m.WeaponDefId
                    writeIsParalyzer w m.IsParalyzer
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : UnitDamagedEvent =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "attackerId" -> { value with AttackerId = AttackerId.ReadJsonField kvPair.Value }
                    | "damage" -> { value with Damage = Damage.ReadJsonField kvPair.Value }
                    | "direction" -> { value with Direction = Direction.ReadJsonField kvPair.Value }
                    | "weaponDefId" -> { value with WeaponDefId = WeaponDefId.ReadJsonField kvPair.Value }
                    | "isParalyzer" -> { value with IsParalyzer = IsParalyzer.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _UnitDamagedEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._UnitDamagedEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UnitDestroyedEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable AttackerId: OptionBuilder<int> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.AttackerId.Set (ValueCodec.Int32.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.UnitDestroyedEvent = {
            UnitId = x.UnitId
            AttackerId = x.AttackerId.Build
            }

type private _UnitDestroyedEvent = UnitDestroyedEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type UnitDestroyedEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("attackerId")>] AttackerId: int option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<UnitDestroyedEvent>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let AttackerId = FieldCodec.Optional ValueCodec.Int32 (2, "attackerId")
        // Proto Definition Implementation
        { // ProtoDef<UnitDestroyedEvent>
            Name = "UnitDestroyedEvent"
            Empty = {
                UnitId = UnitId.GetDefault()
                AttackerId = AttackerId.GetDefault()
                }
            Size = fun (m: UnitDestroyedEvent) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + AttackerId.CalcFieldSize m.AttackerId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: UnitDestroyedEvent) ->
                UnitId.WriteField w m.UnitId
                AttackerId.WriteField w m.AttackerId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.UnitDestroyedEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeAttackerId = AttackerId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: UnitDestroyedEvent) =
                    writeUnitId w m.UnitId
                    writeAttackerId w m.AttackerId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : UnitDestroyedEvent =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "attackerId" -> { value with AttackerId = AttackerId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _UnitDestroyedEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._UnitDestroyedEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UnitGivenEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable OldTeamId: int // (2)
            val mutable NewTeamId: int // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.OldTeamId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.NewTeamId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.UnitGivenEvent = {
            UnitId = x.UnitId
            OldTeamId = x.OldTeamId
            NewTeamId = x.NewTeamId
            }

type private _UnitGivenEvent = UnitGivenEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type UnitGivenEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("oldTeamId")>] OldTeamId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("newTeamId")>] NewTeamId: int // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<UnitGivenEvent>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let OldTeamId = FieldCodec.Primitive ValueCodec.Int32 (2, "oldTeamId")
        let NewTeamId = FieldCodec.Primitive ValueCodec.Int32 (3, "newTeamId")
        // Proto Definition Implementation
        { // ProtoDef<UnitGivenEvent>
            Name = "UnitGivenEvent"
            Empty = {
                UnitId = UnitId.GetDefault()
                OldTeamId = OldTeamId.GetDefault()
                NewTeamId = NewTeamId.GetDefault()
                }
            Size = fun (m: UnitGivenEvent) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + OldTeamId.CalcFieldSize m.OldTeamId
                + NewTeamId.CalcFieldSize m.NewTeamId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: UnitGivenEvent) ->
                UnitId.WriteField w m.UnitId
                OldTeamId.WriteField w m.OldTeamId
                NewTeamId.WriteField w m.NewTeamId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.UnitGivenEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeOldTeamId = OldTeamId.WriteJsonField o
                let writeNewTeamId = NewTeamId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: UnitGivenEvent) =
                    writeUnitId w m.UnitId
                    writeOldTeamId w m.OldTeamId
                    writeNewTeamId w m.NewTeamId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : UnitGivenEvent =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "oldTeamId" -> { value with OldTeamId = OldTeamId.ReadJsonField kvPair.Value }
                    | "newTeamId" -> { value with NewTeamId = NewTeamId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _UnitGivenEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._UnitGivenEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UnitCapturedEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable OldTeamId: int // (2)
            val mutable NewTeamId: int // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.OldTeamId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.NewTeamId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.UnitCapturedEvent = {
            UnitId = x.UnitId
            OldTeamId = x.OldTeamId
            NewTeamId = x.NewTeamId
            }

type private _UnitCapturedEvent = UnitCapturedEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type UnitCapturedEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("oldTeamId")>] OldTeamId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("newTeamId")>] NewTeamId: int // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<UnitCapturedEvent>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let OldTeamId = FieldCodec.Primitive ValueCodec.Int32 (2, "oldTeamId")
        let NewTeamId = FieldCodec.Primitive ValueCodec.Int32 (3, "newTeamId")
        // Proto Definition Implementation
        { // ProtoDef<UnitCapturedEvent>
            Name = "UnitCapturedEvent"
            Empty = {
                UnitId = UnitId.GetDefault()
                OldTeamId = OldTeamId.GetDefault()
                NewTeamId = NewTeamId.GetDefault()
                }
            Size = fun (m: UnitCapturedEvent) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + OldTeamId.CalcFieldSize m.OldTeamId
                + NewTeamId.CalcFieldSize m.NewTeamId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: UnitCapturedEvent) ->
                UnitId.WriteField w m.UnitId
                OldTeamId.WriteField w m.OldTeamId
                NewTeamId.WriteField w m.NewTeamId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.UnitCapturedEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeOldTeamId = OldTeamId.WriteJsonField o
                let writeNewTeamId = NewTeamId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: UnitCapturedEvent) =
                    writeUnitId w m.UnitId
                    writeOldTeamId w m.OldTeamId
                    writeNewTeamId w m.NewTeamId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : UnitCapturedEvent =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "oldTeamId" -> { value with OldTeamId = OldTeamId.ReadJsonField kvPair.Value }
                    | "newTeamId" -> { value with NewTeamId = NewTeamId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _UnitCapturedEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._UnitCapturedEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module EnemyEnterLOSEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable EnemyId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.EnemyId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.EnemyEnterLOSEvent = {
            EnemyId = x.EnemyId
            }

type private _EnemyEnterLOSEvent = EnemyEnterLOSEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type EnemyEnterLOSEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("enemyId")>] EnemyId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<EnemyEnterLOSEvent>> =
        lazy
        // Field Definitions
        let EnemyId = FieldCodec.Primitive ValueCodec.Int32 (1, "enemyId")
        // Proto Definition Implementation
        { // ProtoDef<EnemyEnterLOSEvent>
            Name = "EnemyEnterLOSEvent"
            Empty = {
                EnemyId = EnemyId.GetDefault()
                }
            Size = fun (m: EnemyEnterLOSEvent) ->
                0
                + EnemyId.CalcFieldSize m.EnemyId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: EnemyEnterLOSEvent) ->
                EnemyId.WriteField w m.EnemyId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.EnemyEnterLOSEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeEnemyId = EnemyId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: EnemyEnterLOSEvent) =
                    writeEnemyId w m.EnemyId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : EnemyEnterLOSEvent =
                    match kvPair.Key with
                    | "enemyId" -> { value with EnemyId = EnemyId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _EnemyEnterLOSEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._EnemyEnterLOSEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module EnemyLeaveLOSEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable EnemyId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.EnemyId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.EnemyLeaveLOSEvent = {
            EnemyId = x.EnemyId
            }

type private _EnemyLeaveLOSEvent = EnemyLeaveLOSEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type EnemyLeaveLOSEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("enemyId")>] EnemyId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<EnemyLeaveLOSEvent>> =
        lazy
        // Field Definitions
        let EnemyId = FieldCodec.Primitive ValueCodec.Int32 (1, "enemyId")
        // Proto Definition Implementation
        { // ProtoDef<EnemyLeaveLOSEvent>
            Name = "EnemyLeaveLOSEvent"
            Empty = {
                EnemyId = EnemyId.GetDefault()
                }
            Size = fun (m: EnemyLeaveLOSEvent) ->
                0
                + EnemyId.CalcFieldSize m.EnemyId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: EnemyLeaveLOSEvent) ->
                EnemyId.WriteField w m.EnemyId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.EnemyLeaveLOSEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeEnemyId = EnemyId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: EnemyLeaveLOSEvent) =
                    writeEnemyId w m.EnemyId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : EnemyLeaveLOSEvent =
                    match kvPair.Key with
                    | "enemyId" -> { value with EnemyId = EnemyId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _EnemyLeaveLOSEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._EnemyLeaveLOSEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module EnemyEnterRadarEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable EnemyId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.EnemyId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.EnemyEnterRadarEvent = {
            EnemyId = x.EnemyId
            }

type private _EnemyEnterRadarEvent = EnemyEnterRadarEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type EnemyEnterRadarEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("enemyId")>] EnemyId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<EnemyEnterRadarEvent>> =
        lazy
        // Field Definitions
        let EnemyId = FieldCodec.Primitive ValueCodec.Int32 (1, "enemyId")
        // Proto Definition Implementation
        { // ProtoDef<EnemyEnterRadarEvent>
            Name = "EnemyEnterRadarEvent"
            Empty = {
                EnemyId = EnemyId.GetDefault()
                }
            Size = fun (m: EnemyEnterRadarEvent) ->
                0
                + EnemyId.CalcFieldSize m.EnemyId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: EnemyEnterRadarEvent) ->
                EnemyId.WriteField w m.EnemyId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.EnemyEnterRadarEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeEnemyId = EnemyId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: EnemyEnterRadarEvent) =
                    writeEnemyId w m.EnemyId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : EnemyEnterRadarEvent =
                    match kvPair.Key with
                    | "enemyId" -> { value with EnemyId = EnemyId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _EnemyEnterRadarEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._EnemyEnterRadarEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module EnemyLeaveRadarEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable EnemyId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.EnemyId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.EnemyLeaveRadarEvent = {
            EnemyId = x.EnemyId
            }

type private _EnemyLeaveRadarEvent = EnemyLeaveRadarEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type EnemyLeaveRadarEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("enemyId")>] EnemyId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<EnemyLeaveRadarEvent>> =
        lazy
        // Field Definitions
        let EnemyId = FieldCodec.Primitive ValueCodec.Int32 (1, "enemyId")
        // Proto Definition Implementation
        { // ProtoDef<EnemyLeaveRadarEvent>
            Name = "EnemyLeaveRadarEvent"
            Empty = {
                EnemyId = EnemyId.GetDefault()
                }
            Size = fun (m: EnemyLeaveRadarEvent) ->
                0
                + EnemyId.CalcFieldSize m.EnemyId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: EnemyLeaveRadarEvent) ->
                EnemyId.WriteField w m.EnemyId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.EnemyLeaveRadarEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeEnemyId = EnemyId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: EnemyLeaveRadarEvent) =
                    writeEnemyId w m.EnemyId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : EnemyLeaveRadarEvent =
                    match kvPair.Key with
                    | "enemyId" -> { value with EnemyId = EnemyId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _EnemyLeaveRadarEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._EnemyLeaveRadarEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module EnemyDamagedEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable EnemyId: int // (1)
            val mutable AttackerId: OptionBuilder<int> // (2)
            val mutable Damage: float32 // (3)
            val mutable Direction: OptionBuilder<Highbar.Vector3> // (4)
            val mutable WeaponDefId: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.EnemyId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.AttackerId.Set (ValueCodec.Int32.ReadValue reader)
            | 3 -> x.Damage <- ValueCodec.Float.ReadValue reader
            | 4 -> x.Direction.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 5 -> x.WeaponDefId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.EnemyDamagedEvent = {
            EnemyId = x.EnemyId
            AttackerId = x.AttackerId.Build
            Damage = x.Damage
            Direction = x.Direction.Build
            WeaponDefId = x.WeaponDefId
            }

type private _EnemyDamagedEvent = EnemyDamagedEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type EnemyDamagedEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("enemyId")>] EnemyId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("attackerId")>] AttackerId: int option // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("damage")>] Damage: float32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("direction")>] Direction: Highbar.Vector3 option // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("weaponDefId")>] WeaponDefId: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<EnemyDamagedEvent>> =
        lazy
        // Field Definitions
        let EnemyId = FieldCodec.Primitive ValueCodec.Int32 (1, "enemyId")
        let AttackerId = FieldCodec.Optional ValueCodec.Int32 (2, "attackerId")
        let Damage = FieldCodec.Primitive ValueCodec.Float (3, "damage")
        let Direction = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (4, "direction")
        let WeaponDefId = FieldCodec.Primitive ValueCodec.Int32 (5, "weaponDefId")
        // Proto Definition Implementation
        { // ProtoDef<EnemyDamagedEvent>
            Name = "EnemyDamagedEvent"
            Empty = {
                EnemyId = EnemyId.GetDefault()
                AttackerId = AttackerId.GetDefault()
                Damage = Damage.GetDefault()
                Direction = Direction.GetDefault()
                WeaponDefId = WeaponDefId.GetDefault()
                }
            Size = fun (m: EnemyDamagedEvent) ->
                0
                + EnemyId.CalcFieldSize m.EnemyId
                + AttackerId.CalcFieldSize m.AttackerId
                + Damage.CalcFieldSize m.Damage
                + Direction.CalcFieldSize m.Direction
                + WeaponDefId.CalcFieldSize m.WeaponDefId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: EnemyDamagedEvent) ->
                EnemyId.WriteField w m.EnemyId
                AttackerId.WriteField w m.AttackerId
                Damage.WriteField w m.Damage
                Direction.WriteField w m.Direction
                WeaponDefId.WriteField w m.WeaponDefId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.EnemyDamagedEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeEnemyId = EnemyId.WriteJsonField o
                let writeAttackerId = AttackerId.WriteJsonField o
                let writeDamage = Damage.WriteJsonField o
                let writeDirection = Direction.WriteJsonField o
                let writeWeaponDefId = WeaponDefId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: EnemyDamagedEvent) =
                    writeEnemyId w m.EnemyId
                    writeAttackerId w m.AttackerId
                    writeDamage w m.Damage
                    writeDirection w m.Direction
                    writeWeaponDefId w m.WeaponDefId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : EnemyDamagedEvent =
                    match kvPair.Key with
                    | "enemyId" -> { value with EnemyId = EnemyId.ReadJsonField kvPair.Value }
                    | "attackerId" -> { value with AttackerId = AttackerId.ReadJsonField kvPair.Value }
                    | "damage" -> { value with Damage = Damage.ReadJsonField kvPair.Value }
                    | "direction" -> { value with Direction = Direction.ReadJsonField kvPair.Value }
                    | "weaponDefId" -> { value with WeaponDefId = WeaponDefId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _EnemyDamagedEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._EnemyDamagedEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module EnemyDestroyedEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable EnemyId: int // (1)
            val mutable AttackerId: OptionBuilder<int> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.EnemyId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.AttackerId.Set (ValueCodec.Int32.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.EnemyDestroyedEvent = {
            EnemyId = x.EnemyId
            AttackerId = x.AttackerId.Build
            }

type private _EnemyDestroyedEvent = EnemyDestroyedEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type EnemyDestroyedEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("enemyId")>] EnemyId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("attackerId")>] AttackerId: int option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<EnemyDestroyedEvent>> =
        lazy
        // Field Definitions
        let EnemyId = FieldCodec.Primitive ValueCodec.Int32 (1, "enemyId")
        let AttackerId = FieldCodec.Optional ValueCodec.Int32 (2, "attackerId")
        // Proto Definition Implementation
        { // ProtoDef<EnemyDestroyedEvent>
            Name = "EnemyDestroyedEvent"
            Empty = {
                EnemyId = EnemyId.GetDefault()
                AttackerId = AttackerId.GetDefault()
                }
            Size = fun (m: EnemyDestroyedEvent) ->
                0
                + EnemyId.CalcFieldSize m.EnemyId
                + AttackerId.CalcFieldSize m.AttackerId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: EnemyDestroyedEvent) ->
                EnemyId.WriteField w m.EnemyId
                AttackerId.WriteField w m.AttackerId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.EnemyDestroyedEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeEnemyId = EnemyId.WriteJsonField o
                let writeAttackerId = AttackerId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: EnemyDestroyedEvent) =
                    writeEnemyId w m.EnemyId
                    writeAttackerId w m.AttackerId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : EnemyDestroyedEvent =
                    match kvPair.Key with
                    | "enemyId" -> { value with EnemyId = EnemyId.ReadJsonField kvPair.Value }
                    | "attackerId" -> { value with AttackerId = AttackerId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _EnemyDestroyedEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._EnemyDestroyedEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module WeaponFiredEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable WeaponDefId: int // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.WeaponDefId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.WeaponFiredEvent = {
            UnitId = x.UnitId
            WeaponDefId = x.WeaponDefId
            }

type private _WeaponFiredEvent = WeaponFiredEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type WeaponFiredEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("weaponDefId")>] WeaponDefId: int // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<WeaponFiredEvent>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let WeaponDefId = FieldCodec.Primitive ValueCodec.Int32 (2, "weaponDefId")
        // Proto Definition Implementation
        { // ProtoDef<WeaponFiredEvent>
            Name = "WeaponFiredEvent"
            Empty = {
                UnitId = UnitId.GetDefault()
                WeaponDefId = WeaponDefId.GetDefault()
                }
            Size = fun (m: WeaponFiredEvent) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + WeaponDefId.CalcFieldSize m.WeaponDefId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: WeaponFiredEvent) ->
                UnitId.WriteField w m.UnitId
                WeaponDefId.WriteField w m.WeaponDefId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.WeaponFiredEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeWeaponDefId = WeaponDefId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: WeaponFiredEvent) =
                    writeUnitId w m.UnitId
                    writeWeaponDefId w m.WeaponDefId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : WeaponFiredEvent =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "weaponDefId" -> { value with WeaponDefId = WeaponDefId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _WeaponFiredEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._WeaponFiredEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PlayerCommandEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Units: RepeatedBuilder<int> // (1)
            val mutable CommandTopicId: int // (2)
            val mutable CommandId: int // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Units.AddRange ((ValueCodec.Packed ValueCodec.Int32).ReadValue reader)
            | 2 -> x.CommandTopicId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.CommandId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.PlayerCommandEvent = {
            Units = x.Units.Build
            CommandTopicId = x.CommandTopicId
            CommandId = x.CommandId
            }

type private _PlayerCommandEvent = PlayerCommandEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type PlayerCommandEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("units")>] Units: int list // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("commandTopicId")>] CommandTopicId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("commandId")>] CommandId: int // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<PlayerCommandEvent>> =
        lazy
        // Field Definitions
        let Units = FieldCodec.Primitive (ValueCodec.Packed ValueCodec.Int32) (1, "units")
        let CommandTopicId = FieldCodec.Primitive ValueCodec.Int32 (2, "commandTopicId")
        let CommandId = FieldCodec.Primitive ValueCodec.Int32 (3, "commandId")
        // Proto Definition Implementation
        { // ProtoDef<PlayerCommandEvent>
            Name = "PlayerCommandEvent"
            Empty = {
                Units = Units.GetDefault()
                CommandTopicId = CommandTopicId.GetDefault()
                CommandId = CommandId.GetDefault()
                }
            Size = fun (m: PlayerCommandEvent) ->
                0
                + Units.CalcFieldSize m.Units
                + CommandTopicId.CalcFieldSize m.CommandTopicId
                + CommandId.CalcFieldSize m.CommandId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: PlayerCommandEvent) ->
                Units.WriteField w m.Units
                CommandTopicId.WriteField w m.CommandTopicId
                CommandId.WriteField w m.CommandId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.PlayerCommandEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnits = Units.WriteJsonField o
                let writeCommandTopicId = CommandTopicId.WriteJsonField o
                let writeCommandId = CommandId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: PlayerCommandEvent) =
                    writeUnits w m.Units
                    writeCommandTopicId w m.CommandTopicId
                    writeCommandId w m.CommandId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : PlayerCommandEvent =
                    match kvPair.Key with
                    | "units" -> { value with Units = Units.ReadJsonField kvPair.Value }
                    | "commandTopicId" -> { value with CommandTopicId = CommandTopicId.ReadJsonField kvPair.Value }
                    | "commandId" -> { value with CommandId = CommandId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _PlayerCommandEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._PlayerCommandEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SeismicPingEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Position: OptionBuilder<Highbar.Vector3> // (1)
            val mutable Strength: float32 // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 2 -> x.Strength <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SeismicPingEvent = {
            Position = x.Position.Build
            Strength = x.Strength
            }

type private _SeismicPingEvent = SeismicPingEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SeismicPingEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("strength")>] Strength: float32 // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<SeismicPingEvent>> =
        lazy
        // Field Definitions
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (1, "position")
        let Strength = FieldCodec.Primitive ValueCodec.Float (2, "strength")
        // Proto Definition Implementation
        { // ProtoDef<SeismicPingEvent>
            Name = "SeismicPingEvent"
            Empty = {
                Position = Position.GetDefault()
                Strength = Strength.GetDefault()
                }
            Size = fun (m: SeismicPingEvent) ->
                0
                + Position.CalcFieldSize m.Position
                + Strength.CalcFieldSize m.Strength
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SeismicPingEvent) ->
                Position.WriteField w m.Position
                Strength.WriteField w m.Strength
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SeismicPingEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writePosition = Position.WriteJsonField o
                let writeStrength = Strength.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SeismicPingEvent) =
                    writePosition w m.Position
                    writeStrength w m.Strength
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SeismicPingEvent =
                    match kvPair.Key with
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | "strength" -> { value with Strength = Strength.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SeismicPingEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SeismicPingEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CommandFinishedEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable CommandId: int // (2)
            val mutable CommandTopicId: int // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.CommandId <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.CommandTopicId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.CommandFinishedEvent = {
            UnitId = x.UnitId
            CommandId = x.CommandId
            CommandTopicId = x.CommandTopicId
            }

type private _CommandFinishedEvent = CommandFinishedEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CommandFinishedEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("commandId")>] CommandId: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("commandTopicId")>] CommandTopicId: int // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<CommandFinishedEvent>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let CommandId = FieldCodec.Primitive ValueCodec.Int32 (2, "commandId")
        let CommandTopicId = FieldCodec.Primitive ValueCodec.Int32 (3, "commandTopicId")
        // Proto Definition Implementation
        { // ProtoDef<CommandFinishedEvent>
            Name = "CommandFinishedEvent"
            Empty = {
                UnitId = UnitId.GetDefault()
                CommandId = CommandId.GetDefault()
                CommandTopicId = CommandTopicId.GetDefault()
                }
            Size = fun (m: CommandFinishedEvent) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + CommandId.CalcFieldSize m.CommandId
                + CommandTopicId.CalcFieldSize m.CommandTopicId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CommandFinishedEvent) ->
                UnitId.WriteField w m.UnitId
                CommandId.WriteField w m.CommandId
                CommandTopicId.WriteField w m.CommandTopicId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.CommandFinishedEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writeCommandId = CommandId.WriteJsonField o
                let writeCommandTopicId = CommandTopicId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CommandFinishedEvent) =
                    writeUnitId w m.UnitId
                    writeCommandId w m.CommandId
                    writeCommandTopicId w m.CommandTopicId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CommandFinishedEvent =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "commandId" -> { value with CommandId = CommandId.ReadJsonField kvPair.Value }
                    | "commandTopicId" -> { value with CommandTopicId = CommandTopicId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CommandFinishedEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CommandFinishedEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LoadEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = LoadEvent.empty

[<StructuralEquality;StructuralComparison>]
type LoadEvent = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<LoadEvent>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<LoadEvent>
            Name = "LoadEvent"
            Empty = LoadEvent.empty
            Size = fun (m: LoadEvent) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LoadEvent) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                LoadEvent.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> LoadEvent.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SaveEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = SaveEvent.empty

[<StructuralEquality;StructuralComparison>]
type SaveEvent = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<SaveEvent>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<SaveEvent>
            Name = "SaveEvent"
            Empty = SaveEvent.empty
            Size = fun (m: SaveEvent) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SaveEvent) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                SaveEvent.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> SaveEvent.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module EnemyCreatedEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable EnemyId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.EnemyId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.EnemyCreatedEvent = {
            EnemyId = x.EnemyId
            }

type private _EnemyCreatedEvent = EnemyCreatedEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type EnemyCreatedEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("enemyId")>] EnemyId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<EnemyCreatedEvent>> =
        lazy
        // Field Definitions
        let EnemyId = FieldCodec.Primitive ValueCodec.Int32 (1, "enemyId")
        // Proto Definition Implementation
        { // ProtoDef<EnemyCreatedEvent>
            Name = "EnemyCreatedEvent"
            Empty = {
                EnemyId = EnemyId.GetDefault()
                }
            Size = fun (m: EnemyCreatedEvent) ->
                0
                + EnemyId.CalcFieldSize m.EnemyId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: EnemyCreatedEvent) ->
                EnemyId.WriteField w m.EnemyId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.EnemyCreatedEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeEnemyId = EnemyId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: EnemyCreatedEvent) =
                    writeEnemyId w m.EnemyId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : EnemyCreatedEvent =
                    match kvPair.Key with
                    | "enemyId" -> { value with EnemyId = EnemyId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _EnemyCreatedEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._EnemyCreatedEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module EnemyFinishedEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable EnemyId: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.EnemyId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.EnemyFinishedEvent = {
            EnemyId = x.EnemyId
            }

type private _EnemyFinishedEvent = EnemyFinishedEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type EnemyFinishedEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("enemyId")>] EnemyId: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<EnemyFinishedEvent>> =
        lazy
        // Field Definitions
        let EnemyId = FieldCodec.Primitive ValueCodec.Int32 (1, "enemyId")
        // Proto Definition Implementation
        { // ProtoDef<EnemyFinishedEvent>
            Name = "EnemyFinishedEvent"
            Empty = {
                EnemyId = EnemyId.GetDefault()
                }
            Size = fun (m: EnemyFinishedEvent) ->
                0
                + EnemyId.CalcFieldSize m.EnemyId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: EnemyFinishedEvent) ->
                EnemyId.WriteField w m.EnemyId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.EnemyFinishedEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeEnemyId = EnemyId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: EnemyFinishedEvent) =
                    writeEnemyId w m.EnemyId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : EnemyFinishedEvent =
                    match kvPair.Key with
                    | "enemyId" -> { value with EnemyId = EnemyId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _EnemyFinishedEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._EnemyFinishedEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LuaMessageEvent =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Data: string // (1)
            val mutable InMessageId: int // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Data <- ValueCodec.String.ReadValue reader
            | 2 -> x.InMessageId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.LuaMessageEvent = {
            Data = x.Data |> orEmptyString
            InMessageId = x.InMessageId
            }

type private _LuaMessageEvent = LuaMessageEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LuaMessageEvent = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("data")>] Data: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("inMessageId")>] InMessageId: int // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<LuaMessageEvent>> =
        lazy
        // Field Definitions
        let Data = FieldCodec.Primitive ValueCodec.String (1, "data")
        let InMessageId = FieldCodec.Primitive ValueCodec.Int32 (2, "inMessageId")
        // Proto Definition Implementation
        { // ProtoDef<LuaMessageEvent>
            Name = "LuaMessageEvent"
            Empty = {
                Data = Data.GetDefault()
                InMessageId = InMessageId.GetDefault()
                }
            Size = fun (m: LuaMessageEvent) ->
                0
                + Data.CalcFieldSize m.Data
                + InMessageId.CalcFieldSize m.InMessageId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LuaMessageEvent) ->
                Data.WriteField w m.Data
                InMessageId.WriteField w m.InMessageId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.LuaMessageEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeData = Data.WriteJsonField o
                let writeInMessageId = InMessageId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LuaMessageEvent) =
                    writeData w m.Data
                    writeInMessageId w m.InMessageId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LuaMessageEvent =
                    match kvPair.Key with
                    | "data" -> { value with Data = Data.ReadJsonField kvPair.Value }
                    | "inMessageId" -> { value with InMessageId = InMessageId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LuaMessageEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._LuaMessageEvent.Proto.Value.Empty

