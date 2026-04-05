# Quickstart: Test Suite and Functionality Report

**Feature**: 002-test-suite-report

## Prerequisites

- .NET 10.0 SDK installed
- Project builds successfully: `dotnet build` from repo root

## Run Tests

```bash
cd /home/developer/projects/FSBarV1
dotnet test src/FSBar.Client.Tests/ --logger "trx;LogFileName=testresults.trx" --logger "console;verbosity=detailed"
```

## Run Only Unit Tests (skip integration)

```bash
dotnet test src/FSBar.Client.Tests/ --filter "Category!=Integration"
```

## View Report

After tests run and report is generated:

```bash
cat reports/testreports/test-report.md
```

## Test File Locations

| Module | Test File |
|--------|-----------|
| EngineConfig | `src/FSBar.Client.Tests/EngineConfigTests.fs` |
| ScriptGenerator | `src/FSBar.Client.Tests/ScriptGeneratorTests.fs` |
| Connection | `src/FSBar.Client.Tests/ConnectionTests.fs` |
| Protocol | `src/FSBar.Client.Tests/ProtocolTests.fs` |
| Commands | `src/FSBar.Client.Tests/CommandsTests.fs` |
| Events | `src/FSBar.Client.Tests/EventsTests.fs` |
| BarClient | `src/FSBar.Client.Tests/BarClientTests.fs` |
