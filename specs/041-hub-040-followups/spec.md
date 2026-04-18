# Feature Specification: Feature 040 follow-ups — overlay compositing, tab-state routing, live-test coverage, and admin-speed codec fix

**Feature Branch**: `041-hub-040-followups`
**Created**: 2026-04-18
**Status**: Implemented
**Input**: User description: "create specs for the deferred tasks and known problems from feature 040"

Feature 040 (`040-grpc-full-hub-ui`) shipped the full gRPC parity surface
for the FSBar Hub — every user-facing Hub action has an RPC, and remote
clients can observe rendered frames and Hub state. Several items were
consciously deferred during that feature so the MVP surface could land
in one pass. This feature closes those gaps and fixes a pre-existing
defect inherited from feature 039.

## Clarifications

### Session 2026-04-18

- Q: How should the fixed admin-channel speed codec handle old wire bytes produced by a hub running the feature-039 broken format? → A: Pure fix-forward — decoder accepts only the new format; old bytes reject with `ParseError`.
- Q: What should the renderer do at runtime when an individual frame's overlay composite exceeds its budget? → A: Log a `DiagnosticsLine Warning` and continue — overrun is soft; the frame ships with all overlays, operators investigate from logs.
- Q: How should the GUI surface a rejected `HubStateStore` mutation back to the user? → A: Silent rollback — next render shows the store's authoritative value; a `DiagnosticsLine Warning` is emitted with the reason. No visual feedback on the control itself.

---

Scope covers four classes of work:

1. **User-facing correctness gaps** — RPCs accept data that currently
   isn't drawn or doesn't round-trip end-to-end.
2. **Pre-existing defect** — three unit tests in
   `FSBar.Client.Tests.AdminChannelCodecTests` have been red since the
   039 squash merge. They guard the wire format for the admin channel
   and were suppressed (not fixed) during 040.
3. **Live-test coverage** — Phase-9 live integration tests from 040
   (SC-001 map matrix, SC-003 pixel fidelity + SC-008 latency, SC-005
   convergence, SC-009 overlay visibility, SC-010 disconnect cleanup,
   SC-004 preset round-trip) that require a real BAR engine and were
   skipped for session-scope reasons.
4. **GUI-state routing cleanup** — three Hub tabs (Configurator,
   Encyclopedia, Settings) still own local mutable state for fields
   that `HubStateStore` now authoritatively holds. The gRPC RPCs work
   today because each tab writes to both places; removing the local
   state eliminates drift risk and simplifies the mental model.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Uploaded overlays are actually drawn on rendered frames (Priority: P1) 🎯 MVP

A scripting client uploads a named overlay layer via `PutLayer`. The
next `GetRenderFrame` response and every subsequent
`StreamRenderFrames` tick contain the uploaded primitives composited
on top of the base Viewer scene. World-coordinate primitives follow
the Viewer camera's pan/zoom; screen-coordinate primitives stay fixed
in the viewport.

**Why this priority**: Today the overlay RPCs accept, validate, and
store layers correctly but `HeadlessRenderer` renders only the base
scene. The round-trip contract the `21-hub-overlay-layers.fsx`
walkthrough advertises is not actually honored — this is a
user-visible correctness gap, not a polish item.

**Independent Test**: Upload one `World`-space circle at a known map
coordinate and one `Screen`-space label at a fixed pixel position;
capture a frame via `GetRenderFrame`; assert the PNG contains a
circle of the expected color at the expected pixel (with camera
transform applied) and a label at the expected pixel. Pan the camera;
the circle moves, the label does not.

**Acceptance Scenarios**:

1. **Given** a session is running and a client has called `PutLayer`
   with a `World`-space circle at map coordinate (200, 200),
   **When** the client subsequently calls `GetRenderFrame`, **Then**
   the rendered image contains a circle whose pixel coordinates match
   the camera-transformed (200, 200) within 1 px.
2. **Given** a session is running and a client has called `PutLayer`
   with a `Screen`-space label at pixel (20, 20), **When** the camera
   is panned, **Then** the label remains anchored at pixel (20, 20)
   in the next rendered frame.
