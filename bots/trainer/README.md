# Bot Iterative Trainer

**Features**: [020](../../specs/020-bot-iterative-trainer/spec.md) →
[021](../../specs/021-rerun-trainer-highbar/spec.md) →
[022](../../specs/022-incorporate-highbar-030/spec.md) →
[023](../../specs/023-trainer-builder-economy/spec.md)
**Current branch**: `023-trainer-builder-economy`

A scripted trainer for Beyond All Reason AI bots. Runs an F# `.fsx` bot in a
headless engine against one of several opponent rungs, captures structured
logs into a run directory, and supports an operator-driven diagnose-improve-
commit-push loop.

**Two in-tree bots** (feature 023):
- `bot.fsx` — the **rush bot**: MoveCommand-based commander-rush targeting
  the unique-def enemy. Clean win on NullAI + BARb/dev via a single moving
  commander. Simplest possible bot that still wins.
- `bot_macro.fsx` — the **macro bot** (023 archetype): 4-phase state
  machine (Opening → Production → Upgrade → Attack) plus FR-016b defend
  interrupt. Selected via the `BOT_SCRIPT` environment variable.

Both bots share the same helper library under `helpers/` and must remain
runnable on every commit per FR-022/FR-023.

## Mantra

**The primary objective is the helper library. Winning is the forcing function.**

Every iteration either improves `bot.fsx` or extracts a reusable helper into
`helpers/`. The ladder escalation exists to drive extractions, not to produce
a leaderboard bot.

## Quickstart

See [`specs/020-bot-iterative-trainer/quickstart.md`](../../specs/020-bot-iterative-trainer/quickstart.md)
for the one-time setup (engine patch, FSBar.Client build) and the first run.

Short version:

```bash
bash bots/trainer/engine-patches/install-barb-profiles.sh
dotnet build tests/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug
# rush bot (default, omits BOT_SCRIPT):
bash bots/trainer/run.sh NullAI smoke
# macro bot (023 archetype):
BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI smoke
```

### BOT_SCRIPT environment variable

`run.sh` reads `BOT_SCRIPT` to decide which `.fsx` file to launch.
Default is `bot.fsx`. The variable is exported so the bot can see
which script it is (useful for divergent code paths that live in the
same file).

## Iteration loop

Follow [`PLAYBOOK.md`](PLAYBOOK.md) every iteration. Append to
[`HISTORY.md`](HISTORY.md) after every commit. No pull requests — all work
goes directly on the `020-bot-iterative-trainer` branch with commit-and-push
after every change (FR-025 through FR-030).

## Directory layout

```
bots/trainer/
├── bot.fsx                         rush bot (020/021/022 lineage)
├── bot_macro.fsx                   macro bot (023 archetype)
├── helpers/
│   ├── prelude.fsx                 #r directives + opens
│   ├── log.fsx                     frame log + result.json + phase_transitions.jsonl
│   ├── perception.fsx              base centre, enemies-in-base, pickEnemyCommanderPos
│   ├── tactics.fsx                 the main match loop (trainerLoopRun)
│   ├── opening_build.fsx           023 US1: opening-build order + idle-defect detector
│   ├── production_queue.fsx        023 US2: factory queue keeper + FR-008 gate
│   ├── constructor_dispatch.fsx    023 US2: idle-constructor dispatcher + FR-007 telemetry
│   ├── upgrade_gate.fsx            023 US3: entry/exit predicates + FR-012 stall path
│   └── attack_launch.fsx           023 US4: combat classifier + launch commands
├── engine-patches/
│   ├── BARb_AIOptions.lua          patched copy, easy/medium/hard profiles uncommented
│   └── install-barb-profiles.sh    idempotent installer
├── ladder.json                     opponent rungs + fixed map + fixed seed (NullAI=36000 frames, BARb/dev=36000)
├── run.sh                          one-iteration runner (writes run dir, reads BOT_SCRIPT)
├── PLAYBOOK.md                     operator decision tree (READ THIS — §12 for macro)
├── HISTORY.md                      per-iteration ledger
└── README.md                       this file
```

The per-iteration artifacts live under `bots/runs/<timestamp>_<rung>_<iter>/`
and are **gitignored**. They are the operator's working memory, not the
feature's output.

## Helper catalogue

Five extracted helpers land across features 021 (`perception.fsx`
`pickEnemyCommanderPos`) and 023 (the five builder-economy helpers).
Each was extracted under FR-020's two-organic-sites rule. One-line
summary per helper; see `contracts/helpers.md` in the owning feature
spec for the full API and `PLAYBOOK.md §12.3` for the edit map.

- **`helpers/log.fsx`** — `createLogger`, `logStart`, `logFrame`,
  `writeResult`, `writeError`, plus 023's `TrainerPhaseTransitionRecord`
  and `logPhaseTransition` (appends to `phase_transitions.jsonl`;
  absent-by-design for rush-bot runs).
- **`helpers/perception.fsx`** — `pickEnemyCommanderPos` (021 first
  extraction), plus 023's `computeBaseCentre` and `enemiesInBase`
  (2D distance from a base centre, powers the FR-016b defend
  interrupt).
- **`helpers/tactics.fsx`** — `trainerLoopRun` (the main match loop
  consumed by both bots; unchanged from 020).
- **`helpers/opening_build.fsx`** (023 US1) — opening-build order
  helper. `defaultOpening` (Armada 2×mex + 2×solar + 1×lab),
  `resolveOpeningBuildOrder`, `nextOpeningCommand`, `openingComplete`,
  plus `OpeningProgress` and `PositionChooser` types. Consumers advance
  on `UnitFinished` (not `UnitCreated`) so partial structures don't
  get abandoned.
