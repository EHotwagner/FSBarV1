# Research: 013-auto-engine-version

## R1: BAR Engine Directory Structure

**Decision**: Scan `<datadir>/engine/recoil_<YYYY.MM.DD>/` directories for engine binaries.

**Rationale**: The installed BAR data directory at `~/.local/state/Beyond All Reason/engine/` contains versioned subdirectories named `recoil_<version>`. Each contains both `spring-headless` and `spring` binaries. The `recoil_YYYY.MM.DD` naming convention is lexicographically sortable, making "latest" unambiguous via reverse string sort.

**Alternatives considered**:
- Parsing engine binary metadata: Not feasible — no version metadata embedded in binaries.
- Using modification timestamps: Unreliable across installs/copies.

## R2: BAR Game Version Discovery

**Decision**: Parse the rapid `versions.gz` file at `<datadir>/rapid/repos-cdn.beyondallreason.dev/byar/versions.gz` to resolve the `byar:test` tag to a concrete game version string.

**Rationale**: The rapid versioning system stores a `versions.gz` file that maps tags like `byar:test` to concrete version strings (e.g., `Beyond All Reason test-29876-f8bb848`). The last line matching `byar:test,` gives the current version. This is the same mechanism BAR's launcher uses.

**Alternatives considered**:
- Scanning `.sdp` packages by name: Package filenames are hashes, not human-readable version strings.
- Hardcoding a version pattern: Defeats the purpose of auto-detection.

## R3: Resolution Priority Chain

**Decision**: Use this priority chain for engine resolution:
1. `HIGHBAR_TEST_ENGINE` environment variable (existing override — full binary path)
2. `engine-version.json` config file (explicit pin — version + binary name)
3. Auto-detection from standard BAR data directory (scan + latest)
4. Error with searched locations

**Rationale**: Preserves backward compatibility with the existing `HIGHBAR_TEST_ENGINE` override used in CI/container environments. The config file serves as a middle ground for reproducibility. Auto-detection is the new default for developer convenience.

**Alternatives considered**:
- Removing `engine-version.json` entirely: Would break explicit version pinning for reproducible tests.
- Adding a new env var for version-only override: Unnecessary complexity when `engine-version.json` already serves this role.

## R4: Where to Add Auto-Detection Logic

**Decision**: Add a new `EngineDiscovery` module to `FSBar.Client` that encapsulates version scanning, resolution, and validation. Modify `EngineConfig.defaultConfig` to call into discovery for `EngineBin`, `GameType`, and `AppImagePath` defaults.

**Rationale**: Keeps discovery logic testable and isolated. The `EngineConfig` record type itself doesn't change — only the defaults populated by `defaultConfig()`. The `check-prerequisites.sh` script also gets updated to use auto-detection when `engine-version.json` is absent.

**Alternatives considered**:
- Inlining discovery in `EngineConfig.defaultConfig`: Would make the function too complex and harder to test.
- Adding discovery to `EngineLauncher`: Wrong responsibility — launcher launches, doesn't discover.

## R5: Corrupted Version Handling

**Decision**: When the latest engine version directory is found but the binary is missing or not executable, fail with a clear error identifying the corrupted version.

**Rationale**: Per clarification, corrupted versions are treated as hard errors. This is safer than silently falling back — a missing binary likely indicates a failed install that the developer should fix.

## R6: Impact on Existing Tests

**Decision**: Tests that assert specific hardcoded values (e.g., `Assert.Equal("spring-headless", config.EngineBin)`) will need to change. Tests should verify that `defaultConfig()` produces a valid, non-empty engine binary path rather than a specific hardcoded string.

**Rationale**: With auto-detection, the exact values returned by `defaultConfig()` depend on what's installed. Tests should verify structural correctness (non-empty, valid path) rather than exact values. Tests for the discovery module itself will test specific scanning behavior.
