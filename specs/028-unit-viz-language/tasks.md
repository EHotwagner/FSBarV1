---
description: "Task list for 028-unit-viz-language"
---

# Tasks: Unit Visual Representation for SkiaViewer

**Input**: Design documents from `/specs/028-unit-viz-language/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included. The feature spec and plan explicitly require xUnit coverage for label generation, shape/tier/faction classification, scene composition, overlay composition, and surface-area baseline (plan.md §Testing; research.md §R3 acceptance evidence).

**Organization**: Tasks are grouped by user story so each story can be implemented and verified independently.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Different file, no dependency on an incomplete task — safe to run in parallel
- **[US1]..[US4]**: User-story label maps the task to a spec story for traceability
- Every task lists an exact repo-relative file path

## Path Conventions

- `src/FSBar.Viz/` — target module for new code
- `tests/FSBar.Viz.Tests/` — target test project for new tests
- `specs/028-unit-viz-language/contracts/` — authoritative `.fsi` contracts (read-only reference)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the tooling and empirical `BarData` field shape before touching production code (research.md R1).

- [X] T001 Open FSI (`restart_fsi` MCP tool) and verify the `BarData.UnitDef` fields listed in research.md R1 (`subfolder: string`, `customParams: Map<string,string>`, `category: string option`, `movement.movementClass`, `movement.canFly`, `canMove`) exist and carry the assumed types. Record the result as a comment at the top of `src/FSBar.Viz/UnitGlyph.fs` (created in T011) — amend data-model.md only if a field name/type diverges.
- [X] T002 [P] Confirm `nupkg/BarData.1.0.3.nupkg` is the current locally-referenced version; run `./scripts/check-deps.sh` and record the BarData version string that will be written into `UnitLabels.generated.fs` as `BarDataVersion`.
- [X] T003 [P] Confirm `dotnet build src/FSBar.Viz/FSBar.Viz.fsproj` and `dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj` both succeed on the current branch tip before adding any code (baseline for regression detection).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Introduce the shared types, palettes, feature flag, and empty module stubs that every user story depends on. No user story work may begin until this phase is complete.

**⚠️ CRITICAL**: All tasks here land before Phase 3.

- [X] T004 Extend `src/FSBar.Viz/VizTypes.fsi` additively per `specs/028-unit-viz-language/contracts/VizTypes.delta.fsi`: add `MovementShape`, `Tier`, `FactionId`, `OrderKind`, `StatusFlags`, `CommandWaypoint`, `UnitDisplay`, `FactionPalette`, `TeamPalette`, `UnitGlyphStyle`, `EventEffectKind`, `EventEffect`. Append the four new `OverlayKind` cases (`WeaponRanges`, `SightRanges`, `CommandQueue`, `FullNames`). Add `UseGlyphRenderer: bool` and `GlyphStyle: UnitGlyphStyle` to the existing `VizConfig` record. Do not remove or rename any existing declaration.
- [X] T005 Mirror T004 in `src/FSBar.Viz/VizTypes.fs` so the implementation file exposes the same additive surface declared in the `.fsi`. Keep all existing definitions in place.
- [X] T006 Update `src/FSBar.Viz/VizDefaults.fs` (and its `.fsi` if present) so `defaultConfig` carries `UseGlyphRenderer = true` and `GlyphStyle = UnitGlyphPalettes.defaults`. Keep existing fields unchanged. Forward-reference `UnitGlyphPalettes` (defined in T008) via project file ordering only — do not put implementation bodies here.
- [X] T007 Create `src/FSBar.Viz/UnitGlyphPalettes.fsi` verbatim from `specs/028-unit-viz-language/contracts/UnitGlyphPalettes.fsi` (namespace `FSBar.Viz`). Register it in `src/FSBar.Viz/FSBar.Viz.fsproj` above `UnitGlyphPalettes.fs`.
- [X] T008 Create `src/FSBar.Viz/UnitGlyphPalettes.fs` implementing the contract: `defaultFactionPalette` (distinct `SKColor` per faction), `defaultTeamPalette` (wraps existing `ColorMaps` team colors with a neutral fallback), `defaults: UnitGlyphStyle` with the numeric values listed in data-model.md §3, and `withOverrides`. Pure data — no side effects.
- [X] T009 Create `src/FSBar.Viz/UnitGlyph.fsi` verbatim from `specs/028-unit-viz-language/contracts/UnitGlyph.fsi`. Register it in `src/FSBar.Viz/FSBar.Viz.fsproj` above `UnitGlyph.fs` and after `UnitGlyphPalettes.fs`.
- [X] T010 Create `src/FSBar.Viz/UnitLabels.generated.fs` as a minimal stub exposing `BarDataVersion`, `GeneratedAtUtc`, `Labels: Map<string,string>` (empty), `tryLookup`, `lookupOrFallback` per data-model.md §6. Mark the file header `// Generated — do not edit by hand. Regenerate via scripts/gen-unit-labels.fsx.` Register in the `.fsproj` above `UnitGlyph.fs`. T028 will overwrite the stub with real data.
- [X] T010a Create `src/FSBar.Viz/UnitLabels.generated.fsi` declaring the public surface from `data-model.md §6`: `val BarDataVersion: string`, `val GeneratedAtUtc: string`, `val Labels: Map<string, string>`, `val tryLookup: internalName: string -> string option`, `val lookupOrFallback: internalName: string -> string`. Header comment: `// Generated — do not edit by hand. Regenerate via scripts/gen-unit-labels.fsx.` Register in `src/FSBar.Viz/FSBar.Viz.fsproj` immediately above `UnitLabels.generated.fs`. Required by Constitution §II — every public module needs a signature file, and the plan's "internal" language cannot be enforced without one.
- [X] T011 Create `src/FSBar.Viz/UnitGlyph.fs` with stub bodies for every value in the `.fsi` (`classifyShape`, `classifyTier`, `classifyFaction`, `buildUnit`, `buildOverlayLayer`, `buildUnitsGlyph`, `advanceEffects`, `resetSession`). Each stub returns an empty list or `MovementShape.Unknown`/`Tier.T1`/`FactionId.Neutral` and captures no side effects yet. The file must compile against the `.fsi`; US1/US2 tasks will fill in real logic.
- [X] T012 Run `dotnet build src/FSBar.Viz/FSBar.Viz.fsproj` and resolve any compile errors introduced by T004–T011. Do not proceed to Phase 3 until the project builds cleanly.
- [X] T013 (Removed during /speckit.analyze — baseline refresh consolidated into T054 so `FSBar.Viz.baseline` is written exactly once, after the real additive surface is stable.)

