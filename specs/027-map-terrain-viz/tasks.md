---

description: "Task list for feature 027-map-terrain-viz"
---

# Tasks: Map Terrain Visualization Rework

**Input**: Design documents from `/specs/027-map-terrain-viz/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/viz-api.md, quickstart.md

**Tests**: Tests are REQUIRED for this feature. Constitution Principle III
mandates automated test evidence for behavior-changing code, and each
user story in `spec.md` defines independent verification criteria that
are covered here by xUnit tests.

**Organization**: Tasks are grouped by user story to enable independent
implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Single solution with multiple F# projects under `src/` and matching
test projects under `tests/`. All paths below are absolute from the
repository root (`/home/developer/projects/FSBarV1/`).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the branch builds as-is before any edits and that
the cached-map baseline path used by the quickstart is present.

- [X] T001 Verify clean baseline build on branch `027-map-terrain-viz` by running `dotnet build` in `/home/developer/projects/FSBarV1` and confirming zero errors from all projects (particularly `src/FSBar.Client`, `src/FSBar.Viz`, `tests/FSBar.Client.Tests`, `tests/FSBar.Viz.Tests`).
- [X] T002 [P] Confirm the committed cache file `bots/trainer/map-cache/avalanche_3.4.json` exists and that `MapCacheFile.read` against it succeeds via an ad-hoc FSI probe (no code change — if missing, run `bots/trainer/map-cache/refresh-all.sh` per CLAUDE.md map-analysis caching notes).
- [X] T003 [P] Capture the current `FSBar.Viz` and `FSBar.Client` surface-area baseline files' checksums (for reference; the files live under `tests/FSBar.*.Tests/baselines/` per feature 007) so drift introduced by this feature is obvious in the final diff.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add the `LayerKind.BaseTerrain` DU variant and the
placeholder color scheme entry so that both the renderer (US1) and the
cycling entry point (US3) can compile against the new variant. Nothing
in this phase must introduce a behavior change on its own — the
default `BaseLayer` flip and the default overlay flip happen in US1.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 Add `| BaseTerrain` as the first variant of `LayerKind` in `src/FSBar.Viz/VizTypes.fsi` and `src/FSBar.Viz/VizTypes.fs`, keeping all existing variants unchanged (FR-015, FR-016).
- [X] T005 Add a `LayerKind.BaseTerrain -> <identity scheme>` arm to `ColorMaps.colorSchemeFor` in `src/FSBar.Viz/ColorMaps.fs` returning a `ColorScheme` whose `MapValue` maps any input to `SKColors.Black` (the value is ignored by the dedicated renderer path — see T010). Do NOT change `src/FSBar.Viz/ColorMaps.fsi`.
- [X] T006 Add a `LayerKind.BaseTerrain -> renderBaseTerrain grid` arm in the `renderLayer` dispatch inside `src/FSBar.Viz/LayerRenderer.fs`, plus an empty private stub `let private renderBaseTerrain (grid: MapGrid) : SKBitmap = new SKBitmap(1, 1)` so the file compiles. The real body lands in T010. Include `LayerKind.BaseTerrain -> "base-terrain"` in the `cacheKey` function and leave `isDynamic` returning `false` for it.

**Checkpoint**: Foundation ready — `dotnet build` still green, no behavior change yet, all three user stories can now proceed in parallel.

---

## Phase 3: User Story 1 — Elevation-shaded terrain base layer (Priority: P1) 🎯 MVP

**Goal**: When the user opens the viewer on any supported cached map,
land renders as dark→light brown and water renders as dark→light blue,
with a crisp shoreline at elevation 0 and per-map min/max ramp
rescaling for flat and extreme maps alike. `BaseTerrain` becomes the
default base layer for both `PreviewSession` and `LiveSession` but all
other layers remain selectable (FR-016).

**Independent Test**: Run the new `BaseTerrainRenderingTests` xUnit
suite and see all cases pass; then, manually, run `dotnet fsi src/FSBar.
Viz/scripts/examples/04-base-terrain-cache.fsx` (delivered in US3, but
if run now it will still show the new base layer over Avalanche 3.4).

### Tests for User Story 1 ⚠️ WRITE FIRST

> **NOTE**: Write these tests FIRST, ensure they FAIL (because the T010 implementation is still the stub from T006), then complete T010 and re-run to see them pass.

- [X] T007 [P] [US1] Create `tests/FSBar.Viz.Tests/BaseTerrainRenderingTests.fs` with five xUnit tests:
  - ```renders land cells on brown ramp and water cells on blue ramp`` `` — build a synthetic `MapGrid` with a 4×4 `HeightMap` containing both negative and positive values, call `LayerRenderer.renderLayer grid LayerKind.BaseTerrain scheme`, assert that pixels where `height >= 0` have R > B (brown-ish) and pixels where `height < 0` have B > R (blue-ish).
  - `` `is deterministic given identical input` `` — two invocations produce byte-identical `SKBitmap.Bytes`.
  - `` `scales ramp to per-map min/max` `` — a flat-but-tiny-variation map (all land, heights in `[0.1, 0.2]`) still uses the full brown ramp (min cell is darkest, max cell is lightest); verified by checking that the min- and max-value cells produce distinct RGB outputs.
  - `` `monotonic lightness with elevation` `` — on a land map with a strictly increasing height gradient along x, compute perceived luminance `0.2126*R + 0.7152*G + 0.0722*B` for each land pixel and assert the sequence is non-decreasing. Repeat with a water map with strictly decreasing (more negative) heights and assert luminance is non-decreasing as depth shrinks. Covers FR-002 and FR-003's directional requirements.
  - `` `handles empty / zero-size grids gracefully` `` — a grid with `WidthHeightmap = 0` returns a 1×1 bitmap without throwing.
