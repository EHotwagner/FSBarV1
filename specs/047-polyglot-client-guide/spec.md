# Feature Specification: Polyglot scripting-client guide + Python reference

**Feature Branch**: `047-polyglot-client-guide`
**Created**: 2026-04-19
**Status**: Draft
**Input**: User description: "create a doc section how to create a scripting client in a different language that has grpc support, like python. explain the proto services. create a rudimentary python client."

## Context

Feature 046 made `fsbar.hub.scripting.v1` a fully-fledged headless BAR
client contract — a gRPC caller in any supported language can read
per-tick `GameStateFrame`, subscribe to typed `GameEventEnvelope`s,
query map data, look up extended unit defs, and batch-submit
`AICommand`s. The existing FSI walkthroughs
(`scripts/examples/16..24-hub-*.fsx`) prove this contract in F#.

What's missing is the *documented on-ramp* for non-F# authors. A
developer who opens the repo today finds:

- Proto files (`proto/hub/scripting.proto`, `proto/highbar/*.proto`)
  with no "here's how to generate bindings for language X" guidance.
- F#-only FSI walkthroughs whose imports and record-construction
  style don't map cleanly onto Python / Go / TypeScript idioms.
- A `CLAUDE.md` scripting section that describes the contract but
  not how to start from zero in a generic language.

This feature closes that gap: a "Scripting from another language"
documentation section explaining the RPC surface in language-neutral
terms, plus a small, working Python reference client that exercises
the five most important capabilities (launch / observe / map query /
unit def / batch command) against a running Hub.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Polyglot on-ramp documentation (Priority: P1) 🎯 MVP

As a developer comfortable with Python, Go, or TypeScript who has
**no prior F# exposure**, I want a focused docs page that explains
how to generate gRPC bindings from the repo's proto files, where to
point my client (loopback endpoint + message-size limits), and a
language-neutral tour of the `ScriptingService` RPC surface so I
can write my own client without reading F# code.

**Why this priority**: Without docs, every non-F# adopter has to
reverse-engineer the wire contract from proto files and F#
examples. This is the one change that makes the feature-046 surface
truly polyglot. A reference implementation (US2) is only useful
once a reader knows *where to point it*.

**Independent Test**: A reader can follow the docs from a clean
machine, generate Python (or Go, or TypeScript) bindings from the
`.proto` files, and write a working "call `GetMapInfo`" snippet
without needing any F# tooling installed.

**Acceptance Scenarios**:

1. **Given** a fresh checkout and a reader who has never opened an
   F# file, **When** they follow the docs page from top to bottom,
   **Then** they can (a) locate the proto files, (b) run the
   documented codegen command for their language, (c) connect to a
   running Hub and issue one unary RPC, in under 15 minutes.
2. **Given** a reader writing a bot, **When** they consult the
   docs' "RPC catalog", **Then** for each of the 5 capability
   families (session lifecycle, state + events stream, map data,
   unit-def queries, command submission) they see: one-sentence
   purpose, request/response message names, cadence / one-shot,
   and the feature-046 FR-citation anchoring the wire contract.
3. **Given** a reader who hits the 4 MiB default gRPC message cap,
   **When** they consult the docs, **Then** they find the explicit
   "raise `MaxReceiveMessageSize` to 64 MiB" guidance that matches
   the Hub server's configuration (feature-046 FR-006) and a
   per-language code snippet showing how to apply it.
