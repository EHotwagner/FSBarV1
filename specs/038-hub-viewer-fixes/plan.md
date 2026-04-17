# Implementation Plan: Hub Viewer Fixes

**Branch**: `038-hub-viewer-fixes` | **Date**: 2026-04-17 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/038-hub-viewer-fixes/spec.md`

## Summary

Four targeted fixes to the FSBar Hub GUI:

1. **Viewer-tab glyph parity** — replace the placeholder `UnitDisplay`
   construction in `SceneBuilder.gameStateToSnapshotWith` with a proper
   `UnitDefCache`-driven adapter shared between Viewer, Units-tab
   encyclopedia, and Style-tab preview (FR-001, FR-002).
2. **Start paused** — persist a `StartPausedDefault` flag in
   `HubSettings`, issue `/pause` via `BarClient.SendCommands` on the
   first `Running` transition when enabled, and add a Viewer-tab
   pause/unpause button backed by a new `SessionManager.TogglePause`
   (FR-003, FR-004, FR-004a, FR-004b).
3. **Live graphical engine option** — add a Setup-tab checkbox wired
   to the already-present `HubSettings.LaunchGraphicalViewerDefault`
   and flip `EngineConfig.Mode` in `LobbyConfig.toEngineConfig`; error
   clearly when `ActiveEngine.GraphicalBin = None` (FR-005, FR-006,
   FR-006a, FR-007, FR-008).
4. **Direction triangle** — swap the ellipse facing pip in
   `UnitGlyph.fs` for a small triangle rotated by heading; suppress on
   non-rotating structures; render triangle-up on static previews
   (FR-009, FR-010, FR-010a).

Technical approach in full: [research.md](./research.md).

## Technical Context

**Language/Version**: F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints)
**Primary Dependencies**: Existing in-repo only — `FSBar.Hub`,
`FSBar.Hub.App`, `FSBar.Viz`, `FSBar.Client`, `FSBar.Proto`, `FSBar.SyntheticData`;
`SkiaViewer 1.1.3-dev` (local nupkg), `SkiaSharp 2.88.6`,
`Grpc.AspNetCore 2.67.0` (unchanged from 035), `BarData` (NuGet local
feed), `xUnit 2.9.x`. **No new NuGet dependencies.**
**Storage**: `$XDG_CONFIG_HOME/fsbar-hub/settings.json` — one additive
boolean field (`StartPausedDefault`), no schema-version bump. No new
on-disk formats.
**Testing**: xUnit 2.9.x unit tests in `tests/FSBar.Viz.Tests/` and
`tests/FSBar.Hub.Tests/`; live integration test in
`tests/FSBar.Hub.LiveTests/` that exercises the `/pause` chat command
round-trip against a real `spring-headless`. Surface-area baseline
tests must be regenerated for every modified `.fsi`.
**Target Platform**: Linux x86_64 (BAR engine target). Hub GUI uses
SkiaViewer + OpenGL/Silk.NET.
**Project Type**: Desktop GUI application with an embedded gRPC
scripting service (unchanged from 035).
**Performance Goals**: Viewer-tab render at ≥ 60 fps matching
pre-feature behaviour (FR-006a — no rate change under graphical
engine). Glyph construction must be O(units) per frame; the adapter
does a single `UnitDefCache.lookupName` per unit (already the case
in `GameViz.buildDisplayUnits`).
**Constraints**: Glyph parity must be byte-exact across surfaces (SC-001
measured by screenshot diff, not approximate comparison). Pause must
take effect before ≥ 10 s of wall time elapses (SC-002). Graphical
launch reachable in ≤ 3 clicks (SC-003).
**Scale/Scope**: Single-user desktop app, ≤ 1 active match at a time,
~hundreds-to-low-thousands of units per frame (trainer-scale matches).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### §I Spec-First Delivery

- ✅ Feature has a spec (`spec.md`) with four prioritised user stories,
  measurable success criteria, and documented clarifications.
- ✅ This plan maps every FR to a concrete implementation handle in
  `research.md`. Tasks will group under US1–US4 with 1:1 FR traceability.
- ✅ Changes are **Tier 1**: modify public `.fsi` surfaces
  (`HubSettings`, `SessionManager`, `SceneBuilder`, plus a new
  `UnitDisplayAdapter.fsi` and `EncyclopediaData.fsi`). Full artifact
  chain (spec, plan, `.fsi`, surface baselines, tests, docs) is
  required.

### §II Compiler-Enforced Structural Contracts

- ✅ Every new public module gets a matching `.fsi`:
  `UnitDisplayAdapter.fsi`, `EncyclopediaData.fsi`.
- ✅ Modified modules' `.fsi` files ship in the same PR:
  `HubSettings.fsi` (+1 field), `SessionManager.fsi` (+2 members,
  changed `Launch` signature), `SceneBuilder.fsi` (+ `defCache`
  parameter on two functions).
- ✅ Surface-area baselines regenerated for
  `tests/FSBar.Hub.Tests/Baselines/{HubSettings,SessionManager}.baseline`
  and `tests/FSBar.Viz.Tests/Baselines/{SceneBuilder,UnitDisplayAdapter,EncyclopediaData}.baseline`.
- ✅ No `private` / `internal` modifiers in non-generated source per
  repo convention; `.fsi` gates all visibility.

### §III Test Evidence Is Mandatory

- ✅ Each user story has dedicated test coverage:
  - US1: `UnitDisplayAdapter` shared-path tests + Viewer glyph
    screenshot baseline in `tests/FSBar.Hub.Tests/Baselines/ViewerGlyph.*.png`.
  - US2: `HubSettings` round-trip test for `StartPausedDefault`;
    `SessionManagerTests` new-test for `TogglePause` state transitions;
    live `PauseLiveTest` in `tests/FSBar.Hub.LiveTests/` that launches
    headless, asserts clock stalled for ≥ 5 s, toggles, asserts clock
    advances.
  - US3: `LobbyConfigTests` — `toEngineConfig` returns `Graphical`
    mode when setting is on; failure path test when graphical binary
    is missing.
  - US4: `UnitGlyphTests.FacingTriangle` — snapshot path commands at
    heading 0 / π/2 / π / 3π/2; structure-shape suppression test.
- ✅ All tests MUST fail pre-implementation and pass post-implementation.

### §IV Observability and Safe Failure Handling

- ✅ Every state transition that matters already emits a `HubEvents`
  entry (`SessionPaused`, `StateChanged`); feature 038 reuses them.
- ✅ FR-008 explicitly forbids silent fallback when graphical engine
  launch fails — error surfaces through `SessionManager.Launch`'s
  existing `Result<unit, string>` and lands on the Setup-tab status
  area.
- ✅ The `/pause` chat command is the hub's pause mechanism per
  research.md §R2; the known drift-from-engine limitation is called
  out in the quickstart so operators know the recovery.

### §V Scripting Accessibility

- ✅ `FSBar.Hub`'s FSI prelude at `src/FSBar.Hub/scripts/prelude.fsx`
  stays loadable; no new types flow through FSI. New
  `UnitDisplayAdapter` + `EncyclopediaData` modules in `FSBar.Viz`
  pick up the existing `FSBar.Viz` prelude automatically; add one
  example script (`scripts/examples/NN-unit-display-adapter.fsx`) to
  demonstrate building a `UnitDisplay` from a BarData entry.

**Gate status**: ✅ All five principles pass pre-Phase-0 and remain
passing post-Phase-1 design.

## Project Structure

### Documentation (this feature)

```text
specs/038-hub-viewer-fixes/
├── plan.md                 # This file (/speckit.plan)
├── research.md             # Phase 0 — 7 decisions documented
├── data-model.md           # Phase 1 — entity + state delta
├── quickstart.md           # Phase 1 — manual + automated validation
├── contracts/              # Phase 1 — .fsi signature previews
│   ├── HubSettings.fsi
│   ├── SessionManager.fsi
│   ├── UnitDisplayAdapter.fsi
│   └── SceneBuilder.delta.md
├── spec.md                 # Feature specification (input)
└── tasks.md                # Phase 2 — produced later by /speckit.tasks
```

### Source Code (repository root)

```text
src/
├── FSBar.Proto/            # (no changes this feature)
├── FSBar.Client/
│   ├── EngineLauncher.fs(i)      # No change — launchGraphical already lives here
│   └── EngineConfig.fs(i)        # No change — Mode/EngineBin/AppImagePath all present
├── FSBar.SyntheticData/    # (no changes this feature)
├── FSBar.Viz/
│   ├── UnitGlyph.fs(i)           # ⚙ triangle swaps the ellipse pip (lines 412-425 area)
│   ├── UnitDisplayAdapter.fs(i)  # ★ NEW — shared UnitDisplay constructor
│   ├── EncyclopediaData.fs(i)    # ★ NEW — EncyclopediaEntry moved out of Hub.App
│   └── SceneBuilder.fs(i)        # ⚙ defCache threaded into buildSceneHeadless{View,Sized}
├── FSBar.Hub/
│   ├── HubSettings.fs(i)         # ⚙ +StartPausedDefault field
│   ├── LobbyConfig.fs(i)         # ⚙ toEngineConfig picks Headless vs Graphical
│   └── SessionManager.fs(i)      # ⚙ Launch(+ startPaused), IsPaused, TogglePause, real pause wiring
└── FSBar.Hub.App/
    ├── Tabs/
    │   ├── SetupTab.fs(i)        # ⚙ + "Start paused" and "Launch graphical engine" checkboxes
    │   ├── ViewerTab.fs(i)       # ⚙ + pause button in top-right, defCache passed to buildSceneHeadlessView
    │   └── EncyclopediaTab.fs(i) # ⚙ delegate to UnitDisplayAdapter.ofEncyclopediaEntry
    └── Program.fs                # (minimal — wiring the new SessionManager signature)

tests/
├── FSBar.Viz.Tests/
│   ├── UnitGlyphTests.fs                       # ⚙ + FacingTriangle tests
│   ├── UnitDisplayAdapterTests.fs              # ★ NEW
│   └── Baselines/{SceneBuilder,UnitDisplayAdapter,EncyclopediaData}.baseline
├── FSBar.Hub.Tests/
│   ├── HubSettingsTests.fs                     # ⚙ + StartPausedDefault round-trip
│   ├── SessionManagerTests.fs                  # ⚙ + TogglePause transition test
│   └── Baselines/{HubSettings,SessionManager}.baseline
└── FSBar.Hub.LiveTests/
    └── PauseLiveTest.fs                        # ★ NEW — live pause/unpause against spring-headless
```

Legend: ★ new file / ⚙ modified file. No removals.

**Structure Decision**: Single-project F# solution (`FSBarV1.slnx`)
already in place; feature 038 only edits existing projects and adds
two new `.fsi`/`.fs` pairs to `FSBar.Viz`. No new csproj/fsproj.

## Complexity Tracking

No Constitution Check violations. Table left empty intentionally.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| *(none)*  |            |                                     |
