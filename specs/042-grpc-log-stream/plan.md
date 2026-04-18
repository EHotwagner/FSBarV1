# Implementation Plan: Comprehensive gRPC Logging Stream for Hub Diagnostics

**Branch**: `042-grpc-log-stream` | **Date**: 2026-04-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/042-grpc-log-stream/spec.md`

## Summary

Feature 040 exposed the Hub's user-facing actions over gRPC and feature 041
wired overlays, tab-state routing, and live coverage. Between those two
features the only remote visibility into Hub internals is
`StreamHubStateEvents` — which is coarse by design (one event per
user-facing state change) and carries no sub-component traces. Tests that
exercise the admin channel, session lifecycle, or scripting RPC dispatch
currently have no way to assert *why* a given outcome happened; failures
require local reproduction against the GUI or `infolog.txt`.

This feature closes that gap by introducing a first-class Hub log bus and
exposing it over the existing scripting gRPC service as a new
`StreamHubLog` bidi RPC. Four moving parts:

1. **`FSBar.Hub.HubLog` module** — canonical, in-process emit surface for
   structured log entries (timestamp, severity `Debug/Info/Warning/Error`,
   category DU, message, optional correlation ID + session ID + scripting
   client ID). Non-blocking; O(1) work when no subscribers are attached
   (FR-016). Drops are counted per subscriber and reported inline
   (FR-012). Messages over 8 KiB UTF-8 are truncated with a trailing
   ` …[truncated N bytes]` marker (FR-012a).
2. **Correlation-ID infrastructure** — a gRPC interceptor wraps every
   unary RPC on `ScriptingService`, reads an optional `x-fsbar-correlation-id`
   request-metadata header, assigns a fresh GUID when absent, stores the
   effective ID in `AsyncLocal<CorrelationId option>`, echoes it back in
   the response's trailing metadata, and is picked up transparently by
   every `HubLog.emit` call inside the RPC handler (FR-009 / FR-009a).
3. **New emission sites** (per Clarifications Q1) — wire-level
   `Debug` traces in `AdminChannelHost` (outbound command + inbound
   event), `Debug` RPC-dispatch traces in `ScriptingHub` (at entry + at
   completion), and `Info`-level per-action entries at every user-facing
   action across `SessionManager`, `ProxyInstaller`, `HeadlessRenderer`,
   `HubStateStore`, preset persistence (inline in `ScriptingService`),
   `LobbyConfig`, and `HubSettings`. Existing `HubEvent.DiagnosticsLine`
   emissions are mirrored through `HubLog` via a small adapter so they
   continue to surface to the local GUI and arrive on the new stream
   uniformly (FR-014). Out-of-scope per Clarifications Q1: engine-launcher
   `infolog.txt` capture, map-analysis, synthetic-data, and viz-rendering
   internals.
4. **Bidi gRPC surface** — a new `rpc StreamHubLog(stream StreamHubLogRequest)
   returns (stream LogEntryMessage)` on `ScriptingService`. The client
   sends an initial `StreamHubLogRequest` carrying filter (categories,
   severity floor, optional preset); subsequent client messages mutate
   the effective filter in-place (FR-006) and receive an in-stream
   `filter_ack` acknowledgement. Subscription count is capped via
   `HubSettings.MaxLogStreamSubscribers` (default 8, range 1–32) with
   `ResourceExhausted` rejection on overflow (FR-015a).

Primary risks:

- **Silent re-entrancy / cross-category leakage**. A `HubLog.emit` call
  from inside a hot path must not allocate or lock on the happy path.
  Phase-0 research R1 pins the implementation to a lock-free snapshot
  read of the subscriber array, structured entries carried as records
  with pre-computed severity/category discriminants, and per-subscriber
  filter evaluation inlined before any string formatting cost is paid.
- **Correlation-ID propagation across await boundaries**. The interceptor
  uses `AsyncLocal<_>` rather than `ThreadLocal<_>` so it survives
  `Task.Run` / `Async.StartAsTask` hops inside RPC handlers. R3 documents
  the one caveat — background work handed off *after* the RPC has
  completed must `use` an explicit `HubLog.withCorrelationId` scope if
  its log lines should still carry the RPC's ID; otherwise they carry
  `None`.
- **Additive wire-contract gate**. Feature 040 and 041 examples must
  continue to compile (FR-017 / SC-006). The proto change is strictly
  additive — one new RPC + its messages + a new `LogSeverity` and
  `LogCategory` enum; no existing RPC, message, field, or enum is
  renumbered, removed, or repurposed. Verified in Phase 1 via
  `buf breaking` against the feature-041 snapshot.

**Non-goals for this pass (deferred)**:

- Persistent log history / replay (spec assumption — live-only).
- Per-field redaction (scripting endpoint remains loopback-only, per
  spec Edge Cases).
- Emission sites in `FSBar.Client`, `FSBar.Viz`, `FSBar.SyntheticData`,
  and engine-launcher `infolog.txt` capture (spec Clarifications Q1
  scoped this out; eligible for a follow-up feature).

## Technical Context

**Language/Version**: F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints).
**Primary Dependencies**: Existing in-repo only — `FSBar.Proto`, `FSBar.Client`, `FSBar.Viz`, `FSBar.Hub`, `FSBar.Hub.App`. NuGet: `Grpc.AspNetCore 2.67.0`, `Grpc.Core.Api 2.67.0`, `FsGrpc 1.0.6`, `SkiaSharp 2.88.6`, `SkiaViewer 1.1.3-dev` (local feed), `BarData` (local feed), `xUnit 2.9.x`, `Microsoft.NET.Test.Sdk 17.x`. **No new NuGet dependencies.**
**Storage**: Filesystem only — unchanged from feature 041. `HubSettings.MaxLogStreamSubscribers` persists in `$XDG_CONFIG_HOME/fsbar-hub/settings.json` (schema v3, one-field bump from v2). `HubLog` subscriber state is in-memory only, released within 1 s of gRPC channel close (FR-013).
**Testing**: `xUnit 2.9.x` + `Microsoft.NET.Test.Sdk 17.x` via `tests/FSBar.Hub.Tests` (unit) and `tests/FSBar.Hub.LiveTests` (integration). New unit suites: `HubLogTests`, `HubLogFanOutTests`, `CorrelationIdInterceptorTests`, `HubLogFilterTests`, `HubLogTruncationTests`. New live suite: `LiveAdminChannelLogStreamTests` tagged `[<Trait("Category", "LogStream")>]`, exercised by `dotnet test --filter "Category=LogStream"` against a real launched session.
**Target Platform**: Linux (Arch-based dev image). The Hub app itself runs on Linux + `DISPLAY=:0` for Skia windowing; the gRPC server is loopback-only on Kestrel via `Grpc.AspNetCore`. No new platform surface.
**Project Type**: Extension of the existing desktop + library hybrid — `FSBar.Hub` is the packable core library and `FSBar.Hub.App` is the GUI host. This feature is library-only: the GUI is unchanged; all new surface lands in `FSBar.Hub` and `FSBar.Proto`.
**Performance Goals**:
- SC-002: zero measurable overhead on RPC/render/event-bus paths when no subscriber is attached. Pinning mechanism — lock-free subscriber-count read + early return *before* any record allocation or string format.
- SC-003: slow subscriber (1 entry/s consuming, 100 entry/s producing) does not stall any non-log Hub operation over 10 minutes. Per-subscriber bounded channel (capacity 256, `BoundedChannelFullMode.DropOldest`) backed by the same pattern used in `ScriptingHub` frame fan-out. Verified by `tests/FSBar.Hub.LiveTests/LogStreamSoakTests.fs` (tagged `Category=LogStreamSoak`, see tasks.md T072a/T072b).
- Throughput target: 10 000 entries/s sustained across all categories with `Debug` floor. Every entry ≤ 8 KiB UTF-8 (FR-012a).
**Constraints**:
- Additive wire-contract only (FR-017 / SC-006).
- No net-new NuGet dependencies (Constitution §Engineering Constraints).
- Loopback-only (scripting gRPC endpoint already bound to `127.0.0.1`).
- Clarifications Q2: empty-filter default = all categories, `Info` floor; `Debug` requires explicit opt-in.
- Clarifications Q4: per-entry message cap = 8 KiB UTF-8, truncation marker ` …[truncated N bytes]`.
- Clarifications Q5: `HubSettings.MaxLogStreamSubscribers` default 8, range 1–32, `ResourceExhausted` on overflow.
**Scale/Scope**:
- 1 library module (`HubLog.fs(i)`) + 1 gRPC interceptor module + edits to
  8 existing Hub files (sink threading + new emission sites) +
  1 `HubSettings` field + `proto/hub/scripting.proto` additive block +
  generated F# code refresh + 1 new example script +
  5 new unit-test files + 1 new live-test file.
- Expected LOC delta: ~800 source + ~600 test + ~400 proto-generated.
- Touches feature-040/041 gRPC surface without modifying any existing RPC or message.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Spec-First Delivery — PASS
Spec exists at `specs/042-grpc-log-stream/spec.md` with five
user stories (P1×2, P2×2, P3×1), 20+ testable FRs, measurable SCs,
assumptions, and edge cases. Clarifications session 2026-04-18 resolved
five open questions (see spec §Clarifications). This plan maps every FR
to a Phase 0 research slot or a Phase 1 artifact.

### II. Compiler-Enforced Structural Contracts — PASS
New modules ship with paired `.fsi` signatures and surface-area baselines:

- `src/FSBar.Hub/HubLog.fsi` + `.fs` → new baseline `tests/FSBar.Hub.Tests/Baselines/HubLog.baseline`.
- `src/FSBar.Hub/CorrelationId.fsi` + `.fs` (private module exposing the interceptor + `AsyncLocal` accessor) → new baseline.
- Edits to `HubSettings.fsi` (new field + validator) and `ScriptingHub.fsi`
  (constructor takes the new log facade) bump the existing baselines —
  one field each, `SURFACE_AREA_UPDATE=1` regeneration documented in
  Phase 1.
- Proto changes land in `src/FSBar.Proto/Generated/hub/scripting.gen.fs` via
  `cd proto && buf generate`; the generated diff is committed.

No `private` / `internal` modifiers in non-generated source (Constitution
§II restatement in CLAUDE.md). Internal wiring that must not leak to the
`.fsi` is gated via module-level `let` bindings inside the `.fs` only.

### III. Test Evidence Is Mandatory — PASS
Each user story has paired unit + integration coverage:

- US1 (stream fine-grained logs) → `HubLogTests.streamReceivesEmittedEntries`,
  `HubLogFanOutTests.multiSubscriberSeesIdenticalEntries`, and live-test
  `LiveAdminChannelLogStreamTests.LaunchSessionEmitsAdminChannelTrace`.
- US2 (filter) → `HubLogFilterTests.categoryWhitelistExcludesOthers`,
  `HubLogFilterTests.severityFloorDropsLower`,
  `HubLogFilterTests.filterMutationAppliesOnNextEntry`,
  live-test `LiveAdminChannelLogStreamTests.FilterMutationTakesEffectMidSession`.
- US3 (correlation) →
  `CorrelationIdInterceptorTests.autoAssignsIdWhenHeaderAbsent`,
  `CorrelationIdInterceptorTests.honoursClientSuppliedId`,
  `HubLogTests.emitPicksUpAsyncLocalCorrelationId`,
  live `LiveAdminChannelLogStreamTests.PauseRpcLogsCarryCorrelationId`.
- US4 (drop handling) →
  `HubLogFanOutTests.slowSubscriberDropsOldestAndReportsCount`,
  `HubLogFanOutTests.disposeReleasesPerSubscriberStateWithin1s`.
- US5 (presets) → `HubLogTests.presetBundlesCategoriesAndFloor`,
  `HubLogTests.explicitCategoriesOverridePreset`.

FR-018's motivating admin-channel scenario is pinned by
`LiveAdminChannelLogStreamTests.FullAdminCycleEmitsExpectedEntries`
— pause → resume → speed-change → force-end — asserting (a) category
coverage, (b) correlation IDs, (c) admin-channel status transitions.

### IV. Observability and Safe Failure Handling — PASS
The feature *is* observability infrastructure. Failure paths (subscriber
overflow, filter update mid-flight, Hub shutdown) each emit a terminal
acknowledgement entry or gRPC status per spec Edge Cases. No swallowed
exceptions in emit paths — all error conditions surface as either a
`LogEntryMessage` on the affected subscriber's stream or a gRPC status
code, never a silent drop other than the FR-012 drop-oldest path which
*is* reported via `dropped_since_last`.

### V. Scripting Accessibility — PASS
Phase 1 produces `scripts/examples/22-hub-log-stream.fsx` — a numbered
example that opens a bidi stream, subscribes to the `session-lifecycle`
preset, triggers a pause, asserts at least one admin-channel entry
arrives within two seconds, and cleanly cancels. The existing
`scripts/prelude.fsx` does not need changes — the new example is
self-contained via `#r "nuget: FSBar.Hub, *-*"` per the established
pattern (see `16-hub-admin.fsx`).

