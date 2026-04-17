# Feature Specification: Central GUI Hub App

**Feature Branch**: `035-central-gui-hub`
**Created**: 2026-04-17
**Status**: Draft
**Input**: User description: "i want a gui app as the center of the whole app. it should start first as a process and have gui to configure map/enemies/live original game viewer/allies/gamemode/speed/... it should also incorporate the configurator. it should also have an encyclopedia of all units/buildings their ui representation and their data. it should also incorporate the skia live game viewer. it should also expose grpc endpoints for scripting clients. server sends gamestates to client. client sends commands to server. use fsgrpc skills to build this. it should also have configuration options for bar location. it should also install the proxy in chobby see bar.info.md for requirements."

## Clarifications

### Session 2026-04-17

- Q: What player/team/mode scope should v1 support? → A: Full lobby parity — variable team count, human + AI seats, Skirmish / FFA / Team modes, spectator seats, handicaps.
- Q: In v1, does the hub attach to Chobby-launched sessions or only its own? → A: Hub-launched sessions only. Chobby install is for enabling HighBarV2 in human play; the hub does not observe those games.
- Q: Where does the hub obtain the HighBarV2 proxy files it installs? → A: Bundled binary committed to this repo under a known path, refreshed by a maintainer script that copies from a sibling HighBarV2 build. Self-contained checkout; users do not need their own HighBarV2 checkout.
- Q: During a running session, how does the user reach other hub areas? → A: Persistent sidebar / tab bar always visible. Viewer, session setup, encyclopedia, configurator, settings, and gRPC status are sibling tabs reachable at any time regardless of session state.
- Q: What runtime controls on a live session must v1 support? → A: Speed + pause/resume + end-session, exposed on a persistent session-status bar visible from every tab.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — One-click BAR session from the hub (Priority: P1)

A user opens the hub app, picks a map, chooses which AI plays on which team (allies / enemies), sets the game mode and engine speed, and clicks **Launch**. A BAR session spins up, the proxy AI is loaded into the engine, and the embedded live viewer begins rendering game state as soon as the first frame arrives.

**Why this priority**: This is the hub's reason to exist. Every other feature (viewer, encyclopedia, configurator, scripting API) only matters once a session is running. Without this, the hub is a tech demo. With it, the hub replaces the current ad-hoc `run.sh` + script editing workflow.

**Independent Test**: Starting from a clean hub install (no open sessions), the user selects Map = "Avalanche 3.4", Ally = "HighBarV2", Enemy = "BARb", Mode = "Skirmish", Speed = "1.0x", and clicks Launch. Within 30 seconds a session is running, the proxy is producing state, and the viewer shows both teams' starting units.

**Acceptance Scenarios**:

1. **Given** the hub is open on the session-setup screen, **When** the user fills in required fields and clicks Launch, **Then** the hub starts an engine process configured with those choices and displays session status ("starting", "running", "ended") in real time.
2. **Given** a running session, **When** the user changes the engine speed slider, **Then** the engine speed updates live and the viewer frame rate reflects the change.
3. **Given** the user picks a map that is not installed locally, **Then** the hub blocks Launch and surfaces a clear message identifying the missing map — it does not fail silently or crash.
4. **Given** a session is already running, **When** the user clicks Launch again, **Then** the hub prompts before replacing the current session.

---

### User Story 2 — First-run setup: locate BAR and install the proxy (Priority: P1)

The first time the hub starts on a machine, it walks the user through locating their BAR install, verifying the engine and data directories exist, and installing the HighBarV2 proxy AI so it shows up in Chobby's bot dropdown and can be launched by the hub. The hub handles the three required steps from `docs/bar-info.md`: copying the AI files into the engine directory, creating `devmode.txt`, and setting `simpleAiList = false` in the Chobby config.

**Why this priority**: Without this, User Story 1 fails on any machine that hasn't already been hand-configured using the bar-info cheat-sheet. Bundling the setup into the hub removes an undocumented precondition and makes the app self-contained.

