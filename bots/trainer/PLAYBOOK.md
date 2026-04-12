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
