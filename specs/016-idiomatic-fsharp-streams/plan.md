# Implementation Plan: Idiomatic F# Streams Refactor

**Branch**: `016-idiomatic-fsharp-streams` | **Date**: 2026-04-09 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/016-idiomatic-fsharp-streams/spec.md`

## Summary

Refactor FSBar.Client to expose game state as a `seq<GameFrame>` and commands via a separate `SendCommands` method, replacing the step-based handler API (`StepWith`/`Run`/`RunUntil`). Remove redundant `private` qualifiers from .fs files (30 occurrences across 11 files). Keep idiomatic patterns already in use (records, DUs, pattern matching, mutable in hot paths).

## Technical Context

**Language/Version**: F# / .NET 10.0  
**Primary Dependencies**: FsGrpc 1.0.6 (protobuf), FSBar.Proto (generated types), BarData (unit definitions)  
**Storage**: N/A (in-memory session state + Unix domain sockets)  
**Testing**: xUnit 2.9.x, Microsoft.NET.Test.Sdk  
**Target Platform**: Linux (Arch)  
**Project Type**: Library (NuGet-packable)  
**Performance Goals**: Frame processing throughput equivalent to current implementation  
**Constraints**: Lock-step protocol (receive frame → send commands → next frame)  
**Scale/Scope**: Single game session, single consumer, ~13 modules

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| Spec-First Delivery (I) | PASS | Spec complete with clarifications, plan in progress |
| Compiler-Enforced Contracts (II) | PASS | All .fsi files exist, will be updated for new API surface |
| Test Evidence (III) | PASS | Existing tests will be updated; surface area baselines regenerated |
| Observability (IV) | PASS | No change to diagnostics behavior |
| Scripting Accessibility (V) | PASS | Prelude and example scripts updated in Phase 5 (after old API removed) |
| F# exclusive stack | PASS | No language changes |
| .fsi for every public module | PASS | All 13 modules have .fsi files |
| Surface area baselines | PASS | Will regenerate after API changes |

**Scripting Accessibility (V)**: Prelude and 9 example scripts use Step/StepWith/Run/RunUntil. These are updated in implementation Phase 5 after the old API is removed, maintaining constitution compliance.

## Project Structure

### Documentation (this feature)

```text
specs/016-idiomatic-fsharp-streams/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── bar-client-fsi.md
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
src/FSBar.Client/
├── EngineDiscovery.fsi/fs   # No API changes, remove private qualifiers
├── EngineConfig.fsi/fs      # No changes
├── Connection.fsi/fs        # No API changes, remove private qualifiers
├── Events.fsi/fs            # No API changes, remove private qualifier
├── Commands.fsi/fs          # No API changes, remove private qualifiers
├── Protocol.fsi/fs          # No API changes, remove private qualifiers
├── ScriptGenerator.fsi/fs   # No changes
├── EngineLauncher.fsi/fs    # No API changes, remove private qualifiers
├── Callbacks.fsi/fs         # No API changes, remove private qualifiers
├── MapGrid.fsi/fs           # No API changes, remove private qualifiers
├── MapQuery.fsi/fs          # No API changes, remove private qualifier
├── MapCache.fsi/fs          # No API changes, remove private qualifiers
└── BarClient.fsi/fs         # API changes: add Frames/SendCommands, remove Step*/Run*

