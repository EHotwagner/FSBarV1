# Quickstart — Macro Bot Primitive-Driven Command Path

**Feature**: 025-macro-primitive-driven
**Audience**: operator running the feature through its first live iteration
**Pre-requisites**: feature 024 (`024-tactical-map-primitives`) merged; BAR install with Avalanche 3.4; `dotnet-10` + `dotnet fsi` on PATH; the sibling `HighBarV2` repo built with the post-031 callback-frame-interleaving fix (documented in CLAUDE.md §Upstream dependency workflow).

This quickstart walks the operator from a fresh checkout of the 025 branch through the first live iteration on NullAI and the trace-level checks that confirm each user story is live.

---

## 0. Sanity check — rush bot still wins

The feature's FR-019 invariant is that the rush bot `bot.fsx` remains runnable at every commit. Run this **before** building anything, to confirm the 024-merged baseline is healthy:

```bash
cd ~/projects/FSBarV1
bash bots/trainer/run.sh NullAI 025-baseline-rush
```

Expected: `bots/runs/025-baseline-rush-*/result.json` contains `"outcome": "win"` with a `cause` string mentioning engine shutdown. If this fails, stop — the 024 merge is broken and feature 025 cannot start.

---

## 1. Build the Tier 1 plumbing (Commands queued variant)

The queued `MoveCommand` variant is a public API surface change on `FSBar.Client.Commands`. Build it first, run its unit test, and refresh the surface-area baseline. This is the first commit on the 025 branch.

```bash
cd ~/projects/FSBarV1
dotnet build src/FSBar.Client/FSBar.Client.fsproj
dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj --filter "FullyQualifiedName~CommandsTests"
```

Expected: the two new `Commands_MoveCommandQueued_*` tests pass (asserting `Options = 40u` on the queued variant and `Options = 8u` on the regular variant). The existing `MoveCommand_returns_valid_command` test also still passes.

If the surface-area baseline test fails because it sees a new symbol:

```bash
# Refresh the baseline per the mechanism documented in tests/FSBar.Client.Tests/SurfaceAreaTests.fs
# (typically an environment variable gate, see the 024 surface-area baseline task for the exact invocation)
UPDATE_BASELINES=1 dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj --filter "FullyQualifiedName~SurfaceArea"
```

Commit: `feat(Commands): add MoveCommandQueued for waypoint traversal` + baseline update + unit test. Rush bot (`bot.fsx`) is unaffected; run `bash bots/trainer/run.sh NullAI 025-iter1-rush` to prove it.

---

## 2. Bake the extended map cache for Avalanche 3.4

Feature 025 loads a real `MapGrid` at warmup from an extended `bots/trainer/map-cache/avalanche_3.4.json`. The extension adds a `mapGrid` block (base64-gzipped float32 arrays). Run the updated cache writer:

```bash
cd ~/projects/FSBarV1
dotnet fsi scripts/examples/14-cache-map-analysis.fsx 'Avalanche 3.4'
```

Expected stdout:

```
[cache] parsing /home/developer/.local/state/Beyond All Reason/maps/avalanche_3.4.sd7
[cache] SmfParser parsed 512x512 heightmap, min=... max=...
[cache] findChokepoints returned N chokepoints
[cache] serialising mapGrid block (heightMap + slopeMap + resourceMap, base64+gzip)
[cache] wrote bots/trainer/map-cache/avalanche_3.4.json (~500 KB)
```

Verify the file:

```bash
python3 -c 'import json; d = json.load(open("bots/trainer/map-cache/avalanche_3.4.json")); print("has mapGrid:", "mapGrid" in d, "schemaVersion:", d.get("mapGrid",{}).get("schemaVersion"))'
```

Expected: `has mapGrid: True schemaVersion: 1`.

Add the cache file to `.gitignore` and commit the writer-script extension separately:

```bash
echo 'bots/trainer/map-cache/*.json' >> .gitignore
git add .gitignore scripts/examples/14-cache-map-analysis.fsx
git rm --cached bots/trainer/map-cache/avalanche_3.4.json 2>/dev/null || true
git commit -m "feat(cache): extend map-cache schema with MapGrid blob (025)"
```

