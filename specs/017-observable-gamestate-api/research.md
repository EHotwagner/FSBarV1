# Research: Observable GameState API

**Branch**: `017-observable-gamestate-api` | **Date**: 2026-04-09

## R1: IObservable Implementation Without Rx Dependency

**Decision**: Implement a minimal custom `IObservable<GameFrame>` using a lock-protected subscriber list, without depending on System.Reactive.

**Rationale**: `IObservable<'T>` and `IObserver<'T>` are BCL interfaces in `System` namespace. A simple implementation needs only:
- A mutable list of `IObserver<GameFrame>` protected by a lock
- A background thread that reads frames and calls `OnNext` for each subscriber
- `OnCompleted` on clean disconnect, `OnError` on protocol failure
- `Subscribe` returns an `IDisposable` that removes the observer from the list

This avoids adding System.Reactive as a dependency (constitution: minimize dependencies). Consumers who want advanced operators (Buffer, Throttle, etc.) can reference System.Reactive themselves — `IObservable<'T>` is the standard interface it operates on.

**Alternatives considered**:
- System.Reactive NuGet package: adds ~1MB dependency, full operator library. Rejected — consumers can add it optionally.
- F# `Event<'T>` / `IEvent`: simpler but doesn't support OnCompleted/OnError semantics needed for stream lifecycle.
- `IAsyncEnumerable<'T>`: pull-based (same as seq but async). Rejected — goal is push-based for multi-subscriber support.
- `MailboxProcessor` (F# agent): good for internal queueing but doesn't directly expose IObservable. Could be used internally.

## R2: Thread Safety for GameState Updates

**Decision**: GameState is updated on the background frame thread and exposed as an immutable snapshot. Consumers read the latest snapshot via a volatile reference.

**Rationale**: The GameState record is immutable — each frame produces a new record. The BarClient holds a `mutable gameState: GameState` field updated atomically (single writer = background thread). Readers get a consistent snapshot at any point. No locks needed for read access.

**Alternatives considered**:
- ConcurrentDictionary for units: adds unnecessary complexity since updates are single-threaded.
- Actor/mailbox for state: overhead not justified for single-writer pattern.
- Lock-protected mutable state: unnecessary when using immutable snapshots with single writer.

## R3: Unit Definition Cache Loading Strategy

**Decision**: Load all unit definitions synchronously during session initialization (after handshake, before first game frame).

**Rationale**: Loading ~2500 definitions requires ~2500 protocol round-trips (getUnitDefName, getUnitDefCost, getBuildSpeed, getMaxWeaponRange, getBuildOptions for each). At normal game speed this takes a few seconds. This is acceptable as a one-time init cost. Definitions are stable within a session.

The cache stores two maps:
- `Map<int, UnitDefInfo>` — lookup by ID (O(log n))
- `Map<string, int>` — reverse lookup by name → ID (O(log n))

**Alternatives considered**:
- Lazy per-def loading: slower for first access of each def, adds complexity.
- Background async loading: complicates API, definitions needed before first frame processing.
- Dictionary instead of Map: marginally faster but not idiomatic F#; Map is immutable which fits the pattern.

## R4: Pre-existing Unit Seeding

**Decision**: On Init event, query the engine for all units owned by our team using existing callbacks, and seed them into GameState.

**Rationale**: The commander (and any other pre-placed units) exist before the AI receives its first frame. The Init event provides the team ID. After Init, we can call callbacks to discover existing units. The engine doesn't emit UnitCreated for pre-existing units, so we must actively discover them.

**Approach**: After Init, iterate a reasonable range of unit IDs (or use a callback to list team units if available). For each unit found, populate TrackedUnit with position, health, and def info from the cache.

## R5: Observable vs seq — Breaking Change Migration

**Decision**: Replace `Frames: seq<GameFrame>` with `Frames: IObservable<GameFrame>`. This is a breaking API change.

**Rationale**: The change affects:
1. **BarClient.fsi** — type signature changes
2. **Consumer code** — `for frame in client.Frames` becomes `client.Frames.Subscribe(fun frame -> ...)`
3. **LiveSession.fs** — internal consumer, updated in this feature
4. **prelude.fsx + example scripts** — updated in this feature
5. **Surface-area baseline** — BarClient.baseline updated

All in-repo consumers are updated as part of this feature. External consumers (if any) will need to adapt.

## R6: Map Dynamic Layer Refresh

**Decision**: Refresh LOS and radar maps each frame via existing callbacks. Static layers (height, slope, resources) loaded once and cached permanently.

**Rationale**: LOS and radar change every frame as units move and are built/destroyed. Height, slope, and resource distribution are static terrain properties that never change during a game.

The existing `MapGrid.refreshLos` and `MapGrid.refreshRadar` functions already exist. MapCache needs a `refreshDynamic` function that calls both.

**Alternatives considered**:
- Refresh on-demand only: stale data risk, consumer must remember to refresh.
- Refresh every N frames: adds complexity for marginal perf gain.
- Skip radar/LOS refresh entirely: not useful for AI decision-making without current visibility data.

## R7: Nearest Metal Spot Query

**Decision**: Add `nearestMetalSpot` to MapQuery module. Uses linear scan of cached metal spots (from `getMetalSpots` callback, stored in MapGrid or MapCache).

**Rationale**: Typical BAR maps have 20-60 metal spots. Linear scan is O(n) with n < 100 — sub-microsecond. No spatial index needed.

**Alternatives considered**:
- KD-tree: overkill for < 100 points.
- Pre-sorted by region: adds complexity without meaningful perf gain.
