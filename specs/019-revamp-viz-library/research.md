# Research: Revamp Viz Library with Declarative SkiaViewer

**Branch**: `019-revamp-viz-library` | **Date**: 2026-04-10

## R1: SkiaViewer Declarative Scene API Migration

**Decision**: Migrate from imperative `OnRender: SKCanvas -> Vector2D<int> -> unit` callbacks to declarative `IObservable<Scene>` emission.

**Rationale**: The revamped SkiaViewer (v1.1.3) removes the callback-based API entirely. The new API takes `IObservable<Scene>` and returns `ViewerHandle * IObservable<InputEvent>`. This is a forced migration — the old API no longer exists.

**Key API surface**:
- `Viewer.run: ViewerConfig -> IObservable<Scene> -> ViewerHandle * IObservable<InputEvent>`
- Scene = `{ BackgroundColor: SKColor; Elements: Element list }`
- Element = Rect | Ellipse | Line | Text | Image | Path | Group | Points | Vertices | Arc | Picture | TextBlob
- InputEvent = KeyDown | KeyUp | MouseMove | MouseDown | MouseUp | MouseScroll | WindowResize | FrameTick

**Migration pattern**:
- Old: GameViz holds mutable state, SkiaViewer calls `OnRender` with canvas → GameViz calls `SceneBuilder.drawFrame` imperatively
- New: GameViz holds mutable state + an `Event<Scene>`. On state change, build Scene tree and trigger event. SkiaViewer subscribes to the observable.
- Input: Old used `OnKeyDown`/`OnMouseScroll`/`OnMouseDrag` callbacks. New subscribes to `IObservable<InputEvent>` returned by `Viewer.run`.

**Alternatives considered**:
- Wrapping old API in adapter: Not possible — old API removed from SkiaViewer
- Using SceneRenderer directly: Internal API, not part of public surface

## R2: Advanced SkiaSharp Visual Effects

**Decision**: Use SkiaViewer's Scene DSL shader/filter types for visual richness. No direct SkiaSharp API calls needed — the Scene type exposes all effects declaratively.

**Rationale**: The Scene API provides full access to shaders, filters, and effects through discriminated unions. This is both more maintainable and aligns with the declarative architecture.

**Effects to use**:

1. **Terrain layers — Shader.Image**: Render layer data to SKBitmap via LayerRenderer (existing approach), then wrap in `Element.Image` with optional shader overlay for visual richness.

2. **Unit markers — Shader.RadialGradient**: Friendly units get cyan-to-transparent radial gradient; enemies get red-to-transparent. Creates a soft glow effect around unit positions.

3. **Event animations — Shader.RadialGradient + ImageFilter.Blur**: Expanding rings use `Element.Ellipse` with increasing radius over time. Combat events use `MaskFilter.Blur` for glow. Alpha fades via `Paint.Opacity` decreasing over duration.

4. **Economy HUD — Shader.LinearGradient**: Bar gauges with linear gradient fill (green→yellow→red based on fill level). Background uses subtle Perlin noise shader for texture.

5. **Metal spots — Shader.RadialGradient**: Concentric gradient circles showing richness intensity.

**Alternatives considered**:
- Shader.RuntimeEffect (SkSL): Too complex for initial implementation; can be added later for custom effects
- Direct SKCanvas operations: Incompatible with declarative Scene model

## R3: Animation Timing Strategy

**Decision**: Use `InputEvent.FrameTick` elapsed seconds for animation timing. Animations are parameterized by normalized progress [0..1] computed from elapsed time.

**Rationale**: FrameTick provides precise wall-clock elapsed time independent of game frame rate. This decouples visual animations from game simulation speed.

**Pattern**:
```
EventIndicator has FrameCreated + DurationFrames (game frames)
→ Convert to wall-clock duration using game FPS
→ On each FrameTick, compute progress = elapsed / duration
→ progress drives: ring radius, opacity fade, glow intensity
→ When progress ≥ 1.0, remove indicator
```

**For playback**: Game frames advance based on wall-clock time × game FPS. Each game frame produces a new GameSnapshot. The Scene is rebuilt from the current snapshot + animation state on each FrameTick.

**Alternatives considered**:
- Game-frame-locked animations: Animations would stutter at low game FPS
- Fixed timer thread: Unnecessary complexity; FrameTick already fires at viewer's target FPS

