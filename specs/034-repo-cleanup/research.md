# Phase 0 Research — Repository Cleanup and Test Consolidation

**Feature**: 034-repo-cleanup
**Date**: 2026-04-17

This document resolves unknowns from the spec and records the decisions that shape the plan. It is the foundation for Phase 1 design (data-model, contracts, quickstart).

## R1. Duplicate code groups — the merge list

### R1.1 Surface-area tests: three implementations collapse to one

| File | Lines | Approach |
|---|---|---|
| `src/FSBar.Client.Tests/SurfaceAreaTests.fs` | 138 | Compares FSI signatures against committed `.baseline` files; mature update-mode + orphan detection. |
| `src/FSBar.SyntheticData.Tests/SurfaceAreaTests.fs` | 8 | Placeholder single passing test ("Phase 6"). |
| `tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs` | 367 | Reflection-based smoke tests; does NOT compare baselines. |

**Decision**: `FSBar.Client.Tests/SurfaceAreaTests.fs` is the canonical implementation. The reflection-style assertions in `FSBar.Viz.Tests/SurfaceBaselineTests.fs` are parallel machinery for the same goal (compile-time public surface) and can be replaced by the baseline-compare style once Viz baselines are in place (they already are — 14 Viz baselines committed under `tests/FSBar.Viz.Tests/Baselines/`).

**Action**: Lift the canonical compare-to-baseline logic into a shared helper (`tests/Common/SurfaceAreaHelper.fs` or similar) that both Client and Viz test projects reference. Delete `tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs` and the `SyntheticData.Tests` placeholder, replacing them with thin wrappers that call the shared helper with project-local baseline directories.

**Rationale**: The user's directive is "no copy-paste fixtures" (FR-006). Today there are literally three SurfaceArea test files and two divergent approaches. One shared helper + one `[<Fact>]` per project matches the target.

### R1.2 Engine-fixture scaffolding: stays separated — not duplicate

- `tests/FSBar.LiveTests/EngineFixture.fs` — real engine launch + warmup (30 frames).
- `tests/FSBar.Viz.Tests/VizEngineFixture.fs` — synthetic MapGrid + GameSnapshot builder; no engine.

**Decision**: These are complementary, not duplicate. Keep both. The Live fixture exercises the real socket; the Viz fixture exercises synthetic data paths. Document this split in `tests/README.md` (new, minimal) so future contributors don't merge them by mistake.

### R1.3 Synthetic MapGrid builders: collapse into `FSBar.SyntheticData`

- `src/FSBar.Client.Tests/SyntheticMapGrid.fs` — parameterized test builder (~60 lines).
- `tests/FSBar.Viz.Tests/VizEngineFixture.fs:testMapGrid` — minimal inline builder (~20 lines).

**Decision**: Lift synthetic MapGrid construction into the `FSBar.SyntheticData` library (where synthetic game data already lives — `UnitSim`, `EconomySim`, `Scenes`). Expose `FSBar.SyntheticData.SyntheticMapGrid.build : params -> MapGrid`. Both test projects reference it; the inline `testMapGrid` in Viz is deleted.

**Rationale**: `FSBar.SyntheticData` is the declared home for synthetic data (per feature 018). A synthetic MapGrid belongs there. FR-006 mandates shared fixtures live in one location.

### R1.4 Same-basename test files across Client/Live test projects

| Basename | Client (unit) | Live (integration) | Verdict |
|---|---|---|---|
| `ConnectionTests.fs` | mocks socket | real engine handshake | **Complementary**. Rename Live → `LiveConnectionTests.fs`. |
| `CommandsTests.fs` / `CommandTests.fs` | unit | live | **Complementary**. Normalize naming: `CommandsTests` (unit), `LiveCommandsTests` (live). |
| `EventsTests.fs` / `EventTests.fs` | unit | live | **Complementary**. Normalize: `EventsTests`, `LiveEventsTests`. |
| `MapQueryTests.fs` | unit against synthetic MapGrid | integration against SMF parse | **Complementary**. Rename Live → `LiveMapQueryTests.fs`. |

**Decision**: Rename the `tests/FSBar.LiveTests/` counterparts with a `Live` prefix so basenames are globally unique across the whole test tree. After the rename, SC-005 ("zero duplicated test-file basenames") is satisfied by simple path listing.

### R1.5 Coordinate-conversion helpers: already centralized, no action

`MapQuery.elmoToGrid` / `MapQuery.gridToElmo` are the canonical converters. Inline `/ 8` / `* 8` in tests are acceptable local constants in their test context. `SceneBuilder.mapX` / `SceneBuilder.mapZ` are per-frame hot-path inlines — leaving them alone (FR-009).

### R1.6 MapData binary serializer: unique, no action

Single implementation in `src/FSBar.Viz/MapData.fs`. No duplication.

## R2. Hot-path inventory (what the style pass must NOT touch)

Per FR-009, these paths keep their current mutable/imperative style. The style pass skips them entirely.

### R2.1 Per-frame render path (Viz)

