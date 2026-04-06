# Feature Specification: Harden SkiaSharp OpenGL Viewer

**Feature Branch**: `009-harden-skiasharp-viewer`  
**Created**: 2026-04-06  
**Status**: Draft  
**Input**: User description: "the opengl skiasharp viewer seems to be flakey. test, harden and fix any problems. do this separate from fsbar related content/context. the viewer must be battlehardened and work. use skiaprimitives recs.... to create graphics."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Viewer Renders Graphics Reliably (Priority: P1)

A developer opens the viewer window and sees SkiaSharp-drawn graphics (rectangles, circles, text, lines) rendered correctly and consistently on every frame. The window does not flicker, show blank frames, crash, or produce visual artifacts regardless of how long it runs.

**Why this priority**: The viewer is worthless if rendering is unreliable. This is the core value proposition — a stable drawing surface.

**Independent Test**: Create a standalone test that launches the viewer with a simple SkiaSharp render callback drawing known primitives (colored rectangles, circles, text). Capture multiple frames and verify visual output is present and consistent. The viewer should run for at least 60 seconds without crashing or producing blank frames.

**Acceptance Scenarios**:

1. **Given** the viewer is started with a render callback that draws colored rectangles and text, **When** the viewer runs for 60+ seconds, **Then** every rendered frame contains the expected primitives with no blank frames, flickering, or visual corruption
2. **Given** the viewer is running, **When** the window is resized multiple times rapidly, **Then** the rendered content scales correctly without crashes, blank frames, or surface errors
3. **Given** the viewer is running, **When** the render callback throws an exception, **Then** the viewer continues running and recovers on the next frame without crashing

---

### User Story 2 - Viewer Lifecycle Is Robust (Priority: P1)

A developer can start, stop, and restart the viewer multiple times in the same process without resource leaks, crashes, or hangs. Disposing the viewer cleanly releases all GPU and system resources.

**Why this priority**: Flaky lifecycle management is the most common source of "works once then breaks" behavior. Must be rock-solid for iterative development.

**Independent Test**: Start and stop the viewer 10 times in a loop. Verify no crashes, hangs, or resource leaks. Verify the viewer renders correctly after each restart.

**Acceptance Scenarios**:

1. **Given** the viewer has been started, **When** it is stopped and restarted 10 times in succession, **Then** each instance renders correctly and shuts down cleanly without errors
2. **Given** the viewer is running, **When** Dispose is called from a different thread, **Then** the viewer shuts down cleanly within 2 seconds without deadlocking
3. **Given** the viewer is running, **When** the user closes the window via the OS close button, **Then** all resources are released and subsequent restarts work correctly

---

### User Story 3 - Standalone Demo with SkiaSharp Primitives (Priority: P2)

A developer can run a standalone demo (independent of FSBar game context) that exercises the viewer with SkiaSharp drawing primitives: filled/stroked rectangles, circles, lines, text, and color gradients. This serves as both a smoke test and a visual verification tool.

**Why this priority**: A standalone demo decouples viewer testing from the game engine, making it easy to verify the viewer works in isolation and to diagnose rendering issues.

**Independent Test**: Run the demo executable or test. Verify it opens a window showing a scene composed of SkiaSharp primitives. The demo should respond to keyboard and mouse input (pan, zoom, resize).

**Acceptance Scenarios**:

1. **Given** the standalone demo is launched, **When** the window appears, **Then** it displays a scene with at least 5 different SkiaSharp primitive types (rectangles, circles, lines, text, gradients)
2. **Given** the demo is running, **When** the user scrolls the mouse wheel, **Then** the scene zooms in/out smoothly around the cursor position
3. **Given** the demo is running, **When** the user drags with the left mouse button, **Then** the scene pans smoothly following the drag direction

---

### User Story 4 - Viewer Handles Edge Conditions Gracefully (Priority: P2)

