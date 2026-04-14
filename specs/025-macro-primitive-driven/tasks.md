---
description: "Task list for feature 025-macro-primitive-driven"
---

# Tasks: Macro Bot Primitive-Driven Command Path

**Input**: Design documents from `/specs/025-macro-primitive-driven/`
**Prerequisites**: [spec.md](./spec.md), [plan.md](./plan.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: One mandatory unit test on the Tier 1 `Commands` surface change (FR-008a). Live-iteration verification is the primary evidence path for US1/US2/US3/US4/US5 per Constitution §III.

**Organization**: Tasks are grouped by user story. **Critical commit-ordering note**: per the spec's "deep single-pass refactor replacing hardcoded logic in one commit" framing, US1 + US2 + US3 + US4 bot_macro.fsx edits land in a **single atomic commit**. The task breakdown below treats them as separate phases for clarity, but the commit gate at end of Phase 6 (US4) is the same commit that includes Phases 3/4/5. See `contracts/bot-macro-integration.md` §"Commit discipline" for the full ordering.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

## Path Conventions

- **Tier 1 library delta**: `src/FSBar.Client/Commands.{fs,fsi}`
- **Tier 1 unit test**: `src/FSBar.Client.Tests/CommandsTests.fs`
- **Surface-area baseline**: `src/FSBar.Client.Tests/Baselines/Commands.baseline` (or equivalent — verified in Phase 2)
- **Cache writer**: `scripts/examples/14-cache-map-analysis.fsx`
- **FSI example**: `scripts/examples/NN-queued-move.fsx` (NN = next available number, determined in Phase 2)
- **Integration consumer**: `bots/trainer/bot_macro.fsx`
- **Rush bot (unchanged but guarded)**: `bots/trainer/bot.fsx`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the 025 branch baseline and prove the 024 merge is healthy before any edits land.

- [X] T001 Verify branch `025-macro-primitive-driven` is checked out and HEAD is at `c8888ad` or later (the 024 squash-merge point on master). Run `git status` + `git log --oneline -5` to confirm; no uncommitted changes expected besides the new `specs/025-macro-primitive-driven/` directory.
- [X] T002 Run the rush-bot baseline smoke: `BOT_SCRIPT=bot.fsx bash bots/trainer/run.sh NullAI 025-baseline-rush`. Confirm `bots/runs/025-baseline-rush-*/result.json` contains `"outcome": "win"` with a `cause` mentioning engine shutdown. This establishes the FR-019 invariant baseline.
- [X] T003 [P] Determine the next-available example-script number for `scripts/examples/NN-queued-move.fsx` by running `ls scripts/examples/ | grep -oE '^[0-9]+' | sort -n | tail -1`. Record the chosen NN (likely `15` if 14 is the most recent 024-era script) for use in Phase 2 and Phase 7.
- [X] T004 [P] Locate the `FSBar.Client` surface-area baseline file by running `ls src/FSBar.Client.Tests/Baselines/` (or `tests/FSBar.Client.Tests/Baselines/` per the actual repo layout). Record the exact filename covering the `Commands` module for use in Phase 2 task T008.

**Checkpoint**: Baseline rush smoke is green. Surface-area baseline location is known. The 025 branch is ready for Tier 1 plumbing.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Land the Tier 1 `FSBar.Client.Commands` API surface change (new `MoveCommandQueued` function) and the extended map-cache writer. These two workstreams are independent of each other and of `bot_macro.fsx`. Both MUST complete before Phases 3–6 (the single-commit integration) can begin, because Phase 4 (US2) consumes `MoveCommandQueued` and Phase 6 (US4) consumes the extended cache.

**⚠️ CRITICAL**: Rush bot (`bot.fsx`) MUST remain runnable after every commit in this phase (FR-019). Each sub-phase ends with a rush smoke.

### Tier 1 plumbing: queued MoveCommand (commit 1)

> **TDD gate**: T005 MUST be written and T005's new tests MUST fail (on `MoveCommandQueued` not-yet-defined) before T006/T007 are written. Per Constitution §III.

- [X] T005 Add two new `[<Fact>]` unit tests to `src/FSBar.Client.Tests/CommandsTests.fs`: `MoveCommandQueued sets INTERNAL_ORDER and SHIFT_KEY bits` (asserts `Options = 40u` and `Options = INTERNAL_ORDER ||| SHIFT_KEY`) and `MoveCommand does NOT set SHIFT_KEY bit` (asserts `Options &&& 32u = 0u`). Reference the exact assertion shape in `contracts/commands-queued-move.md` §"Unit test delta". Run `dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj --filter "FullyQualifiedName~MoveCommandQueued"` and confirm both tests FAIL (compilation error or missing symbol). Maps to FR-008a.
- [X] T006 Add the `SHIFT_KEY = 32u` literal and the `MoveCommandQueued` signature to `src/FSBar.Client/Commands.fsi`. Insert after the existing `MoveCommand` declaration (currently line 6). Use the exact `.fsi` delta from `contracts/commands-queued-move.md` §".fsi delta", including the XML-doc comment. Maps to FR-008a.
- [X] T007 Add the `SHIFT_KEY = 32u` literal and the `MoveCommandQueued` function implementation to `src/FSBar.Client/Commands.fs`. Insert after the existing `INTERNAL_ORDER = 8u` literal (line 7) and after the `MoveCommand` builder (lines 14–21). Use the exact `.fs` delta from `contracts/commands-queued-move.md` §".fs delta". Run `dotnet build src/FSBar.Client/FSBar.Client.fsproj` and confirm zero errors. Maps to FR-008a.
- [X] T008 Refresh the `FSBar.Client` surface-area baseline captured in the file recorded in T004. Run `dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj --filter "FullyQualifiedName~SurfaceArea"` — expect one failure (new symbol `Commands.MoveCommandQueued` detected). Refresh the baseline via the existing mechanism (typically an env var like `UPDATE_BASELINES=1` or a regen flag — consult the baseline test source for the exact invocation). Re-run the surface-area test and confirm it passes. Maps to Constitution §II.
- [X] T009 Run the full `src/FSBar.Client.Tests` suite: `dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj`. Confirm all tests pass including the two new FR-008a tests and the refreshed surface baseline. No regressions.
- [X] T010 Commit: `feat(Commands): add MoveCommandQueued for waypoint traversal (025 FR-008a)`. Include `Commands.fs`, `Commands.fsi`, the baseline file, and `CommandsTests.fs`. Commit message body should reference the FR-008a trace and the Tier 1 classification. Do NOT `git push` until after T011.
- [X] T011 Run the rush smoke gate: `BOT_SCRIPT=bot.fsx bash bots/trainer/run.sh NullAI 025-iter1-rush`. Confirm `"outcome": "win"` — the rush bot must still win cleanly after the Commands API addition. (FR-019 preserved.) Then `git push origin 025-macro-primitive-driven`.

### Extended map-cache writer (commit 2)

- [X] T012 Edit `scripts/examples/14-cache-map-analysis.fsx` to emit the `mapGrid` block per the writer sketch in `contracts/map-cache-format.md` §"Writer contract". Add a `gzipAndBase64` helper function (≈15 LOC using `System.IO.Compression.GZipStream` + `System.Convert.ToBase64String`) and extend the JSON emission to include the `mapGrid` object with `schemaVersion = 1`, the three base64-gzipped arrays (`heightMap.gzip.b64`, `slopeMap.gzip.b64`, `resourceMap.gzip.b64`), and the matching dimensions. Maps to FR-014 write path + R1 decision.
- [X] T013 Re-bake the Avalanche 3.4 cache: `dotnet fsi scripts/examples/14-cache-map-analysis.fsx 'Avalanche 3.4'`. Confirm the new file at `bots/trainer/map-cache/avalanche_3.4.json` is ~400–800 KB (vs ~2 KB for the 024 schema) and that a Python one-liner confirms `mapGrid.schemaVersion = 1`: `python3 -c 'import json; d = json.load(open("bots/trainer/map-cache/avalanche_3.4.json")); print("has mapGrid:", "mapGrid" in d, "schemaVersion:", d.get("mapGrid",{}).get("schemaVersion"))'`. Expected output: `has mapGrid: True schemaVersion: 1`.
- [X] T014 Add `bots/trainer/map-cache/*.json` to the repo `.gitignore`. Run `git rm --cached bots/trainer/map-cache/avalanche_3.4.json` (the 024 version of this file was tracked). Confirm `git status` shows `.gitignore` modified, the old cache file removed from the index, and `scripts/examples/14-cache-map-analysis.fsx` modified. The newly-baked extended cache file should NOT appear in `git status` (it's now ignored).
- [X] T015 Commit: `feat(cache): extend map-cache schema with MapGrid blob (025 FR-014)`. Include `.gitignore`, the git-rm of the old cache file, and the `14-cache-map-analysis.fsx` extension. Body references FR-014, R1 decision, and `contracts/map-cache-format.md`. Do NOT push until after T016.
- [X] T016 Run the rush smoke gate: `BOT_SCRIPT=bot.fsx bash bots/trainer/run.sh NullAI 025-iter2-rush`. Confirm `"outcome": "win"`. The rush bot does not read the map cache at all, so this should pass trivially — but the gate discipline matters. Then `git push origin 025-macro-primitive-driven`.

**Checkpoint**: `Commands.MoveCommandQueued` is shipped and tested. Extended map cache is written and verified. Rush bot still wins cleanly. Phases 3–6 can now begin — but all four will be committed atomically as one commit per the spec's single-pass-refactor framing.

---

## Phase 3: User Story 1 — BasePlan drives opening command emission (P1) 🎯 MVP

**Goal**: The macro bot's Opening phase issues `BuildCommand`s derived from `BasePlan.resolvePlan`'s `ResolvedSlot` positions, not from the 023 `opening_build.fsx` helper's hardcoded offset list. Mid-game plan re-resolution runs on every Opening tactics tick (~30 game frames / ~1 sim-second, per clarification Q4).

**Independent Test**: After the integration commit lands, run `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 025-iter3-macro` and `grep '[plan] issuing BuildCommand <def> @ (x,z) from resolvePlan' bots/runs/025-iter3-macro-*/stdout.log` — expect ≥5 matches (one per `defaultArmadaOpening` slot) AND `grep -c '[opening] idx=' bots/runs/025-iter3-macro-*/stdout.log` should return 0 (the 023 helper's emission signature is mutually exclusive). Maps to SC-002.

**Code-landing gate**: Phase 3 tasks modify `bot_macro.fsx` only. No commit at end of phase — the single-commit rule means Phases 3/4/5/6 batch into one commit at T044.

> **Phase-boundary note**: T017 intentionally declares both `planProgress` (US1 state) and the `AttackPathCache` record + `attackPathCache` mutable (US2 state). This forward declaration avoids an FSI script-load forward-reference error, because Phases 3/4/5/6 batch into the atomic commit at T044 and there is no intermediate commit at which only US1 state exists. T024 in Phase 4 verifies the T017 declaration; it does not redeclare.

### Implementation for User Story 1

- [X] T017 [US1] Add two module-mutable declarations near the top of `bots/trainer/bot_macro.fsx` (around line 273 alongside existing `mapGrid`/`pinnedChokepoints`/`planResolvedAtWarmup` mutables): `let mutable planProgress : BasePlan.PlanProgress = BasePlan.emptyPlanProgress` and the `AttackPathCache` record type declaration + `let mutable attackPathCache : AttackPathCache option = None` (T024 also references `attackPathCache`; declaring both here avoids a forward-reference in Phase 4). Maps to data-model.md §1 + §4.
- [X] T018 [US1] Add three private helpers near the top of `bot_macro.fsx` (below the mutables, above the main tactics function):
    1. `structureDefNames : Set<string>` — computed once from `BasePlan.defaultArmadaOpening.Slots |> List.map (fun s -> s.DefName) |> Set.ofList`. This is the authoritative structure classifier for feature 025: any DefName in the active plan is a structure; anything else is not. The classifier auto-updates when `defaultArmadaOpening` is edited, preserving the US1 acceptance scenario "edit the plan, behaviour changes on the next iteration."
    2. `isStructureDef (cache: UnitDefCache) (defId: int) : bool` — looks up the def by id via `UnitDefCache.tryFindById`, returns `Set.contains info.Name structureDefNames`, returns `false` on lookup failure. Rationale: `UnitDefInfo` in `src/FSBar.Client/UnitDefCache.fs:5–13` has no `IsBuilding` / `SpeedMax` flag, so a positive-set classifier from the plan's DefName list is the only unambiguous approach. See data-model.md §2 "Structure classifier" for the full reasoning.
    3. `currentExistingStructures () : OwnStructureFootprint list` — walks `client.GameState.Units |> Map.toSeq`, filters to `u.IsFinished && isStructureDef client.GameState.UnitDefs u.DefId`, maps each match to an `OwnStructureFootprint` record. Consult `src/FSBar.Client/Pathing.fsi:7–14` at implementation time for the exact field names (likely `Centre` + `Xsize`/`Zsize`). Footprint dimensions come from the matching `PlanSlot` metadata: walk `defaultArmadaOpening.Slots` for the slot whose `DefName` matches `info.Name` and read its footprint fields.
    Called fresh per Opening tactics tick. Maps to FR-001 + data-model.md §2.
- [X] T019 [US1] Rewrite the Opening-phase tactics-tick command-emission path in `bot_macro.fsx` to call `BasePlan.resolvePlan BasePlan.defaultArmadaOpening context` at the start of each tactics tick (clarification Q4 cadence), build the `ResolveContext` with `ExistingStructures = currentExistingStructures ()` and `Progress = planProgress`. Use the exact sketch in `contracts/bot-macro-integration.md` §"Opening-phase command emission edits" step 1. Maps to FR-001.
- [X] T020 [US1] Inside the Opening-phase tactics-tick handler, emit a `Commands.BuildCommand` whose `(x, y, z)` matches the `ResolvedSlot.Position` exactly for the first slot with `BuildableNow = true && Slot.Name ∉ planProgress.ConsumedSlots`. Log `[plan] issuing BuildCommand <defName> @ (<px>,<pz>) from resolvePlan`. Maps to FR-002, SC-002.
- [X] T021 [US1] On each `BuildCommand` emission in T020, update the persistent mutable: `planProgress <- BasePlan.markInFlight planProgress slot`. Maps to FR-003.
- [X] T022 [US1] Wire `BasePlan.markConsumed` into the existing `UnitFinished` event handler in `bot_macro.fsx`. Whenever `UnitFinished` fires with a `DefName` matching any slot in `defaultArmadaOpening`, update `planProgress <- BasePlan.markConsumed planProgress slot`. Maps to FR-004.
- [X] T023 [US1] Wrap the `BasePlan.resolvePlan` call in a `try`/`with` block. On exception, log `[plan] resolvePlan exception — falling back to 023 helper: <msg>` and call `opening_build.nextOpeningCommand` as the belt-and-suspenders recovery. Do NOT enter this fallback when `resolvePlan` returns slots with `Failure = Some _` — only on thrown exceptions. Maps to FR-005, FR-006.

**Checkpoint**: Opening-phase command emission is wired to `BasePlan.resolvePlan`. `planProgress` persists across ticks. Exception-fallback path preserves `helpers/opening_build.fsx` as the recovery mechanism (FR-006 / FR-020). Code is in-place but not yet committed — continue to Phase 4.

---

## Phase 4: User Story 2 — Pathing.findPath drives attack routing (P1)

**Goal**: Each combat unit in an attack launch receives a sequence of queued `MoveCommand`s corresponding to the waypoints of a single cached `Pathing.findPath` result. First waypoint unqueued (replaces existing order); subsequent waypoints use `Commands.MoveCommandQueued` (SHIFT_KEY bit set, appends to queue).

**Independent Test**: After the integration commit lands, run the macro smoke and check `bots/runs/025-iter3-macro-*/stdout.log` contains `[attack] path waypoints=N cost=C status=Complete` (typically N=3 on Avalanche 3.4) AND `unwired_commands.json` contains ≥ `(combat_units × N)` `MoveCommand` entries in the attack-launch frame range. Maps to SC-003.

**Code-landing gate**: same single-commit rule — continue to T044.

### Implementation for User Story 2

- [X] T024 [US2] Verify the `AttackPathCache` record type declaration from T017 has the four fields required by Phase 4: `TargetUnitId: int`, `TargetPosition: (float32 * float32 * float32)`, `Path: Pathing.Path`, `LaunchTick: int`. If T017 declared a subset, extend it in-place; do not redeclare the type in a new location. Maps to data-model.md §1.
- [X] T025 [US2] Reuse the `currentExistingStructures` helper from T018 as the source of `ownStructures` for the Attack-phase `Pathing.findPath` call. Rename it to `currentOwnStructureFootprints` if a clearer name is wanted (single source of truth; both the Opening-phase `ResolveContext.ExistingStructures` and the Attack-phase `Pathing.findPath`'s `ownStructures` parameter consume the exact same footprint list). Do NOT duplicate the classifier or the footprint-derivation logic. One source of truth.
- [X] T026 [US2] Add an `emitWaypointCommands` helper inside the Attack-phase block of `bot_macro.fsx`. For each combat unit, issue the first waypoint via `Commands.MoveCommand uid w0.X w0.Y w0.Z` (unqueued, replaces existing order) and every subsequent waypoint via `Commands.MoveCommandQueued uid w.X w.Y w.Z` (SHIFT_KEY set, appended to queue). Use the sketch in `contracts/bot-macro-integration.md` §"Attack-phase command emission edits" step 2. Maps to FR-008.
- [X] T027 [US2] Rewrite the first-Attack-phase-tick command emission path in `bot_macro.fsx` (currently around line 623 where `helpers/attack_launch.fsx` is invoked) to call `Pathing.findPath grid MoveType.Kbot ownStructures centre tpos budget` **once** per launch with `budget = 50` ms. Cache the result as `attackPathCache <- Some { TargetUnitId = tid; TargetPosition = tpos; Path = path; LaunchTick = currentFrame }`. Log `[attack] path waypoints=N cost=C status=<Complete|Partial budget-exhausted>`. Maps to FR-007, FR-011, SC-003.
- [X] T028 [US2] On subsequent Attack-phase ticks when `attackPathCache` is `Some` and valid, emit the cached waypoints via `emitWaypointCommands` for any NEW combat units that weren't in the previous tick's combat set. Do NOT re-run `findPath`. Maps to FR-009.
- [X] T029 [US2] Add the target-death check at the start of each Attack tactics tick (clarification Q3 / FR-009a): if `attackPathCache = Some cache` and `cache.TargetUnitId` is absent from `client.GameState.Units`, log `[attack] target <id> absent from GameState — re-pathing`, clear the cache, and fall through to the first-tick branch from T027 (which re-runs `pickAttackTarget` + `findPath` in the same tick). Maps to FR-009a, data-model.md §1 lifecycle row.
- [X] T030 [US2] Handle `Result.Error NoRoute`: log `[attack] findPath NoRoute — falling back to direct move`, set `attackPathCache <- None`, and emit a single direct `Commands.MoveCommand` per combat unit toward `tpos` (matching the 024-partial behaviour). Maps to FR-010.
- [X] T031 [US2] Handle `Result.Ok { Status = Partial true }`: issue the partial waypoints via `emitWaypointCommands` AS-IS (first unqueued + rest queued), log the `Partial budget-exhausted` status. Do NOT implement any retry logic (clarification Q5). Re-pathing only fires via T029 target-death or attack-phase exit. Maps to FR-011.

**Checkpoint**: Attack-phase command emission is wired to `Pathing.findPath` with a single per-launch call and cached reuse. Target-death invalidation is in place. Partial and NoRoute fallbacks preserved. Continue to Phase 5.

---

## Phase 5: User Story 3 — Defend interrupt filters to combat units (P2)

**Goal**: When the defend interrupt fires (enemy in base radius), the bot routes only `isCombatDef`-classified units to the nearest chokepoint's `Position`. Workers, constructors, and the commander continue their current tasks. When the filtered combat set is empty, fall through to the 023 nearest-enemy `AttackCommand` commander-fallback path.

**Independent Test**: The NullAI rung does not fire the defend interrupt, so the US3 behaviour is verified structurally at the code level (code path exists and filters correctly) rather than by a live-fire assertion on the 025 smoke. A future BARb live-rung will exercise the filter in anger; for 025, SC-002/SC-003 confirm the code is reachable via the macro bot's post-warmup `tacticsFn`.

**Code-landing gate**: same single-commit rule — continue to T044.

### Implementation for User Story 3

- [X] T032 [US3] In `bots/trainer/bot_macro.fsx` around line 461 (the `nearestChokepointTo centre pinnedChokepoints` branch), replace the combat-unit filter `client.GameState.Units |> Seq.filter (fun (_, u) -> u.IsFinished)` with `client.GameState.Units |> Seq.filter (fun (_, u) -> u.IsFinished && Attack_launch.isCombatDef client.GameState.UnitDefs u.DefId)`. Use the exact edit in `contracts/bot-macro-integration.md` §"Defend-interrupt edits". Maps to FR-012.
- [X] T033 [US3] Add the `[defend] routing combat units only n=<N>` trace on the filtered path. When the filtered combat set is empty, emit `[defend] no combat units available — commander fallback` and fall through to the 023 `nearestEnemyId` path that issues a `Commands.AttackCommand commanderId <eid>` (the existing fallback branch in the same function). Maps to FR-013.

**Checkpoint**: Defend-interrupt filter now gates on `isCombatDef`. Commander fallback preserved. Continue to Phase 6.

---

## Phase 6: User Story 4 — Real MapGrid at runtime without warmup catch-up OOM (P2)

**Goal**: The macro bot's `BasePlan.resolvePlan` call consumes a real `MapGrid` (correct heightmap, non-zero slope values, correct resource map) loaded from the extended cache written in Phase 2. On cache-miss for a target-set map (Avalanche 3.4), hard-fail warmup with an actionable error. On cache-miss for non-target-set maps, log `[cache-miss] WARN` and degrade to the 024 synthetic skeleton. Warmup total CPU budget < 100 ms. Zero `Socket not writable, dropping frame` lines in `engine.infolog` during the warmup window.

**Independent Test**: `grep -c 'Socket not writable, dropping frame' bots/runs/025-iter3-macro-*/engine.infolog` MUST return 0. Engine-frame delta between `[trainer] BarClient connected` and `[trainer] entering main frame loop` MUST be ≤ 1000 game frames. Maps to FR-016, FR-017, SC-005.

**Code-landing gate**: the integration commit. Phase 6's final task is the atomic commit of Phases 3/4/5/6 together.

### Implementation for User Story 4

- [X] T034 [US4] Add a `MapGridCache_loadFromJson` local helper function (~40 LOC) to `bots/trainer/bot_macro.fsx`. Implementation per `contracts/map-cache-format.md` §"Reader contract": `JsonDocument.Parse` the cache file, check `mapGrid.schemaVersion = 1`, base64-decode + gzip-decompress each of the three arrays, reconstruct `Array2D<float32>` values, and return `Some MapGrid` or `None` on absent `mapGrid` block. Hard-fail with `failwithf` on schema-version mismatch, dimension mismatch, or gzip truncation. Place the helper near the existing chokepoint-cache parser (~line 840 region).
- [X] T035 [US4] Add a `MapTargetSet.contains` helper in `bot_macro.fsx`: single-element list `[ "Avalanche 3.4" ]` per clarification Q2. Placed alongside `MapGridCache_loadFromJson`. Maps to FR-014 target-set gating.
- [X] T036 [US4] Replace the synthetic `planMapGrid` construction at `bot_macro.fsx:874–891` with the real-`MapGrid` load using the sketch from `contracts/bot-macro-integration.md` §"Warmup path edits" step 1. On target-set cache-miss: `failwithf "[warmup] no MapGrid in cache at %s — run scripts/examples/14-cache-map-analysis.fsx '%s'"`. On non-target-set cache-miss: `printfn "[cache-miss] WARN: US1/US2 will behave like 024 partial — run 14-cache-map-analysis.fsx"` and fall through to the 024 synthetic skeleton (exact same code as the 024 partial). Maps to FR-014.
- [X] T037 [US4] Store the loaded grid in the existing `mutable mapGrid : MapGrid option` (line 273) so the Attack-phase `findPath` path from Phase 4 (T027) consumes it. Confirm no code elsewhere in `bot_macro.fsx` overwrites `mapGrid` to the synthetic skeleton after this point.
- [X] T038 [US4] Wrap the warmup US5 block (chokepoint load + MapGrid load + resolvePlan + any other warmup CPU work) in a `Stopwatch.StartNew()` measurement. After the block, log `[warmup] CPU budget <elapsedMs> ms (limit 100 ms)`. If > 100 ms, ALSO emit `[warmup] WARN: CPU budget exceeded`. Non-fatal — the warning is diagnostic only, not a failure gate. Maps to FR-015.
- [X] T039 [US4] Manually stress-test the warmup budget before the integration commit: run `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 025-warmup-probe-1` and inspect `stdout.log` for the `[warmup] CPU budget` trace. Confirm < 100 ms and zero `Socket not writable` lines. If > 100 ms, investigate which sub-step is hot (likely candidates: gzip decompress, `Array2D.zeroCreate` large grids) before continuing to T040.
- [X] T040 [US4] Run the full macro smoke to confirm the integration compiles and boots: `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 025-warmup-probe-2`. Any FSI compilation errors from Phases 3/4/5/6 must be fixed here. Do NOT worry about win/loss outcome at this probe — the probe's purpose is only to confirm the script loads and the warmup traces fire.
- [X] T041 [US4] Verify FR-021 invariant before the commit: `git diff --name-only` should list `bots/trainer/bot_macro.fsx` and nothing else from `src/FSBar.Client/{Pathing,SmfParser,Chokepoints,WallIn,BasePlan}.{fs,fsi}`. The 024 primitives are frozen for this feature.
- [X] T042 [US4] Verify FR-020 invariant: `test -f bots/trainer/helpers/opening_build.fsx && dotnet fsi --check bots/trainer/helpers/opening_build.fsx` (or the equivalent quick syntax check) — confirm the file is still present and syntactically valid. The 023 helper is the exception-fallback path from T023.
- [X] T043 [US4] Verify FR-019 invariant one more time before the integration commit: `BOT_SCRIPT=bot.fsx bash bots/trainer/run.sh NullAI 025-iter2b-rush`. Confirm `"outcome": "win"`. This gate catches any accidental shared-file damage that would break the rush bot.
- [X] T044 [US1][US2][US3][US4] **The atomic integration commit**. Stage `bots/trainer/bot_macro.fsx` (the only file changed across Phases 3/4/5/6) and commit: `feat(bot_macro): drive commands from BasePlan + Pathing primitives (025 US1-US4)`. Body: reference FR-001..FR-014, clarifications Q1–Q5, the contracts in `specs/025-macro-primitive-driven/contracts/`, and the spec's "deep single-pass refactor" framing. Do NOT push yet — Phase 7 runs the first macro iteration next, which may produce a fix commit before push.

**Checkpoint**: All four user-story code paths land in one commit (T044). The macro bot now consumes primitives for real. Ready for first live iteration in Phase 7.

---

## Phase 7: User Story 5 — Invariant verification (P1 invariant)

**Goal**: The macro bot preserves its clean NullAI win (`cause = "commander-death-win-after-upgrade"`) on the first integration iteration, within the 3-iter SC-007 budget. The rush bot (`bot.fsx`) still wins at every commit.

**Independent Test**: `bash bots/trainer/run.sh NullAI 025-iter3-macro` produces `result.json.cause = "commander-death-win-after-upgrade"` AND `result.json.victory_signal = "engine-shutdown-gameover"`. AND `bash bots/trainer/run.sh NullAI 025-iter3-rush` produces `outcome=win` with `frames ≤ 13000`. Maps to SC-001, SC-004, SC-007.

### Implementation for User Story 5

- [X] T045 [US5] Run the first macro live iteration: `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 025-iter3-macro`. Inspect `bots/runs/025-iter3-macro-*/result.json`. Expected: `"outcome": "win"`, `"cause": "commander-death-win-after-upgrade"`, `"victory_signal": "engine-shutdown-gameover"`. Maps to FR-018, SC-001.
- [X] T046 [US5] SC-002 verification: `grep -E '\[plan\] issuing BuildCommand' bots/runs/025-iter3-macro-*/stdout.log | wc -l` MUST be ≥ 5 (one per `defaultArmadaOpening` slot). AND `grep -c '\[opening\] idx=' bots/runs/025-iter3-macro-*/stdout.log` MUST be 0 (the 023 helper's emission signature is mutually exclusive). If either fails, the US1 code path is not firing — diagnose via the `[plan] resolvePlan exception` trace and file a fix commit.
- [X] T047 [US5] SC-003 verification: `grep '\[attack\] path waypoints' bots/runs/025-iter3-macro-*/stdout.log` returns at least one line with `status=Complete` and waypoints ≥ 2. Cross-check with `unwired_commands.json`: attack-launch frame range contains ≥ (combat_units × waypoints) `MoveCommand` entries. On Avalanche 3.4 Player-1 start, expect waypoints = 3 and ~36 MoveCommand entries per launch.
- [X] T048 [US5] SC-005 verification: `grep -c 'Socket not writable, dropping frame' bots/runs/025-iter3-macro-*/engine.infolog` MUST be 0. AND the engine-frame delta between the first `BarClient connected` and `entering main frame loop` trace MUST be ≤ 1000 game frames. Maps to FR-016, FR-017.
- [X] T049 [US5] SC-004 verification: run the rush bot smoke in parallel to the macro check: `BOT_SCRIPT=bot.fsx bash bots/trainer/run.sh NullAI 025-iter3-rush`. Confirm `"outcome": "win"` with `frames ≤ 13000` (within 5% of the 024 rush baseline ~12390). Maps to FR-019, SC-004.
- [X] T050 [US5] **Conditional fix-per-iter loop**: if T045 produced a clean win, skip to T051. If T045 did NOT produce `cause = "commander-death-win-after-upgrade"`, diagnose the failure signature (stdout.log + engine.infolog), make **one** targeted fix-commit, re-run the macro smoke as iter 4 (`025-iter4-macro`), and re-check SC-001..SC-005. Budget: 3 total iters (iter 3/4/5). If iter 5 still fails, halt and file `Mailbox/2026-04-XX_budget-exhaustion-025.md` per 023 PLAYBOOK §10. Maps to SC-007, FR-018.
- [X] T051 [US5] Once SC-001 is green, `git push origin 025-macro-primitive-driven`. Macro bot is primitive-driven and proven on NullAI.

**Checkpoint**: Live iteration green. SC-001..SC-005 all verified. Rush bot invariant preserved. Feature is technically complete; Phase 8 handles documentation and cross-cutting polish.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Update documentation, append history entries, and flag the stale proto comment discovered in research R2.

- [X] T052 [P] Append a one-line-per-iteration entry to `bots/trainer/HISTORY.md` for each 025 iteration that ran in Phase 7. Format matches the existing 023/024 entries in the file: date, iter tag, commit sha, outcome, short notes.
- [X] T053 [P] Append a new section `§13 primitive-driven command path (025)` to `bots/trainer/PLAYBOOK.md` documenting: how the extended map cache is written, how the bot's cache-miss fallback splits between target-set hard-fail and non-target-set degrade, and the operator command to re-bake a cache when maps are added to the target set. Cross-reference `specs/025-macro-primitive-driven/quickstart.md`.
- [X] T054 [P] Optionally update `bots/trainer/README.md` to mention the `bot_macro.fsx` primitive-driven behaviour and point readers at `specs/025-macro-primitive-driven/` for details. Skip if the README is already sufficient.
- [X] T055 [P] Create `scripts/examples/NN-queued-move.fsx` (NN from T003) — a minimal FSI example per Constitution §V that demonstrates the queued `MoveCommand` variant end-to-end: construct a three-waypoint sequence, emit one unqueued `MoveCommand` + two `MoveCommandQueued`, print the `Options` bitmask of each command so the operator can visually confirm `40u` on the queued entries. Body reference: `contracts/commands-queued-move.md` §"Wire-level contract summary".
- [X] T056 [P] Run the `fsdoc` agent on the `FSBar.Client.Commands` module per Workflow gate 7. The public API surface gained one new function (`MoveCommandQueued`) plus one new literal (`SHIFT_KEY`) — both need XML-doc comments, known-issues refresh, and (if the fsdoc agent emits it) an updated `tests.fsx` entry. Skip other modules — feature 025 does not touch them.
- [X] T057 Add a one-line note to `proto/highbar/common.proto` (or equivalent tracker) flagging the stale `line 18` comment for a future doc-only cleanup feature. DO NOT edit the proto in 025 — R2 explicitly keeps it out of scope. The note can live in `bots/trainer/HISTORY.md` or in a dedicated `Mailbox/2026-04-XX_proto-comment-stale.md` so the next maintainer sees it.
- [X] T058 Run `quickstart.md` §6 verification checklist end-to-end. Tick every box. Any unchecked box blocks merge. Reference: `specs/025-macro-primitive-driven/quickstart.md`.
- [X] T059 When all of Phase 8 is complete and the branch is green, invoke `/speckit-mergeBranches` (or the repo-equivalent squash-merge-to-master workflow). Merging is an explicit user action; do not auto-merge.

**Checkpoint**: Feature 025 is documented, merged, and shipped. `FSBar.Client.Commands` has a new public function, the map cache has an extended schema, and the macro bot's Opening/Attack/Defend command paths now consume the 024 primitives directly.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: no dependencies — can start immediately on branch checkout.
- **Phase 2 (Foundational)**: depends on Phase 1. **BLOCKS all user-story phases** because Phase 4 consumes `MoveCommandQueued` (T006/T007) and Phase 6 consumes the extended cache (T012/T013). Phase 2 contains two internal sub-phases (Tier 1 Commands + cache writer) that are independent of each other; they can land in either commit order but both must land before Phase 3 starts.
- **Phase 3 (US1)** / **Phase 4 (US2)** / **Phase 5 (US3)** / **Phase 6 (US4)**: all depend on Phase 2. These four phases edit the same file (`bot_macro.fsx`) and land in a **single atomic commit** at T044. They can be implemented in any order within the working copy, but the commit MUST be atomic.
- **Phase 7 (US5 invariant)**: depends on Phase 6 (T044 commit). This phase runs the first live iteration and may add fix-commits per SC-007.
- **Phase 8 (Polish)**: depends on Phase 7 (clean win established).

### User Story Dependencies

- **US1 (Opening drives commands)**: depends on Phase 2 (cache writer + `.fs` delta). Independently testable via SC-002 trace count.
- **US2 (Pathing drives attack)**: depends on Phase 2 (Tier 1 Commands `MoveCommandQueued`). Depends on US4's real `MapGrid` being loaded at warmup — technically US2's Attack-tick path can fall through to NoRoute direct-move if `mapGrid = None`, but the "meaningful waypoint traversal" SC-003 assertion requires US4's real grid. The single-commit rule avoids ordering pain.
- **US3 (Defend filter)**: depends only on existing `Attack_launch.isCombatDef`. Independent of US1/US2/US4 at the code level; bundled into the same commit only because of the spec's single-pass-refactor framing.
- **US4 (Real MapGrid)**: depends on Phase 2 extended cache writer. Enables the "real terrain" assertion of US1 (clearance checks) and US2 (`findPath` cost weights).
- **US5 (Invariant)**: depends on all four above landing.

### Within Each User Story

- US1: T017 → T018 → T019 → T020 → T021 → T022 → T023 (sequential, same file region).
- US2: T024 → T025 → T026 → T027 → T028 → T029 → T030 → T031 (sequential, same file region).
- US3: T032 → T033 (sequential, small edit).
- US4: T034 → T035 → T036 → T037 → T038 → T039 → T040 → T041 → T042 → T043 → T044 (T044 is the commit gate).
- US5: T045 → (T046 ∥ T047 ∥ T048 ∥ T049 parallel verification) → T050 (conditional loop) → T051.

### Parallel Opportunities

- **T003 ∥ T004**: Phase 1 recon tasks (record example-script number, locate baseline file) are independent.
- **T005 → T006/T007 sequential** (TDD gate: test first, then signature, then implementation). T008 (baseline refresh) must come after T007.
- **T010 (Commands commit) ∥ T012 (cache writer edit)**: the two foundational sub-workstreams touch disjoint files and can overlap. T012 can begin as soon as T010 is written; it does not depend on T010's commit being in HEAD. The two commits (T010 and T015) can land in either order.
- **T046 ∥ T047 ∥ T048 ∥ T049**: Phase 7 verification greps / rush smoke all read the iter 3 artifacts (and T049 runs an independent smoke); they are independent data checks.
- **T052 ∥ T053 ∥ T054 ∥ T055 ∥ T056 ∥ T057**: all Phase 8 polish tasks touch disjoint files.

---

## Parallel Example: Phase 2 foundational sub-workstreams

```bash
# Developer A: Tier 1 Commands plumbing
Task: "T005 add FR-008a unit tests to CommandsTests.fs"
Task: "T006 add MoveCommandQueued signature to Commands.fsi"
Task: "T007 add MoveCommandQueued impl to Commands.fs"
Task: "T008 refresh Commands surface-area baseline"

# Developer B (or same developer, serially): cache writer extension
Task: "T012 extend 14-cache-map-analysis.fsx with mapGrid block"
Task: "T013 re-bake avalanche_3.4 cache and verify schemaVersion=1"
Task: "T014 gitignore bots/trainer/map-cache/*.json and git rm --cached the 024 file"
```

---

## Parallel Example: Phase 7 verification

```bash
# After T045 (first macro iter) lands:
Task: "T046 SC-002 trace count — [plan] issuing vs [opening] idx"
Task: "T047 SC-003 [attack] path waypoints trace + unwired_commands.json cross-check"
Task: "T048 SC-005 zero Socket-not-writable + ≤1000-frame warmup delta"
Task: "T049 SC-004 rush bot smoke in parallel iter tag"
```

---

## Implementation Strategy

### MVP scope

This feature does not have a partial-ship MVP. Per the spec's "deep single-pass refactor replacing hardcoded logic in one commit" framing, US1+US2+US3+US4 land together or not at all. The "MVP" is the entire feature in a single atomic integration commit (T044), preceded by the two foundational commits (T010 and T015). The rush-bot invariant (FR-019) is the commit-by-commit safety net — every commit on the 025 branch must pass the rush smoke.

### Incremental delivery — commit ordering

1. **Commit 1** (T010): Tier 1 `Commands` delta + unit test + baseline refresh. Rush bot unaffected. Macro bot unaffected.
2. **Commit 2** (T015): cache writer extension + cache re-bake + gitignore. Rush bot unaffected. Macro bot unaffected.
3. **Commit 3** (T044): atomic US1+US2+US3+US4 integration in `bot_macro.fsx`. Rush bot still runnable. Macro bot now primitive-driven.
4. **Commits 4..N** (T050): optional fix-per-iter commits if iter 3 does not clean-win. Budget 3 total iters (iter 3/4/5). Halt at iter 5 with mailbox.
5. **Final** (T051 push + T059 merge): push + invoke `/speckit-mergeBranches`.

### 023 PLAYBOOK §2c discipline

Every commit on the 025 branch ends with a rush-smoke gate. Any commit that breaks the rush bot is rolled back immediately — even if it is the atomic integration commit. Fix forward, not by merging a broken branch into master.

---

## Notes

- **[P] tasks** = different files, no dependencies on incomplete prior tasks in the same phase.
- **[Story] labels** = trace each task to the FR / SC it satisfies. Phase 1/2/8 tasks have no story label because they are cross-cutting.
- **Commit atomicity**: Phases 3/4/5/6 land in **one commit** (T044). Do not tempt fate with partial commits inside the integration — the spec framing is explicit.
- **FR-021 guard**: `git diff --name-only master..025-head -- src/FSBar.Client/{Pathing,SmfParser,Chokepoints,WallIn,BasePlan}.{fs,fsi}` MUST return nothing at feature end. T041 is the explicit gate; T058 reverifies in Phase 8.
- **Verify tests fail before implementing**: T005 tests MUST fail before T006/T007. Per Constitution §III TDD gate for public API changes.
- **Commit after each task or logical group**: foundational sub-workstreams commit at end of each (T010, T015). Integration commits only once at T044. Live-iteration fix commits are per-iter.
