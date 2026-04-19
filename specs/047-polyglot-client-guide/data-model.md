# Data Model — Polyglot scripting-client guide

This feature is documentation + a reference client. It introduces no
new wire types, no new persistent state, and no new runtime entities
on the Hub side. The "entities" worth naming are the documentation
artifacts themselves, for traceability.

## Artifacts

### A1. Docs page — "Scripting from another language"
- **Path**: `docs/scripting-polyglot.md`
- **Format**: CommonMark Markdown.
- **Sections** (fixed):
  1. Where the proto files live (`proto/hub/*.proto`,
     `proto/highbar/*.proto`).
  2. Per-language codegen table — Python (required), Go,
     TypeScript/Node.
  3. Connection: endpoint `127.0.0.1:5021`, insecure channel,
     `MaxReceiveMessageSize` / `MaxSendMessageSize` = 64 MiB with a
     Python one-liner.
  4. RPC catalog, grouped into five capability families:
     - Session lifecycle: `ConfigureLobby`, `LaunchSession`,
       `StopSession`, `Pause`/`Resume`/`SetEngineSpeed`/`ForceEnd`/
       `SendAdminMessage` (anchor: feature 039 autohost channel).
     - State + events stream: `StreamGameFrames` carrying
       `GameStateFrame` + `GameEventEnvelope[]` (anchor: feature
       046 FR-001 / FR-002).
     - Map data: `GetMapInfo`, `GetHeightmap`, `GetCornersHeightmap`,
       `GetSlopeMap`, `GetLosMap`, `GetRadarMap`, `GetResourceMap`,
       `ListMetalSpots` (anchor: feature 046 FR-004 / FR-006 —
       warmup-cached grids + 64 MiB channel).
     - Unit-def queries: `GetUnitDefExtended` (anchor: feature 046
       FR-005 — encyclopedia ∪ `UnitDefCache`).
     - Command submission: `SendCommandBatch` (anchor: feature 046
       FR-007 — ≤1024 commands per call, 1:1 `CommandOutcome[]`).
  5. `oneof health_info` totality rule in prose (anchor: feature 046
     FR-003).
  6. Cross-links: `scripts/examples/24-hub-full-client.fsx`,
     `proto/hub/scripting.proto`, `proto/highbar/*.proto`.
- **Validation**: covered by human review against FR-001…FR-004,
  FR-010, FR-012.

### A2. Python reference client
- **Path**: `scripts/examples/python/hub_full_client.py`
- **Size budget**: ≤200 LOC excluding generated bindings (SC-003).
- **Structure**: five section-delimited blocks, each self-contained
  (FR-011):
  1. Channel setup + fast-fail diagnostic (FR-007).
  2. `ConfigureLobby` + `LaunchSession`.
  3. `StreamGameFrames` × 10 ticks, printing frame number + unit
     counts + any `GameEventEnvelope`.
  4. `GetMapInfo` + `ListMetalSpots` + `GetUnitDefExtended`.
  5. `SendCommandBatch` + `StopSession` (with SIGINT handler —
     FR-008).
- **Runtime deps**: `grpcio` only (no `grpcio-tools` at runtime).
- **Validation**: manual run against a live Hub per US2 AS1–AS4;
  SC-002 (<2 min) and SC-006 (Ctrl-C → Idle within 15 s) spot-checked.

### A3. Python example README
- **Path**: `scripts/examples/python/README.md`
- **Contents**:
  - Prerequisites (Python 3.10+, `pip`).
  - `pip install -r requirements.txt`.
  - Codegen command
    (`python -m grpc_tools.protoc -I proto --python_out=generated
    --grpc_python_out=generated proto/hub/scripting.proto
    proto/highbar/*.proto`).
  - "Run it" invocation + expected output shape.
- **Validation**: FR-006, FR-009.

### A4. Python requirements pin
- **Path**: `scripts/examples/python/requirements.txt`
- **Contents**: `grpcio>=1.60,<2`, `grpcio-tools>=1.60,<2`.
- **Rationale**: pinned to the current stable `grpcio` line; matches
  the Hub's `Grpc.AspNetCore 2.67.0` wire compatibility.

### A5. (Optional) Committed generated bindings
- **Path**: `scripts/examples/python/generated/*_pb2.py`,
  `*_pb2_grpc.py`.
- **Status**: optional per spec Assumptions. If committed, the README
  MUST still document the regen command so readers can refresh after
  a proto bump.

## Non-entities (explicit)

- No new proto messages — feature is documentation-only on the wire
  contract (FR-013, SC-005).
- No new `.fsi` signature changes — no F# source is added or modified.
- No new runtime state on the Hub — the Python client reads the same
  `BarClient.GameState` surface via the unchanged
  `fsbar.hub.scripting.v1` contract.
- No new tests in `FSBarV1.slnx` — verification is manual per
  Assumptions ("No new CI gates").