**Checkpoint**: Foundation ready. `FSBar.Viz` builds, stub API is in place, palettes/types available. User-story phases may now begin in parallel.

---

## Phase 3: User Story 1 — Read unit identity and state at a glance (Priority: P1) 🎯 MVP

**Goal**: The permanent visual layer encodes movement class, team, faction, tier, facing, HP, build progress, label, stun, damage flash — readable without any overlay.

**Independent Test**: Load a `FSBar.SyntheticData.Scenes` snapshot containing every movement class, faction, tier, and state into the SkiaViewer glyph renderer and verify by visual inspection (`scripts/examples/NN-unit-glyph.fsx`) plus `UnitGlyphTests` that each unit's identity and state is legible without any overlay toggle.

### Tests for User Story 1 ⚠️

> Write these tests FIRST and confirm they FAIL before implementing.

- [X] T014 [P] [US1] Create `tests/FSBar.Viz.Tests/UnitGlyphTests.fs` with classifier rule-stack tests covering `UnitGlyph.classifyShape`: `!canMove → Building`, `canFly → Air`, `BOT`/`KBOT`/`ARMBOT` → `Bot`, `TANK`/`VEHICLE`/`ATV` → `Vehicle`, `HOVER` → `Hover`, `BOAT`/`UBOAT`/`SHIP` → `Ship`, unknown class → `Unknown` with `logMiss` invoked exactly once. Register the file in `tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj`.
- [X] T015 [P] [US1] Extend `tests/FSBar.Viz.Tests/UnitGlyphTests.fs` with `classifyTier` cases: `customParams["techlevel"] = "2"` → `T2`; no customParams but `category` contains `LEVEL3` → `T3`; neither → `T1` + `logMiss` once.
- [X] T016 [P] [US1] Extend `tests/FSBar.Viz.Tests/UnitGlyphTests.fs` with `classifyFaction` cases: `subfolder = "Units/armada/tanks"` → `Armada`; empty subfolder with `internalName = "corcom"` → `Cortex`; unknown both → `Neutral` + `logMiss` once.
- [X] T017 [P] [US1] Create `tests/FSBar.Viz.Tests/UnitGlyphSceneTests.fs` with a permanent-layer composition test: build a `UnitDisplay seq` covering all six `MovementShape` cases from fabricated fixtures, call `UnitGlyph.buildUnitsGlyph` with an empty overlay set, and assert the returned `Scene list` is non-empty and contains at least one primitive per input unit. Register in the `.fsproj`.
- [X] T018 [P] [US1] Extend `UnitGlyphSceneTests.fs` with HP-arc / facing-pip / dashed-construction assertions: a fixture at 100% HP produces no HP arc; a fixture at 40% HP produces an HP arc primitive; a fixture with `BuildProgress = 0.3` produces a dashed-stroke primitive.
- [X] T019 [P] [US1] Extend `UnitGlyphSceneTests.fs` with an event-effect test: run `advanceEffects` on two successive frames where HP decreased, assert an `EventEffect.UnderAttackFlash` is produced with `DurationMs = style.EventFlashDurationMs`; advance `nowMs` past the duration and assert the effect is retired.

