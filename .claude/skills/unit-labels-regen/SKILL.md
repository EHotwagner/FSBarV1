---
name: "unit-labels-regen"
description: "Regenerate src/FSBar.Viz/UnitLabels.generated.fs — the byte-stable 2- or 3-char label table for every BarData unit. Use after nupkg/BarData.*.nupkg changes or when a new unit appears."
user-invocable: true
---

## Regenerate

```bash
dotnet fsi src/FSBar.Viz/scripts/gen-unit-labels.fsx [--clean]
```

The script writes `src/FSBar.Viz/UnitLabels.generated.fs`. The matching `.fsi` is hand-maintained — **do not** let the generator rewrite it.

## Tripwire

The script exits non-zero if an existing label would change without a genuine collision (SC-006). That usually means a unit was renamed upstream — investigate before passing `--clean`.

## Algorithm (for reference)

Two-pass, pure generator in `src/FSBar.Viz/UnitLabelsGenerator.fsi` (feature 028 research R3):
1. Name-derived letter pairs.
2. Alphabetical pool sweep for overflow.

## When to run

Regenerate whenever `nupkg/BarData.*.nupkg` is updated (see `upstream-pack` skill).
