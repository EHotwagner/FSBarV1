# Data Model: 029-trainer-viewer-options

**Date**: 2026-04-16

## Entities

### SpeedLevel (new concept, no persistent storage)

Maps human-friendly speed names to engine game speed integers.

| Level | Engine GameSpeed | Description |
|-------|-----------------|-------------|
| 1     | 1               | Real-time (1x) |
| 2     | 5               | Slow observation (5x) |
| 3     | 10              | Moderate observation (10x) — viewer default |
| 4     | 20              | Fast observation (20x) |
| 5     | 50              | Very fast (50x) |
| max   | 100             | Maximum speed (100x) — headless default |

### CLI Options → Environment Variable Mapping

| CLI Option | Env Var | Default (no viewer) | Default (with viewer) | Validation |
|------------|---------|--------------------|-----------------------|------------|
| `--viewer` | `BOT_VIEWER` | unset (headless only) | `1` | Flag, no value |
| `--speed <val>` | `BOT_GAME_SPEED` | `100` | `10` (speed 3) | Must be 1-5 or "max" |
| `--map <name>` | `BOT_MAP` | From ladder.json | From ladder.json | Non-empty string |
| `--bot <script>` | `BOT_SCRIPT` | `bot.fsx` | `bot.fsx` | File must exist |
| `--opponent <name>` | `BOT_OPPONENT` | From ladder.json | From ladder.json | Non-empty string |
| `--profile <name>` | `BOT_OPPONENT_OPTIONS` | From ladder.json | From ladder.json | Wrapped as `{"profile":"<val>"}` |

### Run Metadata Extension (meta.json)

Existing `meta.json` fields are preserved. New fields added:

| Field | Type | Description |
|-------|------|-------------|
| `viewer` | boolean | Whether FSBar.Viz viewer was active |
| `speed_level` | string | The speed level used ("1"-"5" or "max") |
| `map_override` | string or null | Map name if overridden via CLI, null if from ladder |
| `bot_script` | string | Bot script filename used |
| `opponent_override` | string or null | Opponent AI if overridden via CLI, null if from ladder |
| `opponent_profile` | string or null | Opponent profile if set via CLI |

## State Transitions

No persistent state machines. The viewer has two runtime states:

1. **Active**: SkiaViewer window is open, receiving frames from `client.Frames` subscription.
2. **Closed**: User closed the window or game ended. Subscription disposed, `GameViz.stop()` called. Game continues headless if still running.

Transition: Active → Closed (user closes window OR game ends). One-way, no recovery.
