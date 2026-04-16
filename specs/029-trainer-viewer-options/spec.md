# Feature Specification: Trainer Viewer and Runtime Options

**Feature Branch**: `029-trainer-viewer-options`  
**Created**: 2026-04-16  
**Status**: Draft  
**Input**: User description: "improve the iterative trainer with the option of a full viewer experience. add speed option 1-5,max. add map option. add self ai, opponent ai options."

## Clarifications

### Session 2026-04-16

- Q: How should the viewer handle frame rendering when the game runs faster than display rate? → A: Viewer renders at ~60fps independently; game simulation runs at requested speed with no throttling (frames may be skipped visually).
- Q: Should the engine's own graphical mode also be an option, or is FSBar.Viz the exclusive viewer? → A: FSBar.Viz is the exclusive viewer; engine always runs headless.
- Q: When the developer closes the SkiaViewer window mid-game, what should happen? → A: The game continues headless to completion; the viewer is purely observational.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Run Trainer with FSBar.Viz Viewer (Priority: P1)

As a developer, I want to launch a training run with an FSBar.Viz viewer window so that I can visually observe my bot's behavior, unit movements, and combat outcomes in real time using the project's information-dense unit glyph rendering.

**Why this priority**: Visual observation is the primary feature request and the most impactful for debugging bot behavior and understanding game dynamics. Without seeing what happens, the developer must infer behavior from logs alone.

**Independent Test**: Launch a training run with the viewer option enabled and verify a SkiaViewer window opens displaying the live game state (map terrain, unit glyphs, overlays), while the engine runs headless and the bot executes its strategy.

**Acceptance Scenarios**:

1. **Given** the trainer is invoked with the viewer option, **When** the game starts, **Then** the engine launches in headless mode and a SkiaViewer window opens displaying the live game state via FSBar.Viz.
2. **Given** the trainer is running with the viewer, **When** the bot issues commands, **Then** unit movements, builds, and attacks are reflected in the viewer window in near-real-time.
3. **Given** the trainer is invoked without the viewer option, **When** the game starts, **Then** the engine launches in headless mode with no viewer window (backward-compatible default).
4. **Given** the viewer is open and the developer closes the SkiaViewer window, **When** the game is still running, **Then** the game continues headless to completion and produces all normal run artifacts.
5. **Given** the viewer is active, **When** the game simulation runs faster than display rate, **Then** the viewer renders at ~60fps independently, skipping intermediate frames without throttling the game.

---

### User Story 2 - Set Game Speed (Priority: P1)

As a developer, I want to choose a game speed level (1 through 5, or max) so that I can watch the game at a comfortable pace when using the viewer, or run at maximum speed for batch iterations.

**Why this priority**: Speed control is essential for the viewer experience to be useful. At the current default (100x), the game is unwatchable. Speed control also matters for headless runs where the developer may want different throughput.

**Independent Test**: Launch a training run at each speed level and verify the in-game simulation runs at the expected pace.

**Acceptance Scenarios**:

1. **Given** the developer specifies speed level 1, **When** the game starts, **Then** the simulation runs at normal (1x real-time) speed.
2. **Given** the developer specifies speed level 3, **When** the game starts, **Then** the simulation runs at a moderate speed suitable for observation.
3. **Given** the developer specifies speed level "max", **When** the game starts, **Then** the simulation runs at maximum engine speed (equivalent to the current 100x default).
4. **Given** no speed is specified, **When** the game starts, **Then** the simulation runs at maximum speed (preserving current default behavior).

---

### User Story 3 - Select Map (Priority: P2)

As a developer, I want to choose which map the training run plays on so that I can test my bot's behavior across different terrain and map layouts.

**Why this priority**: Map variety is important for testing bot generalization, but a single default map is sufficient for initial development. This extends the existing static map configuration.

**Independent Test**: Launch a training run with a specific map name and verify the game loads on that map.

**Acceptance Scenarios**:

1. **Given** the developer specifies a map name (e.g., "Red Comet Remake 1.8"), **When** the game starts, **Then** the engine loads the specified map.
2. **Given** the developer specifies a map that is not installed locally, **When** the trainer attempts to launch, **Then** the trainer reports a clear error before or shortly after engine launch.
3. **Given** no map is specified, **When** the game starts, **Then** the default map from the ladder configuration is used (currently "Avalanche 3.4").

---

### User Story 4 - Select Self AI Bot Script (Priority: P2)

As a developer, I want to choose which bot script runs as "my" AI so that I can quickly switch between the rush bot, macro bot, or any new bot script without editing environment variables.

**Why this priority**: Switching between bot scripts is a common workflow during development. Making it a first-class option reduces friction and error.

**Independent Test**: Launch a training run specifying different bot scripts and verify each one loads and executes its strategy.

**Acceptance Scenarios**:

1. **Given** the developer specifies the rush bot, **When** the game starts, **Then** the rush bot strategy (bot.fsx) is used.
2. **Given** the developer specifies the macro bot, **When** the game starts, **Then** the macro bot strategy (bot_macro.fsx) is used.
3. **Given** the developer specifies a custom bot script path, **When** the trainer launches, **Then** that script is loaded and executed.
4. **Given** the developer specifies a bot script that does not exist, **When** the trainer attempts to launch, **Then** a clear error is reported before engine launch.

---

### User Story 5 - Select Opponent AI (Priority: P2)

As a developer, I want to choose the opponent AI and its difficulty/profile so that I can test my bot against different skill levels and AI types.

