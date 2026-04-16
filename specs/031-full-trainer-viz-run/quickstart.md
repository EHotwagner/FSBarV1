# Quickstart: Full Trainer Game with Complete Visualization

**Feature**: 031-full-trainer-viz-run
**Date**: 2026-04-16

## Usage

Run a full trainer game with all visualization overlays at speed level 2:

```bash
bots/trainer/run.sh NullAI 001 --full-viz
```

This is equivalent to `--viewer` but with:
- All 8 gameplay overlays enabled from the first frame
- Base terrain layer visible (instead of height map)
- Speed level 2 / 5x game speed (instead of viewer's default level 3 / 10x)
- No frame-count limit (game runs to natural win/loss)

## Override speed

```bash
bots/trainer/run.sh NullAI 001 --full-viz --speed 3
```

Explicit `--speed` always takes precedence.

## Combine with other options

```bash
bots/trainer/run.sh NullAI 001 --full-viz --map "Avalanche 3.4" --opponent BARb
```

All existing CLI options work alongside `--full-viz`.

## Keyboard controls during game

All overlay toggles work as normal:

| Key | Overlay |
|-----|---------|
| U | Units |
| E | Events |
| M | MetalSpots |
| H | EconomyHud |
| W | WeaponRanges |
| L | SightRanges |
| C | CommandQueue |
| N | FullNames |
| B | Base terrain layer |
| 1-6 | Debug terrain layers (HeightMap, SlopeMap, etc.) |
| Home | Reset/auto-fit view |
| Scroll | Zoom |
| Drag | Pan |

## Stopping the game

- Close the viewer window for graceful exit
- Ctrl+C in the terminal
- Wait for natural win/loss (commander destroyed)
