# Implementation Plan: Polyglot scripting-client guide + Python reference

**Branch**: `047-polyglot-client-guide` | **Date**: 2026-04-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/047-polyglot-client-guide/spec.md`

## Summary

Document how to drive `fsbar.hub.scripting.v1` from non-F# languages and
ship a working Python reference client. Deliverables are additive-only:
a new Markdown guide under `docs/` and a self-contained script tree
under `scripts/examples/python/`. No proto, server, or F# source
changes; no new NuGet dependencies; no CI gates added.

Technical approach: reuse the existing loopback Hub scripting endpoint
(`127.0.0.1:5021`, 64 MiB message cap). Python client uses the standard
`grpcio` + `grpcio-tools` stack — the same libs Google's own protobuf
docs recommend — to generate `*_pb2.py` / `*_pb2_grpc.py` from the
current `proto/hub/*.proto` + `proto/highbar/*.proto` tree. The script
walks the five capability families (lifecycle, state+events stream,
map data, unit-def query, batch command) linearly, with section
delimiters so each block is copy-paste liftable.

## Technical Context

**Language/Version**: Markdown for the docs page; Python 3.10+ for
the reference client (modern type hints, `asyncio` optional).
**Primary Dependencies**: `grpcio` (≥1.60), `grpcio-tools` (for
codegen). Hub side is unchanged (`FsGrpc 1.0.6`, `Grpc.AspNetCore
2.67.0`). No F# or .NET dependencies in the Python client per FR-014.
**Storage**: N/A — the Python client is stateless; live session state
lives in `BarClient.GameState` on the Hub side.
**Testing**: Manual verification against a running Hub, matching how
the F# FSI walkthroughs (`scripts/examples/16..24-hub-*.fsx`) are
validated today. No addition to `dotnet test`.
**Target Platform**: Loopback Linux (primary); guide is OS-neutral
where codegen commands are standard.
**Project Type**: Documentation + standalone external client example
(separate from the F# solution — permitted under the constitution's
"multi-language needs via gRPC" clause in Engineering Constraints).
**Performance Goals**: SC-001 (first-light in <30 min), SC-002 (full
Python run <2 min wall-clock), SC-003 (≤200 LOC excluding generated
bindings).
**Constraints**: FR-004 `MaxReceiveMessageSize = 64 MiB` on client
channel. FR-007 fast-fail on unreachable Hub (no hang, no multi-page
traceback). FR-008 Ctrl-C cancels streaming RPC + calls `StopSession`
before exit.
**Scale/Scope**: One docs page, one Python script + README (+
optionally committed generated bindings), zero F# source changes.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec-First Delivery | PASS | Spec and this plan in place; acceptance criteria and scope boundaries explicit. No code-without-spec. |
| II. Compiler-Enforced Structural Contracts | N/A | No F# source added or changed; no `.fsi` surface moves; surface-area baselines unaffected. |
| III. Test Evidence Is Mandatory | PASS (scoped) | Verification for US1/US2 is manual against a running Hub (explicit FR-scoped assumption: "No new CI gates"). This matches the existing `scripts/examples/*.fsx` validation model. No behavior-changing F# code is added that would otherwise demand automated tests. |
| IV. Observability and Safe Failure Handling | PASS | Python client fails fast with actionable diagnostics on unreachable Hub (FR-007) and installs signal handler for clean `StopSession` (FR-008). No silent failures introduced. |
| V. Scripting Accessibility | PASS (reinforced) | Feature *expands* scripting accessibility to a second language. F# prelude / numbered examples remain authoritative (docs cross-link to `scripts/examples/24-hub-full-client.fsx` per FR-010). |

**Engineering Constraints**:
- "F# on .NET is the exclusive stack … multi-language needs MUST be
  addressed by separate projects communicating via gRPC" — the Python
  client is a *separate project* under `scripts/examples/python/`
  talking to the Hub over gRPC; no Python code enters any `src/` F#
  project. Compliant.
- "No new NuGet dependencies" — none added.
- `buf breaking` stays green (FR-013 / SC-005).

**Verdict**: No violations. No Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/047-polyglot-client-guide/
├── plan.md              # This file
├── spec.md              # Feature spec (already present)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (trivial — no new entities)
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (pointers only — no new wire contracts)
└── tasks.md             # Phase 2 (NOT produced by /speckit.plan)
```

### Source Code (repository root)

```text
docs/
└── scripting-polyglot.md          # NEW — "Scripting from another language"
                                    #  • proto file locations
                                    #  • per-language codegen table (Python required; Go, TS nice-to-have)
                                    #  • connection + 64 MiB message-cap guidance
                                    #  • RPC catalog (5 capability families) with
                                    #    feature-046 FR anchors
                                    #  • prose description of the `oneof health_info` totality rule
                                    #  • cross-link to scripts/examples/24-hub-full-client.fsx

scripts/examples/python/           # NEW directory
├── README.md                      # prerequisites, codegen one-liner, run invocation
├── hub_full_client.py             # linear walkthrough of the 5 capability families (≤200 LOC)
├── requirements.txt               # grpcio, grpcio-tools pins
└── generated/                     # (optional) committed *_pb2.py / *_pb2_grpc.py for zero-setup

# Unchanged — referenced, not modified:
proto/hub/scripting.proto
proto/highbar/*.proto
src/FSBar.*                        # no edits
scripts/examples/24-hub-full-client.fsx   # F# sibling, cross-linked
```

**Structure Decision**: Docs land at `docs/scripting-polyglot.md`
(sibling to the existing `docs/scripting.fsx` / `docs/hub.fsx` pages).
Python example lives under `scripts/examples/python/`, deliberately
separated from the numbered `.fsx` walkthroughs so there is no naming
collision and so language-specific onboarding (README, generated
bindings) has a natural home. This structure matches the
constitution's "multi-language via separate project" requirement: the
Python tree is a standalone client, not an F# project, and has no
build-time coupling to `FSBarV1.slnx`.

## Complexity Tracking

> No Constitution Check violations. Section intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