The viewer handles edge conditions without crashing: zero-size windows, rapid resize sequences, render callbacks that take too long, and concurrent access from multiple threads.

**Why this priority**: Edge conditions are where flakiness lives. Hardening these paths prevents the "works on my machine" class of bugs.

**Independent Test**: Write tests that exercise each edge condition: minimize the window (zero-size framebuffer), resize rapidly, inject slow render callbacks, call viewer methods from multiple threads concurrently.

**Acceptance Scenarios**:

1. **Given** the viewer is running, **When** the window is minimized to zero size, **Then** the viewer continues running without errors and resumes rendering when restored
2. **Given** the viewer is running, **When** the render callback takes longer than the frame budget (>16ms), **Then** the viewer drops frames gracefully without accumulating lag or crashing
3. **Given** the viewer is running, **When** multiple threads call viewer methods concurrently (e.g., resize events and render), **Then** no race conditions, crashes, or corrupted state occur

### Edge Cases

- What happens when the GL context is lost or becomes invalid mid-session?
- What happens when SkiaSharp surface creation fails (e.g., out of memory)?
- What happens when the window receives resize events before the GL context is initialized?
- What happens when the render callback is null or replaced while rendering?
- What happens when the viewer is disposed while a frame is mid-render?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The viewer MUST render SkiaSharp primitives (rectangles, circles, lines, text, paths) to an OpenGL-backed window without visual artifacts
- **FR-002**: The viewer MUST handle window resize events by recreating the SkiaSharp surface at the new size without crashes or blank frames
- **FR-003**: The viewer MUST gracefully handle zero-size framebuffers (e.g., minimized windows) by skipping rendering until a valid size is restored
- **FR-004**: The viewer MUST cleanly release all GPU resources (textures, shaders, buffers, vertex arrays) and SkiaSharp surfaces on disposal
- **FR-005**: The viewer MUST support being stopped and restarted multiple times within the same process without resource leaks or crashes
- **FR-006**: The viewer MUST catch and log exceptions from the render callback without crashing the viewer thread
- **FR-007**: The viewer MUST be thread-safe: render callbacks, input events, and lifecycle methods must not produce race conditions
- **FR-008**: The viewer MUST provide a standalone demo mode using only SkiaSharp primitives (no FSBar dependencies) to verify correct operation
- **FR-009**: The viewer MUST upload raster SkiaSharp pixels to an OpenGL texture each frame and display via a fullscreen quad (no GPU-accelerated SkiaSharp backend due to environment constraints)
- **FR-010**: The viewer MUST not leak native memory or handles across start/stop cycles

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The viewer renders 60 seconds of continuous frames where every frame fires the render callback without exceptions (verified by frame counting, not pixel comparison)
- **SC-002**: The viewer survives 10 consecutive start/stop cycles without crashes, hangs, or resource leaks
- **SC-003**: The viewer recovers from render callback exceptions on the next frame without user intervention
- **SC-004**: The viewer handles 20 rapid resize events in 2 seconds without crashes or surface corruption
- **SC-005**: The standalone demo displays at least 5 distinct SkiaSharp primitive types and responds to mouse/keyboard input
- **SC-006**: All viewer tests pass consistently across 5 consecutive test runs (no flaky tests)

## Clarifications

### Session 2026-04-06

- Q: How should automated tests verify rendering correctness (no blank frames, no artifacts)? → A: Frame callback counting — verify the render callback fires every frame with no exceptions thrown. No pixel-level comparison.

## Assumptions

- The SkiaSharp GPU backend (GRContext) is not available in this environment; raster rendering with GL texture upload is the required approach
- The viewer runs on Linux with X11 (DISPLAY=:0) and GLFW as the windowing backend via Silk.NET
- OpenGL 3.3 Core profile is available in the target environment
- The viewer is single-window; multiple simultaneous viewer instances are not required
- Testing will use the actual windowing system (X11/GLFW), not headless/mock rendering, consistent with project testing policy
