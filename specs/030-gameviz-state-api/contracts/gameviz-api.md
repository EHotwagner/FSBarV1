# Contract: GameViz Public API (Feature 030)

**Module**: `FSBar.Viz.GameViz`  
**Signature File**: `src/FSBar.Viz/GameViz.fsi`

## New Functions

### `attachWithState`

```fsharp
val attachWithState:
    mapGrid: MapGrid ->
    metalSpots: (float32 * float32 * float32 * float32) array ->
    teamId: int ->
        unit
```

**Preconditions**:
- `GameViz.start` must have been called first (viewer window open).
- `mapGrid` should have non-zero dimensions for meaningful terrain rendering.

**Postconditions**:
- `mapGridRef`, `metalSpots`, and `myTeamId` are populated.
- `clientRef` remains `None` — no socket reference stored.
- Auto-fit viewport to map dimensions.
- Viewer is immediately render-ready.

**Thread safety**: Acquires `stateLock`.

### `onFrameWithState`

```fsharp
val onFrameWithState:
    gameState: GameState ->
    mapGrid: MapGrid ->
        unit
```

**Preconditions**:
- `GameViz.start` must have been called.
- `attachWithState` should have been called (otherwise map renders empty).

**Postconditions**:
- `units` map rebuilt from `gameState.Units` + `gameState.Enemies`.
- `defPropsCache` populated for all encountered DefIds (via BarData, no socket).
- `unfinishedUnits` updated from `TrackedUnit.IsFinished` status.
- Event indicators created from `gameState.Events` (destruction, damage, creation).
- Expired indicators pruned.
- Economy data derived from `gameState.Metal` and `gameState.Energy`.
- `snapshot` updated with new `GameSnapshot`.
- Scene emitted on next `FrameTick`.

**Socket reads**: Zero. All data sourced from function parameters.

**Thread safety**: Acquires `stateLock`.

## Existing Functions (Unchanged)

All existing `GameViz` functions retain their current signatures and behavior:
- `start`, `stop`, `attachToClient`, `seedUnits`, `onFrame`
- `setDisconnected`, `resetView`
- `setBaseLayer`, `toggleOverlay`, `enableOverlay`, `disableOverlay`
- `setConfig`, `updateConfig`, `setColorScheme`, `setMarkerSize`, `setOverlayOpacity`
- `toggleGridLines`, `pan`, `zoom`, `screenshot`

## Surface Area Baseline Impact

The `.fsi` gains two new `val` declarations. The surface-area baseline test
must be updated to include the new functions.