- **`helpers/production_queue.fsx`** (023 US2) — factory queue keeper.
  `defaultArmadaKbotPolicy` (armck + armpw, minDepth=3, target
  constructor ratio 0.4, FR-008 gate at income ≥ 10),
  `resolveQueuePolicy`, `observeFrame`, `computeQueueTopUp`.
- **`helpers/constructor_dispatch.fsx`** (023 US2) — idle-constructor
  dispatcher. `DispatchState`, `findConstructors`, `dispatchIdle`,
  `idleDefectCandidates`, `markDefectReported`. IsIdle is unreliable
  (engine's `UnitIdle` event often doesn't fire for fresh factory
  products), so the helper tracks `Dispatched : Set<int>` explicitly.
- **`helpers/upgrade_gate.fsx`** (023 US3) — upgrade entry/exit
  predicates + FR-012 stall path. `UpgradeThresholds`, `UpgradeGateState`,
  `entryPredicateMet`, `markReached`, `decideUpgradeExit` (returns
  `AttackNow|StallAndLose|WaitLonger` with the "no degenerate rush"
  invariant enforced).
- **`helpers/attack_launch.fsx`** (023 US4) — army composition + attack
  launch. `isCombatDef` (MaxWeaponRange > 0 AND empty BuildOptions —
  excludes commander, constructors, structures), `countCombatUnits`,
  `launchFreshCombat` (issues `MoveCommand` per unit toward target —
  `FightCommand` was rejected in iter 024 because it halts units to
  engage en route), `pickAttackTarget` (prefers the unique-def enemy,
  falls back to a fixed position).

## Success criteria

The feature is "done" when:

- `HISTORY.md` shows at least one `win` on `NullAI` (no-op rung).
- `HISTORY.md` shows at least one `win` on `BARb/dev` (first competitive rung).
- `bots/trainer/helpers/` has ≥3 actively-used helpers.

See `specs/020-bot-iterative-trainer/spec.md` §Success Criteria for the
full list.

## 021-rerun-trainer-highbar integration note

Feature `021-rerun-trainer-highbar` re-ran the iteration loop against the
integrated HighBarV2 `029-fix-trainer-issues` proxy. Summary of the
integration is in the outbound mailbox
`Mailbox/2026-04-12_from_FSBarV1_integration_complete.md`, and a
follow-up probe report is in
`Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md`.

Key outcomes:
- The four feature-020 trainer-side workarounds (`botDeclaredVictory`,
  "No active session" sniffer, `enum_move=42`, real-path `peak_metal: 0`)
  are gone from shipping code.
- Canonical end-of-game flows through `GameEvent.Shutdown` in the frame
  event stream. `FSBar.Client.Protocol.fs` was updated to synthesize a
  terminal frame for the proxy's standalone `Shutdown` envelope, because
  the proxy delivers it out-of-band (not inside a final `Frame` message).
- `bots/trainer/helpers/perception.fsx` gained its first substantive
  helper — `pickEnemyCommanderPos`, used from two organic call sites in
  `bot.fsx`.
- `PLAYBOOK.md` §10 (10-iteration per-rung budget) and §11 (cross-repo
  defect routing) added for future iteration sessions.
- The `NullAI` rung was dropped from the iteration loop in 021 because
  the engine does not declare `Spring.GameOver` for the scenario; its
  MVP contribution (economy fix verification) is still captured in the
  `smoke-021` / `smoke-021b` HISTORY entries.

The 021 spec, plan, tasks, research, data-model, contracts delta, and
quickstart live under `specs/021-rerun-trainer-highbar/`.

## Tactical primitives (024-tactical-map-primitives)

`bot_macro.fsx` now consumes the five new `FSBar.Client` modules shipped
in feature 024: `Pathing`, `Chokepoints`, `BasePlan`, `WallIn`, and
`SmfParser`. Map-dependent analysis (chokepoint detection in particular)
runs **offline** via `scripts/examples/14-cache-map-analysis.fsx` and is
loaded from `bots/trainer/map-cache/<map>.json` at warmup in < 10 ms —
running it live would block the frame-reading path long enough to
trip `Socket not writable, dropping frame` in the engine's infolog and
OOM the Lua VM at 100× headless game speed.

The bot emits `[chokepoint] loaded N chokepoints from cache …`,
`[plan] resolved 5 slots`, `[plan] slot <name> (<def>) resolved @ (x,z)`,
and `[defend] chokepoint pos=(x,z) width=W id=...` traces in addition to
the 023 phase-transition and `[probe-idle]` lines. See
`PLAYBOOK.md §13 "Tactical primitives integration"` for the full trace
reference, the offline cache workflow, and the `BasePlan` extension
procedure.

The 023 helpers (`opening_build`, `production_queue`, `constructor_dispatch`,
`upgrade_gate`, `attack_launch`) remain in-tree and still drive the
main command path. Feature 024 adds the tactical primitives as
observability + defend-interrupt routing, not as a full replacement.
The rush bot (`bot.fsx`) is untouched across 024 — FR-030 preserved,
verified on every 024 commit via `024-rush-smoke*` HISTORY entries.

A companion FSBarV1↔HighBarV2 cross-repo fix landed as part of 024:
`Protocol.sendCallback` in FSBar.Client now implements the
HighBarV2 feature-031 "callback/frame interleaving" contract — mid-game
callbacks no longer drop engine events. See mailboxes
`Mailbox/2026-04-14_to_HighBarV2_mid-game-callback-event-drop.md` and
`Mailbox/2026-04-14_from_HighBarV2_…callback-event-drop-resolved.md`.
