namespace FSBar.Viz

open System
open SkiaSharp
open Silk.NET.Maths

/// Configuration for the visualization viewer window.
type ViewerConfig =
    { Title: string
      Width: int
      Height: int
      TargetFps: int
      ClearColor: SKColor
      OnRender: SKCanvas -> Vector2D<int> -> unit
      OnResize: int -> int -> unit
      OnKeyDown: Silk.NET.Input.Key -> unit
      OnMouseScroll: float32 -> float32 -> float32 -> unit
      OnMouseDrag: float32 -> float32 -> unit }

/// Manages a Silk.NET window on a background thread with SkiaSharp GPU rendering.
module Viewer =
    /// Start the viewer window on a background thread. Returns IDisposable to stop it.
    val run: config: ViewerConfig -> IDisposable
