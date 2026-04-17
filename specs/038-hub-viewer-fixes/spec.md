# Feature Specification: Hub Viewer Fixes

**Feature Branch**: `038-hub-viewer-fixes`
**Created**: 2026-04-17
**Status**: Draft
**Input**: User description: "hubviewer does not display correct unit glyphs. hub matches should start paused. need an option to view live bar gameengine, not headless. dir pip should be a triangle with top pointing in dir."

## Clarifications

### Session 2026-04-17

- Q: How does the user unpause a headless match? → A: The Hub itself provides a pause/unpause control on the Viewer tab that works for both engine modes via its scripting connection; there is no reliance on an engine-owned keybind for the headless case.
- Q: Should "start paused" be always-on, or a user-togglable option on the Setup tab? → A: User-togglable checkbox on the Setup tab, defaulting to "start paused = on". The Hub remembers the last choice across restarts.
- Q: Does the engine-mode choice (headless vs graphical) persist across Hub restarts? → A: Yes, same persistence rule as "start paused". Factory default on fresh install is headless.
- Q: When the graphical engine is active, what should the Hub's Viewer tab do? → A: Render in parallel with the native BAR client, unchanged from headless behavior — no rate reduction, no focus-based suspension.
- Q: On Hub surfaces without live facing data (Units-tab encyclopedia, Style-tab preview), how should the direction triangle render? → A: Triangle always points up (north) on static preview surfaces; the live Viewer tab uses real heading.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Viewer-tab glyphs match the rest of the Hub (Priority: P1)

When a user launches a match from the Hub and watches it on the Viewer tab,
every unit on the field is rendered with the same shape, label, tier ring,
and faction color they would see for that unit on the Units-tab
encyclopedia or in a standalone GameViz window. Today, units rendered in
the Viewer tab do not match — shapes, labels, or colors are wrong or
placeholder-like, which makes it impossible to read the match at a glance.

**Why this priority**: Correctness bug in the primary "watch a running
match" surface. Any tooling, coaching, or trainer-run review that relies
on the Viewer tab gives misleading signals until this is fixed.

**Independent Test**: Start a match from the Hub Setup tab, let a handful
of units of each faction spawn, flip between the Viewer tab and the
Units-tab encyclopedia entry for the same unit type, and confirm the
rendered glyph (shape family, label text, tier ring, faction color) is
visually identical.

**Acceptance Scenarios**:

1. **Given** a running Hub match containing at least one commander, one
   builder, one combat unit, and one structure from each side, **When**
   the user is on the Viewer tab, **Then** each on-field unit is drawn
   with the same glyph the Units tab shows for that unit internal name.
2. **Given** the same running match, **When** the user switches the
   active team/side, **Then** the faction color shown on the Viewer tab
   matches the faction color shown on the Units tab for the same side.
3. **Given** a unit type that has a specific tier (T1/T2/T3) in the
   encyclopedia, **When** it appears on the Viewer tab, **Then** its
   tier ring matches the encyclopedia rendering.

---

### User Story 2 - Matches start paused (Priority: P2)

When a user launches a match from the Hub Setup tab, the engine starts in
a paused state so the user can settle on the Viewer tab, pick overlays,
and mentally prepare before any game time elapses. Today, matches begin
running immediately, so the first several seconds of each match are
"lost" while the user is still navigating the Hub.

**Why this priority**: Small but high-value UX fix. It costs the user
nothing when they don't want it (one keystroke to unpause) and saves the
first-impression portion of every match when they do.

**Independent Test**: Launch a match from the Hub Setup tab, open the
Viewer tab, and confirm the game clock does not advance until the user
explicitly unpauses. Unpause and confirm the match resumes normally.

**Acceptance Scenarios**:

1. **Given** the user clicks "Launch" on the Hub Setup tab, **When** the
   engine finishes starting and the Viewer tab first shows units,
   **Then** the game clock reads the same value for at least several
   seconds of wall time and no unit moves.
2. **Given** the match is in this initial paused state, **When** the
   user clicks the Hub's pause/unpause control on the Viewer tab,
   **Then** the game clock begins advancing and units begin acting.
3. **Given** the "start paused" Setup-tab checkbox is enabled and a
   match is already running (unpaused), **When** the user launches a
   second match, **Then** the second match also starts paused — each
   launch independently honors the current checkbox state.