Rush bot still unaffected — run `bash bots/trainer/run.sh NullAI 025-iter2-rush` to prove.

---

## 3. Integrate `bot_macro.fsx` with the primitives (the atomic commit)

This is the US1+US2+US3+US4 integration commit. Per the spec framing, US5 is the invariant; the four substantive USes land together. Follow the edit sequence in [contracts/bot-macro-integration.md](./contracts/bot-macro-integration.md).

1. Add the `MapGridCache_loadFromJson` local helper + the `MapTargetSet.contains` helper to `bot_macro.fsx`.
2. Replace the synthetic `planMapGrid` skeleton with the real-`MapGrid` load (US4).
3. Add module-mutable `attackPathCache` and `planProgress`; declare the `AttackPathCache` record.
4. Rewrite the Opening-phase command-emission path to consume `BasePlan.resolvePlan` (US1).
5. Rewrite the Attack-phase command-emission path to consume `Pathing.findPath` + cache (US2).
6. Add `Attack_launch.isCombatDef` filter to the defend-interrupt path (US3).
7. Keep `helpers/opening_build.fsx` consumption ONLY on the FR-005 exception-fallback path.

Commit: `feat(bot_macro): drive commands from BasePlan + Pathing primitives (025 US1-US4)`.

---

## 4. First live iteration — macro smoke

```bash
cd ~/projects/FSBarV1
BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 025-iter3-macro
```

Expected outcome (FR-018 / SC-001):

```bash
cat bots/runs/025-iter3-macro-*/result.json | python3 -m json.tool
```

Look for:

```json
{
  "outcome": "win",
  "cause": "commander-death-win-after-upgrade",
  "victory_signal": "engine-shutdown-gameover",
  ...
}
```

Then check each user story's trace signature in `stdout.log`:

### US1 — BasePlan drives opening (SC-002)

```bash
grep -E '\[plan\] (issuing BuildCommand|resolved|slot)' bots/runs/025-iter3-macro-*/stdout.log | head -20
```

Expected: at least one `[plan] issuing BuildCommand <def> @ (x,z) from resolvePlan` per slot in `defaultArmadaOpening` (5 slots in the default plan → 5+ lines). Confirm **absence** of the 023 helper's signature:

```bash
grep -c '\[opening\] idx=' bots/runs/025-iter3-macro-*/stdout.log
```

Expected: `0`. If non-zero, FR-005's exception fallback fired — investigate the exception trace with `grep '\[plan\] resolvePlan exception'` and fix.

### US2 — Pathing drives attack routing (SC-003)

```bash
grep '\[attack\] path waypoints' bots/runs/025-iter3-macro-*/stdout.log
```

Expected: one line per attack launch, e.g. `[attack] path waypoints=3 cost=5824.5 status=Complete`. For Avalanche 3.4 Player-1 start, waypoints typically = 3.

Verify the `MoveCommand` volume in the unwired commands log:

```bash
jq '.[] | select(.type == "MoveCommand")' bots/runs/025-iter3-macro-*/unwired_commands.json | grep -c '"type": "MoveCommand"'
```

Expected: ≥ `(combat_units × waypoints)` MoveCommand entries in the attack-launch frame range. For 12 combat units × 3 waypoints = 36 MoveCommands minimum.

### US3 — Defend filter (FR-012)

The NullAI smoke does not usually fire the defend interrupt because NullAI never raids the base. So you will NOT see a `[defend] routing combat units only` line in a clean NullAI run. This is expected — FR-012 is verified structurally (the code path exists and filters correctly) rather than by live fire on this rung. For a BARb rung once 025 is live, a dedicated defend-fire test will exercise it.

If the `[defend] routing` line **does** fire on a NullAI run, make sure it reports a count consistent with the combat-unit classifier — e.g., `[defend] routing combat units only n=<N>` where N matches `grep -c '[attack] combat launch includes' stdout.log` minus any casualties.

### US4 — Warmup with real MapGrid (SC-005)

