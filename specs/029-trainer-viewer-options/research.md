# Research: 029-trainer-viewer-options

**Date**: 2026-04-16

## R1: Viewer Integration Strategy

**Decision**: Use GameViz directly from bot scripts via a new `helpers/viewer.fsx` helper, rather than using `LiveSession.start()`.

**Rationale**: The existing `LiveSession` module orchestrates both `BarClient` and `GameViz` together, but the trainer bots already manage the `BarClient` lifecycle themselves (via `trainerLoopRun`). Introducing `LiveSession` would require reworking the entire trainer loop. Instead, the bots should:
1. Optionally call `GameViz.start(vizConfig)` and `GameViz.attachToClient(client)` after `client.Start()`.
2. Feed frames to `GameViz.onFrame` alongside the existing logger inside the `trainerLoopRun` tactics function, or subscribe to `client.Frames` observable.
3. Call `GameViz.stop()` in the `finally` block.

A shared `helpers/viewer.fsx` encapsulates the conditional setup so both `bot.fsx` and `bot_macro.fsx` share the same code.

**Alternatives considered**:
- `LiveSession.start()`: Would replace the BarClient lifecycle management, conflicting with the trainer's own result handling, frame logging, and tactics loop. Rejected.
- Inline the viewer code in each bot script: Duplication across bot.fsx and bot_macro.fsx. Rejected.

## R2: Frame Feeding Without Throttling

**Decision**: Subscribe to `client.Frames` observable to feed `GameViz.onFrame` independently of the trainer loop.

**Rationale**: `BarClient.Frames` is an `IObservable<GameFrame>`. GameViz already handles thread-safety via `lock stateLock`. Subscribing the viewer to the observable means:
- Frames arrive at game speed regardless of how the trainer loop processes them.
- The SkiaViewer renders at ~60fps on its own thread, naturally skipping frames that arrive faster.
- The trainer loop's `trainerLoopRun` continues to use its own `tacticsFn` callback without modification.
- Closing the viewer window only disposes the subscription; the observable continues feeding the trainer loop.

**Alternatives considered**:
- Calling `GameViz.onFrame` inside the trainer's `tacticsFn` callback: Would couple viewer and tactics, and miss frames during long tactic computations. Rejected.

## R3: Speed Level Mapping

**Decision**: Map speed levels to engine game speed values: 1→1, 2→5, 3→10, 4→20, 5→50, max→100.

**Rationale**: These values provide a geometric-ish progression from real-time (1x) to the current trainer default (100x). Speed 3 (10x) is the default when viewer is active — fast enough to see macro behavior unfold, slow enough to observe individual unit movements.

**Alternatives considered**:
- Linear mapping (1→20, 2→40, etc.): Lower speeds too fast for observation. Rejected.
- Finer granularity (1-10): More options than needed; 5 levels plus max covers all practical use cases. Rejected.

## R4: CLI Argument Architecture

**Decision**: Add `getopts`-style long option parsing to `run.sh`. New options set environment variables consumed by bot scripts. All options are optional with sensible defaults preserving backward compatibility.

**Rationale**: The existing architecture flows through `run.sh → env vars → bot.fsx`. Adding CLI options to run.sh is the natural extension point. The env var bridge means bot scripts don't need to parse CLI args themselves.

New options:
- `--viewer` (flag) → sets `BOT_VIEWER=1`
- `--speed <1-5|max>` → overrides `BOT_GAME_SPEED` with mapped value
- `--map <name>` → overrides `BOT_MAP` (from ladder.json default)
- `--bot <script>` → overrides `BOT_SCRIPT` (default: bot.fsx)
- `--opponent <name>` → overrides `BOT_OPPONENT` (from ladder.json)
- `--profile <name>` → overrides opponent profile in `BOT_OPPONENT_OPTIONS`

**Alternatives considered**:
- Pure env var approach (no CLI changes): Already works for power users but poor ergonomics for common operations. Rejected as sole interface; env vars remain as override mechanism.

## R5: Native Library Loading for Viewer in FSI

**Decision**: The `helpers/viewer.fsx` helper must `dlopen` SkiaSharp and GLFW native libraries before loading FSBar.Viz DLLs, following the established pattern in `scripts/examples/ReplGraphical.fsx`.

**Rationale**: FSI locks DLLs on first reference. Native libraries (libSkiaSharp.so, libglfw.so.3) must be loaded via `dlopen` before any managed code references them, or they won't be found. This is a known requirement documented in CLAUDE.md.

## R6: Viewer Window Close Handling

**Decision**: Wrap `GameViz.stop()` in a try-catch so that if the viewer window is closed by the user mid-game, the subscription is disposed but the trainer loop continues uninterrupted.

**Rationale**: Per spec FR-013, closing the viewer must not terminate the game. The `client.Frames` observable subscription should be disposed when the viewer closes, but the BarClient itself continues. The trainer's `trainerLoopRun` is unaffected since it uses its own frame processing path.