4. **Given** the user unchecks "start paused" on the Setup tab and
   restarts the Hub, **When** the user launches the next match,
   **Then** it launches running (unpaused) because the preference was
   persisted across restart.

---

### User Story 3 - Option to launch the live graphical engine (Priority: P2)

When a user wants to watch a match in the actual Beyond All Reason client
(with full graphics, audio, camera controls, etc.) instead of the Hub's
Viewer tab, they can pick that option on the Hub Setup tab before
launching. The Hub still controls the match lifecycle and scripting
connection; only the rendering surface changes. Today, every match runs
under the headless engine, so the only way to see it is via the Viewer
tab.

**Why this priority**: Unblocks demos, debugging of visual issues that
originate in the engine itself (not the Hub), and any coaching workflow
that wants BAR's native HUD. It is an additive option — headless remains
the default.

**Independent Test**: On the Hub Setup tab, switch the engine mode to
"graphical/live", click Launch, and confirm a normal BAR game window
opens (windowed, not fullscreen) with the match in progress and still
observable through the Hub's scripting connection.

**Acceptance Scenarios**:

1. **Given** the user selects the graphical engine option and clicks
   Launch, **When** the engine starts, **Then** a windowed BAR client
   appears and the Hub reports the same session as active.
2. **Given** a graphical-engine match is running, **When** the Hub's
   Viewer tab is opened, **Then** the Hub still streams game state
   frames and renders the Viewer tab normally in parallel with the
   native client.
3. **Given** the user selects the graphical engine option, **When** the
   engine starts, **Then** the client opens in windowed mode (never
   fullscreen), matching existing project policy.
4. **Given** the user launches without changing anything, **When** the
   engine starts, **Then** the headless engine is still used — the
   default behavior is unchanged.

---

### User Story 4 - Direction indicator is a forward-pointing triangle (Priority: P3)

When a user looks at a unit glyph on any Hub surface that renders them
(Viewer tab, Units tab encyclopedia preview, Style tab preview), the
unit's facing direction is shown as a small triangle whose apex points in
the unit's facing direction. Today the direction indicator is a dot or
short line that does not convey direction as readably.

**Why this priority**: Purely visual improvement on top of the unit
glyph language. Low risk, nice-to-have, but genuinely improves readability
once the higher-priority Viewer glyph bug is fixed.

**Independent Test**: On any surface that renders a unit glyph, rotate a
unit through the cardinal and diagonal headings and confirm the triangle's
apex tracks the facing direction in each case.

**Acceptance Scenarios**:

1. **Given** a unit facing north, **When** its glyph is drawn, **Then**
   the direction triangle's apex points toward the top of the glyph.
2. **Given** a unit facing east/south/west/diagonals, **When** its glyph
   is drawn, **Then** the triangle's apex points in the corresponding
   direction of travel.
3. **Given** a stationary unit with no meaningful facing (e.g., a
   non-rotating structure), **When** its glyph is drawn, **Then** the
   direction triangle is either omitted or rendered in a neutral
   orientation that does not mislead — never in a random direction.

---

### Edge Cases

- What happens when the Viewer tab is opened before the first game frame
  arrives? Glyph correctness must hold as soon as units appear; no
  pre-frame placeholder glyph should leak into the Viewer tab.
- What happens if the user unpauses the initial-pause state before the
  Viewer tab has fully loaded? The match must resume cleanly and any
  subsequent Viewer frames must be rendered correctly.
- What happens if the graphical engine fails to launch (missing binary,
  display unavailable)? The Hub must surface a clear error rather than
  silently falling back to headless.
- What happens for units the Hub does not have an encyclopedia entry for
  (e.g., mod units, new content)? The Viewer must render a stable
  fallback glyph that visibly signals "unknown" rather than silently
  showing a wrong glyph.
- What facing does the direction triangle point when heading data is
  unavailable or degenerate (zero-length heading vector)? — On live
  Viewer, suppressed or held in its last-known orientation; on static
  previews, always "up" per FR-010a.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Hub Viewer tab MUST render each on-field unit with the
  exact same glyph (shape family, label, tier ring, faction color,
  size-to-role mapping) that the Units-tab encyclopedia shows for that
  unit.
- **FR-002**: Unit glyph rendering in the Hub MUST be driven by a single
  shared code path so that Viewer-tab, Units-tab, and Style-tab previews
  cannot diverge.
