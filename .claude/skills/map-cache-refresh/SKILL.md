---
name: "map-cache-refresh"
description: "Regenerate committed per-map analysis cache (chokepoints + MapGrid blobs) under bots/trainer/map-cache/*.json. Use after bumping MapCacheFile codeVersion or adding a SupportedMap."
user-invocable: true
---

## Regenerate all

```bash
bots/trainer/map-cache/refresh-all.sh
```

## Contract

Authoritative shape lives in `src/FSBar.Client/MapCacheFile.fsi`:
`schemaVersion`, `codeVersion`, `SupportedMap`, `write` / `read` / `formatLoadError`.

Files are self-describing JSON under `bots/trainer/map-cache/<safe-name>.json` with gzip+base64 blobs for heightmap / slope map / resource map. Per-map cap ~1.5 MB, total ~15 MB (SC-005).

## When to run

- You bumped `codeVersion` in `MapCacheFile.fsi`.
- You added/removed a `SupportedMap`.
- Trainer warmup hard-aborts with a mismatch (FR-006).

## Verify

Trainer warmup reads via `MapCacheFile.read` and hard-aborts on any mismatch — a successful `bots/trainer/run.sh` start against a supported map is the smoke test.
