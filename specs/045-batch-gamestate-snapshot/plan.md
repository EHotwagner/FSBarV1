# Implementation Plan: Batched GameState snapshot + FSBAR_TEST_ENGINE alias

**Branch**: `045-batch-gamestate-snapshot` | **Date**: 2026-04-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/045-batch-gamestate-snapshot/spec.md`

## Summary

Adopt HighBarV2 0.1.5's single-RPC `CALLBACK_GAME_GET_STATE = 15` as the
sole per-tick refresh path for `FSBar.Client.GameState`. Add the new proto
surface to `FSBar.Proto`, add a typed `getGameStateSnapshot` callback to
`FSBar.Client.Callbacks`, rewrite `GameState.processEvent`'s
`GameEvent.Update` branch to consume the snapshot, and delete the per-unit
`refreshUnit` + per-enemy refresh + per-resource `refreshEconomy` code. On
proxy version shortfall the connection fails fast with a descriptive
error. Secondary: accept `FSBAR_TEST_ENGINE` as preferred alias for
`HIGHBAR_TEST_ENGINE` across `EngineDiscovery`, `check-prerequisites.sh`,
and documentation.

## Technical Context

**Language/Version**: F# 9 on .NET 10.0
**Primary Dependencies**: FsGrpc 1.0.6, FSBar.Proto (regenerated via
`proto-regen` skill), existing `FSBar.Client.Protocol` replay buffer
**Storage**: N/A (pure in-memory per-tick refresh)
**Testing**: xUnit 2.9.x; surface-area baselines via `SurfaceAreaHelper`;
live integration under `FSBar.LiveTests`
**Target Platform**: Linux (headless BAR engine)
**Project Type**: F# client library + live-engine integration tests
**Performance Goals**: One snapshot RPC per `GameEvent.Update`, < 10 ms
wall-clock at 200 friendlies + 50 enemies (SC-001)
**Constraints**: No new NuGet dependencies (constitution §Engineering
Constraints); no per-unit/per-enemy/per-resource RPCs on the update path;
hard error (not fallback) when proxy lacks callback 15
**Scale/Scope**: Per-tick snapshot capped at `HIGHBAR_SNAPSHOT_MAX_UNITS`
(proxy-side default 4096); proto additions: 1 enum value + 5 messages +
1 oneof variant; F# surface additions: ~5 types + 1 callback + GameState
update-path rewrite.

## Constitution Check

| Principle | Applies | How the plan complies |
|-----------|---------|-----------------------|
| I. Spec-First Delivery | Yes — Tier 1 (public API + proto contract change) | Spec 045 + this plan + tasks.md (next phase); requirements traceable FR-001..FR-012 |
| II. Compiler-Enforced Structural Contracts | Yes | New / updated `.fsi` files: `Callbacks.fsi` (+`getGameStateSnapshot` + snapshot record types), `GameState.fsi` (no public-shape change; `TrackedEnemy.Health` already `option`), `EngineDiscovery.fsi` (doc update only). Surface-area baselines regenerated with `SURFACE_AREA_UPDATE=1`. |
| III. Test Evidence Is Mandatory | Yes | Unit tests for proto encode/decode + snapshot → `GameState` mapper (including frozen-enemy retention); live integration tests for (a) correctness vs engine ground-truth sampling, (b) radar-only `Health = None`, (c) hard-error on pre-0.1.5 proxy (forced by stubbing or running against older binary). |
| IV. Observability & Safe Failure Handling | Yes | Snapshot failure raises `EngineDisconnectedException` (existing surface) or a new descriptive error naming the minimum required proxy version — emitted via `HubLog` category `Error` for Hub sessions; no silent fallback. |
| V. Scripting Accessibility | Yes | Update one numbered FSI example (or add `scripts/examples/NN-gamestate-snapshot.fsx`) calling `Callbacks.getGameStateSnapshot` through the client prelude. |

All gates PASS. No Complexity Tracking entries.

## Project Structure

### Documentation (this feature)

```text
specs/045-batch-gamestate-snapshot/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── gamestate-snapshot.md
└── tasks.md             # created by /speckit.tasks
```

### Source Code (repository root)

```text
proto/highbar/
└── callbacks.proto                         # +CALLBACK_GAME_GET_STATE=15, +5 msgs, +snapshot_value=8 oneof

src/FSBar.Proto/
└── Generated/highbar/callbacks.proto.gen.fs   # regenerated via /proto-regen

src/FSBar.Client/
├── Callbacks.fsi / .fs                     # +snapshot record types, +getGameStateSnapshot
├── GameState.fsi / .fs                     # rewrite Update branch; remove refreshUnit + refreshEconomy
└── EngineDiscovery.fsi / .fs               # FSBAR_TEST_ENGINE alias + dual-read precedence

tests/FSBar.Client.Tests/
├── Baselines/*.baseline                    # regenerated (SURFACE_AREA_UPDATE=1)
├── CallbacksSnapshotTests.fs (new)         # proto roundtrip + mapper (frozen-enemy, radar None)
└── EngineDiscoveryTests.fs                 # +FSBAR_TEST_ENGINE precedence cases

tests/FSBar.LiveTests/
└── GameStateSnapshotLiveTests.fs (new)     # live-engine correctness + hard-error case

tests/
├── check-prerequisites.sh                  # read FSBAR_TEST_ENGINE then HIGHBAR_TEST_ENGINE
└── ENGINE-VERSION.md                       # doc update

scripts/examples/
└── 23-gamestate-snapshot.fsx (new)         # FSI walkthrough

CLAUDE.md                                   # doc update (FSBAR_TEST_ENGINE preferred)
```

**Structure Decision**: Single-project layout under `src/FSBar.{Proto,Client}` + existing test projects. No new projects; no new packages.

## Complexity Tracking

None. All gates pass without deviation.
