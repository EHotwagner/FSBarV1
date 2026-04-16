# Feature Specification: Lockfree Viewer Dataflow

**Feature Branch**: `032-lockfree-viewer-dataflow`  
**Created**: 2026-04-16  
**Status**: Draft  
**Input**: User description: "Analyse the viewer performance problem from 031 diagnosis. Can Rx Observable help? Is there a simple elegant solution refactoring the whole dataflow structure (push/pull/backpressure)?"

## Problem Analysis

The 031 viewer performance diagnosis identified a catastrophic throughput
collapse when the GameViz viewer runs alongside the macro bot. The root cause
is a shared `stateLock` that serializes bot-thread state mutations and
render-thread scene building, creating bidirectional contention:

- **Bot thread**: Calls `onFrameWithState` up to 150x/sec (macro bot with
  per-frame protocol delivery), each call holding the lock for milliseconds
  while rebuilding unit maps, processing events, and constructing snapshots.
- **Render thread**: Acquires the same lock 60x/sec to run `emitScene`
  (interpolation + scene building with 8 overlays).
- **Result**: Both threads spend most of their time waiting on each other.
  Game simulation drops from ~150 game-fps to <1 game-fps.

The current mitigation (frame-skip throttle of 30 in `bot_macro.fsx`) reduces
the symptom but doesn't fix the architecture. The lock still blocks both
threads during every state update.

### Why Rx Observable Is Not the Right Fit

Rx (System.Reactive) solves a different problem: composing and transforming
asynchronous event streams with operators like `Throttle`, `Buffer`,
`ObserveOn`. The viewer's problem is not event composition -- it's **shared
mutable state under a contention-prone lock**. Rx would add:

- A new dependency (System.Reactive NuGet) against the project's "no new
  NuGet dependencies" convention.
- Scheduler complexity (which thread observes? how to backpressure without
  losing frames?).
- No actual elimination of the lock -- the observer still needs to read
  coherent state, which means either copying or locking.

The real solution is simpler: **eliminate the shared lock entirely** by making
the bot thread publish immutable state snapshots via an atomic reference swap,
and having the render thread independently sample the latest snapshot.

### The Elegant Solution: Publish-Sample Pattern

The entire dataflow reduces to three lines of logic:

1. **Bot thread publishes**: Atomically swap a reference to the latest
   immutable snapshot. O(1), no lock, no blocking.
2. **Render thread samples**: Read the current reference on each frame tick.
   O(1), no lock, no blocking.
3. **Render thread derives**: Build interpolated positions, display units,
   and scene graph from the sampled snapshot. All derived work happens on the
   render thread's own time budget.

This is the classic "single-producer single-consumer latest-value" pattern.
No queues, no backpressure, no Rx operators needed. The render thread always
sees the most recent state. If the bot publishes 150 snapshots/sec and the
render thread samples at 60 fps, ~90 snapshots are silently skipped -- which
is exactly the correct behavior for a real-time visualization.

## User Scenarios & Testing

### User Story 1 - Viewer Runs Without Slowing the Bot (Priority: P1)

A developer runs the macro bot trainer with `--full-viz` enabled. The bot
completes its game at full simulation speed (~140+ game-fps at 5x speed)
while the viewer renders all 8 overlays at a smooth 60 fps. The bot's
performance is indistinguishable from running without a viewer.

**Why this priority**: This is the core problem. Without this, the viewer
is unusable during training -- it turns a 2.5-minute game into an hours-long
crawl.

**Independent Test**: Run `bot_macro.fsx` with `--full-viz` and measure game
completion time. Compare against the no-viewer baseline (~2.5 minutes at 5x
speed). The viewer-attached run must complete within 10% of the baseline.

**Acceptance Scenarios**:

1. **Given** the macro bot is running at speed level 2 (5x) with the viewer
   active and all overlays enabled, **When** the game completes, **Then** the
   total game frames and wall-clock time are within 10% of the no-viewer run.
2. **Given** the macro bot is running with the viewer active, **When** the bot
   thread publishes state, **Then** no blocking wait occurs on the bot thread
   due to viewer rendering.

---

### User Story 2 - Smooth Visual Playback (Priority: P2)

A developer watches the viewer during a training run. Unit movement appears
smooth and continuous at 60 fps, even though game state updates arrive at
~5 per second (simple bot) or are published at variable rates (macro bot).

**Why this priority**: Visual smoothness is the viewer's reason for existing.
Without interpolation working correctly in the new architecture, the viewer
is jarring and hard to read.

**Independent Test**: Run either bot with the viewer and visually confirm
smooth unit movement. The perf counter overlay should show ~60 render fps
and the state update rate matching the bot's natural cadence.

**Acceptance Scenarios**:

1. **Given** the viewer is rendering with state arriving at 5 updates/sec,
   **When** the render thread draws a frame between updates, **Then** unit
   positions are interpolated smoothly from the previous to the current state.
