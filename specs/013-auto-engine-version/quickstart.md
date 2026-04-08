# Quickstart: 013-auto-engine-version

## What This Feature Does

Eliminates the need to manually update `engine-version.json` and hardcoded defaults when BAR pushes an engine or game update. The system auto-detects the latest installed engine and game versions from the standard BAR data directory.

## Key Files to Modify

1. **New: `src/FSBar.Client/EngineDiscovery.fs` + `.fsi`** — Core discovery logic: scan engine directories, parse rapid `versions.gz`, validate binaries, resolve priority chain.

2. **Modify: `src/FSBar.Client/EngineConfig.fs`** — Update `defaultConfig()` to call `EngineDiscovery` for engine binary, game type, and AppImage path instead of returning hardcoded values.

3. **Modify: `tests/check-prerequisites.sh`** — Make `engine-version.json` optional; fall back to auto-detection when absent.

4. **Modify: `tests/engine-version.json`** — Remains as optional override file; document that it's no longer required.

5. **New: `src/FSBar.Client.Tests/EngineDiscoveryTests.fs`** — Unit tests for discovery logic.

6. **Modify: `src/FSBar.Client.Tests/EngineConfigTests.fs`** — Update tests that assert hardcoded values to verify structural correctness instead.

## Resolution Priority

```
HIGHBAR_TEST_ENGINE env var → engine-version.json → Auto-detect from ~/.local/state/Beyond All Reason/
```

## Build & Test

```bash
dotnet build src/FSBar.Client/FSBar.Client.fsproj
dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj
```
