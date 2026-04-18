# Implementation Plan: Feature 040 follow-ups — overlay compositing, tab-state routing, live-test coverage, and admin-speed codec fix

**Branch**: `041-hub-040-followups` | **Date**: 2026-04-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/041-hub-040-followups/spec.md`

## Summary

Feature 040 shipped the full gRPC parity surface for the FSBar Hub but
left four conscious gaps. This feature closes them in a single pass:

1. **Overlay compositing in `HeadlessRenderer`** (US1, P1). The
   `OverlayLayerStore` is wired and validated, but the renderer ignores
   it. Take a per-frame `OverlayLayerStore.snapshot` inside the encode
   worker, rasterize each `OverlayPrimitive` with the current Skia
   canvas (applying the camera transform for `World` primitives,
   bypassing it for `Screen` primitives), and emit a
   `HubEvent.DiagnosticsLine Warning` if the per-frame composite
   exceeds the SC-002 5 ms P95 budget — the frame ships unchanged.
2. **Admin-channel speed codec fix** (US2, P1). Three pre-existing
   `AdminChannelCodecTests` have been red since the 039 squash.
   Restore green: emit shortest-round-trip text for `SetGameSpeed`
   (already done via `%g`), settle the documented send order
   (Phase 0 R1 confirms `setminspeed` first, `setmaxspeed` second to
   match both the test contract and engine's argUserSpeed gate
   semantics for upward changes), and pure fix-forward — decoder
   rejects old wire bytes with `ParseError` per the 2026-04-18
   clarification.
3. **Live integration test matrix** (US3, P2). Six SC-tagged tests
   wired into the existing `tests/FSBar.Hub.LiveTests` project under
   `[<Trait("Category", "UiParity")>]`: SC-001 (map theory matrix),
   SC-003+SC-008 (pixel/latency), SC-005 (multi-client convergence),
   SC-009 (overlay visibility), SC-010 (disconnect cleanup), SC-004
   (preset round-trip). Each skips on missing fixtures via the
   established `LiveSession` pattern.
4. **GUI-state routing cleanup** (US4, P3). Three tabs
   (Configurator, Encyclopedia, Settings) each carry a
   `let mutable <tab>State` block in `Program.fs` that mirrors
   fields the `HubStateStore` already authoritatively owns. Refactor
   each tab's `render` / `handleMouse` to read from
   `HubStateStore.current()` and write through the store's typed
   mutators. `Program.fs`'s `let mutable activeTab` becomes a
   read-through of `HubStateStore.current().ActiveTab`. Rejected
   mutations silently re-render with the store's authoritative value
   and emit a `DiagnosticsLine Warning`.
5. **Polish audits** (US5, P3). One coverage-audit Markdown, one
   SC-006 extensibility-probe log, one fsdoc refresh, one quickstart
   walkthrough log — all under the feature's spec directory.

Primary risks:

- **Overlay composite latency**. SC-002 caps the composite pass at
  5 ms P95 on the reference 10 Hz / 1024×768 stream at maximum legal
  load (8,000 primitives × 16 layers). Per-primitive Skia draws are
  cheap; the soft-overrun semantics (clarified 2026-04-18) means an
  individual frame can exceed budget without dropping overlays — only
  a `DiagnosticsLine Warning` is emitted. Validated end-to-end by the
  US3 SC-008 latency probe.
- **Admin codec ordering regression**. The existing implementation
  sends `setmaxspeed` first per a since-removed engine concern. The
  tests assert `setminspeed` first. Phase 0 R1 verifies the engine
  accepts either order on a real autohost socket and confirms the
  test order is correct — see research.md.
- **Tab-state refactor is invasive but mechanical**. Three tabs each
  follow the same pattern already established by feature 040's
  `Program.fs` event subscriptions. SC-006 grep for residual
  `let mutable` keywords confirms zero misses.

## Technical Context

**Language/Version**: F# 9 on .NET 10.0 (exclusive per Constitution
§Engineering Constraints).
**Primary Dependencies**: Existing in-repo only — `FSBar.Proto`,
`FSBar.Client`, `FSBar.Viz`, `FSBar.Hub`, `FSBar.Hub.App`. NuGet:
`Grpc.AspNetCore 2.67.0`, `Grpc.Core.Api 2.67.0`, `FsGrpc 1.0.6`,
`SkiaSharp 2.88.6`, `SkiaViewer 1.1.3-dev` (local feed), `BarData`
(local feed), `xUnit 2.9.x`, `Microsoft.NET.Test.Sdk 17.x`. **No new
NuGet dependencies.**
**Storage**: Filesystem only — unchanged from feature 040. Overlay
state stays in-memory in `OverlayLayerStore`; the per-frame snapshot is
allocated and discarded each render tick (FR-006).
**Testing**: `xUnit 2.9.x` under `tests/FSBar.Hub.Tests/`,
`tests/FSBar.Hub.LiveTests/`, and `tests/FSBar.Client.Tests/`. The
new live integration matrix is selectable via
`dotnet test FSBarV1.slnx --filter "Category=UiParity"` (FR-011).
**Target Platform**: Linux x86-64 (container dev image / host).
Loopback-only gRPC at `127.0.0.1:5021` (unchanged). Hub still requires
DISPLAY + GLFW for its GUI viewport; the off-screen render path keeps
its raster `SKSurface` (no GRContext, per CLAUDE.md).
**Project Type**: Continuation of the feature-040 desktop-app
(`FSBar.Hub.App`) + packable core library (`FSBar.Hub`) + in-repo
`.proto` contract layout. No new projects.
**Performance Goals**: Overlay composite ≤ 5 ms P95 added latency at
maximum legal load (SC-002); render-frame stream P95 ≤ 200 ms at 10 Hz
unchanged from 040 (validated by SC-008); preset round-trip ≤ 500 ms
unchanged (SC-004); pixel fidelity ≥ 99% vs local Viewer (SC-003);
multi-client state convergence within one render frame (SC-005);
remote `SetVizAttribute` reflected in local GUI within one frame
(SC-005 of this spec); UiParity matrix completes in ≤ 20 minutes
(SC-004 of this spec).
**Constraints**: Proto surface is unchanged — feature 040's
`scripting.proto` already declares `PutLayer`/`DeleteLayer`/etc., and
feature 041 only fills in the renderer half (no new RPCs, no new wire
messages). Codec change is fix-forward only (clarified 2026-04-18,
FR-010). No new NuGet dependencies. Every public F# module touched
keeps its `.fsi` + surface baseline aligned (Constitution II).
**Scale/Scope**: ~6 .fs file edits in `FSBar.Hub` /
`FSBar.Hub.App.Tabs` / `FSBar.Client`, ~3 surface-baseline regenerations,
~7 new test files (1 unit + 6 live), ~4 audit Markdown deliverables.
Concurrent scripting clients: single-digit (unchanged). Overlay load
unchanged: 16 layers × 500 primitives = 8,000 primitives per client.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I — Spec-First Delivery

- Spec at `specs/041-hub-040-followups/spec.md` exists, has 5
  prioritized user stories, 27 FRs, 9 SCs, 3 clarifications. Tier 1
  for the codec wire change (decoder rejects old format, FR-010);
  Tier 2 for the renderer wiring (no new public surface — extends
  existing `HeadlessRenderer.create` signature only if a snapshot
  callback is needed; see research.md R2). **PASS.**

### Principle II — Compiler-Enforced Structural Contracts

- Touched modules and their `.fsi` impact:
  - `src/FSBar.Hub/HeadlessRenderer.fsi` — no signature change
    expected (the snapshot is fetched internally from the existing
    `overlays: OverlayLayerStore.T` already passed to `create`). If
    R2 finds we need a thin `HubEvents.OverlayDiagnostics` event
    case, it lands additively in `HubEvents.fsi` with surface
    baseline updated.
  - `src/FSBar.Hub/OverlayLayerStore.fsi` — no signature change. The
    existing `snapshot: T -> OverlayLayerSnapshot` is exactly what
    the renderer needs (FR-006).
  - `src/FSBar.Client/AdminChannel.fsi` — no signature change. The
    fix is purely behavioural inside `encodeCommandToDatagrams`.
    The `SurfaceAreaTests AdminChannel` baseline must match the
    existing `.fsi` byte-for-byte (FR-009) — that test is currently
    red because the baseline was regenerated against an unrelated
    surface change, not because the API surface drifted. Re-running
    `SURFACE_AREA_UPDATE=1` once captures the correct snapshot.
  - `src/FSBar.Hub/HubStateStore.fsi` — no signature change.
    Existing mutators (`setVizConfig`, `setVizAttribute`,
    `setEncyclopedia`, `setSettings`, `setActiveTab`) cover every
    new write path the tabs need.
  - `src/FSBar.Hub.App/Tabs/ConfiguratorTab.fsi`,
    `EncyclopediaTab.fsi`, `SettingsTab.fsi` — `init` /
    `applyStatus` / `applyInstallResult` may shrink as fields move
    out of the local state record. The `.fsi` is updated in lockstep
    and the surface baseline regenerated in
    `tests/FSBar.Hub.Tests/Baselines/`.
- Surface-area baselines: regenerated under
  `tests/FSBar.Hub.Tests/Baselines/` and
  `tests/FSBar.Client.Tests/Baselines/` once after intentional
  signature changes, then committed alongside the `.fsi` edits.
- **PASS** gated on tasks-list including the `.fsi` + baseline updates.

### Principle III — Test Evidence Is Mandatory

- Each user story gets at least one independent test:
  - US1: `LiveOverlayCompositeTests` (extends 040 `LiveOverlayLayerTests`)
    — assert pixel-presence after `PutLayer` for both `World` and
    `Screen` coordinate spaces. Two-layer ordering test
    (`(ownerId, zHint, uploadedAt)` ascending). Camera-pan test that
    a `World` circle moves and a `Screen` label does not.
  - US2: existing `AdminChannelCodecTests.fs` returns to green; no
    new test file needed. Surface-area test for `AdminChannel`
    follows.
  - US3: six new live test files under `tests/FSBar.Hub.LiveTests/`,
    each tagged `[<Trait("Category", "UiParity")>]`.
  - US4: `LiveTabStateRoutingTests` — single live test that
    `SetVizAttribute` from gRPC reflects in the Configurator-tab
    panel within one frame, plus grep-based SC-006 check (zero
    `let mutable` for HubState fields in the three touched tabs).
  - US5: deliverables are Markdown audits — verified by reviewer
    inspection plus the SC-006 grep + SC-009 fsdoc warning count.
- **PASS** gated on tasks.

### Principle IV — Observability and Safe Failure Handling

- New `DiagnosticsLine` emit paths:
  - `HeadlessRenderer` overlay-overrun warning (FR-006a) carrying
    measured ms, primitive count, subscriber count.
  - `HubStateStore` rejected-mutation warning (FR-023a) carrying
    control name and rejection reason.
  - `AdminChannelCodec` decoder rejection emits `DiagnosticsLine
    Error` upstream (callers map `ParseError` → diagnostics line).
- All gRPC handlers continue to map well-formed failures to
  canonical status codes (no change from 040).
- **PASS.**

### Principle V — Scripting Accessibility

- The feature 040 `scripts/examples/21-hub-overlay-layers.fsx`
  walkthrough goes from "uploads accepted, primitives invisible" to
  "uploads accepted, primitives drawn on the next captured frame".
  No new example script is required; the existing one becomes
  end-to-end honest.
- Existing `16-hub-admin.fsx`, `17-hub-lobby-launch.fsx`,
  `18-hub-render-frames.fsx`, `19-hub-vizconfig-drive.fsx`,
  `20-hub-state-observer.fsx`, `21-hub-overlay-layers.fsx` MUST
  keep working — covered by the FR-026 fsdoc + FR-027 walkthrough
  audits in US5.
- **PASS** gated on tasks.

### Engineering Constraints

- F# on .NET exclusive — YES.
- `.fsi` + baselines — see II above.
- `dotnet pack` → local NuGet store — packable surface unchanged
  except for behaviour-only edits to `FSBar.Hub` and
  `FSBar.Client`. Versions bump at feature completion via the
  existing pack-dev workflow.
- gRPC unchanged — no proto edits, no `buf generate`.
- **PASS.**

### Complexity Tracking

No violations; no entries required.

## Project Structure

### Documentation (this feature)

```text
specs/041-hub-040-followups/
├── plan.md                    # this file
├── spec.md                    # feature spec (existing)
├── research.md                # Phase 0 output (this run)
├── data-model.md              # Phase 1 output (this run)
├── quickstart.md              # Phase 1 output (this run)
├── contracts/
│   └── admin-speed-codec.md   # wire-format note for FR-007/FR-008/FR-010
├── coverage-audit.md          # FR-024 audit (US5 deliverable)
├── sc-006-probe.md            # FR-025 audit (US5 deliverable)
└── quickstart-walkthrough.md  # FR-027 audit (US5 deliverable)
```

### Source Code (repository root)

```text
src/FSBar.Client/
├── AdminChannel.fs              # EDIT — fix encodeCommandToDatagrams ordering + decoder fix-forward (US2)
└── AdminChannel.fsi             # UNCHANGED (surface stays the same)