### Implementation for User Story 1

- [X] T020 [US1] Implement `UnitGlyph.classifyShape` in `src/FSBar.Viz/UnitGlyph.fs` per research.md R4 and spec FR-001: building → air → prefix-matched movementClass → `Unknown`. Emit `logMiss` exactly once per distinct unknown class via a module-private `ConcurrentDictionary<string, unit>`. Tests from T014 must pass.
- [X] T021 [US1] Implement `UnitGlyph.classifyTier` in `src/FSBar.Viz/UnitGlyph.fs` per FR-005: read `customParams["techlevel"]` → parse `category` for `LEVEL{1,2,3}` → default `T1`. Tests from T015 must pass.
- [X] T022 [US1] Implement `UnitGlyph.classifyFaction` in `src/FSBar.Viz/UnitGlyph.fs` per FR-004: second path segment of `subfolder` → `internalName` prefix → `Neutral`. Tests from T016 must pass.
- [X] T023 [US1] Implement `UnitGlyph.buildUnit` in `src/FSBar.Viz/UnitGlyph.fs` covering FR-001..FR-013: shape via `Shape` dispatch using existing `SkiaViewer.Scene` primitives (ellipse/square/polygon/rounded rect/triangle/hexagon), team fill from `style.TeamPalette`, faction stroke from `style.FactionPalette`, stroke width from tier, facing pip at `HeadingRadians`, HP arc opposite the pip (hidden at full HP, red below `LowHpFraction`), dashed stroke + alpha from `BuildProgress`, low-HP shader placeholder (noise+red tint emulated via a tinted overlay primitive), red stroke flash driven by `activeEffects`, desaturation when `Status.IsStunned`, just-built ring when `JustCompletedWithinMs` is set. Clamp `BuildProgress`, suppress HP arc when `MaxHealth = 0`, handle `nan` heading per data-model §2 validation.
- [X] T024 [US1] Implement `UnitGlyph.buildUnitsGlyph` in `src/FSBar.Viz/UnitGlyph.fs` so that with an empty `activeOverlays` set it returns the flattened permanent-layer primitives (`buildUnit` per unit concatenated). Cache `classifyShape`/`classifyTier`/`classifyFaction` results in a module-private `ConcurrentDictionary<int, _>` keyed by `DefId` per data-model §7. Tests from T017 must pass.
- [X] T025 [US1] Implement `UnitGlyph.advanceEffects` and `UnitGlyph.resetSession` in `src/FSBar.Viz/UnitGlyph.fs` per data-model §5: detect HP decrease → `UnderAttackFlash`, `IsUnderConstruction` falling edge → `JustBuiltRing`, `IsStunned` high → `StunnedDesaturate`. Store in a module-private mutable list; remove effects whose `(nowMs - StartedAtMs) ≥ DurationMs`. `resetSession` clears the effect list and the static cache. Tests from T018 and T019 must pass.
- [X] T026 [US1] Wire `UnitGlyph.buildUnitsGlyph` into `src/FSBar.Viz/SceneBuilder.fs` behind `VizConfig.UseGlyphRenderer`: when `true`, the new path replaces the legacy `buildUnits` branch at `SceneBuilder.fs:128` (research.md R5); when `false`, fall through to the existing legacy path. Do not change the public `SceneBuilder.fsi` surface. Mark the legacy `OverlayKind.Units` branch with a comment linking to the follow-up removal.
- [X] T027 [US1] Extend `src/FSBar.SyntheticData/UnitSim.fs` (plan.md §Project Structure) to populate synthetic `UnitDisplay` values with deterministic `HeadingRadians` and `BuildProgress`, plus `StatusFlags` defaults. Expose a helper `SyntheticData.toUnitDisplays: Scene -> Frame -> UnitDisplay seq` (referenced in quickstart §3). Do not touch `FSBar.Client.TrackedUnit` (research.md R2).