**Independent Test**: On a machine where BAR is installed but the proxy has never been set up, launching the hub triggers a first-run wizard. Completing the wizard leaves the proxy files in place under the detected engine version, `devmode.txt` present, and `simpleAiList = false` in `IGL_data.lua` — verifiable by listing those paths and by launching Chobby and seeing HighBarV2 in the AI dropdown.

**Acceptance Scenarios**:

1. **Given** a fresh machine with BAR installed at the default location, **When** the user opens the hub, **Then** it auto-detects the BAR data and engine directories and asks the user to confirm before writing anything.
2. **Given** BAR is installed at a non-default location, **When** the user points the hub at that path in settings, **Then** the hub validates it contains the expected `engine/recoil_*/` subtree and persists the location for future launches.
3. **Given** the proxy is already installed but an older version than the one bundled with the hub, **When** the hub starts, **Then** it detects the stale install and offers to upgrade in place.
4. **Given** the engine has been updated to a newer `recoil_*` directory, **When** the hub next launches, **Then** it notices the newer engine, offers to reinstall the proxy under it, and clearly indicates which engine version is now active.
5. **Given** the user has never touched `IGL_data.lua`, **When** the hub toggles `simpleAiList`, **Then** it does so via a targeted edit (the key's value) and does not otherwise rewrite or reorder the file.

---

### User Story 3 — Embedded live viewer as the hub's primary surface (Priority: P2)

While a session is running, the hub's Viewer tab shows the Skia live game viewer, with overlays (weapon ranges, sight, commands, names) toggleable from the hub's own controls in addition to the existing in-viewer hotkeys. The user can switch to other tabs (Setup, Encyclopedia, Configurator, Settings, gRPC) mid-session without interrupting the viewer or the session. When the session ends, the Viewer tab clears but remains present in the nav.

**Why this priority**: The existing viewer is already built; integrating it as the hub's main view turns the hub into a usable cockpit rather than just a launcher. This is the user's primary tool for watching what the proxy is doing.

**Independent Test**: With a session running, the hub's main pane shows the same map + units the standalone viewer does, responds to the same overlay hotkeys, and the hub's own overlay toggles (buttons / menu items) produce the same effect as the keyboard hotkeys.

**Acceptance Scenarios**:

1. **Given** a session is running, **When** the first gamestate frame arrives, **Then** the viewer pane displays the map terrain plus both teams' starting units within one frame.
2. **Given** the viewer is showing a running session, **When** the user clicks overlay toggle buttons in the hub chrome, **Then** the viewer updates accordingly and the button state and hotkey state stay synchronized.
3. **Given** the session ends (either normally or by crash), **When** the hub detects the end, **Then** the Viewer tab clears, the Setup tab is reselectable, and any error details from the engine are surfaced.
4. **Given** a session is running on the Viewer tab, **When** the user switches to the Encyclopedia or Configurator tab, **Then** the session, viewer frame pump, and gRPC stream continue unaffected; switching back to Viewer resumes the live display.

---

### User Story 4 — Optional launch of the original BAR graphical engine (Priority: P2)

When configuring a session, the user can toggle "Launch original BAR viewer". If enabled, the hub starts the graphical `spring` binary (windowed) in parallel with the proxy, so the user can watch the session in BAR's own renderer alongside the hub's Skia viewer. If disabled, only the headless engine is used.

**Why this priority**: The user called this out explicitly ("live original game viewer"). It lets the user sanity-check the Skia renderer against the ground truth and watch sessions in the familiar native UI. It is an add-on to User Story 1, not a replacement.

**Independent Test**: Launch a session with the toggle on — a spring graphical window appears, windowed, showing the same session the Skia viewer is rendering. Turn the toggle off, launch again — no graphical window appears and the session runs headless.

**Acceptance Scenarios**:

1. **Given** the toggle is on, **When** the session starts, **Then** the graphical engine window is windowed (not fullscreen), and closing that window does not tear down the hub or the session.
2. **Given** the toggle is on but the graphical engine binary is missing, **When** the user clicks Launch, **Then** the hub blocks with an explanatory message and does not start the session at all.

---

### User Story 5 — Unit and building encyclopedia (Priority: P3)

From the hub's main navigation, the user can open an encyclopedia browsing every unit and building from BarData. Each entry shows the unit's hub-visualization (the same glyph / color / shape the viewer uses) alongside its raw data (cost, health, build time, weapon profile, movement, build options). The list is filterable by faction, tier, and role.

**Why this priority**: Essential for understanding what the proxy is doing and for interpreting the viewer, but not required to launch or watch a session. Ships after the core session loop works.

**Independent Test**: Open the encyclopedia with no session running. Every unit returned by `BarData.AllUnitDefs` has an entry, the entry's glyph matches what the live viewer would render for the same unit type, and the filters narrow the list correctly.

**Acceptance Scenarios**:

1. **Given** the encyclopedia is open, **When** the user filters by faction "Armada", **Then** only Armada units are listed and the glyphs in the list use the Armada faction palette.
2. **Given** the encyclopedia is open, **When** the user selects a unit, **Then** the detail pane shows cost / health / weapons / build options pulled from the BarData definition plus the rendered glyph from the viz pipeline.
3. **Given** BarData has been updated to a newer version, **When** the hub starts, **Then** the encyclopedia reflects the new unit set without code changes.

---

### User Story 6 — Embedded style configurator (Priority: P3)

The existing viz style configurator (feature 033) is reachable from the hub without entering a separate mode. Changes apply immediately to the embedded viewer, and the user can save / load named presets from within the hub.

**Why this priority**: The configurator already exists; this story is about surfacing it consistently inside the hub. Useful but not blocking.

**Independent Test**: With a session running, open the configurator from the hub menu, change a slider, and confirm the viewer updates on the next frame. Save a preset, restart the hub, reopen the same preset, and confirm the saved values are restored.

**Acceptance Scenarios**:

1. **Given** a session is running and the configurator is open, **When** the user edits a color swatch, **Then** the viewer updates within one frame.
2. **Given** the user has saved named presets, **When** they open the hub on another day, **Then** those presets are still available and loadable.

---

### User Story 7 — gRPC scripting API for external clients (Priority: P3)

The hub exposes a gRPC server that external scripts (F# `.fsx`, Python, etc.) can connect to. Once connected, a scripting client subscribes to a gamestate stream (every tracked frame the hub sees) and can send commands that the hub forwards to the proxy's command queue. The server-to-client direction is gamestate; the client-to-server direction is commands.

**Why this priority**: Unlocks the trainer and other automation use cases, which today launch their own engine via `run.sh`. With the hub running persistently, scripts can attach/detach from live sessions without managing the engine lifecycle themselves. Depends on sessions being launchable (P1) to have anything to stream.

**Independent Test**: Start the hub, launch a session, then run a minimal external script that connects to the gRPC endpoint, prints the first 5 gamestate frames it receives, and sends one no-op command. The script succeeds end-to-end without having to know where the engine socket is.

**Acceptance Scenarios**:

1. **Given** a session is running and a scripting client connects, **When** the next gamestate tick is produced, **Then** the client receives a frame within the same polling cadence the embedded viewer does.
2. **Given** a connected scripting client issues a command, **When** the engine accepts it, **Then** the command's effect becomes visible in the next gamestate frame delivered to all connected clients.
3. **Given** a scripting client disconnects mid-stream, **When** the session continues, **Then** the hub does not crash and other clients continue to receive frames.
4. **Given** no session is running, **When** a scripting client connects, **Then** the connection succeeds but the gamestate stream stays empty until a session starts.

---

### Edge Cases

- BAR install path configured in the hub no longer exists (user uninstalled BAR or moved it) — the hub refuses to launch sessions and surfaces a clear "BAR install not found at X" message, not a crash.
- The engine's `recoil_*` directory is upgraded between hub launches — the hub detects the new directory and flags the proxy install as stale.
- A session process dies mid-game (crash, kill) — the hub detects the exit, tears down the viewer cleanly, and returns to the setup screen with any stderr captured.
- Two scripting clients send contradictory commands in the same tick — both are forwarded; the hub does not arbitrate. Last-writer-wins at the engine level is acceptable for v1.
- The Chobby config file (`IGL_data.lua`) has been hand-edited with unusual formatting — the hub's targeted edit must not corrupt the surrounding structure.
- `libSkirmishAI.so` on disk is newer than what the hub bundles (user built a local version) — the hub warns before overwriting and offers to skip.
- User closes the hub while a session is running — the hub prompts, and on confirmation tears down the session cleanly.

## Requirements *(mandatory)*

### Functional Requirements

**Hub lifecycle**

- **FR-001**: The hub MUST run as a long-lived process that starts before any BAR session it manages; closing the hub MUST tear down any sessions it launched.
- **FR-002**: The hub MUST expose its entire feature set through a graphical interface — session setup, viewer, encyclopedia, configurator, settings, and gRPC status — without requiring the user to drop to a terminal for core flows.
- **FR-002a**: The hub MUST present a persistent navigation (sidebar or tab bar) that lets the user switch between session setup, viewer, encyclopedia, configurator, settings, and gRPC status at any time — including while a session is running — without ending the session or opening extra windows.

**BAR install configuration**

- **FR-003**: The hub MUST detect a default BAR data directory on first run and allow the user to confirm or override both the data directory and the engine directory.
- **FR-004**: The hub MUST persist BAR install settings across restarts and re-validate them on startup.
- **FR-005**: When multiple `recoil_*` engine versions are present, the hub MUST let the user pick which one is active and default to the newest.

**Proxy install into Chobby**

- **FR-006**: The hub MUST install the HighBarV2 proxy AI files (`libSkirmishAI.so`, `AIInfo.lua`, `AIOptions.lua`) into the active engine's `AI/Skirmish/HighBarV2/<version>/` directory, sourcing them from a bundled copy committed to this repository under a known path (e.g., `proxy/bundled/<version>/`).
- **FR-006a**: The repository MUST ship a maintainer refresh script that copies the HighBarV2 build outputs from a sibling checkout into the bundled path and records the bundled version. Users MUST NOT need a HighBarV2 checkout of their own to install or run the hub.
- **FR-006b**: The bundled version string MUST be visible in the hub's settings/about surface so users can see which proxy revision they are running.
- **FR-007**: The hub MUST create (`touch`) `devmode.txt` in the BAR data directory when enabling developer mode, and MUST report whether this step is needed before performing it.
- **FR-008**: The hub MUST set `simpleAiList = false` in `LuaMenu/Config/IGL_data.lua` via a targeted edit of that key's value, leaving other keys and formatting untouched.
- **FR-009**: The hub MUST detect when the proxy install is missing, out-of-date, or installed under a stale engine version, and offer a one-click remediation.
- **FR-010**: The hub MUST NOT modify files in `packages/` or `pool/` (per `docs/bar-info.md`: "Do NOT Patch Pool Files").

**Session configuration and launch**

- **FR-011**: The hub MUST let the user configure a full lobby: a variable number of teams, each team containing one or more seats, where each seat is either an AI (picked from the installed skirmish AI list) or a human player; plus spectator seats, per-seat handicap, the map, the game mode (Skirmish / FFA / Team), and the engine speed.
- **FR-011a**: The hub MUST enforce the minimum seat constraints required to start a BAR match (at least two opposing sides each with at least one non-spectator seat) and surface a clear message naming which constraint is unmet before enabling Launch.
- **FR-011b**: The hub MUST persist the last-used lobby layout and offer it as the default on the next launch.
- **FR-012**: The hub MUST validate that the selected map is installed locally and that selected AIs are available before enabling Launch.
- **FR-013**: The hub MUST launch BAR sessions directly (without going through Chobby) in windowed mode when the graphical engine is involved, consistent with the existing `EngineLauncher` convention.
- **FR-014**: The hub MUST offer an opt-in toggle to also launch the BAR graphical engine (`spring`) in parallel with the session.
- **FR-015**: The hub MUST expose session status (starting, running, ended, failed) in its UI and make the Setup tab reselectable when a session ends.
- **FR-015a**: The hub MUST only observe and stream sessions it launched itself. Sessions started outside the hub (e.g., from Chobby using the installed proxy) MUST NOT cause the hub's viewer or gRPC stream to engage, even if a proxy connection is attempted against the hub's socket.
- **FR-015b**: The hub MUST render a persistent session-status bar, visible from every tab, that shows the current session state and exposes three runtime controls while a session is running: engine speed (adjustable live), pause/resume (toggleable), and end-session.
- **FR-015c**: The Pause control MUST suspend the engine's simulation such that no new gamestate frames are produced until the user resumes; Resume MUST restore the engine speed that was in effect before pausing.
- **FR-015d**: The End-Session control MUST gracefully tear down the engine process, any parallel graphical-viewer process, and clear the Viewer tab, without exiting the hub. Connected gRPC clients MUST remain connected and observe the gamestate stream stop naturally.

**Embedded live viewer**

- **FR-016**: The hub MUST embed the existing Skia live game viewer as its main pane while a session is running.
- **FR-017**: Overlay toggles (weapon ranges, sight, commands, full names, etc.) MUST be reachable both from the hub's chrome and via the existing in-viewer hotkeys, with the two states synchronized.
- **FR-018**: Viewer frame cadence MUST NOT exceed ~60fps regardless of engine tick rate, consistent with the existing throttling guidance in `CLAUDE.md`.

**Encyclopedia**

- **FR-019**: The hub MUST present an encyclopedia of all units and buildings derived from `BarData`, with one entry per unit.
- **FR-020**: Each encyclopedia entry MUST show the same visual representation (glyph, color, label) the live viewer uses for that unit, so the encyclopedia and viewer cannot drift.
- **FR-021**: Each encyclopedia entry MUST show the unit's raw data: cost, health, build time, weapon summary, movement / buildability, and build options.
- **FR-022**: The encyclopedia MUST support filtering by faction, tier, and role.

**Style configurator**

- **FR-023**: The hub MUST embed the existing style configurator such that it is reachable without leaving the hub and applies changes to the embedded viewer live.
- **FR-024**: The hub MUST provide save / load of named style presets from within the hub UI, reusing the existing preset file format.

**Scripting API (gRPC)**

- **FR-025**: The hub MUST run a gRPC server that accepts local client connections.
- **FR-026**: The hub MUST stream every gamestate snapshot the engine produces to each connected scripting client, independent of any viewer-side throttling. The viewer's ≤60 fps cap (FR-018) MUST NOT throttle the gRPC fan-out.
- **FR-027**: The hub MUST accept commands from gRPC clients and forward them to the active session's proxy command pipeline.
- **FR-028**: The hub MUST tolerate multiple concurrent scripting clients; no client MUST be able to starve or block another.
- **FR-029**: Client disconnection MUST NOT affect the session, other clients, or the embedded viewer.
- **FR-030**: When no session is running, the gRPC server MUST still accept connections; the gamestate stream stays empty until a session starts.

**Observability / diagnostics**

- **FR-031**: When a session fails to start or dies mid-game, the hub MUST surface the engine's stderr / infolog excerpt rather than only a generic error.

### Key Entities

- **BAR Install**: A pointer to a local BAR environment — data directory, engine directory, and which `recoil_*` version is active. Validated on startup.
- **Proxy Install Status**: Per-engine-version record of whether the HighBarV2 AI files are present and match the bundled version, whether `devmode.txt` exists, and whether `simpleAiList = false` in the Chobby config.
- **Bundled Proxy**: The copy of `libSkirmishAI.so` + `AIInfo.lua` + `AIOptions.lua` committed in this repo under a known path, identified by a version string. Refreshed by a maintainer script from a sibling HighBarV2 build; consumed by the proxy installer.
- **Session Config**: Map, game mode (Skirmish / FFA / Team), engine speed, "launch graphical viewer" toggle, and a list of teams. Each team has one or more seats; each seat is either an AI seat (AI name + options) or a human seat (player name), with an optional handicap. Spectator seats are tracked separately. Populated by the setup screen and consumed by the launcher.
- **Running Session**: The live session — engine process handle, connected proxy, gamestate stream, any connected scripting clients, and status. At most one at a time in v1.
- **Unit Entry**: Per-unit encyclopedia record built from `BarData` — identity, faction, tier, role, cost, health, build time, weapons, movement, build options, and the viz glyph used to render it.
- **Style Preset**: A named bundle of `VizConfig` / `UnitGlyphStyle` values (reusing feature 033's existing format).
- **Scripting Client**: A connected gRPC consumer — receives gamestate frames, sends commands. Identified for the session's lifetime; cleaned up on disconnect.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user who has just installed BAR and the hub can complete first-run setup and launch their first session in under 5 minutes, start to finish, without opening a terminal or the bar-info cheat-sheet.
- **SC-002**: The time from clicking Launch to the first viewer frame is under 30 seconds on a representative dev machine.
- **SC-003**: 100% of units in the current BarData package appear in the encyclopedia, with glyphs that byte-match what the live viewer draws for the same unit type.
- **SC-004**: A scripting client that connects after a session starts sees its first gamestate frame within 2 seconds of connection.
- **SC-005**: With 5 scripting clients connected simultaneously and a session running at 1.0x speed, every client receives every gamestate frame (no dropped frames attributable to the hub).
- **SC-006**: Disconnecting a scripting client, crashing it, or saturating its receive buffer does not affect the embedded viewer's frame rate or other clients' streams.
- **SC-007**: After the hub installs the proxy, launching Chobby (outside the hub) shows "HighBarV2" in the skirmish AI dropdown without any further manual step.
- **SC-008**: On a machine that was hand-configured per the bar-info cheat-sheet before the hub existed, running the hub's first-run flow produces the same end state (same files present, same config values) without duplicating or corrupting existing content.

## Assumptions

- The hub targets Linux desktop first, consistent with the rest of the project's platform. Cross-platform is out of scope for v1.
- The hub is the user's launch vehicle; it does not replace Chobby. Users who prefer Chobby can still use it, and the hub's proxy install is what makes the AI available there.
- "Live original game viewer" refers to launching the BAR graphical `spring` binary (windowed) in parallel with a session, not a screen-share or overlay. The hub does not render BAR's native UI itself.
- Only one session is active at a time in the hub. Multi-session is out of scope for v1.
- The gRPC endpoint is localhost-only; authentication and remote access are out of scope for v1. Anything connecting to it is trusted.
- gRPC contract scope for v1 mirrors what the in-repo trainer bot consumes today: gamestate snapshot stream + command send + unit-definition lookup. Full parity with every `BarClient` callback is a follow-up.
- The BAR data directory layout matches `docs/bar-info.md` (`~/.local/state/Beyond All Reason/` by default, with `engine/`, `maps/`, `packages/`, `pool/`, `LuaMenu/Config/IGL_data.lua`).
- The hub owns the engine process lifecycle for sessions it launches. Sessions launched outside the hub (e.g., from Chobby) are out of scope for v1 — the hub neither observes nor manages them, though the installed proxy still lets those sessions run HighBarV2 for human play.
- The encyclopedia is read-only in v1; editing or extending BarData from within the hub is out of scope.
- The hub reuses existing in-repo components: `FSBar.Client` (BarClient, GameState, MapCacheFile), `FSBar.Viz` (GameViz, SceneBuilder, UnitGlyph, configurator), `FSBar.SyntheticData`, and the protobuf types in `FSBar.Proto`. It does not fork or reimplement them.
- Viewer windowing uses the existing `SkiaViewer` / Silk.NET path already validated elsewhere in the repo; the hub hosts it rather than building a parallel renderer.
