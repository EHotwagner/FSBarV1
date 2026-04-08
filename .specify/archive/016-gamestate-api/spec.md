# Feature Specification: GameState API, Unit Debugging, and Permanent Map

**Feature Branch**: `016-gamestate-api`
**Created**: 2026-04-08
**Status**: Draft
**Input**: User description: "Better API to handle/query gamestate and units/unitdefs. Better debugging with per-unit logging. Permanent queryable map representation."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Centralized Game State Tracking (Priority: P1)

As an AI developer using the FSBar REPL, I want the system to automatically track all friendly units, known enemies, and economy so that I don't have to manually process events or maintain my own unit tracking code.

**Why this priority**: This is the foundation — without centralized state tracking, every consumer (REPL, visualization, AI scripts) must duplicate event processing logic. Currently the REPL and GameViz each have their own independent tracking with ~30 and ~150 lines of duplicated code respectively. Units that exist before tracking starts (e.g., the commander at game start) are missed entirely.

**Independent Test**: Start a game session, advance frames, and query the game state for all friendly units, their positions, health, and idle status — without writing any event processing code.

**Acceptance Scenarios**:

1. **Given** a new game session is started, **When** the first frame is processed with tracked stepping, **Then** the game state contains the commander unit with correct position, health, and definition info.
2. **Given** a running game with tracked state, **When** a new unit is built and completes, **Then** the game state automatically includes the new unit with its current position and health.
3. **Given** a running game with tracked state, **When** a unit is destroyed, **Then** it is removed from the game state on the next frame.
4. **Given** a running game with tracked state, **When** the economy changes, **Then** metal and energy snapshots (current, income, usage, storage) are updated each frame.
5. **Given** a running game with tracked state, **When** an enemy enters line of sight, **Then** it appears in the enemies collection with position, health, and visibility flags.

---

### User Story 2 - Instant Unit Definition Lookup (Priority: P1)

As an AI developer, I want to look up unit definitions by name instantly so that I can issue build commands without scanning hundreds of definitions over slow protocol round-trips.

**Why this priority**: Currently, finding the definition ID for "armmex" requires calling `getUnitDefs(500)` then looping through each ID calling `getUnitDefName` — hundreds of synchronous protocol calls that take minutes at low game speeds. This blocks the entire workflow.

**Independent Test**: After game initialization, look up "armmex" by name and receive its definition ID, cost, build speed, and build options instantly.

**Acceptance Scenarios**:

1. **Given** a game session is initialized with tracked state, **When** a unit definition is looked up by name (e.g., "armmex"), **Then** the result is returned instantly without additional protocol calls.
2. **Given** a game session is initialized, **When** all unit definitions are queried, **Then** each definition includes name, cost, build speed, weapon range, and build options.
3. **Given** a game session is initialized, **When** a non-existent unit name is looked up, **Then** the result indicates the definition was not found.

---

### User Story 3 - Permanent Queryable Map (Priority: P2)

As an AI developer, I want a persistent map representation that loads static data once and refreshes dynamic layers (visibility, radar) each frame, with convenient query functions for metal spots and terrain passability.

**Why this priority**: The current map loads all layers on demand via expensive callbacks. There's no way to quickly ask "what's the nearest metal spot?" or "is this position passable for tanks?" without loading the full map and writing custom query code.

**Independent Test**: After game initialization, query the nearest metal spot to the commander's position and check terrain passability at specific coordinates — all returning results instantly from cached data.

**Acceptance Scenarios**:

1. **Given** a game session with a loaded map, **When** the nearest metal spot to a position is queried, **Then** the correct spot is returned with its coordinates and richness value.
2. **Given** a game session with a loaded map, **When** terrain passability is checked for a specific unit movement type at given coordinates, **Then** the result correctly reflects whether that unit type can traverse the terrain.
3. **Given** a game session with a loaded map, **When** frames advance, **Then** line-of-sight and radar layers are refreshed while static layers (height, slope, resources) remain cached.
4. **Given** a game session, **When** the map is queried before any explicit load call, **Then** the map loads automatically on first access and caches for subsequent queries.

---

### User Story 4 - Unit Debugging and Watch System (Priority: P2)

As an AI developer, I want to "watch" specific units and see their position, health, and status printed each frame so that I can debug unit behavior without manually querying after every step.

**Why this priority**: When debugging build orders or movement commands, developers must manually call `units()` or `unit' id` after every step. A watch system that automatically reports tracked units each frame saves significant time during iterative debugging.

**Independent Test**: Watch a unit by ID, advance several frames, and observe automatic status reports printed each frame showing the unit's position, health, and idle status.

**Acceptance Scenarios**:

1. **Given** a running game, **When** a unit is added to the watch list and frames advance with auto-reporting enabled, **Then** the unit's position, health, and status are printed after each frame.
2. **Given** a watched unit, **When** the unit is destroyed, **Then** the watch report indicates the unit no longer exists.
3. **Given** multiple watched units, **When** frames advance, **Then** all watched units are reported in each frame's output.
4. **Given** auto-reporting is disabled, **When** frames advance, **Then** no automatic output is produced, but manual report calls still work.

