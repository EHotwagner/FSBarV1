# Test Suite Documentation

FSBarV1 has 101 tests across two projects: 84 unit tests (no engine needed) and 17 integration tests (live engine).

## Unit Tests (FSBar.Client.Tests)

### EngineConfig (12 tests)

**`defaultConfig_returns_headless_mode`** — Creates a default config and asserts `Mode` equals `Headless`.

**`defaultConfig_returns_expected_map_name`** — Asserts default map is `"Red Rock Desert v2"`.

**`defaultConfig_returns_expected_game_type`** — Asserts default game type is `"Beyond All Reason test-29840-d9b7dba"`.

**`defaultConfig_returns_expected_opponent_ai`** — Asserts default opponent is `"NullAI"`.

**`defaultConfig_returns_expected_sides`** — Asserts `OurSide = "Armada"` and `OpponentSide = "Cortex"`.

**`defaultConfig_returns_expected_timeout`** — Asserts `TimeoutMs = 30000`.

**`defaultConfig_returns_expected_engine_bin`** — Asserts `EngineBin = "spring-headless"`.

**`defaultConfig_returns_none_spring_data_dir`** — Asserts `SpringDataDir = None` (auto-detected at runtime).

**`defaultConfig_returns_expected_game_speed`** — Asserts `GameSpeed = 100`.

**`defaultConfig_generates_unique_socket_paths`** — Calls `defaultConfig()` twice and asserts the socket paths differ (GUID-based).

**`defaultConfig_socket_path_starts_with_tmp`** — Asserts socket path starts with `"/tmp/fsbar-"` and ends with `".sock"`.

**`custom_config_overrides_work`** — Creates a config with `Mode = Graphical`, `MapName = "TestMap"`, `GameSpeed = 50`, `SpringDataDir = Some "/custom/data"` and asserts all values are preserved.

**`headless_mode_variant_constructs`** / **`graphical_mode_variant_constructs`** — Verify DU case construction for `EngineMode`.

### ScriptGenerator (14 tests)

**`generate_headless_produces_valid_script`** — Generates a script from default config, asserts it contains `[GAME]` and `}`.

**`generate_contains_map_name`** — Asserts the script contains `"Red Rock Desert v2"`.

**`generate_contains_game_type`** — Asserts the script contains the BAR mod version string.

**`generate_contains_socket_path`** — Asserts the script contains the config's socket path verbatim.

**`generate_contains_our_side`** / **`generate_contains_opponent_side`** — Assert `Side=Armada;` and `Side=Cortex;` appear.

**`generate_contains_opponent_ai`** — Asserts `"NullAI"` appears in the AI1 section.

**`generate_contains_game_speed`** — Asserts both `MinSpeed=100;` and `MaxSpeed=100;` appear.

**`generate_contains_faction_modoptions`** — Asserts `teamfaction_0=armada;` and `teamfaction_1=cortex;` (lowercased).

**`generate_with_graphical_config`** — Generates with `Mode = Graphical`, asserts `[GAME]` and map name still present.

**`generate_with_custom_config`** — Generates with custom map, game type, sides, and speed. Asserts all custom values appear.

**`generate_contains_required_sections`** — Asserts all 9 required script sections: `[MODOPTIONS]`, `[MAPOPTIONS]`, `[PLAYER0]`, `[AI0]`, `[AI1]`, `[TEAM0]`, `[TEAM1]`, `[ALLYTEAM0]`, `[ALLYTEAM1]`.

**`generate_contains_highbar_ai_config`** — Asserts `Name=HighBarV2;` and `ShortName=HighBarV2;` appear for our AI.

### Commands (17 tests)

Each test creates a specific command and verifies the protobuf `AICommand.CommandCase` variant and fields. Tests cover: `MoveCommand`, `BuildCommand`, `PatrolCommand`, `AttackCommand`, `GuardCommand`, `StopCommand`, `RepairCommand`, `ReclaimUnitCommand`, `FightCommand`, `SelfDestructCommand`, `SetWantedMaxSpeedCommand`, `CustomCommand`, `SendTextMessageCommand`, `GiveMeResourceCommand`, `GiveMeNewUnitCommand`, `CallLuaRulesCommand`, `CallLuaUICommand`.