src/FSBar.Hub/
├── HeadlessRenderer.fs          # EDIT — composite OverlayLayerStore.snapshot per frame (US1)
├── HeadlessRenderer.fsi         # UNCHANGED (no public-surface change)
├── HubStateStore.fs             # EDIT — emit DiagnosticsLine Warning on Rejected mutation (FR-023a)
├── HubStateStore.fsi            # UNCHANGED
├── OverlayLayerStore.fs         # UNCHANGED (snapshot already exists)
├── HubEvents.fs                 # POTENTIAL EDIT — new DU case if R2 calls for it (else unchanged)
└── HubEvents.fsi                # POTENTIAL EDIT — match HubEvents.fs

src/FSBar.Hub.App/
├── Program.fs                   # EDIT — drop activeTab/configuratorState/encyclopediaState/settingsState mutables; route through HubStateStore (US4)
└── Tabs/
    ├── ConfiguratorTab.fs       # EDIT — render reads VizConfig from HubStateStore.current(); writes through setVizAttribute / setVizConfig
    ├── ConfiguratorTab.fsi      # EDIT — narrow state record (drop fields owned by HubStateStore)
    ├── EncyclopediaTab.fs       # EDIT — render reads FactionFilter + Selected from HubStateStore.current().Encyclopedia; writes through setEncyclopedia
    ├── EncyclopediaTab.fsi      # EDIT — narrow state record
    ├── SettingsTab.fs           # EDIT — render reads Settings from HubStateStore.current().Settings; writes through HubSettings.update* + setSettings
    └── SettingsTab.fsi          # EDIT — narrow state record