---

### User Story 5 - Simplified REPL and Visualization (Priority: P3)

As an AI developer, I want the REPL and visualization to use the centralized game state so that they stay in sync and I don't encounter bugs from divergent tracking logic.

**Why this priority**: Once the centralized state exists, consumers should use it to eliminate duplicated code and ensure consistency. This is a natural follow-on to User Story 1.

**Independent Test**: Start a game with visualization, advance frames, take a screenshot, and verify that units shown in the viz match the units reported by the REPL — both reading from the same game state.

**Acceptance Scenarios**:

1. **Given** a game with visualization open, **When** units are queried in the REPL, **Then** the same units appear in the visualization.
2. **Given** the REPL's unit lookup functions, **When** called, **Then** they read from the centralized game state rather than maintaining their own tracking.
3. **Given** the visualization, **When** it receives game state updates, **Then** it renders units, economy, and events from the centralized state without its own event processing.

---

### Edge Cases

- What happens when a unit definition lookup is attempted before the game state is initialized?
- How does the system handle units that existed before tracking started (e.g., commander at game start)?
- What happens when a watched unit is transferred to another team?
- How does enemy tracking behave when an enemy leaves both LOS and radar — is it kept as stale intel or removed?
- What happens when the engine connection is lost during a tracked step?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST maintain a centralized game state that tracks all friendly units with their position, health, definition, and idle status.
- **FR-002**: System MUST track known enemy units with position, health, definition, and visibility flags (in LOS, in radar).
- **FR-003**: System MUST cache all unit definitions at initialization time, providing instant lookup by name or ID.
- **FR-004**: System MUST update economy data (metal and energy: current, income, usage, storage) each frame.
- **FR-005**: System MUST provide tracked stepping that combines frame advancement with automatic state updates.
- **FR-006**: System MUST support a unit watch list that reports watched units' status each frame when auto-reporting is enabled.
- **FR-007**: System MUST cache static map layers (height, slope, resource, terrain classification) after first load and only refresh dynamic layers (LOS, radar) each frame.
- **FR-008**: System MUST provide a nearest-metal-spot query that returns the closest extraction point to given coordinates.
- **FR-009**: System MUST provide terrain passability checks for different unit movement types at given coordinates.
- **FR-010**: System MUST seed pre-existing units (units that existed before tracking started) into the game state at initialization.
- **FR-011**: System MUST preserve backward compatibility — existing step and query functions continue to work unchanged.
- **FR-012**: System MUST track unit idle status by setting IsIdle=true on engine UnitIdle callbacks. IsIdle is reset to false when position change is detected during the Update refresh (indicating the unit is executing an order).

### Key Entities

- **GameState**: Central record holding all tracked state — frame number, team ID, friendly units, known enemies, economy, cached unit definitions, and last frame's events.
- **TrackedUnit**: A friendly unit with ID, definition, position, health, completion status, and idle status.
- **TrackedEnemy**: A known enemy unit with ID, definition, last-known position, health, and visibility flags.
- **UnitDefInfo**: Cached unit definition with ID, name, cost, build speed, weapon range, and build options.
- **EconomySnapshot**: Resource state for one resource type (current level, income rate, usage rate, storage capacity).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Unit definition lookup by name completes in under 1 millisecond after initialization (vs. current minutes at low game speed).
- **SC-002**: All existing REPL commands (units, economy, build, move, viz) continue to work identically from the user's perspective.
- **SC-003**: Pre-existing units (e.g., commander) appear in the game state after initialization and warmup frames (the standard warmup step count), without requiring any consumer-side event processing code.
- **SC-004**: Nearest metal spot queries return results in under 1 millisecond from cached data.
- **SC-005**: Developers can watch a unit and see its status reported each frame without writing any custom query code.
- **SC-006**: The REPL and visualization show consistent unit data when both are active, with no divergence between tracked units.
- **SC-007**: Map passability queries return correct results for all four movement types (kbot, tank, hover, ship) without requiring full map reload.

## Assumptions

- Unit definition IDs are stable within a session (same engine version produces the same IDs). Definitions are loaded once at initialization and not refreshed.
- The engine proxy's callback API is fixed — no new callbacks can be added. All state tracking must work within the existing 27 callbacks.
- The one-time cost of loading all unit definitions at initialization (estimated ~2500 protocol round-trips) is acceptable. This takes a few seconds at normal game speed.
- Enemy units that leave both LOS and radar are kept in state with stale positions (marked as not visible) rather than removed. They are only removed on explicit destroy events.
- The existing `Step()` and `StepWith()` methods remain unchanged. Tracked stepping is an opt-in addition via new methods.
- Position and health refresh for tracked units happens every frame. For sessions with hundreds of units, this may need throttling in the future, but the initial implementation refreshes all units each frame.
