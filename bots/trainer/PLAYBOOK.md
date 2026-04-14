# Trainer Playbook

**Feature**: 020-bot-iterative-trainer
**Branch**: `020-bot-iterative-trainer`
**Audience**: the operator (human or AI) driving the diagnose → improve → commit → push loop.

The primary objective of this feature is the **helper library** under
`bots/trainer/helpers/`. Winning matches is the forcing function that exposes
the next helper extraction. Every iteration should either improve the bot or
extract a helper.

---

## 0. Start of every iteration

```bash
# 1. Verify branch
git rev-parse --abbrev-ref HEAD    # → 020-bot-iterative-trainer

# 2. Before re-running, retry any push that failed previously (see §6)
git push origin 020-bot-iterative-trainer
```

If the branch check fails, stop and ask before proceeding.

---

## 1. Run one iteration

```bash
bash bots/trainer/run.sh <rung_name> <iter_id>
```

Arguments:

- `<rung_name>`: one of the names in `bots/trainer/ladder.json` → `.rungs[].name`.
- `<iter_id>`: three-digit counter starting at `001`, OR the literal `smoke`
  for the infrastructure check.

The runner writes `bots/runs/<timestamp>_<rung_slug>_<iter_id>/` containing
nine files. If any file is missing, that is an **infrastructure regression**
and must be fixed before any other classification (FR-022, SC-007).

---

## 2. Post-run analysis — fixed steps

After every run, walk these three commands in order:

```bash
# newest run dir
RUN=$(ls -1td bots/runs/* | head -1)
echo "inspecting $RUN"

# 2a. Terminal result
jq . "$RUN/result.json"

# 2b. Human-readable event trail (captured by Trainer.Log)
tail -n 80 "$RUN/stdout.log"

# 2c. Engine native infolog (last page only — the file is often huge)
tail -n 60 "$RUN/engine.infolog"
```

Read `result.json.outcome` first. Then use the decision tree below.

---

## 3. Classification decision tree

| outcome    | next step                                                                           | label                    |
|------------|--------------------------------------------------------------------------------------|--------------------------|
| `win`      | advance rung OR extract a helper if duplication is visible                          | `clean-win`              |
| `loss`     | read event trail — diagnose why commander died                                       | `bot-logic` / `helper-extraction` |
| `timeout`  | no progress was made; bot probably issued too few commands                          | `bot-logic`              |
| `error`    | exception ripped through frame loop                                                  | `bot-logic` / `repo-bug` |
| `interrupted` | operator hit Ctrl-C — not an iteration, discard and retry                        | n/a                      |

Then classify the root cause:

- **`bot-logic`** — the bot's decisions were wrong. Edit `bots/trainer/bot.fsx`.
- **`repo-bug`** — `FSBar.Client` misbehaved (exception, missing event, wrong
  command rendering). Edit `src/FSBar.Client/*.fs`, then run:
  ```bash
  dotnet build src/FSBar.Client/FSBar.Client.fsproj -c Debug
  dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug
  ```
  Add a regression test for the bug before fixing it (Constitution §III).
- **`helper-extraction`** — the same perception/tactic code is in `bot.fsx`
  for the second iteration running. Extract into
  `bots/trainer/helpers/perception.fsx` or `helpers/tactics.fsx` under a
  clearly named `let`. Update `bot.fsx` to call the new helper.
- **`infrastructure-regression`** — a required file is missing from the run
  directory, or `meta.json`/`result.json` failed to parse. Fix `run.sh` or
  the bot's exit paths. Counts against the SC-007 10% budget.
- **`out-of-scope`** — the cause is outside what we can fix on this branch
  (e.g. engine crash, new API surface required). Write a short report to
  `bots/runs/REPORT.md`, **HALT**, and ask the user.
- **`cross-repo-defect`** *(021 FR-021)* — the root cause is a defect in
  a sibling repo (today: HighBarV2 proxy behaviour). File an inbound
  mailbox per §11, **HALT** the loop on this rung, and do NOT edit the
  sibling repo from inside this feature.
- **`budget-exhausted`** *(021 FR-016a)* — the rung has consumed its
  10-iteration hard cap without clearing. File a budget-exhaustion
  mailbox per §10, suffix the HISTORY line with `[budget-exhausted]`,
  and **HALT** the loop on this rung.

Pick exactly one label per iteration. Write it into `HISTORY.md`.

---

