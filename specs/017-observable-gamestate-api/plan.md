# Implementation Plan: Observable GameState API

**Branch**: `017-observable-gamestate-api` | **Date**: 2026-04-09 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/017-observable-gamestate-api/spec.md`

## Summary

Replace the pull-based `seq<GameFrame>` stream with a push-based `IObservable<GameFrame>` and add a centralized `GameState` layer that automatically tracks friendly units, enemies, economy, and cached unit definitions on top of the observable. Includes permanent queryable map with static layer caching and dynamic layer refresh. Clean up F# idioms throughout.

## Technical Context

**Language/Version**: F# / .NET 10.0  
**Primary Dependencies**: FsGrpc 1.0.6 (protobuf), BarData (NuGet local feed), System.IObservable (BCL — no external Rx needed)  
**Storage**: In-memory (Map, ConcurrentDictionary caches, Array2D grids)  
**Testing**: xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x  
**Target Platform**: Linux (Unix domain sockets)  
**Project Type**: Library (FSBar.Client) consumed by REPL scripts, viz tools, and AI agents  
**Performance Goals**: Frame processing at engine speed (~60fps at 1x game speed), sub-1ms unit def lookups, sub-1ms map queries  
**Constraints**: Single-threaded frame consumption internally (observable background thread), callbacks are synchronous over the same socket  
**Scale/Scope**: ~2500 unit definitions cached at init, up to hundreds of tracked units per session, 13 public modules in FSBar.Client

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| I. Spec-First Delivery | PASS | Spec 017 covers all changes; Tier 1 (public API surface changes) |
| II. Compiler-Enforced Structural Contracts | PASS | New modules (GameState, UnitDefCache) will have .fsi files; existing .fsi files updated for IObservable change; surface-area baselines updated |
| III. Test Evidence Is Mandatory | PASS | Unit tests for GameState tracking, IObservable behavior, UnitDefCache lookups; live tests updated for new API |
| IV. Observability and Safe Failure | PASS | Observable OnError/OnCompleted for disconnect; errors raised on post-session command submission |
| V. Scripting Accessibility | PASS | prelude.fsx and example scripts updated for IObservable API |
| Engineering: .fsi for every public module | PASS | All new modules get .fsi files |
| Engineering: Surface-area baselines | PASS | Baselines updated for all changed modules |
| Engineering: dotnet pack | PASS | FSBar.Client already packable |

## Project Structure

### Documentation (this feature)

```text
specs/017-observable-gamestate-api/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── bar-client-fsi.md
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── FSBar.Client/
│   ├── BarClient.fs/.fsi          # MODIFY: seq<GameFrame> → IObservable<GameFrame>, add GameState property
│   ├── GameState.fs/.fsi          # NEW: GameState record, TrackedUnit, TrackedEnemy, EconomySnapshot
│   ├── UnitDefCache.fs/.fsi       # NEW: Bulk unit def loading + instant name/ID lookup
│   ├── Events.fs/.fsi             # UNCHANGED
│   ├── Commands.fs/.fsi           # UNCHANGED
│   ├── Callbacks.fs/.fsi          # UNCHANGED (kept as on-demand queries)
│   ├── Connection.fs/.fsi         # UNCHANGED
│   ├── Protocol.fs/.fsi           # UNCHANGED
│   ├── MapGrid.fs/.fsi            # UNCHANGED
│   ├── MapQuery.fs/.fsi           # MODIFY: add nearestMetalSpot query
│   ├── MapCache.fs/.fsi           # MODIFY: add dynamic layer refresh, auto-load on first access
│   ├── EngineConfig.fs/.fsi       # UNCHANGED
│   ├── EngineDiscovery.fs/.fsi    # UNCHANGED
│   ├── EngineLauncher.fs/.fsi     # UNCHANGED
│   └── ScriptGenerator.fs/.fsi   # UNCHANGED
├── FSBar.Proto/                   # UNCHANGED
└── FSBar.Viz/
    └── LiveSession.fs/.fsi        # MODIFY: subscribe to IObservable instead of iterating seq

src/FSBar.Client.Tests/
├── GameStateTests.fs              # NEW: unit/enemy tracking, economy, pre-existing unit seeding
├── UnitDefCacheTests.fs           # NEW: bulk load, name lookup, ID lookup
├── BarClientTests.fs              # MODIFY: test IObservable behavior, multi-subscriber
├── SurfaceAreaTests.fs            # Existing — picks up new baselines automatically
└── Baselines/
    ├── GameState.baseline         # NEW
    ├── UnitDefCache.baseline      # NEW
    ├── BarClient.baseline         # UPDATE
    ├── MapQuery.baseline          # UPDATE
    └── MapCache.baseline          # UPDATE

