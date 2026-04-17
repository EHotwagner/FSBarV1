# Phase 0 Research — Hub Viewer Fixes (038)

**Scope**: Resolve every NEEDS CLARIFICATION and pin down the concrete
integration points for the four user stories (glyph parity, start-paused,
graphical engine option, direction triangle).

---

## R1. Why Viewer-tab glyphs don't match the encyclopedia

**Decision**: The fix is to populate `GameSnapshot.DisplayUnits` in
`SceneBuilder.gameStateToSnapshotWith` using the live `BarClient.UnitDefCache`
plus the same `resolveDefPropsFromBarData` classification helper that
`GameViz.buildDisplayUnits` already uses. `resolveDisplayUnits` falls
through to `legacyToUnitDisplay` only when `DisplayUnits` is empty;
we'll make it non-empty on the live path.

**Rationale**:
- `src/FSBar.Viz/SceneBuilder.fs:135-157` defines `legacyToUnitDisplay`,
  which hard-codes `Faction = Neutral`, `Tier = T1`, `Shape = Bot`,
  `LabelCode = "??"`, `FootprintWidthElmo = 32`, `HeadingRadians = 0.0`.
  That is the wrong glyph for every real unit.
- `SceneBuilder.buildUnits` (line 165) picks `UnitGlyph.buildUnitsGlyph`
  when `UseGlyphRenderer = true` (the default), but first calls
  `resolveDisplayUnits` (line 159), which **only uses `snap.DisplayUnits`
  when it's non-empty**. Otherwise it falls back to the placeholder
  constructor above.
- `SceneBuilder.gameStateToSnapshotWith` (line 440-475) is the live-path
  constructor used by `buildSceneHeadlessSized` /
  `buildSceneHeadlessView`. It sets `DisplayUnits = Map.empty` — so the
  placeholder path is always hit in the Hub.
- `GameViz.buildDisplayUnits` (line 216-223) proves the correct
  construction exists: `toUnitDisplay u props` where `props` comes from
  `resolveDefPropsFromBarData (UnitDefCache.lookupName defId)`. GameViz
  populates this into its own `buildSnapshot`; SceneBuilder does not.
- `EncyclopediaTab.fs:290-317` builds a `UnitDisplay` directly from a
  `BarData` encyclopedia entry and calls `UnitGlyph.buildUnit`. Same
  renderer, correct props — that's why it looks right.

**Alternatives considered**:
1. Move `buildDisplayUnits` from `GameViz` into `SceneBuilder` and have
   both callers use it — rejected because `GameViz` holds its own
   `defPropsCache` mutable state (populated as units are sighted),
   whereas the Hub's `SessionManager.RunningSession` exposes
   `BarClient.UnitDefCache` directly. Passing the cache into
   `SceneBuilder` is the cleanest cut.
2. Keep the placeholder in SceneBuilder and upgrade it to look up
   `BarData` by `sprintf "def%d"` — rejected because def-id-to-name
   requires engine callback data that the cache already holds.

**Implementation handle**: A new `SceneBuilder.populateDisplayUnits:
GameState -> UnitDefCache -> Map<int, UnitDisplay>` helper (publicly
exposed in `.fsi`). `gameStateToSnapshotWith` takes an optional
`UnitDefCache` argument; when provided, calls the helper; when `None`,
leaves `DisplayUnits = Map.empty` and the legacy fallback still works
for callers without a cache (tests, preview sessions).

**Encyclopedia refactor (FR-002)**: Extract
`EncyclopediaTab.EncyclopediaEntry -> UnitDisplay` into
`FSBar.Viz.UnitDisplayAdapter.ofEncyclopediaEntry`. The new adapter
module also exposes `ofTrackedUnit` / `ofTrackedEnemy` so the live
path and the encyclopedia both flow through **one** construction
function — making FR-002 ("single shared code path") compiler-enforced.

---

## R2. Start-paused mechanism

**Decision**: Send the chat command `/pause` via
`Commands.SendTextMessageCommand` on the first live frame after the
session transitions to `Running`, iff
`HubSettings.StartPausedDefault = true`. Re-send `/pause` on the
Viewer-tab button click to toggle.

**Rationale**:
- BAR's engine pause is a global toggle driven by the `/pause` chat
  command. Every client (AI or human) can issue it from team 0 with
  authority.
