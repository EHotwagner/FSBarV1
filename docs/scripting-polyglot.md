# Scripting from another language

You do not need F# to drive an FSBar Hub session. The Hub's scripting
surface is a plain gRPC service, `fsbar.hub.scripting.v1`, reachable on
loopback. This page walks a non-F# reader from "what are the proto
files" to "first live frame" in a new language of choice.

A runnable Python reference lives at
[`scripts/examples/python/hub_full_client.py`](../scripts/examples/python/hub_full_client.py).
The F# sibling is
[`scripts/examples/24-hub-full-client.fsx`](../scripts/examples/24-hub-full-client.fsx).

---

## Where the proto files live

All wire contracts for the scripting surface are authored as `.proto`
files in this repo. They are the **authoritative** schema — every F#
and non-F# client alike generates bindings from them.

| Path | Contents |
|---|---|
| `proto/hub/scripting.proto` | The `ScriptingService` RPCs + Hub-namespace request/response messages (including `GameStateFrame`, `GameEventEnvelope`, `UnitDefInfoExtended`, `SendCommandBatchRequest/Response`). |
| `proto/highbar/messages.proto` | `highbar.Frame` envelope used by the legacy `GameFrameMessage.frame` field. |
| `proto/highbar/commands.proto` | `highbar.AICommand` payload used by `SendCommand` and `SendCommandBatch`. |
| `proto/highbar/{callbacks,common,events}.proto` | Shared enums + helper messages referenced transitively. |

Generate against the entire `proto/` tree — the files import each
other, so partial codegen will fail.

---

## Per-language codegen

Run the commands from the **repo root**. Output directories are
created automatically when the tool supports it, otherwise create them
first (`mkdir -p <dir>`).

| Language | Codegen command | Output lands in |
|---|---|---|
| **Python** (required row) | `python -m grpc_tools.protoc -I proto --python_out=scripts/examples/python/generated --grpc_python_out=scripts/examples/python/generated proto/hub/scripting.proto proto/highbar/*.proto` | `scripts/examples/python/generated/{hub,highbar}/*_pb2{,_grpc}.py` |
| **Go** | `protoc -I proto --go_out=out/go --go_opt=paths=source_relative --go-grpc_out=out/go --go-grpc_opt=paths=source_relative proto/hub/scripting.proto proto/highbar/*.proto` (requires `protoc-gen-go` + `protoc-gen-go-grpc` on `PATH`) | `out/go/{hub,highbar}/*.pb.go` + `*_grpc.pb.go` |
| **TypeScript / Node** | `npm i @grpc/grpc-js @grpc/proto-loader` then load dynamically at runtime via `protoLoader.loadSync(['proto/hub/scripting.proto'], {includeDirs:['proto']})` and `grpc.loadPackageDefinition(...)` — no separate codegen step | in-memory service descriptor; no files on disk |

Alternative worth knowing: `buf generate` (uses `buf.gen.yaml`) wraps
any of the above with configuration files; we document the raw
`protoc` path here to keep the moving parts minimal.

---

## Connect to the Hub

The Hub scripting service is **loopback-only, insecure** — no TLS, no
auth. Remote or authenticated scripting is out of scope today. Target:

```text
127.0.0.1:5021
```

You **must** raise the channel's send and receive message-size caps to
64 MiB. Map-data responses (`GetHeightmap`, `GetSlopeMap`, …) exceed
the default 4 MiB gRPC cap on any non-trivial `SupportedMap`.

Python one-liner:

```python
import grpc
MAX = 64 * 1024 * 1024
channel = grpc.insecure_channel(
    "127.0.0.1:5021",
    options=[("grpc.max_receive_message_length", MAX),
             ("grpc.max_send_message_length", MAX)],
)
```

The equivalent option names in other stacks:

| Stack | Option |
|---|---|
| Go (`google.golang.org/grpc`) | `grpc.WithDefaultCallOptions(grpc.MaxCallRecvMsgSize(64<<20), grpc.MaxCallSendMsgSize(64<<20))` |
| Node (`@grpc/grpc-js`) | `new grpc.Client("127.0.0.1:5021", grpc.credentials.createInsecure(), { "grpc.max_receive_message_length": 64*1024*1024, "grpc.max_send_message_length": 64*1024*1024 })` |
| F# / .NET | `GrpcChannelOptions(MaxReceiveMessageSize = Nullable(64 * 1024 * 1024))` |

---

## RPC catalog

The scripting surface groups into five capability families. The full
authoritative list lives in `proto/hub/scripting.proto`; this table is
a planning view.

