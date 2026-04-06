# Implementation Plan: Harden SkiaSharp OpenGL Viewer

**Branch**: `009-harden-skiasharp-viewer` | **Date**: 2026-04-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/009-harden-skiasharp-viewer/spec.md`

## Summary

Harden the existing `Viewer.fs` (Silk.NET + SkiaSharp raster-to-GL-texture renderer) to eliminate flaky behavior around lifecycle management, surface recreation, thread safety, and edge conditions. Add a standalone test suite that exercises the viewer with SkiaSharp primitives only (no FSBar dependencies), verifying reliability via frame callback counting. Fix identified issues in the existing code: race conditions on surface access during resize/render, missing guards for zero-size framebuffers, and incomplete resource cleanup on disposal.

## Technical Context

**Language/Version**: F# / .NET 10.0  
**Primary Dependencies**: Silk.NET.Windowing 2.22.0, Silk.NET.OpenGL 2.22.0, Silk.NET.Input 2.22.0, SkiaSharp 2.88.6  
**Storage**: N/A  
**Testing**: xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.14.1 (existing FSBar.Viz.Tests project)  
**Target Platform**: Linux x86_64, X11 (DISPLAY=:0), GLFW windowing backend  
**Project Type**: Library (rendering component)  
**Performance Goals**: 60 fps render loop  
**Constraints**: No SkiaSharp GPU backend (GRContext segfaults); raster SKSurface + GL texture upload only  
**Scale/Scope**: Single-window viewer, single consumer

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| I. Spec-First Delivery | PASS | Spec exists at `specs/009-harden-skiasharp-viewer/spec.md` with user stories and acceptance criteria |
| II. Compiler-Enforced Structural Contracts | PASS | `Viewer.fsi` already exists. If public API surface changes, `.fsi` and baselines will be updated |
| III. Test Evidence Is Mandatory | PASS | New standalone viewer tests will be added; frame counting verification approach documented |
| IV. Observability and Safe Failure Handling | PASS | FR-006 requires logging exceptions; existing `eprintfn` diagnostics will be retained/enhanced |
| V. Scripting Accessibility | N/A | Viewer is not a standalone public API library; it's consumed via GameViz which has scripting support |

**Engineering Constraints**:
- F# exclusive: PASS
- `.fsi` files: PASS (exists for all modules)
- Surface baselines: PASS (exists in `tests/FSBar.Viz.Tests/Baselines/`)
- Dependencies: No new dependencies required

## Project Structure

### Documentation (this feature)

```text
specs/009-harden-skiasharp-viewer/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/FSBar.Viz/
├── Viewer.fsi           # Public API contract (existing, may be updated)
├── Viewer.fs            # Core viewer implementation (primary hardening target)
├── VizTypes.fsi/.fs     # Shared types (no changes expected)
├── ColorMaps.fsi/.fs    # Color schemes (no changes expected)
├── LayerRenderer.fsi/.fs # Layer rendering (no changes expected)
├── SceneBuilder.fsi/.fs  # Scene composition (no changes expected)
└── GameViz.fsi/.fs      # Game integration (no changes expected)

tests/FSBar.Viz.Tests/
├── ViewerTests.fs       # NEW: standalone viewer hardening tests
├── GameVizIntegrationTests.fs  # Existing (no changes expected)
├── LayerRendererTests.fs       # Existing (no changes expected)
├── SurfaceBaselineTests.fs     # Existing (validates .fsi baselines)
└── Baselines/
    └── Viewer.baseline  # May need update if .fsi changes