## 4. Commit and push

Commit templates, one per classification:

| Label                    | Message template                                       |
|--------------------------|--------------------------------------------------------|
| `bot-logic`              | `trainer: bot iter <N> — <short description>`         |
| `repo-bug`               | `fix: <short description>` (+ `020: ...` if spec-tied)|
| `helper-extraction`      | `trainer: extract <name> into <helper> helper`        |
| `infrastructure-regression` | `trainer: runner <short description>`              |
| `clean-win`              | `trainer: rung <name> cleared on iter <N>`            |

Rules (FR-025–FR-030):

- Every iteration is its own commit. Do NOT amend.
- Staging: add only what changed; never `git add -A` blindly if the diff
  includes unrelated changes (e.g. baselines, caches).
- No pull requests. We push directly to `origin/020-bot-iterative-trainer`.
- No commits on `master`.

```bash
git add <paths>
git commit -m "<template from table above>"
git push origin 020-bot-iterative-trainer
```

---

## 5. HISTORY.md append

After commit-and-push, append exactly one line to `bots/trainer/HISTORY.md`
using this pipe-delimited format:

```
<iter_id> | <timestamp UTC> | <rung_name> | <outcome> | <frames> | <sha> | <run_dir_name> | <one-line note>
```

Example:

```
001 | 2026-04-12T14:30:00Z | NullAI | win | 4123 | a1b2c3d | 2026-04-12T14-30-00_NullAI_001 | first win; commander idle-walked past NullAI defenders
```

---

## 6. Push-failure recovery (FR-029)

If `git push origin 020-bot-iterative-trainer` exits non-zero:

1. **DO NOT amend or reset.** The commit is valid locally; the push is the
   only thing that failed.
2. Edit the most recent line of `HISTORY.md` to append a `[unpushed]` tag at
   the end of the note.
3. You may continue iterating locally. Each subsequent commit must still
   retry the push at step 0 of the next iteration, AND the current one.
4. Once a retry succeeds, remove the `[unpushed]` tag from every line that
   was previously unpushed. The tag is a local-only marker.

---

## 7. Stall detection (per FR-018)

After every iteration on the same rung, run this one-liner to read out the
last five runs' telemetry side-by-side:

```bash
RUNG_SLUG="NullAI"      # replace with current rung slug (no spaces, no /)
ls -1td bots/runs/*_"${RUNG_SLUG}"_* 2>/dev/null | head -5 | while read d; do
  jq -c --arg d "$(basename "$d")" \
    '{dir: $d, tel: .telemetry}' "$d/result.json"
done
```

Read the five lines. If NONE of the following strictly increased across the
five iterations, you are **stalled**:

- `frames_survived`
- `enemy_units_killed`
- `peak_metal`
- `peak_energy`
- `units_built`

On stall: write a short note into `HISTORY.md` with prefix `STALL:`, **HALT**,
and ask the user before retrying the same rung.

---

## 8. Rung advance

When the current rung produces a `win` outcome:

1. The iteration that won is committed and pushed.
2. Append the win to `HISTORY.md`.
3. Pick the next rung in `bots/trainer/ladder.json`. Reset `<iter_id>` back
   to `001` for the new rung — iteration counters are per-rung.
4. Continue.

The feature's primary completion criterion (SC-011) is: at least one `win`
on the no-op rung AND at least one `win` on the first competitive rung
(`BARb/dev`). After that, further rungs are best-effort — keep iterating
and extracting helpers until a natural stopping point.

---

## 9. Halt conditions

Halt and ask the user before continuing if any of these hold:

- Classification is `out-of-scope`.
- Stall detected on the current rung.
- Three consecutive iterations all classified `infrastructure-regression`.
- Any `dotnet test` failure on `FSBar.Client.Tests`.
- A push has failed twice in a row on unrelated error messages.
- The 10-iteration per-rung budget has been exhausted (see §10).
- A cross-repo defect has been identified (see §11).

---

## 10. Per-rung iteration budget (021 FR-016a)

Each rung in this feature's session has a **hard cap of 10 iterations**.
If a rung has not produced at least one clean canonical `win`
(`victory_signal=engine-shutdown-gameover`, derived from the real
`GameEvent.Shutdown` path — not a shim) within those 10 iterations,
do **not** start an 11th. Instead:

