# Implementation Plan: Fully comprehensive scripting gRPC client

**Branch**: `046-scripting-full-client` | **Date**: 2026-04-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/046-scripting-full-client/spec.md`

## Summary

Extend `fsbar.hub.scripting.v1` so a gRPC-only client can operate as a
fully-fledged headless BAR bot: per-tick `GameState` + typed
`GameEvent`s carried on `StreamGameFrames`; unary map-data RPCs
(heightmap, slope, LOS, radar, resource map, metal spots, corners
heightmap, map info); extended `UnitDefInfoExtended`; and a
`SendCommandBatch` RPC (≤1024 `AICommand` entries, whole-batch
rejection above cap). All projection reads directly from
`BarClient.GameState` (feature 045 single source of truth); no new
engine callbacks. Grids are returned as `repeated float` /
`repeated int32` + width/height, with Hub channel
`MaxReceiveMessageSize` / `MaxSendMessageSize` raised accordingly.
Additive proto changes only — `buf breaking` must pass clean.

## Technical Context

**Language/Version**: F# 9 on .NET 10.0 (Constitution §Engineering Constraints)
**Primary Dependencies**: FsGrpc 1.0.6, Grpc.AspNetCore 2.67.0, SkiaSharp 2.88.6, BarData (local nupkg)
**Storage**: N/A — live session state is in-process (`BarClient.GameState`)
**Testing**: xUnit 2.9.x; live-engine tests under `tests/FSBar.Hub.LiveTests/` (no mocks per CLAUDE.md)
**Target Platform**: Linux (primary), loopback gRPC only (no new auth surface)
**Project Type**: gRPC service additive extension (single-project structure; affects `FSBar.Proto`, `FSBar.Hub`, `FSBar.Hub.LiveTests`, `scripts/examples/`)
**Performance Goals**: SC-002 per-tick message <64 KiB at 200 friendlies + 50 enemies; SC-003 <30 ms added latency on top of 1 Hz proxy cadence
**Constraints**: additive proto only (FR-015, `buf breaking` zero); state derived from `BarClient.GameState` — no new engine RPCs (FR-011); reuse existing fan-out + drop-on-slow-client from `StreamGameFrames` (FR-010); correlation-id header flow-through (FR-014)
**Scale/Scope**: one active session; worst-case SupportedMap grid payload (a few MiB — drives `MaxReceiveMessageSize`/`MaxSendMessageSize` bump); command batch cap 1024

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. Spec-First | PASS | spec.md present with 15 FRs, 7 SCs, 5 resolved clarifications |
| II. Compiler-Enforced `.fsi` | PASS (planned) | new/changed modules in `FSBar.Hub/ScriptingHub.fs(i)`, `FSBar.Proto/Generated/*.gen.fs` (generated — no `.fsi`). Surface-area baselines regenerate with `SURFACE_AREA_UPDATE=1` |
| III. Test Evidence | PASS (planned) | Live-engine tests per US1–US4 in `FSBar.Hub.LiveTests`; no mocks |
| IV. Observability / Safe Failure | PASS | correlation-id passthrough (FR-014); explicit "no-session" responses (FR-012); batch-oversize diagnostic (FR-008); HubLog categories unchanged |
| V. Scripting Accessibility | PASS | New numbered FSI walkthrough `scripts/examples/24-hub-full-client.fsx` (FR-013, SC-006) |
| Engineering: F# exclusive, no new deps | PASS | FR (Assumptions): no new NuGet |
| Engineering: gRPC via fsgrpc-* | PASS | additive change only; proto regen via `proto-regen` skill |

**No violations** — Complexity Tracking table is empty.

## Project Structure

### Documentation (this feature)

```text
specs/046-scripting-full-client/
├── plan.md                 # this file
├── spec.md                 # feature spec (input)
├── research.md             # Phase 0 output
├── data-model.md           # Phase 1 output
├── quickstart.md           # Phase 1 output
├── contracts/
│   └── scripting.proto.md  # proto-surface delta (additive)
├── checklists/             # existing
└── tasks.md                # produced by /speckit.tasks (not this command)
```

### Source Code (repository root)

```text
proto/
├── highbar/                # unchanged (types reused: AICommand, GameEvent shapes, UnitDefInfo base)
└── hub/
    └── scripting.proto     # extended: GameFrameMessage.{game_state,game_events}, new unary
                            # map RPCs, UnitDefInfoExtended, SendCommandBatch

src/
├── FSBar.Proto/
│   └── Generated/*.gen.fs  # regenerated via `proto-regen` skill (no .fsi for generated)
├── FSBar.Client/           # unchanged — BarClient.GameState already the single source of truth
└── FSBar.Hub/
    ├── ScriptingHub.fs(i)  # wire projection: GameState → GameStateFrame;
                            #                  Callbacks → map-query responses;
                            #                  AICommand[] batch → engine forward;
                            #                  message-size limits on server channel
    └── ScriptingHub.Tests (if present)

tests/FSBar.Hub.LiveTests/  # new live tests: StateStreamLiveTests, MapQueriesLiveTests,
                            # CommandBatchLiveTests, CommandSurfaceLiveTests (FR-009)

scripts/examples/
└── 24-hub-full-client.fsx  # SC-006 FSI walkthrough
```

**Structure Decision**: Single-project additive extension of the existing
`fsbar.hub.scripting.v1` service. Wire surface lives in
`proto/hub/scripting.proto`; implementation concentrates in
`src/FSBar.Hub/ScriptingHub.fs(i)`; all state derives from the feature
045 `BarClient.GameState` — no new engine-side paths.

## Complexity Tracking

*None — Constitution Check passes without violations.*
