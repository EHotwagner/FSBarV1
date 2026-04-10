# Feature Specification: Synthetic Visualization Test Data

**Feature Branch**: `018-synthetic-viz-data`  
**Created**: 2026-04-10  
**Status**: Draft  
**Input**: User description: "create complex synthetic data to create and test a visualization with. create realistic scenes that last for 10 seconds, thats 300 frames. create 3 different scenes with different maps. changes between frames need to be approx realistic."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Generate a complete synthetic game scene (Priority: P1)

A developer working on visualization tooling needs a sequence of 300 GameState snapshots (10 seconds at 30 fps) representing a plausible BAR game. The scene includes friendly units, enemy units, economy values, and a stream of GameEvents -- all using the real FSBar.Client types (GameState, TrackedUnit, TrackedEnemy, EconomySnapshot, UnitDefCache, GameEvent, GameFrame). The data must be usable without a live engine connection.

**Why this priority**: Without realistic synthetic data, visualization work cannot proceed independently of the live engine.

**Independent Test**: Can be tested by generating the scene and validating that every frame contains well-formed GameState records with internally consistent data (e.g., units referenced in events exist in the Units/Enemies maps, economy values stay within storage bounds, positions change by plausible per-frame deltas).

**Acceptance Scenarios**:

1. **Given** a request for Scene 1, **When** the generator runs, **Then** it produces exactly 300 GameState snapshots with monotonically increasing FrameNumbers, each populated with units, enemies, economy, and events.
2. **Given** any generated GameState in the sequence, **When** inspected, **Then** all TrackedUnit positions differ from the previous frame by no more than a realistic movement speed (~2-6 elmos/frame), and economy values change smoothly.
3. **Given** any generated GameFrame, **When** its Events list is inspected, **Then** events reference only unit/enemy IDs that exist (or are being created/destroyed) in that frame's GameState.

---

### User Story 2 - Three distinct scenes with different map characteristics (Priority: P1)

The developer needs variety: three separate scenes on different maps so the visualization can be tested against different terrain sizes, unit counts, and tactical situations. Each scene should tell a different "story" (e.g., early-game buildup, mid-game skirmish, late-game siege).

**Why this priority**: A single scene risks overfitting the visualization to one data shape. Multiple diverse scenes are essential for robust testing.

**Independent Test**: Can be tested by generating all three scenes and verifying they differ in map dimensions, unit def composition, unit counts over time, and event distributions.

**Acceptance Scenarios**:

1. **Given** the three scenes, **When** compared, **Then** each uses different map dimensions (world-space coordinate bounds).
2. **Given** Scene A, **When** its unit composition is examined, **Then** it represents an early-game buildup (few units growing to moderate, mostly constructors and basic combat units).
3. **Given** Scene B, **When** its event stream is examined, **Then** it contains significant combat activity (UnitDamaged, EnemyDamaged, WeaponFired, UnitDestroyed events).
4. **Given** Scene C, **When** examined, **Then** it features high unit counts and diverse unit types characteristic of a late-game scenario.

---

### User Story 3 - Realistic frame-to-frame continuity (Priority: P1)

Each frame must be a plausible evolution from the previous one. Units don't teleport. Economy doesn't spike randomly. New units appear via UnitCreated/UnitFinished event pairs. Enemies transition through radar/LOS states realistically.

**Why this priority**: The visualization will animate transitions between frames. Unrealistic jumps would expose the synthetic nature and fail to test interpolation/rendering edge cases properly.

**Independent Test**: Can be tested by iterating consecutive frame pairs and asserting that position deltas, economy deltas, and event sequences are within realistic bounds.

**Acceptance Scenarios**:

1. **Given** consecutive frames N and N+1, **When** a unit exists in both, **Then** its position has changed by at most ~6 elmos per axis per frame.
2. **Given** a unit that appears in frame N but not frame N-1, **Then** frame N's events include a UnitCreated event for that unit ID.
3. **Given** economy values across consecutive frames, **Then** Current values change by no more than Income + Usage magnitudes, and never exceed Storage or drop below 0.
4. **Given** an enemy that transitions from InRadar=false to InLOS=true, **Then** there is an intermediate frame with EnemyEnterRadar and/or EnemyEnterLOS events.

---

### Edge Cases

