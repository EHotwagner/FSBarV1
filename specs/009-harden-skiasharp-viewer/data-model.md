# Data Model: Harden SkiaSharp OpenGL Viewer

**Date**: 2026-04-06

## Entities

This feature does not introduce new persistent data entities. The viewer operates on transient in-memory state only.

### ViewerConfig (existing, unchanged)

Configuration record passed to `Viewer.run`. Immutable after creation.

| Field | Type | Description |
|-------|------|-------------|
| Title | string | Window title |
| Width | int | Initial window width |
| Height | int | Initial window height |
| TargetFps | int | Target frame rate |
| ClearColor | SKColor | Background clear color |
| OnRender | SKCanvas -> Vector2D<int> -> unit | Render callback |
| OnResize | int -> int -> unit | Resize callback |
| OnKeyDown | Key -> unit | Keyboard callback |
| OnMouseScroll | float32 -> float32 -> float32 -> unit | Scroll callback |
| OnMouseDrag | float32 -> float32 -> unit | Drag callback |

### Internal Viewer State (modified by hardening)

Mutable state managed within the `Viewer.run` closure. Not part of the public API.

| Field | Type | Change |
|-------|------|--------|
| surface | SKSurface | Existing — now accessed under lock |
| surfaceWidth | int | Existing — now accessed under lock |
| surfaceHeight | int | Existing — now accessed under lock |
| gl | GL | Existing — unchanged |
| texture | uint32 | Existing — unchanged |
| vao | uint32 | Existing — unchanged |
| vbo | uint32 | Existing — unchanged |
| shaderProgram | uint32 | Existing — unchanged |
| shutdownRequested | bool | **NEW** — cross-thread shutdown flag |
| windowCompleted | ManualResetEventSlim | **NEW** — completion signal |
| surfaceLock | obj | **NEW** — synchronization object |

### State Transitions

```
Created → Loading → Running → ShutdownRequested → Closing → Completed
                      ↑                                        |
                      └── (restart cycle) ─────────────────────┘
```

- **Created → Loading**: `Window.Create` called, thread started
- **Loading → Running**: `win.add_Load` fires, GL context + surface initialized
- **Running → ShutdownRequested**: `stop()` sets `shutdownRequested = true`
- **ShutdownRequested → Closing**: Update callback detects flag, calls `win.Close()`
- **Closing → Completed**: `win.add_Closing` fires, resources released, `windowCompleted.Set()` signaled
