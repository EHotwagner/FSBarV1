# Feature Specification: gRPC parity for Hub UI and rendered viewer

**Feature Branch**: `040-grpc-full-hub-ui`
**Created**: 2026-04-18
**Status**: Draft
**Input**: User description: "make all ui functions of the hub available to the grpc client. also make custom skia drawings in the hubviewer available to grpc."

## Clarifications

### Session 2026-04-18

- Q: Should the service distinguish driver vs observer clients, or keep equal trust for all loopback clients? → A: Equal trust — every loopback client can invoke every RPC, including destructive ones. No role state, no handshake. The loopback boundary is the security boundary.
- Q: What P95 end-to-end latency target is acceptable for the render-frame stream (server render → client decode)? → A: P95 ≤ 200 ms at default 10 Hz cadence.
- Q: How does a newly-attached client obtain the current UI state on the state-event stream? → A: Future-only deltas on the stream + a new unary `GetHubState` snapshot RPC for rehydration. No in-stream snapshot event, no cursor replay.
- Q: In what wire form does a gRPC client describe a drawing to be rendered in the Hub Viewer? → A: A bounded declarative overlay DSL — typed primitives (line / polyline / polygon / rect / circle / path / text / image) with typed styling (stroke width, fill color, stroke color, opacity, font size, anchor). The hub translates each primitive to `SKPaint` + `SKPath`. No raw `SkPicture` blobs, no SVG, no direct serialisation of the internal `Scene` DU.
- Q: Which coordinate system do uploaded overlay elements live in? → A: Both — each element carries a `CoordinateSpace` enum (`World` / `Screen`). World-anchored elements transform with the camera (pan/zoom); screen-anchored elements stay fixed in viewport pixels.
- Q: What's the scoping + lifecycle model for uploaded overlay elements? → A: Per-client name-keyed layers. Each client manages N named layers; `PutLayer(name, primitives)` replaces a layer atomically, `DeleteLayer(name)` removes one. Layers auto-clean on client disconnect; they survive session boundaries. Clients cannot see or mutate each other's layers.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Headless session orchestration through scripting (Priority: P1)

An author of an external scripting client (a trainer bot runner, a CI
smoke-test harness, or an ops tool running on a remote workstation)
wants to launch, observe, and shut down a Hub-hosted BAR session
entirely through the gRPC service — without a human touching the Setup
tab, the Viewer tab, or the Style tab. The client picks a map,
configures teams and AIs, sets engine speed, chooses whether to start
paused, chooses whether to launch the graphical engine, launches,
drives admin actions, then stops the session. Everything a human
operator can do from the Hub's GUI to run one session is available as
a scripting-service call.

**Why this priority**: This is the core of the request. Today, a
scripting client can stream frames, submit bot commands, and drive the
admin channel, but session launch, lobby configuration, and most
Viewer/Style/Units/Cfg controls are GUI-only. Until a client can
orchestrate a full session without the GUI, headless automation has
to awkwardly co-launch a human-driven hub next to any script.

**Independent Test**: Start the Hub with no human interaction (e.g.
via a test harness), connect a scripting client over gRPC, run the
full loop `configure lobby → launch session → wait for Running →
receive at least one frame → stop session`, and verify the hub ends in
the idle state with no GUI clicks performed.

**Acceptance Scenarios**:

1. **Given** a freshly-started Hub with no active session, **When** the
   scripting client calls the new "configure lobby" RPC(s) with a
   valid map name, team layout, AI seats, and engine speed, **Then**
   the Hub's lobby state reflects those values and `GetSessionStatus`
   (or an equivalent query) returns the same values.
2. **Given** a validly-configured lobby, **When** the client calls the
   new "launch session" RPC with `startPaused = true` and
   `launchGraphicalViewer = false`, **Then** the session transitions
   `Starting → Running` and the engine is paused at frame zero.
3. **Given** a running session, **When** the client calls the new
   "stop session" RPC, **Then** the session transitions to the idle
   state and any subsequent `StreamGameFrames` subscriber observes no
   further frames for that session.
