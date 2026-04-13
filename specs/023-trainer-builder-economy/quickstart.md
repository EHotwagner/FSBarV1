# Quickstart — 023 Builder-Economy Macro Bot

**Branch**: `023-trainer-builder-economy`
**Audience**: the operator (human or AI) driving the trainer loop for this feature.
**Reading order**: read this file first, then `bots/trainer/PLAYBOOK.md §12` for the macro-specific extensions to the existing playbook, then the spec's user stories as you iterate.

This quickstart assumes you are on an already-set-up FSBarV1 workstation with BAR installed under `~/.local/state/Beyond All Reason/` — the same assumptions as features 020/021/022. If any of those assumptions do not hold, see `specs/020-bot-iterative-trainer/quickstart.md` for the one-time setup.

## 1. Verify branch

```bash
git rev-parse --abbrev-ref HEAD    # → 023-trainer-builder-economy
```

If it says anything else, stop. This feature's commit discipline requires every iteration to land on `023-trainer-builder-economy` (FR-020 + inherited 020 FR-025..FR-029).

## 2. Run one macro-bot iteration

The macro bot is `bots/trainer/bot_macro.fsx`. The existing rush bot (`bots/trainer/bot.fsx`) remains runnable on this branch — this feature adds a second bot alongside the existing one (Session 2026-04-13 Q3 clarification, FR-022).

Use the `BOT_SCRIPT` environment variable to tell `run.sh` which bot to launch:

```bash
# Macro bot, no-op rung, iteration 001
BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 001
```

The runner materialises a run directory under `bots/runs/<timestamp>_NullAI_001/` with the usual nine files **plus** (for macro-bot iterations) `phase_transitions.jsonl`. Expected wall-clock on the no-op rung: **2–5 minutes** — macro matches are slower than rush matches because the bot waits for the opening and the upgrade.

```bash
# Rush bot still works too (default BOT_SCRIPT is bot.fsx)
bash bots/trainer/run.sh NullAI 002
```

Running the rush bot on the same rung is an A/B sanity check that the helper-library changes haven't broken `bot.fsx` (FR-022 / FR-023).

## 3. Post-run analysis for macro iterations

Extends the PLAYBOOK §2 post-run analysis with a fourth step that reads the phase-transition log:

```bash
RUN=$(ls -1td bots/runs/* | head -1)
echo "inspecting $RUN"

# 3a. Terminal result
jq . "$RUN/result.json"

# 3b. Human-readable event trail
tail -n 80 "$RUN/stdout.log"

# 3c. Engine native infolog (last page)
tail -n 60 "$RUN/engine.infolog"

# 3d. NEW: phase transitions — the single most useful macro diagnostic
if [[ -f "$RUN/phase_transitions.jsonl" ]]; then
    cat "$RUN/phase_transitions.jsonl" | jq -c .
else
    echo "WARN: no phase_transitions.jsonl — macro bot never emitted a transition?"
fi
```

Read the phase-transition log first. You should see, in order:

1. One or more `Opening → Defending` / `Defending → Opening` pairs (if any enemy probe reached your base).
2. An `Opening → Production` line with `reason="first-factory-finished"`. If this is missing, the bot never finished a factory — diagnose the opening-build helper.
3. A `Production → Upgrade` line with `reason="upgrade-entry-predicate-met"`. If missing, your economy thresholds are too high or your production policy is off.
4. An `Upgrade → Attack` line with `reason="upgrade-reached-normal"` or `upgrade-deadline-fallback`. If missing with `upgrade-stall-no-army` present, FR-012 fired and the bot accepted a stall loss.
5. The match outcome from `result.json.outcome` — ideally `win` for the no-op rung.

Each missing line is a forcing function for a helper extraction or a bot-logic fix.

## 4. Classification extensions for macro iterations

Extends PLAYBOOK §3 with four new root-cause labels for macro-bot iterations:

- **`opening-regression`** — the opening phase never completed (no `first-factory-finished` transition). Edit `bot_macro.fsx` or `opening_build.fsx`. Root cause is almost always (a) an unresolved def name, (b) no reachable metal spots in range, or (c) commander idled due to a missing dispatch.
- **`production-regression`** — factory completed but queue never filled. Edit `production_queue.fsx` or `bot_macro.fsx`. Root cause is almost always an off-by-one in the top-up loop or a bad `MinQueueDepth`.
- **`upgrade-stall`** — reached Upgrade but `decideUpgradeExit` returned `StallAndLose`. Classify the stall cause from the run log: economy too thin (raise thresholds in helper), or combat production too slow (tune queue ratio).
- **`attack-regression`** — reached Attack but no commander-death win. Classify per the spec's FR-016 buckets: insufficient army composition, attack mistimed, pathing failure, upgrade still missing, or out-of-scope.

All four are `bot-logic` or `helper-extraction` under the PLAYBOOK's existing label set; the macro-specific slug above is appended to the HISTORY.md note so stall detection remains meaningful.

## 5. Commit discipline

One iteration, one commit — same rule as 020/021/022. Commit templates specific to this feature:

| Iteration kind                     | Message template                                                  |
|------------------------------------|-------------------------------------------------------------------|
| New macro-bot logic                | `trainer: macro iter <N> — <short description>`                  |
| Helper extraction (FR-021)         | `trainer: extract <helper-name> helper for macro bot`            |
| Rush-bot breakage fix (FR-023)     | `trainer: fix bot.fsx after <helper> interface change`           |
| Macro win on no-op rung            | `trainer: macro rung NullAI cleared on iter <N>`                 |

Every commit on this branch MUST be pushed:

```bash
git push origin 023-trainer-builder-economy
```

Never commit on `master`. No pull requests for iteration commits (inherited from 020 FR-028).

## 6. Feature completion check

The feature is complete (SC-010) when both of the following hold:

1. The most recent macro-bot iteration on the no-op rung has `result.json.outcome = "win"` and `result.json.cause = "commander-death-win-after-upgrade"` (or `...deadline-fallback` — the fallback path still counts), AND its `phase_transitions.jsonl` shows the full `Opening → Production → Upgrade → Attack` sequence.
2. All five helpers from FR-021 exist in `bots/trainer/helpers/` (`opening_build.fsx`, `production_queue.fsx`, `constructor_dispatch.fsx`, `upgrade_gate.fsx`, `attack_launch.fsx`), each is `#load`-ed by `bot_macro.fsx`, each is referenced in PLAYBOOK §12, and `bot.fsx` (the rush bot) still runs to a clean result against the no-op rung.

Wins on the competitive rung (`BARb/dev`) are **bonus** — they satisfy SC-005 but are not required for feature completion.

## 7. Second-operator check (SC-009)

Within one hour, a second operator reading only the updated PLAYBOOK §12 and the helper library should be able to:

1. Describe the four macro phases and the trigger for each transition.
2. Point at the helper module that gates each transition.
3. Write a minimal "alternative macro bot" that reuses at least three of the five new helpers without modifying them.

If you finish the feature and cannot satisfy this check, the PLAYBOOK §12 addition is the gap, not the helpers.

## 8. Halt conditions

All of PLAYBOOK §9's halt conditions apply unchanged. Two additions for this feature:

- **`both-bots-broken`** — an iteration's helper change makes *both* bots fail to produce a conformant run directory. Halt; revert the last commit if necessary (per FR-023). This is the one case where destructive git action is pre-authorised for this feature.
- **`upgrade-stall-loop`** — three consecutive macro iterations all classified `upgrade-stall`. Halt and ask the user whether the upgrade thresholds are realistic for the current map.