- What happens when a unit is created and destroyed within the same 300-frame window? At least one scene must include this.
- How does the data handle units at map boundaries? Some units should patrol near coordinate edges.
- What happens when economy storage is full? At least one scene should show income exceeding usage while at max storage (Current clamped to Storage).
- An enemy should enter LOS, leave LOS, then re-enter LOS within the scene to test visibility toggling.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST produce exactly 300 GameState records per scene, with FrameNumber values from 1 to 300.
- **FR-002**: System MUST produce a corresponding list of 300 GameFrame records per scene, where each GameFrame's Events list contains the events that caused the transition to that frame's GameState.
- **FR-003**: All generated data MUST use the real FSBar.Client types: GameState, TrackedUnit, TrackedEnemy, EconomySnapshot, UnitDefCache, UnitDefInfo, GameEvent, and GameFrame.
- **FR-004**: System MUST provide 3 distinct scenes with the following characteristics:
  - **Scene A ("Small Map - Early Game Buildup")**: Small map (~4096x4096 elmos). Starts with 1 commander per side. Over 300 frames, ~10-15 friendly units are constructed. Economy ramps from zero income to moderate production. Few enemies visible.
  - **Scene B ("Medium Map - Mid-Game Skirmish")**: Medium map (~8192x8192 elmos). Starts with ~20 friendly units and ~15 enemies. Active combat: units take damage, fire weapons, some are destroyed. Economy fluctuates as factories produce and units are lost.
  - **Scene C ("Large Map - Late-Game Siege")**: Large map (~16384x16384 elmos). Starts with ~50 friendly units and ~40 enemies. Diverse unit types. High event density. Economy near storage caps. Units cluster around attack/defense positions.
- **FR-005**: Each scene MUST include a pre-populated UnitDefCache with realistic unit definitions (name, cost, build speed, weapon range, build options) for all DefIds referenced by units and enemies in that scene.
- **FR-006**: Unit positions MUST remain within the map's coordinate bounds (0 to map width on X, 0 to map height on Z; Y represents terrain height and should vary plausibly between 0 and 400).
- **FR-007**: Economy snapshots MUST be internally consistent: Current >= 0, Current <= Storage, Income >= 0, Usage >= 0, and frame-to-frame Current changes must approximate (Income - Usage) scaled by frame time.
- **FR-008**: The Init event MUST appear in frame 1's GameFrame events. Update events MUST appear in every frame's GameFrame events.
- **FR-009**: Unit lifecycle events MUST be consistent: UnitCreated before UnitFinished, UnitFinished before UnitIdle, and UnitDestroyed removes the unit from subsequent frames.
- **FR-010**: Enemy visibility events MUST follow valid state transitions: EnemyEnterRadar/EnemyEnterLOS before EnemyLeaveRadar/EnemyLeaveLOS, and visibility flags on TrackedEnemy must match the most recent visibility event.

### Key Entities

- **GameState**: The per-frame snapshot -- contains all unit maps, economy, events, and frame number.
- **TrackedUnit**: A friendly unit with id, def id, 3D position, health, max health, finished/idle flags.
- **TrackedEnemy**: An enemy unit with id, optional def id, 3D position, optional health, LOS/radar flags.
- **EconomySnapshot**: Metal or energy resource state (current, income, usage, storage).
- **UnitDefCache / UnitDefInfo**: Cached unit type definitions (name, cost, build speed, weapon range, build options).
- **GameEvent**: Discriminated union of 28 event types covering unit lifecycle, combat, economy, and signals.
- **GameFrame**: A frame number paired with its list of GameEvents.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All three scenes generate exactly 300 frames each with zero validation errors (no missing units, no out-of-bounds positions, no inconsistent economy values).
- **SC-002**: A visualization consumer can load any scene and render smooth unit movement with no visible teleportation artifacts (position deltas <= 6 elmos/frame per axis).
- **SC-003**: Each scene contains at least 5 distinct unit types (DefIds) in the UnitDefCache.
- **SC-004**: Scene B contains at least 20 combat events (UnitDamaged, EnemyDamaged, WeaponFired) across its 300 frames.
- **SC-005**: Each scene can be generated in under 1 second on a standard developer machine (no engine dependency, no network calls).
- **SC-006**: Generated data is directly consumable as FSBar.Client typed values -- no deserialization or conversion step required.

## Assumptions

- BAR game simulation runs at 30 frames per second, so 300 frames = 10 seconds of game time.
- Unit movement speeds in BAR range from ~1 elmo/frame (slow units) to ~5 elmos/frame (fast scouts), with 6 elmos/frame as the upper bound.
- Map coordinate system: X and Z are horizontal (map width/height), Y is vertical (terrain height). Origin is at (0, 0, 0).
- Unit DefIds and names used in synthetic data are fictional but structurally realistic (e.g., "arm_commander", "arm_solar", "arm_peewee", "arm_flash", "cor_gator") to match BAR naming conventions.
- The synthetic data generator is a pure function (no side effects, no engine connection). It exists solely as a test/development utility.
- Economy storage values are fixed per scene (e.g., starting at 1000 metal/energy storage, increasing if storage buildings are constructed).