1. File a budget-exhaustion mailbox at
   `Mailbox/<YYYY-MM-DD>_from_FSBarV1_budget_exhausted_<rung-slug>.md`
   where `<rung-slug>` is the rung name with `/` replaced by `-` and
   lowercased. Examples:
   - `NullAI` → `nullai`
   - `BARb/dev` → `barb-dev`

2. The mailbox MUST contain: the rung name, the list of all 10
   iteration ids, each iteration's outcome, the trajectory of the
   telemetry fields used in the stall check (FramesSurvived,
   EnemyKilled, UnitsBuilt, PeakMetal, PeakEnergy), and a short
   hypothesis for why the rung did not clear.

3. Append the last iteration's HISTORY line with the literal suffix
   `[budget-exhausted]` (no space around the brackets).

4. **HALT** the loop on that rung. The operator may restart the rung
   only after either (a) an out-of-band fix lands on the feature
   branch that invalidates the budget-exhaustion hypothesis, or (b)
   an explicit operator decision to re-open the budget per a new
   `/speckit.clarify` round.

---

## 11. Cross-repo defect routing (021 FR-021)

If an iteration's classification is `cross-repo-defect` — meaning the
root cause is a defect in a sibling repo such as HighBarV2 proxy — do
**not** edit the sibling repo from inside this feature. Instead:

1. File an inbound mailbox to the sibling repo at
   `Mailbox/<YYYY-MM-DD>_to_HighBarV2_<short-symptom-slug>.md`
   using the same slug rule as §10 (`/` → `-`, lowercased, no
   spaces). Examples:
   - AttackCommand issues → `attack-command-stationary`
   - Callback NaN on unexpected resource id → `economy-callback-nan`

2. The mailbox MUST contain: a pointer to the run directory that
   exhibits the defect, the relevant `frames.jsonl` excerpt, the
   relevant `engine.infolog` excerpt, and a minimal repro plan if the
   sibling repo's test harness can host it.

3. **HALT** the loop. Do not start another iteration on the current
   rung until the sibling repo responds with a fix or a disposition.

Iterations made while a cross-repo defect is outstanding are not
counted against the §10 budget — a halted loop is halted.

---

## 12. Macro archetype (023-trainer-builder-economy)

The macro bot (`bot_macro.fsx`) is a builder-economy archetype layered
on top of the same runner, helpers, and discipline as the rush bot. It
drives a four-phase state machine plus a defend interrupt. This section
is the operator's quick reference for running it, classifying macro
regressions, and knowing which helper to edit.

**Invocation**:

```
BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh <rung> <iter>
```

(Omitting `BOT_SCRIPT` runs the existing rush bot `bot.fsx` — both bots
must remain runnable at every commit on this branch per FR-022/FR-023.)

**Post-run phase-transition diagnostic**:

```
cat bots/runs/<run_dir>/phase_transitions.jsonl | jq -c .
```

The absence of this file in a macro-bot run is a bot-logic regression,
not infrastructure. For a rush-bot run the file is expected to be
absent — the rush bot does not call `logPhaseTransition`.

### 12.1 Macro phases and their triggers

| Phase      | Entry trigger                                                                            | Helper owning the predicate                     |
|------------|------------------------------------------------------------------------------------------|-------------------------------------------------|
| Opening    | Match start                                                                              | `opening_build.fsx` (`defaultOpening` + `nextOpeningCommand`) |
| Production | First factory finishes construction (FR-004)                                             | `opening_build.fsx.openingComplete` emits the event |
| Upgrade    | `entryPredicateMet`: metal income ≥ 20 AND factory-built count ≥ 6                      | `upgrade_gate.fsx.entryPredicateMet`            |
| Attack     | `decideUpgradeExit → AttackNow Normal`: upgrade reached AND combat units ≥ 12           | `upgrade_gate.fsx.decideUpgradeExit` + `attack_launch.fsx.launchFreshCombat` |
| Attack     | `decideUpgradeExit → AttackNow DeadlineFallback` (FR-012): deadline exceeded AND combat ≥ 12 | `upgrade_gate.fsx.decideUpgradeExit` |
| (stall)    | `decideUpgradeExit → StallAndLose` (FR-012): deadline exceeded AND combat < 12          | `upgrade_gate.fsx.decideUpgradeExit`; records `upgrade-stall-no-army` in `phase_transitions.jsonl`; bot overrides `result.json.cause = loss-by-stall-upgrade-deadline` |
| Defending  | Any non-critter enemy inside `baseRadius` (FR-016b interrupt, not a proper phase)        | `perception.fsx.enemiesInBase` + bot-side critter filter |