### 1. Session lifecycle

Configure a lobby, launch a session, manage engine speed, stop.

| RPC | Kind | Purpose |
|---|---|---|
| `ConfigureLobby` | unary | Set map, teams, AI seats. |
| `ValidateLobby` | unary | Dry-run validation without mutating Hub state. |
| `LaunchSession` | unary | Start the engine; returns a `session_id`. |
| `StopSession` | unary | Force-stop the running session. |
| `Pause` / `Resume` / `SetEngineSpeed` / `ForceEndMatch` / `SendAdminMessage` | unary | Autohost admin channel (feature 039 anchor). |

### 2. State + events stream

One server-streaming RPC carries both decoded per-tick state and typed
gameplay events. Feature 046 FR-001 / FR-002 anchor.

| RPC | Kind | Purpose |
|---|---|---|
| `StreamGameFrames` | server-streaming | Each `GameFrameMessage` carries `GameStateFrame` (friendly + enemy units + economy) and `repeated GameEventEnvelope` (typed `oneof` payload: unit created/finished/destroyed, enemy enter/leave LOS/radar, etc.). Cadence: one message per engine tick with per-client drop-oldest buffering. |

### 3. Map data

All grids read from the warmup-cached `RunningSession.MapGrid` +
`MetalSpots`. Feature 046 FR-004 / FR-006 anchor. Because grids are
`repeated float` / `repeated int32` with `width` + `height`, you
**need** the 64 MiB channel caps.

| RPC | Kind | Purpose |
|---|---|---|
| `GetMapInfo` | unary | `width`, `height`, `map_name`, `data_dir`. |
| `GetHeightmap` / `GetCornersHeightmap` | unary | Row-major heights. |
| `GetSlopeMap` | unary | Half-resolution slope grid. |
| `GetLosMap` / `GetRadarMap` | unary | Visibility grids. |
| `GetResourceMap` | unary | Engine resource-intensity grid. |
| `ListMetalSpots` | unary | `MetalSpot[]` with world (x, y, z) + `metal_value`. |

### 4. Unit-def queries

Feature 046 FR-005 anchor — merges the BarData encyclopedia with the
live `UnitDefCache`.

| RPC | Kind | Purpose |
|---|---|---|
| `GetUnitDef` | unary | Legacy slim `UnitDefInfo` (kept for back-compat). |
| `GetUnitDefExtended` | unary | Full `UnitDefInfoExtended` — cost, build-speed, sight + weapon ranges, build options. Selector: `def_id` or `internal_name`. |

### 5. Command submission

Feature 046 FR-007 anchor.

| RPC | Kind | Purpose |
|---|---|---|
| `SendCommand` | unary | Single `highbar.AICommand`. |
| `SendCommandBatch` | unary | Up to **1024** `AICommand`s per call. Oversize → whole-batch rejection with a diagnostic naming the cap. Response is one `forwarded_at_frame` + a 1:1 `CommandOutcome[]` (per-command `accepted` + `diagnostic`). |

---

## Enemy health discriminator

`EnemyUnitState.health_info` is a proto `oneof` with exactly two arms:

```proto
oneof health_info {
  float health = 4;
  EnemyHealthUnknown unknown = 5;
}
```

Totality (feature 046 FR-003): **exactly one arm is always set**.
Treat the three cases distinctly — do not collapse them:

| Case | Meaning |
|---|---|
| `health` arm with `health > 0` | Enemy currently visible (`in_los` true) with a known health reading. |
| `health` arm with `health == 0` | Enemy visible and **dying** this tick — not the same as "unknown". |
| `unknown` arm | Radar-only contact (def may or may not be disclosed) **or** a frozen last-known position after the enemy dropped from both LOS and radar. |

Always match on the `oneof` (e.g. in Python, `msg.WhichOneof("health_info")`).
A client that reads the `health` field without checking presence will
see `0.0` for radar-only enemies and silently misclassify them as dead.

---

## See also

- [`scripts/examples/24-hub-full-client.fsx`](../scripts/examples/24-hub-full-client.fsx) — F# sibling walkthrough for the same five capability families.
- [`scripts/examples/python/hub_full_client.py`](../scripts/examples/python/hub_full_client.py) — runnable Python reference.
- [`scripts/examples/python/README.md`](../scripts/examples/python/README.md) — Python prerequisites + expected output + troubleshooting.
- [`proto/hub/scripting.proto`](../proto/hub/scripting.proto) — authoritative service + message definitions.
- `proto/highbar/*.proto` — shared envelope + command types.
