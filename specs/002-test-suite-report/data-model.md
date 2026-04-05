# Data Model: Test Suite and Functionality Report

**Feature**: 002-test-suite-report
**Date**: 2026-04-05

## Entities

### Test Case

A single verifiable check exercising one capability of one module.

| Field | Type | Description |
|-------|------|-------------|
| Name | string | Descriptive test method name (e.g., `defaultConfig_returns_headless_mode`) |
| Module | string | Target module name (EngineConfig, Commands, Protocol, etc.) |
| Category | string | "Unit" or "Integration" |
| Result | Pass / Fail / Skip | Outcome after execution |
| FailureMessage | string? | Error details if failed |

**Naming convention**: `<module>_<function>_<scenario>` — enables grouping by module in the report.

### Test Report

A structured Markdown document summarizing test outcomes.

| Section | Content |
|---------|---------|
| Header | Date, branch, environment info |
| Summary | Total pass/fail/skip counts |
| Module Sections (x7) | Per-module status (Working / Partially Working / Not Working), list of tests with results |
| Failures Detail | Each failure with test name, expected vs actual, error message |
| Untestable Areas | Capabilities requiring external dependencies, marked as such |

### Module Coverage Map

| Module | Testable Without Engine | Key Functions to Test |
|--------|------------------------|----------------------|
| EngineConfig | Yes | defaultConfig, record construction, mode variants |
| ScriptGenerator | Yes | generate (headless config, graphical config) |
| Connection | Partial (MemoryStream) | sendMessage/recvBytes round-trip, length-prefix framing |
| Protocol | Partial (MemoryStream) | handshake parsing, receiveFrame, sendFrameResponse |
| Commands | Yes | All 16+ command constructors produce valid AICommand |
| Events | Yes | fromProto for all 28 GameEvent variants |
| BarClient | Partial | Initial state, config access, error paths on missing socket |
| Callbacks | No (requires live connection) | Mark as integration-only |
| EngineLauncher | No (requires engine binary) | Mark as integration-only |

## State Transitions (BarClient)

```
Idle ──Start()──> Starting ──socket connect──> Connected ──first frame──> Running
  ^                                                                          |
  └────────────────────────Reset()/Stop()─────────────────────────────────────┘
                                                          Error ←── any failure
```

## Relationships

- Each **Test Case** belongs to exactly one **Module**
- The **Test Report** aggregates all **Test Cases** grouped by **Module**
- **Module Coverage Map** determines which tests are unit vs integration
