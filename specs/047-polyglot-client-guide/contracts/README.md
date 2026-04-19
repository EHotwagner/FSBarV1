# Contracts — Polyglot scripting-client guide

This feature adds **no new wire contracts**. The authoritative proto
files are unchanged and remain:

- `proto/hub/scripting.proto` — `fsbar.hub.scripting.v1`
  (`ScriptingService`).
- `proto/highbar/*.proto` — shared message types (`GameStateFrame`,
  `GameEventEnvelope`, map-grid messages, `AICommand`, etc.).

The Python reference client and the polyglot docs page consume this
surface *as-is*; no fields, messages, or RPCs are added, removed, or
renamed. `buf breaking` MUST continue to report zero incompatibilities
after this feature lands (spec FR-013 / SC-005).

## Wire-contract anchors (for traceability)

The docs page (`docs/scripting-polyglot.md`) cites these feature-046
FR anchors in its RPC catalog. They are pointers into
`specs/046-scripting-full-client/`, not new contracts:

| Capability family | Primary RPC(s) | FR anchor (feature 046) |
|---|---|---|
| Session lifecycle | `ConfigureLobby`, `LaunchSession`, `StopSession`, admin ops | feature 039 autohost + 046 session mgmt |
| State + events stream | `StreamGameFrames` | FR-001 / FR-002 |
| Enemy health discriminator | `oneof health_info` | FR-003 |
| Map data (grids + metal spots) | `GetMapInfo`, `GetHeightmap`, …, `ListMetalSpots` | FR-004 / FR-006 |
| Unit-def queries | `GetUnitDefExtended` | FR-005 |
| Command submission | `SendCommandBatch` (≤1024, 1:1 outcomes) | FR-007 |

## Reference-client contract (documentation-only)

The Python example's *behavioral* contract is captured by the spec's
User Story 2 acceptance scenarios and Success Criteria SC-001…SC-006.
That is the full contract for the client; this directory intentionally
contains no proto-style schema because the client is a consumer of the
existing proto surface.
