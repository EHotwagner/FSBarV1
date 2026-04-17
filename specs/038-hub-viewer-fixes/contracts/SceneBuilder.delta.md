# `SceneBuilder` — surface-area delta (feature 038)

`SceneBuilder.fs(i)` is the live-path scene constructor shared by both
`GameViz` and the Hub's `ViewerTab`. This delta lists the exact
signature additions needed to populate `GameSnapshot.DisplayUnits`
with correct glyph data.

## Existing signature (pre-038)

```fsharp
val buildSceneHeadlessView:
    state: FSBar.Client.GameState ->
    map: FSBar.Client.MapGrid option ->
    metalSpots: (float32 * float32 * float32 * float32) array ->
    config: VizConfig ->
    viewState: ViewState ->
        Scene
```

## New signature (post-038)

```fsharp
val buildSceneHeadlessView:
    state: FSBar.Client.GameState ->
    map: FSBar.Client.MapGrid option ->
    metalSpots: (float32 * float32 * float32 * float32) array ->
    defCache: FSBar.Client.UnitDefCache option ->   // NEW
    config: VizConfig ->
    viewState: ViewState ->
        Scene
```

`defCache` is threaded through to `gameStateToSnapshotWith`. When
`Some`, `GameSnapshot.DisplayUnits` is populated via
`UnitDisplayAdapter.ofTrackedUnit` / `ofTrackedEnemy`. When `None`,
the legacy placeholder path is used (tests, preview sessions, any
caller without a BarClient).

Identical delta for `buildSceneHeadlessSized`:

```fsharp
val buildSceneHeadlessSized:
    state: FSBar.Client.GameState ->
    map: FSBar.Client.MapGrid option ->
    metalSpots: (float32 * float32 * float32 * float32) array ->
    defCache: FSBar.Client.UnitDefCache option ->   // NEW
    config: VizConfig ->
    viewportWidth: int ->
    viewportHeight: int ->
        Scene
```

`buildSceneHeadless` keeps its existing (cache-less) signature as a
compatibility shim — call sites that don't have a cache keep working.

## `gameStateToSnapshotWith` — promoted to public?

Currently module-private (`.fsi` does not expose it). Feature 038 keeps
it private — the `defCache`-aware `buildScene*` entry points are the
only public surface.

## Surface-area baseline

Regenerate `tests/FSBar.Viz.Tests/Baselines/SceneBuilder.baseline` with
`SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Viz.Tests/` after the
.fsi edit lands.
