# Contract — `fsbar.hub.scripting.v1` additive delta

Package: `fsbar.hub.scripting.v1`
File: `proto/hub/scripting.proto`
Evolution rule: **additive only** (FR-015). `buf breaking` MUST report zero
incompatibilities before and after this feature (SC-005).

This document describes the contract-level delta — exact field numbers
are assigned during proto authoring.

## Service surface

```proto
service ScriptingService {
  // EXISTING — extended: GameFrameMessage gains game_state + game_events.
  rpc StreamGameFrames(StreamGameFramesRequest) returns (stream GameFrameMessage);

  // EXISTING — return type changed to UnitDefInfoExtended (field numbers
  // of existing UnitDefInfo fields preserved; new fields are additive).
  rpc GetUnitDef(GetUnitDefRequest) returns (UnitDefInfoExtended);

  // NEW — unary map-data queries. All grid responses use repeated
  // float / repeated int32 plus width/height; no chunking.
  rpc GetMapInfo(Empty) returns (MapInfoResponse);
  rpc GetHeightmap(Empty) returns (HeightmapResponse);
  rpc GetCornersHeightmap(Empty) returns (CornersHeightmapResponse);
  rpc GetSlopeMap(Empty) returns (SlopeMapResponse);
  rpc GetLosMap(Empty) returns (LosMapResponse);
  rpc GetRadarMap(Empty) returns (RadarMapResponse);
  rpc GetResourceMap(Empty) returns (ResourceMapResponse);
  rpc ListMetalSpots(Empty) returns (MetalSpotsResponse);

  // NEW — ordered command batch; ≤1024 entries; whole-batch
  // rejection above cap.
  rpc SendCommandBatch(SendCommandBatchRequest) returns (SendCommandBatchResponse);
}
```

## `GameFrameMessage` delta

```proto
message GameFrameMessage {
  // existing fields preserved (frame_number, team_id, events [now deprecated]...)
  GameStateFrame game_state = <new>;
  repeated GameEventEnvelope game_events = <new>;
}
```

The Phase-9 `events = []` placeholder on the existing `highbar.Frame`
return is removed from the contract surface in favor of the populated
`game_events` field on `GameFrameMessage`. If the field number must
be reserved rather than reused, it is reserved; the populated events
use a new field number.

## New / extended messages

See `../data-model.md` for field-level tables. Summary:

- `GameStateFrame`, `FriendlyUnitState`, `EnemyUnitState`,
  `EconomySnapshotWire`, `GameEventEnvelope`.
- `MapInfoResponse`, `HeightmapResponse`, `CornersHeightmapResponse`,
  `SlopeMapResponse`, `LosMapResponse`, `RadarMapResponse`,
  `ResourceMapResponse`, `MetalSpotsResponse`, `MetalSpot`.
- `UnitDefInfoExtended` (superset of `UnitDefInfo`).
- `SendCommandBatchRequest`, `SendCommandBatchResponse`,
  `CommandOutcome`.

### `EnemyUnitState.health_info` — mandatory discriminator

```proto
message EnemyUnitState {
  // ...
  oneof health_info {
    float health = <n>;
    google.protobuf.Empty unknown = <n+1>;
  }
}
```

Exactly one arm is always set (FR-003).

### `SendCommandBatch` cap

```proto
message SendCommandBatchRequest {
  repeated highbar.AICommand commands = 1; // ≤1024; above cap → whole-batch rejection
}
message SendCommandBatchResponse {
  int32 forwarded_at_frame = 1;           // single frame for all accepted entries
  repeated CommandOutcome outcomes = 2;   // 1:1 with request.commands when forwarded
  string batch_diagnostic = 3;            // set when the whole batch is rejected (e.g., oversize)
}
message CommandOutcome {
  int32 index = 1;
  bool accepted = 2;
  string diagnostic = 3;
}
```

## Channel configuration

Both the Hub scripting server and the default client channel MUST
configure `MaxReceiveMessageSize` and `MaxSendMessageSize` large
enough to fit the worst-case SupportedMap grid payload (FR-006).

## Correlation IDs

Every new RPC flows `x-fsbar-correlation-id` through the existing
`FSBar.Hub.CorrelationId.ServerInterceptor` (FR-014) — no per-message
correlation field required.

## No-session semantics (FR-012)

- Streaming RPCs: respect `close_on_session_end` identically to
  today's `StreamGameFrames`.
- Unary RPCs (map, unit-def, command, batch): return a well-formed
  response with an explicit "no-session" diagnostic rather than
  hanging or crashing.

## Compatibility gate

- `buf breaking` MUST report **zero** incompatibilities (FR-015,
  SC-005).
- Existing FSI walkthroughs `scripts/examples/15-22-hub-*.fsx` MUST
  continue to run unchanged.