## R4: Scene Tree Architecture

**Decision**: SceneBuilder produces a `Scene` (not draws to canvas). The scene tree is structured as nested Groups with transforms for viewport control.

**Rationale**: Declarative scene trees enable caching (SkiaViewer's CachedRenderer can skip unchanged subtrees), composability, and testability (inspect tree structure without rendering).

**Scene tree structure per frame**:
```
Scene
├── Group (viewport transform: translate + scale from ViewState)
│   ├── Element.Image (base layer bitmap from LayerRenderer)
│   ├── Group (grid overlay, if enabled)
│   │   └── Line elements for grid
│   ├── Group (metal spots overlay, if enabled)
│   │   └── Ellipse elements per spot with RadialGradient
│   ├── Group (unit overlay, if enabled)
│   │   └── Ellipse elements per unit with RadialGradient paint
│   └── Group (event overlay, if enabled)
│       └── Ellipse/Path elements per active indicator with animated paint
└── Group (HUD overlay, screen-space, no viewport transform)
    ├── Rect (HUD background with Perlin noise shader)
    ├── Rect (metal bar gauge with LinearGradient)
    ├── Rect (energy bar gauge with LinearGradient)
    ├── Text (metal values)
    └── Text (energy values)
```

**Alternatives considered**:
- Flat element list: No viewport transform grouping; harder to separate world-space from screen-space elements
- SKPicture recording: Pre-renders to bitmap, losing declarative benefits

## R5: Thread Safety and State Management

**Decision**: Maintain the existing pattern of mutable module-level state protected by `lock stateLock`. Scene emission happens under lock to ensure snapshot consistency.

**Rationale**: The existing pattern works well and is proven. The new addition is that instead of SkiaViewer calling back into GameViz (pull model), GameViz pushes Scene objects when state changes (push model via `Event<Scene>.Trigger`).

**State flow**:
1. External event (onFrame, user input, config change) acquires lock
2. Update mutable state (units, indicators, economy, viewState, config)
3. Build Scene tree from current state
4. Trigger `sceneEvent` (releases to SkiaViewer for rendering)
5. Release lock

**FrameTick handling**: The FrameTick event from SkiaViewer drives animation updates. On each tick:
1. Acquire lock
2. Update animation progress for active indicators
3. Prune expired indicators
4. Rebuild Scene tree
5. Trigger sceneEvent
6. Release lock

**Alternatives considered**:
- Immutable state with message passing (mailbox processor): More complex, no proven benefit for single-viewer scenario
- Lock-free with Interlocked: Too complex for composite state updates

## R6: MapData Binary Format Preservation

**Decision**: Keep the existing "FSMG" binary format unchanged. MapData module reimplemented with identical save/load behavior.

**Rationale**: Existing .mapdata files must remain loadable. The format is simple, proven, and has no issues.

**Alternatives considered**:
- Protobuf serialization: Would break existing files; no benefit for this use case
- JSON: Too large for heightmap data

## R7: Test Strategy with Synthetic Data

**Decision**: Create `SyntheticVizTests.fs` that generates all three scenes via `Scenes.generate`, converts GameState frames to GameSnapshot, renders through the full pipeline, and validates via screenshot comparison and element-presence checks.

**Rationale**: Synthetic data provides deterministic, reproducible test scenarios without requiring a running game engine. All three scenes cover different game phases (early/mid/late).

**Test structure**:
1. **Per-scene smoke test**: Generate scene, render frame 0/150/299, capture screenshots, verify no exceptions
2. **Layer switching test**: Render each LayerKind, verify bitmap dimensions and non-empty content
3. **Animation progression test**: Render 5 consecutive frames with events, verify element count/properties change
4. **Economy HUD test**: Render frames with different economy values, verify HUD elements present
5. **Playback loop test**: Start playback of 300 frames, verify frame counter advances and loops
6. **Full playback test**: Play all 300 frames of each scene without errors

**GameState → GameSnapshot conversion**: The test code must convert FSBar.Client.GameState (from SyntheticData) to FSBar.Viz.GameSnapshot. This requires mapping TrackedUnit → UnitState and extracting economy data. A helper function in the test project handles this.

**Alternatives considered**:
- Pixel-perfect baseline comparison: Too brittle with shader effects and animation timing
- Only testing SceneBuilder output (tree inspection): Misses rendering bugs
