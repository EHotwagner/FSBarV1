# Feature Specification: GameViz State-Based Rendering API

**Feature Branch**: `030-gameviz-state-api`  
**Created**: 2026-04-16  
**Status**: Draft  
**Input**: User description: "Add onFrameWithState to GameViz — a socket-free entry point that accepts pre-built GameState + MapGrid as pure data, enabling the trainer bot to drive the visualizer without protocol corruption or deadlocks."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Trainer Bot Drives Visualizer Without Socket Contention (Priority: P1)

A developer runs the trainer bot with the `--viewer` flag. The bot performs its normal game loop — reading from the engine socket, updating its GameState, issuing commands. On each frame tick, the bot passes its already-built GameState and MapGrid to the visualizer. The visualizer renders the current game state (unit positions, health, economy, map terrain) in real time without ever touching the engine socket. The developer sees a live visualization window that stays perfectly in sync with the bot's view of the game, with no protocol corruption, deadlocks, or missed frames.

**Why this priority**: This is the core motivation for the feature. Without it, developers cannot use the visualizer alongside the trainer bot at all — the shared socket causes hard failures.

**Independent Test**: Can be tested by running the macro trainer bot with `--viewer` and confirming the visualization window renders unit positions, health bars, economy data, and map terrain throughout a full game without any socket errors or lockups in the bot loop.

**Acceptance Scenarios**:

1. **Given** a trainer bot game is running with visualization enabled, **When** the bot completes a frame and passes its GameState + MapGrid to the visualizer, **Then** the visualizer renders the current unit positions, health, and economy without performing any socket reads.
2. **Given** units are destroyed during a frame, **When** the visualizer receives the updated GameState (which no longer contains destroyed units), **Then** destruction indicators (explosions/flashes) appear at the correct positions before the units disappear from the display.
3. **Given** the bot is running at high game speed (5x+), **When** the visualizer receives rapid state updates, **Then** the bot loop does not stall or slow down due to visualization overhead.

---

### User Story 2 - Visualizer Initialization Without Socket Handshake (Priority: P2)

A developer starts the trainer bot with visualization. The visualizer receives map data (terrain grid, metal spot positions) and team assignment directly from the bot's warmup data, rather than performing its own socket-based map queries. The visualizer is ready to render the first frame immediately after attachment, with no deferred initialization or socket round-trips.

**Why this priority**: The existing socket-based initialization performs socket reads for map data during setup, which conflicts with the bot's socket usage. Immediate attachment from pre-computed data eliminates a whole class of startup race conditions.

**Independent Test**: Can be tested by confirming that initializing the visualizer with pre-computed map data, metal spots, and team ID results in a fully functional visualizer (terrain renders correctly, metal spots displayed) without any socket operations.

**Acceptance Scenarios**:

1. **Given** the bot has completed warmup and has a MapGrid, metal spots, and team ID, **When** it initializes the visualizer with these values, **Then** the visualizer is immediately ready to render frames without any socket communication.
2. **Given** the visualizer is initialized via state-based attachment, **When** the first frame is rendered, **Then** map terrain and metal spot overlays display correctly.

---

### User Story 3 - Non-Macro Bot Visualization Support (Priority: P3)

A developer runs the simpler bot script (which does not perform full map analysis during warmup) with the `--viewer` flag. The visualizer still works correctly, using either a cached map analysis or a minimal map skeleton derived from available map dimensions. The developer sees a functional (though potentially less detailed) visualization.

**Why this priority**: Extends visualization support beyond the macro bot to the simpler bot script, broadening the feature's utility. Lower priority because the macro bot is the primary trainer workflow.

**Independent Test**: Can be tested by running the simpler bot with `--viewer` and confirming the visualization window opens and renders unit positions and economy data, even if terrain detail is reduced.

**Acceptance Scenarios**:

1. **Given** the simpler bot is running with visualization but has no pre-computed MapGrid, **When** the visualizer is initialized, **Then** it uses a fallback map representation (from cache or map dimensions) and renders a functional display.
2. **Given** the fallback map data is less detailed than a full MapGrid, **When** the visualizer renders, **Then** unit positions, health, and economy data still display correctly on the simplified map.

---

### Edge Cases

- What happens when the GameState contains unit definition IDs not yet seen by the visualizer? The visualizer must resolve them from the unit definition cache without socket access.
- What happens when game events reference units that were destroyed between frames? The visualizer must process destruction events against the previous frame's unit map before rebuilding from the current GameState.
- What happens when the MapGrid is unavailable in the non-macro bot? The system must provide a graceful fallback rather than crashing.
- What happens when the visualizer receives its first frame before any units exist (e.g., very early game)? It should render an empty battlefield with just map terrain and economy.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a way to initialize the visualizer with map data (terrain grid, metal spot positions) and team identity from pre-computed values, without any engine socket communication.
- **FR-002**: The system MUST provide a per-frame rendering entry point that accepts a complete game state snapshot and map grid as input, producing a visual frame without any engine socket communication.
- **FR-003**: The system MUST resolve unit visual properties (shape, tier, faction, label) from the unit definition cache and bundled unit data when encountering new unit definition IDs, without socket access.
- **FR-004**: The system MUST process unit destruction and damage events against the previous frame's unit data before rebuilding the current frame's unit map, ensuring destruction indicators appear correctly.
- **FR-005**: The system MUST derive economy display data (metal and energy levels) from game state resource fields.
- **FR-006**: The system MUST support visualization for bot scripts that lack a pre-computed MapGrid by accepting a fallback map representation.
- **FR-007**: The existing socket-based visualization entry points MUST continue to work unchanged for non-trainer use cases (no regressions).
- **FR-008**: The trainer bot helper scripts MUST be updated to use the new state-based entry points, passing map data and game state directly instead of routing through socket callbacks.

### Key Entities

- **Game State Snapshot**: Pre-built snapshot of the game containing friendly units, enemy units, economy data, and game events. Sourced from the trainer bot's client.
- **Map Grid**: Terrain data (heightmap, slope map, resource map, dimensions) either from live map analysis or cached map files.
- **Unit Definition Properties**: Visual metadata (shape, size, weapon ranges, sight radius) resolved from bundled unit data by unit name, cached per definition ID.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The trainer bot with visualization enabled completes a full game (1000+ frames) with zero socket contention errors, deadlocks, or protocol corruption.
- **SC-002**: The state-based frame rendering path performs zero engine socket reads per frame.
- **SC-003**: The visualizer displays unit positions, health indicators, economy bars, and map terrain that match the bot's game state within the same frame.
- **SC-004**: The state-based initialization completes without any deferred socket queries — the visualizer is render-ready immediately after attachment.
- **SC-005**: Existing socket-based visualization (non-trainer use) continues to function identically, with no regressions in rendering or behavior.

## Assumptions

- The trainer bot's game state contains all data needed for visualization (unit positions, health, definition IDs, economy, events). No supplementary socket queries are required.
- LOS/radar overlay refresh is not needed for trainer visualization — the bot has full information and the trainer developer does not need fog-of-war rendering.
- The existing frame assembly logic can be reused to compose the final frame from the data populated by the state-based path.
- The macro bot already has MapGrid and metal spots from its warmup phase; the simpler bot can obtain equivalent data from the map cache or from map dimensions available at runtime.
- The bundled unit definition lookup works without socket access, using only the static unit definition data shipped with the project.