3. **Given** two clients have each uploaded one layer, **When** a
   third client calls `GetRenderFrame`, **Then** both layers appear
   in the same frame, sorted by `(ownerId, zHint ascending,
   uploadedAt ascending)`.
4. **Given** a client calls `ClearLayers`, **When** the next frame
   renders, **Then** its primitives no longer appear.
5. **Given** the base scene's latency budget was P95 ≤ 200 ms at
   10 Hz (SC-008 from feature 040), **When** overlays are composited,
   **Then** P95 latency stays within budget at the default cap load
   (16 layers × 500 primitives).

---

### User Story 2 — Admin-channel speed codec round-trips correctly (Priority: P1)

The `FSBar.Client.AdminChannelCodec` produces the exact wire format
the BAR engine's autohost socket expects for `SetGameSpeed`. Three
unit tests that guard the fractional format (no trailing zeros) and
the `setminspeed` + `setmaxspeed` expansion have been failing since
the feature-039 squash merge.

**Why this priority**: Wire-format defects on the admin channel are
how pause / resume / speed commands reach the engine. A regression
here can silently break every feature-039 live scenario. The defect
is isolated and small.

**Independent Test**: Run
`dotnet test tests/FSBar.Client.Tests --filter "FullyQualifiedName~AdminChannelCodecTests"`
and `--filter "FullyQualifiedName~SurfaceAreaTests AdminChannel"` —
all three tests must pass green; the surface-area baseline for the
`AdminChannel` module must match its `.fsi`.

**Acceptance Scenarios**:

1. **Given** the codec is asked to serialize `SetGameSpeed 1.5f`,
   **When** the emitted bytes are decoded, **Then** the wire text
   contains `1.5` (no trailing zeros) — not `1.500000`.
2. **Given** the codec is asked to serialize `SetGameSpeed 2.0f`,
   **When** the emitted bytes are decoded, **Then** the wire text
   contains `setminspeed 2 setmaxspeed 2` (both commands, with
   integer form when value is whole).
3. **Given** the SurfaceAreaTests baseline is regenerated,
   **When** `dotnet test` runs unmodified, **Then** the baseline
   matches the current `AdminChannel.fsi` byte-for-byte.

---

### User Story 3 — Live integration test matrix validates SC-001 through SC-010 end-to-end (Priority: P2)

Every feature-040 success criterion that requires a live BAR engine
has a corresponding live integration test, green against the local
dev install. The suite is invokable via
`dotnet test --filter "Category=UiParity"`.

**Why this priority**: Feature 040's in-process unit tests cover RPC
mechanics, but SC-003 (pixel fidelity), SC-008 (render latency),
SC-005 (multi-client convergence), SC-009 (overlay visibility),
SC-010 (disconnect cleanup), and SC-004 (preset round-trip) were
only validated by the FSI walkthrough examples. Automated coverage
catches regressions before they ship.

**Independent Test**: On a dev box with BAR installed and the canonical
map set (Avalanche 3.4, Red Comet Remake 1.8, Titan v2) plus the
HighBarV2 + BARb skirmish AIs, run
`dotnet test FSBarV1.slnx --filter "Category=UiParity"` — the full
matrix must be green.

**Acceptance Scenarios**:

1. **Given** the US1 `LiveHeadlessOrchestrationTests` smoke test
   already passes on Avalanche 3.4, **When** the `[<Theory>]` variant
   runs 20 launches each on Avalanche 3.4, Red Comet Remake 1.8, and
   Titan v2, **Then** ≥ 19 of 20 launches per map succeed (SC-001).
2. **Given** a fixed-seed synthetic session is running, **When** a
   client subscribes to `StreamRenderFrames` at 10 Hz and captures
   20 frames while the local Viewer tab captures the same 20 frames,
   **Then** ≥ 99% of pixels match per frame (SC-003) and P95 latency
   (encoded-at → client decode) is ≤ 200 ms (SC-008).
3. **Given** two clients have both subscribed to
   `StreamHubStateEvents`, **When** a third actor mutates `VizConfig`
   via `SetVizAttribute`, **Then** both clients receive a matching
   event within one render frame (SC-005).
