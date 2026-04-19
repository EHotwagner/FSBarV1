# Tasks — Polyglot scripting-client guide + Python reference

**Feature**: `047-polyglot-client-guide`
**Spec**: [spec.md](./spec.md) · **Plan**: [plan.md](./plan.md)

Tests are **not** generated (spec Assumptions: "No new CI gates";
verification is manual against a running Hub, matching the existing
`scripts/examples/*.fsx` model).

---

## Phase 1: Setup

- [X] T001 Create directory `scripts/examples/python/` with an empty
      `.gitkeep` so the tree exists before subsequent tasks write into it
- [X] T002 [P] Add `scripts/examples/python/requirements.txt` pinning
      `grpcio>=1.60,<2` and `grpcio-tools>=1.60,<2`
- [X] T003 [P] Create empty placeholder `docs/scripting-polyglot.md`
      with just the H1 title "Scripting from another language" (content
      lands in US1)

## Phase 2: Foundational (blocking prerequisites)

- [X] T004 Verify `buf breaking` is green against `master` on the
      current branch (baseline for FR-013 / SC-005 — no proto changes
      expected in this feature); record the command used in
      `specs/047-polyglot-client-guide/research.md` if not already
      present
- [X] T005 Document the codegen command in
      `scripts/examples/python/README.md` (prereqs, `pip install -r
      requirements.txt`, the `python -m grpc_tools.protoc …` invocation
      from quickstart.md step 3, and the "run it" line) — satisfies
      FR-006 / FR-009 and unblocks both US1 (which cites it) and US2
      (which depends on the generated bindings)

---

## Phase 3: User Story 1 — Polyglot on-ramp documentation (P1) 🎯 MVP

**Story goal**: A non-F# reader can land on `docs/scripting-polyglot.md`
and, top-to-bottom, generate gRPC bindings for their language, connect
to a running Hub with the right channel options, and understand the
five RPC-capability families without reading F# code.

**Independent test** (US1): open `docs/scripting-polyglot.md` on a
clean machine, follow the Python row of the codegen table, connect
to a running Hub, and issue one unary RPC (`GetMapInfo`) in under
15 minutes (spec US1 AS1).

- [X] T006 [US1] Write "Where the proto files live" section in
      `docs/scripting-polyglot.md` naming `proto/hub/scripting.proto`
      and `proto/highbar/*.proto` as authoritative (FR-001, FR-010)
- [X] T007 [US1] Write "Per-language codegen" table in
      `docs/scripting-polyglot.md` with Python (required row, exact
      `python -m grpc_tools.protoc …` command), Go
      (`protoc-gen-go` + `protoc-gen-go-grpc`), and TypeScript/Node
      (`@grpc/grpc-js` + `@grpc/proto-loader`) rows (FR-012, US3 AS1/AS2)
- [X] T008 [US1] Write "Connect to the Hub" section in
      `docs/scripting-polyglot.md` covering loopback endpoint
      `127.0.0.1:5021`, insecure channel, and the required 64 MiB
      `MaxReceiveMessageSize` / `MaxSendMessageSize` caps — include
      a Python one-liner for the channel options (FR-004, US1 AS3)
- [X] T009 [US1] Write "RPC catalog" section in
      `docs/scripting-polyglot.md` covering the five capability
      families (session lifecycle, state+events stream, map data,
      unit-def queries, command submission) with per-family
      one-sentence purpose, proto message names, unary vs.
      server-streaming, and feature-046 FR anchors (FR-001, FR-002,
      US1 AS2)
- [X] T010 [US1] Write "Enemy health discriminator" section in
      `docs/scripting-polyglot.md` describing `oneof health_info {
      float health; EnemyHealthUnknown unknown; }` totality in prose
      — exactly-one-arm; `health = 0` is a dying reading; `unknown`
      covers radar-only + frozen-last-known. No F# shown (FR-003,
      US1 AS4)