src/FSBar.Client.Tests/
├── BarClientTests.fs        # Update for new API surface
├── ProtocolTests.fs         # Minimal changes (protocol layer unchanged)
├── SurfaceAreaTests.fs      # Regenerate baselines
├── ConnectionTests.fs       # No changes
├── CommandsTests.fs         # No changes
├── EventsTests.fs           # No changes
├── EngineConfigTests.fs     # No changes
├── ScriptGeneratorTests.fs  # No changes
└── EngineDiscoveryTests.fs  # No changes
```

**Structure Decision**: No new files or directories. This is a refactoring of existing modules within the existing project structure.

## Implementation Phases

### Phase 1: Remove Private Qualifiers (P2 stories, mechanical)

Remove all redundant `private` qualifiers from module-level bindings in .fs files. These are safe to remove because the corresponding .fsi files already restrict visibility.

**Files to modify** (30 removals):
- `Callbacks.fs`: 7 `private` qualifiers on helper functions (intParam, getInt, getFloat, getString, getVector3, getFloatArray, getIntArray)
- `EngineDiscovery.fs`: 6 (standardDataDir, isExecutable, tryBinary, resolveFromEnvVar, resolveFromConfigFile, sourceLabel)
- `EngineLauncher.fs`: 5 (extractGuid, detectSpringDataDir, copyArchiveCache, writePidFile, launchEngine)
- `MapGrid.fs`: 2 (toFloat32Array2D, toIntArray2D)
- `MapCache.fs`: 2 (gridCache, passabilityCache)
- `Protocol.fs`: 2 (protocolVersion, nextRequestId)
- `Connection.fs`: 1 (readExact)
- `Events.fs`: 1 (shutdownReasonToString)
- `MapQuery.fs`: 1 (boundsCheck)
- `Commands.fs`: 2 (INTERNAL_ORDER, MAX_TIMEOUT)

**Not removed**: `BarClient.fs` line 227 `member private _.CleanupResources()` — this is a class member, not a module binding. Class members need explicit `private`.

**Verification**: `dotnet build` + `dotnet test` after all removals.

### Phase 2: Refactor BarClient to Stream API (P1 stories)

#### 2a: Add Frames property and SendCommands method

Add to BarClient class:
- `mutable pendingCommands: Highbar.AICommand list` field (initialized to `[]`)
- `Frames` property returning `seq<GameFrame>` using a sequence expression that:
  1. Checks state is Connected
  2. In a loop: sends pending commands for previous frame (or empty), receives next frame, yields it
  3. On shutdown (None from receiveFrame): transitions to Stopped, ends sequence
  4. On disconnect exception: transitions to Error, ends sequence
- `SendCommands` method that:
  1. Validates session is Connected or Running
  2. Sets `pendingCommands` to the provided list
  3. Raises `InvalidOperationException` if session is Stopped/Idle/Error

#### 2b: Update BarClient.fsi

- Add `Frames: seq<Protocol.GameFrame>` property
- Add `SendCommands: commands: Highbar.AICommand list -> unit` method
- Remove `Step`, `StepWith`, `Run`, `RunUntil` signatures

#### 2c: Remove Step/StepWith/Run/RunUntil

Remove the four methods from BarClient.fs implementation. The `Reset` method needs updating to use the new stream internally (iterate Frames + SendCommands instead of StepWith).

#### 2d: Update BarClient tests

- Remove tests for `stream_access_before_connect_throws` (if Stream is still exposed)
- Update or replace any tests that exercised Step/StepWith/Run/RunUntil
- Add tests for:
  - `Frames` returns sequence in Connected state
  - `SendCommands` raises when session is Stopped
  - `SendCommands` queues commands for next frame response

#### 2e: Regenerate surface area baselines

Run `UPDATE_BASELINES=true dotnet test` to update the BarClient baseline file reflecting the new API surface (Frames, SendCommands added; Step, StepWith, Run, RunUntil removed).

### Phase 2.5: Update Scripts (Constitution §V)

Update prelude.fsx and all 9 affected example scripts to use the new Frames+SendCommands API in place of Step/StepWith/Run/RunUntil. All script updates are parallelizable (different files).

**Affected scripts** (10 files, all under `scripts/`):
- `prelude.fsx`, `examples/01-hello-bar.fsx`, `examples/02-graphical-game.fsx`
- `examples/04-step-by-step.fsx`, `examples/05-map-layers.fsx`
- `examples/06-game-viz-basic.fsx`, `examples/07-game-viz-layers.fsx`
- `examples/Repl.fsx`, `examples/ReplGraphical.fsx`

**Verification**: All scripts load in FSI without errors.

### Phase 3: Verify & Finalize

- Full `dotnet build` for the solution
- Full `dotnet test` for FSBar.Client.Tests
- Review all .fsi files for consistency
- Verify no `private` qualifiers remain on module-level bindings
- Qualitative frame throughput check (SC-006)

## Complexity Tracking

No constitution violations requiring justification. All changes are within established patterns.

## Risks

| Risk | Mitigation |
|------|------------|
| Reset method uses StepWith internally | Rewrite Reset to use Frames + SendCommands |
| Sequence expression and mutable pendingCommands interaction | Sequence is single-consumer; pendingCommands is set between iterations |
| Downstream consumers break (viz, scripts) | Out of scope per clarification; follow-up work tracked |