**Phase invariants**:
- `Defending` is an interrupt: on exit the bot resumes the phase it was in before the intruder arrived (stored in `preDefendPhase`).
- `Attack` is terminal — the bot does not return to Production/Upgrade once launched.
- The defend interrupt filters out critter defs (name prefix `critter`) at warmup so static NullAI wildlife doesn't trap the bot.
- Upgrade→Attack is wired via `decideUpgradeExit`, which respects the "no degenerate rush" invariant: never AttackNow with `Reached.IsNone AND combat < threshold`.

**Canonical end-of-match outcomes**:
- Clean win: `outcome=win, cause=commander-death-win-after-upgrade, victory_signal=engine-shutdown-gameover`
- Deadline-fallback win: same outcome with `cause=commander-death-win-deadline-fallback`
- Stall loss: `outcome=loss, cause=loss-by-stall-upgrade-deadline` (bot-side override when `upgradeGateState.StallRecorded=true`)
- Timeout (non-terminal): `outcome=timeout, cause=frame limit reached (max_frames=...)` — the bot didn't complete the macro cycle in the rung's budget; classify via the labels in §12.2.

**Second-operator quickstart (SC-009)** — to reproduce a macro clean win on NullAI from scratch:
1. `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI smoke`
2. Expect telemetry like: `{units_built≈60, units_lost≤15, enemy_units_killed≥1, peak_metal≈2000, peak_energy≈2000, frames_survived≈21000}`
3. Expect `phase_transitions.jsonl` with exactly these transitions (frame numbers ±200): Opening→Production (~2750), Production→Upgrade (~10350), Upgrade→Attack (~16500), optional defend interrupts
4. Expect `result.json`: `outcome=win, cause=commander-death-win-after-upgrade, victory_signal=engine-shutdown-gameover`
5. To reuse ≥3 helpers in a new bot: `#load` `opening_build.fsx` + `production_queue.fsx` + `attack_launch.fsx` and wire the `resolveXxx`/`observeFrame`/`launchFreshCombat` calls at warmup + per-frame. See `bot_macro.fsx` as the reference consumer.

### 12.2 Macro-specific classification labels

When diagnosing a failed macro iteration, use these labels in the
HISTORY note column in addition to the §3 generic labels.

- **`opening-regression`** — bot failed to complete the opening phase.
  - *Symptoms*: missing `Opening→Production` line in `phase_transitions.jsonl`, fewer than 5 `UnitFinished` events for opening defs, `[commander-idle-defect]` line in stdout, `units_built < 5`.
  - *Common causes (observed in iters 001-006)*: (a) faction mismatch — using Cortex def names when commander is Armada, engine silently drops BuildCommand; (b) position collision — two 2×2 structures placed within ~40 elmos of each other; (c) Y-coord from `getMetalSpots` passed literally to BuildCommand (terrain altitude ≠ nominal build-site y); (d) advance-on-UnitCreated vs advance-on-UnitFinished — commander abandons partial structures if we advance on Created.
  - *Fix location*: `helpers/opening_build.fsx` (`defaultOpening`, `nextOpeningCommand`) or `bot_macro.fsx` warmup.