- [X] T008 [P] [US1] Register the new test file in `tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj` in the correct compile order (after any existing fixture/helper files it shares). Run `dotnet test tests/FSBar.Viz.Tests --filter BaseTerrainRenderingTests` and confirm all four tests FAIL (because `renderBaseTerrain` is still the `new SKBitmap(1, 1)` stub from T006).

### Implementation for User Story 1

- [X] T009 [US1] Add the private brown/blue palette ramps to `src/FSBar.Viz/ColorMaps.fs`: two `let private brownLandRamp : float32 -> SKColor` and `let private blueWaterRamp : float32 -> SKColor` producing visually distinct deep→light gradients (deep brown ≈ `(58, 36, 18)`, light brown ≈ `(214, 172, 120)`, deep blue ≈ `(10, 22, 60)`, light blue ≈ `(120, 196, 232)`; exact values tunable). Keep both `let private` — no `.fsi` change.
- [X] T010 [US1] Replace the stub `renderBaseTerrain` in `src/FSBar.Viz/LayerRenderer.fs` with the full per-cell implementation that walks `grid.HeightMap` at heightmap resolution, computes separate `(minLand, maxLand)` and `(minWater, maxWater)` over one pass, then writes an `RGBA8888` `SKBitmap` using `brownLandRamp` for cells `>= 0` (normalised against the land range) and `blueWaterRamp` for cells `< 0` (normalised against the water range). Copy the pattern from `renderFloatArray` for the pixel-buffer and `Marshal.Copy` idiom (lines 53–64 of the same file). Handle empty grids with the `new SKBitmap(1, 1)` fallback already used by the file.
- [X] T011 [US1] Flip the default base layer and default overlays in `src/FSBar.Viz/VizTypes.fs::VizDefaults.defaultConfig` to `BaseLayer = LayerKind.BaseTerrain` and `ActiveOverlays = Set.ofList [ OverlayKind.MetalSpots ]` (FR-015). Leave all other fields unchanged. No `.fsi` edit required — only the default value moves.
- [X] T012 [US1] Run `dotnet test tests/FSBar.Viz.Tests --filter BaseTerrainRenderingTests` and confirm all four tests now PASS. Commit only after green.
- [X] T013 [US1] In `src/FSBar.Viz/PreviewSession.fs::processKey`, assign a fresh single-key binding to `LayerKind.BaseTerrain` (pick `Key.B` if free; otherwise document the chosen key in the scripted header of T029). Mirror the binding in `src/FSBar.Viz/GameViz.fs::processKey`. Leave all existing `Number1..Number0` bindings for the legacy layers untouched (FR-016).

