# Test Report Contract

The generated test report at `reports/testreports/test-report.md` follows this structure:

## Required Sections

1. **Header**: Title, date, branch name, environment (OS, .NET version)
2. **Executive Summary**: Total tests, passed, failed, skipped counts
3. **Module Status Table**: One row per module with status (Working / Partially Working / Not Working / Not Testable)
4. **Per-Module Detail Sections**: For each module:
   - Module name and status
   - List of tests with pass/fail/skip indicator
   - Failure details (if any)
5. **Untestable Areas**: Modules/functions requiring external dependencies
6. **Conclusion**: Overall project health assessment

## Status Definitions

| Status | Meaning |
|--------|---------|
| Working | All unit tests pass |
| Partially Working | Some tests pass, some fail |
| Not Working | All tests fail or critical functionality broken |
| Not Testable | Requires external dependencies (engine, live socket) |