**Checkpoint**: Permanent layer renders synthetic scenes through the glyph path. US1 acceptance scenarios 1–9 pass; scenario 10 (unique labels) is validated in US3 once T028 lands.

---

## Phase 4: User Story 3 — Unique labels across the full unit catalog (Priority: P2)

**Goal**: Deterministic, minimally-churning, byte-stable 2-/3-char label table covering every `BarData.AllUnits` entry, committed to the repository.

**Independent Test**: Run the generator twice against the current `BarData` version and verify byte-identical output, zero duplicate labels, ≥ 90% 2-char labels, and ≥ 95% preservation against a fabricated `BarData+1` input. Covered by `UnitLabelsGeneratorTests`.

> This story is placed before US2 because US1 acceptance scenario 10 and US2 rely on the real label table — once US1 lands, the stub `Labels = Map.empty` must be replaced. Running US3 next keeps the dependency chain tight.

### Tests for User Story 3 ⚠️

- [X] T028 [P] [US3] Create `tests/FSBar.Viz.Tests/UnitLabelsGeneratorTests.fs` with a determinism test: invoke the generator function twice on the committed `BarData.AllUnits` list and assert the two resulting `Map<string,string>` values are structurally equal. Register the file in `tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj`.
- [X] T029 [P] [US3] Extend `UnitLabelsGeneratorTests.fs` with a uniqueness test: the generated map has no duplicate values.
- [X] T030 [P] [US3] Extend `UnitLabelsGeneratorTests.fs` with a 2-char rate test: ≥ 90% of values have `String.length = 2` (SC-002).
- [X] T031 [P] [US3] Extend `UnitLabelsGeneratorTests.fs` with a stability test: starting from a snapshot of the committed `UnitLabels.generated.fs`, adding a fabricated new unit to the input set and re-running the generator preserves ≥ 95% of existing codes (SC-006) and surfaces any reassignment.
- [X] T032 [P] [US3] Extend `UnitLabelsGeneratorTests.fs` with a prefix-strip test: every internal name in `BarData.AllUnits` beginning with `arm`/`cor`/`leg`/`rap`/`scav` has its prefix stripped before the generator's letter-selection walk (FR-021).

### Implementation for User Story 3

- [X] T033 [US3] Factor the label generator into a pure testable module: add `src/FSBar.Viz/UnitLabelsGenerator.fs` (plus `.fsi`) exposing `generate: names: string seq -> previous: Map<string,string> option -> Map<string,string>` implementing the two-pass algorithm in research.md R3 (Pass 1 title-case pair walk with consonant preference, Pass 2 stability preservation against `previous`). Pure function, no I/O. Register in `FSBar.Viz.fsproj` above `UnitLabels.generated.fs`.
- [X] T034 [US3] Create `src/FSBar.Viz/scripts/gen-unit-labels.fsx` (plan.md §Project Structure): load `BarData` from the local nupkg feed, call `UnitLabelsGenerator.generate` with the current committed file as `previous`, and emit `src/FSBar.Viz/UnitLabels.generated.fs` with `BarDataVersion` from T002, `GeneratedAtUtc` in ISO 8601, and the generated `Labels` map. Exit non-zero if any existing label would change without a genuine collision (quickstart §1). Document the invocation in the script header. The companion `UnitLabels.generated.fsi` from T010a is stable (surface does not change across regenerations) — the generator only touches the `.fs` file.
- [X] T035 [US3] Run `dotnet fsi src/FSBar.Viz/scripts/gen-unit-labels.fsx` and commit the produced `src/FSBar.Viz/UnitLabels.generated.fs`. Confirm `UnitLabelsGeneratorTests` (T028–T032) all pass and that the committed file reports the correct `BarDataVersion`.
- [X] T036 [US3] Update `UnitGlyph.buildUnit` in `src/FSBar.Viz/UnitGlyph.fs` so it reads `LabelCode` directly from `UnitDisplay.LabelCode` (already populated by the data-source adapter) and falls back to `UnitLabels.lookupOrFallback` when the field is empty. Ensures the rendered label in US1 acceptance scenario 10 is now sourced from the real table.
- [X] T037 [US3] Update `SyntheticData.toUnitDisplays` (from T027) to fill `UnitDisplay.LabelCode` by calling `UnitLabels.lookupOrFallback` for each synthetic unit's `InternalName`. No direct `BarData` lookups from the renderer hot path (data-model §7).

