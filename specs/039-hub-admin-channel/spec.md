# Feature Specification: Hub admin/host channel

**Feature Branch**: `039-hub-admin-channel`
**Created**: 2026-04-17
**Status**: Draft
**Input**: User description: "implement the admin/host channel on the hub side and expose the capabilities."

## Clarifications

### Session 2026-04-17

- Q: Where should the new admin controls (engine speed, force-end, admin-message input) live on the Hub UI? → A: All admin controls grouped into one toolbar on the Viewer tab, near the existing pause button.
- Q: How should the engine-speed control be represented in the admin toolbar? → A: Preset buttons (0.5x / 1x / 2x / 5x / 10x) plus a small numeric input for arbitrary custom values.
- Q: How should the Hub surface admin-channel unavailability to the operator? → A: Inline status line in the Viewer-tab admin toolbar, adjacent to the disabled controls, showing the reason.
- Q: What speaker attribution should admin messages carry in the engine's in-game chat log? → A: Whatever the engine natively attributes autohost/admin-channel messages as (no Hub-supplied name).
- Q: Should the Hub persist a user-chosen default engine speed across Hub restarts (like `StartPausedDefault`)? → A: No — matches always launch at engine-native 1.0x; the user adjusts live after launch via the admin toolbar.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reliable game pause and resume (Priority: P1)

When a user clicks the pause control on the Hub's Viewer tab, the active
match's game simulation halts immediately and stays halted until the
user clicks resume. Today the pause button freezes only the rendered
view — the underlying game continues to advance in the background — so
"pause" doesn't actually pause the match. This user story delivers a
real pause: the in-game clock stops, units stop moving, resources stop
accumulating, and the state the user sees on unpause is exactly the
state at the moment of pause.

**Why this priority**: Closes the most visible gap left by feature 038.
The pause button is the single most-used session-control action on the
Viewer tab; making it actually pause the game is the highest-value
delta this feature can ship.

**Independent Test**: Launch a match from the Setup tab. On the Viewer
tab, note the current game clock, click pause, wait at least 15 seconds
of wall time, and confirm the clock reads the same value. Click resume;
confirm the clock advances and units move again. Confirm the "start
paused" checkbox on Setup now produces a real simulation pause at match
start, not just a frozen view.

**Acceptance Scenarios**:

1. **Given** a match is running on the Viewer tab, **When** the user
   clicks the pause control, **Then** the game clock stops advancing
   within one frame of the click, units mid-motion finish their current
   tick and halt, and resources stop accumulating.
2. **Given** the match is paused via the Hub's control, **When** the
   user clicks the resume control, **Then** the game clock resumes
   advancing from where it stopped and all units continue their
   previous orders.
3. **Given** the Setup-tab "Start paused" checkbox is enabled and a
   match is launched, **When** the engine reaches its first simulation
   frame, **Then** the game clock reads `0` (or the engine's lobby-zero
   equivalent) and does not advance until the user clicks resume.
4. **Given** a paused match, **When** the user closes the Hub or the
   session ends naturally, **Then** pause state does not leak to the
   next launched match — each new match honors the checkbox's state at
   launch time.

---

### User Story 2 - Real-time engine speed control (Priority: P2)

When a user wants to fast-forward through a slow build-up or slow down
a chaotic fight for analysis, they can adjust the match's engine speed
from the Hub's Viewer tab without touching the engine binary or
restarting the session. Today the engine speed is fixed at the value
the lobby was launched with.

**Why this priority**: Unlocks a workflow the spec-level pause doesn't
cover — users reviewing trainer runs want to accelerate through
low-activity phases and slow down around decision points. The
underlying mechanism (admin channel) is the same as pause; once pause
ships, speed control is a small additional surface.

**Independent Test**: Launch a match, confirm the base speed is `1.0x`.
Use the Hub's speed control to set `5.0x`; confirm the game clock
advances roughly five times faster than wall time for at least 30
seconds. Set back to `1.0x`; confirm normal cadence resumes.

**Acceptance Scenarios**:

1. **Given** a match running at `1.0x`, **When** the user requests
   `5.0x` via the Hub, **Then** game-clock advancement over a 10-second
   wall-time window is roughly 50 seconds of game time (±10% to
   accommodate engine limits).
2. **Given** a match running at any speed, **When** the user requests a
   value outside the engine's supported range, **Then** the Hub rejects
   the request with a clear operator-visible error and the current
   speed is preserved.
3. **Given** a match is paused, **When** the user requests a speed
   change, **Then** the requested speed is stored but the match stays
   paused; on resume the match adopts the new speed.

---

### User Story 3 - Force-end a running match (Priority: P2)

When a user wants to end a match cleanly before the game's natural
ending condition (commander death, time limit), they can click a
"force-end" control on the Hub and the session terminates without
requiring them to kill the engine process externally. The Hub returns
to its idle state ready for the next launch.