- `FSBar.Viz.GameViz.onFrame` + `onFrameWithState` — frame event-dispatch loop and atomic snapshot swap.
- `FSBar.Viz.SceneBuilder.buildScene` + `updatePulsePhase` — scene graph construction per frame.
- `FSBar.Viz.LayerRenderer.renderLayer` + `renderFloatArray` — pixel-loop bitmap rendering; allocates `pixels[]` and fills via double nested loop.

### R2.2 Per-tick game loop (Client)

- `FSBar.Client.BarClient.startFrameThread` body (lines ~83–121) — `receive → processFrame → notify`.
- `FSBar.Client.GameState.processFrame` — per-frame game state update.
- `FSBar.Client.MapGrid.refreshLos` / `refreshRadar` — per-frame cached LOS/Radar sync.

### R2.3 Per-frame indicator list (flagged risk, but untouched by this feature)

`FSBar.Viz.GameViz.indicators` uses a persistent F# list (cons + filter) per frame. The research notes this may be a performance hotspot worth replacing with `ResizeArray` — but that is a separate optimization, **out of scope** for this cleanup. Leave it alone.

### R2.4 Decision rule for borderline cases

When a helper is called from a hot path but is itself stateless (e.g. `elmoToGrid`), the style pass MAY apply idiomatic pipelines as long as no allocation or branching changes result. When in doubt, leave it alone.

## R3. `private` / `internal` removal impact

### R3.1 Current count (from research agent)

- `/src` non-generated, non-test: ~313 `private`, ~13 `internal` across 48/8 files.
- `/tests`: ~43 `private`, 1 `internal` across 13 files.
- Grand total: ~482 uses across non-generated F#.

### R3.2 Will baselines change?

**No — not if `.fsi` files are left alone.** The `.fsi` is the authoritative public surface (constitution §II, "Any symbol omitted from the `.fsi` file becomes module-private by design"). `let private foo = …` inside a `.fs` file affects **assembly-internal** visibility, not the `.fsi`-gated public surface. Removing `private` keywords from `.fs` files makes symbols visible *within the assembly* but not *to consumers*.

**Decision**: The style pass removes `private`/`internal` from `.fs` and `.fsi` files but does NOT edit the set of names published in `.fsi` files. Baselines should therefore remain byte-identical after the style pass. If a baseline does change, treat it as a bug — either the `.fsi` was accidentally edited, or the private keyword was on a `module private Foo` declaration that was the only thing gating a symbol (rare; known cases will be identified during implementation).

### R3.3 Hard exclusions

- `src/FSBar.Proto/Generated/**` — regenerated by proto tooling.
- `src/FSBar.Viz/UnitLabels.generated.fs` + `.fsi` — regenerated by `gen-unit-labels.fsx`.

Any `private`/`internal` inside those paths stays. A grep used for acceptance (SC-002) excludes these paths.

## R4. Test-suite consolidation plan

### R4.1 Target layout

All test projects under top-level `tests/`:

```
tests/
├── Common/                          (new — shared helpers)
│   └── SurfaceAreaHelper.fs
├── FSBar.Client.Tests/              (moved from src/)
├── FSBar.SyntheticData.Tests/       (moved from src/)
├── FSBar.LiveTests/                 (unchanged path)
├── FSBar.Viz.Tests/                 (unchanged path)
├── engine-version.json              (unchanged)
├── ENGINE-VERSION.md                (unchanged)
├── run-all.sh                       (updated paths)
└── README.md                        (new — test taxonomy)
```

### R4.2 Why `tests/Common/` instead of a shared test .fsproj

Two options were considered:

1. A third-party `FSBar.TestCommon` project that all test projects reference.
2. A `tests/Common/` directory containing loose `.fs` files that each test `.fsproj` compiles via `<Compile Include="..\Common\SurfaceAreaHelper.fs" />`.

**Decision**: Option 2 (`tests/Common/` directory, include-as-compile-file). No new project. Matches the existing lightweight-fixture style (`EngineFixture.fs`, `VizEngineFixture.fs`) and avoids a new NuGet pack cycle. The helpers are not a shipped library.

### R4.3 Single top-level test command

`tests/run-all.sh` already exists and drives the live tests. The cleanup extends it to cover every test project via `dotnet test FSBarV1.slnx --filter '<category>'`, or simpler: `dotnet test` run once per test `.fsproj` with aggregated exit code. Decision: **`dotnet test FSBarV1.slnx`** at the repo root is the documented top-level command (FR-005, SC-004). `run-all.sh` keeps its live/graphical-specific prerequisite checks as a wrapper that invokes `dotnet test` after validating the engine is present.

## R5. Solution file membership

`FSBarV1.slnx` currently lists 3 of 7+ projects. After the cleanup it lists all 8:

1. `src/FSBar.Proto/FSBar.Proto.fsproj`
2. `src/FSBar.Client/FSBar.Client.fsproj`
3. `src/FSBar.SyntheticData/FSBar.SyntheticData.fsproj`
4. `src/FSBar.Viz/FSBar.Viz.fsproj`
5. `tests/FSBar.Client.Tests/FSBar.Client.Tests.fsproj` (moved)
6. `tests/FSBar.SyntheticData.Tests/FSBar.SyntheticData.Tests.fsproj` (moved)
7. `tests/FSBar.LiveTests/FSBar.LiveTests.fsproj`
8. `tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj`

