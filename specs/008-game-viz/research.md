# Research: Game State Visualization

**Feature**: 008-game-viz | **Date**: 2026-04-06

## R1: Rendering Stack Selection

**Decision**: Silk.NET 2.22.0 (windowing + OpenGL) + SkiaSharp 2.88.6 (GPU rendering)

**Rationale**: This is the proven stack from GameVizCurrent, already validated with performance benchmarks (60fps with 2000 objects on integrated AMD GPU). Cross-platform via Silk.NET abstraction. SkiaSharp provides high-quality 2D rendering with GPU acceleration via OpenGL backend.

**Alternatives considered**:
- Avalonia UI: Heavier framework, unnecessary for a headless-friendly visualization window. Adds significant dependency weight.
- Terminal-based (Spectre.Console): Cannot render 2D map bitmaps with sufficient fidelity or performance.
- Raw OpenGL via Silk.NET: Too low-level for 2D rendering; would need to implement text, shapes, gradients manually.

## R2: Input Handling on Linux

**Decision**: Silk.NET.Input 2.22.0 for keyboard and mouse events

**Rationale**: Silk.NET.Input integrates with the Silk.NET windowing system and provides cross-platform keyboard/mouse abstractions. On Linux, it uses X11 or Wayland input backends automatically. No additional dependencies needed.

**Alternatives considered**:
- Raw X11/Wayland input: Unnecessary complexity; Silk.NET already abstracts this.
- Separate input library (SDL2): Would conflict with Silk.NET's event loop.

## R3: Map Layer Bitmap Rendering Performance

**Decision**: Render Array2D → SKBitmap using `SKBitmap.SetPixel` or `LockPixels` + direct memory write for large grids. Cache bitmaps; invalidate only on data change.

**Rationale**: A 512x512 heightmap grid = 262,144 pixels. At 4 bytes/pixel (RGBA), this is ~1MB per layer bitmap. `LockPixels` + `Marshal.Copy` can fill this in <1ms. Caching avoids re-rendering static layers (height, slope, resource) every frame. Dynamic layers (LOS, radar) are re-rendered per frame but are typically at lower resolution.

**Alternatives considered**:
- Per-pixel SKCanvas.DrawRect: Too many draw calls for large grids (262K rects). Measured at ~50ms for 512x512 in similar projects.
- SKImage from pixel array: Similar performance to SKBitmap but less mutable; harder to update incrementally.

## R4: Thread Safety Pattern

**Decision**: Lock-guarded mutable state (same as GameVizCurrent Prototype), with atomic snapshot reads.

**Rationale**: The GameVizCurrent pattern uses `lock obj (fun () -> ...)` for thread-safe reads and writes. The game thread updates the `GameSnapshot` (map layers, unit positions, economy). The render thread reads a snapshot each frame. This is simple, proven, and sufficient for the low-contention scenario (one writer, one reader, updates at game-frame rate).

**Alternatives considered**:
- Immutable snapshots with `Interlocked.Exchange`: Slightly lower contention but more allocation pressure. Not needed at this scale.
- MailboxProcessor (as in v2 Scripting.fs): More complex; better suited for command dispatch than data sharing. We use it for commands, not for game state.

## R5: New Dependency Justification

| Dependency | Version | Need | Maintenance |
|-----------|---------|------|-------------|
| Silk.NET.Windowing | 2.22.0 | Cross-platform OpenGL window hosting on background thread | Active (.NET Foundation project), pinned |
| Silk.NET.OpenGL | 2.22.0 | OpenGL context for SkiaSharp GPU surface | Same as above |
| Silk.NET.Input | 2.22.0 | Keyboard/mouse input for layer switching, pan/zoom | Same as above |
| SkiaSharp | 2.88.6 | GPU-accelerated 2D rendering (bitmaps, shapes, text) | Active (Microsoft-maintained), pinned |

All four are established, well-maintained .NET libraries already validated in GameVizCurrent.