Each test asserts:
- Correct `CommandCase` discriminator
- `UnitId` matches input
- Position/target fields match input
- `Options` includes `INTERNAL_ORDER` flag (8u)
- `Timeout` equals `MAX_TIMEOUT`

### Events (19 tests)

Each test constructs a protobuf `EngineEvent` with a specific event case, passes it through `Events.fromProto`, and asserts the resulting `GameEvent` DU case and fields match.

Tests cover all 28 event types: `Init`, `Release`, `Update`, `Message`, `UnitCreated`, `UnitFinished`, `UnitIdle`, `UnitMoveFailed`, `UnitDamaged` (with and without attacker), `UnitDestroyed`, `UnitGiven`, `UnitCaptured`, `EnemyEnterLOS`, `EnemyLeaveLOS`, `EnemyEnterRadar`, `EnemyLeaveRadar`, `EnemyDamaged`, `EnemyDestroyed`, `WeaponFired`, `CommandFinished`, `EnemyCreated`, `EnemyFinished`, `LuaMessage`, `Unknown` (None case).

### Connection (7 tests)

Tests use an in-process Unix socket pair (no engine required).

**`sendMessage_recvBytes_roundtrip`** — Sends `[| 1; 2; 3; 4; 5 |]` through `sendMessage`, receives via `recvBytes`, asserts equality.

**`sendMessage_writes_length_prefix_header`** — Sends 3-byte payload, reads raw 7 bytes, asserts 4-byte LE header = 3 followed by the payload.

**`sendMessage_recvBytes_large_payload`** — Roundtrips a 10,000-byte array (sequential byte values mod 256).

**`sendMessage_recvBytes_single_byte_payload`** — Roundtrips a single byte `[| 42 |]`.

**`sendMessage_recvBytes_multiple_messages`** — Sends two messages sequentially, receives both in order.

**`createListener_creates_socket_file`** — Creates a listener, asserts the socket file exists on disk.

**`createListener_removes_stale_socket`** — Creates a file at the socket path, then creates a listener — asserts the listener succeeds (stale file removed).

**`cleanup_removes_socket_file`** — Creates a file, calls `cleanup`, asserts file is gone.

**`acceptConnection_timeout_throws`** — Creates a listener with no connecting client, calls `acceptConnection` with 1ms timeout, asserts exception.

### Protocol (7 tests)

Tests simulate the proxy side by writing raw protobuf messages to a socket pair.

**`handshake_succeeds`** — Sends a `ProxyMessage.Handshake` (version 1), calls `Protocol.handshake`, asserts `HandshakeInfo` fields match.

**`handshake_rejects_wrong_version`** — Sends handshake with version 99, asserts `Protocol.handshake` throws.

**`receiveFrame_parses_frame`** — Sends a `ProxyMessage.Frame` with events, calls `receiveFrame`, asserts frame number and event list.

**`receiveFrame_returns_none_on_shutdown`** — Sends a `ProxyMessage.Shutdown`, asserts `receiveFrame` returns `None`.

**`sendFrameResponse_serializes`** — Calls `sendFrameResponse` with commands, reads raw bytes from the other end, deserializes, asserts commands match.

**`receiveFrame_handles_save_request`** — Sends `ProxyMessage.SaveRequest` followed by a `Frame`. Asserts `receiveFrame` transparently handles the save and returns the frame.

**`sendCallback_roundtrip`** — Sends a `CallbackRequest`, writes a `CallbackResponse` from the proxy side, asserts the result matches.

### BarClient (8 tests)

**`create_returns_idle_state`** — Creates a client, asserts `State = Idle`.

**`create_config_matches_provided`** — Asserts the client's config fields match the input.

**`create_with_custom_config_preserves_settings`** — Creates with custom map, game type, mode, speed. Asserts all preserved.

**`create_handshake_is_none`** — Asserts `Handshake` is `None` before `Start()`.

**`stream_access_before_connect_throws`** — Accesses `client.Stream` before connecting, asserts exception.

