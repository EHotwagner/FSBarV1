# Contract: Baseline & `.fsi` Invariance Across the Cleanup

**Feature**: 034-repo-cleanup
**Phase**: 1 (Design)

This is the primary external contract for the cleanup. It makes constitution §II compliance (compiler-enforced structural contracts + surface-area baselines) an explicit acceptance gate.

## Contract statement

**For every module that exists both before and after the cleanup, the contents of its `.fsi` signature file and its `.baseline` file MUST be byte-identical before and after.**

The only permitted exceptions are:

1. **New module**: `FSBar.SyntheticData.SyntheticMapGrid` — a new `.fsi` and new baseline are added; nothing to compare against pre-cleanup.
2. **Deliberate surface change**: if the cleanup discovers a symbol whose privacy was the only thing gating it (e.g. `module private Foo` without an `.fsi` — rare), the surface change MUST be called out explicitly in the PR description and the baseline re-committed in the same commit that changes the `.fsi`.

No other `.fsi` or `.baseline` diffs are permitted in the merge that closes this feature.

## Permitted baseline deltas

The following baseline changes are pre-approved for this feature. Any delta NOT in this list fails the contract.

1. **New module**: `tests/FSBar.SyntheticData.Tests/Baselines/SyntheticMapGrid.baseline` — first-commit of the baseline for the lifted `FSBar.SyntheticData.SyntheticMapGrid` module.
2. **Path rename of 21 Client baselines**: `src/FSBar.Client.Tests/Baselines/*.baseline` → `tests/FSBar.Client.Tests/Baselines/*.baseline`. Content (sha256) MUST be identical; only the path changes.
3. **Pre-existing stale Viz baselines regenerated** (US1 side-effect — documented below).
4. **`.fsi` private/internal removals from T036b** — enumerated per-file in the table below, populated by T036a's audit before T036b runs. Each entry lists the `.fsi` file, the symbol(s) becoming public, and the expected `.baseline` file that will be regenerated.

### (3) Pre-existing stale Viz baselines regenerated (US1 side-effect)

Before the cleanup, `tests/FSBar.Viz.Tests/` used a reflection-based smoke test (`SurfaceBaselineTests.fs`) rather than the compare-to-baseline style used by `FSBar.Client.Tests`. The committed `Baselines/*.baseline` files were never validated against their `.fsi` sources, so 8 of them drifted over time.

US1 consolidates every test project onto the compare-to-baseline helper (`tests/Common/SurfaceAreaHelper.fs`). That surfaced the drift. Since the `.fsi` files are the authoritative public-surface gate per constitution §II, the baselines are regenerated from their `.fsi` sources. No `.fsi` was edited — only the pre-existing stale baselines are brought in sync.

The 8 regenerated files:

| Baseline file | Drift observed |
|---|---|
| `tests/FSBar.Viz.Tests/Baselines/GameViz.baseline` | XML docs / parameter ordering only |
| `tests/FSBar.Viz.Tests/Baselines/LiveSession.baseline` | XML docs + `System.IDisposable` short form |
| `tests/FSBar.Viz.Tests/Baselines/MapData.baseline` | XML doc lines removed |
| `tests/FSBar.Viz.Tests/Baselines/MockSnapshot.baseline` | XML doc lines removed |
| `tests/FSBar.Viz.Tests/Baselines/SceneBuilder.baseline` | Minor XML doc reflow |
| `tests/FSBar.Viz.Tests/Baselines/UnitLabels.generated.baseline` | Label table additions (legitimate BarData drift) |
| `tests/FSBar.Viz.Tests/Baselines/UnitLabelsGenerator.baseline` | XML doc + signature reflow |
| `tests/FSBar.Viz.Tests/Baselines/VizTypes.baseline` | XML doc additions |

No public symbol was removed or renamed in any `.fsi`; every baseline update is a doc/formatting sync. Post-cleanup, all Viz baselines are byte-equal to their `.fsi`.


<!-- T036b populates this table before T037 runs. Reject any baseline diff whose file is not listed here. -->

| .fsi file | Symbol(s) exposed | Baseline regenerated |
|---|---|---|
| _to be populated by T036a_ | | |

## Why this contract exists

The user's spec clarification (Q2, 2026-04-17) mandates hard-zero `private`/`internal` modifiers in non-generated F#. The constitution mandates `.fsi`-gated public surface with committed baselines. Those two rules are only compatible if the `.fsi` is understood as the authoritative public-surface gate — the `private` keyword in the `.fs` file is redundant once the `.fsi` is in place.

This contract encodes that compatibility: remove the redundant keyword, do NOT change the surface it was redundantly hiding.

## How it is verified

### Pre-cleanup step (baseline snapshot)

Before any code changes land, capture the current `.fsi` and `.baseline` hashes:

```bash
find src tests \( -name "*.fsi" -o -name "*.baseline" \) -not -path "*/obj/*" -not -path "*/bin/*" -not -path "*/Generated/*" \
  | sort | xargs sha256sum > /tmp/034-pre-cleanup-hashes.txt
```

### Post-cleanup step (diff check)

After the cleanup:

```bash
find src tests \( -name "*.fsi" -o -name "*.baseline" \) -not -path "*/obj/*" -not -path "*/bin/*" -not -path "*/Generated/*" \
  | sort | xargs sha256sum > /tmp/034-post-cleanup-hashes.txt

diff /tmp/034-pre-cleanup-hashes.txt /tmp/034-post-cleanup-hashes.txt
```

**Expected diff**: only the lines corresponding to:

- Moved `.fsi` files (path change: `src/FSBar.Client.Tests/Baselines/*` → `tests/FSBar.Client.Tests/Baselines/*`).
- The new `SyntheticMapGrid.fsi` + its new baseline.
- Any deliberately-flagged surface change (should be zero; otherwise PR description must enumerate).

**Path-rename lines**: hash must match — only the filename column changes. If the hash for a moved file differs, the file was edited during the move and that is a contract violation.

## Enforcement point in the tasks phase

The tasks phase (`/speckit.tasks`) MUST produce:

1. A task that captures the pre-cleanup hash list as the first step.
2. A final verification task that re-runs the hash capture and diffs it against the pre-cleanup snapshot.
3. Any delta beyond path-rename + the one new module fails the feature.

## Relationship to other acceptance criteria

- **FR-012** (baselines regenerated on legitimate surface change) — this contract makes "no incidental baseline changes" the default.
- **SC-007** (no new build warnings) — is satisfied only if the cleanup does not force `.fsi` edits to suppress new warnings introduced by removing `private`.
- **SC-008** (full test suite passes) — surface-area tests in `FSBar.Client.Tests` and `FSBar.Viz.Tests` will fail loudly if a baseline drift escapes the hash check.

## What this contract is NOT

- It is NOT a contract over `.fs` file contents — those change substantially (keyword removal, idiomatic-style pass on cold modules, file moves).
- It is NOT a contract over `.fsproj` files — those change (project moves, solution-file inclusion, Compile-order updates for renamed files).
- It is NOT a contract over test runtime behavior beyond pass/fail — test renames and helper lifting may change test names/ids.
