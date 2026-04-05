# Implementation Plan: Test Suite and Functionality Report

**Branch**: `002-test-suite-report` | **Date**: 2026-04-05 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-test-suite-report/spec.md`

## Summary

Create a comprehensive xUnit test suite for all 7 major FSBar.Client modules, run it, and produce a Markdown report in `/reports/testreports/` documenting which modules are working and which are not. Tests target pure logic and serialization round-trips where possible, avoiding external dependencies (game engine, live sockets).

## Technical Context

**Language/Version**: F# / .NET 10.0
**Primary Dependencies**: FsGrpc 1.0.6 (protobuf), BarData (unit definitions), xUnit 2.9.x
**Storage**: N/A (file-based report output only)
**Testing**: xUnit + Microsoft.NET.Test.Sdk 17.x (already configured in FSBar.Client.Tests)
**Target Platform**: Linux (development environment)
**Project Type**: Library (test suite addition + one-time report)
**Performance Goals**: N/A (tests should complete in under 60 seconds)
**Constraints**: No game engine available; no guaranteed Unix domain socket availability
**Scale/Scope**: ~7 test files, ~50-80 test cases, 1 report file

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec-First Delivery | PASS | Spec exists at specs/002-test-suite-report/spec.md |
| II. Compiler-Enforced Structural Contracts | N/A | Test files do not require .fsi signatures (internal test code, not public API) |
| III. Test Evidence Is Mandatory | PASS | This feature IS the test evidence — tests will validate existing behavior |
| IV. Observability and Safe Failure | PASS | Test failures provide clear diagnostics via xUnit assertions |
| V. Scripting Accessibility | N/A | Tests are not a public API; no prelude/script changes needed |
| Engineering: F# exclusive stack | PASS | All tests in F# |
| Engineering: .fsi for public modules | N/A | No new public modules introduced |
| Engineering: Surface-area baselines | N/A | No public API changes |

**Post-Phase 1 Re-check**: All gates still pass. No public API changes, no new dependencies, no .fsi files needed.

## Project Structure

### Documentation (this feature)

```text
specs/002-test-suite-report/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── test-report-format.md
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── FSBar.Client/              # Existing library (unchanged)
│   ├── EngineConfig.fs(i)
│   ├── ScriptGenerator.fs(i)
│   ├── Connection.fs(i)
│   ├── Protocol.fs(i)
│   ├── Commands.fs(i)
│   ├── Events.fs(i)
│   ├── Callbacks.fs(i)
│   ├── EngineLauncher.fs(i)
│   └── BarClient.fs(i)
├── FSBar.Client.Tests/        # Test suite (new files added here)
│   ├── FSBar.Client.Tests.fsproj
│   ├── EngineConfigTests.fs   # NEW
│   ├── ScriptGeneratorTests.fs # NEW
│   ├── ConnectionTests.fs     # NEW
│   ├── ProtocolTests.fs       # NEW
│   ├── CommandsTests.fs       # NEW
│   ├── EventsTests.fs         # NEW
│   └── BarClientTests.fs      # NEW
└── FSBar.Proto/               # Existing protobuf bindings (unchanged)

reports/
└── testreports/
    └── test-report.md         # NEW — generated after test run
```

**Structure Decision**: Tests go in the existing `FSBar.Client.Tests` project. Report goes in a new `reports/testreports/` directory at repo root. No new projects or dependencies are introduced.

## Implementation Phases

### Phase 1: Unit-Testable Modules (EngineConfig, ScriptGenerator, Commands, Events)

These modules have no external dependencies and can be fully tested with pure unit tests.

**EngineConfigTests.fs**:
- `defaultConfig` returns expected defaults (Headless mode, standard paths, timeout)
- Custom config overrides work correctly
- Both EngineMode variants construct properly

**ScriptGeneratorTests.fs**:
- `generate` with headless config produces valid script content
- `generate` with graphical config produces valid script content
- Generated script contains expected map name, game type, AI settings

**CommandsTests.fs**:
- Each of the 16+ command constructors (MoveCommand, BuildCommand, AttackCommand, PatrolCommand, GuardCommand, StopCommand, RepairCommand, ReclaimUnitCommand, FightCommand, SelfDestructCommand, SetWantedMaxSpeedCommand, CustomCommand, SendTextMessageCommand, GiveMeResourceCommand, GiveMeNewUnitCommand, CallLuaRulesCommand) returns a valid `Highbar.AICommand`
- Commands contain correct parameters (unit IDs, positions, amounts)

**EventsTests.fs**:
- `fromProto` correctly maps each of the 28 `Highbar.EngineEvent` variants to the corresponding `GameEvent` discriminated union case
- Unknown event types map to `GameEvent.Unknown`

### Phase 2: Stream-Dependent Modules (Connection, Protocol)

These modules use sockets/streams but can be tested with MemoryStream substitution for serialization logic.

**ConnectionTests.fs**:
- `sendMessage` + `recvBytes` round-trip: write to MemoryStream, read back, verify identical bytes
- Length-prefix framing: verify 4-byte big-endian length header is written correctly
- Empty message handling

**ProtocolTests.fs**:
- Handshake message parsing from pre-constructed byte stream
- `receiveFrame` correctly deserializes a protobuf-encoded frame
- `sendFrameResponse` serializes commands correctly
- Frame with multiple events produces correct GameFrame

### Phase 3: State Machine Module (BarClient)

**BarClientTests.fs**:
- Initial state is `Idle` after creation
- Config is accessible and matches what was provided
- `create` with custom config preserves settings
- State transitions to `Error` on connection failure (non-existent socket path)
- Dispose cleans up resources

### Phase 4: Run Tests and Generate Report

1. Run `dotnet test` with TRX logger and console output
2. Analyze results by module
3. Write Markdown report to `reports/testreports/test-report.md` following the contract format
4. Report includes: executive summary, module status table, per-module details, failure analysis, untestable areas

## Test File Registration

All new `.fs` test files must be added to `FSBar.Client.Tests.fsproj` in the `<ItemGroup>` with `<Compile Include="...">` entries, in dependency order.

## Complexity Tracking

No constitution violations — no complexity justification needed.
