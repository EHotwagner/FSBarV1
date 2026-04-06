# Quickstart: 005-incorporate-highbarv2-fixes

## What this feature does

Ports three fixes from HighBarV2 to FSBarV1:
1. **Typed disconnection exception** — `EngineDisconnectedException` replaces generic `failwith` for socket/timeout errors
2. **Configurable read timeouts** — prevents indefinite hangs via config, env var, or default
3. **Resilient map tests** — catches disconnections in `tryLoadGrid()` so tests skip instead of cascade-failing

## Files changed

### Client library (`src/FSBar.Client/`)

| File | Change |
| ---- | ------ |
| `Connection.fsi` | Add `EngineDisconnectedException` type declaration |
| `Connection.fs` | Add exception type, wrap `readExact` errors, apply read timeout to stream |
| `EngineConfig.fsi` | Add `ReadTimeoutMs: int option` field |
| `EngineConfig.fs` | Add field with `None` default, add timeout resolution helper |

### Tests (`tests/FSBar.LiveTests/`)

| File | Change |
| ---- | ------ |
| `MapGridTests.fs` | Expand `tryLoadGrid()` catch to include `IOException` and `EngineDisconnectedException` |
| `MapQueryTests.fs` | Same `tryLoadGrid()` fix |

## How to verify

```bash
# Run map tests — should pass or skip, not fail with Broken pipe
dotnet test tests/FSBar.LiveTests/ --filter "Category=MapGrid|Category=MapQuery"

# Run full test suite
dotnet test tests/FSBar.LiveTests/
```

## Key design decisions

- Exception inherits `IOException` for backward compatibility with existing catch blocks
- Read timeout is separate from connection timeout (`EngineConfig.TimeoutMs` = 30s for accept, `ReadTimeoutMs` = 10s for reads)
- No changes to EngineFixture — the fixture model is different from HighBarV2's persistent harness
