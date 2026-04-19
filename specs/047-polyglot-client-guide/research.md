# Research — Polyglot scripting-client guide + Python reference

Phase 0 consolidates decisions for technology and documentation choices
that the spec leaves deliberately open. No NEEDS CLARIFICATION markers
remained after this pass.

## 1. Python gRPC toolchain

- **Decision**: `grpcio` + `grpcio-tools` (`python -m grpc_tools.protoc`)
  as the documented codegen path.
- **Rationale**: These are the official Google-maintained Python
  bindings, the same ones referenced by `grpc.io`'s tutorials. They
  produce `*_pb2.py` / `*_pb2_grpc.py` that work without extra runtime
  glue and honor standard channel options (including
  `grpc.max_receive_message_length`), which we need for FR-004's 64 MiB
  cap.
- **Alternatives considered**:
  - `betterproto` — nicer dataclass ergonomics, but (a) it's
    third-party, (b) its feature coverage of `oneof` has historically
    lagged, and (c) making it the documented path would fork readers
    away from Google's own examples. Not worth the maintenance tax
    for a reference client.
  - `grpclib` + `protoc-gen-python-betterproto` — async-first, but
    extra moving parts for a script whose goal is "shortest path to
    trust".

## 2. Docs location + format

- **Decision**: New file `docs/scripting-polyglot.md`, sibling to the
  existing `docs/scripting.fsx`, `docs/hub.fsx`, `docs/library.fsx`
  pages.
- **Rationale**: The repo already keeps a flat `docs/` tree with a
  mix of Markdown (`index.md`, `known-issues.*`) and `.fsx`
  walkthroughs. A Markdown page for a non-F# audience fits cleanly
  and avoids polluting the F#-scripting docs with
  language-switching noise.
- **Alternatives considered**: inlining the section into `CLAUDE.md`
  — rejected because `CLAUDE.md` is the assistant-facing guide and
  this content is end-user docs; also `CLAUDE.md` is already dense
  and would dilute signal.

## 3. Example directory layout

- **Decision**: `scripts/examples/python/` with one script
  (`hub_full_client.py`), one `README.md`, one `requirements.txt`,
  and an optional committed `generated/` subdir.
- **Rationale**: Mirrors the F# walkthrough convention (numbered
  `.fsx` files under `scripts/examples/`) while giving Python its own
  namespaced home. `generated/` as a subdir keeps the hand-written
  script file easy to spot and makes "zero-setup run" a checkbox for
  readers who don't want to install `grpcio-tools` yet — consistent
  with the spec Assumption ("Committed generated bindings are
  optional").
- **Alternatives considered**: a single top-level `python/` dir
  (rejected — clutters repo root for a low-frequency example); a
  numbered `.py` sibling inside `scripts/examples/` (rejected —
  language-specific deps and generated code deserve their own
  folder).

## 4. Loopback + auth posture

- **Decision**: Document the Hub as loopback-only, no TLS, no auth,
  `127.0.0.1:5021`. The Python client connects with
  `grpc.insecure_channel(...)` and `grpc.max_receive_message_length`
  + `grpc.max_send_message_length` set to `64 * 1024 * 1024`.
- **Rationale**: Matches the existing Hub deployment model (feature
  042 / 046). Remote + authenticated scripting is explicitly out of
  scope per the spec's Assumptions.
- **Alternatives considered**: showing a TLS variant — deferred;
  would invite confusion about which channel options to use today
  when the server doesn't serve TLS.

## 5. Ctrl-C / shutdown handling

- **Decision**: Python client installs a `signal.signal(SIGINT, …)`
  handler that (a) cancels the in-flight `StreamGameFrames` iterator,
  (b) calls `StopSession`, (c) exits 0. A `try/finally` around the
  streaming loop guarantees `StopSession` runs even on exceptions.
- **Rationale**: FR-008 + SC-006 require that a stale session doesn't
  block the next `LaunchSession`. The Hub's session state machine
  (feature 042) returns to Idle within a few seconds after
  `StopSession`, comfortably inside the 15-second SC-006 budget.
- **Alternatives considered**: relying on the Hub's own timeout to
  reap dead sessions — rejected because it's slower, user-surprising,
  and SC-006 expects a clean shutdown on Ctrl-C.

## 6. Error-diagnostic shape for unreachable Hub

- **Decision**: Wrap the first RPC (`ConfigureLobby` or a cheap
  `GetHubStatus`-equivalent unary) in a `try`/`except grpc.RpcError`
  that checks for `StatusCode.UNAVAILABLE` and prints a single line:
  `"could not reach 127.0.0.1:5021 — is the Hub running? (run: /skill hub-run)"`
  then exits non-zero.
- **Rationale**: FR-007 requires human-readable fast-fail. Pointing
  at the `/hub-run` skill ties the diagnostic back to the repo's own
  onboarding.
- **Alternatives considered**: pre-flight TCP probe — rejected
  because it duplicates logic that gRPC already signals via
  `UNAVAILABLE`, and the gRPC surface is the one the reader is here
  to learn.

## 7. Which RPCs the example exercises

- **Decision**: `ConfigureLobby` → `LaunchSession` → 10 frames of
  `StreamGameFrames` (read `game_state` + `game_events`) →
  `GetMapInfo` → `ListMetalSpots` → `GetUnitDefExtended("armcom")` →
  `SendCommandBatch` with ≥1 no-op `AICommand` → `StopSession`.
- **Rationale**: Hits all five capability families named in FR-001
  / FR-005; `armcom` is a universally available commander def across
  BAR maps, so the script doesn't need map-specific logic.
- **Alternatives considered**: Exercising every RPC (`GetHeightmap`,
  `GetSlopeMap`, etc.) — rejected; SC-003 caps the script at 200 LOC
  for readability, and the docs' RPC catalog already enumerates the
  full surface.

## 8. Per-language codegen table (FR-012 / US3)

- **Decision**: Include Python (required row), Go, and
  TypeScript/Node rows. Go uses `protoc-gen-go` +
  `protoc-gen-go-grpc`. TS uses `@grpc/grpc-js` +
  `@grpc/proto-loader` as the recommended path (one-tool, no
  separate codegen step at dev time).
- **Rationale**: US3 is P3, so go broad-but-shallow: one commandline
  per row with an expected output directory, no deep language-by-
  language tutorial.
- **Alternatives considered**: naming `buf generate` — mentioned in
  passing but not the headline path, because it adds a
  configuration-file hop that obscures the simpler `protoc` story.

## 9. `buf breaking` baseline

- **Decision**: `buf breaking proto --against
  '.git#branch=master,subdir=proto'` run from the repo root is the
  canonical invocation for FR-013 / SC-005. Verified green on the
  047 branch (no proto changes expected in this feature).

## 10. Constitution "F# is exclusive" vs. Python script

- **Decision**: Land the Python tree under
  `scripts/examples/python/` as a standalone client that talks to
  the Hub over gRPC. It is explicitly not a project inside
  `FSBarV1.slnx`, has no `.fsproj`, and is not built by `dotnet
  build`.
- **Rationale**: Engineering Constraints allow multi-language needs
  "addressed by separate projects communicating via gRPC". The Python
  tree satisfies that by construction.
- **Alternatives considered**: placing the Python code in a separate
  Git repo — rejected because colocation with the proto files and
  the F# walkthrough makes cross-reference trivial and keeps
  regeneration instructions one directory away from the
  authoritative `.proto`.