```

**Structure Decision**: All changes stay within existing `FSBar.Viz` and `FSBar.Viz.Tests` projects. No new projects needed. A new test file `ViewerTests.fs` will contain all standalone viewer tests using SkiaSharp primitives only.

## Identified Issues in Current Code

### Issue 1: Race condition on surface access (Viewer.fs:73-84, 180-205)

`recreateSurface()` disposes and replaces the `surface` mutable from the framebuffer resize callback, while the `Render` callback reads `surface`, `surfaceWidth`, and `surfaceHeight` concurrently. No synchronization exists between these two paths.

**Fix**: Guard surface access with a lock object. The render callback acquires the lock to read the surface reference; `recreateSurface` acquires the lock to swap the surface. Alternatively, use an atomic surface swap pattern where the new surface is created first, then atomically swapped in, and the old one disposed after.

### Issue 2: Missing zero-size framebuffer guard (Viewer.fs:79)

`recreateSurface` checks `fbSize.X > 0 && fbSize.Y > 0` before creating a surface, but if the framebuffer is zero-size, `surface` is set to `Unchecked.defaultof<_>` (null) after disposing the old one. The render path checks `not (obj.ReferenceEquals(surface, null))` which handles this, but the old surface is still disposed without the new one being created — leaving a window where the render callback could try to use a disposed surface due to Issue 1.

**Fix**: Combine with Issue 1 fix. Under the lock, only dispose the old surface after the new one is ready. For zero-size, keep the old surface alive or set to null atomically.

### Issue 3: Broad exception swallowing in render (Viewer.fs:202-205)

The render callback catches `ObjectDisposedException`, `NullReferenceException`, and `ArgumentNullException` silently. This masks real bugs during development.

**Fix**: Log caught exceptions with `eprintfn` including the exception type and message. Keep the catch-and-continue pattern (FR-006 requires it) but add diagnostics (Constitution IV).

### Issue 4: Window close from non-window thread (Viewer.fs:230-239)

`w.Close()` is called from the disposing thread, but Silk.NET/GLFW requires window operations on the thread that created the window. This can cause crashes or hangs.

**Fix**: Use `w.Invoke(fun () -> w.Close())` or set a flag that the window thread checks to initiate shutdown via `w.Close()` from within the event loop.

### Issue 5: No completion signal from window thread (Viewer.fs:49-225)

After calling `stop()`, there's no way to know when the window thread has actually finished. The `Thread.Sleep(500)` in `GameViz.doStop()` is a fragile workaround.

**Fix**: Add a `ManualResetEventSlim` or similar that the window thread signals when `win.Run()` returns. The `stop()` method waits on this event with a timeout.

## Implementation Approach

### Phase 1: Harden Viewer.fs internals

1. **Add surface synchronization**: Introduce a lock around surface state (surface, surfaceWidth, surfaceHeight). The render callback acquires the lock to snapshot these values, then operates on the snapshot outside the lock.

2. **Fix zero-size framebuffer handling**: In `recreateSurface`, under lock, create the new surface first, swap atomically, then dispose the old. For zero-size, set surface to null under lock.

3. **Add diagnostic logging**: Replace silent exception catches with logged warnings. Add `eprintfn` for surface creation failures, render exceptions, and lifecycle events.

4. **Fix cross-thread window close**: Use a shutdown flag checked by the window's update loop. When `stop()` sets the flag, the next update cycle calls `w.Close()` from the correct thread.

5. **Add completion signaling**: Use `ManualResetEventSlim` signaled when `win.Run()` completes. `stop()` waits on this with a 5-second timeout.

6. **Guard against pre-init resize**: Ensure `recreateSurface` is safe to call before `setupGl()` has run (gl may be uninitialized).

### Phase 2: Add standalone viewer tests (ViewerTests.fs)

Tests use SkiaSharp primitives only — no FSBar.Client, no game engine. Verification by frame callback counting.

1. **Test: Basic rendering** — Start viewer with a render callback that draws rectangles, circles, lines, text, and a gradient. Count frames for 3 seconds. Assert frame count > 0 and no exceptions.

2. **Test: Start/stop cycles** — Start and stop the viewer 10 times. Assert no crashes, no hangs (timeout per cycle).

3. **Test: Render exception recovery** — Start viewer with a callback that throws on every 10th frame. Run for 3 seconds. Assert viewer is still alive and frame count continues incrementing past the exception frames.

4. **Test: Rapid resize simulation** — Start viewer, programmatically trigger resize events. Assert no crashes.

5. **Test: Cross-thread dispose** — Start viewer on background thread, dispose from main thread. Assert clean shutdown within 2 seconds.

### Phase 3: Update .fsi and baselines if needed

If the public API of `Viewer` changes (e.g., adding a completion event or status property), update `Viewer.fsi` and regenerate `Viewer.baseline`.

## Complexity Tracking

No constitution violations. All work stays within existing project structure using existing dependencies.