**Checkpoint**: The label table is committed, tested, and feeding both synthetic fixtures and the glyph renderer. US1 acceptance scenario 10 now passes end-to-end.

---

## Phase 5: User Story 2 — Toggle informational overlays with sticky hotkeys (Priority: P2)

**Goal**: `W` weapon ranges, `L` sight, `C` command queue, `N` full names as sticky-toggle overlays that compose with each other and with the permanent layer.

**Independent Test**: With a synthetic scene loaded, flip each overlay hotkey through the viewer's input layer and verify the corresponding overlay primitives appear, persist, compose with every other overlay, and are reflected in the status-line indicator. Covered by `UnitGlyphSceneTests.Overlay*` tests plus the manual FSI walkthrough.

### Tests for User Story 2 ⚠️

- [X] T038 [P] [US2] Extend `tests/FSBar.Viz.Tests/UnitGlyphSceneTests.fs` with an overlay independence test: call `UnitGlyph.buildOverlayLayer` with `activeOverlays = {WeaponRanges}` on fixtures that include armed and unarmed units, and assert only armed units contribute weapon-range primitives.
- [X] T039 [P] [US2] Extend `UnitGlyphSceneTests.fs` with an overlay composition test: with `activeOverlays = {WeaponRanges; SightRanges}` both layers' primitives are present; the `SightRanges` stroke style must differ from `WeaponRanges` (dashed vs solid, distinct alpha) per research.md R6 and FR-018.
- [X] T040 [P] [US2] Extend `UnitGlyphSceneTests.fs` with a command-queue overlay test: a fixture with a populated `CommandQueue` and `activeOverlays = {CommandQueue}` produces a polyline primitive through every waypoint, the current waypoint uses a thicker stroke, and waypoints are color-coded by `OrderKind` (FR-019).
- [X] T041 [P] [US2] Extend `UnitGlyphSceneTests.fs` with a full-names overlay test: with `activeOverlays = {FullNames}` every unit contributes a text primitive carrying its `InternalName`, bypassing any zoom-threshold label suppression (FR-019a).
- [X] T042 [P] [US2] Extend `UnitGlyphSceneTests.fs` with a status-line projection test: build an active-overlays set and call `UnitGlyph.statusLine` with it; assert the returned string contains exactly the single-letter codes for the active overlays in stable `WLCN` order (FR-015).

### Implementation for User Story 2

