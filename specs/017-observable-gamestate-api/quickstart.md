# Quickstart: Observable GameState API

**Branch**: `017-observable-gamestate-api` | **Date**: 2026-04-09

## Build & Test

```bash
# Build everything
dotnet build

# Run unit tests
dotnet test src/FSBar.Client.Tests/

# Run live tests (requires engine)
dotnet test tests/FSBar.LiveTests/

# Update surface-area baselines after API changes
UPDATE_BASELINES=true dotnet test src/FSBar.Client.Tests/ --filter SurfaceAreaTests
```

## New Files to Create

| File | Purpose |
|------|---------|
| `src/FSBar.Client/GameState.fs` | GameState record, TrackedUnit, TrackedEnemy, EconomySnapshot, frame processing |
| `src/FSBar.Client/GameState.fsi` | Public API signature for GameState module |
| `src/FSBar.Client/UnitDefCache.fs` | Bulk unit def loading, name/ID lookup |
| `src/FSBar.Client/UnitDefCache.fsi` | Public API signature for UnitDefCache module |
| `src/FSBar.Client.Tests/GameStateTests.fs` | Unit tracking, economy, seeding tests |
| `src/FSBar.Client.Tests/UnitDefCacheTests.fs` | Cache loading and lookup tests |
| `src/FSBar.Client.Tests/Baselines/GameState.baseline` | Surface-area baseline |
| `src/FSBar.Client.Tests/Baselines/UnitDefCache.baseline` | Surface-area baseline |

## Files to Modify

| File | Change |
|------|--------|
| `src/FSBar.Client/BarClient.fs` | Replace seq loop with IObservable + background thread, add GameState property |
| `src/FSBar.Client/BarClient.fsi` | `Frames: IObservable<GameFrame>`, add `GameState: GameState` |
| `src/FSBar.Client/MapQuery.fs` | Add `nearestMetalSpot` function |
| `src/FSBar.Client/MapQuery.fsi` | Add `nearestMetalSpot` signature |
| `src/FSBar.Client/MapCache.fs` | Add `refreshDynamic` function |
| `src/FSBar.Client/MapCache.fsi` | Add `refreshDynamic` signature |
| `src/FSBar.Client/FSBar.Client.fsproj` | Add new .fs/.fsi files to compile order |
| `src/FSBar.Viz/LiveSession.fs` | Subscribe to IObservable instead of iterating seq |
| `scripts/prelude.fsx` | Update for IObservable API |
| `scripts/examples/*.fsx` | Update affected example scripts |
| `src/FSBar.Client.Tests/BarClientTests.fs` | Test IObservable behavior |

## Implementation Order

1. **UnitDefCache** — standalone, no dependencies on other new code
2. **GameState** — depends on UnitDefCache
3. **BarClient IObservable** — depends on GameState (wires it up)
4. **MapQuery/MapCache** — nearestMetalSpot + refreshDynamic
5. **Consumer updates** — LiveSession, scripts
6. **Idiom cleanup** — private qualifier removal, pattern audit

## Key Design Decisions

- **No System.Reactive dependency** — IObservable<T> is a BCL interface; custom implementation uses lock + subscriber list
- **Single-writer pattern** — background thread updates GameState; consumers read immutable snapshots
- **Sync init** — UnitDefCache loads all ~2500 defs at startup (few seconds), blocking until complete
- **Map auto-load** — MapCache loads on first access, refreshes LOS/radar each frame
