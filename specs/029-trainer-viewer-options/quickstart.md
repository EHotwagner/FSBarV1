# Quickstart: 029-trainer-viewer-options

**Date**: 2026-04-16

## Usage Examples

### Watch a game against NullAI with viewer at comfortable speed
```bash
bash bots/trainer/run.sh NullAI smoke --viewer
```
Launches with viewer at speed 3 (10x) by default.

### Watch a game at real-time speed
```bash
bash bots/trainer/run.sh NullAI smoke --viewer --speed 1
```

### Watch macro bot vs BARb hard on a different map
```bash
bash bots/trainer/run.sh NullAI smoke --viewer --speed 2 \
  --bot bot_macro.fsx --opponent BARb --profile hard \
  --map "Red Comet Remake 1.8"
```

### Headless batch run (unchanged from current behavior)
```bash
bash bots/trainer/run.sh NullAI 001
```

### Headless run with different opponent
```bash
bash bots/trainer/run.sh NullAI 001 --opponent BARb --profile dev
```

### Viewer at max speed (for quick visual check)
```bash
bash bots/trainer/run.sh NullAI smoke --viewer --speed max
```

## Hotkeys (in viewer window)

| Key | Action |
|-----|--------|
| W | Toggle weapon range overlays |
| L | Toggle sight range overlays |
| C | Toggle command queue overlays |
| N | Toggle full unit names |

These are the existing GameViz hotkeys, available without modification.

## Files Modified

| File | Change |
|------|--------|
| `bots/trainer/run.sh` | Add CLI option parsing (--viewer, --speed, --map, --bot, --opponent, --profile) |
| `bots/trainer/helpers/viewer.fsx` | New helper: conditional GameViz setup/teardown |
| `bots/trainer/bot.fsx` | Load viewer helper, wire up conditional viewer |
| `bots/trainer/bot_macro.fsx` | Load viewer helper, wire up conditional viewer |
| `bots/trainer/helpers/log.fsx` | Record new options in meta.json |