4. **Given** a client has just called `PutLayer`, **When** the next
   render tick fires, **Then** the uploaded primitives appear within
   ≤ 100 ms of the `PutLayer` wall-clock (SC-009).
5. **Given** client A has layers in the store and then disconnects,
   **When** client B captures frames, **Then** client A's primitives
   are absent within 2 frames (SC-010).
6. **Given** a preset is saved via gRPC, **When** it is loaded back,
   **Then** the round-trip completes in < 500 ms and the
   reconstructed `VizConfig` is byte-equivalent (SC-004).

---

### User Story 4 — Hub tabs read/write authoritative state through the store (Priority: P3)

The Configurator, Encyclopedia, and Settings tabs read their state
from `HubStateStore.current()` and write mutations via the store's
helper API. Remote gRPC writes (`SetVizAttribute`, `SelectUnit`,
`SetHubSettings`, `SavePreset`) show up in the GUI within one render
frame without the GUI holding redundant local mirrors.

**Why this priority**: The gRPC path works today because each tab's
local state is updated by both the GUI input handler and the
store-event subscriptions in `Program.fs`. Removing the local mirrors
tightens the mental model and eliminates drift risk on future edits;
it does not change end-user behavior.

**Independent Test**: From an external gRPC client, call
`SetVizAttribute` with a known descriptor key and observe the
Configurator-tab panel reflect the new value within one frame.
Similarly for `SelectUnit` (EncyclopediaTab) and `SetHubSettings`
(SettingsTab). The GUI must NOT revert the change on the next
frame (which would happen if the local mirror still held the old
value).

**Acceptance Scenarios**:

1. **Given** the Configurator tab is displayed, **When** a remote
   client calls `SetVizAttribute("overlays.weaponRanges", true)`,
   **Then** the corresponding toggle in the panel reads "on" within
   one frame and stays "on" on subsequent frames.
2. **Given** the Encyclopedia tab is displayed with no unit pinned,
   **When** a remote client calls `SelectUnit(InternalName="armcom")`,
   **Then** the Encyclopedia tab highlights Armada Commander and
   shows its details pane within one frame.
3. **Given** the Settings tab is displayed, **When** a remote client
   calls `SetHubSettings(StartPausedDefault=false)`, **Then** the
   Start-Paused checkbox on the Setup tab reflects `false` within
   one frame and the setting persists to the JSON settings file.
4. **Given** the Configurator tab's preset panel is displayed,
   **When** a remote client calls `SavePreset("demo")`, **Then**
   "demo" appears in the preset list within one frame.

---

### User Story 5 — Polish audits document coverage and extensibility (Priority: P3)

Four documentation audits ship: an FR-018 GUI-action→RPC coverage
matrix, an SC-006 extensibility probe, a refreshed fsdoc run, and a
manual quickstart walkthrough log. These are operator-facing
artifacts that make the feature-040 contract discoverable.

**Why this priority**: The RPCs work; the audits prove that. They
don't unlock new capabilities but make the contract visible to
stakeholders and catch scope gaps early in future features.

**Independent Test**: Reviewers open
`specs/041-hub-040-followups/coverage-audit.md` and see every
feature-040 GUI action mapped to a gRPC RPC. They open the fsdoc
output and see every new public module documented. They run the
manual quickstart walkthrough and every step completes as described.

**Acceptance Scenarios**:

1. **Given** the FR-018 coverage audit is written, **When** a
   reviewer reads it, **Then** every user-facing action in the six
   Hub tabs appears with the matching gRPC RPC (100% mapping per
   SC-002).
2. **Given** the SC-006 probe is completed, **When** a reviewer
   reads the log, **Then** it records the exact lines-changed count
   for adding one new `ConfigDescriptors` attribute and surfacing
   it over `SetVizAttribute` end-to-end.
3. **Given** the fsdoc run has executed against every new public
   surface (`HubStateStore`, `HeadlessRenderer`, `OverlayLayerStore`,
   extended `ScriptingHub` / `SessionManager` / `HubEvents` /
   `HubSettings`), **When** the output is inspected, **Then** every
   public member has a non-empty doc string.

---

### Edge Cases

