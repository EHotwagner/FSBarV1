# Public API Contracts: FSBar.Viz

**Branch**: `019-revamp-viz-library` | **Date**: 2026-04-10

## Module Signatures (.fsi contracts)

### VizTypes.fsi

All type definitions are unchanged from the previous version. The public surface includes:

- `LayerKind` (DU with RequireQualifiedAccess)
- `OverlayKind` (DU with RequireQualifiedAccess)
- `EventKind` (DU with RequireQualifiedAccess)
- `ColorScheme` (record)
- `ViewState` (record)
- `VizConfig` (record)
- `UnitState` (record)
- `EventIndicator` (record)
- `EconomyData` (record)
- `GameSnapshot` (record)
- `VizCommand` (DU with RequireQualifiedAccess)
- `VizDefaults` module (defaultViewState, defaultEconomy, defaultConfig)

### ColorMaps.fsi

```fsharp
module FSBar.Viz.ColorMaps

val grayscale: ColorScheme
val terrain: ColorScheme
val heatMap: ColorScheme
val binary: ColorScheme
val colorSchemeFor: LayerKind -> ColorScheme
```

### LayerRenderer.fsi

```fsharp
module FSBar.Viz.LayerRenderer

val renderLayer: MapGrid -> LayerKind -> ColorScheme -> SkiaSharp.SKBitmap
val invalidateCache: LayerKind -> unit
val invalidateAll: unit -> unit
val cacheStats: unit -> int * int
```

### SceneBuilder.fsi

**Changed**: Returns `Scene` instead of drawing to `SKCanvas`.

```fsharp
module FSBar.Viz.SceneBuilder

open SkiaViewer

val buildScene: snapshot:GameSnapshot -> config:VizConfig -> viewState:ViewState -> Scene
```

Key change: `drawFrame: SKCanvas -> GameSnapshot -> VizConfig -> ViewState -> unit` becomes `buildScene: GameSnapshot -> VizConfig -> ViewState -> Scene`. The caller no longer provides a canvas — the function produces a declarative scene tree.

### MapData.fsi

```fsharp
module FSBar.Viz.MapData

val save: path:string -> MapGrid -> (float32*float32*float32*float32) array -> unit
val load: path:string -> MapGrid * (float32*float32*float32*float32) array
```

Unchanged.

### MockSnapshot.fsi

```fsharp
module FSBar.Viz.MockSnapshot

val emptySnapshot: MapGrid -> GameSnapshot
val withUnits: UnitState list -> GameSnapshot -> GameSnapshot
val withFriendlyAt: (float32*float32*float32) -> GameSnapshot -> GameSnapshot
val withEnemyAt: (float32*float32*float32) -> GameSnapshot -> GameSnapshot
val withEvent: EventKind -> (float32*float32*float32) -> int -> GameSnapshot -> GameSnapshot
val withEconomy: float32 -> float32 -> float32 -> float32 -> GameSnapshot -> GameSnapshot
val withEnergyEconomy: float32 -> float32 -> float32 -> float32 -> GameSnapshot -> GameSnapshot
val withMetalSpots: (float32*float32*float32*float32) array -> GameSnapshot -> GameSnapshot
val withFrame: int -> GameSnapshot -> GameSnapshot
```

Unchanged.

### PreviewSession.fsi

```fsharp
module FSBar.Viz.PreviewSession

val startWithMap: MapGrid -> System.IDisposable
val startWithSnapshot: GameSnapshot -> System.IDisposable
val startPlayback: GameSnapshot seq -> gameFps:int -> System.IDisposable
val stop: unit -> unit
```

Unchanged public API. Internal implementation changes to use `Viewer.run` with `IObservable<Scene>`.

### GameViz.fsi

```fsharp
module FSBar.Viz.GameViz

val start: VizConfig option -> unit
val stop: unit -> unit
val attachToClient: BarClient -> unit
val seedUnits: UnitState list -> unit
val onFrame: GameFrame -> unit
val setDisconnected: unit -> unit
val resetView: unit -> unit
val setBaseLayer: LayerKind -> unit
val toggleOverlay: OverlayKind -> unit
val enableOverlay: OverlayKind -> unit
val disableOverlay: OverlayKind -> unit
val setConfig: VizConfig -> unit
val updateConfig: (VizConfig -> VizConfig) -> unit
val setColorScheme: LayerKind -> ColorScheme -> unit
val setMarkerSize: float32 -> unit
val setOverlayOpacity: float32 -> unit
val toggleGridLines: unit -> unit
val pan: float32 -> float32 -> unit
val zoom: float32 -> float32 -> float32 -> unit
val screenshot: string -> Result<string, string>
```

Unchanged public API. Internal implementation changes to emit Scene via observable instead of rendering in OnRender callback.

### LiveSession.fsi

```fsharp
module FSBar.Viz.LiveSession

[<Sealed>]
type LiveSessionHandle =
    interface System.IDisposable
    member FrameCount: int
    member IsRunning: bool
    member LastError: string option

val start: EngineConfig -> VizConfig option -> LiveSessionHandle
val startWithClient: BarClient -> VizConfig option -> LiveSessionHandle
```

Unchanged.

## Breaking Changes Summary

| Module | Change | Impact |
|--------|--------|--------|
| SceneBuilder | `drawFrame` → `buildScene`, returns `Scene` instead of taking `SKCanvas` | Any code calling `drawFrame` directly must update. Tests that inspect canvas output must inspect Scene tree instead. |
| PreviewSession | Internal only | No API change |
| GameViz | Internal only | No API change |
| LiveSession | Internal only | No API change |

Only SceneBuilder has a public API signature change. All other modules maintain identical signatures.
