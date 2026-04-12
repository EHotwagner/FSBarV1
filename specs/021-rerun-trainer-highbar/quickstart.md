# Quickstart: Re-run the Trainer Loop with the Integrated HighBar Proxy

**Feature**: `021-rerun-trainer-highbar` | **Date**: 2026-04-12

This is the operator quickstart. It assumes you already walked feature 020 (or have read its `bots/trainer/PLAYBOOK.md`) and know how the iteration loop works. The differences from feature 020 are concentrated in steps 1–4 (integrate the proxy, delete workarounds, smoke-verify) and step 7 (10-iteration budget + cross-repo defect routing).

## Prerequisites

- You are checked out on `021-rerun-trainer-highbar` and ahead of `master`.
- HighBarV2 is cloned at `../HighBarV2` (sibling of `FSBarV1`) on a branch / commit that contains the squash-merged `029-fix-trainer-issues` work.
- Beyond All Reason is installed at `~/.local/state/Beyond All Reason/engine/recoil_<version>` and the BARb difficulty patch from feature 020 is installed (or re-installable via `bots/trainer/engine-patches/install-barb-profiles.sh`).
- You can run `bash`, `dotnet fsi`, `cmake`, `jq`, and `git`.

## Step 1 — Integrate the rebuilt HighBarV2 proxy

```bash
cd ../HighBarV2
git pull
cmake --build build
cmake --install build
cd -
```

`cmake --install build` writes the rebuilt `libSkirmishAI.so` directly into the engine's AI plugin path:

```
~/.local/state/Beyond All Reason/engine/recoil_<version>/AI/Skirmish/HighBarV2/0.1/
```

Verify the file's mtime matches your build:

```bash
stat -c '%y %n' ~/.local/state/Beyond\ All\ Reason/engine/recoil_*/AI/Skirmish/HighBarV2/0.1/libSkirmishAI.so
```

## Step 2 — Restart FSI (if it was running)

If you have an FSI MCP server session open with FSBar.Client DLLs loaded, **restart it now** (use the `restart_fsi` MCP tool, or stop and re-launch the server). FSI locks DLLs through `#r`, and the proxy is loaded by the engine on session start — but any FSBarV1-side wrappers cached in FSI will keep stale assumptions about callback semantics.

```
restart_fsi
```

## Step 3 — Smoke-verify the integration (US1)

Run one iteration against NullAI with iter id `smoke-021`:

```bash
bash bots/trainer/run.sh NullAI smoke-021
```

Then inspect the run directory `bots/runs/<timestamp>_NullAI_smoke-021/`:

| Check | What to look for | If it fails |
|---|---|---|
| `result.json` exists | written by `tactics.fsx` (not the stub fallback) | bot crashed before writing — read `stdout.log` |
| `result.json` `telemetry.peak_metal` is non-zero or `null` | non-zero = real economy values flowing; `null` = NaN sentinel (acceptable but unusual for valid resource ids 0/1) | proxy fix did not take effect — re-check Step 1 mtime |
| `result.json` `victory_signal` = `"engine-shutdown-gameover"` | proxy delivered Shutdown(GAME_OVER) | proxy fix did not deliver event — file an inbound mailbox to HighBarV2 (FR-021) |
| `engine.infolog` is much smaller than 020's comparable smoke | default-off `verbose_commands` working | proxy was rebuilt from a stale tree — `git status` in HighBarV2 |
| `unwired_commands.json` exists with `rc_minus_2_count` integer | `run.sh` post-match step working | task to wire the post-match grep is incomplete |

If all five checks pass, the integration is verified. Commit any incidental changes with the message `chore: smoke-021 verifies integrated proxy` and push.

## Step 4 — Delete the trainer-side workarounds (US2)

Each removal is its own commit, pushed immediately. The order is important — start with the workaround that depends on the canonical Shutdown event being live, since you just verified it in Step 3:

1. **Remove `botDeclaredVictory`** from `bots/trainer/helpers/tactics.fsx`. Search for the identifier (it appears around lines 95, 175, 183–185, 234, 260, and the synthetic-victory branch on lines 260–276) and delete it together with the synthetic-victory branch in the result-classification block. Victory now flows exclusively from `shutdownSeen && commanderAlive` (line 277, already present and correct).
   ```bash
   git commit -am "fix(trainer): remove botDeclaredVictory shim — Shutdown(GAME_OVER) is now canonical"
   git push
   ```

2. **Remove the "No active session" sniffer** from `tactics.fsx` (around line 208). The exception sniffer was inferring end-of-game from a closed socket; the proxy now closes via Shutdown event. Replace the sniffer branch with a simple re-raise so any actual exception fails loudly.
   ```bash
   git commit -am "fix(trainer): remove No-active-session exception sniffer — proxy delivers Shutdown event"
   git push
   ```

3. **Search for any `enum_move=42` constant** anywhere in the trainer or in `bot.fsx`/helpers and remove it. (At time of planning there is no such constant in `bots/trainer/`; this is a verification commit only if the search is empty.)
   ```bash
   grep -rn 'enum_move=42\|enumMove=42\|Command_Move = 42' bots/trainer src/FSBar.Client
   # If anything is found, delete it. Otherwise, no commit.
   ```

4. **Confirm `peak_metal: 0` / `peak_energy: 0` literals exist only in stub-fallback paths** (research.md Decision 7). If a real-path zero placeholder exists, remove it; otherwise no commit.
   ```bash
   grep -n 'peak_metal\s*[:=]\s*0\|peak_energy\s*[:=]\s*0' bots/trainer/run.sh bots/trainer/helpers/*.fsx
   ```