- **Overlay cap behavior under high load**: when a client uploads 16
  layers with 500 primitives each (8,000 primitives total per client,
  close to 1 MB per-push cap), the overlay composite pass must still
  keep P95 render latency within SC-008's 200 ms budget. If an
  individual frame exceeds its SC-002 5 ms composite budget, the
  renderer logs a `DiagnosticsLine Warning` carrying the measured
  overrun and the frame ships unchanged with all overlays drawn
  (clarified 2026-04-18 — soft SLO, no frame suppression, no client
  throttling, no stream detach). Operators investigate from logs and
  the cap matrix is re-evaluated in a follow-up feature if the
  warnings persist.
- **Client disconnects mid-render**: if `HeadlessRenderer` holds a
  snapshot reference to `OverlayLayerStore` at frame start and a
  client disconnects between snapshot and draw, the frame still
  renders that client's primitives (frozen at snapshot time). The
  next frame's snapshot excludes them. This is acceptable per
  SC-010's "within 2 frames" wording; no explicit action needed.
- **Malformed admin-speed wire bytes** (feature-039 defect): the
  decoder rejects old-format bytes with a `ParseError` (clarified
  2026-04-18 — pure fix-forward, no dual-format shim). Operators
  on older hub builds see the rejection surface as a
  `DiagnosticsLine Error` and must upgrade the hub.
- **GUI-state routing during autoload**: when the Hub starts and
  `HubSettings.load()` pulls persisted values, the tabs must not
  momentarily display stale defaults before the store publishes
  `HubSettingsChanged`. The store should be seeded with persisted
  values before any tab first renders.
- **Live-test flakiness on weaker dev boxes**: the SC-003 pixel-diff
  test allows 1% drift; boxes with different Skia text-rendering
  hints may produce different glyph anti-aliasing. The test suite
  must skip (not fail) when pixel drift exceeds 5% — this is a
  local-environment mismatch, not a product defect.

## Requirements *(mandatory)*

### Functional Requirements

#### Overlay compositing (US1)

- **FR-001**: The render pipeline MUST composite every overlay
  primitive from every client on top of the base Viewer scene on
  every rendered frame (both `GetRenderFrame` and per-tick
  `StreamRenderFrames`).
- **FR-002**: `World`-coordinate primitives MUST be transformed by
  the current `HubStateStore.Camera` (scale + origin) before
  rasterization, matching the pixel position of a same-coordinate
  unit glyph in the base scene.
- **FR-003**: `Screen`-coordinate primitives MUST be drawn in
  viewport pixel space, unaffected by the camera transform.
- **FR-004**: Layers MUST be drawn in ascending order of
  `(ownerId, zHint, uploadedAt)` so later-uploaded primitives render
  on top of earlier ones when `zHint` ties.
- **FR-005**: Every `OverlayPrimitive` case (Line, Polyline, Polygon,
  Rectangle, Circle, Path, Text, Image) MUST render with its
  specified `OverlayStyle` (stroke color, stroke width, fill color
  when present, opacity, dash pattern when set).
- **FR-006**: The compositing pass MUST NOT retain references to
  `OverlayLayerStore` state beyond the current frame — the store's
  snapshot operation is O(total-layers) and each frame pays that
  cost independently.
- **FR-006a**: When the overlay composite pass for a single frame
  exceeds the SC-002 5 ms P95 budget, the renderer MUST emit a
  `HubEvent.DiagnosticsLine Warning` naming the measured overrun
  (milliseconds, primitive count, active-subscriber count) and MUST
  still ship the frame unchanged with every primitive drawn. It
  MUST NOT drop overlays, throttle the offending client, or detach
  the stream on overrun.

#### Admin-channel codec (US2)

- **FR-007**: `AdminChannelCodec.encodeSetGameSpeed(speed)` MUST emit
  the value using the shortest textual representation that
  round-trips to the same `float32` (e.g. `1.5` not `1.500000`,
  `2` not `2.0` when the value is a whole integer).
- **FR-008**: `AdminChannelCodec.encodeSetGameSpeed(speed)` MUST emit
  both `setminspeed` and `setmaxspeed` commands — the engine requires
  both to be set on the autohost channel to pin game speed.
