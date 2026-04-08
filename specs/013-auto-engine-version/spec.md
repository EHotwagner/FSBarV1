# Feature Specification: Automatic Engine Version Detection and Update

**Feature Branch**: `013-auto-engine-version`  
**Created**: 2026-04-08  
**Status**: Draft  
**Input**: User description: "make the project robust against engine version changes. the bar engine updates often and hardcoded engine versions are not practicable. some automatic update functionality seems the way to go."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automatic Engine Version Detection (Priority: P1)

A developer runs the project (tests, REPL, game sessions) without manually updating any version configuration. The system automatically discovers the latest installed engine version on the local machine and uses it. If only one engine version is installed, it is used without prompting. If multiple versions are installed, the most recent one is selected by default.

**Why this priority**: This is the core value — eliminating the manual step of updating `engine-version.json` every time BAR pushes an engine update. Without this, every engine update breaks the workflow until a developer manually edits the version file.

**Independent Test**: Can be fully tested by installing a new engine version and running a test suite without modifying any configuration. The system should detect and use the new version automatically.

**Acceptance Scenarios**:

1. **Given** a single engine version is installed in the standard BAR data directory, **When** the system resolves the engine path, **Then** it selects that version's binary without requiring any manual configuration change.
2. **Given** multiple engine versions are installed, **When** the system resolves the engine path, **Then** it selects the most recent version (by version string sort or directory modification time).
3. **Given** no engine is installed in any known location, **When** the system attempts to resolve the engine, **Then** it reports a clear error message indicating no engine was found and where it searched.

---

### User Story 2 - Version Override for Reproducibility (Priority: P2)

A developer needs to pin a specific engine version for debugging, testing, or reproducibility. They can explicitly set a version via environment variable or configuration file, overriding automatic detection.

**Why this priority**: Auto-detection is the default workflow, but reproducible builds and debugging require the ability to lock to a specific version. This ensures auto-detection doesn't break advanced use cases.

**Independent Test**: Can be tested by setting an environment variable or config value to a specific version, then verifying the system uses that exact version even when a newer one is available.

**Acceptance Scenarios**:

1. **Given** automatic detection would select version X, **When** the developer sets an explicit version override to version Y, **Then** the system uses version Y.
2. **Given** the developer sets an override to a version that is not installed, **When** the system resolves the engine, **Then** it reports an error identifying the missing version.
3. **Given** no override is set, **When** the system resolves the engine, **Then** it falls back to automatic detection.

---

### User Story 3 - Version Change Notification (Priority: P3)

When the automatically detected engine version changes (e.g., after a BAR update), the system logs or reports which version it is using at startup. This helps developers notice version transitions and correlate behavior changes with engine updates.

**Why this priority**: Visibility into which version is active prevents confusion when behavior changes after an engine update. Lower priority because it's informational, not functional.

**Independent Test**: Can be tested by switching the installed engine version and observing the startup output for version information.

**Acceptance Scenarios**:

1. **Given** the system starts with auto-detected engine version X, **When** the engine session begins, **Then** the active engine version is logged or displayed.
2. **Given** the engine version changed since the last run, **When** the system starts, **Then** the version information is visible in the output without requiring debug/verbose mode.

---

### Edge Cases

- What happens when the engine directory structure changes in a future BAR update? The system should degrade gracefully and report which directories it scanned.
- When a version directory exists but the binary is missing or not executable, the system fails with a clear error identifying the corrupted version and what is missing.
- Non-standard installation locations are supported via environment variable override; auto-detection only scans known standard directories.
- When the standard BAR data directory (`~/.local/state/Beyond All Reason`) does not exist, the system reports a clear error listing the directories it searched.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST automatically discover installed engine versions and game versions by scanning known installation directories.
- **FR-002**: System MUST select the most recent engine version when multiple versions are available and no override is specified.
- **FR-003**: System MUST support an explicit version override mechanism (environment variable and/or configuration file) that takes precedence over auto-detection.
- **FR-004**: System MUST report a clear, actionable error when no engine version can be found.
- **FR-005**: System MUST log the resolved engine version at session startup.
- **FR-006**: System MUST validate that the selected engine binary (either `spring-headless` or graphical `spring`) exists and is executable before attempting to launch it.
- **FR-007**: System MUST continue to support the existing engine resolution fallback chain (environment variable → PATH → standard locations) while adding auto-detection as the default behavior.
- **FR-010**: When an engine version directory is found but its binary is missing or not executable, the system MUST fail with an error identifying the corrupted version and what is missing.
- **FR-008**: The version configuration file (`engine-version.json`) MUST remain functional but become optional — the system works without it when auto-detection is available.
- **FR-009**: System MUST automatically discover the latest installed game version (e.g., `Beyond All Reason test-XXXXX-XXXXXXX`) alongside the engine version.

### Key Entities

- **Engine Version**: A specific release of the BAR engine identified by its version string (e.g., `recoil_2025.06.21`), containing the engine binary and associated data files.
- **Game Version**: A specific release of the BAR game content identified by its version string (e.g., `Beyond All Reason test-29876-f8bb848`), installed alongside the engine.
- **Engine Discovery Source**: An ordered set of locations where the system searches for installed engines (environment variables, PATH, standard BAR directories).
- **Version Resolution Strategy**: The decision logic that selects which engine version to use — override first, then auto-detect latest, then error.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can run the full test suite after an engine update without modifying any configuration files — zero manual steps required.
- **SC-002**: The system correctly identifies and uses the latest engine version within 2 seconds of startup.
- **SC-003**: When a version override is set, the system uses the specified version 100% of the time.
- **SC-004**: Error messages for missing engines identify at least 2 specific locations that were searched, enabling developers to self-diagnose the issue.
- **SC-005**: Engine version is visible in session startup output for every run, enabling developers to correlate engine version with observed behavior.

## Clarifications

### Session 2026-04-08

- Q: Should the system auto-detect only the engine version, or also the game version (from engine-version.json)? → A: Auto-detect both engine version and game version for fully hands-off operation after BAR updates.
- Q: Should auto-detection discover both `spring-headless` and graphical `spring` binaries, or only headless? → A: Auto-detect both binary types so neither headless (tests) nor graphical (viz) workflows break after updates.
- Q: How should the system handle corrupted or partially installed engine versions? → A: Fail with an error — treat any corrupted version as a hard stop.

## Assumptions

- The BAR engine installation follows the existing directory convention: `<datadir>/engine/recoil_<version>/` containing the engine binary.
- Both engine binary types (`spring-headless` and graphical `spring`) are discovered per version directory.
- The standard BAR data directory (`~/.local/state/Beyond All Reason`) is the primary search location on Linux.
- Engine version strings are sortable to determine "most recent" (lexicographic sort on `recoil_YYYY.MM.DD` format is sufficient).
- The existing environment variable override (`HIGHBAR_TEST_ENGINE`) continues to serve as the highest-priority override mechanism.
- Container environments may have the engine installed at a different path; the system should respect environment variables in those cases.