**Why this priority**: Opponent selection is currently embedded in the ladder rung choice. Making it a direct option enables quick iteration without editing ladder.json.

**Independent Test**: Launch a training run specifying different opponent AIs and verify the correct opponent loads in the game.

**Acceptance Scenarios**:

1. **Given** the developer specifies "NullAI" as the opponent, **When** the game starts, **Then** the opponent is a passive NullAI.
2. **Given** the developer specifies "BARb" with a difficulty profile (e.g., "hard"), **When** the game starts, **Then** the opponent is BARb configured at that difficulty.
3. **Given** the developer specifies an AI name that is not available in the engine, **When** the game starts, **Then** the engine reports the error (trainer surfaces it).
4. **Given** no opponent is specified, **When** the game starts, **Then** the default opponent from the ladder configuration is used.

---

### Edge Cases

- What happens when the viewer option is used but no display is available (e.g., headless server)? The SkiaViewer window creation should fail with a clear error.
- What happens when speed "max" is combined with viewer mode? The game runs at maximum speed even with the viewer open (developer's choice); the viewer renders what it can at ~60fps.
- What happens when the developer specifies both a rung name and explicit map/opponent overrides? Explicit options override the rung defaults.
- What happens when an invalid speed value is provided (e.g., "7" or "abc")? The trainer reports a usage error and exits before launching.
- What happens when the developer closes the viewer window mid-game? The game continues headless to completion; all run artifacts are still produced.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The trainer runner MUST accept an option to open an FSBar.Viz viewer window (via SkiaViewer) that displays the live game state. The engine MUST always run in headless mode regardless of viewer option.
- **FR-002**: The trainer runner MUST accept a speed option with valid values of 1, 2, 3, 4, 5, or "max". Speed level 1 corresponds to 1x real-time; "max" corresponds to the current maximum speed (100x). Intermediate levels map to increasing speed multipliers.
- **FR-003**: The trainer runner MUST accept a map option that overrides the map defined in the ladder rung.
- **FR-004**: The trainer runner MUST accept an option to select the self bot script, overriding the default (bot.fsx) and the BOT_SCRIPT environment variable.
- **FR-005**: The trainer runner MUST accept an option to select the opponent AI name, overriding the opponent defined in the ladder rung.
- **FR-006**: The trainer runner MUST accept an option to set the opponent AI profile/difficulty (e.g., "easy", "medium", "hard", "dev"), passed to the opponent AI's options.
- **FR-007**: When no new options are specified, the trainer MUST behave identically to its current behavior (full backward compatibility).
- **FR-008**: The trainer MUST validate all option values before launching the engine and report clear usage errors for invalid inputs.
- **FR-009**: The speed mapping MUST be: 1 = game speed 1, 2 = game speed 5, 3 = game speed 10, 4 = game speed 20, 5 = game speed 50, max = game speed 100.
- **FR-010**: When viewer mode is active and no speed is explicitly set, the trainer SHOULD default to speed level 3 (moderate observation speed) instead of max.
- **FR-011**: Run metadata (meta.json) MUST record which options were used (viewer mode, speed, map, self AI, opponent AI and profile) for each run.
- **FR-012**: The viewer MUST render at approximately 60fps independently of the game simulation speed, skipping intermediate frames without throttling the engine.
- **FR-013**: Closing the SkiaViewer window MUST NOT terminate the game. The training run MUST continue headless to completion and produce all normal run artifacts.
- **FR-014**: The viewer MUST display the existing FSBar.Viz GameViz experience including map terrain, unit glyphs, and overlay hotkeys (W, L, C, N).

### Key Entities

- **Training Run**: A single game session with a specific bot, opponent, map, speed, and display mode. Produces artifacts in a run directory.
- **Speed Level**: An abstraction over the raw engine game speed multiplier, mapping human-friendly levels (1-5, max) to engine values.
- **Bot Script**: An F# script (.fsx) file that implements the bot's strategy, loaded by dotnet fsi at runtime.
- **Viewer**: An FSBar.Viz GameViz window rendered via SkiaViewer, displaying real-time game state from the headless engine. Purely observational — does not affect game execution.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can launch a training run with a live FSBar.Viz viewer window using a single command invocation, without editing configuration files.
- **SC-002**: Speed level 3 allows comfortable human observation of unit movements and combat (roughly 10x real-time).
- **SC-003**: All existing training workflows (batch headless runs at max speed) continue to work without any changes to invocation.
- **SC-004**: A developer can switch between any combination of map, bot, opponent, and display mode using command-line options alone.
- **SC-005**: Invalid option combinations or values are caught and reported before the engine process is spawned.
- **SC-006**: Closing the viewer window mid-game does not interrupt the training run or prevent artifact generation.

## Assumptions

- A display environment (DISPLAY, XDG_RUNTIME_DIR) is available on the developer's machine when viewer mode is requested.
- Maps referenced by name are already downloaded and available in the engine's data directory.
- The existing FSBar.Viz GameViz pipeline and SkiaViewer infrastructure are sufficient to render live game state from a headless engine session.
- The speed-to-multiplier mapping (FR-009) is a reasonable default; exact values can be tuned during implementation without changing the specification.
- Opponent AI options beyond a single "profile" string (e.g., complex JSON) are out of scope; the profile option covers the primary BARb difficulty use case.
- The viewer uses the existing GameViz hotkeys and overlay system without modification.
