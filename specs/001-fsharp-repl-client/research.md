# Research: F# REPL Client for BAR AI Orchestration

**Branch**: `001-fsharp-repl-client` | **Date**: 2026-04-05

## R1: F# Protobuf Generation from HighBarV2 Proto Schemas

**Decision**: Use FsGrpc (contract-first) to generate native F# records and discriminated unions from the 5 HighBarV2 `.proto` files.

**Rationale**: FsGrpc produces idiomatic F# types — immutable records for messages, discriminated unions for `oneof` fields. This is essential for a REPL-friendly library where pattern matching and immutability are core to the F# developer experience. The alternative (Grpc.Tools) generates mutable C# classes that require a separate C# project, violating the constitution's F#-only mandate.

**Alternatives considered**:
- Grpc.Tools (C# intermediary project): Rejected — requires C# project, mutable classes, poor FSI ergonomics
- protobuf-net code-first: Rejected — requires defining types manually that mirror proto schema, error-prone for 5 files with 100+ message types
- Hand-written serialization: Rejected — massive effort, no wire-format guarantee

**Implementation details**:
- Copy the 5 `.proto` files from `HighBarV2/proto/highbar/` into `FSBarV1/proto/highbar/`
- Create `buf.yaml` and `buf.gen.yaml` for FsGrpc generation
- FsGrpc.Tools hooks into MSBuild — `dotnet build` auto-generates F# bindings
- Generated code goes to `src/FSBar.Proto/Generated/`
- Proto `oneof` (EngineEvent, AICommand, ProxyMessage, AIMessage, CallbackParam, CallbackResult) → F# discriminated unions
- Proto `repeated` → F# `list<T>`
- Proto `optional` → F# `option<T>`

**Proto schema summary** (5 files, source of truth in HighBarV2):
| File | Messages | Key types |
|------|----------|-----------|
| common.proto | 3 | Vector3, UnitRef, CommandOptions |
| messages.proto | 10 + 1 enum | ProxyMessage (6 oneof), AIMessage (4 oneof), Handshake, Frame, Shutdown |
| events.proto | 28 | EngineEvent (27 oneof variants), individual event messages |
| commands.proto | 52 | AICommand (42 oneof variants), individual command messages |
| callbacks.proto | 7 + 1 enum | CallbackRequest, CallbackResponse, CallbackParam (4 oneof), CallbackResult (7 oneof), CallbackId enum (40+ values) |

## R2: BarData NuGet Package Consumption

**Decision**: Consume the BarData library from HighBarV2 as a pre-packed NuGet package from the local NuGet store (`~/.local/share/nuget-local/`).

**Rationale**: The constitution requires libraries to be packable via `dotnet pack`. HighBarV2's BarData project is a pure F# library with no external dependencies — it contains 953 unit definition records. Consuming it as NuGet avoids cross-repo project references and keeps FSBarV1 self-contained.

**Alternatives considered**:
- Project reference to sister repo: Rejected — fragile path dependency, complicates CI
- Copy source files: Rejected — duplication, version drift, maintenance burden
- Reimplement unit data: Rejected — 953 units, massive effort, no value added

**Implementation details**:
- Ensure BarData is packed in HighBarV2: `cd data/bar && dotnet pack -o ~/.local/share/nuget-local/`
- Add `nuget.config` to FSBarV1 with local source: `<add key="local" value="~/.local/share/nuget-local/" />`
- Reference in FSBar.Client.fsproj: `<PackageReference Include="BarData" Version="*" />`

## R3: Unix Domain Socket Communication Pattern

**Decision**: Reimplement the length-prefixed binary framing protocol in pure F# using `System.Net.Sockets.Socket` with `AddressFamily.Unix`.

**Rationale**: The protocol is simple — 4-byte little-endian length prefix followed by protobuf payload. The HighBarV2 HighBar.Client implementation (`Client.fs`) is ~400 lines and well-understood. Reimplementing in F# is straightforward and avoids any C# dependency.

**Alternatives considered**:
- Wrap HighBar.Client: Rejected by clarification — must be pure F# reimplementation
- Use System.IO.Pipes: Rejected — less control over Unix domain socket specifics, HighBarV2 proxy uses raw `AF_UNIX` sockets
- Use third-party socket library: Rejected — unnecessary dependency for a simple protocol

**Implementation details** (reference: `HighBarV2/clients/fsharp/src/Client.fs`):
- Connection flow: Create listening socket → Bind to path → Listen(1) → Accept proxy connection
- Framing: `[4 bytes LE length][protobuf payload]` — same for send and receive
- The proxy (C side) connects to the AI's listening socket (not the other way around)
- Handshake: Receive `ProxyMessage.Handshake`, validate protocol version (1), send `AIMessage.HandshakeResponse`
- Frame loop: Receive `ProxyMessage.Frame` → process events → send `AIMessage.FrameResponse` with commands
- Callbacks: Send `AIMessage.CallbackRequest` during frame processing → receive `ProxyMessage.CallbackResponse`
- Shutdown: Receive `ProxyMessage.Shutdown` → exit frame loop

## R4: Engine Process Management

**Decision**: Launch engine processes using `System.Diagnostics.Process` with generated game-setup scripts, following the same pattern as HighBarV2's test harness.

**Rationale**: HighBarV2's `start-headless.sh` and `PersistentHarness.fs` demonstrate the proven pattern: generate a `script.txt` with socket path substituted, set `HIGHBAR_SOCKET_PATH` env var, launch `spring-headless` (or AppImage) with the script as argument.

**Alternatives considered**:
- Shell out to HighBarV2's start-headless.sh: Rejected — creates dependency on sister repo scripts
- Use CliWrap or similar process library: Rejected — unnecessary dependency for simple process management

**Implementation details**:
- Headless mode: Launch `spring-headless <script.txt>` with `HIGHBAR_SOCKET_PATH` and `SPRING_WRITEDIR` env vars
- Graphical mode: Launch BAR AppImage with the same script, adding windowed mode flags
- PID tracking: Write PID to `<socket_path>.pid` for cleanup
- Shutdown: SIGTERM → wait 5s → SIGKILL (same as HighBarV2's `stop-headless.sh`)
- Engine data dir auto-detection: Check `SPRING_DATADIR` or locate via engine binary path
- Archive cache: Copy `ArchiveCache20.lua` to instance dir for fast startup
- Session dir: `/tmp/fsbar-<guid>/` for engine logs and write dir

## R5: Game Setup Script Generation

**Decision**: Generate game-setup.txt scripts in F# using string templating, matching HighBarV2's format exactly.

**Rationale**: The game-setup.txt format is the Recoil engine's standard start script format. HighBarV2's `game-setup.txt` template is 93 lines with clear substitution points. Generating it in F# provides full control over parameters.

**Implementation details** (reference: `HighBarV2/tests/fixtures/game-setup.txt`):
- Template fields: `__SOCKET_PATH__`, `__MAP_NAME__`, `__GAME_TYPE__`
- Default values: Map = "Red Rock Desert v2", Game = auto-detected from engine, Opponent = NullAI
- AI0 = HighBarV2 proxy with socket_path option, Team 0, Armada
- AI1 = NullAI (default), Team 1, Cortex
- Player0 = Spectator (required by engine)
- MODOPTIONS: GameMode=3, deathmode=neverend, debugcommands=1:cheat|3:globallos, MinSpeed=100, MaxSpeed=100

## R6: REPL Ergonomics (FSI Prelude)

**Decision**: Provide a `scripts/prelude.fsx` that loads the compiled library DLLs and opens the key namespaces, plus numbered example scripts.

**Rationale**: The constitution (Section V) mandates scripting accessibility with a prelude loadable via single `#load` directive. The prelude must reference the packed library assemblies.

**Implementation details**:
- Prelude loads: FSBar.Proto.dll, FSBar.Client.dll, BarData.dll
- Opens: `FSBar.Client`, `FSBar.Client.Commands`, `BarData`
- Example workflow in FSI:
  ```fsharp
  #load "scripts/prelude.fsx"
  let client = BarClient.startHeadless()  // launches engine, connects
  let frame = client.Step()               // receive one frame
  client.Send(Commands.MoveCommand 1 100.0f 0.0f 100.0f)
  client.Stop()                           // clean shutdown
  ```
