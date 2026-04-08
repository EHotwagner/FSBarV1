# Data Model: 013-auto-engine-version

## Entities

### DiscoveredEngine

Represents a validated engine installation found during directory scanning.

- **VersionString**: `string` — e.g., `"2025.06.21"` (extracted from directory name `recoil_2025.06.21`)
- **VersionDir**: `string` — full path to the version directory (e.g., `~/.local/state/Beyond All Reason/engine/recoil_2025.06.21/`)
- **HeadlessBin**: `string option` — path to `spring-headless` binary if present and executable
- **GraphicalBin**: `string option` — path to `spring` binary if present and executable
- **DataDir**: `string` — the resolved Spring data directory containing `maps/` and `packages/`

### DiscoveredGame

Represents a resolved game version from the rapid versioning system.

- **Tag**: `string` — the rapid tag used for lookup (e.g., `"byar:test"`)
- **Name**: `string` — the full game name (e.g., `"Beyond All Reason test-29876-f8bb848"`)
- **Hash**: `string` — the package hash from `versions.gz`

### EngineResolution

The result of the full resolution process.

- **Source**: `OverrideEnvVar | ConfigFile | AutoDetected` — how the engine was resolved
- **Engine**: `DiscoveredEngine` — the selected engine
- **Game**: `DiscoveredGame` — the selected game version
- **Warnings**: `string list` — any diagnostic messages (e.g., "graphical binary not found in version directory")

## State Transitions

```
Start → Check HIGHBAR_TEST_ENGINE env var
  ├─ Set → Validate binary exists & executable → Resolved | Error
  └─ Not set → Check engine-version.json
       ├─ Exists → Locate specified version → Validate → Resolved | Error
       └─ Not found → Scan datadir/engine/recoil_* directories
            ├─ Found → Sort by version string (desc) → Validate latest → Resolved | Error
            └─ None found → Error (list searched locations)
```

## Validation Rules

- Engine binary must exist as a regular file and be executable (`-x` flag)
- Version directory must match pattern `recoil_<YYYY.MM.DD>` (or at minimum `recoil_*`)
- Data directory must contain both `maps/` and `packages/` subdirectories
- Game version resolution requires a valid `versions.gz` file in the rapid directory