- **`production-regression`** — factory queue stalls or role mix is off.
  - *Symptoms*: `Opening→Production` fires but `units_built` growth flatlines post-transition, `[idle-dispatch-defect]` lines in stdout, or only one queue role (only constructors or only combat) in `queueState.ObservedBuilt`.
  - *Common causes (observed in iters 007-012)*: (a) `IsIdle` filter unreliable (engine's `UnitIdle` event doesn't fire for fresh factory products — use `dispatchedConstructors : Set<int>` instead); (b) FR-008 metal-income gate closed permanently (mex dispatch stuck — constructors can't raise income).
  - *Fix location*: `helpers/production_queue.fsx` or `helpers/constructor_dispatch.fsx`.

- **`upgrade-stall`** — hit the FR-012 deadline without reaching the upgrade predicate.
  - *Symptoms*: `phase_transitions.jsonl` shows `upgrade-stall-no-army`, `result.json.cause=loss-by-stall-upgrade-deadline`.
  - *Common causes (observed in iters 013-021)*: (a) `upgradeDeadlineFrame` too tight for the current opening+production runway; (b) wrong builder for the t2 structure — `armcom` cannot build `armalab`, only `armck` can (faction-specific build options — check `UnitDefCache.ById[commanderDefId].BuildOptions` at warmup); (c) single-builder t2 build rate too slow — commander should GuardCommand the armck to add its build speed.
  - *Fix location*: `helpers/upgrade_gate.fsx` (`UpgradeThresholds`) or `bot_macro.fsx` Upgrade-phase command branch.

- **`attack-regression`** — attack launched but commander not reached.
  - *Symptoms*: `Upgrade→Attack` line present, `attackLaunched=true` in stdout, but `enemy_units_killed=0` or mismatched `cause`.
  - *Common causes (observed in iters 024-028)*: (a) `FightCommand` vs `MoveCommand` — FightCommand stops units to engage en route, MoveCommand lets them run straight and auto-fire; (b) travel-time underestimate vs `max_frames` budget — NullAI default 18000 is too tight for macro, raise to 36000; (c) pathing blocked at distant target (HighBar's open watch-item — if AttackCommand goes stationary, fall back to MoveCommand/FightCommand).
  - *Fix location*: `helpers/attack_launch.fsx` (`issueLaunch`, `launchFreshCombat`) or `ladder.json` (`max_frames`).

- **`defend-oscillation`** — defend-interrupt state flips many times per second (observed on BARb/dev probe iter 001).
  - *Symptoms*: `phase_transitions.jsonl` contains dozens of 1-frame-duration `Opening→Defending`/`Defending→Opening` pairs, opening never completes.
  - *Common causes*: an enemy unit's position oscillates across `baseRadius` boundary due to GameState snapshot timing, OR the critter filter isn't catching all harmless enemies.
  - *Fix location*: hysteresis in `bot_macro.fsx` defend interrupt (require N consecutive frames of clear before exiting Defending), or widen the critter filter in `resolveCritterDefIds`.

### 12.3 Helper edit map

Each row says "when this symptom appears in the run log, edit this
helper". Helper files are filled in as US1..US4 iterations organically
produce two extraction sites under FR-020.

- **US1 landed**: `helpers/opening_build.fsx` — opening-build order
  helper. Extracted at iter 3 (T015) after iter 001 inlined the
  sequence and iter 002 kept it unchanged while fixing the
  defend-interrupt critter filter. Exposes `defaultOpening`,
  `resolveOpeningBuildOrder`, `nextOpeningCommand`, `advanceOnCreated`,
  `markIssued`, `openingComplete`, `sortMetalSpotsByDistance`. Edit
  when tuning the opening sequence or adding new `PositionChooser`
  cases; keep `bot_macro.fsx` consumer in sync.
- **US2 landed**: `helpers/production_queue.fsx` — factory queue
  keeper. Extracted at iter 9 (T019) after iter 007 inlined the queue
  and iter 008 kept it unchanged while adding the commander-guard fix.
  Exposes `QueuePolicy`, `ResolvedQueuePolicy`, `QueueState`,
  `defaultArmadaKbotPolicy`, `resolveQueuePolicy`, `queueDepth`,
  `pickNextQueueDef`, `computeQueueTopUp`, `observeFrame`,
  `factoryIdleSince`. Edit when tuning queue tunables, adding new
  queue items, or changing the FR-008 gate policy.
- **US2 landed**: `helpers/constructor_dispatch.fsx` — idle-constructor
  dispatcher. Extracted at iter 12 (T021) after iter 010 inlined the
  dispatch with a broken `IsIdle` filter (0 dispatches) and iter 011
  fixed the filter to use explicit `dispatchedConstructors` tracking
  (17 dispatches, metal income 34.8/s). Exposes `DispatchState`,
  `DispatchDecision`, `emptyDispatchState`, `findConstructors`,
  `dispatchIdle`, `idleDefectCandidates`, `markDefectReported`. Edit
  when adding Repair/AssistCommander job types, refining the
  constructor def-allowlist, or tuning the idle-defect threshold.
- **US3 landed**: `helpers/upgrade_gate.fsx` — upgrade entry/exit
  predicates + FR-012 stall path. Extracted at iter 22 (T025) after
  iter 013 inlined the gate and iter 021 verified the full
  Production→Upgrade→Attack path with a fix sequence that converged
  on `[BuildCommand armck armalab; StopCommand commander; GuardCommand
  commander armck]`. Exposes `UpgradePredicateName`, `UpgradeAttackPath`,
  `UpgradeExitDecision`, `UpgradeThresholds`, `UpgradeGateState`,
  `emptyUpgradeGateState`, `entryPredicateMet`, `upgradeReached`,
  `markReached`, `decideUpgradeExit`. FR-012 stall-path verified via
  iter 023-stall (deadline=1800 → `loss-by-stall-upgrade-deadline`).
  Edit when tuning thresholds, adding new upgrade predicates, or
  changing the no-degenerate-rush invariant.
- **US4 landed**: `helpers/attack_launch.fsx` — army composition +
  attack launch. Extracted at iter 28 (T030) after iter 024 inlined
  with FightCommand (units stopped to engage en route, never arrived)
  and iter 025-026 switched to MoveCommand + raised NullAI
  max_frames → first macro clean win on NullAI at iter 026. Exposes
  `AttackLaunchState`, `isCombatDef`, `countCombatUnits`,
  `buildLaunchSnapshot`, `issueLaunch`, `launchFreshCombat`,
  `maybeRetarget`, `pickAttackTarget`. Edit when tuning the combat
  classifier, adding per-faction unit allowlists, or refining the
  attack target selection.

## 13. Tactical primitives integration (024-tactical-map-primitives)

Feature 024 ships five new `FSBar.Client` modules that the macro bot
consumes for observability and for the defend-interrupt routing:

- **`SmfParser`** — `.sd7` → `SmfMap` → `MapGrid`. Used exclusively
  offline by `scripts/examples/14-cache-map-analysis.fsx`; the runtime
  bot does not call it.
- **`Pathing`** — A\* over `MapGrid.passability` with slope-weighted
  edges and an `ownStructures` mask. Currently log-only at attack launch
  (`[attack] path waypoints=N cost=C status=...`) and skipped in the
  default macro iteration because no live `MapGrid` is pinned at warmup.
- **`Chokepoints`** — union-find bridge detection over a distance
  transform. Runs once per map offline via the cache script; the runtime
  bot reads the JSON at warmup.
- **`WallIn`** — pure connectivity predicate sharing passability rules
  with `Pathing`. Used transitively by `BasePlan.resolvePlan`.
- **`BasePlan`** — declarative slot layout + `resolvePlan` validator.
  Runs live at warmup (pure CPU, < 1 ms) against a synthetic `MapGrid`
  skeleton whose dimensions come from `Callbacks.{getMapWidth, getMapHeight}`.

### Offline pipeline

Maps are fixed, so any analysis that only depends on the `.sd7` is a
function of the file and belongs in a precomputed cache. Never run
`findChokepoints` or `MapGrid.loadFromEngine` during bot warmup — the
250 ms block holds the frame-reading path long enough for the proxy's
socket write buffer to fill at 100× headless speed, tripping
`Socket not writable, dropping frame` in the engine's infolog and
eventually OOM-ing the Lua VM (see HISTORY `024-macro-smoke-replay-4`).

```bash
# Run once per map before the first trainer iteration
dotnet fsi scripts/examples/14-cache-map-analysis.fsx "Avalanche 3.4"
# Writes bots/trainer/map-cache/avalanche_3.4.json
```

The cache file holds the `Chokepoint` list, grid dimensions, and the
`ChokepointQuery` used, keyed on the map name.

### Runtime consumption in `bot_macro.fsx`

`bot_macro.fsx` warmup now:

1. Loads the cached chokepoint list via `File.ReadAllText` +
   `JsonDocument.Parse`. < 10 ms. Stashes the list in `pinnedChokepoints`.
2. Builds a synthetic `MapGrid` skeleton via `Callbacks.{getMapWidth,
   getMapHeight}` (two trivial RPCs, single-call each).
3. Runs `BasePlan.resolvePlan BasePlan.defaultArmadaOpening` against the
   skeleton. Emits `[plan] resolved 5 slots (5 buildable now)` plus one
   `[plan] slot <name> (<def>) resolved @ (x,z)` or `[plan] slot <name>
   failed: <reason>` per `ResolvedSlot`. On `WouldWallIn`, an extra
   `[wall-in-defect] proposed=<slot> …` line fires.
4. Flips `FSBar.Client.Protocol.replayBufferEnabled <- true` and enters
   `trainerLoopRun`. From that point, mid-game callbacks (the 023
   `[probe-idle]` / `[probe-periodic]` `getUnitPos` RPCs) preserve the
   `UnitFinished` events the opening helper depends on.

### Defend interrupt uses chokepoints

When the bot enters `Defending`, the 023 "chase nearest enemy id via
`AttackCommand`" logic is replaced with "`MoveCommand` every finished
unit to the nearest pinned chokepoint's position". Falls back to the
023 nearest-enemy path when `pinnedChokepoints` is empty (open terrain
or the cache wasn't loaded). On first entry a one-shot
`[defend] chokepoint pos=(x,z) width=W id=...` line fires.

### Reading the stdout traces

| Trace | Where it fires | What to check |
|---|---|---|
| `[chokepoint] loaded N chokepoints from cache <path>` | Bot warmup after Protocol replay-buffer flip | Cache is on disk and readable. Run the offline cache script if missing. |
| `[chokepoint] pos=... width=... id=... distFromBase=...` | One per pinned chokepoint, top-5 only | Widths realistic for the map (Avalanche canyons ≈ 60–160 elmos). |
| `[plan] resolved N slots (M buildable now)` | Once at warmup | M should equal N for a clean opening. Anything less is a `Failure` and gets its own line. |
| `[plan] slot <name> resolved @ (x,z)` / `[plan] slot <name> failed: <reason>` | One per `PlanSlot` | Positions match `BasePlan.defaultArmadaOpening` offsets. |
| `[wall-in-defect] proposed=<name> <WallInReason>` | When `BasePlan.resolvePlan` returns a `WouldWallIn` failure | The proposed placement would isolate at least one structure or the base itself. Edit the plan or relocate the slot. |
| `[defend] chokepoint pos=(x,z) width=W id=...` | One-shot, first time the bot enters Defending | Chokepoint is near the raid axis. If absent, falls back to the 023 nearest-enemy AttackCommand. |
| `[attack] launching N combat units at target (x,z)` | First Attack-phase launch (from 023 `attack_launch` helper) | N ≥ `combatUnitThreshold` = 12 on the default plan. |
| `[attack] path waypoints=N cost=C status=Complete` | When a `MapGrid` is pinned (currently only via operator-set TACTICAL_WARMUP=1) | Informational only — log trace for route analysis. |
| `[attack] findPath skipped (no MapGrid)` | Default runtime path | Expected. The bot does not currently pin a live `MapGrid` to keep warmup fast. |

### Adding new `BasePlan` entries

`BasePlan.defaultArmadaOpening` is a `BasePlan` value in
`src/FSBar.Client/BasePlan.fs`. To add a defensive-turret slot:

1. Append a `PlanSlot` with a `PositionChooser`:
   - `NearestMetalSpot i` for economy extensions
   - `NearBaseCentre(dx, dz)` for static-offset slots
   - `AtChokepointHead k` to place a structure at the k-th pinned chokepoint
   - `AtLiteralPosition (x, y, z)` for one-off placements
2. Pick `BuilderDefName` (`"armcom"` for the commander, `"armck"` for a
   construction kbot). The reach check uses the builder's
   `MaxWeaponRange` from `UnitDefCache`, falling back to the table in
   `BasePlan.fs` for unit names the cache doesn't know.
3. Set `ClearanceMargin` in elmos (16 is a comfortable default for
   small footprints; 32 for factories).
4. Regenerate the surface-area baseline via `UPDATE_BASELINES=true
   dotnet test --filter "FullyQualifiedName~SurfaceAreaTests.BasePlan"`
   if the `.fsi` changed.
5. Add or update a unit test in `BasePlanTests.fs` that resolves the
   new plan against a `SyntheticMapGrid.flat` with stubbed metal spots.
6. Re-cache the map with the new plan: `dotnet fsi
   scripts/examples/14-cache-map-analysis.fsx "<map>"`.

### Classification labels added by this feature

- `[infrastructure-regression]` / `[pre-existing]` — inherited from
  024 US5 T063 diagnosis. A failure in an event path the bot assumed
  worked. See mailbox
  `Mailbox/2026-04-14_to_HighBarV2_mid-game-callback-event-drop.md`.
- `[replay-buffer-verified]` — mid-game callback round-trip
  preserved a `UnitFinished` event that the pre-031 path would have
  dropped. Indicates the HighBarV2 031 fix landed correctly on the
  FSBarV1 side.
- `[map-cache-landed]` — warmup used a disk cache instead of running
  analysis live.

