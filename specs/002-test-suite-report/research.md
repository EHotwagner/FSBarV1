# Research: Test Suite and Functionality Report

**Feature**: 002-test-suite-report
**Date**: 2026-04-05

## R-001: Test Strategy for Modules with External Dependencies

**Decision**: Use a tiered test approach — pure unit tests for logic-only modules, integration-style tests with mocked streams for protocol/connection modules, and mark tests requiring the actual game engine with `[<Trait("Category", "Integration")>]` so they can be filtered.

**Rationale**: The BAR game engine and Unix domain sockets are not available in CI or typical dev environments. Testing serialization/deserialization and state machine logic does not require actual sockets — byte arrays and memory streams suffice. This maximizes test coverage without external dependencies.

**Alternatives considered**:
- Full integration tests only: Rejected — requires running game engine, impractical for CI.
- Mocking framework (Moq/NSubstitute): Rejected — overkill for this use case; MemoryStream and direct byte construction are sufficient for protocol testing.

## R-002: Testing Protobuf Serialization Round-Trips

**Decision**: Test serialization by constructing protobuf message objects, serializing via FsGrpc codec, deserializing, and comparing. Use the `Google.Protobuf.MessageExtensions` or FsGrpc's built-in `Codec` serialization.

**Rationale**: FsGrpc generates F# record types with `Codec` static members. Testing round-trip serialization validates that the Connection module's `sendMessage`/`recvBytes` correctly frame messages and that Protocol module correctly interprets them.

**Alternatives considered**:
- Testing against captured wire data: Good for regression but hard to maintain; use round-trip instead.

## R-003: BarClient State Machine Testing

**Decision**: Test state transitions by creating a BarClient instance with a config pointing to a non-existent socket path, then verifying initial state is Idle. For deeper state testing, use MemoryStream injection where possible, or verify that Start() transitions state and handles connection timeouts gracefully.

**Rationale**: The BarClient class manages a state machine (Idle → Starting → Connected → Running). Testing transitions without a real engine requires either injecting a mock stream or testing error paths (timeout on connect).

**Alternatives considered**:
- Extracting state machine into a separate testable module: Too invasive for the test-only feature; test the public API as-is.

## R-004: Report Format and Generation

**Decision**: Generate a Markdown report manually after running `dotnet test` with TRX logger output. Parse the TRX XML or use dotnet test console output to classify results by module (based on test class naming convention `Module_Tests`).

**Rationale**: TRX is the standard .NET test result format. Markdown is human-readable and version-control friendly. The report is a one-time artifact, not an automated pipeline step.

**Alternatives considered**:
- HTML report via ReportGenerator: Heavier dependency for a simple report.
- JSON test output: Less human-readable than Markdown.

## R-005: Test Project Structure

**Decision**: Add test files to the existing `FSBar.Client.Tests` project following the naming convention `<Module>Tests.fs` (e.g., `EngineConfigTests.fs`, `CommandsTests.fs`). Each test file maps 1:1 to a source module.

**Rationale**: The test project already exists with xUnit configured. One test file per module keeps tests organized and makes the report mapping straightforward.

**Alternatives considered**:
- Single monolithic test file: Harder to navigate and doesn't map to module-level reporting.
- Separate test projects per module: Over-engineered for this scope.
