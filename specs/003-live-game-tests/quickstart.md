# Quickstart: 003-live-game-tests

## Prerequisites

1. BAR engine installed (spring-headless on PATH or via HIGHBAR_TEST_ENGINE env var)
2. BAR game data and maps available in SPRING_DATADIR
3. HighBarV2 proxy (libSkirmishAI.so) deployed to engine's AI directory
4. .NET 10.0 SDK installed

## Verify Prerequisites

```bash
./tests/check-prerequisites.sh
```

## Run All Tests

```bash
# Run unit + integration tests (skips integration if engine not available)
./tests/run-all.sh

# Run only unit tests (no engine needed)
./tests/run-all.sh --category unit

# Run only integration tests (requires engine)
./tests/run-all.sh --category integration

# Launch graphical game for visual validation (requires display)
./tests/run-all.sh --graphical
```

## Run Integration Tests Directly

```bash
dotnet test tests/FSBar.LiveTests/ --verbosity normal
```

## Project Layout

```
tests/
├── FSBar.LiveTests/           # Live engine integration tests (xUnit)
│   ├── FSBar.LiveTests.fsproj
│   ├── EngineFixture.fs       # Shared engine lifecycle (IAsyncLifetime)
│   ├── ConnectionTests.fs     # Handshake, frame exchange, disconnect
│   ├── CommandTests.fs        # Move, build, stop, patrol commands
│   └── EventTests.fs          # Init, UnitCreated, UnitFinished, combat events
├── check-prerequisites.sh     # Engine prerequisite validation
├── engine-version.json        # Pinned engine/game/map versions
└── run-all.sh                 # Unified test runner
```
