# Data Model: 005-incorporate-highbarv2-fixes

## New Types

### EngineDisconnectedException

Custom exception for engine/proxy disconnection errors.

| Field | Type | Description |
| ----- | ---- | ----------- |
| Message | string | Descriptive error message (inherited from IOException) |
| LastFrameNumber | uint32 option | Last successfully received frame number, if known |
| InnerException | exn option | Original IOException or SocketException that triggered the disconnect |

**Inherits from**: `System.IO.IOException`

**Raised by**: `Connection.readExact` (zero-byte reads, read timeouts)

**Caught by**: Map test helpers (`tryLoadGrid`), BarClient frame methods (future)

## Modified Types

### EngineConfig

Add optional read timeout field:

| Field | Type | Default | Description |
| ----- | ---- | ------- | ----------- |
| ReadTimeoutMs | int option | None | Stream read timeout. Resolution: explicit > env var > 10000ms |

All existing fields remain unchanged.

## Resolution Chain: Read Timeout

```
1. EngineConfig.ReadTimeoutMs (if Some value)
2. FSBAR_CLIENT_TIMEOUT_MS environment variable (if set and parseable)
3. 10000ms hardcoded default
```

Applied to `NetworkStream.ReadTimeout` in `Connection.acceptConnection`.

## State Transitions

No changes to `SessionState` discriminated union. The new exception type integrates into existing error paths without new states.