5. **Re-run the smoke iteration** after the deletions to verify nothing regressed:
   ```bash
   bash bots/trainer/run.sh NullAI smoke-021-postclean
   ```
   Apply the same five checks from Step 3. If `victory_signal` is still `"engine-shutdown-gameover"` and the bot still wins, US2 is done.

## Step 5 — File the integration outbound mailbox (FR-019)

Once US1 and US2 are green, write `Mailbox/2026-04-XX_from_FSBarV1_integration_complete.md` summarising:
- the inbound mailbox you are responding to,
- the smoke iteration directories that demonstrate the canonical wire signals,
- the four removal commits,
- the date of the rebuilt proxy.

## Step 6 — Walk the iteration loop (US3)

Open `bots/trainer/PLAYBOOK.md` and start at §1 for the NullAI rung. The loop is identical to feature 020 except:

- **Stall checker** now treats `peak_metal=null` / `peak_energy=null` as "skip this field" per `improvedOverPrior` (data-model.md). The stall counter resets only on a real improvement.
- **Per-rung iteration cap**: after each iteration, count the lines in `HISTORY.md` for the current rung *within this feature's branch*. If the count reaches 10 without a win, see Step 7.
- **Helper extraction substance bar**: SC-006 requires *at least one* extracted helper beyond `log.fsx`, motivated by duplication across two iterations *and* used from at least two distinct call sites in the bot at feature completion. Watch for the second occurrence of any ad-hoc perception/tactics snippet — the second time you write it, that's the trigger to extract.
- **Wire the Issue 1 probe** (US4) in exactly one iteration on the NullAI rung when an opportunity comes up — see Step 8.

When you clear the NullAI rung, advance to BARb/dev and repeat. SC-005 wants both rungs cleared in at most 10 iterations each.

## Step 7 — If a rung hits the 10-iteration budget (FR-016a)

Hard halt. Write `Mailbox/2026-04-XX_from_FSBarV1_budget_exhausted_<rung>.md` with:
- rung name,
- the 10 iteration ids and outcomes,
- the telemetry trend across them,
- a hypothesis for why the rung is unwinnable in this configuration.

Mark this as an SC-005 failure in `HISTORY.md` (suffix the last line with `[budget-exhausted]`) and do not start an 11th iteration without an explicit operator decision.

## Step 8 — Run the Issue 1 probe once (US4)

On any NullAI iteration where the bot is going to send an `AttackCommand` against a known live unit, instrument the bot to:
1. Capture `client.GameState.Units.[issuingUnitId].Pos` immediately before `client.SendCommands [AttackCommand …]`.
2. `client.WaitFrames 30` (one game-second) without sending commands.
3. Capture `client.GameState.Units.[issuingUnitId].Pos` again, or note that the unit is no longer in the map (= destroyed).
4. Compute Euclidean distance; classify `moved` (>5.0), `stationary`, or `destroyed`.
5. Write `<run_dir>/attack_probe.json` per the contracts delta schema before the bot exits.

After the iteration finishes, add a `HISTORY.md` note like `Issue 1 probe: outcome=moved|stationary|destroyed` (SC-008) so the upstream maintainer can find it.

If the result is `stationary` for two iterations in a row, file an inbound mailbox to HighBarV2 with the probe data (FR-018).

## Step 9 — If a new HighBarV2 proxy defect surfaces (FR-021)

Halt the loop. Write `Mailbox/2026-04-XX_to_HighBarV2_<short-symptom>.md` with the iteration directory, the symptom, the relevant frame log excerpt, and the relevant `engine.infolog` excerpt. Wait for an operator decision before resuming. **Do not edit HighBarV2 source from inside this feature** — Q1 of the spec clarification chose Option A (hard out-of-scope).

## Step 10 — Feature completion checklist

Before marking the feature done, verify:

- [ ] SC-001 — smoke iteration on NullAI shows non-zero (or `null`) `peak_metal`/`peak_energy`.
- [ ] SC-002 — same smoke iteration's `victory_signal=engine-shutdown-gameover`.
- [ ] SC-003 — `git grep -n 'botDeclaredVictory\|"No active session"\|enum_move=42\|peak_metal: 0' bots/trainer src/FSBar.Client` returns only the stub-fallback hits in `run.sh` (Decision 7).
- [ ] SC-004 — engine.infolog file size for a 021 iteration is at least 80% smaller than a comparable 020 iteration on the same rung and frame budget.
- [ ] SC-005 — at least one `win` on NullAI and one on BARb/dev, both with `victory_signal=engine-shutdown-gameover`, both within 10 iterations.
- [ ] SC-006 — `bots/trainer/helpers/perception.fsx` or `tactics.fsx` contains real extracted code (not a stub), motivated by 2-iteration duplication, called from 2+ distinct call sites in `bot.fsx`.
- [ ] SC-007 — every iteration line in `HISTORY.md` corresponds to a pushed commit on `021-rerun-trainer-highbar`.
- [ ] SC-008 — at least one iteration in this feature has an `attack_probe.json` referenced from `HISTORY.md` or an outbound mailbox.
- [ ] SC-009 — the integration outbound mailbox from Step 5 exists.
- [ ] SC-010 — no `win` outcome in `HISTORY.md` was recorded *before* the corresponding workaround was removed in Step 4.

When all ten boxes are checked, the feature is complete on the branch. Per FR-028 of feature 020 (inherited), there is **no PR** — the branch state on the remote is the deliverable.