- `proto/highbar/commands.proto:112-115` defines
  `SendTextMessageCommand { text, zone }`; it flows through the
  existing `BarClient.SendCommands` pipe that already works on both
  headless and graphical engines (no protocol change).
- The alternative proto-level `PauseTeamCommand` (commands.proto:137) is
  **per-AI-team** and only suspends the AI's decisions; the game clock
  still advances. That's not what the spec wants.
- `SessionManager.SetPaused(bool)` already exists as a stub at
  `SessionManager.fs:200` — it publishes `HubEvents.SessionPaused` but
  does not wire to the engine. We promote it from stub to real by
  forwarding the chat command through `RunningSession.BarClient`.
- For "apply on first frame" (FR-003 — pause _before_ any game time
  elapses): subscribe `SessionManager` to `BarClient.Frames`; when the
  state becomes `Running` and `startPausedThisLaunch = true`, queue
  the chat command and flip an internal "pause-armed" flag. The chat
  command is delivered on the first `SendCommands` trip to the engine.
  SC-002 tolerates ≤ 10 s of wall-time before the user unpauses, and
  in practice the engine emits frames (paused) within ~2 s of socket
  accept — so the first-frame latency is well under budget.

**Alternatives considered**:
1. Modify the BAR start script (`ScriptGenerator.generate`) to embed
   `StartPaused = 1` in `[GAME]`. Rejected: the engine's start-script
   key for "start paused" is inconsistently honoured across Recoil
   versions and we'd couple the hub to a specific engine build. A
   single runtime `/pause` is version-independent.
2. Send `/pause` from the bundled HighBarV2 proxy (Lua side).
   Rejected: the HighBar proxy is a third-party artefact we vendor
   as a nupkg; adding Lua to it expands the bundled-proxy scope.
3. Add a new proto message `SetGamePauseCommand`. Rejected: out of
   scope for this feature; chat-command route is sufficient and does
   not alter wire contracts.

**Pause/unpause control on Viewer tab**: Small button in the Viewer-tab
top-right corner. Click handler calls
`SessionManager.TogglePause()` (new member, see contracts). The
button's visual state reads `sessionManager.IsPaused`.

**Tracking engine pause state**: `/pause` is a toggle; we need to know
"is the engine currently paused" to render the button correctly. The
hub already surfaces `ActiveSession.paused` in
`scripting.proto:135`. Two options:
- (A) Trust a hub-internal counter: hub issues `/pause`, flips bool,
  renders accordingly. Simple; drifts from reality if the user types
  `/pause` in the BAR graphical client.
- (B) Read engine pause state from the game-state stream. Requires a
  callback we don't currently have.

**Pick (A)** for this feature. FR-004b only requires that the Hub
button toggles the engine; it does not require the Hub to reflect
out-of-band pauses from BAR's native UI. Drift is a known edge case
with a trivial recovery ("click it twice"). Document the limitation
in the quickstart.

---

## R3. Graphical engine option

**Decision**: Flip `EngineConfig.Mode` in `LobbyConfig.toEngineConfig`
based on `HubSettings.LaunchGraphicalViewerDefault`. Use
`ActiveEngine.GraphicalBin` path (already discovered) for
`AppImagePath`; keep `EngineBin` pointing at `spring-headless` so the
headless code path remains untouched when the default is active.

**Rationale**:
- **Engine launcher already differentiates** — `BarClient.fs:231-232`
  already branches `Headless | Graphical` and
  `EngineLauncher.launchGraphical` (`EngineLauncher.fs:187`) exists
  and packages to `config.AppImagePath` (which for a recoil install is
  just `<engineDir>/spring`). No new launcher code.
- **`EngineDiscovery` already finds the graphical binary** — per
  `EngineDiscovery.fs:196`, every `recoil_*` directory is probed for
  both `spring-headless` and `spring`. `ActiveEngine.GraphicalBin:
  string option` is populated at Hub startup.
- **The only missing link is the choice** — `LobbyConfig.fs:229-230`
  hard-codes `Mode = EngineMode.Headless`. Change to:
  ```fsharp
  let mode = if settings.LaunchGraphicalViewerDefault then Graphical else Headless
  let engineBin, appImagePath =
      match mode with
      | Headless -> headlessBin, ""
      | Graphical -> headlessBin, graphicalBin
  ```