- [X] T011 [US1] Add "See also" cross-links at the bottom of
      `docs/scripting-polyglot.md` pointing to
      `scripts/examples/24-hub-full-client.fsx` (F# sibling),
      `proto/hub/scripting.proto`, and `proto/highbar/*.proto`
      (FR-010)
- [X] T012 [US1] Proofread `docs/scripting-polyglot.md` against
      US1 AS1–AS4 and SC-004 (all five capability families covered
      with purpose + message name + cadence); fix gaps in place

**Checkpoint**: US1 is independently shippable — docs page reads
top-to-bottom without depending on any code artifact from US2.

---

## Phase 4: User Story 2 — Working Python reference client (P1) 🎯 MVP

**Story goal**: A runnable `scripts/examples/python/hub_full_client.py`
that exercises the five capability families end-to-end against a
live Hub in ≤200 LOC, with fast-fail on missing Hub and clean
Ctrl-C shutdown.

**Independent test** (US2): `pip install -r requirements.txt`, run
codegen, start the Hub, run `python scripts/examples/python/
hub_full_client.py` — see 10 ticks printed, map+def+batch
confirmations, and exit 0 in under 2 minutes (SC-002, US2 AS1).

- [X] T013 [US2] Generate the Python bindings into
      `scripts/examples/python/generated/` using the command from T005;
      commit the resulting `*_pb2.py` + `*_pb2_grpc.py` for zero-setup
      runs (spec Assumption: "Committed generated bindings are
      optional" — we opt to commit; FR-009 still satisfied because
      regen command is documented)
- [X] T014 [US2] Create `scripts/examples/python/hub_full_client.py`
      skeleton: module docstring, five clearly-delimited section
      headers (`# --- 1. channel setup ---` … `# --- 5. batch +
      stop ---`), `if __name__ == "__main__": main()` entrypoint,
      with no cross-section state (FR-011, US2 AS3)
- [X] T015 [US2] Implement section 1 (channel setup + fast-fail) in
      `scripts/examples/python/hub_full_client.py`:
      `grpc.insecure_channel("127.0.0.1:5021",
      options=[("grpc.max_receive_message_length", 64*1024*1024),
      ("grpc.max_send_message_length", 64*1024*1024)])`; wrap a cheap
      first unary call — the section-2 `ConfigureLobby` invocation,
      which is the script's first RPC anyway — in `try/except
      grpc.RpcError`. On `StatusCode.UNAVAILABLE` print `"could not reach
      127.0.0.1:5021 — is the Hub running?"` and `sys.exit(1)`
      (no separate "probe" call; fast-fail piggybacks on the real
      first RPC). (FR-004, FR-007, US2 AS2)
- [X] T016 [US2] Implement section 2 (`ConfigureLobby` +
      `LaunchSession`) in `scripts/examples/python/hub_full_client.py`
      with a minimal lobby config that selects Avalanche 3.4 and
      two players (one friendly, one enemy AI) — print the returned
      session id (FR-005)
- [X] T017 [US2] Implement section 3 (`StreamGameFrames` × 10 ticks)
      in `scripts/examples/python/hub_full_client.py`: iterate the
      server stream, for each `GameStateFrame` print frame number +
      friendly/enemy unit counts; iterate any `GameEventEnvelope[]`
      on the tick and print an event-kind summary; break after 10
      ticks (FR-005, US2 AS1)
- [X] T018 [US2] Implement section 4 (map + unit-def queries) in
      `scripts/examples/python/hub_full_client.py`: call
      `GetMapInfo` (print width/height/map_name), `ListMetalSpots`
      (print count), `GetUnitDefExtended("armcom")` (print cost +
      sight range + build-option count) (FR-005, US2 AS1)
- [X] T019 [US2] Implement section 5 (batch command + stop) in
      `scripts/examples/python/hub_full_client.py`: build one no-op
      `AICommand`, call `SendCommandBatch([cmd])`, assert a single
      `CommandOutcome` accepted, then call `StopSession` (FR-005,
      US2 AS1)
- [X] T020 [US2] Add SIGINT handler + `try/finally` in
      `scripts/examples/python/hub_full_client.py` that cancels the
      streaming iterator and calls `StopSession` before exit — so
      Ctrl-C leaves the Hub session state back at Idle within 15 s
      (FR-008, US2 AS4, SC-006)
- [X] T021 [US2] Enforce the ≤200 LOC budget on
      `scripts/examples/python/hub_full_client.py` (excluding
      `generated/`): run `wc -l`; if over, collapse comments /
      short-circuit helpers without breaking section delimiters or
      FR-011 independence (SC-003)
- [X] T022 [US2] Extend `scripts/examples/python/README.md` with an
      "Expected output" block mirroring quickstart.md §5 and a
      "Troubleshooting" table (unreachable Hub, message-size cap,
      Ctrl-C behavior) (FR-006)
- [X] T023 [US2] Live-run validation: start the Hub (`/hub-run`),
      run the Python client twice back-to-back, confirm US2 AS1
      output shape, US2 AS4 Ctrl-C cleanup (SC-006), and SC-002
      (<2 min wall-clock). Additionally, time one docs-first
      walkthrough — from `docs/scripting-polyglot.md` → README →
      first live frame — on a reviewer's machine to produce SC-001
      evidence (<30 min target). Record both timings in the PR
      description

**Checkpoint**: US2 is independently shippable — the script runs
without the docs page, though the docs page makes it
self-onboarding for new readers.

---

## Phase 5: User Story 3 — Per-language codegen matrix (P3)

**Story goal**: Docs' codegen table covers Go and TypeScript/Node
beyond the Python row shipped in US1.

**Independent test** (US3): open `docs/scripting-polyglot.md`, scan
the codegen table, follow the Go row → `protoc` produces compilable
bindings in the documented directory (US3 AS1); follow the TS/Node
row → `@grpc/grpc-js` + `@grpc/proto-loader` produces usable
bindings (US3 AS2).

> Note: US1's T007 already introduces the table with all three rows
> so the docs ship coherent. This phase is the **deeper validation**
> and cleanup pass specific to US3.

- [ ] T024 [P] [US3] Validate the Go row in
      `docs/scripting-polyglot.md`: run the documented `protoc
      --go_out … --go-grpc_out … proto/hub/*.proto
      proto/highbar/*.proto` command in a scratch dir and confirm
      bindings compile with `go build ./...`; fix the docs command
      if it drifts (US3 AS1)
- [ ] T025 [P] [US3] Validate the TypeScript/Node row in
      `docs/scripting-polyglot.md`: in a scratch dir, `npm i
      @grpc/grpc-js @grpc/proto-loader` and run the documented
      load/generate path against `proto/hub/scripting.proto` +
      `proto/highbar/*.proto`; fix the docs command if it drifts
      (US3 AS2)
- [X] T026 [US3] Add a one-line "expected output directory" note
      per row in the codegen table of `docs/scripting-polyglot.md`
      so readers know where generated artifacts will land (US3
      Independent Test criterion)

**Checkpoint**: US3 is independently shippable — Go/TS rows in the
table are validated without touching `hub_full_client.py`.

---

## Phase 6: Polish & Cross-Cutting

- [X] T027 Cross-reference pass: ensure `docs/scripting-polyglot.md`
      cross-links to the Python example and vice-versa (example's
      `README.md` links back to the polyglot docs page) (FR-010)
- [X] T028 [P] Confirm no F# source / `.fsi` / surface-area baseline
      was modified by this feature: `git diff --stat master...HEAD
      -- 'src/**' 'tests/**'` should be empty (Constitution §II,
      plan Constitution Check)
- [X] T029 [P] Confirm `buf breaking` against `master` is still
      green (FR-013, SC-005)
- [X] T030 Update `CLAUDE.md`'s "Recent Changes" / Active
      Technologies to mention feature 047 (Python scripting-client
      on-ramp) — one-line addition only
- [ ] T031 Bump patch versions on FSBar.{Client,Proto,Hub,
      SyntheticData,Viz} only if any packaged surface actually
      changed; if this feature stays docs-only (expected), skip and
      record the skip in the PR description
- [X] T032 [P] Assert FR-014 (no FSBar F# runtime dependency in the
      Python example) by grepping
      `scripts/examples/python/` — excluding `generated/` — for the
      strings `FSBar.` and `.nupkg`; both counts MUST be zero.
      Record the commands + output in the PR description

---

## Dependencies

- **Phase 1 (T001–T003)** → unblocks Phase 2.
- **Phase 2 (T004–T005)** → unblocks US1 (docs cites the codegen
  command from T005) and US2 (bindings regen procedure).
- **US1 (Phase 3)** and **US2 (Phase 4)** are independent of each
  other once Phase 2 is done. Either can ship first; recommended
  order is US1 then US2 for reviewer flow, but they parallelize.
- **US3 (Phase 5)** depends on US1's T007 (the table exists);
  otherwise independent of US2.
- **Polish (Phase 6)** runs last.

Completion order (minimum → full):
`Setup → Foundational → US1 → US2 → US3 → Polish`.
MVP = `Setup → Foundational → US1 → US2` (both P1 stories).

## Parallel execution opportunities

- **Within Phase 1**: T002 and T003 run in parallel with each other
  after T001.
- **Within US1 (Phase 3)**: T006, T007, T008, T009, T010, T011 all
  touch the same file (`docs/scripting-polyglot.md`) and therefore
  are NOT [P] — serialize them to avoid merge churn. T012 runs last.
- **Within US2 (Phase 4)**: all section-implementation tasks
  (T015–T020) touch the same script and are serialized. T013
  (generated/) is independent and can run in parallel with T014
  (skeleton) once T005 is done.
- **US1 ↔ US2**: parallel at the story level (different files).
- **Within US3 (Phase 5)**: T024 and T025 are [P] (different
  scratch dirs, independent validations). T026 follows after both.
- **Polish (Phase 6)**: T028, T029, and T032 are [P] (read-only verifications).

## Implementation strategy

1. **MVP first**: deliver Phase 1 + Phase 2 + US1 + US2 — this is
   the complete user-visible feature. Both P1 stories are required
   for the "docs + working code" pairing the spec calls out.
2. **Incremental**: US3 (Go/TS validation) can land in a follow-up
   PR if time-constrained; the table rows already ship in US1/T007,
   so the feature remains coherent without the deep validation.
3. **Polish**: run Phase 6 immediately before opening the PR — the
   no-F#-diff and `buf breaking` checks are PR-readiness gates.

## Independent test criteria (summary)

| Story | Independent test |
|---|---|
| US1 | Reader follows docs only; issues `GetMapInfo` via Python in <15 min (spec AS1). |
| US2 | `python hub_full_client.py` completes end-to-end in <2 min, exits 0; Ctrl-C leaves Hub at Idle within 15 s (SC-002, SC-006). |
| US3 | Go and TS codegen commands in the docs table produce compilable/usable bindings in scratch dirs (AS1, AS2). |

## Suggested MVP scope

Phase 1 + Phase 2 + Phase 3 (US1) + Phase 4 (US2). That is: docs
page + runnable Python client. US3 is deferrable.
