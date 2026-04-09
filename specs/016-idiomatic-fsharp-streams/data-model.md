# Data Model: Idiomatic F# Streams Refactor

**Branch**: `016-idiomatic-fsharp-streams` | **Date**: 2026-04-09

## Entities

### SessionState (Discriminated Union — unchanged)

```
SessionState =
  | Idle
  | Starting
  | Connected
  | Running
  | Stopped
  | Error of message: string
```

State machine: `Idle → Starting → Connected ↔ Running → Stopped`; any state can transition to `Error`.

### GameFrame (Record — unchanged)

```
GameFrame = {
  FrameNumber: uint32
  Events: GameEvent list
}
```

Produced by Protocol.receiveFrame. Yielded by the Frames sequence.

### GameEvent (Discriminated Union — unchanged, 28 cases)

Existing DU with cases: Init, Release, Update, Message, UnitCreated, UnitFinished, UnitIdle, UnitMoveFailed, UnitDamaged, UnitDestroyed, UnitGiven, UnitCaptured, EnemyEnterLOS, EnemyLeaveLOS, EnemyEnterRadar, EnemyLeaveRadar, EnemyDamaged, EnemyDestroyed, EnemyCreated, EnemyFinished, WeaponFired, PlayerCommand, SeismicPing, CommandFinished, Load, Save, LuaMessage, Unknown.

No changes needed — already idiomatic.

### AICommand (Record — unchanged)

```
AICommand = {
  Command: CommandCase
}
```

Built by Commands module functions. Submitted via Session.SendCommands.

### HandshakeInfo (Record — unchanged)

```
HandshakeInfo = {
  ProtocolVersion, EngineName, EngineVersion,
  GameName, MapName, ModName, MyTeam, MyAllyTeam, PlayerCount
}
```

### EngineConfig (Record — unchanged)

```
EngineConfig = {
  Mode, SocketPath, MapName, GameType, OpponentAI,
  OpponentSide, OurSide, TimeoutMs, EngineBin, AppImagePath,
  SpringDataDir, GameSpeed, ReadTimeoutMs
}
```

### Session / BarClient (Class — refactored API surface)

**Current public API** (BarClient):
- Properties: State, Config, Handshake, Stream
- Methods: Start, Step, StepWith, Run, RunUntil, Reset, Stop
- IDisposable

**New public API**:
- Properties: State, Config, Handshake, Stream
- Properties (new): Frames (seq<GameFrame>)
- Methods (new): SendCommands (AICommand list -> unit)
- Methods (kept): Start, Stop, Reset
- Methods (removed): Step, StepWith, Run, RunUntil
- IDisposable

### MapGrid, Terrain, MoveType — unchanged

Already idiomatic records and DUs with active patterns. No changes needed.

## Relationships

```
Session --owns--> NetworkStream --used by--> Callbacks module
Session --produces--> seq<GameFrame>
Session --accepts--> AICommand list (via SendCommands)
GameFrame --contains--> GameEvent list
Commands module --builds--> AICommand
Protocol module --frames--> GameFrame from raw socket bytes
```

## Key Behavioral Change

**Before**: Consumer calls `StepWith(handler)` where handler receives `GameFrame` and returns `AICommand list`. Protocol response is sent inside StepWith.

**After**: Consumer iterates `session.Frames`, calls `session.SendCommands(cmds)` between iterations. Protocol response is sent when next frame is requested (or on session stop).

The lock-step invariant is preserved: one response per frame, sent before receiving the next.
