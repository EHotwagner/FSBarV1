# Research: GameState API

**Feature**: 016-gamestate-api
**Date**: 2026-04-08

## R1: GameState Record vs Mutable Class

**Decision**: Immutable F# record with `processFrame` returning a new state.

**Rationale**: F# records are the idiomatic choice. `processFrame` is a pure-ish function (it calls engine callbacks as side effects but returns a new record). This makes state transitions explicit and debuggable — you can compare state before and after a frame. The BarClient class wraps this in a mutable `GameState option` property for convenience.

**Alternatives considered**:
- Mutable class with methods: Rejected — harder to test, harder to reason about state transitions, doesn't align with F# idioms.
- Agent/MailboxProcessor: Rejected — adds unnecessary complexity for a synchronous, single-threaded use case.

## R2: Unit Definition Loading Strategy

**Decision**: Eager load all definitions at `GameState.init` time. Load name, cost, build speed, weapon range, and build options for each definition.

**Rationale**: There are ~500 unit definitions in a typical BAR session. Loading 5 callbacks per def = ~2500 round-trips. At game speed 100 this takes <5 seconds. At speed 3 it will take longer (~30s) but this is a one-time cost that eliminates the current per-lookup scan that takes minutes. The init cost is acceptable because it happens once per session.

**Alternatives considered**:
- Lazy loading per name: Rejected — still requires scanning all defs to find the right name, just defers the cost.
- Cache only name+id, lazy-load details: Possible future optimization but premature — the full load is fast enough.

## R3: Pre-Existing Unit Seeding

**Decision**: During `GameState.init`, after loading unit defs, iterate through the first few step frames and process all UnitCreated/UnitFinished events to seed pre-existing units. The Repl's existing `warmup()` already steps 30 frames at init — `processFrame` will capture the commander during this warmup.

**Rationale**: The engine fires UnitCreated events during the initial frames for all starting units. By processing these frames through `processFrame`, the commander and any other starting units are automatically captured without special-case code.

**Alternatives considered**:
- Manual seeding via external unit list: Rejected — requires consumers to know about units independently, duplicates logic.
- Special init callback: Not possible — engine proxy doesn't support "get all units" callback.

## R4: Enemy Tracking Lifecycle

**Decision**: Enemies are added on `EnemyEnterLOS`/`EnemyEnterRadar`, updated on position queries while in LOS, and retained with stale positions when they leave LOS+radar. Only removed on `EnemyDestroyed`.

**Rationale**: Keeping stale enemy positions provides "fog of war memory" — the AI knows where enemies were last seen. This is standard RTS AI behavior. A `lastSeen` frame field enables consumers to judge staleness.

**Alternatives considered**:
- Remove on leave radar: Too aggressive — loses useful intel.
- Add configurable retention: Future enhancement, not needed for v1.

## R5: Position Refresh Strategy

**Decision**: Refresh positions and health for all tracked friendly units on every `Update` event via callbacks. Do not refresh enemy positions (only updated when in LOS via events).

**Rationale**: The `Update` event fires once per frame. Refreshing friendly units ensures accurate positions for the viz and debugging. With typical counts (<50 friendly units), this is ~100 callbacks per frame — well within the tick budget. Enemy positions are only available through LOS events.

**Alternatives considered**:
- Throttle to every N frames: Premature optimization — implement if profiling shows issues.
- Refresh only changed units: Not possible — no "unit moved" event exists.

## R6: MapCache Enhancement Approach

**Decision**: Extend existing MapCache module with new functions rather than creating a new module.

**Rationale**: MapCache already has the caching infrastructure (ConcurrentDictionary). Adding `refreshDynamic`, `metalSpots`, `nearestMetalSpot`, and `isPassable` is a natural extension of the existing module's responsibility.

**Alternatives considered**:
- New GameMap module: Rejected — would duplicate caching infrastructure already in MapCache.
