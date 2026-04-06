# Research: Harden SkiaSharp OpenGL Viewer

**Date**: 2026-04-06

## R1: Silk.NET Thread Safety for Window Operations

**Decision**: Use a mutable shutdown flag checked in the window's `Update` callback to close from the correct thread.

**Rationale**: GLFW (the Silk.NET windowing backend) requires all window operations to happen on the thread that created the window. Calling `window.Close()` from another thread is undefined behavior. Silk.NET does not provide a built-in cross-thread invocation mechanism in all configurations.

**Alternatives considered**:
- `window.Invoke()`: Not reliably available across all Silk.NET windowing backends
- `SynchronizationContext.Post()`: GLFW doesn't install a synchronization context
- Direct `glfwSetWindowShouldClose` via P/Invoke: Bypasses Silk.NET abstractions, fragile

## R2: SkiaSharp Raster Surface Thread Safety

**Decision**: Use a lock to synchronize surface creation/disposal (resize path) with surface usage (render path). Snapshot the surface reference under lock, then use the snapshot outside the lock to minimize lock hold time.

**Rationale**: `SKSurface` and `SKCanvas` are not thread-safe. The Silk.NET render and framebuffer-resize callbacks may fire on the same thread (the window thread), but the timing is interleaved — a resize can trigger between frames. The current code has no synchronization, so a resize during render can dispose the surface mid-use.

**Alternatives considered**:
- Immutable surface swap with `Interlocked.Exchange`: Simpler but doesn't protect against using a disposed surface in mid-render
- Double-buffering surfaces: Overkill for a single-threaded render loop; adds memory overhead
- Channel-based command queue: Over-engineered for this use case

## R3: Zero-Size Framebuffer Handling

**Decision**: When framebuffer size is zero (window minimized), set surface to null under lock and skip rendering. Do not dispose the old surface until the new one is ready (or null is the intended state).

**Rationale**: On X11/GLFW, minimizing a window can produce a framebuffer resize event with size (0, 0). `SKSurface.Create` with zero dimensions returns null. The render path already guards against null surface, but the transition must be atomic to avoid the race in R2.

**Alternatives considered**:
- Keep the old surface alive during minimization: Wastes memory for no visual benefit
- Skip the resize entirely for zero-size: Could miss the restore event if the next resize comes while surface is stale

## R4: Frame Counting Test Verification

**Decision**: Tests verify rendering correctness by counting render callback invocations and asserting zero exceptions, not by pixel comparison.

**Rationale**: Per clarification session 2026-04-06, pixel-level comparison is too brittle with GPU rendering and would itself become a source of flakiness. Frame counting verifies that the render pipeline is executing without errors, which is the primary goal of hardening.

**Alternatives considered**:
- Pixel snapshot diffing: Brittle, GPU-dependent, would add flakiness
- Pixel sampling (read-back a few points): Better but still GPU-dependent and hard to make deterministic
- GL error checking via `glGetError`: Could supplement frame counting but doesn't verify SkiaSharp path

## R5: Viewer Lifecycle Completion Signaling

**Decision**: Use `ManualResetEventSlim` to signal when `Window.Run()` returns. The `stop()` method sets the shutdown flag, then waits on the event with a 5-second timeout.

**Rationale**: The current code has no way to know when the window thread has finished. `GameViz.doStop()` uses `Thread.Sleep(500)` which is unreliable. A proper completion signal eliminates the sleep and makes start/stop cycles deterministic.

**Alternatives considered**:
- `Thread.Join()` with timeout: Requires keeping the Thread reference and may hang if GLFW cleanup is slow
- `TaskCompletionSource`: Works but `ManualResetEventSlim` is simpler for a single signal
- `CountdownEvent`: Overkill for a single signal