- [X] T043 [US2] Implement `UnitGlyph.buildOverlayLayer` in `src/FSBar.Viz/UnitGlyph.fs` per FR-014..FR-019a and research.md R6: for each `OverlayKind` in `activeOverlays` emit the corresponding primitives (W: solid stroked circles per weapon range; L: dashed stroked circle at `SightRangeElmo`; C: color-coded polyline through `CommandQueue`; N: text primitive beside the unit). Must not mutate global state; must be deterministic in unit order.
- [X] T044 [US2] Update `UnitGlyph.buildUnitsGlyph` in `src/FSBar.Viz/UnitGlyph.fs` to concatenate the permanent layer (`buildUnit` per unit) with `buildOverlayLayer` output. Tests from T038–T041 must pass.
- [X] T045 [US2] Route the four new `OverlayKind` cases through the existing input/toggle dispatch in `src/FSBar.Viz/GameViz.fs` (and any related `InputHandler`/`SessionState` glue) so that pressing `W`/`L`/`C`/`N` flips the corresponding set element. Sticky, not hold (spec FR-014); each key toggles independently; extension points remain for the deferred `R E B T V I X` overlays.
- [X] T046 [US2] Implement `UnitGlyph.statusLine` in `src/FSBar.Viz/UnitGlyph.fs` as a pure function `Set<OverlayKind> -> string`, emitting `WLCN` codes in fixed order filtered by the active set. Then wire `src/FSBar.Viz/GameViz.fs` (or the existing status-line renderer referenced by FR-015) to call `UnitGlyph.statusLine` each frame and route its output to the status-line widget. Test from T042 must pass.
- [X] T047 [US2] Add `src/FSBar.Viz/scripts/examples/NN-unit-glyph.fsx` (plan.md §Project Structure) demonstrating permanent-only → `W` → `W+L` → `W+L+C` on a `FSBar.SyntheticData.Scenes.SceneA` snapshot. Extend `src/FSBar.Viz/scripts/prelude.fsx` to `#load` the new `UnitGlyph` / `UnitGlyphPalettes` modules so the example runs with one `#load`.

**Checkpoint**: All four MVP overlays toggle and compose. US2 acceptance scenarios 1–7 pass. The FSI walkthrough in `quickstart.md §3` runs end-to-end.

---

## Phase 6: User Story 4 — Scale, zoom, and declutter (Priority: P3)

**Goal**: Minimum-pixel radius clamp keeps 1x1 footprints visible at low zoom; default labels suppress below the legibility threshold; `N` overlay bypasses that suppression.

**Independent Test**: Drive the FSI example script through three zoom levels (min/mid/max) and verify small units remain visible at min zoom, default labels disappear below threshold, and `N`-activated full names are always drawn.

### Tests for User Story 4 ⚠️

- [X] T048 [P] [US4] Extend `tests/FSBar.Viz.Tests/UnitGlyphSceneTests.fs` with a min-pixel-radius test: a fixture whose `FootprintWidthElmo = 1.0f` at a simulated low zoom produces a shape primitive whose effective pixel radius is clamped to `style.MinPixelRadius` (FR-002).
- [X] T049 [P] [US4] Extend `UnitGlyphSceneTests.fs` with a label-legibility threshold test: at a zoom below `style.LabelLegibilityZoomThreshold` the default 2-char label primitive is absent while shape/stroke/fill primitives remain present.
- [X] T050 [P] [US4] Extend `UnitGlyphSceneTests.fs` with an `N`-bypass test: the same low-zoom fixture with `OverlayKind.FullNames` active still produces the full-name text primitive (FR-019a + acceptance scenario US4.3).

### Implementation for User Story 4

- [X] T051 [US4] Thread the current zoom scale (world-units-per-pixel) into `UnitGlyph.buildUnit` via an additional parameter on `buildUnitsGlyph` or through `UnitGlyphStyle`. Clamp rendered footprint to `style.MinPixelRadius` when the world-space footprint would drop below the pixel threshold. Update `SceneBuilder.fs` to pass the camera's current zoom from `VizConfig`/`VizState`.
- [X] T052 [US4] In `UnitGlyph.buildUnit`, suppress the default `LabelCode` text primitive when the current zoom is below `style.LabelLegibilityZoomThreshold`. `UnitGlyph.buildOverlayLayer` for `OverlayKind.FullNames` must NOT inspect the threshold — ensuring the `N` overlay always draws. Tests from T049, T050 must pass.
- [X] T053 [US4] Update `src/FSBar.Viz/scripts/examples/NN-unit-glyph.fsx` (from T047) to cycle through three zoom presets and screenshot or print primitive counts at each, giving the manual independent test for US4 a reproducible entry point.