- **Windowed mode (FR-006)**: `EngineLauncher.launchGraphical` honours
  the engine's `Fullscreen` setting in `springsettings.cfg`, and
  CLAUDE.md documents that FSBar already writes `Fullscreen=0` per
  session. Verify the hub session directory follows the same
  convention (it does — `EngineLauncher.launchEngine` writes it
  unconditionally at `EngineLauncher.fs:~120`, to be re-confirmed in
  implementation).
- **Parallel Hub render (FR-006a)**: No change. The hub's internal
  `BarClient` connection works against the graphical engine exactly
  the same as headless, because HighBarV2 is loaded as an AI library
  regardless of engine binary. The `ViewerTab` keeps reading
  `RunningSession.BarClient.GameState`; there is no rate-limit or
  focus-based suspension to add or remove.

**Alternatives considered**:
1. A per-launch picker on the Setup tab instead of a persisted setting.
   Rejected by clarification: the choice persists across Hub restarts.
2. Prompt the user to confirm graphical launch. Rejected — adds a
   click without value; if the graphical engine is missing we already
   surface a clear error via `ActiveEngine.HasGraphicalBin = false`
   (see R5 for error path).

**Failure mode (FR-008)**: When
`HubSettings.LaunchGraphicalViewerDefault = true` but
`ActiveEngine.GraphicalBin = None`, `SessionManager.Launch` returns
`Error "graphical engine not installed at <engineDir>"`. The Setup-tab
status area renders the error and the match does NOT silently fall
back to headless.

---

## R4. Direction triangle

**Decision**: Replace the `Scene.ellipse` facing pip in
`UnitGlyph.fs:412-425` with a small triangle path rotated by the unit's
heading. Encyclopedia and Style-tab preview surfaces pass a fixed
"north-pointing" heading instead of `0.0f`; the renderer remains
heading-agnostic. Static previews without heading data suppress the
triangle for shapes classified as non-rotating structures.

**Rationale**:
- `UnitGlyph.fs:350-351` already derives `heading` from
  `UnitDisplay.HeadingRadians` with a NaN guard → 0.0. The triangle
  will inherit the same source, so the Viewer tab picks up live
  heading as soon as `DisplayUnits` is populated (see R1).
- Spring's heading convention: `0.0 rad` = north (+Z in Spring). The
  existing code comments the shape as "canonical east-facing" and
  uses `cos heading` / `sin heading` for offsets — in Spring's
  heading convention, heading 0 rad puts `sin(0) = 0` on x and
  `cos(0) = 1` on z, which is "up" on the screen. Confirm in the
  implementation by drawing a unit at heading 0 and verifying the
  triangle points up. If the renderer's orientation disagrees (e.g.
  heading 0 produces a right-pointing triangle), apply a constant
  `-π/2` offset in one place — the fix is local and the semantic
  stays "apex points in facing direction".
- **Triangle geometry**: apex at `(r + pipR * 2.0f, 0)` in the
  shape's local frame (i.e. at the same offset as the current pip
  centre), base perpendicular to the heading, width ≈ `pipR * 2`,
  height ≈ `pipR * 2.5`. All scale with the existing
  `UnitGlyphStyle.FacingPipRadius` so the ConfigDescriptor-based
  style configurator still works; the descriptor's semantics change
  from "pip radius" to "pip half-size" but the numeric range stays.
- **Static previews** (FR-010a): `EncyclopediaTab.fs:304` passes
  `HeadingRadians = 0.0f` today. If the verification above confirms
  heading 0 = north, no change needed on the encyclopedia — the
  triangle will automatically point up. Otherwise we pass the
  computed "north" value. Same for `ConfigPanel` previews and the
  Style-tab preview.
- **Non-rotating shapes** (FR-010): When
  `UnitDisplay.Shape = MovementShape.Building` and the live path has
  `HeadingRadians = 0.0f`, suppress the triangle. Structures in BAR
  don't turn, so a persistent "north" triangle is misleading noise.
  Implemented as an early-return in the pip construction block.

**Alternatives considered**:
1. A wedge/arrow with two tails instead of a plain triangle. Rejected —
   adds visual weight without improving readability at the small sizes
   (~4-8 px) where the pip lives.
2. Render the triangle inside the shape outline rather than offset
   outside. Rejected — occludes the label code in dense scenes.

