# bots/trainer/map-cache/

Committed, deterministic per-map analysis artifacts consumed by the trainer
bot's warmup path via `FSBar.Client.MapCacheFile.read`.

These files are **generated**, not hand-written: each one is the output of
`scripts/examples/14-cache-map-analysis.fsx` for a specific `.sd7`, pinned
to the `codeVersion` declared in `src/FSBar.Client/MapCacheFile.fsi`.

## Refreshing

Run the single entry point:

```bash
./bots/trainer/map-cache/refresh-all.sh
```

The script iterates over `MapCacheFile.supportedMaps` (via
`scripts/examples/15-list-supported-maps.fsx`), regenerates each map's cache
whose `.sd7` is installed locally, and skips (with a warning) maps that are
not installed.

## When to refresh

Bump `MapCacheFile.codeVersion` (in both the `.fsi` and the `.fs`) and run
`refresh-all.sh` whenever you change the semantics of any of:

- `Chokepoints.fs` / `BasePlan.fs` / `WallIn.fs`
- `MapGrid.fs` / `SmfParser.fs` / `MapQuery.fs`
- `MapCacheFile.fs` itself (the codec)

If you forget to refresh, the trainer warmup hard-aborts with a structured
`LoadError` pointing at this directory and the refresh command.

## Why this directory is no longer gitignored

Prior to feature 026 these files were ignored as scratch output of the
bake step. They are now committed so that a fresh clone boots the trainer
without running any cache-generation script — and so that `codeVersion`
mismatches surface as a real git diff during code review instead of silently
producing stale analyses on every contributor's machine.