tests/FSBar.LiveTests/
└── EventTests.fs                  # MODIFY: use IObservable subscription

scripts/
├── prelude.fsx                    # MODIFY: update for IObservable API
└── examples/                      # MODIFY: update affected scripts
```

**Structure Decision**: Follows existing single-project layout. Two new modules (GameState, UnitDefCache) added to FSBar.Client. No new projects needed.

## Complexity Tracking

No constitution violations. All changes fit within the existing project structure.

## Implementation Phases

### Phase 1: IObservable Core (P1 — User Stories 1, 4)

Convert `BarClient.Frames: seq<GameFrame>` to `BarClient.Frames: IObservable<GameFrame>`.

**Approach**:
- Background thread in BarClient reads frames from socket (existing loop logic)
- Each frame is pushed to subscribers via a simple Subject-like implementation (custom IObservable using lock + subscriber list — no Rx dependency)
- `SendCommands` remains unchanged (queues commands, delivered on next protocol response)
- OnCompleted on disconnect; OnError on protocol errors
- Late subscribers receive frames from subscription point only

**Files changed**: BarClient.fs/.fsi, BarClient.baseline  
**Tests**: BarClientTests.fs — subscribe/receive, multi-subscriber, completion on disconnect, error on post-session commands

### Phase 2: GameState Tracking (P1 — User Stories 2, 3)

Add `GameState` module and wire it as an internal subscriber to the observable.

**Approach**:
- `GameState` record: frame number, team ID, friendly units (Map<int, TrackedUnit>), enemies (Map<int, TrackedEnemy>), economy (metal + energy EconomySnapshot)
- `UnitDefCache`: at init, calls `getUnitDefs` + `getUnitDefName`/cost/build-speed/range/options for each — stores in Map<int, UnitDefInfo> and Map<string, int> for reverse lookup
- GameState subscribes to the frame observable internally and processes events:
  - UnitCreated/Finished/Destroyed → add/update/remove from friendly units
  - UnitIdle → set IsIdle flag
  - EnemyEnterLOS/LeaveRadar/Destroyed → add/update/remove enemies
  - Update → refresh positions/health via callbacks, reset IsIdle if position changed
  - Economy → refresh via callbacks each frame
- Pre-existing units seeded via Init event + getUnitDefs scan
- Exposed as `BarClient.GameState: GameState` (current snapshot, updated each frame)

**Files changed**: GameState.fs/.fsi (NEW), UnitDefCache.fs/.fsi (NEW), BarClient.fs/.fsi, baselines  
**Tests**: GameStateTests.fs, UnitDefCacheTests.fs — tracking correctness, seeding, cache lookups

### Phase 3: Permanent Map (P2 — User Story 5)

Enhance MapCache to auto-load on first access and refresh dynamic layers per frame.

**Approach**:
- MapCache.fromEngine becomes lazy (auto-loads on first query, not just explicit call)
- Add `refreshDynamic: stream -> unit` to refresh LOS/radar layers
- Add `nearestMetalSpot: MapGrid -> float32 * float32 * float32 -> (float32 * float32 * float32 * float32)` to MapQuery
- GameState calls `refreshDynamic` each frame after initial load

**Files changed**: MapCache.fs/.fsi, MapQuery.fs/.fsi, baselines  
**Tests**: MapQuery tests for nearestMetalSpot, MapCache tests for auto-load + refresh

### Phase 4: F# Idiom Cleanup (P2 — User Story 6)

- Audit .fs files for unnecessary `private` qualifiers on module-level bindings — remove where .fsi handles visibility
- Verify records/DUs used consistently (no unnecessary classes)
- Retain mutable patterns in Connection, Protocol, MapGrid hot paths

**Files changed**: Various .fs files (implementation only — no .fsi changes)  
**Tests**: Full test suite pass confirms behavioral equivalence

### Phase 5: Consumer Updates

- Update LiveSession.fs to subscribe to IObservable instead of iterating seq
- Update prelude.fsx and example scripts for new API
- Update Repl.fsx / ReplGraphical.fsx

**Files changed**: LiveSession.fs/.fsi, scripts/prelude.fsx, scripts/examples/*.fsx  
**Tests**: LiveSession tests, manual script verification
