(**
---
title: Known Issues & Limitations
category: Reference
categoryindex: 5
index: 10
---
*)

(**
# Known Issues & Limitations

## No Known Bugs

A search for `TODO`, `FIXME`, `HACK`, `BUG`, `XXX`, `WORKAROUND`, `NotImplementedException`,
and `"not implemented"` across all `.fs` source files returned zero results.

## Limitations

### Callback Timing at Low Game Speeds

Callbacks (e.g., `Callbacks.getUnitPos`) work reliably at high game speeds (100x) but can
cause protocol desync at low speeds (1-5x). At low speeds, the engine sends the next frame
before the callback round-trip completes, causing the client to receive a `Frame` message
when it expects a `CallbackResponse`.

**Workaround**: Use the raw `Protocol.receiveFrame` / `Protocol.sendFrameResponse` API
instead of `BarClient.StepWith` when issuing callbacks. Avoid callbacks entirely in graphical
mode scripts, or only issue them at high game speeds.

### No Save/Load State Persistence

The protocol handles `SaveRequest` by responding with empty state data. AI state is not
persisted across save/load cycles. This is by design for the current use case (testing and
scripting) but limits replay functionality.

### Single Connection per Session

Each `BarClient` instance manages exactly one engine connection. There is no multiplexing
or reconnection support. If the engine crashes mid-session, the client must be stopped and
a new session started.

### Headless Engine Required for Tests

Integration tests require the `spring-headless` binary and BAR game data installed locally.
The prerequisite check script (`tests/check-prerequisites.sh`) validates this, and the unified
test runner skips integration tests when prerequisites are not met.

### Graphical Mode Requires FUSE or Direct Binary

The BAR AppImage requires FUSE to mount. If FUSE is unavailable, use the direct `spring`
binary from the engine directory instead of the AppImage path. Set `AppImagePath` in the
config to the direct binary path.

### Socket Cleanup on Abnormal Exit

If the process is killed (SIGKILL) rather than stopped gracefully, the Unix socket file at
`/tmp/fsbar-*.sock` may remain on disk. The `Connection.createListener` function removes
stale socket files before binding, so subsequent sessions are not affected.

### Fixed Game Script Template

The `ScriptGenerator` produces a fixed 2-player game script (our AI vs one opponent AI) on
a single map. Multi-player scenarios, team games, or custom Lua scenarios require manual
script construction.
*)