- **FR-003**: When the Setup-tab "start paused" option is enabled,
  launching a match MUST result in the engine entering a paused state
  before any game time elapses, and MUST stay paused until the user
  activates the Hub's pause/unpause control.
- **FR-004**: The "start paused" option MUST apply to every launched
  match independently while enabled, regardless of whether a previous
  match was running, paused, or ended. When disabled, matches launch
  running (unpaused) as they did before this feature.
- **FR-004a**: The Hub Setup tab MUST expose a user-togglable "start
  paused" checkbox, defaulted to ON, whose value MUST persist across
  Hub restarts.
- **FR-004b**: The Hub MUST expose a pause/unpause control on the Viewer
  tab that toggles pause state for the active match via its scripting
  connection, working identically for headless and graphical engine
  modes.
- **FR-005**: The Hub Setup tab MUST expose a user-visible option to
  choose between the headless engine and the live graphical engine for
  the next launched match. The selection MUST persist across Hub
  restarts; the factory default on a fresh install MUST be headless.
- **FR-006**: Selecting the graphical engine option MUST cause the next
  match to launch the standard Beyond All Reason client in windowed
  (never fullscreen) mode, while leaving the Hub's scripting /
  observability connection intact.
- **FR-006a**: The Hub Viewer tab MUST continue to render the active
  match at its normal frame rate when the graphical engine is in use,
  identical to headless-mode behavior — no rate reduction, no
  focus-based suspension.
- **FR-007**: The default engine mode MUST remain headless — users who
  launch without changing the option MUST see no change in behavior.
- **FR-008**: If the graphical engine cannot be started, the Hub MUST
  surface a clear, user-visible error and MUST NOT silently fall back
  to headless.
- **FR-009**: The unit facing indicator MUST be rendered as a triangle
  whose apex points in the unit's current facing direction on every
  Hub surface that draws unit glyphs.
- **FR-010**: For units without meaningful facing in a live match (e.g.,
  non-rotating structures, or when heading data is unavailable), the
  direction triangle MUST be suppressed or rendered in a fixed, neutral
  orientation rather than a random direction.
- **FR-010a**: On static preview surfaces (Units-tab encyclopedia,
  Style-tab preview) where no live heading exists, the direction
  triangle MUST be rendered pointing "up" (north) so preview glyphs
  remain visually equivalent to live glyphs of the same unit.
- **FR-011**: Units not present in the encyclopedia MUST render a stable,
  visually-distinct fallback glyph rather than an incorrect glyph drawn
  from the wrong unit entry.

### Key Entities

- **Unit Glyph**: The compact visual representation of a single unit on
  any Hub surface. Carries shape family, short label, tier ring, faction
  color, role-driven size, and a facing-direction triangle.
- **Match Session**: One running engine instance launched from the Hub.
  Has an engine mode (headless or graphical) chosen at launch, an
  initial paused state, and a scripting connection the Hub uses to
  observe state.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a running match containing at least one commander, one
  builder, one combat unit, and one structure from each side, 100% of
  rendered Viewer-tab glyphs visually match the Units-tab encyclopedia
  glyph for the same unit internal name.
- **SC-002**: With the "start paused" Setup-tab option enabled, after
  clicking Launch on the Hub the game clock advances by zero for at
  least the first 10 seconds of wall time unless the user explicitly
  unpauses via the Hub pause control.
- **SC-003**: A user can launch a match in the live graphical engine
  from the Hub Setup tab in 3 clicks or fewer starting from a fresh Hub
  launch.
- **SC-004**: On every Hub surface that draws unit glyphs, a user can
  correctly identify the cardinal facing of a unit (N/E/S/W) at a
  glance for 100% of units that have meaningful facing data.
- **SC-005**: No regressions in the existing headless launch path —
  every Hub match launched without changing the engine mode behaves
  exactly as it did before this feature.

## Assumptions

- The Hub already exposes a Units-tab encyclopedia rendering that is
  treated as the "correct" reference for unit glyphs; the Viewer tab is
  the surface that needs to be brought into alignment.
- The Beyond All Reason graphical client is available on the user's
  machine and is runnable in windowed mode via the existing engine
  discovery mechanism (same engine version, different binary).
- The Hub's scripting connection works against both the headless and
  graphical engines without protocol changes.
- "Start paused" can be achieved through the engine's existing pause
  mechanism; no new engine-side protocol work is required.
- Unit heading / facing data is already available in the game-state
  stream that feeds Hub renderers.