4. **Given** an invalid lobby configuration (unknown map, empty team,
   engine speed ≤ 0), **When** the client calls the "configure lobby"
   RPC, **Then** the service returns a validation error and makes no
   state change.

---

### User Story 2 - Remote clients see the same pixels as the local Viewer tab (Priority: P1)

The scripting client wants to display to its own user (or record to
disk, or feed into an image-diff assertion) the exact image the Hub's
Viewer tab is showing — terrain base layer, unit glyphs with overlays,
events, economy HUD, admin status line, everything. Today the Viewer
tab renders a custom Skia scene into its local window; remote clients
only receive the raw game-frame envelope and have to reimplement
rendering themselves. This story closes that gap.

**Why this priority**: The user explicitly called this out ("custom
Skia drawings in the hubviewer available to gRPC"). Without it, a
remote operator can't see what a local operator sees, which makes
headless demos, CI screenshot baselines, and remote monitoring
impractical.

**Independent Test**: Launch a session with a fixed-seed synthetic
scene. A scripting client subscribes to the new render-frame stream
and saves the first N frames to PNG; a local observer captures the
same frames directly from the Viewer-tab window. Compare the two sets
— they should be visually equivalent (per SC-003).

**Acceptance Scenarios**:

1. **Given** a running session with the Viewer tab visible, **When**
   the client subscribes to the new render-frame stream at 10 frames
   per second, **Then** it receives a continuous stream of rendered
   viewer frames as image bytes whose content matches what the local
   Viewer tab is displaying.
2. **Given** a running session, **When** the client calls the new
   "screenshot" RPC, **Then** it receives a single rendered image
   matching the current Viewer-tab content.
3. **Given** the client toggles a viz overlay (e.g. weapon ranges) via
   the new configuration RPC, **When** the next frame arrives on the
   render-frame stream, **Then** the new frame reflects the overlay
   change.
4. **Given** no session is running, **When** the client subscribes to
   the render-frame stream, **Then** the stream stays open and emits
   no frames (or a single neutral "no-session" placeholder) rather
   than erroring out.

---

### User Story 3 - Live control of Viewer + Configurator state (Priority: P2)

The scripting client wants to drive the Hub's Viewer and Style-tab
controls — toggle each overlay (units / events / grid / metal spots /
economy HUD / weapon ranges / sight ranges / command queue / full
names), change the base layer, edit glyph-style attributes (colors,
sizes, opacities), and pan/zoom/auto-fit the camera — just as a human
would with mouse, sliders, and the W/L/C/N hotkeys. This gives the
client full "what the operator sees and how" control without the GUI.

**Why this priority**: Needed for reproducing a specific on-screen
state (e.g., in a training dashboard or a demo recording). Less
critical than launching sessions (US1) or capturing frames (US2), but
required for true UI parity.

**Independent Test**: Open a session, set a specific `VizConfig` via
the new RPC, capture a frame via US2, then reset `VizConfig` and
re-apply — confirm the second capture matches the first.

**Acceptance Scenarios**:

1. **Given** a running session, **When** the client pushes a full
   `VizConfig` update (base layer + overlay toggles + glyph style +
   colors + opacities), **Then** the Viewer tab immediately reflects
   every change.
2. **Given** a running session, **When** the client calls the per-
   overlay toggle RPCs (equivalent to W / L / C / N plus the rest of
   the overlay set), **Then** each overlay toggles on or off within
   one render frame.
3. **Given** a running session, **When** the client sets the camera
   state (scale, origin, auto-fit), **Then** the next rendered frame
   reflects the new view; setting auto-fit = true re-letterboxes.
4. **Given** a running session, **When** the client calls a "set
   active tab" RPC, **Then** the Hub GUI (if visible) switches to that
   tab and subsequent state events reflect the new tab.

---

### User Story 6 - Client-authored overlays drawn on the Hub Viewer (Priority: P2)

Scripting clients want to decorate the Hub Viewer with their own drawings —
threat maps, annotations, route lines, status HUDs, debugging overlays —
without having to fork the Hub's renderer. The client uploads a named "layer"
containing a bounded list of typed primitives (lines, polygons, circles,
text, etc.) with explicit styling and a coordinate-space hint; the hub
renders every layer on top of its built-in overlays every frame. Clients
can update or delete their layers at any time. Since US2 then streams the
composed Viewer image back, a remote operator sees a fully decorated scene.

**Why this priority**: Completes the "bi-directional rendering" story the
user called out — US2 sends rendered pixels server → client, US6 sends
declarative primitives client → server. Without US6 a client can observe
the Viewer but cannot augment it; any annotation has to live in the
client's own GUI. Ranked P2 (alongside US3) because it's not on the
critical path for headless session orchestration (US1) or viewer mirroring
(US2) but is needed for every scripted-dashboard and tactical-planner
use case.

**Independent Test**: Connect a scripting client, upload a layer
`test-1` containing one world-anchored red circle at a known map
coordinate plus one screen-anchored yellow label at `(20, 20)`. Capture
a viewer frame (via US2). Verify the circle appears at the expected map
position (moves when the camera pans) and the label appears at viewport
pixel `(20, 20)` (stays fixed when the camera pans). Replace `test-1`
with a single polygon via `PutLayer`; verify the previous primitives
are gone and the polygon is drawn. Disconnect the client; reconnect a
second client and verify `test-1` is absent (per-client scope).

**Acceptance Scenarios**:

1. **Given** a running session, **When** a client calls `PutLayer` with
   layer name `threat`, containing a mix of world-anchored and
   screen-anchored primitives, **Then** the next rendered frame shows
   every primitive with the correct style and coordinate transform.
2. **Given** a client has uploaded layer `threat`, **When** the client
   calls `PutLayer` again on the same name with a new primitive list,
   **Then** the previous primitives are replaced atomically (no frame
   ever shows a partial mix of old and new primitives).
3. **Given** a client has uploaded layer `threat`, **When** the client
   calls `DeleteLayer("threat")`, **Then** the next frame no longer
   contains any of its primitives.
4. **Given** a client has uploaded multiple layers, **When** the client
   disconnects from the scripting service, **Then** all of its layers
   are cleared; no other client's layers are affected.
5. **Given** a layer is uploaded with a malformed element (unknown
   primitive kind, out-of-range color, NaN coordinate), **When** the
   client calls `PutLayer`, **Then** the service returns
   `INVALID_ARGUMENT` and no layer mutation is performed.
6. **Given** a client attempts to upload a layer that exceeds the
   per-layer element cap or the per-client layer cap, **When** the
   client calls `PutLayer`, **Then** the service returns
   `RESOURCE_EXHAUSTED` and no layer mutation is performed.

---

### User Story 4 - Preset, encyclopedia, and settings parity (Priority: P3)

The scripting client wants to manage style presets (list / save /
load / delete), filter and select units in the Encyclopedia (Units)
tab, query and update user-persisted `HubSettings` (start-paused
default, graphical-viewer default, and any other user-editable
fields), and drive the Settings (Cfg) tab's proxy-install actions.

**Why this priority**: Needed for full UI parity but rarely on the
critical path for orchestrating a single match. Useful for dev loops,
automated UI smoke tests, and setup automation.

**Independent Test**: Save a preset via gRPC, disconnect and
reconnect, load the preset back, and verify the loaded `VizConfig`
matches what was saved.

**Acceptance Scenarios**:

1. **Given** an existing set of saved presets on disk, **When** the
   client calls "list presets", **Then** every file returned matches
   what the Style-tab dropdown shows.
2. **Given** a running hub, **When** the client calls "save preset X"
   with a `VizConfig`, **Then** the on-disk preset file is created and
   "load preset X" later restores a byte-equivalent `VizConfig`.
3. **Given** a running hub, **When** the client calls "list units
   filtered by faction=Armada", **Then** it receives the same set the
   Encyclopedia tab would show with the Armada chip active.
4. **Given** the user has the "Start paused" checkbox toggled off,
   **When** the client flips that setting via RPC, **Then** the value
   persists to `settings.json` and the next Hub launch reflects it.
5. **Given** the proxy is not installed, **When** the client calls
   the "install proxy" RPC, **Then** the proxy install runs and the
   Settings-tab proxy-status query returns the new version.

---

### User Story 5 - Remote observation of Hub UI state changes (Priority: P3)

When multiple clients (and the local GUI) are driving Hub UI state,
every client wants a real-time stream of state-change events (active
tab changed, viz config changed, lobby config changed, preset
created/deleted, encyclopedia selection changed, session state
transitioned, admin-channel status changed). This lets external
dashboards mirror the Hub's UI and keeps concurrent clients in sync.

**Why this priority**: Useful but optional. The single-client case
works without it (clients can poll `GetSessionStatus` or track their
own state). It becomes load-bearing only when multiple clients or a
GUI-plus-script setup need consistent views.

**Independent Test**: Connect two scripting clients; have one mutate
a VizConfig field; verify the second client receives a corresponding
state-change event before any GUI refresh.

**Acceptance Scenarios**:

1. **Given** two scripting clients subscribed to the state-event
   stream, **When** one client mutates `VizConfig`, **Then** both
   clients (and the local GUI) reflect the same final state within
   one render frame.
2. **Given** a client subscribed to the state-event stream, **When**
   the human operator toggles the "Start paused" checkbox on the
   Setup tab, **Then** the client receives a state-change event
   describing the new value.
3. **Given** a client subscribed to the state-event stream, **When**
   a session transitions `Idle → Starting → Running → Ending → Idle`,
   **Then** the client receives one event per transition.

---

### Edge Cases

- Render-frame stream subscribed with no session active: stream stays
  open and silent (or emits a single neutral placeholder frame) — it
  does not error.
- Render-frame cadence request exceeds the Hub's native render rate:
  the service caps delivery to the native rate and labels each frame
  with its actual timestamp so clients can still compute skew.
- Render-frame subscribers detach on cumulative backpressure drops,
  consistent with the existing `StreamGameFrames` drop-oldest /
  detach-at-32-cumulative policy.
- Client pushes a `VizConfig` with an unknown field, out-of-range
  value, or malformed color: service returns an invalid-argument error
  and makes no state change.
- Two clients push conflicting state within a short window: last
  write wins, and every intermediate write is visible as a distinct
  event on the state-event stream.
- Preset save with a reserved or filesystem-invalid name (empty,
  traversal, illegal chars): service returns an invalid-argument
  error; no file is created.
- Lobby configuration is edited via gRPC while a session is already
  running: the service rejects the edit until the session has ended
  (the Setup tab is likewise locked while running).
- Hub started with no DISPLAY available (e.g., for frame capture in
  CI): out of scope for this feature — see Assumptions. The hub still
  needs a windowing environment for the Viewer surface to render
  into.
- Client requests a screenshot during a `Starting` or `Ending`
  transition: the service returns whatever the Viewer tab last
  rendered, or a neutral placeholder if no render has happened.
- `PutLayer` with zero primitives: accepted; equivalent to
  `DeleteLayer` for that name.
- `PutLayer` with a primitive whose coordinates are NaN / infinite,
  color with invalid rgba range, or stroke width ≤ 0: rejected with
  `INVALID_ARGUMENT`; existing layer of that name unchanged.
- Image primitive exceeds a sanity size (e.g. > 2048 × 2048 pixels or
  > 256 KB encoded bytes): rejected with `INVALID_ARGUMENT`.
- Client attempts to `DeleteLayer` / `PutLayer` a name not owned by
  the caller (either another client's name or one never created):
  `DeleteLayer` is a no-op with `SENT`; `PutLayer` always creates or
  replaces within the caller's own namespace so a name collision
  with another client is impossible.
- Overlay layer references a font family that's not installed: the
  hub substitutes a default sans-serif font and still renders; the
  service does not error.
- Client rapidly uploads layers just below the per-push cap: accepted
  individually; no aggregate rate limit in v1 since the loopback
  boundary is the trust boundary (consistent with FR-017).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Hub scripting service MUST expose gRPC methods that
  set the full lobby configuration — map name, mode (Skirmish / FFA /
  Team), engine speed, team layout, AI seats, human seats — matching
  every field a human can edit on the Setup tab.
- **FR-002**: The Hub scripting service MUST expose a method to
  launch a session with `startPaused` and `launchGraphicalViewer`
  flags, matching the Setup-tab Launch button.
- **FR-003**: The Hub scripting service MUST expose a method to stop
  or abort the current session.
- **FR-004**: The Hub scripting service MUST expose methods to list
  the set of available maps and to query lobby-validation errors for
  the current lobby configuration.
- **FR-005**: The Hub scripting service MUST expose a server-streaming
  method that delivers rendered Viewer-tab frames as image bytes at a
  client-chosen cadence (default 10 Hz, capped at the Hub's native
  render rate). Each frame MUST include every overlay currently
  active in the Viewer tab (base layer, units, events, grid, metal
  spots, economy HUD, weapon ranges, sight ranges, command queue,
  full names, and the admin-channel status line). End-to-end delivery
  latency (server render → client decode) MUST meet the target in
  SC-008.
- **FR-006**: The Hub scripting service MUST expose a unary method
  to capture and return the current Viewer-tab frame on demand
  (single-frame screenshot).
- **FR-007**: The Hub scripting service MUST expose a method to push
  a full `VizConfig` update, mutating every attribute exposed in the
  Configurator (Style) tab — base layer choice, overlay toggles,
  overlay opacity, grid spacing, unit marker size, glyph style fields
  (faction / team palettes, stroke widths, pip radius, HP arc width,
  label font size, zoom thresholds), and per-layer color schemes.
- **FR-008**: The Hub scripting service MUST expose convenience
  methods for individual overlay toggles equivalent to the Viewer-tab
  hotkeys `W` (weapon ranges), `L` (sight), `C` (command queue), `N`
  (full names), plus the remaining overlays (units, events, grid,
  metal spots, economy HUD).
- **FR-009**: The Hub scripting service MUST expose a `SetCamera`
  RPC to mutate the Viewer tab's camera state (pan origin X / Y,
  zoom scale, auto-fit boolean). The current camera value is
  readable via the `GetHubState` snapshot (FR-015a); reset-to-
  defaults is achieved by calling `SetCamera` with `auto_fit = true`.
- **FR-010**: The Hub scripting service MUST expose methods for
  preset management — list, save, load, delete — matching the
  Configurator-tab preset controls and operating against the same
  on-disk preset store (`viz-presets/*.json`).
- **FR-011**: The Hub scripting service MUST expose methods to list
  units (grouped by faction), filter the list by a set of factions,
  select a unit by internal name or definition id, and retrieve the
  detail view (cost, health, footprint, sight range, weapon ranges)
  for the selected unit — matching the Encyclopedia (Units) tab.
- **FR-012**: The Hub scripting service MUST expose read/write access
  to every user-editable `HubSettings` field, including at minimum
  `StartPausedDefault` and `LaunchGraphicalViewerDefault`. Writes
  MUST persist to `$XDG_CONFIG_HOME/fsbar-hub/settings.json` exactly
  as the local GUI does.
- **FR-013**: The Hub scripting service MUST expose methods to
  trigger Settings (Cfg) tab actions — install proxy, force-reinstall
  proxy, refresh proxy status — and to query the current proxy-install
  status.
- **FR-014**: The Hub scripting service MUST expose a method to set
  the Hub's active tab (Setup / Viewer / Units / Style / Cfg / gRPC),
  matching the `FSBAR_HUB_INITIAL_TAB` environment variable.
- **FR-015**: The Hub scripting service MUST expose a server-streaming
  method carrying every UI state-change event — active tab, viz
  config, lobby config, preset create/delete, encyclopedia selection,
  session state transition, admin-channel status change, hub settings
  change, proxy install progress — so concurrent clients (and the
  local GUI) stay in sync. The stream is
  future-only: on subscribe, a client receives events from that
  instant forward; it does NOT receive a synthetic snapshot of
  pre-subscription state. Clients rehydrate via the snapshot RPC
  defined in FR-015a.
- **FR-015a**: The Hub scripting service MUST expose a unary
  `GetHubState` RPC that returns the current value of every UI-state
  field emitted on the FR-015 stream (active tab, `VizConfig`, lobby
  config, preset list, encyclopedia selection, session state,
  admin-channel status). Clients combine one `GetHubState` call with
  an FR-015 subscription to reconstruct full state. Any field
  readable via a dedicated query RPC (e.g. `GetSessionStatus`) MUST
  return values consistent with `GetHubState` at the same instant.
- **FR-016**: State mutations from multiple clients MUST follow
  last-write-wins semantics; every successful mutation MUST generate
  a distinct event on the state-change stream.
- **FR-017**: The Hub scripting service MUST remain bound to
  loopback (`127.0.0.1`) on the same port assignment scheme as today.
  Exposing additional UI control MUST NOT loosen the network surface.
  Every loopback client is equally trusted — there is no driver /
  observer distinction, no role handshake, and no per-RPC permission
  check. The loopback boundary is the sole security boundary.
- **FR-018**: Every user-facing action in the Hub GUI as of feature
  039 (hub admin channel) — excluding OS-level window chrome — MUST
  have at least one corresponding gRPC entry point after this
  feature. A coverage audit MUST document the mapping for each tab.
- **FR-019**: Existing scripting-service methods (`StreamGameFrames`,
  `SendCommand`, `GetSessionStatus`, `GetUnitDef`, `Pause`, `Resume`,
  `SetEngineSpeed`, `ForceEndMatch`, `SendAdminMessage`) MUST
  continue to work with their pre-feature semantics unchanged;
  existing clients MUST NOT require recompilation to keep calling
  them.
- **FR-020**: Any mutation performed via gRPC MUST produce the same
  visible end-state as the equivalent GUI interaction — e.g. saving a
  preset over gRPC writes the same on-disk file format and the local
  Style tab's preset dropdown picks it up on its next refresh.
- **FR-021**: The Hub scripting service MUST expose unary RPCs for
  per-client, name-keyed overlay-layer management: `PutLayer(name,
  primitives, zHint)` to create or atomically replace a layer,
  `DeleteLayer(name)` to remove one, `ListLayers` to enumerate a
  client's own layers, and `ClearLayers` to drop all of them. Layer
  names are opaque to the hub (UTF-8, 1..64 code points, no path
  separators). Clients cannot read, mutate, or delete another
  client's layers.
- **FR-022**: The overlay-primitive DSL MUST support at minimum the
  following typed primitives: `Line`, `Polyline`, `Polygon`,
  `Rectangle`, `Circle`, `Path` (sequence of move/line/cubic/close
  verbs), `Text` (string, font size, anchor), and `Image` (inline
  `bytes` PNG/JPEG, bounded size). Each primitive carries styling:
  stroke color (rgba), stroke width, fill color (rgba, optional),
  opacity, plus primitive-specific fields (font family/size, text
  anchor, image dimensions). Unknown primitive kinds or invalid
  styling fail validation with `INVALID_ARGUMENT`.
- **FR-023**: Every overlay primitive MUST carry a `CoordinateSpace`
  enum (`World` | `Screen`). `World` primitives are transformed by
  the current `ViewerCamera` (pan/zoom); `Screen` primitives are
  painted in viewport pixel space unaffected by camera state. A
  single layer MAY mix both spaces. `World` coordinates are in BAR
  engine **elmo units** (same units as `MapGrid.width` /
  `MapGrid.height` and unit positions); `Screen` coordinates are in
  viewport pixels with origin at the top-left.
- **FR-024**: Uploaded overlay layers MUST render on top of every
  built-in overlay (units, events, grid, metal spots, economy HUD,
  weapon/sight ranges, command queue, full names, admin status line).
  Within the client-uploaded layer set, layers MUST be ordered by a
  per-layer integer `zHint` (ascending, tied layers ordered by most
  recent upload time).
- **FR-025**: Overlay layers are ephemeral server-side state: on
  client disconnect (clean or overflow-detach) the hub MUST delete
  every layer owned by that client. Layers MUST survive session
  boundaries (`Running → Idle → Running`) — only the owning client
  can cause removal.
- **FR-026**: The hub MUST enforce per-client resource caps on
  overlay state: at most 16 layers per client, at most 500 primitives
  per layer, at most 1 megabyte total serialized `PutLayer` request
  size. Exceeding any cap returns `RESOURCE_EXHAUSTED` with a reason
  identifying the exceeded limit and leaves existing layers
  unchanged.
- **FR-027**: Uploaded layers MUST be included in the composed image
  delivered by the render-frame stream (FR-005) and the `GetRenderFrame`
  unary RPC (FR-006). Every layer's primitives are drawn every frame
  until the owning client replaces, deletes, or disconnects.

### Key Entities

- **Lobby configuration**: the data a human enters on the Setup tab —
  selected map, match mode, engine-speed multiplier, team layout, AI
  seats, human seats, per-session options (start-paused,
  graphical-viewer). Mutable only when no session is running.
- **Visualization configuration (`VizConfig`)**: the user-editable
  style state — base layer, overlay toggle set, overlay opacity, grid
  spacing, unit marker size, glyph-style fields, per-layer color
  schemes. Mutable at any time; changes apply within one render frame.
- **Style preset**: a named saved `VizConfig` snapshot persisted as a
  JSON file under `viz-presets/`. List / save / load / delete scope.
- **Viewer frame**: a single rendered image of the Viewer tab at a
  specific instant, carrying every active overlay. Delivered as image
  bytes with a timestamp and sequence number.
- **Camera state**: the Viewer tab's pan origin, zoom scale, and
  auto-fit flag.
- **Hub UI state event**: a notification of a state-change (which
  kind, old and new values, source client id or "gui") propagated to
  every subscribed client and mirrored into the local GUI's state.
- **Hub settings**: user-persisted defaults (start-paused,
  launch-graphical-viewer, and any other read/write `HubSettings`
  fields) stored in `$XDG_CONFIG_HOME/fsbar-hub/settings.json`.
- **Proxy install status**: the version, install path, and health of
  the bundled or installed HighBarV2 proxy; matches what the Settings
  tab displays.
- **Overlay layer**: a per-client, name-keyed ordered list of overlay
  primitives with a `zHint` integer. Atomically replaceable via
  `PutLayer`; auto-deleted on client disconnect. Scoped to its owning
  client — not visible to or mutable by others.
- **Overlay primitive**: one typed drawing instruction (`Line`,
  `Polyline`, `Polygon`, `Rectangle`, `Circle`, `Path`, `Text`,
  `Image`) with a `CoordinateSpace` (`World` | `Screen`), styling
  fields (stroke/fill color, stroke width, opacity, font size, text
  anchor), and primitive-specific geometry fields.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A scripting client can run a full headless trainer
  cycle (configure lobby → launch session → observe → stop session)
  with no human GUI interaction on three representative maps
  (Avalanche 3.4, Red Comet Remake 1.8, Titan v2). Each map must
  succeed in ≥ 19 of 20 trial runs (≥ 95%), measured in the polish
  phase.
- **SC-002**: An action-coverage audit of every user-facing action in
  the Hub GUI (across the six tabs as of feature 039) shows 100% of
  actions have at least one corresponding gRPC entry point. Any
  intentional exception is documented with a rationale.
- **SC-003**: Frames delivered over the render-frame stream match the
  local Viewer-tab output pixel-for-pixel on ≥ 99% of pixels for a
  fixed-seed synthetic scene, within a fixed viz configuration.
- **SC-004**: Round-trip latency for preset save → load → matching
  `VizConfig` read is under 500 ms on a Hub with one idle
  `SessionManager` (`State = Idle`), ≤ 2 scripting clients attached,
  and no concurrent `StreamRenderFrames` subscriptions (measured
  end-to-end from the client).
- **SC-005**: Under concurrent writes from two clients within a
  100 ms window on the same field, all observing subscribers
  eventually converge on the same final value, and every intermediate
  write is visible as a distinct event on the state-change stream.
- **SC-006**: Adding a new Hub UI action is trivially extensible and
  does not require reshaping existing RPCs. Extension classes: (a) a
  new `VizConfig` attribute registers one new `ConfigDescriptors.all`
  entry and needs no proto change; (b) a new gRPC action adds one
  new proto message + one new hub-side handler. Verified during this
  feature by doing one class-(a) extension in the polish phase and
  recording lines changed in `coverage-audit.md`.
- **SC-007**: All existing scripting-service clients (including
  `scripts/examples/16-hub-admin.fsx`) continue to work without
  recompilation against the updated service.
- **SC-008**: The render-frame stream delivers frames with a P95
  end-to-end latency (server render complete → client-side decoded
  image available) of ≤ 200 ms at the default 10 Hz cadence, measured
  over a loopback client against a fixed-seed synthetic scene.
- **SC-009**: A `PutLayer` call with a 50-primitive mixed World /
  Screen layer is reflected in the next rendered frame within one
  render tick (≤ 100 ms end-to-end at the default 10 Hz cadence) on
  a normally-loaded hub.
- **SC-010**: A client that disconnects while owning N layers has all
  N layers removed from server state before the next render tick;
  observable by a second client subscribing to `StreamRenderFrames`
  seeing no residual primitives within two frames.

## Assumptions

- **Scripting-service security boundary unchanged**: the service
  remains loopback-bound (`127.0.0.1:5021`). Any remote access still
  requires the operator to set up a TCP tunnel or port-forward.
- **Rendered-frame wire format defaults to PNG-encoded image bytes**.
  A simple format enum (PNG / JPEG) is provided, but structured
  scene-tree export is out of scope for this feature. If bandwidth-
  efficient structured export is wanted later, it is a follow-up.
- **Default render-frame stream cadence is 10 Hz**, client-adjustable
  up to the Hub's native render rate. This matches "see what the
  operator sees" expectations without drowning scripts in data.
- **Hub still requires a windowing environment** (DISPLAY + GLFW) to
  render the Viewer surface. Truly headless rendering is OUT of
  scope; the supported pattern for automation is
  "graphical hub with no human input" (e.g. `FSBAR_HUB_AUTO_LAUNCH`
  plus the new RPCs) rather than "no-GUI hub".
- **Multi-client concurrency uses last-write-wins**; clients relying
  on stronger consistency subscribe to the state-change stream and
  reconcile.
- **Existing scripting-service wire surface is additive-only**. No
  pre-feature RPC is renamed, removed, or re-shaped. New RPCs and
  new message fields are added.
- **Proto regeneration uses the same `cd proto && buf generate`
  workflow** documented in `CLAUDE.md`. Generated files continue to
  live under `src/FSBar.Proto/Generated/`.
- **No new runtime dependencies** are added; the feature reuses
  `Grpc.AspNetCore` / `Grpc.Core.Api` already in the graph and
  `SkiaSharp` for PNG encoding. The overlay DSL (US6) is also
  rendered with `SkiaSharp` primitives already in the graph.
- **Overlay caps default to 16 layers per client, 500 primitives per
  layer, 1 MB per `PutLayer` request, 2048×2048 / 256 KB per image
  primitive.** These are static defaults in v1; a later feature can
  promote them to `HubSettings` if operators need tuning.
- **Overlay layers render above every built-in overlay.** Z-ordering
  inside the client-uploaded layer set is by per-layer `zHint`
  ascending; built-in overlays do not expose a z slot that client
  layers can interleave with in v1.
- **Out of scope**:
  - Any new Hub UI tab or surface not already present as of feature
    039 (the hub admin channel).
  - Any change to the bundled-proxy wire format, the game-frame wire
    format, or the admin-channel wire format.
  - Any change to the local Hub GUI's layout, behaviour, or hotkeys
    (the GUI merely picks up the new server-side capabilities).
  - Authentication / authorization — the service stays loopback-only,
    unchanged from feature 035.
  - Truly headless rendering (no DISPLAY). A later feature may add
    this.
  - Scripting bindings in non-F#/.NET languages; the existing buf /
    proto contract already allows codegen for any language the user
    wires up.
