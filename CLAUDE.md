# FSBarV1 Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-06

## Active Technologies
- F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf), BarData (unit definitions), xUnit 2.9.x (002-test-suite-report)
- N/A (file-based report output only) (002-test-suite-report)
- F# / .NET 10.0 + FSBar.Client (in-repo), FSBar.Proto (in-repo), BarData (NuGet), xUnit 2.9.x, Microsoft.NET.Test.Sdk (003-live-game-tests)
- Filesystem only (temp dirs, socket files, log files, Markdown reports) (003-live-game-tests)
- F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf), FSBar.Proto (generated types), BarData (unit definitions) (004-array-map-layers)
- In-memory Array2D grids + ConcurrentDictionary caching (004-array-map-layers)
- Filesystem (socket files, session dirs) (005-incorporate-highbarv2-fixes)
- F# / .NET 10.0 + xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x (existing in FSBar.Client.Tests) (007-fix-surface-baselines)
- Filesystem — `.baseline` text files committed to git (007-fix-surface-baselines)

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

Tests that cannot pass due to out-of-scope issues (e.g., missing server, external dependency unavailable, unimplemented upstream feature) MUST be marked as skipped or have their assertions relaxed. Never mark a failing test as passed.

## Code Style

F# / .NET 10.0: Follow standard conventions

## Recent Changes
- 007-fix-surface-baselines: Added F# / .NET 10.0 + xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x (existing in FSBar.Client.Tests)
- 006-validate-highbar-fixes: Added [if applicable, e.g., PostgreSQL, CoreData, files or N/A]
- 005-incorporate-highbarv2-fixes: Added F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf), BarData (unit definitions), xUnit 2.9.x


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