4. **Given** a reader confused about the `oneof health_info`
   discriminator, **When** they consult the docs, **Then** they see
   the feature-046 FR-003 totality rule ("exactly one arm is always
   set; `health = 0` is a legitimate 'dying' reading; `unknown`
   covers radar-only + frozen-last-known") restated in prose, not
   F#.

---

### User Story 2 — Working Python reference client (Priority: P1) 🎯 MVP

As a Python developer evaluating the scripting surface, I want a
small, self-contained Python script committed under
`scripts/examples/python/` that I can run against a live Hub to see
end-to-end behavior: launch a session, stream 10 ticks of game
state + events, query the map, look up a unit def, submit a batched
command. The script should be short enough to read in one sitting
and structured so each capability is easy to lift into a real bot.

**Why this priority**: Concrete, runnable code is the shortest path
from "I read the docs" to "I trust the contract". A working Python
client also serves as an executable compatibility gate — if a proto
change breaks it, we catch that regression outside the F# test
suite. Same P1 tier as US1 because they're mutually reinforcing:
docs without code are abstract; code without docs is a cargo-cult.

**Independent Test**: A reader with Python 3.10+ and `pip` on their
PATH follows the example's README, runs one `pip`-plus-codegen
setup step, launches the Hub, and runs
`python scripts/examples/python/hub_full_client.py` — seeing a
trace of 10 ticks of live state plus a confirmation that a command
batch was forwarded.

**Acceptance Scenarios**:

1. **Given** a running Hub on `127.0.0.1:5021` with Avalanche 3.4
   installed, **When** the reader runs the Python example after
   generating bindings, **Then** within ~90 seconds the script
   prints (a) per-tick frame number + friendly/enemy counts for 10
   ticks, (b) `GetMapInfo` width/height/map_name, (c)
   `ListMetalSpots` count, (d) `GetUnitDefExtended("armcom")` cost
   + sight range + build-option count, (e) a
   `SendCommandBatch`-accepted confirmation line, and exits 0.
2. **Given** no Hub is running, **When** the reader runs the
   script, **Then** it fails fast with a clear "could not reach
   127.0.0.1:5021 — is the Hub running?" message rather than
   hanging or raising a confusing gRPC traceback.
3. **Given** a reader who wants to tweak one capability (e.g., use
   a different map or run their own command batch), **When** they
   open the script, **Then** each of the 5 sections is delimited
   by a clear comment header and is self-contained (no
   cross-section state) so a copy-paste lift works without
   unraveling helpers.
4. **Given** a developer running the script twice in a row,
   **When** each run completes, **Then** the session is cleanly
   stopped on exit (including on Ctrl-C) and a subsequent
   `LaunchSession` call succeeds.

---

### User Story 3 — Per-language codegen matrix (Priority: P3)

As a maintainer, I want the docs to include a short table listing
the commands to generate client bindings for at least three
gRPC-capable languages (Python, Go, TypeScript/Node) from the same
proto files, so a reader's "I want to try this in Go" question
answers itself without opening an issue.

**Why this priority**: Nice-to-have, not blocking. Python alone
covers the largest bot-author audience. Adding Go and TypeScript
expands reach but doesn't change the underlying contract. P3
because it's a surface-level addition over US1's single-language
example.

**Independent Test**: A reader opens the docs page, scans the
codegen table, and can select their language's row — each row
names the package manager (`pip`, `go get`, `npm`), the codegen
command, and the expected output directory.

**Acceptance Scenarios**:

1. **Given** a reader on Linux, **When** they follow the Go row,
   **Then** `protoc --go_out … --go-grpc_out …
   proto/hub/*.proto proto/highbar/*.proto` produces compilable
   bindings in the documented directory.
2. **Given** a reader on any OS, **When** they follow the
   TypeScript/Node row, **Then** `@grpc/grpc-js` and
   `@grpc/proto-loader` (or a `buf generate` alternative) produce
   usable bindings; the docs name exactly one recommended path.

---

### Edge Cases

- **Hub not running**: Python client emits a single-line
  diagnostic and exits non-zero; it MUST NOT hang or print a
  multi-page gRPC traceback (US2 AS2).
- **Proto files out of sync with server**: Doc mentions that if
  codegen predates a server upgrade, `buf breaking` is the
  authoritative check; clients silently drop unknown fields per
  protobuf semantics, so most additive-only changes "just work"
  without regeneration.
- **Large maps**: Default gRPC client caps at 4 MiB receive;
  without raising the limit, heightmap/corners/resource RPCs will
  fail on real maps. Docs MUST call this out explicitly with the
  64 MiB number matching the server (FR-004).
- **Ctrl-C in the middle of streaming**: Python client installs a
  signal handler that cancels the streaming RPC and calls
  `StopSession` before exiting (US2 AS4).
- **Map name ≠ lobby map name**: Docs note that the `map_name`
  string is the lobby-side name (e.g., "Avalanche 3.4"), not the
  map file stem, to head off confusion when cross-referencing the
  BAR data dir.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The repository MUST carry a docs page (new file
  under `docs/` or a named section in an existing docs tree)
  titled "Scripting from another language" that covers: where the
  proto files live, how to generate bindings, where to point a
  gRPC channel, and a walkthrough of the 5 RPC-capability
  families (session lifecycle, state + events stream, map data,
  unit-def queries, command submission).
- **FR-002**: The docs page MUST list, for each capability family,
  the proto message names a client needs, whether each RPC is
  unary / server-streaming / bidirectional, and which feature-046
  FR anchors its wire contract.
- **FR-003**: The docs page MUST explicitly describe the
  `oneof health_info { float health; EnemyHealthUnknown unknown;
  }` discriminator in prose (no F# shown): exactly-one-arm
  totality; `health = 0` is "dying"; `unknown` is "radar-only or
  frozen-last-known".
- **FR-004**: The docs page MUST instruct readers to raise
  `MaxReceiveMessageSize` on their client channel to at least
  64 MiB to match the server, with a per-language one-liner for
  Python at minimum.
- **FR-005**: A working Python reference client MUST ship under
  `scripts/examples/python/hub_full_client.py` (or a similarly
  named path under that directory) and exercise, end-to-end
  against a running Hub: `ConfigureLobby`, `LaunchSession`, a
  10-tick subscription to `StreamGameFrames` reading `game_state`
  + `game_events`, `GetMapInfo`, `ListMetalSpots`,
  `GetUnitDefExtended` for a known def, `SendCommandBatch` with
  ≥1 command, and `StopSession`.
- **FR-006**: The Python example MUST include a `README.md` in
  the same directory naming prerequisites (`python 3.10+`, a
  one-line `pip install` list, the `grpc_tools.protoc` codegen
  command) and a "run it" invocation.
- **FR-007**: The Python example MUST fail fast with a
  human-readable diagnostic when the Hub is not reachable on the
  configured endpoint — no hang, no multi-page gRPC traceback to
  the user.
- **FR-008**: The Python example MUST handle Ctrl-C by cancelling
  any in-flight streaming RPC and invoking `StopSession` before
  exiting.
- **FR-009**: The generated Python bindings MUST be produced by a
  documented, repeatable command (e.g., `python -m
  grpc_tools.protoc …`) — generated artifacts MAY be committed
  for convenience, but the regen step MUST be documented so
  readers can refresh them.
- **FR-010**: The docs page MUST cross-link to
  `scripts/examples/24-hub-full-client.fsx` as the F# sibling,
  and to `proto/hub/scripting.proto` + `proto/highbar/*.proto` as
  the authoritative wire contracts.
- **FR-011**: The Python example's layout (section delimiters, no
  cross-section state) MUST let a reader lift any one of the 5
  capability sections into their own code without dragging in the
  others (US2 AS3).
- **FR-012**: The docs MUST include a short "Per-language
  codegen" table covering at least Python with the exact command;
  Go and TypeScript/Node rows are nice-to-have (US3).
- **FR-013**: Nothing in this feature modifies `proto/` or any
  existing server code. The surface is additive-to-documentation
  only. `buf breaking` MUST continue to report zero breaking
  changes after this feature lands.
- **FR-014**: The Python example MUST NOT require any FSBar F#
  assembly or nupkg at runtime — its only FSBar-side dependencies
  are the proto files.

### Key Entities

- **Docs page**: Markdown (or the repo's existing docs format)
  titled "Scripting from another language". Location decided at
  planning time (likely `docs/scripting/polyglot.md` or a section
  in `CLAUDE.md`).
- **Python reference client**: A single-file (or at most 2-file)
  script under `scripts/examples/python/`. Reads as a linear
  walk-through of the 5 capability families.
- **Python example README**: A short README adjacent to the
  script naming prerequisites and the run invocation.
- **Per-language codegen table**: Section inside the docs page
  listing the exact codegen command per supported language.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer with Python experience but no F#
  exposure can go from `git clone` to "seeing live game state in
  their terminal" in under 30 minutes, following only the new
  docs page and the example README.
- **SC-002**: The Python example completes its full script —
  launch → 10 ticks → map / def query → batch command → stop —
  in under 2 minutes wall-clock on a reference machine, exiting
  0.
- **SC-003**: The Python example is ≤200 lines of code (excluding
  generated bindings) — short enough to read in one sitting.
- **SC-004**: The docs page covers all 5 RPC-capability families
  with at least a one-sentence description + request/response
  message name + cadence per family.
- **SC-005**: `buf breaking` against `master` continues to report
  zero incompatibilities — this feature changes nothing on the
  wire.
- **SC-006**: A reader who Ctrl-Cs the Python example sees a
  clean shutdown (session back to Idle within 15 seconds) rather
  than a stale session that blocks the next launch.

## Assumptions

- **Feature 046 has landed** and the `fsbar.hub.scripting.v1`
  surface is the one described in `specs/046-scripting-full-client/
  contracts/scripting.proto.md`. No proto changes in this feature.
- **Loopback-only deployment.** The Hub scripting service listens
  on `127.0.0.1:5021` without TLS or auth, per the existing Hub
  deployment model. The Python example uses the same loopback
  endpoint. Remote / authenticated scripting is out of scope
  here.
- **Python 3.10+.** No support for older Pythons; the example
  uses modern type hints and `asyncio` idioms where they improve
  clarity.
- **`grpcio` + `grpcio-tools`** are the baseline Python libs —
  same ones Google's own protobuf examples use. Third-party
  alternatives (`betterproto`, `grpclib`) are not the documented
  path.
- **Committed generated bindings are optional.** Generated
  `*_pb2.py` / `*_pb2_grpc.py` files MAY be committed under the
  example directory for zero-setup runs, but the README MUST show
  how to regenerate them. The canonical source is the `.proto`
  files.
- **Docs format follows the repo's existing convention.** If the
  repo has `docs/` Markdown files, the new page lands there; if
  docs live inline in `CLAUDE.md` or similar, it becomes a new
  section there. Planning phase resolves the exact location.
- **No new CI gates.** The Python example is not added to `dotnet
  test`. Verification is manual against a running Hub, matching
  how the F# FSI walkthroughs (`16..24-hub-*.fsx`) are validated
  today.
- **Python example exercises the happy path.** Exhaustive error
  handling and retry loops are out of scope — the goal is a
  readable template, not a production client.
