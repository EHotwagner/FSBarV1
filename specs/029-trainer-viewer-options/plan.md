# Implementation Plan: Trainer Viewer and Runtime Options

**Branch**: `029-trainer-viewer-options` | **Date**: 2026-04-16 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/029-trainer-viewer-options/spec.md`

## Summary

Add CLI options to `bots/trainer/run.sh` for launching an FSBar.Viz/SkiaViewer window alongside headless training runs, controlling game speed (1-5, max), selecting maps, choosing bot scripts, and picking opponent AIs with difficulty profiles. The engine always runs headless; the viewer is a SkiaViewer window rendering live game state via `GameViz`. All options are backward-compatible — existing invocations continue to work unchanged.

## Technical Context

**Language/Version**: Bash (run.sh CLI) + F# 9 on .NET 10.0 (bot scripts, helpers)
**Primary Dependencies**: FSBar.Client (BarClient, EngineConfig), FSBar.Viz (GameViz, SceneBuilder, UnitGlyph), SkiaViewer (window management), SkiaSharp 2.88.6
**Storage**: Filesystem only — run artifacts under `bots/runs/` (gitignored)
**Testing**: Manual integration testing (launch trainer with each option combination and verify behavior). No xUnit tests — changes are in .fsx scripts and bash, not compiled modules.
**Target Platform**: Linux (developer workstation with X11 display)
**Project Type**: CLI tool / bot training harness
**Performance Goals**: Viewer at ~60fps; game simulation unthrottled at requested speed
**Constraints**: Native libraries (libSkiaSharp.so, libglfw.so.3) must be dlopen'd before FSI loads managed DLLs
**Scale/Scope**: 2 bot scripts, 1 bash runner, 1 new helper module, 1 modified helper

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| Spec-First Delivery (§I) | PASS | Spec and plan created before implementation |
| Compiler-Enforced Contracts (§II) | N/A | No new compiled F# modules. Changes are in .fsx scripts (not compiled) and bash. No .fsi files needed. |
| Test Evidence (§III) | PASS | Manual integration testing appropriate for CLI/script changes. Each user story defines independent verification criteria. |
| Observability (§IV) | PASS | run.sh already logs to stdout; meta.json extended with new option fields |
| Scripting Accessibility (§V) | N/A | No new public API. Changes are to scripts themselves. |
| F# Exclusive Stack (§Eng) | PASS | F# scripts + bash runner (bash is the existing runner, not a new language choice) |
| .fsi for public modules | N/A | No new compiled modules |
| Surface-area baselines | N/A | No new compiled modules |
| Dependencies minimized | PASS | No new NuGet dependencies. Uses existing FSBar.Viz, SkiaViewer, SkiaSharp already in the project. |

**Post-Phase 1 re-check**: All gates still pass. No compiled module changes introduced.

## Project Structure

### Documentation (this feature)

```text
specs/029-trainer-viewer-options/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: research decisions
├── data-model.md        # Phase 1: data model and mappings
├── quickstart.md        # Phase 1: usage examples
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
bots/trainer/
├── run.sh                          # MODIFIED: add CLI option parsing + meta.json extension
├── bot.fsx                         # MODIFIED: load viewer helper, conditional viewer setup
├── bot_macro.fsx                   # MODIFIED: load viewer helper, conditional viewer setup
├── ladder.json                     # UNCHANGED
└── helpers/
    ├── prelude.fsx                 # UNCHANGED
    ├── viewer.fsx                  # NEW: conditional GameViz setup/teardown
    ├── log.fsx                     # UNCHANGED
    ├── tactics.fsx                 # UNCHANGED
    ├── perception.fsx              # UNCHANGED
    ├── opening_build.fsx           # UNCHANGED
    ├── production_queue.fsx        # UNCHANGED
    ├── constructor_dispatch.fsx    # UNCHANGED
    ├── upgrade_gate.fsx            # UNCHANGED
    └── attack_launch.fsx           # UNCHANGED
```

**Structure Decision**: All changes are within the existing `bots/trainer/` tree. One new helper file (`viewer.fsx`) is added. No compiled project changes.

## Implementation Design

### Phase 1: CLI Option Parsing in run.sh

Extend `run.sh` to accept long options after the two positional arguments:

```
bash bots/trainer/run.sh <rung_name> <iter_id> [OPTIONS]

OPTIONS:
  --viewer              Open FSBar.Viz viewer window
  --speed <1-5|max>     Set game speed level (default: max, or 3 with --viewer)
  --map <name>          Override map from ladder.json
  --bot <script>        Override bot script (default: bot.fsx)
  --opponent <name>     Override opponent AI from ladder.json
  --profile <name>      Set opponent difficulty profile
```

Parse with a `while` loop over `$@` after shifting the two positional args. Validate:
- `--speed`: must be 1, 2, 3, 4, 5, or "max"
- `--bot`: file must exist in `bots/trainer/`
- Other string options: non-empty

Export new/overridden environment variables:
- `BOT_VIEWER=1` (when `--viewer`)
- `BOT_GAME_SPEED` mapped from speed level (1→1, 2→5, 3→10, 4→20, 5→50, max→100)
- `BOT_SPEED_LEVEL` (the raw level string, for meta.json)
- `BOT_MAP` overridden if `--map` given
- `BOT_SCRIPT` overridden if `--bot` given
- `BOT_OPPONENT` overridden if `--opponent` given
- `BOT_OPPONENT_OPTIONS` rebuilt if `--profile` given

When `--viewer` is set and `--speed` is not, default `BOT_GAME_SPEED=10` (speed level 3).

### Phase 2: Viewer Helper (helpers/viewer.fsx)

New helper that conditionally starts/stops the FSBar.Viz viewer:

1. Check `BOT_VIEWER` env var. If unset or not "1", all functions are no-ops.
2. When active:
   - `dlopen` native libraries (libglfw.so.3, libSkiaSharp.so) from the test output directory
   - `#r` FSBar.Viz and SkiaViewer DLLs
   - Expose `startViewer: BarClient -> IDisposable` that:
     - Calls `GameViz.start(None)` (default VizConfig)
     - Calls `GameViz.attachToClient(client)`
     - Subscribes to `client.Frames |> Observable.subscribe GameViz.onFrame`
     - Returns a disposable that calls `GameViz.stop()` and disposes the subscription
   - Expose `stopViewer: unit -> unit` for the finally block
3. All viewer operations wrapped in try-catch so viewer failures never crash the trainer.

### Phase 3: Bot Script Integration

Both `bot.fsx` and `bot_macro.fsx`:

1. Add `#load "helpers/viewer.fsx"` after existing helper loads
2. After `client.Start()`, call `Viewer.startViewer client` (returns disposable, stored in mutable)
3. In the `finally` block, call `Viewer.stopViewer()` before `client.Stop()`

### Phase 4: Meta.json Extension

In `run.sh`, extend the `jq -n` block that writes `meta.json` (lines 92-116) with:
- `viewer`: bool (from `BOT_VIEWER`)
- `speed_level`: string (from `BOT_SPEED_LEVEL`)
- `map_override`: string option (if `--map` was used)
- `bot_script`: string (from `BOT_SCRIPT`)
- `opponent_override`: string option (if `--opponent` was used)
- `opponent_profile`: string option (if `--profile` was used)
