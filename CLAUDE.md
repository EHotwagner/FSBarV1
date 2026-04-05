# FSBarV1 Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-05

## Active Technologies
- F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf), BarData (unit definitions), xUnit 2.9.x (002-test-suite-report)
- N/A (file-based report output only) (002-test-suite-report)
- F# / .NET 10.0 + FSBar.Client (in-repo), FSBar.Proto (in-repo), BarData (NuGet), xUnit 2.9.x, Microsoft.NET.Test.Sdk (003-live-game-tests)
- Filesystem only (temp dirs, socket files, log files, Markdown reports) (003-live-game-tests)

- F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf generation), FsGrpc.Tools 1.0.6 (build-time), BarData (NuGet from local store) (001-fsharp-repl-client)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for F# / .NET 10.0

## Testing

Always run tests against the live environment. Do not use mocks, fakes, or in-memory substitutes.

## Code Style

F# / .NET 10.0: Follow standard conventions

## Recent Changes
- 003-live-game-tests: Added F# / .NET 10.0 + FSBar.Client (in-repo), FSBar.Proto (in-repo), BarData (NuGet), xUnit 2.9.x, Microsoft.NET.Test.Sdk
- 002-test-suite-report: Added F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf), BarData (unit definitions), xUnit 2.9.x

- 001-fsharp-repl-client: Added F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf generation), FsGrpc.Tools 1.0.6 (build-time), BarData (NuGet from local store)

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
