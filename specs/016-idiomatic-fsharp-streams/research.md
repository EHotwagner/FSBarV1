# Research: Idiomatic F# Streams Refactor

**Branch**: `016-idiomatic-fsharp-streams` | **Date**: 2026-04-09

## R1: Stream Design for Lock-Step Protocol

**Decision**: Use `seq<GameFrame>` with a separate `SendCommands` method on the session object. Commands queued via `SendCommands` are sent when the consumer requests the next frame. If no commands are queued, an empty response is sent automatically.

**Rationale**: The engine protocol is strictly lock-step: receive frame → send commands → receive next frame. A true `seq<GameFrame>` works because:
1. The consumer pulls the next frame by iterating
2. Before yielding the next frame, the session sends any pending commands (or empty list)
3. This preserves the lock-step invariant while exposing a clean F# sequence

The alternative `IAsyncEnumerable<GameFrame>` was considered but rejected: the underlying socket I/O is blocking, there's a single consumer (game loop), and async adds complexity without benefit. Standard `seq` is the simplest idiomatic choice.

**Alternatives considered**:
- `IAsyncEnumerable<GameFrame>`: Unnecessary complexity for single-consumer blocking I/O
- `MailboxProcessor` channels: Over-engineered for lock-step protocol
- Handler-based (current `StepWith`): Works but not composable with standard seq operations

## R2: Callback Integration with Stream Model

**Decision**: Callbacks remain as module functions taking `NetworkStream`. The session exposes a `Stream` property (as today) for callback use. No change to Callbacks module.

**Rationale**: Callbacks are synchronous request-response over the same socket. They work during frame processing (between receiving a frame and sending commands). The existing pattern `Callbacks.getUnitPos session.Stream unitId` is already idiomatic F# (module function + explicit dependency). Wrapping them as session members would add 25+ methods without improving ergonomics.

**Alternatives considered**:
- Session members: Verbose, 25+ methods, hides the NetworkStream dependency
- Pre-fetching into GameFrame: Would require fetching all possible data regardless of need, performance regression

## R3: Private Qualifier Audit

**Decision**: Remove all `private` qualifiers from module-level bindings in .fs files that have corresponding .fsi files. The .fsi signature file is the authoritative visibility control.

**Rationale**: F# .fsi files define the public API surface. Any binding not listed in the .fsi is module-private by compiler enforcement. Adding `private` in the .fs file is redundant and adds visual noise.

**Findings**: 30 `private` qualifiers found across 11 .fs files:
- Callbacks.fs: 7 (helper functions)
- EngineDiscovery.fs: 6 (helper functions)
- EngineLauncher.fs: 5 (helper functions)
- MapGrid.fs: 2 (array conversion helpers)
- MapCache.fs: 2 (cache dictionaries)
- Protocol.fs: 2 (version constant, request counter)
- Connection.fs: 1 (readExact helper)
- Events.fs: 1 (shutdownReasonToString helper)
- MapQuery.fs: 1 (boundsCheck helper)
- Commands.fs: 2 (constants)
- BarClient.fs: 1 (CleanupResources member - class member, keep as `member private`)

All module-level `private` qualifiers are redundant since corresponding .fsi files exist. The one `member private` on `CleanupResources` in the BarClient class should be kept (class members need explicit private).

## R4: Class vs Record for Session

**Decision**: Keep session management as a class (renamed from `BarClient` to stay or keep name) with IDisposable. This is the one justified use of a class in the codebase.

**Rationale**: The session manages mutable resources (socket, process, stream) with a defined lifecycle (Idle → Starting → Connected → Running → Stopped). IDisposable requires a class. The mutable state is genuinely needed for resource management.

**Alternatives considered**:
- Record with IDisposable: F# records can implement interfaces but mutable resource management is awkward
- Disposable module pattern: More complex, no real benefit over a class

## R5: EngineDisconnectedException

**Decision**: Keep as a class (custom exception). Custom exceptions in F# require class inheritance from `System.Exception`.

**Rationale**: F# exceptions defined with `exception Foo of ...` syntax compile to classes anyway. The current `EngineDisconnectedException` adds a `LastFrameNumber` property which is idiomatic for custom exceptions.

**Alternatives considered**:
- F# `exception` keyword: Cannot add extra properties like `LastFrameNumber`
- Result type: Not appropriate for truly exceptional conditions (socket disconnect)