**Tests**:
- Unit test: `UnitGlyph.buildUnit` with heading values 0, π/2, π,
  3π/2 produces triangle paths whose apex is at the expected
  `(cos, sin)` offset around `(mx, mz)`. Snapshot the generated
  `PathCommand` sequence.
- Visual baseline: Hub-app screenshot test at
  `tests/FSBar.Hub.Tests/Baselines/ViewerGlyph.*.png` — compare
  fresh-render hashes for a canonical fixture.

---

## R5. HubSettings schema evolution

**Decision**: Add one field — `StartPausedDefault: bool` — to
`HubSettings`. The existing `LaunchGraphicalViewerDefault` (already
present with default `false`) covers the engine-mode setting directly.
Both are read by `SetupTab` for UI state and by `LobbyConfig` at
launch; both are written by `SetupTab` on checkbox toggle.

**Rationale**:
- `HubSettings.fs:17-23` has only five fields today; adding one boolean
  with a hand-rolled write/read pair (one `WriteBoolean` + one
  `parseBool` call, mirroring the existing `LaunchGraphicalViewerDefault`
  handling at lines 59 and 103) is a two-line change on each side.
- `settingsPath()` already resolves
  `$XDG_CONFIG_HOME/fsbar-hub/settings.json`; atomic save already uses
  temp-file + rename. No infra changes.
- Default value: `true` (spec clarification — "defaulting to 'start
  paused = on'"). The factory default for the engine-mode option
  stays `false` (headless) per spec clarification.
- **SchemaVersion**: stays `1`. The new field is additive and missing
  values fall back to defaults (`parseBool root "startPausedDefault"
  defaults.StartPausedDefault`), so old settings files load
  compatibly.

**Alternatives considered**:
1. Introduce a `ViewerTabPrefs` nested record. Rejected — one boolean
   does not justify a nested shape, and nested STJ serialization of F#
   records still hits the "no parameterless constructor" warts noted in
   `HubSettings.fs:39-44`.
2. Bump `SchemaVersion` to `2`. Rejected — no migration logic needed;
   version bumps should signal destructive schema changes.

---

## R6. SessionManager pause wiring

**Decision**: Promote the existing stub `SetPaused`/`SetSpeed` pair
into real engine-wired calls. Add two members:
- `IsPaused: bool` (thread-safe read of an `int` flag set
  atomically alongside `/pause` sends)
- `TogglePause: unit -> unit` (flips and sends — convenience for the
  Viewer-tab button)
Internally, hold `startPausedForNextLaunch: bool` on the manager; on
`StateChanged Running`, if the flag is true, immediately issue one
`/pause` via `BarClient.SendCommands` and set `IsPaused <- true`.

**Rationale**:
- `SessionManager.fsi:73-75` already declares `SetPaused: bool -> unit`.
  The addition is `IsPaused: bool` and `TogglePause: unit -> unit` —
  narrow surface change, fully backward compatible.
- The hub-scripting proto (`scripting.proto:135`) already surfaces
  `ActiveSession.paused`; once `IsPaused` reflects reality, the
  existing `GetSessionStatus` response populates it without further
  changes.
- All pause state transitions emit the existing
  `HubEvents.SessionPaused` so the status bar + diagnostics trace
  continue to work.

**Alternatives considered**:
1. Skip `IsPaused`, let Viewer-tab track its own local bool. Rejected
   — duplicates state across two UI surfaces (Viewer tab + status bar)
   and the scripting RPC.
2. Plumb pause through a new proto message end-to-end. Rejected — out
   of scope; `/pause` chat command is the pragmatic shortcut and has
   no wire-contract cost.

---

## R7. NEEDS CLARIFICATION roll-up

All five spec-session clarifications are captured in the spec. No
remaining NEEDS CLARIFICATION items in this plan.

| Topic | Resolution source |
|-------|-------------------|
| Pause/unpause control location | Spec §Clarifications, FR-004b |
| Start-paused persistence | Spec §Clarifications, FR-004a |
| Engine-mode persistence + factory default | Spec §Clarifications, FR-005 |
| Parallel render vs focus-suspend | Spec §Clarifications, FR-006a |
| Triangle on static previews | Spec §Clarifications, FR-010a |
| Pause mechanism (proto vs chat vs script) | R2 above — chat `/pause` |
| DisplayUnits population site | R1 above — SceneBuilder, with UnitDefCache |
| Heading convention for triangle apex | R4 above — verify at impl time |