**Why this priority**: Removes an operator-visible pain point
("I have to `pkill spring-headless` to end a frozen-looking match").
Ships the admin-channel's most-used match-lifecycle command. Separable
from pause/speed — ships independently.

**Independent Test**: Launch a match, let it run for at least 30
seconds. Click force-end on the Hub. Confirm the session transitions to
idle within 5 seconds, the engine process exits, the Hub's status bar
reads "session ended", and a new match can be launched without
restarting the Hub.

**Acceptance Scenarios**:

1. **Given** a running match, **When** the user clicks force-end,
   **Then** the engine terminates cleanly, the Hub returns to idle, and
   the final game state before termination is retained in diagnostic
   logs.
2. **Given** a match is paused, **When** the user clicks force-end,
   **Then** the same termination sequence runs and no "still paused on
   relaunch" state is leaked.
3. **Given** no match is running, **When** the force-end control is
   visible at all, **Then** it is disabled and clicks are no-ops.

---

### User Story 4 - Broadcast an admin message into game chat (Priority: P3)

When a coach or automated trainer wants to annotate a running match
(e.g., "start phase 2", "expected eco swap now"), they can send a text
message that appears in the game's chat log from the Hub, credited to
an admin/spectator speaker rather than as if it came from an AI team.
Today, text messages sent via the existing AI chat path are credited
to an AI team which is noisy and may be filtered by some audiences.

**Why this priority**: Nice-to-have for coaching workflows; the
mechanism comes for free from the admin channel so shipping it costs
little extra. Cleaner audit trail for trainer runs that emit
phase-transition markers.

**Independent Test**: Launch a match, open the in-game chat log (via
the graphical client, since headless has no visible chat). Use the
Hub's admin-message control to send "phase-start". Confirm the message
appears in the chat log with an admin-style credit (not attributed to
an AI team).

**Acceptance Scenarios**:

1. **Given** a running match, **When** the Hub sends an admin message
   "test", **Then** the message appears in the engine's chat log within
   one simulation tick, visible to all teams, credited to a
   host/admin/spectator speaker.
2. **Given** an admin message is sent while paused, **When** the match
   resumes, **Then** the message is already in the chat history at the
   pause point, not queued for post-resume delivery.

---

### Edge Cases