2. **Given** the viewer is rendering, **When** the state update rate changes
   (e.g., game speed change or bot switch), **Then** interpolation adapts
   without visual glitches.

---

### User Story 3 - Input Responsiveness (Priority: P2)

A developer interacts with the viewer (pan, scroll, zoom, hotkey toggles)
during a high-speed training run. Inputs are processed immediately without
waiting for the bot thread to release a lock.

**Why this priority**: Lock contention currently blocks input handling too,
making the viewer feel frozen during heavy state updates.

**Independent Test**: During a `--full-viz` macro bot run, rapidly pan and
zoom. Inputs should respond within one render frame (~16ms).

**Acceptance Scenarios**:

1. **Given** the viewer is running alongside the macro bot, **When** the user
   pans or scrolls, **Then** the viewport updates on the next render frame
   without waiting for any bot-thread operation.

---

### Edge Cases

- What happens when the bot produces no state updates for an extended period
  (e.g., engine stall or disconnect)? The viewer should continue rendering
  the last known state without errors.
- What happens when the bot publishes state faster than the render thread can
  consume it? Intermediate snapshots are silently dropped -- the render thread
  always reads the latest.
- What happens during viewer startup before the first state is published? The
  viewer should render an empty or placeholder scene gracefully.
- What happens if the bot thread and render thread race on the atomic
  reference? Both operations are wait-free; the render thread simply reads
  whichever snapshot is current at that instant.

## Requirements

### Functional Requirements

- **FR-001**: The bot thread MUST publish game state without acquiring any
  shared lock or blocking on the render thread.
- **FR-002**: The render thread MUST read the latest published game state
  without acquiring any shared lock or blocking on the bot thread.
- **FR-003**: State transfer between threads MUST use an atomic reference swap
  to guarantee memory safety without locks.
- **FR-004**: The render thread MUST perform position interpolation from its
  own copy of previous and current state, without requiring bot-thread
  coordination.
- **FR-005**: `onFrameWithState` MUST complete in O(1) time relative to the
  number of units -- it should store the raw state reference, not rebuild
  derived data structures.
- **FR-006**: Scene building (overlay rendering, unit display construction,
  economy HUD) MUST happen entirely on the render thread, reading from the
  latest published snapshot.
- **FR-007**: The existing `stateLock` MUST be removed from the hot path
  (frame processing and scene emission). It MAY be retained for infrequent
  operations like config changes or overlay toggles if needed.
- **FR-008**: The existing frame-skip throttle in `bot_macro.fsx` SHOULD
  become unnecessary and MAY be removed or relaxed, since the bot thread no
  longer blocks on viewer operations.
- **FR-009**: All existing viewer features (8 overlays, perf counter, economy
  HUD, hotkey toggles, position interpolation) MUST continue to function
  identically.
- **FR-010**: No new external dependencies MUST be introduced.

### Key Entities

- **GameSnapshot**: An immutable record capturing the complete renderable
  state at a point in time (unit positions, economy, events, indicators).
  Published atomically by the bot thread, consumed by the render thread.
- **Atomic State Reference**: A single mutable reference cell holding the
  latest `GameSnapshot`. Written by the bot thread via atomic swap, read by
  the render thread on each frame tick.
- **Render-Local State**: Per-frame derived data (interpolated positions,
  display units, scene graph) owned exclusively by the render thread. Never
  shared with the bot thread.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Macro bot game completion time with viewer active is within 10%
  of the no-viewer baseline (currently ~2.5 minutes at 5x speed).
- **SC-002**: Viewer renders at a sustained 60 fps with all 8 overlays active
  during a full macro bot training run.
- **SC-003**: Bot thread spends zero time blocked on viewer operations (no
  lock acquisition, no synchronization wait on the frame-processing path).
- **SC-004**: The state-publishing operation completes in under 100
  microseconds regardless of unit count.
- **SC-005**: All existing viewer features (overlays, interpolation, hotkeys,
  perf counter, economy HUD) pass visual regression testing against the
  current implementation.

## Assumptions

- The existing `GameSnapshot` record (or a minor evolution of it) contains
  all data the render thread needs to build a complete scene. No additional
  engine callbacks or bot-thread queries are required during rendering.
- The render thread has sufficient CPU budget at 60 fps to rebuild derived
  display data (unit maps, display units) from a raw snapshot each frame.
  At typical unit counts (<500) this is well within budget.
- The bot helper (`viewer.fsx`) can be adapted to the new publish pattern
  without changing its public API surface -- callers still invoke
  `viewerOnFrame` with the same arguments.
- Position interpolation logic can be moved to the render thread without
  loss of visual quality, since it only needs the previous and current
  snapshot plus a wall-clock timer.
- The `GameState` passed by the bot is either already immutable or can be
  treated as effectively immutable once published (the bot creates a new
  one each frame, it does not mutate a previously published reference).