## R6. Scripts that reference project paths

Two scripts hard-code project paths and MUST be updated when projects move:

- `pack-dev.sh` — references `src/FSBar.Proto`, `src/FSBar.Client`, `src/FSBar.Viz`. No test references — unaffected by the move.
- `tests/run-all.sh` — references `src/FSBar.Client.Tests/` (line 256) and `tests/FSBar.LiveTests/` (line 281). After the move, both references update to their new `tests/` paths.

All other scripts (`scripts/check-deps.sh`, `tests/check-prerequisites.sh`, `Containerfile`, `bots/trainer/*.sh`) do not reference test project paths directly.

## R7. Baseline files

- 21 committed under `src/FSBar.Client.Tests/Baselines/` — move with the project to `tests/FSBar.Client.Tests/Baselines/`.
- 14 committed under `tests/FSBar.Viz.Tests/Baselines/` — stay put.
- 0 under `src/FSBar.SyntheticData.Tests/Baselines/` — none needed yet; the placeholder test stays a placeholder, just moved under `tests/`.

Surface baselines are regenerated only if the public `.fsi` surface changes. The style pass should not change it (see R3.2). If a baseline diff appears post-pass, investigate before re-committing.

## R8. Idiomatic-style pass: where and how

### R8.1 Candidate modules (non-hot-path, ripe for pipeline-style)

Based on `private` counts (proxy for "has local helpers that could be folded into pipelines"):

- `FSBar.Client/MapCacheFile.fs` (13 private) — JSON serialization helpers.
- `FSBar.Client/Pathing.fs` (11 private) — pathfinding pipeline; **borderline hot** — verify it's not per-tick before touching.
- `FSBar.Viz/ConfigPanel.fs` (30 private) — UI panel; per-frame on `P` toggle, not always. Likely safe.
- `FSBar.Viz/StylePreset.fs` (11 private) — JSON load/save. Safe.
- `FSBar.Viz/ConfigDescriptors.fs` (14 private) — static registry; cold path. Safe.
- `FSBar.Viz/PreviewSession.fs` (26 private) — session lifecycle; cold path. Safe.

### R8.2 Modules to leave alone for style (hot paths from R2)

- `FSBar.Viz/GameViz.fs` (67 private) — hot path.
- `FSBar.Viz/SceneBuilder.fs` (20 private) — hot path.
- `FSBar.Viz/LayerRenderer.fs` (15 private) — hot path.
- `FSBar.Viz/UnitGlyph.fs` (25 private + 3 internal) — hot path (per-frame render).

For hot-path modules, **only remove `private`/`internal` keywords** — do not refactor shape, control flow, or allocation patterns.

### R8.3 Idiomatic targets (only on non-hot code)

- Replace `try … with _ -> ()` swallowing with explicit `Result`/`Option`.
- Replace multi-step `if/else` chains with `match` where it reads more cleanly.
- Replace mutable accumulators with fold/pipeline where allocation doesn't change.
- Replace `ResizeArray` for read-only data with `ImmutableArray` / `list` / `array` where the receiver doesn't need O(1) append.

Scope boundary: A style change that rewrites more than ~30 contiguous lines should be flagged for separate commit so review can isolate it.

## R9. Constitution alignment

Constitution §II requires every public `.fs` module to have an `.fsi` and a committed baseline. The cleanup:

- **Preserves** all existing `.fsi` files.
- **Preserves** all existing baselines (R3.2) unless a genuine public-surface change is made deliberately (FR-012 covers this).
- **Does not add** new public modules — the tests/Common helpers are test-only, not shipped library surface.
- Constitution §V (scripting accessibility) — `FSBar.Client`, `FSBar.Viz`, `FSBar.SyntheticData` already have `scripts/prelude.fsx` under their project dirs (verified in research). `FSBar.Proto` is a generated-types-only project and does not need a prelude. No new script obligations from this feature.

## R10. Open risks captured for the plan

- **Risk**: Removing `private` on a module-level declaration (`module private Foo = …`) where no `.fsi` is present would change the public surface. Research did not fully enumerate these cases. **Mitigation**: The implementation pass greps for `^module\s+private\s` first, checks each hit against the matching `.fsi`, and treats the case case-by-case. If no `.fsi`, the plan's tasks phase adds one.
- **Risk**: Moving `FSBar.Client.Tests` out of `src/` breaks hardcoded paths in `pack-dev.sh` even though that script does not reference test projects today — but it may scan `src/` indirectly. **Mitigation**: `pack-dev.sh` is already scoped to the three library projects; verified by research. Confirm in implementation.
- **Risk**: The shared `tests/Common/SurfaceAreaHelper.fs` approach (include-as-compile-file) can cause parallel-build oddities if multiple `.fsproj`s compile the same file simultaneously. **Mitigation**: Well-known pattern; each project gets its own compiled copy. No observed issues in similar F# setups. Fallback is Option 1 (a dedicated `FSBar.TestCommon.fsproj`).