**Checkpoint**: All four user stories are independently functional. Permanent layer, label table, overlays, and zoom/declutter all pass their acceptance scenarios.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T054 [P] Regenerate `tests/FSBar.Viz.Tests/Baselines/FSBar.Viz.baseline` (touched in T013) to reflect the final `UnitGlyph` / `UnitGlyphPalettes` / `VizTypes` / `VizConfig` surface, run `SurfaceBaselineTests`, and commit any approved diff.
- [X] T055 [P] Add a 200-unit performance smoke test `tests/FSBar.Viz.Tests/UnitGlyphPerfTests.fs` that builds `UnitGlyph.buildUnitsGlyph` with three overlays active and asserts total elapsed time per frame stays under the SC-004 budget on this machine (target ≥ 30 fps → per-frame ≤ 33 ms). Mark the test `[<Trait("Category","Perf")>]` so it can be filtered out of fast CI.
- [X] T056 Run `dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj` in full and confirm every new test passes alongside the existing `MetalPulseSceneTests`, `PreviewSessionCyclingTests`, `LiveSessionIntegrationTests`, and `SyntheticVizTests` (research.md R5 mandates no regressions on the legacy path).
- [X] T057 [P] Walk `specs/028-unit-viz-language/quickstart.md` end-to-end from a clean checkout: regenerate labels (§1), run the filtered test commands (§2, §4), and execute the FSI walkthrough (§3). Record any step that diverges and fix the deviation in code — not in the quickstart — unless the spec itself is wrong.
- [X] T058 Update `CLAUDE.md` Recent Changes block with the glyph-renderer feature-flag note and `UnitGlyph` module reference (additive only; do not rewrite sibling feature entries).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001–T003. Start immediately; T002 and T003 run in parallel.
- **Foundational (Phase 2)**: T004–T013. Depends on Phase 1. T004 → T005 → T006 is sequential (same / linked files); T007/T009/T010 can start in parallel once T004/T005 compile; T008 depends on T007; T011 depends on T007/T009/T010; T012 depends on all of T004–T011; T013 is parallel with T012. **Blocks every user-story phase.**
- **User Story 1 (Phase 3)**: Depends on Phase 2. Implementation tasks (T020–T027) can start after the US1 tests (T014–T019) are red.
- **User Story 3 (Phase 4)**: Depends on Phase 2 and on `UnitGlyph.buildUnit` from T023 existing as a stub or real implementation (so `UnitDisplay.LabelCode` has a consumer). US3 is sequenced before US2 because US1's acceptance scenario 10 requires the committed label table; running US3 immediately after US1 closes that loop.
- **User Story 2 (Phase 5)**: Depends on Phase 2 and the real `UnitGlyph.buildUnit` from US1 (T023/T024). Overlays compose on top of the permanent layer.
- **User Story 4 (Phase 6)**: Depends on the US1 permanent layer (T023/T024) and the US2 `FullNames` overlay (T043/T044) because T050 exercises overlay bypass.
- **Polish (Phase 7)**: Depends on every other phase.

### User Story Dependencies

- **US1 (P1)**: No cross-story dependency. Can start immediately after Phase 2.
- **US3 (P2)**: Independently testable with pure-function generator tests. Only the integration in T036/T037 requires US1's `UnitGlyph.buildUnit` to exist.
- **US2 (P2)**: Independently testable by invoking `buildOverlayLayer` directly on fabricated `UnitDisplay` fixtures, but the integration in `SceneBuilder` and `GameViz` needs US1's permanent-layer path wired (T026).
- **US4 (P3)**: Depends on US1 and on US2's `FullNames` overlay.

### Within Each User Story

- Tests for the story MUST be written and FAIL before implementation (T014–T019 before T020–T027; T028–T032 before T033–T037; T038–T042 before T043–T047; T048–T050 before T051–T053).
- Classifier tasks (T020–T022) can proceed in any order; scene composition (T023/T024) depends on all three classifiers.
- `advanceEffects` (T025) depends on `buildUnit` because effect rendering is exercised through `buildUnit`.

### Parallel Opportunities

- Phase 1: T002, T003 in parallel.
- Phase 2: T007/T009/T010/T010a after T004/T005/T006 are in.
- Phase 3 tests: T014, T015, T016, T017, T018, T019 all in parallel — each either touches a distinct file or a distinct xUnit test class.
- Phase 3 implementation: T020, T021, T022 in parallel (pure classifiers, no mutual dependency beyond the shared `UnitGlyph.fs` file — sequence them locally if two developers touch the same file).
- Phase 4 tests: T028–T032 in parallel (single file, distinct tests).
- Phase 5 tests: T038–T042 in parallel.
- Phase 6 tests: T048–T050 in parallel.
- Phase 7: T054, T055, T057 in parallel; T056 sequential after all tests exist.

