# Feature Specification: Tactical Map Primitives

**Feature Branch**: `024-tactical-map-primitives`
**Created**: 2026-04-13
**Status**: Draft
**Input**: User description: "create specs for that, also add building plans, anti wall in checks...."

## Context

Feature 023 ran a 28-iteration session that produced a working macro bot (opening → production → upgrade → attack → commander-death win on NullAI). Along the way the bot's ad-hoc placement logic surfaced four categories of problem that the existing `FSBar.Client.MapGrid` / `MapQuery` primitives could not solve by themselves:

1. **Attack routing**: peewees issued straight-line `MoveCommand` toward the enemy commander reached the target on open maps but had no way to avoid cliffs, water, or other impassable terrain. Against a mobile enemy or a map with a ridge between the bases this would stall.
2. **Defensive placement**: the FR-016b defend interrupt fires when enemies enter `baseRadius` but the bot has no notion of **where** in the base they should be intercepted — it can't tell a chokepoint from open ground.
3. **Building layout**: the opening plan uses fixed `NearCommander(±200, 0)` / `NearBaseCentre(±200, 0)` offsets. Iter 005 hit a 40-elmo collision between a solar and a metal extractor that silently failed placement, and iter 016-018 saw the commander never start construction because the chosen site was off-map or out of reach. Each failure cost an iteration of diagnosis.
4. **Wall-in**: no pre-flight check that a proposed structure keeps the base reachable from inside. A future bot that builds more densely (walls, turrets, dragon's teeth) could easily trap its own commander, and the current stack has no way to detect that before issuing the BuildCommand.

Feature 024 adds these four primitives to the helper library so the next bot iteration — whether it's a 023 revision, a BARb/dev-tuned variant, or a totally new archetype — can reuse them as first-class building blocks.

## Clarifications

### Session 2026-04-13

- Q: Where do the tactical primitives live in the codebase? → A: Add to `FSBar.Client` next to `MapGrid` / `MapQuery` (compiled Tier 1 — new `.fs` / `.fsi` modules, surface-area baselines updated).
- Q: How are the primitives validated without a live engine? → A: Write an **SMF parser** that reads `.sd7` archives + native Spring Map Files directly from the BAR installation (`~/.local/state/Beyond All Reason/maps/`). Unit tests use synthetic in-memory `MapGrid` fixtures for edge cases (one-gap walls, multi-chokepoint topologies); integration tests parse the three installed BAR maps (Avalanche 3.4, Red Rock Desert v2, Comet Catcher Remake) on the fly. No `.fsmg` binary fixtures committed; no live engine dependency in tests.
- Q: Do friendly structures block pathing? → A: **Yes** — `findPath` takes an explicit `ownStructures` input (positions + footprints) and treats those cells as impassable in the same way cliffs/water are. This makes path cost estimates accurate against our own base and lets `wouldWallIn` share the exact same passability model as `findPath`. The caller is responsible for passing a current snapshot; cache invalidation policy is caller-owned (version counter or re-issue per build event).
- Q: How is a plan slot's clearance radius interpreted? → A: **Additive margin over the structure's footprint edge** — `clearance=M` means "at least M elmos of empty space between this structure's outer edge and any other structure's outer edge". Footprint-independent (same `M` works for small mex and large factories); composes with the anti wall-in check (the margin is where commander/workers walk through, so it must be at least builder-width + safety).
- Q: How deeply does US5 integrate the primitives into the macro bot? → A: **Deep integration in a single pass** — opening-build logic, attack routing, AND defend-at-chokepoint behaviour are all replaced with primitive calls in one commit. The post-024 macro bot is expected to *use* pathing and chokepoints at runtime, not just log them. Iter budget for US5 is the usual 020/023 discipline; if the deep refactor surfaces multiple regressions in one run, subsequent iterations MUST each fix one issue at a time per the diagnose-one-fix rule from 023 PLAYBOOK §2c.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Slope-aware pathing for cross-map movement (Priority: P1)

A combat unit is issued a command to reach a target position across a map that has impassable terrain between start and goal (cliffs, water, unit-type mismatch). The unit follows a path computed from the map's slope + passability data that routes around the impassable terrain while minimising total travel cost.

**Why this priority**: Without this, any macro bot's attack phase is limited to maps where the enemy is in a straight line. 023 iter 026 won on Avalanche only because peewees can climb; on a map with a central ridge the rush bot's proven pattern would also fail. Pathing is the enabler for every competitive-rung iteration past NullAI.

**Independent Test**: Given a saved `MapGrid` from any BAR map and a known impassable obstacle between two positions, a call to `findPath` returns a sequence of waypoints such that every waypoint is `Passable` for the specified move type and consecutive waypoints share a neighbour relationship. When issued to a bot combat unit, the unit reaches within 32 elmos of the goal without getting stuck.

**Acceptance Scenarios**:

1. **Given** a map with a cliff between (500, 500) and (3500, 3500) passable for `Kbot` only via a detour at (2000, 2000), **When** the bot asks for a path from (500, 500) to (3500, 3500) for `MoveType.Kbot`, **Then** the returned waypoint list starts near (500, 500), passes through the (2000, 2000) region, and ends near (3500, 3500), and every waypoint is Kbot-passable.
2. **Given** the same map with no passable connection for `MoveType.Tank`, **When** the bot asks for a `Tank` path, **Then** the function returns "no path" (an explicit no-result value — not a silent empty list that could be confused with "start==goal").
3. **Given** two positions on flat terrain 500 elmos apart, **When** the bot asks for a path, **Then** the returned path is approximately straight (waypoints lie within a small distance of the straight line) and the returned estimated cost matches the straight-line distance within a small tolerance.
4. **Given** a very long path (e.g. corner to corner of a 2048×2048 elmo map), **When** the pathing function is invoked during a per-frame tactics callback, **Then** it returns within a time budget that lets the frame callback still meet its own deadline (the bot should remain responsive).

---

### User Story 2 — Chokepoint detection for defensive placement (Priority: P1)

The macro bot's Defending interrupt fires when an enemy enters base radius. Instead of meeting each intruder wherever they cross the radius line, the bot knows in advance that its base has (e.g.) two narrow approach corridors and can pre-position defenders at those corridors. Once chokepoint detection is live, the bot can also place static defences (turrets) at the chokepoint head.

**Why this priority**: The 023 macro bot's BARb/dev probe showed 60 oscillating defend/clear transitions in a single match because BARb raiders were skating across the `baseRadius` boundary. A chokepoint-aware defence would intercept the raiders at the single approach corridor instead of chasing each one across open ground. Without chokepoints, the defend interrupt is noisy and easily exploited.

**Independent Test**: Given a saved `MapGrid` and a "base centre" coordinate, a call to `findChokepoints` returns a list of chokepoint descriptors (position + width + direction) that, when visualised against the map, match human-visible narrow corridors leading into the base radius. The test is: can an operator sketch the expected chokepoints on a screenshot of the map and see them match the helper's output within a few hundred elmos?

**Acceptance Scenarios**:

1. **Given** a map with a single narrow canyon approach to our base, **When** the bot asks for chokepoints within 2500 elmos of base centre, **Then** exactly one chokepoint is returned whose position is inside the canyon and whose width (in elmos) matches the canyon's narrowest point within ±20%.
2. **Given** a map with two separate approach corridors (north and south), **When** the bot asks for chokepoints, **Then** both corridors are in the result list, each marked with a direction vector pointing outward from base centre.
3. **Given** an open map with no natural chokepoints within base-approach range, **When** the bot asks for chokepoints, **Then** the function returns an empty list (not a fabricated "least-wide point" — silence is the correct answer for open terrain).
4. **Given** the same `MapGrid` and base centre, **When** `findChokepoints` is called twice with the same inputs, **Then** the two results are identical (deterministic over static map data).

---

### User Story 3 — Declarative building plans with collision & reach checks (Priority: P1)

The macro bot currently reads a hardcoded list of offset pairs (`NearCommander`, `NearBaseCentre`) that were tuned by hand over six iterations. The replacement is a **building plan**: a named, reusable layout that describes how to place a set of structures relative to a base centre, subject to the constraints "each structure is on buildable terrain", "each structure fits within its own footprint plus a clearance margin", "no two structures overlap or clip", and "every structure is reachable by the builder that's supposed to construct it". The bot queries the plan for "give me the next site for armmex / armsolar / armlab" and the plan returns a position that already passes all four constraints — or reports that the plan is exhausted.

**Why this priority**: 023 wasted 4 iterations on layout bugs (iter 005 collision, iter 016-018 off-map / out-of-reach). A declarative plan with collision and reach checks removes these from the iteration loop entirely; layout bugs surface at plan-resolution time, not match-runtime, and are fixed in one place instead of six hardcoded offsets scattered across `bot_macro.fsx`.

**Independent Test**: Given a saved `MapGrid`, a base centre, and a named plan template, a call to `resolvePlan` returns a sequence of `(slotName, defName, position)` tuples in construction order. Each position can be verified against the map: non-cliff, non-water, non-overlapping, within builder reach. When the plan is fed into a macro bot, all structures in the plan finish construction without the `[commander-idle-defect]` telemetry line firing.

**Acceptance Scenarios**:

1. **Given** an opening plan with slots for 2 mex + 2 solar + 1 factory, **When** the bot calls `resolvePlan` at match start on Avalanche 3.4, **Then** the returned sequence has 5 tuples and the factory's resolved position is at least 64 elmos from every other structure (clearance margin honoured).
2. **Given** a plan slot specified as "nearest free metal spot", **When** the plan is resolved twice on the same map, **Then** both calls return the same metal spot (deterministic pick, not round-robin across runs).
3. **Given** a plan with a slot that cannot be satisfied (e.g. "3rd nearest metal spot" on a map with only 2 spots), **When** the plan is resolved, **Then** the function reports which slot failed and why — it does not return a garbage position.
4. **Given** a plan with a slot near the commander and another slot near base centre, **When** the builder reach radius is narrower than "commander → slot distance", **Then** the plan resolver either re-projects the slot toward the builder or reports the slot as unreachable (it does not silently emit an out-of-reach position the way iter 016 did).

---

### User Story 4 — Anti wall-in check on every placement (Priority: P2)

Before the bot issues any BuildCommand for a structure, the proposed placement is checked against a simple connectivity rule: after the structure is placed, must the base centre still be reachable from every previously-placed own structure AND from every corner of the current outer hull of own structures? If not, the placement is rejected and the builder is given either a relocated site or a fallback command (idle, retry later, use a looser clearance). This prevents the bot from accidentally walling itself in — a real risk once it starts building walls, turrets, or dense production lines.

**Why this priority**: The 023 macro bot never builds densely enough to wall itself in, so this hasn't yet caused a regression. It becomes a P2 risk as soon as the next iteration adds static defences (turrets at chokepoints — which is exactly what US2 unlocks). Priority P2 because it's a guard rail that prevents a future class of regression; the feature works without it at the 023 density.

**Independent Test**: Given a sequence of already-placed structures and a proposed new placement, `wouldWallIn` returns `true` if the new placement would disconnect the base centre from any existing structure (using the same passability rules as US1's pathing), and `false` otherwise. The test is: stage a ring of 8 structures around the commander with one gap, propose a structure in the gap, and confirm the check returns `true`.

**Acceptance Scenarios**:

1. **Given** a base with an open corridor leading out to a factory and a proposed structure placed in the middle of the corridor, **When** `wouldWallIn` is called for the proposed placement, **Then** it returns `true` and flags which pre-existing structures become unreachable from base centre.
2. **Given** the same scenario with the proposed structure placed to the side of the corridor, **When** `wouldWallIn` is called, **Then** it returns `false`.
3. **Given** a proposed placement that closes a loop but leaves the commander with alternate paths, **When** `wouldWallIn` is called, **Then** it returns `false` (a closed loop is fine as long as every interior point remains reachable from base centre through at least one surviving path).
4. **Given** a plan resolver from US3 that proposes a placement the anti-wall-in check rejects, **When** the plan is re-resolved, **Then** the plan either returns a relocated placement that passes the check or reports the slot as unfulfillable.

---

### User Story 5 — Deep macro bot refactor in one pass (Priority: P2)

The macro bot from feature 023 is refactored in a **single integration commit** to consume the four new primitives end-to-end: the opening-build sequence is replaced by a `resolvePlan` call against `defaultArmadaOpening`, the attack launch is replaced by `findPath` (with `ownStructures` threaded from the current placement set) routing peewees from the factory to the enemy commander, and the defend interrupt is replaced by positioning interceptors at the nearest chokepoint from `findChokepoints` instead of chasing intruders wherever they cross `baseRadius`. The refactor targets the same 023 iter 026 win outcome on NullAI (`commander-death-win-after-upgrade`) but with the new behaviours actually driving runtime commands, not just logging alongside the old logic.

**Why this priority**: The primitives are only valuable if a real consumer exercises them in live matches. P2 because US1–US4 are each individually testable against synthetic and SMF-parsed map data with no live engine required, so the primitives can ship before US5 is integrated. But US5 is the forcing function that proves they're runtime-grade, not just unit-test-grade.

**Independent Test**: One iteration of `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI smoke` against the refactored bot produces `result.json.cause = "commander-death-win-after-upgrade"`, and the run directory visibly exercises all four primitives: `phase_transitions.jsonl` shows the canonical Opening→Production→Upgrade→Attack sequence, stdout contains `[plan] resolved 5 slots`, `[attack] path has N waypoints`, and `[defend] chokepoint pos=...` lines. Rush bot `bot.fsx` must also remain runnable (FR-022/FR-023 inherited from 023).

**Acceptance Scenarios**:

1. **Given** the refactored macro bot running on Avalanche 3.4 against NullAI, **When** the opening phase begins, **Then** `resolvePlan` emits 5 `ResolvedSlot` records (2 mex + 2 solar + 1 factory) with no Failure flags and the commander builds each in order without the `[commander-idle-defect]` line firing.
2. **Given** the same bot transitioning into Attack phase, **When** peewees launch toward the enemy commander, **Then** they follow a `findPath` waypoint sequence (not a straight-line MoveCommand), visible as a stdout `[attack] path waypoints=<N>` trace, and reach within 32 elmos of the target within the Attack-phase window.
3. **Given** the same bot against BARb/dev, **When** BARb raiders enter base radius, **Then** the defend interrupt places interceptors at the nearest chokepoint from `findChokepoints` — visible via a `[defend] chokepoint pos=(X,Y) width=W` stdout line — instead of AttackCommand-chasing each raider where they crossed the boundary.
4. **Given** a commit of this refactor, **When** the rush bot `bot.fsx` is run on NullAI, **Then** it still produces a conformant run directory and a `win` outcome via the existing MoveCommand-to-unique-def pattern — no regression from the new helpers.
5. **Given** the refactor surfaces a regression in match behaviour (the one-pass deep integration fails on its first run), **When** the operator iterates, **Then** each subsequent iteration MUST fix exactly one issue at a time per 023 PLAYBOOK §2c — the "fix everything in one more commit" temptation is explicitly out of scope.

---

### Edge Cases

- **Pathing time budget**: A corner-to-corner path on a 4096×4096 elmo map has ~250K grid cells. A naïve A* could exceed the per-frame tactics deadline (~33 ms at GameSpeed=100). Pathing must either complete within the deadline or run incrementally across frames — it cannot block the frame loop indefinitely.
- **Dynamic terrain changes**: metal spots are static but LOS / radar change every frame. The primitives must distinguish "rebuild cost every N frames" from "use cached result" — a full `MapGrid` recompute every frame is not affordable.
- **Mobile enemy in chokepoint query**: BARb's raiders move between frames. Chokepoint results should be stable across short time windows so the defend interrupt doesn't thrash (see 023 BARb/dev probe's 60 oscillations).
- **Plan collision against existing enemy structures**: once we see an enemy building in LOS/radar, a plan slot that would place on top of it must either relocate or fail. The collision check uses the same `GameState.Enemies` map the rest of the bot uses.
- **Off-map coordinates**: plans that offset from a commander near the map edge could produce negative or out-of-bounds positions. The plan resolver must clamp/reject these at resolve time, not issue a BuildCommand the engine silently drops.
- **Multi-phase plans**: a plan that spans Opening → Production → Upgrade needs to track which slots have been consumed so a re-resolve mid-match doesn't re-issue the mex builds. Consumption state lives outside the static plan template.
- **Anti-wall-in false positives**: a proposed structure might pass the per-pair reachability check but still block a future plan slot that doesn't exist yet. The wall-in check only reasons about the current state; it is not a forward planner.
- **Pathing vs wall-in collaboration**: a plan's placement passes anti-wall-in at construction time but a later structure (or an enemy building discovered in LOS) could still trap the base. The anti-wall-in check runs on every placement — it's not a one-shot.

## Requirements *(mandatory)*

### Functional Requirements

**Pathing (US1)**

- **FR-001**: System MUST expose a pure function `findPath` that takes a `MapGrid`, a `MoveType`, an `ownStructures` collection (positions + footprints of our own placed structures), a start coordinate, and a goal coordinate, and returns either a sequence of waypoints (world coordinates, in construction order start-to-goal) or an explicit "no path" indicator.
- **FR-002**: System MUST compute waypoint paths using the `MapGrid.passability` grid for the specified `MoveType` AND must treat every grid cell covered by an `ownStructures` footprint as impassable, and MUST weight edge traversal cost by a function of distance plus slope (slope penalty configurable; default: linear with `slopeCost = 1 + slope * 2`).
- **FR-003**: System MUST return paths deterministically for identical inputs (same `MapGrid`, same `ownStructures`, same endpoints, same move type → same path).
- **FR-004**: System MUST return waypoints spaced such that any two consecutive waypoints are passable when connected by a straight line for the given move type, so the caller can issue straight-line `MoveCommand` between consecutive waypoints.
- **FR-005**: System MUST complete a path query up to a configurable budget (default 50 ms wall clock, or a configurable node count ceiling) and return a partial / approximate path with an explicit "budget exhausted" flag if the budget is hit before the goal is reached.
- **FR-006**: System SHOULD expose a helper `pathCost` that returns the estimated traversal cost of a path without requiring the caller to sum edge weights by hand.
- **FR-006a**: The caller owns cache invalidation: `findPath` MUST NOT mutate the `ownStructures` collection and MUST NOT cache results internally across calls with different `ownStructures` snapshots. Callers that want memoization are expected to key their own cache on `(mapGridVersion, ownStructuresVersion, start, goal, moveType)`.

**Chokepoint analysis (US2)**

- **FR-007**: System MUST expose a pure function `findChokepoints` that takes a `MapGrid`, a `MoveType`, a base-centre coordinate, and a search radius and returns a list of chokepoint descriptors ordered by distance from base centre (closest first).
- **FR-008**: Each chokepoint descriptor MUST include: a representative position (world coordinates), an estimated width (elmos), a direction vector pointing outward from base centre, and an identifier the caller can use to reference the chokepoint across subsequent queries (stable across deterministic re-queries).
- **FR-009**: System MUST return an empty list when no chokepoints exist within the search radius — a fabricated "least-wide point" on open terrain is a bug, not a valid answer.
- **FR-010**: System MUST classify a passage as a chokepoint only when its width is strictly less than a configurable maximum (default 5 heightmap cells / 40 elmos) AND when it is the only (or primary) route from inside the base radius to a region of the map outside the base radius.
- **FR-011**: System SHOULD provide deterministic chokepoint IDs stable across calls with the same `MapGrid`+base-centre — so the defend interrupt can track "we're already defending chokepoint X" without recomputing state.

**Building plans (US3)**

- **FR-012**: System MUST expose a `BasePlan` record that describes a named, typed set of structure slots relative to a base centre, independent of any particular `MapGrid`.
- **FR-013**: Each slot in a `BasePlan` MUST declare: its symbolic name (e.g. "mex#1"), its structure def name (e.g. "armmex"), its position chooser (nearest metal spot / offset from base / offset from commander / chokepoint head), its required builder type, and its clearance margin (an additive edge-to-edge distance, in elmos, that MUST remain empty between this slot's footprint and any other placed-or-resolved structure's footprint — see Clarifications 2026-04-13 Q4).
- **FR-014**: System MUST expose a `resolvePlan` function that takes a `BasePlan`, a `MapGrid`, a base centre, and a collection of already-placed-own-structure positions, and returns a sequence of `ResolvedSlot` records (slot name → concrete world position + selected builder reach feasibility) in construction order.
- **FR-015**: `resolvePlan` MUST reject a slot (and report the reason) when any of: the position is not buildable for a structure of the slot's footprint (using terrain + slope checks), the edge-to-edge distance between this slot's footprint and any previously-resolved slot OR previously-placed own structure is less than the clearance margin (FR-013), the position is outside the reach of the selected builder, or the position is off-map.
- **FR-016**: System MUST provide at least one built-in plan matching the current 023 opening sequence (2 mex + 2 solar + 1 factory) as `defaultArmadaOpening` so that feature-024 integration does not require the operator to hand-write a plan on day one.
- **FR-017**: System MUST allow plans to reference chokepoint identifiers from FR-011 as position choosers for defensive slots (e.g. "turret at chokepoint[0]").
- **FR-018**: System MUST expose plan consumption state (`PlanProgress`) so a long-running match can query "which slots have I already satisfied" without re-resolving the whole plan and double-issuing BuildCommands.

**Anti wall-in check (US4)**

- **FR-019**: System MUST expose a pure function `wouldWallIn` that takes a `MapGrid`, the base-centre coordinate, the current set of placed own structures (with footprints), a proposed new placement (def + position), and the `MoveType` used for "can the base be evacuated" semantics, and returns `true` if placing the proposed structure would disconnect the base centre from any point currently reachable, `false` otherwise.
- **FR-020**: `wouldWallIn` MUST use the same passability rules as FR-002's pathing (including the "ownStructures cells are impassable" rule) so a placement rejected by the wall-in check is guaranteed impassable to units of the same `MoveType`, and `findPath` called on the same inputs would also fail to cross the blocked cells.
- **FR-021**: `wouldWallIn` MUST return enough diagnostic data (which side of the base becomes unreachable, or which existing structure gets cut off) that the caller can log a meaningful `[wall-in-defect]` telemetry line, matching the discipline of FR-002 / FR-007 defect detectors in feature 023.
- **FR-022**: System MUST NOT mutate the `MapGrid` or the placed-structures collection during a `wouldWallIn` call — it is pure over inputs so the caller can repeatedly probe candidate placements.
- **FR-023**: System MUST integrate `wouldWallIn` into the `resolvePlan` function so that any slot resolved by a plan is automatically anti-wall-in-checked against the current structure set, with failing placements treated the same as clearance collisions (FR-015).

**SMF parser (test fixture enabler)**

- **FR-024**: System MUST expose a pure F# module that reads a BAR `.sd7` archive (7-zip), locates the `.smf` (Spring Map File) inside, parses the SMF header, and returns a populated `MapGrid` value equivalent to what `MapGrid.loadFromEngine` would return from a live engine for the same map (same `WidthHeightmap` / `HeightHeightmap`, same heightmap array shape, same metal/resource map array shape).
- **FR-025**: The SMF parser MUST handle Spring Map File format version 1 (the format BAR uses as of 2026-04). It MUST parse: header (magic `"spring map file"` + version + dimensions), tile index offsets, heightmap (int16 array decoded to float32 world-space heights), metal map (uint8 array), and type map (uint8 array). Texture tiles (`.smt` payload) are out of scope — the parser extracts terrain data only.
- **FR-026**: The SMF parser MUST compute a slope map locally from the heightmap using the same formula the Spring engine uses (derivative-based slope per heightmap cell), with the result dimensionally equivalent to the engine's `getSlopeMap` output so downstream primitives cannot tell the difference.
- **FR-027**: The parser MUST fail with a descriptive error (not silently zero-fill) when given a `.sd7` that lacks a `.smf` entry, an `.smf` with unsupported format version, or a truncated/corrupt heightmap.
- **FR-028**: The parser MUST NOT require a running BAR engine, a proxy connection, or any files beyond the `.sd7` archive itself.

**Integration + discipline (US5)**

- **FR-029**: The `bot_macro.fsx` macro bot from feature 023 MUST consume the new primitives and MUST continue to win cleanly on NullAI (iter 026 baseline) after the refactor, producing `result.json.cause = "commander-death-win-after-upgrade"`.
- **FR-030**: The rush bot `bot.fsx` MUST remain runnable and produce a conformant run directory at every commit (inherits FR-022/FR-023 from feature 023).
- **FR-031**: Feature 024 MUST follow the 020/023 commit-and-push discipline: one commit per meaningful change, every iteration line recorded in `HISTORY.md`, no unpushed commits.

### Key Entities

- **Path**: ordered sequence of world-coordinate waypoints plus an estimated cost, classified as `Complete | Partial { budgetExhausted }`. Produced by `findPath`.
- **Chokepoint**: a representative position, a width estimate, a direction vector, and a stable ID. Produced by `findChokepoints`.
- **PlanSlot**: symbolic description of a structure slot — name, def name, position chooser, required builder, clearance radius. Read-only input to `resolvePlan`.
- **BasePlan**: a named collection of `PlanSlot`s plus metadata (plan name, owning strategy tag). Reusable across matches.
- **ResolvedSlot**: output of `resolvePlan` — slot name, concrete position, estimated builder reachability, clearance-ok flag, wall-in-ok flag, resolution-failure reason (if any).
- **PlanProgress**: immutable record tracking which slots have been consumed in the current match — used to drive incremental `resolvePlan` calls.
- **WallInResult**: `Passes | Fails { reason; unreachablePositions }`. Produced by `wouldWallIn`.
- **SmfMap**: parsed Spring Map File — heightmap (float32[,]), metal map (uint8[,]), type map (uint8[,]), computed slope map, map dimensions. Produced by the SMF parser module, structurally compatible with `MapGrid` so the same downstream primitives consume either source.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A path query on a representative 2048×2048 elmo `MapGrid` completes within the configured wall-clock budget on ≥95% of calls across a 100-call randomised workload, and every returned path satisfies the passability invariant (every waypoint Passable, consecutive waypoints connectable by straight line).
- **SC-002**: On at least one saved `MapGrid` with a known central ridge, `findPath` returns a detour path that a human operator would accept as "correct" (the operator confirms the returned waypoints trace around the ridge rather than through it).
- **SC-003**: `findChokepoints` on the Avalanche 3.4 map at base centre `(500, 397)` returns a list whose top-1 result coincides (within ±150 elmos) with the human-recognised canyon entrance leading to the NullAI spawn area.
- **SC-004**: `resolvePlan` run against `defaultArmadaOpening` on Avalanche 3.4 produces 5 `ResolvedSlot` records with no `Failure` flags, and a subsequent macro bot iteration consuming those resolutions completes the opening phase with no `[commander-idle-defect]` stdout line — matching 023 iter 006/026 behaviour.
- **SC-005**: `wouldWallIn` correctly rejects a structure placement that closes a known one-corridor base (staged via a synthetic scenario) and accepts a placement that preserves at least one surviving path, verified by a unit test that constructs both scenarios against a synthetic `MapGrid`.
- **SC-006**: The refactored macro bot produces a clean win on NullAI with `cause = "commander-death-win-after-upgrade"` in ≤3 iterations after the 024 primitives land (measured from the first macro bot run on the 024 branch).
- **SC-007**: Rush bot (`bot.fsx`) remains winnable on NullAI after every commit on the 024 branch (verified via smoke iteration after each helper extraction / integration commit).
- **SC-008**: A second operator (fresh session, reads only `PLAYBOOK.md §13 Tactical primitives` and the new helper source files) can sketch a bot that reuses ≥3 of the 4 new primitives without modification — same discipline as feature 023 SC-009.
- **SC-009**: Feature 024 extraction cost — across all user stories, iterations classified as `infrastructure-regression` (helpers breaking the rush bot) stay at ≤10% of total iterations, matching the 023 SC-008 threshold.
- **SC-010**: The SMF parser produces a `MapGrid` from `~/.local/state/Beyond All Reason/maps/avalanche_3.4.sd7` whose heightmap dimensions match the live-engine `getCornersHeightMap` call (513 × 513 for Avalanche 3.4) and whose min/max values are within ±1 elmo of a **reference pair captured from a live engine** (allowing for float32 quantisation). The reference pair (currently 130.0 / 700.0 for Avalanche 3.4) is captured from a prior live-engine run — verified on 2026-04-13 via the FSI MCP probe against the installed BAR engine, and matches the 2026-04-06 HighBarV2 extraction report (`Mailbox/2026-04-06_highbarv2_map_data_extraction_report.md`) byte-for-byte. If a future BAR release ships a modified Avalanche 3.4, the reference pair is refreshed via a one-line FSI probe (see `quickstart.md §1`) and the constant in the test is updated.

## Assumptions

- **Terrain data is trustworthy**: `MapGrid.loadFromEngine` returns correct heightmap + slope + passability data across all BAR maps in scope. Feature 006 validated this against Avalanche 3.4; assumes it holds for BARb/dev rungs and any future ladder additions.
- **Static map topology**: metal spots, heightmap, and slope don't change during a match. The primitives cache `MapGrid.loadFromEngine` output once at warmup and refresh only LOS/radar per frame (FR-001..FR-010 all operate on the cached static layers).
- **MoveType granularity is sufficient**: the existing `Kbot / Tank / Hover / Ship` types from `MapGrid.fsi` adequately model the units the macro bot cares about. A future feature may add `Gunship`, `Spider`, etc.; not in scope here.
- **Armada-first default**: built-in plans target Armada unit def names because the 023 iteration used Armada. Cortex / Legion variants are operator discretion — a follow-up plan can be added by anyone following the plan template.
- **No enemy pathing inference**: the primitives reason about *our* movement, not enemy movement. We do not try to predict where BARb's raiders will path — we only identify chokepoints statically. This keeps the feature scope bounded.
- **Wall-in check is connectivity-only**: `wouldWallIn` uses simple reachability on the passability grid. It does not reason about "can my commander squeeze through an 8-elmo gap" — it uses the same grid resolution as pathing (heightmap cells, 8 elmos each).
- **Ongoing iteration on bot_macro.fsx**: US5 integration produces at least one commit on the 024 branch where the bot consumes the primitives. It does NOT commit to a full bot rewrite — the 023 structure is preserved.
- **Iteration budget interpretation from 023 still applies**: the `PLAYBOOK.md §10` 10-iter-per-rung cap applies to **win-seeking** iterations. Helper extraction and primitive validation iterations (US1–US4 testable without a live match) don't count against that budget.
- **Ladder unchanged**: the existing `NullAI` and `BARb/dev` rungs in `bots/trainer/ladder.json` are the validation targets. No new rungs in scope for feature 024.
- **No new NuGet dependencies**: implementation is in-repo F# on the existing `FSBar.Client` / `FSBar.Client.Tests` / `bots/trainer/helpers` surface. If pathing benefits from a priority-queue library, add it only if a comparable F# stdlib equivalent doesn't exist.
- **`.sd7` = 7-zip, extractor required**: BAR ships maps as 7-zip archives. The only 7-zip-compatible tool on the dev image is `bsdtar` (via libarchive). An in-F# 7-zip reader would be a new dependency; the spec assumes the SMF parser is allowed to shell out to `bsdtar` to decompress the `.smf` file to a temp location, then parse the `.smf` bytes in-process. If this is objectionable, a managed 7-zip library (SharpCompress, already transitively available via some .NET SDK paths) may be swapped in during the plan phase without spec change.
- **BAR maps installed locally**: tests assume `~/.local/state/Beyond All Reason/maps/` contains at least Avalanche 3.4 (mandatory — matches feature 023's ladder map). Other maps (Red Rock Desert v2, Comet Catcher Remake) are tested opportunistically — if the file is present the test runs, otherwise it skips. CI environments without BAR installed skip all SMF integration tests; this matches the existing 003 live-game-tests pattern.
- **Primitives live in `FSBar.Client`** (per clarification 2026-04-13 Q1): the four primitives land as new `.fs` / `.fsi` modules alongside `MapGrid` / `MapQuery`. This is a **Tier 1** change per the constitution — spec → plan → `.fsi` updates → surface-area baseline updates → test evidence all required. Scripts under `bots/trainer/helpers/` may still create thin wrappers for bot-specific concerns (e.g. a macro-bot-tuned default plan) but the core `findPath` / `findChokepoints` / `resolvePlan` / `wouldWallIn` logic lives in the compiled library so other consumers (`FSBar.Viz`, future bots, test harnesses) can reuse it without script coupling.