- What happens if the admin channel fails to attach at session launch
  (port conflict, engine build doesn't advertise the port, firewall)?
  The Hub must surface a clear operator-visible warning, continue
  running the session without admin capabilities, disable the admin
  controls on the Viewer tab, and never silently pretend the command
  worked.
- What happens if the user clicks pause while the admin channel is
  unavailable? The pause control must visibly indicate "unavailable"
  (grayed out or similar) rather than appearing to succeed with no
  effect.
- What happens if the engine disconnects the admin channel mid-session
  (crash, network blip)? The Hub must note the loss in diagnostics and
  disable admin controls; the session itself should remain observable
  (read-only) until it ends naturally.
- What happens on the graphical engine where pause is also reachable
  via the in-game keybind? The Hub's displayed pause state may
  temporarily drift from the engine's actual state; the next admin
  command (pause or resume) resyncs.
- What happens when two rapid clicks on pause/resume arrive before the
  engine acknowledges the first? The Hub de-duplicates redundant
  requests so the engine's pause state ends up matching the user's
  last click.
- What happens when the Hub is driven by an external scripting client
  (via the gRPC scripting service)? Admin capabilities exposed on the
  Hub UI MUST also be invokable programmatically through the scripting
  surface with equivalent semantics.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Hub MUST establish an authoritative admin control
  channel with the active match's engine at session launch, separate
  from the existing AI-client game-state stream.
- **FR-002**: The Hub MUST pause the running match's simulation on user
  request, such that the game clock, unit motion, and resource
  accumulation all halt until the user requests resume.
- **FR-003**: The Hub MUST resume a paused match on user request, from
  exactly the state it was paused at.
- **FR-004**: The "Start paused" Setup-tab checkbox MUST result in a
  real simulation pause at the first engine frame — not a view-only
  freeze as in feature 038.
- **FR-005**: The Hub MUST allow the user to set the match's engine
  speed to any value in the engine's supported range, effective within
  one simulation frame of the request, preserving pause state if the
  match is currently paused. The Viewer-tab toolbar MUST expose
  one-click preset buttons at 0.5x, 1x, 2x, 5x, and 10x plus a
  numeric input for custom speeds within the engine's supported range.
- **FR-006**: The Hub MUST allow the user to force-end a running match,
  terminating the engine cleanly and returning the session state to
  idle within five seconds of the request.
- **FR-007**: The Hub MUST allow the user to send an admin message that
  appears in the match's in-game chat log via the admin channel, not
  the AI-client chat path. The Hub MUST NOT supply a speaker name; the
  message MUST carry whatever attribution the engine natively applies
  to autohost/admin-channel messages.
- **FR-008**: Admin capabilities (pause, resume, speed, force-end,
  admin message) MUST be exposed via the Hub's scripting service so
  external clients can drive them with the same effect as the Viewer-
  tab controls.
- **FR-009**: When the admin channel fails to attach or becomes
  unavailable mid-session, the Hub MUST surface a clear operator-
  visible warning and disable the corresponding controls rather than
  silently pretending commands succeeded. The warning MUST render as
  an inline status line inside the Viewer-tab admin toolbar,
  spatially adjacent to the disabled controls, and MUST include the
  reason (e.g., "Admin channel unavailable: port conflict").
- **FR-010**: The Hub's displayed pause and speed state MUST reflect
  the most recent admin command the Hub issued, and any re-sync (e.g.,
  after the user paused via an engine-native keybind on the graphical
  client) must converge on the next admin command without requiring a
  session restart.
- **FR-011**: Rapid repeat requests for the same admin command (e.g.,
  two pause clicks in quick succession) MUST collapse to a single
  consistent end state matching the user's last click.
- **FR-012**: Admin capabilities MUST be available identically for
  both the headless and graphical engine launch modes; there must be
  no user-facing divergence between the two.
- **FR-013**: The feature MUST NOT regress any existing Hub behavior:
  the Viewer tab's rendering, the Units-tab encyclopedia, the Setup
  tab lobby builder, and the scripting service's existing endpoints
  continue to function unchanged when the admin channel is not in use.

### Key Entities *(include if feature involves data)*

- **Admin Channel**: The authoritative control link the Hub holds to
  the active match's engine. Lives for the duration of one session;
  carries outbound admin commands (pause, resume, speed, force-end,
  admin message) and inbound admin acknowledgements/events. Not
  persisted across sessions.
- **Admin Command**: One operator intent issued via the admin channel.
  Has a kind (pause / resume / speed / force-end / message), an
  optional payload (speed value, message text), and an acknowledgement
  state (pending / accepted / rejected).
- **Admin Channel Status**: The Hub-visible state of the admin channel —
  `Attached`, `Unavailable(reason)`, `Lost(reason)`. The UI renders
  control enablement off this status; the scripting service exposes it
  to external clients.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With the user-facing pause control in the paused state,
  the in-game clock advances by zero seconds for at least ten seconds
  of wall-time unless the user clicks resume, on 100% of launched
  matches (both headless and graphical engine).
- **SC-002**: Resume after pause restores full simulation activity —
  unit motion and resource accumulation resume — within one second of
  the click.
- **SC-003**: Engine-speed changes take observable effect within one
  second of the request; the effective speed falls within ±10% of the
  requested multiplier over any ten-second sampling window.
- **SC-004**: Force-end terminates a running match and returns the
  Hub session state to idle within five seconds of the click, without
  requiring any external process intervention, on 100% of attempts.
- **SC-005**: Admin messages sent via the Hub appear in the engine's
  in-game chat log within one simulation tick of the request, on 100%
  of attempts.
- **SC-006**: When the admin channel cannot be attached at launch, the
  session continues in read-only mode, the Hub surfaces a diagnostic
  within ten seconds of launch, and the UI visibly disables admin
  controls — zero silent failures.
- **SC-007**: Every admin capability accessible through the Viewer
  tab is also accessible through the scripting service surface, with
  identical observable semantics (verified by running the same
  scenario through both surfaces and comparing outcomes).
- **SC-008**: No regression in any pre-feature-039 user-facing Hub
  behavior: Setup tab, Units tab, Style tab, and gRPC scripting
  endpoints that existed before this feature continue to pass their
  existing acceptance tests unchanged.

## Assumptions

- The target engine exposes an authoritative control interface
  distinct from the AI-client chat path — we assume the Spring/Recoil
  family's autohost-style admin channel is the mechanism. The feature
  name is deliberately generic so if the chosen engine build instead
  exposes admin control via a different transport, this spec still
  applies.
- The Hub owns exactly one active session at a time (unchanged from
  feature 035 assumption), so the admin channel is a singleton within
  the session's lifetime.
- The user driving the Hub is always privileged to administer the
  match they launched — we're a single-user desktop tool, not a lobby
  server hosting strangers.
- The engine's admin interface does not require authentication beyond
  launch-time key exchange; the Hub controls the engine process, so
  shared secrets are bootstrapped locally at launch.
- External scripting clients that connect to the Hub's scripting
  service are trusted (loopback-only by existing policy); admin
  capabilities exposed through the scripting surface are not a new
  attack surface relative to the existing AI-command surface.
- The feature inherits feature 038's pause-button location (Viewer
  tab, top-right corner) and reuses it for the real pause. All other
  admin controls (engine speed, force-end, admin-message input) live
  in a single admin toolbar on the Viewer tab adjacent to the existing
  pause button; no new tab or separate panel is introduced.
- Persisting admin-channel-specific user preferences (e.g., "auto-
  reconnect on channel loss", "default engine speed on launch") is
  out of scope for this feature; matches always launch at engine-
  native 1.0x. If added later it becomes an additive change to
  `HubSettings`.