---

## Parallel Example: User Story 1

```bash
# Launch all US1 tests together (distinct xUnit test classes / fixtures):
Task: "Shape classifier tests in tests/FSBar.Viz.Tests/UnitGlyphTests.fs (T014)"
Task: "Tier classifier tests in tests/FSBar.Viz.Tests/UnitGlyphTests.fs (T015)"
Task: "Faction classifier tests in tests/FSBar.Viz.Tests/UnitGlyphTests.fs (T016)"
Task: "Permanent-layer composition test in tests/FSBar.Viz.Tests/UnitGlyphSceneTests.fs (T017)"
Task: "HP/pip/dashed-construction test in tests/FSBar.Viz.Tests/UnitGlyphSceneTests.fs (T018)"
Task: "Event-effect lifecycle test in tests/FSBar.Viz.Tests/UnitGlyphSceneTests.fs (T019)"

# Launch pure classifier implementations in parallel (coordinate file edits):
Task: "Implement classifyShape in src/FSBar.Viz/UnitGlyph.fs (T020)"
Task: "Implement classifyTier in src/FSBar.Viz/UnitGlyph.fs (T021)"
Task: "Implement classifyFaction in src/FSBar.Viz/UnitGlyph.fs (T022)"
```

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 3)

1. Complete Phase 1: Setup (T001–T003).
2. Complete Phase 2: Foundational (T004–T013) — the compile gate.
3. Complete Phase 3: User Story 1 (T014–T027). Validate the permanent layer visually through `quickstart.md §3` and pass `UnitGlyphTests` + `UnitGlyphSceneTests`.
4. Complete Phase 4: User Story 3 (T028–T037). Commit the label table so US1 acceptance scenario 10 passes end-to-end.
5. **STOP and VALIDATE**: Run `dotnet test` and the FSI walkthrough. If the permanent layer + labels read well on `SceneA/B/C`, MVP is demonstrable.

### Incremental Delivery

1. MVP (US1 + US3) → demoable permanent-layer glyph renderer on synthetic scenes.
2. Add US2 (Phase 5) → demoable overlay composition with sticky hotkeys.
3. Add US4 (Phase 6) → zoom/declutter behavior polished.
4. Phase 7 polish → baseline refresh, perf smoke, quickstart walk, CLAUDE.md update.
5. Follow-up features (out of scope here): live-game adapter for `FSBar.Client.TrackedUnit`, deferred overlays `R E B T V I X`, legacy-path removal once every consumer is on the glyph renderer.

### Parallel Team Strategy

With multiple developers:

1. Pair through Phase 1 + Phase 2 together (single compile unit; coordination tax is low and dependency chain tight).
2. After Phase 2:
   - Developer A: Phase 3 (US1 — classifiers, permanent layer, synthetic adapter).
   - Developer B: Phase 4 (US3 — label generator + generator tests) in parallel, using the stubbed `UnitLabels.generated.fs` until T035.
3. When Phase 3 and Phase 4 are done, a single developer wires T036/T037 to cross the US1⇄US3 seam.
4. Developer A moves to Phase 5 (US2 — overlays + input toggles); Developer B moves to Phase 6 (US4 — zoom clamps + label suppression) once the permanent layer is stable.
5. Phase 7 can be split: baseline refresh + perf test on one side, quickstart walk + CLAUDE.md update on the other.

---

## Notes

- [P] tasks = different files or distinct non-overlapping test cases; no dependency on an incomplete task.
- Tests MUST fail before implementation — this feature ships with explicit xUnit coverage per plan.md §Testing.
- Never mock `BarData` or `FSBar.SyntheticData` per `CLAUDE.md` testing guidance — use the real local-feed `BarData` package and real synthetic scenes.
- Do not touch `FSBar.Client.TrackedUnit`, the live-game adapter, the deferred overlays, or surface-area baselines for projects other than `FSBar.Viz` (quickstart.md §6).
- Commit after each task or small logical group; keep the legacy `OverlayKind.Units` path in place behind the flag until a dedicated follow-up removes it (research.md R5).