**Checkpoint**: User Story 1 is now testable independently — the new `BaseTerrain` renderer produces brown/blue terrain from any MapGrid, with tests proving determinism and ramp scaling. `HeightMap` and friends still work as before.

---

## Phase 4: User Story 2 — Animated metal-spot highlights (Priority: P2)

**Goal**: Metal spots (derived from `MapGrid.ResourceMap` for the
cached path, from `GameSnapshot.MetalSpots` for the live path) are
drawn on top of the base terrain as pulsing markers whose brightness
and radius rise and fall smoothly at a steady ~1.5 s cadence, never
fully occluding the terrain underneath and never fully vanishing.

**Independent Test**: Run the new `MapQueryMetalSpotsTests` xUnit
suite and a new headless `SceneBuilder` pulse test, both added in
this phase. Manual visual check: open the viewer on a map with known
metal spots and count pulsing markers.

### Tests for User Story 2 ⚠️ WRITE FIRST

- [X] T014 [P] [US2] Create `tests/FSBar.Client.Tests/MapQueryMetalSpotsTests.fs` with five xUnit tests:
  - `` `empty resource map returns empty array` `` — all zeros → `[||]`.
  - `` `single isolated non-zero cell returns one spot at elmos coordinates` `` — single cell `[2,3] = 100` → exactly one spot with `worldX = 3 * 8`, `worldZ = 2 * 8` and `worldY` matching `HeightMap.[2, 3]`.
  - `` `two disjoint 3-cell clusters return exactly two spots` `` — verifies 8-connected flood fill groups adjacent cells and doesn't merge disjoint ones.
  - `` `diagonally adjacent cells are in the same cluster` `` — assertion on 8-connectivity (distinguishes this helper from a 4-connected alternative).
  - `` `is deterministic across calls` `` — two invocations on the same grid return equal arrays including ordering.
- [X] T015 [P] [US2] Register the new test file in `tests/FSBar.Client.Tests/FSBar.Client.Tests.fsproj`. Run `dotnet test tests/FSBar.Client.Tests --filter MapQueryMetalSpotsTests` and confirm all five tests FAIL (because the public function does not exist yet).
- [X] T016 [P] [US2] Create `tests/FSBar.Viz.Tests/MetalPulseSceneTests.fs` with two headless `SceneBuilder` tests:
  - `` `pulse phase varies between frames with different elapsed seconds` `` — call `SceneBuilder.updatePulsePhase 0.0` then build a scene and capture the metal-marker's effective alpha; call `SceneBuilder.updatePulsePhase 0.75` (half a period) and capture again; assert the two alphas differ.
  - `` `metal marker alpha never reaches 0 or 255` `` — sweep across elapsed values in `[0.0, 3.0]` at 0.05 s intervals, call the pure helper `SceneBuilder.computePulseAlpha elapsed 1.5` for each, and assert the returned byte stays inside `[60, 220]` inclusive (FR-008). The test consumes `computePulseAlpha` directly as a pure public-to-test helper — no `InternalsVisibleTo` or mutable-state inspection required. T021 must expose `computePulseAlpha` with `let computePulseAlpha` (module-level, not `let private`) so the test project can reference it.
- [X] T017 [US2] Register `MetalPulseSceneTests.fs` in `tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj` and run `dotnet test tests/FSBar.Viz.Tests --filter MetalPulseSceneTests` to confirm tests FAIL.

### Implementation for User Story 2

