# Quickstart: Permanent, Committed Map Cache

**Feature**: 026-permanent-map-cache
**Audience**: Contributors who either (a) run the trainer after cloning, or (b) modify map-analysis code and need to refresh the committed cache.

This is the short, action-oriented reference for the feature. Full context lives in `spec.md`, `plan.md`, and `data-model.md`.

---

## For first-time contributors: run the trainer without thinking about the cache

```bash
git clone <repo>
cd FSBarV1
./pack-dev.sh                                # build FSBar.* packages
cd bots/trainer
./run-trainer.sh --map "Avalanche 3.4"       # (or whatever the documented entry point is)
```

The trainer's warmup log should include:

```text
[mapcache] loaded bots/trainer/map-cache/avalanche_3_4.json in 18 ms (codeVersion=1)
[chokepoint] loaded 7 chokepoints from cache ...
[mapgrid] loaded from cache 512x512 heightMap slopeMap resourceMap
[plan] resolved 9 slots (9 buildable now)
```

You do **not** need to run any cache-generation script. The cache file is committed to the repository.

If instead you see:

```text
[mapcache] FATAL: codeVersion mismatch on bots/trainer/map-cache/avalanche_3_4.json
            expected 1, found 0 — run: bots/trainer/map-cache/refresh-all.sh
```

that means your checkout contains code changes (yours or a peer's) that bumped `MapCacheFile.codeVersion` without regenerating the committed cache. See the next section.

---

## For contributors modifying map-analysis code: refresh the cache

**When you must refresh**: any PR that touches one of

- `src/FSBar.Client/Chokepoints.fs`
- `src/FSBar.Client/BasePlan.fs`
- `src/FSBar.Client/WallIn.fs`
- `src/FSBar.Client/MapGrid.fs`
- `src/FSBar.Client/SmfParser.fs`
- `src/FSBar.Client/MapQuery.fs`
- `src/FSBar.Client/MapCacheFile.fs` (the codec itself)

or any change to the per-map inputs in `MapCacheFile.supportedMaps` (base centre, chokepoint query parameters).

**How to refresh**:

```bash
# 1. Edit codeVersion in src/FSBar.Client/MapCacheFile.fsi (and .fs) — bump by +1.
$EDITOR src/FSBar.Client/MapCacheFile.fsi

# 2. Rebuild FSBar.Client so the refresh script sees the new constants.
./pack-dev.sh

# 3. Run the refresh script. It loops over MapCacheFile.supportedMaps,
#    regenerates the cache for each supported map whose .sd7 is
#    installed locally, and skips (with a warning) any missing ones.
./bots/trainer/map-cache/refresh-all.sh

# 4. Commit the changes. The diff should include: (a) your code change,
#    (b) the bumped codeVersion, (c) one or more regenerated .json files
#    under bots/trainer/map-cache/.
git add src/FSBar.Client/ bots/trainer/map-cache/
git commit -m "fix: ... (cache refreshed for codeVersion=N)"
```

**Sanity checks**:

- Running `refresh-all.sh` a second time on the same source tree should leave `git status` clean (SC-004 determinism).
- Running it on a contributor's machine that does not have Avalanche 3.4 installed should print a clear warning, leave `bots/trainer/map-cache/avalanche_3_4.json` unchanged, and exit non-zero if **no** supported map could be refreshed (so you notice that nothing happened).

---

## For contributors adding a new supported map

1. Install the map's `.sd7` archive under `~/.local/state/Beyond All Reason/maps/`.
2. Append a new `SupportedMap` record to `MapCacheFile.supportedMaps` in `src/FSBar.Client/MapCacheFile.fs`. Set `MapName`, `Sd7FileStem`, `BaseCentre` (canonical top-left start-area centroid), and `ChokepointQuery` (start with `Chokepoints.defaultChokepointQuery MoveType.Kbot` and override fields as needed).
3. Rebuild: `./pack-dev.sh`
4. Refresh: `./bots/trainer/map-cache/refresh-all.sh`
5. Commit the new code and the new `bots/trainer/map-cache/<sanitised-name>.json` together.

No other changes are required. The trainer warmup, the refresh script, and any future CI check all consult `MapCacheFile.supportedMaps` directly.

---

## Troubleshooting

| Symptom | Meaning | Fix |
|---|---|---|
| `FileMissing` on a supported map | The committed cache file is missing from the working tree. Someone removed it, or you're on an old branch from before this feature landed. | Either check out a branch that has the cache, or run `refresh-all.sh` and commit the result. |
| `CodeVersionMismatch` | You pulled a change that bumped `codeVersion` but the committed cache files in your branch are still at the old version. | `refresh-all.sh` and commit. |
| `SchemaVersionMismatch` | The on-disk file format changed. Every cache file in the branch needs regeneration. | Same fix: `refresh-all.sh` and commit. |
| `ParametersMismatch` | The per-map inputs in `supportedMaps` (base centre, chokepoint query) changed in source but the cache file still reflects the old inputs. | Same fix — the `codeVersion` should have been bumped alongside that edit; if it wasn't, bump it now and refresh. |
| `BlobCorrupted` | The gzipped blob inside a cache file is malformed. Usually means the file was hand-edited or truncated by an interrupted write. | Delete the file and `refresh-all.sh`. |
| `UnsupportedMap` on a map the trainer is trying to analyze | The trainer is being pointed at a map that is not in `supportedMaps`. | Add it (see "adding a new supported map") or run the trainer against a supported map. |
| All operations succeed but performance is bad | Something else is at play — cache load is <25 ms on supported hardware; if it's slower, the bottleneck is elsewhere (disk, JIT warm-up, proxy). | Profile; file an issue. |

---

## What this feature explicitly does not do

- Does not add CI enforcement of cache freshness. Drift is caught at runtime by the loader's hard abort.
- Does not automatically detect staleness from source-file hashes. Every bump is manual and visible in the `codeVersion` diff.
- Does not support partial / incremental cache updates. Every refresh is a full rewrite.
- Does not cache LOS or radar (those are per-frame dynamic layers, handled by `FSBar.Client.MapCache`).
- Does not cache pathfinding results (those depend on query, not just the map).