### Engineering Constraints — PASS
- F# on .NET only — yes.
- Every public `.fs` has `.fsi` — yes, including the new `HubLog` and
  `CorrelationId` modules.
- Surface-area baselines — yes, new baselines added, existing ones
  updated for the additive `HubSettings` / `ScriptingHub` changes.
- No new NuGet dependencies — confirmed; all transport work uses
  existing `Grpc.AspNetCore` + `FsGrpc` + BCL primitives
  (`AsyncLocal<_>`, `System.Threading.Channels.BoundedChannel`).
- Packable — `FSBar.Hub` and `FSBar.Proto` continue to produce nupkgs
  to `~/.local/share/nuget-local/`; this feature does not add new
  packable projects.
- gRPC setup via `fsgrpc-setup` — yes, `proto/hub/scripting.proto` is
  regenerated with `cd proto && buf generate` per CLAUDE.md workflow.

**Gate verdict**: All gates pass before Phase 0.
**Complexity tracking**: empty (no violations to justify).

## Project Structure

### Documentation (this feature)

```text
specs/042-grpc-log-stream/
├── plan.md              # This file (/speckit.plan output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── scripting.proto.delta   # additive proto block for review
│   ├── HubLog.fsi              # public F# surface sketch
│   └── CorrelationId.fsi       # private module sketch (inside FSBar.Hub)
├── spec.md              # Authored by /speckit.specify (already present)
├── checklists/
│   └── requirements.md         # Already present
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── FSBar.Proto/                     # Generated protobuf types — regenerate via `cd proto && buf generate`
│   └── Generated/hub/scripting.gen.fs   # additive edits only
├── FSBar.Hub/                       # Packable core library — THIS FEATURE's primary surface
│   ├── HubLog.fsi                       # NEW — public log-emit surface + subscriber fan-out
│   ├── HubLog.fs                        # NEW — implementation (lock-free subscriber array, bounded per-subscriber channels)
│   ├── CorrelationId.fsi                # NEW — AsyncLocal-backed correlation-ID carrier + gRPC interceptor
│   ├── CorrelationId.fs                 # NEW
│   ├── HubSettings.fsi / .fs            # EDITED — add MaxLogStreamSubscribers field, bump SchemaVersion to 3, updateMaxLogStreamSubscribers validator
│   ├── ScriptingHub.fsi / .fs           # EDITED — constructor takes HubLog.T; interceptor wired into Kestrel options; new StreamHubLog handler; per-RPC emit at dispatch/completion
│   ├── SessionManager.fsi / .fs         # EDITED — emit HubLog entries at each state transition + admin dispatch
│   ├── AdminChannelHost.fsi / .fs       # EDITED — emit Debug-level wire trace on every inbound/outbound datagram + status transitions
│   ├── HeadlessRenderer.fsi / .fs       # EDITED — emit Info entries on subscribe/detach/overflow + Debug on each frame encode summary
│   ├── HubStateStore.fsi / .fs          # EDITED — emit Info entries on every successful mutator + Warning on Rejected outcome
│   ├── ProxyInstaller.fsi / .fs         # EDITED — emit Info entries per install step (in addition to existing HubEvent)
│   ├── LobbyConfig.fsi / .fs            # EDITED — emit Warning on validation failures with per-error line
│   └── (no changes to BarInstall / BundledProxy / OverlayLayerStore / HubUiTypes)
├── FSBar.Hub.App/                   # GUI host — UNCHANGED
│   └── Program.fs                       # one edit: wire HubLog into event-bus + pass to ScriptingService constructor
└── (FSBar.Client / FSBar.Viz / FSBar.SyntheticData — UNCHANGED per Clarifications Q1 out-of-scope)

tests/
├── FSBar.Hub.Tests/                 # unit tests — NEW suites:
│   ├── HubLogTests.fs
│   ├── HubLogFanOutTests.fs
│   ├── HubLogFilterTests.fs
│   ├── HubLogTruncationTests.fs
│   ├── CorrelationIdInterceptorTests.fs
│   └── Baselines/
│       ├── HubLog.baseline               # NEW
│       ├── CorrelationId.baseline        # NEW
│       ├── HubSettings.baseline          # UPDATED (one-line add)
│       └── ScriptingHub.baseline         # UPDATED (constructor signature extended)
└── FSBar.Hub.LiveTests/             # integration tests — NEW suite:
    └── LiveAdminChannelLogStreamTests.fs  # [<Trait("Category", "LogStream")>]

scripts/
└── examples/
    └── 22-hub-log-stream.fsx         # NEW — runnable walkthrough per SC-005

proto/
└── hub/
    └── scripting.proto               # EDITED — additive block: new RPC, messages, enums
```

**Structure Decision**: All new library code lives in the existing
`src/FSBar.Hub/` packable core library so the gRPC client + headless
test harness can consume `HubLog` + the generated proto surface without
pulling the GUI layer. The GUI host (`FSBar.Hub.App`) gets exactly one
wiring edit in `Program.fs` to instantiate the log bus and pass it
through `ScriptingService` and into every Hub module that emits. Tests
follow the existing `FSBar.Hub.Tests` (xUnit unit) + `FSBar.Hub.LiveTests`
(real-engine integration) split documented in `tests/README.md`. No new
projects are added.

## Complexity Tracking

*No Constitution-Check violations; section intentionally empty.*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| *(none)* | — | — |