```bash
grep -c 'Socket not writable, dropping frame' bots/runs/025-iter3-macro-*/engine.infolog
```

Expected: `0`. Any non-zero count is a FR-017 regression.

```bash
grep -E 'BarClient connected|entering main frame loop' bots/runs/025-iter3-macro-*/stdout.log
```

Compare the `[frame=N]` values between the two lines: the delta MUST be ≤ 1000 game frames (FR-016).

### US5 — Rush smoke unaffected (SC-004)

In a separate run:

```bash
bash bots/trainer/run.sh NullAI 025-iter3-rush
python3 -c "import json; d=json.load(open('bots/runs/025-iter3-rush-*/result.json')); print(d['outcome'], d.get('frames','?'))" 2>/dev/null
```

Expected: `win <≈12500>` — within 5% of the 024 rush baseline of 12390 frames.

---

## 5. If iteration 1 fails: 023 PLAYBOOK §2c discipline

Per SC-007, the iteration budget is **3**. If iteration 1 does not produce `cause = "commander-death-win-after-upgrade"`:

1. Collect the failure signature from `stdout.log` + `engine.infolog`.
2. Make **one** targeted fix — smallest diff that could plausibly address the observed failure mode.
3. Commit with `fix(bot_macro): <one-line explanation>`.
4. Re-run `bash bots/trainer/run.sh NullAI 025-iter4-macro` and check again.
5. Repeat at most twice more (iter 4, iter 5 total).

If iter 5 still fails, halt and file a mailbox under `Mailbox/2026-04-XX_budget-exhaustion-025.md` per 023 PLAYBOOK §10. Do NOT merge 025 into master in that state.

---

## 6. Verification checklist for feature-end

- [ ] `Commands.fsi` has the new `MoveCommandQueued` signature.
- [ ] `Commands.fs` has the `SHIFT_KEY` literal and the new function builder.
- [ ] `tests/FSBar.Client.Tests/Baselines/*` baseline refreshed to include `MoveCommandQueued`.
- [ ] `CommandsTests.fs` has two new tests (queued sets bits; regular does NOT set SHIFT_KEY).
- [ ] `scripts/examples/14-cache-map-analysis.fsx` emits the `mapGrid` block.
- [ ] `bots/trainer/map-cache/*.json` is in `.gitignore`.
- [ ] `bot_macro.fsx` loads real `MapGrid` at warmup, runs `resolvePlan` per tactics tick in Opening, uses `findPath` + cache for attack, filters defend to combat units.
- [ ] `helpers/opening_build.fsx` still compiles and is consumed only on exception fallback.
- [ ] `bot.fsx` rush bot still wins cleanly at every commit on the 025 branch.
- [ ] SC-001..SC-005 all green on a single iteration's artifacts.
- [ ] `fsdoc` agent run on `FSBar.Client.Commands` for the public-API surface change (Workflow gate 7).

Once all boxes checked, merge 025 via `/speckit-mergeBranches`.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `[warmup] no MapGrid in cache` at startup | cache file missing or old-schema | Re-run `14-cache-map-analysis.fsx` per step 2 |
| `[plan] resolvePlan exception — falling back to 023 helper` | exception inside `BasePlan.resolvePlan` | Check the exception message; most likely dimension mismatch in the loaded grid — re-bake the cache |
| `Socket not writable, dropping frame` during warmup | warmup CPU budget blown (FR-015) | Profile the `Stopwatch` around the US5 block; if MapGridCache.loadFromJson > 50 ms, investigate gzip decompress cost |
| 36+ MoveCommand entries but units still take direct path | queued variant not sending SHIFT bit | Verify `Commands.MoveCommandQueued` is the variant being called; `CommandsTests.fs` should have caught this |
| Rush bot fails mid-iteration | 025 branch broke something shared | Run `git bisect` between the last known-good rush commit and HEAD on the 025 branch |
| Opening phase never transitions to Production | `markConsumed` not firing on `UnitFinished` | Check the event handler change in step 3; verify `Slot.DefName` matches the `UnitFinished` event's def name string exactly |
