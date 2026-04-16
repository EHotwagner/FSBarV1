# Implementation Plan: GameViz State-Based Rendering API

**Branch**: `030-gameviz-state-api` | **Date**: 2026-04-16 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/030-gameviz-state-api/spec.md`

## Summary

Add socket-free entry points (`attachWithState` + `onFrameWithState`) to the `GameViz` module so the trainer bot can drive the visualizer by passing pre-built `GameState` and `MapGrid` directly, eliminating the shared-socket contention that causes protocol corruption and deadlocks. The existing socket-based path remains unchanged for non-trainer use cases.

## Technical Context

**Language/Version**: F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints)  
**Primary Dependencies**: Existing in-repo — `FSBar.Viz` (`GameViz`, `SceneBuilder`, `VizTypes`, `UnitGlyph`, `UnitLabels`), `FSBar.Client` (`GameState`, `MapGrid`, `UnitDefCache`, `MapCacheFile`, `BarClient`), `SkiaViewer` 1.1.3-dev, `SkiaSharp` 2.88.6, `BarData` (NuGet from local store), `xUnit 2.9.x`. **No new NuGet dependencies.**  
**Storage**: N/A (in-memory only, no persistence changes)  
**Testing**: xUnit 2.9.x, live integration tests with engine  
**Target Platform**: Linux x64  
**Project Type**: Library (FSBar.Viz) + scripts (bots/trainer/)  
**Performance Goals**: Zero socket reads per state-based frame; ≤1ms frame assembly overhead; no stall in bot loop at 5x+ game speed  
**Constraints**: Must not regress existing socket-based visualization; must maintain `.fsi` signature contracts  
**Scale/Scope**: 2 new public functions in GameViz, ~150 lines of new implementation, ~50 lines of viewer.fsx changes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Phase 0 Check

| Gate | Status | Evidence |
|------|--------|----------|
| §I Spec-First Delivery | PASS | Spec at `specs/030-gameviz-state-api/spec.md` with user stories, FR/SC criteria |
| §I Tier 1 — public API surface | PASS | Adds 2 new `val` declarations to `GameViz.fsi` — plan defines signatures |
| §I Tier 1 — new dependencies | PASS | No new dependencies |
| §I Tier 1 — inter-project contracts | PASS | No `.proto` or OpenAPI changes |
| §II `.fsi` signature files | PASS | `GameViz.fsi` will be updated with new functions |
| §II Surface-area baselines | PASS | Baseline test will be updated |
| §III Test evidence | PASS | Plan includes unit tests for state-based path + live integration |
| §IV Observability | PASS | `eprintfn "[GameViz]"` diagnostics on attach/frame errors |
| §V Scripting accessibility | N/A | No new project — extends existing module |

### Post-Phase 1 Re-Check

| Gate | Status | Evidence |
|------|--------|----------|
| §II `.fsi` contracts defined | PASS | See [contracts/gameviz-api.md](contracts/gameviz-api.md) |
| §II No undocumented API drift | PASS | Only 2 new functions, signatures specified in contract |
| §III Verification criteria | PASS | Each user story has independent test criteria in spec |

## Project Structure

### Documentation (this feature)

```text
specs/030-gameviz-state-api/
├── plan.md              # This file
├── research.md          # Phase 0 output — 7 research decisions
├── data-model.md        # Phase 1 output — entity catalog + data flow
├── quickstart.md        # Phase 1 output — usage guide
├── contracts/
│   └── gameviz-api.md   # Phase 1 output — GameViz API contract
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── FSBar.Viz/
│   ├── GameViz.fsi          # Updated: +2 new val declarations
│   ├── GameViz.fs           # Updated: +attachWithState, +onFrameWithState, +helpers
│   └── (all other files unchanged)
├── FSBar.Client/
│   └── (no changes — GameState, MapGrid, UnitDefCache already have all needed API)

tests/
├── FSBar.Viz.Tests/
│   └── GameVizSurfaceTests.fs  # Updated: surface-area baseline for new functions

bots/trainer/
├── helpers/
│   └── viewer.fsx           # Updated: use attachWithState/onFrameWithState
├── bot.fsx                  # Updated: callsites pass mapGrid/gameState to viewer
└── bot_macro.fsx            # Updated: callsites pass mapGrid/metalSpots/gameState to viewer
```

**Structure Decision**: No new projects or directories. Changes are confined to `FSBar.Viz` (2 files), `FSBar.Viz.Tests` (1 baseline + 1 behavioral test), and `bots/trainer/` (viewer.fsx + bot callsites).

## Implementation Approach

### Phase 1: Core API (`attachWithState` + `onFrameWithState`)

**GameViz.fsi** — Add two new function signatures:

```fsharp
val attachWithState: mapGrid: MapGrid -> metalSpots: (float32 * float32 * float32 * float32) array -> teamId: int -> unit
val onFrameWithState: gameState: GameState -> mapGrid: MapGrid -> unit
```

**GameViz.fs** — Implementation:

1. **`ensureDefPropsFromCache`** (private helper): Like `ensureDefProps` but resolves DefId → name via `UnitDefCache.tryFindById` instead of `Callbacks.getUnitDefName`. Falls back to `sprintf "def%d" defId` for unknown DefIds.

2. **`attachWithState`**: Populates `mapGridRef`, `metalSpots`, `myTeamId` from parameters. No `clientRef` needed. Calls `computeAutoFit`. Emits `eprintfn` diagnostic.

3. **`onFrameWithState`**: 
   - Process `gameState.Events` for indicators (destruction → create indicator at last known position; damage → combat indicator; creation → creation indicator; enemy spotted → indicator). Uses `gameState.Units`/`Enemies` for positions instead of socket queries.
   - Rebuild `units` map from `gameState.Units` (friendly, `isEnemy=false`) + `gameState.Enemies` (enemy, `isEnemy=true`).
   - Populate `defPropsCache` via `ensureDefPropsFromCache` for all encountered DefIds.
   - Track `unfinishedUnits` from `TrackedUnit.IsFinished`.
   - Derive economy from `gameState.Metal`/`Energy` → `EconomyData`.
   - Update `mapGridRef` with provided `mapGrid`.
   - Build and store snapshot via `buildSnapshot`.

### Phase 2: Viewer Script Update

**viewer.fsx** — Replace socket-dependent path:

- `startViewer` takes `mapGrid: MapGrid option`, `metalSpots`, `teamId` instead of `client`. When `Some`, calls `attachWithState` immediately (no deferred pattern needed since there are no socket reads). When `None`, falls back to MapCacheFile or flat MapGrid (US3).
- `viewerOnFrame` calls `onFrameWithState client.GameState mapGrid` instead of `onFrame frame`.
- Remove `pendingClient`/`clientAttached` deferred-attach machinery.
- Update callsites in both `bot.fsx` and `bot_macro.fsx`.

### Phase 3: Testing + Baseline

- Update surface-area baseline to include `attachWithState` and `onFrameWithState`.
- Integration test: construct a `GameState` with known units, call `onFrameWithState`, verify `GameSnapshot` contains expected `DisplayUnits`.

## Complexity Tracking

No constitution violations. No complexity justifications needed.
