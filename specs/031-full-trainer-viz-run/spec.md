# Feature Specification: Full Trainer Game with Complete Visualization

**Feature Branch**: `031-full-trainer-viz-run`  
**Created**: 2026-04-16  
**Status**: Draft  
**Input**: User description: "run a full trainer game at 2 speed with full viz"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Launch a Full Trainer Game with All Overlays (Priority: P1)

A developer wants to observe a complete trainer bot game with every visualization overlay active so they can study unit behavior, economy, weapon ranges, sight lines, command queues, and terrain data simultaneously. They invoke a single command that starts the game at speed level 2 (5x game speed), opens the viewer window, and enables all available overlays from the start.

**Why this priority**: This is the core ask — a single-command experience for running a fully observable trainer game. Without this, the developer must manually toggle each overlay via keyboard after the game starts, missing early-game activity.

**Independent Test**: Can be tested by running the trainer command and verifying that the viewer opens with all overlays active and the game runs at speed level 2.

**Acceptance Scenarios**:

1. **Given** the trainer is invoked with full-viz mode, **When** the viewer window opens, **Then** all overlays (Units, Events, MetalSpots, EconomyHud, WeaponRanges, SightRanges, CommandQueue, FullNames) are active from the first rendered frame.
2. **Given** the trainer is invoked with speed level 2, **When** the game loop starts, **Then** the engine runs at 5x game speed and the viewer renders smoothly.
3. **Given** full-viz mode is active, **When** the user toggles an overlay off via keyboard (e.g., presses `W` to disable WeaponRanges), **Then** the toggle works as normal — the overlay turns off, and pressing `W` again re-enables it.

---

### User Story 2 - Run to Natural Game Completion (Priority: P2)

A developer wants the trainer game to run until a natural win or loss condition rather than stopping at a frame limit. At speed level 2, the game is slow enough to observe in real time, so there is no need for an artificial frame cap to prevent runaway games.

**Why this priority**: "Full game" implies running to completion. An artificial frame cap would cut the game short and defeat the purpose of observing the entire match.

**Independent Test**: Can be tested by running the trainer in full-viz mode and verifying the game continues past the default max_frames until a win/loss condition triggers or the user manually exits.

**Acceptance Scenarios**:

1. **Given** full-viz mode is active, **When** the game reaches the rung's default max_frames without a win/loss, **Then** the game continues running instead of stopping.
2. **Given** the game is running in full-viz mode, **When** the bot achieves victory (e.g., enemy commander destroyed), **Then** the game stops normally and writes results with the outcome.
3. **Given** the game is running in full-viz mode, **When** the user closes the viewer window, **Then** the game stops gracefully and writes partial results.

---

### User Story 3 - Terrain Layer Defaults for Full Viz (Priority: P3)

A developer wants the base terrain layer visible alongside all unit/economy overlays so the map context is clear. In full-viz mode, the base terrain layer should be active by default, providing a ground-truth backdrop for all overlay data.

**Why this priority**: Terrain context makes all other overlays more readable, but it is a secondary concern — the developer can always press `B` to toggle it on.

**Independent Test**: Can be tested by verifying the viewer opens with the base terrain layer visible when full-viz mode is active.

**Acceptance Scenarios**:

1. **Given** full-viz mode is active, **When** the viewer opens, **Then** the base terrain layer is rendered beneath all overlays.
2. **Given** full-viz mode is active and terrain is visible, **When** the user presses `B`, **Then** the terrain layer toggles off as normal.

### Edge Cases

- What happens when the game map has no cached map data? The viewer should fall back to a flat grid (existing behavior) without crashing.
- What happens if the engine crashes mid-game? The run directory should still capture all available logs and partial results (existing behavior).
- What happens if the developer passes `--speed` alongside full-viz mode? The explicit `--speed` value should take precedence over the full-viz default of speed 2.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a CLI option (e.g., `--full-viz`) that enables viewer mode with all overlays active from game start.
- **FR-002**: When full-viz mode is active, the default overlay set MUST include: Units, Events, MetalSpots, EconomyHud, WeaponRanges, SightRanges, CommandQueue, and FullNames.
- **FR-003**: When full-viz mode is active and no explicit `--speed` is provided, the speed level MUST default to 2 (5x game speed).
- **FR-004**: When full-viz mode is active, the base terrain layer MUST be enabled by default.
- **FR-005**: When full-viz mode is active, the max_frames limit from the ladder rung MUST be ignored, allowing the game to run to natural completion (win/loss/draw).
- **FR-006**: All existing keyboard toggles MUST continue to work normally in full-viz mode, allowing the developer to turn individual overlays on/off during the game.
- **FR-007**: The run metadata MUST record that full-viz mode was active, including the complete set of initially enabled overlays.
- **FR-008**: The `--full-viz` option MUST be compatible with all other existing CLI options (`--map`, `--bot`, `--opponent`, `--profile`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can start a fully observable trainer game with a single command invocation — no post-launch keyboard presses needed to enable overlays.
- **SC-002**: All eight overlay categories are visible within the first rendered frame of a full-viz game.
- **SC-003**: The game runs to natural completion (win or loss) without being cut short by a frame limit.
- **SC-004**: The viewer maintains smooth rendering (no visible stutter) at speed level 2 with all overlays active.
- **SC-005**: The run metadata captures the full-viz configuration so post-game analysis can distinguish full-viz runs from normal runs.

## Assumptions

- The developer has a working display environment (`DISPLAY=:0`) since visualization requires a graphical window.
- The engine and all required maps/data are already installed at their standard locations.
- Speed level 2 (5x) is slow enough for useful real-time observation with all overlays — no frame-skip or throttle mechanism is needed beyond the existing 60fps render cap.
- "Full viz" means all gameplay-information overlays; it does not include debug-only terrain layers (HeightMap, SlopeMap, ResourceMap, passability layers) which remain togglable via number keys.
- Existing `--viewer` behavior (default speed 3, limited overlay set) is preserved as-is; `--full-viz` is an additive option, not a replacement.
