# Research: 005-incorporate-highbarv2-fixes

## Decision 1: Where to define EngineDisconnectedException

**Decision**: Define `EngineDisconnectedException` in `Connection.fsi`/`Connection.fs`, alongside the existing socket I/O functions.

**Rationale**: Connection.fs is the second file in compilation order (after EngineConfig), making the exception type available to all downstream modules (Protocol, Callbacks, MapGrid, BarClient). HighBarV2 placed it in Client.fs (their equivalent top-level type), but FSBarV1's Connection module is the natural home since it owns all low-level read/write operations where disconnections originate.

**Alternatives considered**:
- Separate `Exceptions.fs` file: Over-engineered for a single type, adds compilation ordering complexity.
- In BarClient.fs: Too late in compilation order — Connection.fs and Protocol.fs need to reference it.

## Decision 2: Read timeout configuration approach

**Decision**: Add an optional `ReadTimeoutMs` field to `EngineConfig` with a resolution chain: explicit config value > `FSBAR_CLIENT_TIMEOUT_MS` env var > 10000ms default. Apply the timeout to `NetworkStream.ReadTimeout` in `Connection.acceptConnection`.

**Rationale**: HighBarV2 added timeouts at the `HighBarClient` constructor level. FSBarV1 already passes config through `BarClient → Connection.acceptConnection`, so extending `EngineConfig` with the optional timeout field keeps the single source of configuration. The env var name uses `FSBAR_` prefix consistent with the project naming convention.

**Alternatives considered**:
- Separate timeout parameter on `Connection.acceptConnection`: Would require changing the call signature and passing it through the chain manually.
- Always use env var: Not programmatically configurable, limits test flexibility.

## Decision 3: How to fix map test cascading failures

**Decision**: Modify `tryLoadGrid()` in both `MapGridTests.fs` and `MapQueryTests.fs` to catch `EngineDisconnectedException` and `IOException` (including `SocketException` inner) in addition to the existing `"empty array"` message check. Return `None` and write a SKIP message for all these cases.

**Rationale**: The current `tryLoadGrid()` only catches `ex.Message.Contains("empty array")`, but broken pipe errors from proxy disconnection surface as `IOException` with `SocketException` inner. This is exactly the pattern HighBarV2 fixed in T9_MapTests.fs. The test results confirm: 11/12 tests fail with `IOException: Broken pipe` because the first callback disconnect cascades to all subsequent tests sharing the same stream.

**Alternatives considered**:
- Reconnecting the client between tests: Too invasive — FSBarV1's EngineFixture shares a single client. A fresh engine per test would be slow.
- Catching only `IOException`: Would miss the new `EngineDisconnectedException` type once it's wired in.

## Decision 4: readExact error wrapping strategy

**Decision**: In `Connection.readExact`, catch `IOException` (timeout) and wrap in `EngineDisconnectedException`. For zero-byte reads, raise `EngineDisconnectedException` directly instead of `failwith`. Keep existing `failwith` for `sendMessage` write errors — those are less common and less ambiguous.

**Rationale**: HighBarV2's `readFully` helper catches `IOException` and wraps it. FSBarV1's `readExact` already detects zero-byte reads but uses `failwith`. Upgrading to a typed exception enables downstream code to distinguish disconnections from protocol errors.

**Alternatives considered**:
- Wrapping all I/O errors (read + write): Write errors are already clear (`Broken pipe`) and less frequent. Over-wrapping reduces diagnostic specificity.
- Adding frame tracking to readExact: readExact doesn't know the frame number. Frame context should be added at the Protocol level where frame state is known.

## Decision 5: .fsi signature changes

**Decision**: Update `Connection.fsi` to declare the new `EngineDisconnectedException` type and update `EngineConfig.fsi` to add the `ReadTimeoutMs` field. No new .fsi files needed since no new modules are added.

**Rationale**: Constitution requires .fsi for all public types. The exception is a public type used by tests and downstream code. The config change is a public record field.
