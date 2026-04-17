# Quickstart — Building and Testing After the Cleanup

**Feature**: 034-repo-cleanup
**Phase**: 1 (Design)

This is what a contributor should see after the cleanup lands. If any step fails on the merged branch, the cleanup is not done.

## Prerequisites

- Developer container environment (per `CLAUDE.md`).
- `dotnet --version` ≥ 10.0.
- Beyond All Reason engine under `~/.local/state/Beyond All Reason/engine/recoil_*/` for the live test suite (auto-detected by `EngineDiscovery`).

## Build the whole solution

```bash
dotnet build FSBarV1.slnx
```

Expected: all 8 projects build without warnings newly introduced by this feature (SC-007).

## Run the whole test suite

```bash
dotnet test FSBarV1.slnx
```

Expected: 4 test projects discovered and run; aggregated pass/fail summary (SC-004). This is the single documented top-level test command.

### Narrower test runs

- Unit tests only (skip live engine): `dotnet test tests/FSBar.Client.Tests tests/FSBar.SyntheticData.Tests tests/FSBar.Viz.Tests`
- Live tests only: `dotnet test tests/FSBar.LiveTests` (requires engine present)
- One test project: `dotnet test tests/FSBar.Viz.Tests`

### Legacy wrapper (preserved for backwards compatibility)

```bash
./tests/run-all.sh
```

The wrapper still exists for engine-prerequisite checks and graphical-test orchestration. It invokes `dotnet test` internally against the updated project paths.

## Verify the cleanup guarantees

### 1. Zero `private`/`internal` modifiers in non-generated F#

```bash
rg -n '^\s*(module|let|member|type)\s+(private|internal)\b' src tests \
   --glob '!src/FSBar.Proto/Generated/**' \
   --glob '!*.generated.fs' --glob '!*.generated.fsi'
```

Expected: no output (SC-002).

### 2. No duplicate test-file basenames across projects

```bash
find tests -name '*Tests.fs' -not -path '*/obj/*' -not -path '*/bin/*' -printf '%f\n' | sort | uniq -d
```

Expected: no output (SC-005).

### 3. Solution file lists all 8 projects

```bash
grep -c '<Project Path=' FSBarV1.slnx
```

Expected: 8 (SC-003).

### 4. Baselines byte-stable (for moved files, compare by content)

The pre-cleanup hashes snapshot captured in `contracts/baseline-invariant.md` was committed as `specs/034-repo-cleanup/pre-cleanup-baseline-hashes.txt`. Post-merge:

```bash
find src tests \( -name "*.fsi" -o -name "*.baseline" \) \
  -not -path "*/obj/*" -not -path "*/bin/*" -not -path "*/Generated/*" \
  -exec sha256sum {} + | awk '{print $1}' | sort > /tmp/post-hashes.txt

awk '{print $1}' specs/034-repo-cleanup/pre-cleanup-baseline-hashes.txt | sort > /tmp/pre-hashes.txt

# The only expected new hash is SyntheticMapGrid.fsi + SyntheticMapGrid.baseline.
diff /tmp/pre-hashes.txt /tmp/post-hashes.txt
```

Expected: exactly 2 lines added (corresponding to the new `SyntheticMapGrid` module), zero lines removed or changed.

### 5. Trainer smoke run

```bash
./bots/trainer/run.sh --map "Avalanche 3.4" --smoke
```

Expected: trainer starts, reaches a steady game frame, emits non-empty JSONL frame logs under `bots/runs/<timestamp>/`, exits cleanly (SC-009). Byte-equivalence across runs is not required.

## Where to find things

| Looking for | Location |
|---|---|
| Production source | `src/FSBar.Proto/`, `src/FSBar.Client/`, `src/FSBar.SyntheticData/`, `src/FSBar.Viz/` |
| Unit tests | `tests/FSBar.Client.Tests/`, `tests/FSBar.SyntheticData.Tests/`, `tests/FSBar.Viz.Tests/` |
| Live (engine-dependent) tests | `tests/FSBar.LiveTests/` (files prefixed `Live*`) |
| Shared test helpers | `tests/Common/` (compile-included into consuming projects) |
| Surface baselines | `tests/<project>/Baselines/` |
| Trainer bot | `bots/trainer/` (unchanged) |
| Committed map analysis cache | `bots/trainer/map-cache/` (unchanged) |
| Visualizer style presets | `viz-presets/` (user-local, unchanged) |

## What changed compared to pre-cleanup

If you are landing onto master after the merge of this feature, expect:

- `src/FSBar.Client.Tests/` and `src/FSBar.SyntheticData.Tests/` are gone — their new home is `tests/`.
- `tests/FSBar.LiveTests/ConnectionTests.fs` and three sibling files are renamed with a `Live` prefix.
- `tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs` is gone; baseline checks are now done via the shared helper in `tests/Common/SurfaceAreaHelper.fs`.
- `FSBarV1.slnx` lists every project, not just 3.
- No more `private`/`internal` modifiers in non-generated F#. The `.fsi` files still gate the public surface exactly as before.
- `tests/README.md` is new — read it if you are wondering where to add a new test.
- `CLAUDE.md` has an updated "Project Structure" section reflecting all of the above.

If you were referencing any of the moved or renamed files in a local branch, rebase onto master and update your paths accordingly.
