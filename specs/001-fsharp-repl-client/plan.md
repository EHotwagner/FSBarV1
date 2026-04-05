# Implementation Plan: F# REPL Client for BAR AI Orchestration

**Branch**: `001-fsharp-repl-client` | **Date**: 2026-04-05 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-fsharp-repl-client/spec.md`

## Summary

Build a pure F# library (FSBar.Client) that orchestrates BAR game processes and communicates with the HighBar V2 proxy AI over Unix domain sockets using a protobuf binary protocol. The library is designed for interactive use from F# Interactive (FSI), providing a single `BarClient` object that manages the entire lifecycle: launching the engine, establishing the socket connection, exchanging protobuf frames, and cleanly shutting down. F# protobuf bindings are generated from HighBarV2's `.proto` schemas using the `fsgrpc-proto` skill (FsGrpc). The BarData unit library is consumed as a NuGet package.

## Technical Context

**Language/Version**: F# / .NET 10.0
**Primary Dependencies**: FsGrpc 1.0.6 (protobuf generation), FsGrpc.Tools 1.0.6 (build-time), BarData (NuGet from local store)
**Storage**: Filesystem only — Unix domain sockets, temp directories, PID files, game-setup scripts
**Testing**: xUnit 2.9.x, Microsoft.NET.Test.Sdk
**Target Platform**: Linux (x86_64) — BAR engine and spring-headless are Linux-native
**Project Type**: Library (consumed from FSI/REPL)
**Performance Goals**: < 500us protobuf round-trip (matching HighBar V2 budget), < 30s cold start to first game event
**Constraints**: Pure F# only (constitution mandate), no runtime dependency on HighBar.Client or HighBar.Proto, must be packable via `dotnet pack`
**Scale/Scope**: Single-user local tool, 1 game session at a time, 953 unit definitions, 5 proto files to bind

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Requirement | Status | Notes |
|------|-------------|--------|-------|
| I. Spec-First | Change maps to current spec | PASS | Spec `001-fsharp-repl-client/spec.md` completed with clarifications |
| II. Compiler-Enforced Contracts | `.fsi` signature files for public modules | PASS (deferred to implementation) | Will create `.fsi` files for all public modules during task execution |
| III. Test Evidence | Automated tests per user story | PASS (deferred to implementation) | Test plan defined: unit tests for protocol, integration tests for engine lifecycle |
| IV. Observability | Structured diagnostics for significant events | PASS | FR-012 requires lifecycle console output |
| V. Scripting Accessibility | FSI prelude script | PASS | FR-016 requires `scripts/prelude.fsx` |
| E1. F#-only stack | No other languages in repo | PASS | Pure F# reimplementation, FsGrpc generates F# bindings |
| E2. `.fsi` for public modules | Signature files | PASS (deferred) | |
| E3. Surface-area baselines | Baseline tests | PASS (deferred) | |
| E4. `dotnet pack` | Library must be packable | PASS | Standard .fsproj with NuGet metadata |
| E5. Dependency minimization | Each dependency justified | PASS | FsGrpc (proto bindings), BarData (game unit data) — both essential |

## Project Structure

### Documentation (this feature)

```text
specs/001-fsharp-repl-client/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
proto/
├── buf.yaml                    # Buf configuration
├── buf.gen.yaml                # FsGrpc generation config
└── highbar/                    # Proto source files (copied from HighBarV2)
    ├── callbacks.proto
    ├── commands.proto
    ├── common.proto
    ├── events.proto
    └── messages.proto

src/
├── FSBar.Proto/                # Generated F# protobuf bindings
│   ├── FSBar.Proto.fsproj
│   └── Generated/              # FsGrpc output (auto-generated, not checked in)
│       ├── Callbacks.fs
│       ├── Commands.fs
│       ├── Common.fs
│       ├── Events.fs
│       └── Messages.fs
│
├── FSBar.Client/               # Core client library
│   ├── FSBar.Client.fsproj
│   ├── Connection.fs           # Unix socket connection (length-prefixed framing)
│   ├── Connection.fsi
│   ├── Protocol.fs             # Handshake, frame loop, callback request/response
│   ├── Protocol.fsi
│   ├── Events.fs               # GameEvent DU (28 variants) from proto EngineEvent
│   ├── Events.fsi
│   ├── Commands.fs             # Typed command builders (Move, Build, Attack, etc.)
│   ├── Commands.fsi
│   ├── Callbacks.fs            # Engine callback wrappers (map, economy, unit queries)
│   ├── Callbacks.fsi
│   ├── EngineConfig.fs         # Configuration record (mode, paths, timeouts, map)
│   ├── EngineConfig.fsi
│   ├── EngineLauncher.fs       # Process management (start/stop headless & graphical)
│   ├── EngineLauncher.fsi
│   ├── ScriptGenerator.fs      # game-setup.txt template generation
│   ├── ScriptGenerator.fsi
│   ├── BarClient.fs            # Top-level orchestrator (the REPL entry point)
│   └── BarClient.fsi
│
└── FSBar.Client.Tests/         # Test project
    ├── FSBar.Client.Tests.fsproj
    ├── ProtocolTests.fs        # Unit tests: framing, serialization, handshake
    ├── EventTests.fs           # Unit tests: event DU conversion
    ├── CommandTests.fs         # Unit tests: command builders
    ├── ScriptGeneratorTests.fs # Unit tests: game-setup.txt generation
    └── IntegrationTests.fs     # Integration: engine launch + connect + frames

scripts/
├── prelude.fsx                 # FSI prelude script (#load this to get started)
└── examples/
    ├── 01-hello-bar.fsx        # Minimal: start headless, receive events, stop
    ├── 02-graphical-game.fsx   # Launch graphical BAR and control from REPL
    ├── 03-query-units.fsx      # Query BarData unit definitions
    └── 04-step-by-step.fsx     # Frame-by-frame stepping and continuous run
```

**Structure Decision**: Three F# projects following the constitution's separation of concerns — generated proto bindings (FSBar.Proto), client library (FSBar.Client), and tests. The proto project is separate because it's auto-generated and should not contain hand-written code. The client library references the proto project and BarData NuGet.

## Complexity Tracking

No constitution violations to justify. The three-project structure (Proto, Client, Tests) is the minimum viable separation required by the auto-generation workflow.