- **FR-009**: The public surface of `FSBar.Client.AdminChannel` MUST
  match its `.fsi` signature byte-for-byte (surface-area baseline).
- **FR-010**: The codec is fix-forward only. The decoder MUST accept
  the new (correct) wire format and MUST reject the old (broken)
  format with a `ParseError`. No dual-format compatibility shim is
  provided; every hub in the fleet upgrades atomically via the
  `FSBar.Hub` / `FSBar.Client` package bump.

#### Live integration test matrix (US3)

- **FR-011**: The test suite MUST expose a `[<Trait("Category",
  "UiParity")>]` filter selector that runs every new live integration
  test and nothing else, so CI can invoke the full matrix in one
  command.
- **FR-012**: Each live test MUST skip (not fail) when its required
  BAR fixtures (engine binary, AI binaries, map archives) are
  missing from the dev box, matching the existing LiveSession
  pattern.
- **FR-013**: The pixel-diff test MUST be deterministic against a
  fixed-seed synthetic session; drift above 5% SHOULD skip (local
  rendering environment mismatch), between 1%–5% SHOULD warn, and
  at-or-below 1% MUST pass (SC-003's 99% threshold).
- **FR-014**: The two-client convergence test MUST verify that a
  mutation from one client surfaces in both clients' event streams
  within a window of one render frame (SC-005).
- **FR-015**: The overlay visibility test MUST verify that a
  `PutLayer` call surfaces in the next render frame within ≤ 100 ms
  (SC-009).
- **FR-016**: The disconnect-cleanup test MUST verify that a client's
  layers disappear from every other client's render within 2 frames
  after the owning client's stream closes (SC-010).

#### GUI-state routing cleanup (US4)

- **FR-017**: The Configurator tab MUST read `VizConfig` from
  `HubStateStore.current().VizConfig` on every render; it MUST NOT
  hold a local mutable mirror.
- **FR-018**: The Configurator tab MUST write every slider / toggle /
  color-picker mutation through `HubStateStore.setVizAttribute`
  (single-attribute path) or `HubStateStore.setVizConfig` (full
  replace path on preset load), never directly mutating its own
  state.
- **FR-019**: The Encyclopedia tab MUST read `FactionFilter` and
  `SelectedDefId` from `HubStateStore.current().Encyclopedia` on
  every render; it MUST NOT hold a local mutable mirror.
- **FR-020**: The Encyclopedia tab MUST write filter-toggle and
  selection changes through `HubStateStore.setEncyclopedia`.
- **FR-021**: The Settings tab MUST read `HubSettings` from
  `HubStateStore.current().Settings` on every render; it MUST NOT
  hold a local mutable mirror.
- **FR-022**: The Settings tab MUST write checkbox changes through
  the `HubSettings.update*` helpers + `HubSettings.save` +
  `HubStateStore.setSettings` in that order so the GUI, persisted
  file, and event stream stay synchronized.
- **FR-023**: The hub-level `activeTab` variable in Program.fs MUST
  be a read-through of `HubStateStore.current().ActiveTab`; tab-bar
  clicks MUST call `HubStateStore.setActiveTab` rather than
  assigning `activeTab` directly.
- **FR-023a**: When a `HubStateStore` mutator returns `Rejected`
  (invalid value, unknown key, write contention, or any other
  documented rejection path), the tab MUST silently re-render with
  the store's current authoritative value — the user sees no visual
  feedback on the clicked control itself (no red flash, no banner,
  no modal). The rejection MUST emit a `HubEvent.DiagnosticsLine
  Warning` carrying the control name and the rejection reason so
  operators can diagnose recurring rejections from the Diagnostics
  pane.

#### Polish audits (US5)

- **FR-024**: A coverage-audit document at
  `specs/041-hub-040-followups/coverage-audit.md` MUST list every
  user-facing action in the six Hub tabs and map each to exactly one
  feature-040 RPC. Unmapped actions MUST be explicitly called out as
  "no RPC — intentional" or "no RPC — gap".
- **FR-025**: An extensibility-probe log at
  `specs/041-hub-040-followups/sc-006-probe.md` MUST record the
  exact count of lines added, modified, and deleted to surface one
  new `ConfigDescriptors` attribute over `SetVizAttribute`
  end-to-end.
- **FR-026**: The fsdoc output MUST cover every public member of the
  modules widened by feature 040 (`HubStateStore`,
  `HeadlessRenderer`, `OverlayLayerStore`, `ScriptingHub`,
  `SessionManager`, `HubEvents`, `HubSettings`).
- **FR-027**: A walkthrough log at
  `specs/041-hub-040-followups/quickstart-walkthrough.md` MUST
  document each step of feature 040's `quickstart.md` as executed
  manually on a dev box, with timings and any unexpected friction
  points noted.

### Key Entities

- **Overlay snapshot**: an immutable per-frame projection of
  `OverlayLayerStore` used by the render pass. Already modeled by
  the existing `OverlayLayerSnapshot` type; US1 extends the
  renderer to consume it.
- **UiParity test category**: an xUnit trait that groups the live
  integration tests shipped by this feature. Not a new data type —
  a tagging convention the test runner honors.
- **Coverage-audit matrix**: a read-only table mapping GUI actions
  to RPCs, persisted as Markdown under the feature's spec
  directory. Not an in-process artifact.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A round-trip `PutLayer → GetRenderFrame` returns a
  PNG whose pixel at the expected World-transformed coordinate
  matches the uploaded primitive's color within 2 RGB steps
  (tolerant of anti-aliasing).
- **SC-002**: The overlay-composite pass adds ≤ 5 ms to P95 frame
  latency on the reference 10-Hz / 1024×768 render stream at
  maximum legal load (8,000 primitives across 16 layers).
- **SC-003**: All three `FSBar.Client.Tests.AdminChannelCodecTests`
  tests pass on master; full `dotnet test tests/FSBar.Client.Tests`
  run is 272/272 green (today it's 269/272).
- **SC-004**: `dotnet test FSBarV1.slnx --filter "Category=UiParity"`
  runs to completion in ≤ 20 minutes on a reference dev box and
  reports 0 failures, N skips (where N counts only tests whose
  fixtures are absent).
- **SC-005**: A remote gRPC `SetVizAttribute` call is reflected in
  the local GUI within 1 render frame in 100% of 50 sample calls.
- **SC-006**: The Configurator, Encyclopedia, and Settings tabs
  contain zero `let mutable` bindings for fields that live in
  `HubStateStore.HubState` — verified via grep.
- **SC-007**: Adding one new `ConfigDescriptors.all` entry and
  exercising it via a fresh `SetVizAttribute` call requires ≤ 10
  lines of change across at most 2 files.
- **SC-008**: The FR-018 coverage audit shows 100% of Hub GUI
  actions mapped to a feature-040 RPC; any unmapped actions are
  explicitly categorized with a stated reason.
- **SC-009**: The fsdoc output covers 100% of public members in the
  widened modules — zero "missing doc" warnings.

## Assumptions

- Overlay compositing uses the same raster `SKSurface` already used
  for base-scene rasterization in `HeadlessRenderer`; no GPU
  backend is introduced (CLAUDE.md documents the GRContext
  segfault).
- The admin-speed codec fix is pure fix-forward (clarified
  2026-04-18). Every hub in the fleet upgrades atomically via the
  FSBar.Hub / FSBar.Client package bump; the decoder has no
  dual-format compatibility path and rejects old-format bytes with a
  `ParseError`.
- The live-test matrix runs against the dev-box BAR install
  (`~/.local/state/Beyond All Reason/`) using the auto-detected
  engine version and the canonical HighBarV2 + BARb AIs. Tests
  skip on missing fixtures rather than failing.
- `HubStateStore` is already the single source of truth for
  `VizConfig`, `Encyclopedia`, `Settings`, and `ActiveTab` — the
  GUI-state cleanup in US4 is purely about removing redundant
  local mirrors, not introducing a new store.
- The fsdoc agent used by feature 040 (per Constitution §7) works
  unchanged against the feature-040 public surface; no agent
  updates are needed.
- CI for this project runs on Linux x86-64 with a DISPLAY-capable
  environment for the Hub's GUI; live-test fixture files live on
  the CI dev volume and are cached across runs.