tests/FSBar.Client.Tests/
├── AdminChannelCodecTests.fs    # UNCHANGED (already authoritative — implementation is fixed to match)
└── Baselines/AdminChannel.baseline  # REGENERATE via SURFACE_AREA_UPDATE=1 (FR-009)

tests/FSBar.Hub.Tests/
├── HeadlessRendererTests.fs     # EDIT — add overlay-presence + ordering assertions (unit-level pixel checks at small viewport)
├── HubStateStoreTests.fs        # EDIT — add Rejected → DiagnosticsLine Warning assertion (FR-023a)
└── Baselines/                   # REGENERATE if any .fsi narrows

tests/FSBar.Hub.LiveTests/
├── LiveHeadlessOrchestrationTests.fs  # EDIT — promote single-launch smoke to [<Theory>] over Avalanche/Red Comet/Titan (SC-001)
├── LiveRenderFrameStreamTests.fs      # EDIT — extend pixel-fidelity + latency probe (SC-003 + SC-008)
├── LiveHubStateEventTests.fs          # EDIT — add two-client convergence assertion (SC-005)
├── LiveOverlayLayerTests.fs           # EDIT — add SC-009 visibility timing + SC-010 disconnect cleanup
├── LivePresetRoundtripTests.fs        # EDIT — add ≤ 500 ms timing assertion (SC-004)
└── LiveTabStateRoutingTests.fs        # NEW — gRPC SetVizAttribute → tab reflects within one frame (US4)
```

**Structure Decision**: All work lands inside the existing
feature-040 file layout. No new projects, no new public modules. The
single new test file (`LiveTabStateRoutingTests.fs`) joins the
existing `tests/FSBar.Hub.LiveTests/` project.

## Complexity Tracking

No Constitution violations; this section intentionally empty.
