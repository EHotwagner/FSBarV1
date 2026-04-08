# engine-version.json

The `engine-version.json` file is **optional**. It is only needed when you want to pin a specific engine and game version for reproducibility or debugging.

## When to use it

- **Debugging**: Pin a specific engine version to reproduce a bug
- **CI/testing**: Lock tests to a known-good engine version
- **Version comparison**: Switch between versions to compare behavior

## When you don't need it

If `engine-version.json` is absent, the system auto-detects:
- **Engine version**: Scans `~/.local/state/Beyond All Reason/engine/recoil_*/` and selects the latest
- **Game version**: Parses `rapid/repos-cdn.beyondallreason.dev/byar/versions.gz` for the `byar:test` tag

## Resolution priority

1. `HIGHBAR_TEST_ENGINE` environment variable (full binary path)
2. `engine-version.json` (version + binary name)
3. Auto-detect from standard BAR data directory

## Format

```json
{
  "engine": {
    "version": "2025.06.19",
    "binary": "spring-headless",
    "downloadUrl": "https://github.com/beyond-all-reason/spring/releases"
  },
  "game": {
    "name": "Beyond All Reason test-29876-f8bb848",
    "rapidTag": "byar:test"
  },
  "map": {
    "name": "Avalanche 3.4"
  }
}
```
