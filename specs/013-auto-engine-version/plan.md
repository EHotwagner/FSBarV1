# Implementation Plan: Automatic Engine Version Detection and Update

**Branch**: `013-auto-engine-version` | **Date**: 2026-04-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/013-auto-engine-version/spec.md`

## Summary

Replace hardcoded engine and game version defaults with automatic detection from the installed BAR data directory. A new `EngineDiscovery` module scans `~/.local/state/Beyond All Reason/engine/recoil_*/` for engine binaries and parses rapid `versions.gz` for game versions. The resolution chain is: env var override → config file pin → auto-detect latest. Corrupted installs produce hard errors.

## Technical Context

**Language/Version**: F# / .NET 10.0
**Primary Dependencies**: FSBar.Client (in-repo), System.IO, System.IO.Compression (for gzip)
**Storage**: Filesystem scanning (read-only)
**Testing**: xUnit 2.9.x, Microsoft.NET.Test.Sdk
**Target Platform**: Linux (Arch)
**Project Type**: Library (FSBar.Client)
**Performance Goals**: Engine resolution within 2 seconds of startup
**Constraints**: Must not break existing `HIGHBAR_TEST_ENGINE` override or `engine-version.json` pinning
**Scale/Scope**: Single developer workstation; typically 1-3 engine versions installed

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| I. Spec-First Delivery | PASS | Spec exists with clarifications. This adds a public module → Tier 1: requires .fsi, baselines, tests |
| II. Compiler-Enforced Structural Contracts | PASS | New `EngineDiscovery.fsi` will be created. Surface-area baseline will be added |
| III. Test Evidence Is Mandatory | PASS | EngineDiscoveryTests will cover all resolution paths |
| IV. Observability and Safe Failure | PASS | Resolution logs version at startup; corrupted installs fail with actionable errors |
| V. Scripting Accessibility | PASS | Discovery functions will be usable from FSI via existing prelude mechanism |
| Engineering: .fsi for public modules | PASS | EngineDiscovery.fsi planned |
| Engineering: Surface-area baselines | PASS | EngineDiscovery.baseline planned |
| Engineering: dotnet pack | PASS | FSBar.Client already packable; no change needed |

## Project Structure

### Documentation (this feature)

```text
specs/013-auto-engine-version/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/FSBar.Client/
├── EngineDiscovery.fs       # NEW: version scanning, resolution, validation
├── EngineDiscovery.fsi      # NEW: public API signature
├── EngineConfig.fs          # MODIFY: defaultConfig() uses EngineDiscovery
└── EngineConfig.fsi         # MODIFY: add discovery-related signatures if needed

src/FSBar.Client.Tests/
├── EngineDiscoveryTests.fs  # NEW: tests for discovery logic
├── EngineConfigTests.fs     # MODIFY: update hardcoded value assertions
├── ScriptGeneratorTests.fs  # MODIFY: update hardcoded GameType assertions
└── Baselines/
    └── EngineDiscovery.baseline  # NEW: surface-area baseline

tests/
├── engine-version.json      # MODIFY: document as optional override
└── check-prerequisites.sh   # MODIFY: auto-detect when config absent
```

**Structure Decision**: All changes fit within the existing `src/FSBar.Client` and `src/FSBar.Client.Tests` projects. One new module (`EngineDiscovery`) is added to FSBar.Client.

## Implementation Phases

### Phase 1: EngineDiscovery Module (P1 — Auto-Detection Core)

Create `EngineDiscovery.fs` and `EngineDiscovery.fsi` with:

1. **`discoverEngines`**: Scan `<datadir>/engine/recoil_*/` directories. For each, check for `spring-headless` and `spring` binaries. Return list of `DiscoveredEngine` records sorted by version string descending.

2. **`discoverGameVersion`**: Parse `<datadir>/rapid/repos-cdn.beyondallreason.dev/byar/versions.gz` to extract the game name for the `byar:test` tag. Uses `System.IO.Compression.GZipStream` to decompress.

3. **`resolveEngine`**: Implement the priority chain:
   - Check `HIGHBAR_TEST_ENGINE` env var → validate binary exists
   - Check `engine-version.json` if path provided → locate specified version
   - Auto-detect: call `discoverEngines`, take latest, validate
   - Error with searched locations if nothing found

4. **`validateEngine`**: Verify binary exists and is executable. Fail with actionable error on corrupted installs.

5. **Logging**: `printfn` the resolved engine version and source at resolution time.

### Phase 2: Integrate into EngineConfig (P1 — Wire Up)

1. Update `EngineConfig.defaultConfig()` to call `EngineDiscovery.resolveEngine` for:
   - `EngineBin` — resolved headless binary path
   - `AppImagePath` — resolved graphical binary path (or existing default if graphical not found)
   - `GameType` — resolved game version string

2. Handle the case where discovery fails (no engine installed): `defaultConfig()` should still return a config with sensible fallback values and log the error, since not all uses of `defaultConfig()` launch an engine.

### Phase 3: Update check-prerequisites.sh (P1 — Script Integration)

1. Make `engine-version.json` optional — if not present, auto-detect engine version by scanning the engine directory.
2. Auto-detect game version from rapid `versions.gz` when not specified in config.
3. Preserve all existing checks (game archive, map files, data directory).

### Phase 4: Update Tests (P1 — Verification)

1. **New `EngineDiscoveryTests.fs`**:
   - Test `discoverEngines` finds installed engine(s)
   - Test `discoverGameVersion` parses rapid versions
   - Test `resolveEngine` priority chain (env var > config > auto)
   - Test error on missing/corrupted engine directory
   - Test version sorting (newest first)

2. **Update `EngineConfigTests.fs`**:
   - Tests asserting `"spring-headless"` → assert non-empty engine path
   - Tests asserting `"Beyond All Reason test-29876-f8bb848"` → assert starts with `"Beyond All Reason"`
   - Tests asserting hardcoded AppImagePath → assert non-empty or valid path

3. **Update `ScriptGeneratorTests.fs`**:
   - Update tests that assert hardcoded `GameType` strings

4. **New baseline**: `EngineDiscovery.baseline` for surface-area test.

### Phase 5: Documentation & Cleanup (P3 — Polish)

1. Update `engine-version.json` with comments/docs indicating it's optional.
2. Update CLAUDE.md engine paths section to note auto-detection.
3. Ensure FSI prelude can access `EngineDiscovery` functions.

## Complexity Tracking

No constitution violations to justify.
