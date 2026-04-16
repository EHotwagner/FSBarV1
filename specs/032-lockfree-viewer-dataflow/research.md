# Research: 032-lockfree-viewer-dataflow

**Date**: 2026-04-16

## R1: Current Lock Contention Architecture

### Findings

The `stateLock` (GameViz.fs:105) is a single `obj()` monitor lock that
serializes **all** mutable state access in GameViz. It is acquired in:

- `onFrameWithState` (line 412) — bot thread, holds for O(n) unit map
  rebuild + event processing + snapshot construction
- `handleInput` FrameTick (line 331) — render thread, holds for
  `emitScene()` (interpolation + scene build trigger)
- `handleInput` other events (lines 246–330) — render thread, holds
  for scroll/pan/drag/hotkeys
- `start`, `stop`, `attachToClient`, `attachWithState`, `seedUnits`,
  `onFrame`, config setters — various threads, infrequent

The hot-path contention is between `onFrameWithState` (bot thread,
up to 150/sec with macro bot) and FrameTick (render thread, 60/sec).
Both hold the lock for milliseconds, creating bidirectional blocking.

### Decision

Remove `stateLock` from the frame-processing and scene-emission hot path
entirely. Use atomic reference swap for state publication.

### Alternatives Considered

1. **Rx Observable pipeline** — rejected (see spec Problem Analysis).
   Adds dependency, doesn't eliminate lock.
2. **Fine-grained locks** (per-field or read-write locks) — rejected.
   More complex, still has contention under heavy load, harder to reason
   about correctness.
3. **Lock-free concurrent queue** — rejected. Adds backpressure
   complexity for a latest-value-wins pattern. Overkill.
4. **Publish-sample with atomic reference** — chosen. Simplest correct
   solution. Zero contention. Well-understood pattern.

---

## R2: GameSnapshot Immutability

### Findings

`GameSnapshot` (VizTypes.fs:198–207) is an F# record — **structurally
immutable by default**. All fields are value types or immutable
collections (`Map<K,V>`, `list`, `array`). No mutable fields.

`GameState` from FSBar.Client is the raw input. It contains unit lists
and economy data. The bot creates a new `GameState` on each protocol
frame — it does not mutate previously published instances.

### Decision

`GameSnapshot` can be safely shared across threads without defensive
copying. An atomic reference swap of a `GameSnapshot option` is
sufficient for thread-safe publication.

---

## R3: Render Thread Work Budget

### Findings

At 60 fps, the render thread has ~16.6ms per frame. Current work:
- Position interpolation: O(n) lerp over ~200-500 units ≈ <0.1ms
- `SceneBuilder.buildScene`: builds Scene tree from snapshot — measured
  at <2ms with 8 overlays in Vulkan mode
- GPU submit (Vulkan 4x MSAA): <1ms for typical scenes

Total: ~3ms per frame, leaving ~13ms headroom.

New work to add (only when a new snapshot arrives, ~5/sec):
- Rebuild `units: Map<int, UnitState>` from GameState — O(n), ~0.1ms
- Compute EventIndicators from unit diff — O(n), ~0.05ms
- Build `DisplayUnits: Map<int, UnitDisplay>` with DefProps — O(n),
  ~0.2ms (ConcurrentDictionary cache hit)
- Prune expired indicators — O(k), negligible

Total additional: ~0.35ms, only 5x/sec = amortized ~0.03ms/frame.

### Decision

The render thread has ample budget for derived-state reconstruction.
No optimization needed.

---

## R4: Config and ViewState Threading

### Findings

`VizConfig` and `ViewState` are modified by:
- Hotkey toggles (render thread, in FrameTick handler)
- Public API calls like `setConfig`, `toggleOverlay` (any thread)

These are infrequent (user-driven, <10/sec). Current approach: all
under `stateLock`.

### Decision

Use a separate lightweight lock (`configLock`) for config/view state
mutations. This keeps config changes thread-safe without interfering
with the hot-path state publication. Alternatively, since config and
viewState are small records, they could also use atomic reference swap.

---

## R5: SceneBuilder Mutable State

### Findings

`SceneBuilder` has module-level mutable state:
- `pulsePhase`, `pulseElapsedSeconds` (lines 16-21) — pulse animation
  clock, updated in `updatePulsePhase` called from FrameTick
- `prevMetalDisplay`, `prevEnergyDisplay` (lines 10-11) — economy bar
  smoothing, updated during `buildScene`

All mutations happen exclusively on the render thread (FrameTick
handler). No cross-thread access.

### Decision

No change needed. SceneBuilder's mutable state is render-thread-local
and does not participate in the lock contention problem.

---

## R6: .fsi and Baseline Impact

### Findings

The public API surface of `GameViz.fsi` includes `onFrameWithState`
with signature:
```
val onFrameWithState: gameState: GameState -> mapGrid: MapGrid -> unit
```

The function signature does not change — only its internal
implementation (from lock-and-rebuild to atomic-swap). No .fsi change
needed for this function.

The `GameViz.baseline` is out of date — it doesn't include
`attachWithState`, `onFrameWithState`, `seedUnits`, or `screenshot`
which are present in the .fsi. This is a pre-existing issue.

### Decision

- No .fsi signature changes required for existing public functions
- Update `GameViz.baseline` to match current .fsi (pre-existing debt)
- No new public API surface needed — the architectural change is
  internal to the module
