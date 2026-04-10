# Research: Synthetic Visualization Test Data

## R-001: Where should the synthetic data generator live?

**Decision**: New project `src/FSBar.SyntheticData/` with corresponding `src/FSBar.SyntheticData.Tests/`.

**Rationale**: The generator produces FSBar.Client types but has no dependency on the engine, networking, or protobuf layers. A separate project keeps the dependency graph clean — it references FSBar.Client for types but doesn't pull in FSBar.Proto or engine-specific code. This also allows the generator to be packed as a NuGet package for use in other test/viz projects.

**Alternatives considered**:
- Putting it in FSBar.Client.Tests — rejected because it's not test code, it's a reusable data utility.
- Putting it in FSBar.Client — rejected because it would add dead code to the production library.
- A standalone script — rejected because it needs to produce typed F# values, not serialized data.

## R-002: How to generate realistic unit movement?

**Decision**: Use simple linear interpolation with waypoint targets. Each unit has a current position and a target position. Each frame, the unit moves toward the target at its speed (1-5 elmos/frame depending on unit type). When the target is reached, a new random waypoint within map bounds is chosen.

**Rationale**: This produces smooth, bounded movement without requiring pathfinding or terrain awareness. The movement is visually plausible for visualization testing purposes.

**Alternatives considered**:
- Full pathfinding — rejected as massive over-engineering for synthetic test data.
- Random walk — rejected because it produces unrealistic jittery movement.

## R-003: How to generate realistic economy curves?

**Decision**: Model economy as a simple simulation: income = sum of economy building outputs, usage = sum of factory/constructor drain. Current is updated each frame as `clamp(0, current + (income - usage) / 30, storage)`. Income and usage values shift as buildings are constructed.

**Rationale**: This matches how the real BAR economy works at a coarse level and produces smooth, realistic curves without needing the actual engine simulation.

**Alternatives considered**:
- Static economy values — rejected because economy is a key visualization element and needs temporal variation.
- Random fluctuations — rejected because real economy follows deterministic patterns.

## R-004: How to model enemy visibility transitions?

**Decision**: Each enemy has a visibility state machine: NotVisible → InRadar → InLOS → InRadar → NotVisible. Transitions are triggered at predetermined frame numbers per enemy, generating the corresponding GameEvent. The TrackedEnemy fields (InLOS, InRadar, Health, DefId) are updated to match.

**Rationale**: This ensures event/state consistency, which is a core requirement (FR-010). The state machine prevents invalid transitions.

**Alternatives considered**:
- Random visibility toggling — rejected because it risks invalid state transitions.

## R-005: How to structure the scene definitions?

**Decision**: Each scene is defined as a declarative "scenario script" — a list of scheduled actions (spawn unit at frame N, begin combat at frame M, etc.) that the generator executes frame by frame. The generator maintains mutable state internally and outputs an immutable `GameState array` and `GameFrame array`.

**Rationale**: Declarative scene definitions are easy to read, modify, and extend. They separate "what happens" from "how state is updated."

**Alternatives considered**:
- Fully procedural generation — rejected because it's harder to control and reproduce specific scenarios.
- Recorded/replayed real game data — rejected because it requires engine access and doesn't satisfy the "no engine dependency" requirement.

## R-006: UnitDefCache population

**Decision**: Each scene pre-defines a static set of UnitDefInfo records with realistic BAR-like values. DefIds are sequential starting from 1. Names follow BAR conventions (faction_unitname). Cost, build speed, weapon range, and build options are plausible values based on BAR unit archetypes.

**Rationale**: The UnitDefCache must be complete before frame generation begins so that all DefId references are valid.

**Alternatives considered**:
- Loading real unit defs from BarData — rejected because it introduces a runtime dependency and the synthetic data should be self-contained.