- [X] T018 [P] [US2] Add `metalSpotsFromResourceMap: grid: MapGrid -> (float32 * float32 * float32 * float32) array` to `src/FSBar.Client/MapQuery.fsi` with the docstring from `contracts/viz-api.md`, placed after the existing public query functions.
- [X] T019 [US2] Implement `metalSpotsFromResourceMap` in `src/FSBar.Client/MapQuery.fs`: 8-connected flood fill with a queue/worklist over `grid.ResourceMap`, emit one `(worldX, worldY, worldZ, richness)` per cluster with `worldX = float32 centroidX * 8.0f`, `worldZ = float32 centroidZ * 8.0f`, `worldY` read from `grid.HeightMap.[nearestZ, nearestX]` at the nearest integer centroid, and `richness = (float32 sumValue / float32 cellCount) / float32 globalMax` clamped to `[0, 1]`. Sort the resulting array by `(clusterMinZ, clusterMinX)` for deterministic ordering.
- [X] T020 [US2] Run `dotnet test tests/FSBar.Client.Tests --filter MapQueryMetalSpotsTests`; confirm all five tests PASS. Commit.
- [X] T021 [P] [US2] Add private `let mutable pulsePhase = 0.0f` at the top of `src/FSBar.Viz/SceneBuilder.fs` alongside the existing HUD interpolation state. Add a helper `let computePulseAlpha (elapsed: float) (periodSeconds: float) : byte` that returns `byte (60.0 + 160.0 * (0.5 + 0.5 * sin(2π * elapsed / periodSeconds)))`, clamped to `[60, 220]`. **Expose `computePulseAlpha` with `let` (not `let private`)** so `MetalPulseSceneTests` in T016 can reference it directly as a pure helper. Add an `updatePulsePhase (elapsed: float) : unit` (also non-private, so FrameTick handlers in `PreviewSession` and `GameViz` can call it) that stores `0.5f + 0.5f * float32 (sin(2π * elapsed / 1.5))` into `pulsePhase`. Keep `pulsePhase` itself `let mutable private`.
- [X] T022 [US2] Modify `SceneBuilder.buildMetalSpots` (lines 67–83 of `src/FSBar.Viz/SceneBuilder.fs`) to:
  - Keep sourcing its spot list from `snap.MetalSpots` (no fallback, no length-based branching). The cached-preview path populates that field at snapshot-construction time in T029; the live path populates it from `Callbacks.getMetalSpots` as today. This keeps the render-time code path single and unambiguous, including for genuinely zero-metal live maps (FR-017, I1 remediation).
  - Drive alpha and radius by the shared `pulsePhase`. Effective alpha = `computePulseAlpha` for the opaque center; outer radius = `r * (0.85f + 0.30f * pulsePhase)`. Preserve the existing radial gradient (transparent edge) so the marker never fully occludes terrain underneath (FR-008).
- [X] T023 [US2] Thread `FrameTick` elapsed seconds into the pulse phase: in `src/FSBar.Viz/PreviewSession.fs::handleInput`, change the `InputEvent.FrameTick _` pattern to `InputEvent.FrameTick elapsed`, call `SceneBuilder.updatePulsePhase elapsed` before `emitScene ()`. Do the same in `src/FSBar.Viz/GameViz.fs::handleInput`.
- [X] T024 [US2] Run `dotnet test tests/FSBar.Viz.Tests --filter MetalPulseSceneTests`; confirm both tests PASS. Commit.
- [X] T024a [US2] Add `tests/FSBar.Viz.Tests/LiveSessionSmokeTests.fs` containing one xUnit test `` `GameViz-shaped snapshot with BaseTerrain default produces terrain bitmap plus metal markers` ``. Construct a synthetic `GameSnapshot` with a non-empty `MetalSpots: (float32 * float32 * float32 * float32) array` (two entries at distinct positions), `MapGrid` with a non-trivial height gradient containing both land and water cells, and the default `VizConfig` (which after T011 has `BaseLayer = LayerKind.BaseTerrain`). Call the same scene-build entry point used by `GameViz.emitScene` / `PreviewSession.emitScene` — expose a pure `SceneBuilder.buildSceneForTest snap config viewState : Scene` wrapper if one does not already exist — and assert the returned `Scene` contains at least one `Shader.Image` blit (the terrain bitmap) and exactly two marker primitives (one per metal spot). This gives FR-017 automated coverage of the live-path scene-build contract without needing a live BAR client. Register the new file in `tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj`. Run the test and confirm PASS after T011 + T022 are in place. Commit.