**`dispose_from_idle_is_safe`** — Disposes a client in Idle state, asserts no crash and state remains Idle.

**`stop_from_idle_is_safe`** — Calls `Stop()` in Idle state, asserts no crash.

**`defaultConfig_module_function_works`** — Calls `BarClient.defaultConfig()`, asserts Headless mode and correct map.

**`multiple_create_dispose_cycles`** — Creates and disposes 3 clients in a loop, asserts no resource leaks.

---

## Integration Tests (FSBar.LiveTests)

All integration tests run against a live headless BAR engine. The `EngineFixture` launches the engine, captures 30 warm-up frames, and shares the connected `BarClient` across all tests in the collection.

### ConnectionTests (6 tests)

**`Harness smoke test - engine starts and client is connected`** — Asserts `engine.IsEngineAlive` is true and `engine.Client` is not null.

**`Client connects to engine proxy socket`** — Asserts `client.State` is `Connected` or `Running`.

**`Handshake completes with valid protocol metadata`** — Asserts `Handshake.IsSome`, `ProtocolVersion > 0`, `TeamId >= 0`.

**`First frames contain Init event`** — Searches `engine.InitialEvents` for a `GameEvent.Init` case. Asserts at least one exists.

**`Empty command responses work for consecutive frames`** — Runs 5 frames with empty handlers. Asserts frame count >= 5 and frame numbers are monotonically increasing.

**`Graceful disconnect after receiving frames`** — Runs 3 frames, asserts engine is still alive afterward.

### CommandTests (4 tests)

**`MoveCommand causes unit to change position`** — Gets the commander unit ID from warm-up events. Sends a `MoveCommand` to (2048, 100, 2048) on the first frame. Runs 35 frames. Asserts the move was sent and all frames completed.

**`BuildCommand triggers unit creation`** — Sends a `BuildCommand` (unit def 1) on the first frame. Runs 70 frames, collecting `UnitCreated` events after the build order. Asserts the build command was sent.

**`StopCommand halts a moving unit`** — Sends `MoveCommand` at frame 3, then `StopCommand` at frame 10. Runs 25 total frames. Asserts both commands were sent and all frames completed without error.

**`Patrol Guard Attack Fight commands accepted without crashing`** — Sends `PatrolCommand` at frame 5, `GuardCommand` at frame 10, `AttackCommand` at frame 15, `FightCommand` at frame 20. Runs 30 frames. Asserts all 4 commands sent and no crashes.

### EventTests (5 tests)

**`Init event received with valid team ID`** — Filters warm-up events for `GameEvent.Init`. Asserts at least one exists with `teamId >= 0`.

**`Update events received with matching frame numbers`** — Runs 5 frames. For each frame with `FrameNumber > 0`, asserts at least one `GameEvent.Update` event exists.

**`UnitCreated event received for builder unit`** — Filters warm-up events for `GameEvent.UnitCreated`. Asserts at least one exists with `unitId > 0`.

**`UnitFinished event received for commander`** — Collects created and finished unit IDs from warm-up. Asserts at least one `UnitFinished` event exists and its unit ID appears in the created set.

**`Unknown events do not crash the frame loop`** — Runs 10 frames. Asserts all 10 completed without exception (unknown events are handled gracefully).

### BarbRushTests (2 tests)

These use a separate `BarbFixture` with `OpponentAI = "BARb"`.

**`Commander reaches enemy base against BARb AI`** — Sends `MoveCommand` toward enemy start position (4608, 100, 4096). Every 500 frames, queries commander position via callbacks and computes distance to target. Runs up to 5000 frames. Asserts the commander reached within 300 units of the enemy base.

**`Commander assassinates enemy commander`** — Three-phase test:
1. **Move**: Walk commander to enemy base (same as above)
2. **Hunt**: On arrival, patrol around enemy base. Check each `EnemyEnterLOS` unit's definition name via callbacks — looking for names containing "com" (e.g., "corcom").
3. **Kill**: Once enemy commander identified, send `AttackCommand` every 200 frames. Monitor for `EnemyDestroyed` event matching the target.

Runs up to 12000 frames. Asserts the enemy commander was destroyed.