**Checkpoint**: User Story 2 is now testable independently — `metalSpotsFromResourceMap` has unit coverage, the pulse animation has headless coverage, and the viewer (when run via US3's `.fsx` entry) will show pulsing markers over the `BaseTerrain` layer.

---

## Phase 5: User Story 3 — Browse any cached map without a live game (Priority: P3)

**Goal**: The user can launch a `.fsx` entry script, open any
committed cached map by name, and press `[` / `]` (or `,` / `.`) to
cycle to the previous/next map in `MapCacheFile.supportedMaps` without
restarting the viewer or touching the live game.

**Independent Test**: Run the new `PreviewSessionCyclingTests` xUnit
suite to validate the state transitions, then manually run the new
`.fsx` script against `bots/trainer/map-cache/` and walk through the
`quickstart.md` steps 1–6.

### Tests for User Story 3 ⚠️ WRITE FIRST

- [X] T025 [P] [US3] Create `tests/FSBar.Viz.Tests/PreviewSessionCyclingTests.fs` with three xUnit tests exercising a stubbed cycling routine. Because `startWithCachedMaps` spins up a graphical viewer, pull the pure cycling logic into a testable helper — e.g., `PreviewSession.__debug_advanceCycleIndex: n: int -> direction: int -> current: int -> int` (or expose the function via `InternalsVisibleTo`). Tests:
  - `` `advance wraps past end to zero` `` — n=3, current=2, direction=+1 → 0.
  - `` `retreat wraps from zero to last` `` — n=3, current=0, direction=-1 → 2.
  - `` `single-map list stays on the same index in both directions` `` — n=1, both directions → 0.
- [X] T026 [P] [US3] Add a `` `start with empty supportedMaps throws ArgumentException` `` xUnit test in the same file that calls `PreviewSession.startWithCachedMaps [] None` and expects `ArgumentException`. Mark the test with `[<Trait("Category","Unit")>]` if the existing suite tags tests that way.
- [X] T027 [US3] Register `PreviewSessionCyclingTests.fs` in `tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj`. Run `dotnet test tests/FSBar.Viz.Tests --filter PreviewSessionCyclingTests` and confirm all tests FAIL (because the API does not exist yet).

### Implementation for User Story 3

- [X] T028 [US3] Add the new public signature to `src/FSBar.Viz/PreviewSession.fsi`: `val startWithCachedMaps: supportedMaps: MapCacheFile.SupportedMap list -> initialMapName: string option -> System.IDisposable`. Use the full docstring from `specs/027-map-terrain-viz/contracts/viz-api.md` verbatim.
- [X] T029 [US3] Implement `startWithCachedMaps` in `src/FSBar.Viz/PreviewSession.fs`:
  - Private record `type private CyclingState = { SupportedMaps: MapCacheFile.SupportedMap list; mutable CurrentIndex: int; RepoRoot: string }` stored in a module-level `mutable cyclingState: CyclingState option`.
  - Guard: `if List.isEmpty supportedMaps then raise (System.ArgumentException("supportedMaps must be non-empty"))`.
  - Resolve `repoRoot` by walking up from `System.AppContext.BaseDirectory` until a directory containing `.specify` is found; on failure, fall back to `System.Environment.CurrentDirectory` and log a warning via `eprintfn "[PreviewSession] Warning: ..."`.
  - Resolve the initial index: `match initialMapName with Some n -> supportedMaps |> List.tryFindIndex (fun m -> m.MapName = n) |> Option.defaultValue 0 | None -> 0`. If a name was given but not found, `eprintfn` with the rejected name and the valid list.
  - Extract a private `loadAtIndex (idx: int) : Result<GameSnapshot, string>` that calls `MapCacheFile.read supportedMaps.[idx] (MapCacheFile.cachePathFor repoRoot supportedMaps.[idx])`, maps `Ok loaded` to a `GameSnapshot` with `MapGrid = loaded.Grid` AND `MetalSpots = MapQuery.metalSpotsFromResourceMap loaded.Grid` (derivation happens once per load, not every frame — FR-017, I1 remediation), and maps `Error e` to `Error (MapCacheFile.formatLoadError e)`.
  - On successful load: call `LayerRenderer.invalidateAll ()`, update `currentSnapshot`, set `viewState <- { viewState with AutoFit = true }`, reset `autoFitDone <- false` (or the post-R6 equivalent), and `eprintfn "[PreviewSession] Switched to map %s" supportedMaps.[idx].MapName`.
  - On failure: leave `currentSnapshot` unchanged; store the formatted error so `buildBaseLayer` (or a new banner helper) can overlay it — a pragmatic approach is a module-level `mutable errorBanner: string option` that `emitScene`/`buildBaseLayer` draws via `Scene.text` when `Some`.
  - Call `doStart (Some initialSnapshot)` after the first successful load.
  - Return an `IDisposable` that clears `cyclingState <- None`, `errorBanner <- None`, then `stop ()`.
- [X] T030 [US3] Add a pure testable helper alongside `startWithCachedMaps`: `let internal advanceCycleIndex (n: int) (direction: int) (current: int) : int = ((current + direction) % n + n) % n`. Tag with `InternalsVisibleTo("FSBar.Viz.Tests")` if not already enabled.
- [X] T031 [US3] Add key handlers in `src/FSBar.Viz/PreviewSession.fs::processKey` for `Key.RightBracket` and `Key.Period` (advance) and `Key.LeftBracket` and `Key.Comma` (retreat). Each calls `advanceCycleIndex`, then the internal `loadAtIndex` path from T029, scoped under `lock stateLock`. Any `Error` path from `loadAtIndex` sets `errorBanner` and leaves the index unchanged.
- [X] T032 [US3] Fix `WindowResize` handling in BOTH `src/FSBar.Viz/PreviewSession.fs::handleInput` (lines 113–115) and `src/FSBar.Viz/GameViz.fs::handleInput` (lines ~107) to re-run `computeAutoFit` when `viewState.AutoFit = true` after updating `WindowWidth`/`WindowHeight`. This closes FR-009a. In the same pass, delete the redundant `autoFitDone` flag and replace all its reads/writes with `viewState.AutoFit`; update `computeAutoFit` to no longer force `AutoFit = false` at the end (research R6). Confirm `dotnet build` still green.
- [X] T033 [US3] Create the new `.fsx` entry script at `src/FSBar.Viz/scripts/examples/04-base-terrain-cache.fsx` that: (a) `#load "../prelude.fsx"`, (b) `open FSBar.Client` and `open FSBar.Viz`, (c) reads `fsi.CommandLineArgs` for an optional map name, (d) calls `PreviewSession.startWithCachedMaps MapCacheFile.supportedMaps initial`, (e) blocks on `System.Console.ReadLine()`. Include a short header comment documenting the `[` / `]` / `,` / `.` keybindings, the `Key.B` BaseTerrain toggle from T013, and the fault-injection example from `quickstart.md` step 6.
- [X] T034 [US3] Run `dotnet test tests/FSBar.Viz.Tests --filter PreviewSessionCyclingTests` and confirm all cycling tests PASS. Commit.

**Checkpoint**: User Story 3 complete — the `.fsx` script opens any cached map, `[` / `]` cycle through maps without a restart, bad cache files produce an in-viewer error banner, and `dotnet test` is green for all three user stories.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Surface-area baseline refresh, manual quickstart validation, and `fsdoc` update.

- [X] T035 Refresh the `FSBar.Viz` surface-area baseline by running the existing update-baseline flow (per feature 007's convention — likely `dotnet test tests/FSBar.Viz.Tests --filter SurfaceArea` with an environment variable or the approved update script). Commit the regenerated baseline file in the same PR as the `.fsi` additions.
- [X] T036 [P] Refresh the `FSBar.Client` surface-area baseline for the `MapQuery.metalSpotsFromResourceMap` addition using the same flow as T035, targeted at `tests/FSBar.Client.Tests`.
- [X] T037 [P] Run the full test suite — `dotnet test` at the repository root — and confirm zero regressions in `FSBar.Client.Tests`, `FSBar.Viz.Tests`, and any other project that depends on `FSBar.Viz` (e.g., `FSBar.LiveTests`). Investigate any failure — do not suppress.
- [ ] T038 Walk through `specs/027-map-terrain-viz/quickstart.md` steps 1–6 manually on a graphical environment (`DISPLAY=:0`, `XDG_RUNTIME_DIR=/tmp/runtime-developer`). Capture a screenshot of each step into the PR description for manual-verification evidence.
- [ ] T039 [P] Run the `fsdoc` agent for `FSBar.Viz` and `FSBar.Client` to update generated documentation for the `LayerKind.BaseTerrain` variant, the new `PreviewSession.startWithCachedMaps` entry, and the `MapQuery.metalSpotsFromResourceMap` helper (Constitution Workflow §7).
- [X] T040 [P] Update `CLAUDE.md`'s "Recent Changes" / "Active Technologies" snippets via `.specify/scripts/bash/update-agent-context.sh claude` (already done at plan time; re-run if any technical-context lines drifted during implementation).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; run first.
- **Foundational (Phase 2)**: Depends on Setup. T005 depends on T004; T006 depends on T004. T004 must land before any test file that references `LayerKind.BaseTerrain` compiles.
- **User Story 1 (Phase 3)**: Depends on Foundational. Within: T007 and T008 first (FAIL), then T009/T010 in that order, then T011, T012, T013. T013 touches the same file (`PreviewSession.fs`) as T023/T031/T032 — schedule T013 before those to avoid merge churn.
- **User Story 2 (Phase 4)**: Depends on Foundational. US2 does NOT depend on US1 at the code level — pulse and marker logic work on any base layer — but for a satisfying visual demo, run US2 after US1. T018 depends on T014/T015 being red first. T019 depends on T018. T022 depends on T019 (`MapQuery.metalSpotsFromResourceMap` must exist) and T021 (pulse phase machinery). T023 depends on T021. T024 depends on T019 + T021 + T022 + T023. T024a depends on T011 (default flip) plus T022 (no-fallback `buildMetalSpots`); schedule after T024.
- **User Story 3 (Phase 5)**: Depends on Foundational and benefits from US1 visually. T028 depends on T025/T026/T027 being red. T029 depends on T028. T030 and T031 depend on T029. T032 is independent of the cycling API but touches the same file; schedule after T029/T031 to minimize rebase churn. T033 depends on T029. T034 depends on T029/T030/T031.
- **Polish (Phase 6)**: Depends on US1, US2, and US3 all green.

### User Story Dependencies

- **US1 (P1)**: depends on Foundational only — no dependency on US2 or US3.
- **US2 (P2)**: depends on Foundational only — no dependency on US1 or US3. Visually complements US1 but is independently testable (metal extraction from ResourceMap has no coupling to the base layer renderer).
- **US3 (P3)**: depends on Foundational only — no dependency on US1 or US2. The cycling logic is independent of what layer is active. However, the `.fsx` quickstart (T033) implicitly assumes US1's `BaseTerrain` default, so run US3 after US1 for a meaningful manual demo.

### Within Each User Story

- Tests MUST be written and FAIL before implementation (T007/T008 → T009/T010; T014/T015/T016/T017 → T018/T019/T021/T022; T025/T026/T027 → T028/T029).
- Signature (`.fsi`) additions come just before their implementation (T018 → T019; T028 → T029) so the compiler validates the contract each step.
- Commit after each green test run.

### Parallel Opportunities

- **Setup phase**: T002 and T003 are independent of T001's build check; run them alongside T001.
- **Foundational phase**: T005 and T006 are independent edits (different functions in different files) once T004 has landed; run T005 and T006 in parallel.
- **US1**: T007 and T008 edit different files; parallelizable. T009 is a single-file edit independent of T007/T008's test code and can start in parallel with them.
- **US2**: T014, T015, T016 all edit fresh test files; parallelizable. T018 and T021 edit different files (`MapQuery.fsi` vs `SceneBuilder.fs`) and can run in parallel.
- **US3**: T025 and T026 edit the same file — NOT parallelizable with each other. T032 touches `PreviewSession.fs` and `GameViz.fs`, same files as T029 and T031; serialise within US3.
- **Polish phase**: T036, T037, T039, T040 are independent; run in parallel. T035 and T036 are in different test projects and can also run in parallel.
- **Across stories once Foundational is done**, US1/US2/US3 can be staffed by three developers in parallel — the only shared file is `PreviewSession.fs` (US1's T013, US2's T023, US3's T029/T031/T032), which is the single coordination point.

---

## Parallel Example: User Story 2

```bash
# Write all US2 tests together (different files):
Task: "Create tests/FSBar.Client.Tests/MapQueryMetalSpotsTests.fs (T014)"
Task: "Create tests/FSBar.Viz.Tests/MetalPulseSceneTests.fs (T016)"

# Land the US2 signatures and pulse-phase wiring in parallel (different files):
Task: "Add metalSpotsFromResourceMap to src/FSBar.Client/MapQuery.fsi (T018)"
Task: "Add pulse-phase state to src/FSBar.Viz/SceneBuilder.fs (T021)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup — confirm clean baseline build.
2. Complete Phase 2: Foundational — add `LayerKind.BaseTerrain` + stubs.
3. Complete Phase 3: User Story 1 — brown/blue terrain rendering, default layer flip, tests green.
4. **STOP and VALIDATE**: open the `.fsx` script manually (if US3 is not yet built, use an ad-hoc FSI session calling `PreviewSession.startWithMap` on a grid loaded via `MapCacheFile.read`). Visually confirm brown land / blue water / sharp shoreline.
5. Ship as MVP if the rest of the feature needs to wait.

### Incremental Delivery

1. Setup + Foundational → baseline safe.
2. Add US1 → test green → manual MVP visual check → optional demo.
3. Add US2 → test green → visual check (metal pulsing on top of terrain).
4. Add US3 → test green → quickstart walkthrough.
5. Polish (baselines, fsdoc, full `dotnet test`).

### Parallel Team Strategy

After Foundational is complete:

- Developer A: User Story 1 (terrain renderer)
- Developer B: User Story 2 (metal extraction + pulse animation)
- Developer C: User Story 3 (cycling API + `.fsx` script + AutoFit fix)

The only shared file is `PreviewSession.fs`. Coordinate by merging US1's small `processKey` edit (T013) first, then US2's `handleInput` FrameTick edit (T023), then US3's cycling additions (T029/T031/T032). Each merge is a small, review-friendly diff.

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks.
- [Story] label maps every implementation task back to a spec user story.
- Constitution II requires `.fsi` edits to land in the same PR as their implementation — T018/T019 and T028/T029 are ordered this way intentionally.
- Constitution III requires each behavior-changing change to have tests that fail before the change and pass after — the "write tests first" gates in each user-story phase enforce this.
- Constitution IV requires failures to fail fast or degrade explicitly — the error banner path in T029 / T031 (rather than crashing or blanking) is how this feature honours it.
- Do NOT commit surface-area baseline drift without running the refresh flow in T035/T036.
- Do NOT mark a task complete until its linked test (or build) run is green.
