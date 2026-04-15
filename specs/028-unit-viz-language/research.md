# Phase 0: Research Notes — Unit Visual Representation for SkiaViewer

**Feature**: 028-unit-viz-language
**Date**: 2026-04-15
**Purpose**: Resolve technical unknowns before Phase 1 design.

---

## R1 — `BarData.UnitDef` field accessibility from F#

**Question**: The spec commits to reading `customParams["techlevel"]`, `category`, `subfolder`, and `movement.movementClass` / `movement.canFly` / `canMove` from `BarData.UnitDef`. Are these actually exposed as F# record fields on the compiled `BarData` DLL, with the types we assumed in the spec?

**Decision**: Treat the schema documented in the spec investigation (`BarData.UnitDef` with 29 fields including `subfolder: string`, `customParams: Map<string,string>`, `category: Option<string>`, `movement: MovementDef`) as authoritative for planning. Verify field presence empirically during task T001 by loading a sample unit in FSI and reflecting on the type; if any assumed field is missing or typed differently, amend the data-model and fall back to the spec's documented fallback rules (log + neutral default).

**Rationale**: The earlier Explore-agent investigation already unzipped `nupkg/BarData.1.0.3.nupkg` and enumerated these fields by name. That evidence is strong enough to plan against, but a live FSI check at task T001 is cheap insurance against a field rename between `BarData` versions.

**Alternatives considered**:
- **Reflect at build time in the label generator** — adds complexity and couples planning to tool execution; rejected.
- **Hand-curate all derived fields** (tier, faction, movement class) in one committed table — bypasses `BarData` entirely but freezes the mapping on every `BarData` update, contradicting SC-006; rejected.

---

## R2 — `TrackedUnit` lacks heading and build progress

**Question**: `FSBar.Client.GameState.TrackedUnit` currently exposes `UnitId`, `DefId`, `Position`, `Health`, `MaxHealth`, `IsFinished`, `IsIdle` — no heading, no buildProgress, no stun flag. The spec requires all of these. Where does that data live for the MVP?

**Decision**: Introduce a `UnitDisplay` record in `FSBar.Viz.VizTypes` that is the input to the new renderer, decoupled from `TrackedUnit`. For the MVP, `FSBar.SyntheticData` constructs `UnitDisplay` values directly (or via a light adapter module). `TrackedUnit` is NOT modified in this feature. When the live-game follow-up ships, an adapter module will extend `TrackedUnit` (or supply an adjacent tracker) with the missing fields and convert to `UnitDisplay`.

**Rationale**:
- The clarification session locked MVP to synthetic data only (Q4 → A). The synthetic generator (`FSBar.SyntheticData.UnitSim`) already fabricates unit positions deterministically; adding heading + buildProgress to its output is a local, low-risk change that does not leak into the live `FSBar.Client` surface.
- Decoupling the renderer from `TrackedUnit` keeps `FSBar.Viz` from dragging schema changes back onto the client API. The follow-up live-game wiring can evolve `TrackedUnit` on its own timeline.
- Aligns with Constitution principle II: the renderer's public contract depends only on its own `UnitDisplay` input type, which is declared in `VizTypes.fsi` and baselined.

**Alternatives considered**:
- **Extend `TrackedUnit` now** with nullable heading/buildProgress/statusFlags — bleeds into `FSBar.Client` public API, requires `.fsi` + baseline + tests on a module that isn't otherwise in scope; rejected.
- **Make the renderer consume `TrackedUnit` directly and synthesize defaults for the missing fields** — the spec's acceptance scenarios explicitly test facing and buildProgress, so synthesized defaults would make those tests meaningless; rejected.

---

## R3 — Label generator algorithm

**Question**: Given ~953 `BarData` units, how do we deterministically produce unique 2- or 3-character labels with ≥ 90% at 2 chars (SC-002) and ≥ 95% stability across `BarData` updates (SC-006)?

**Decision**: Two-pass generator.

**Pass 1 — proposal**: For each unit, in deterministic iteration order (sorted by internal name):
1. Strip faction prefix (`arm`, `cor`, `leg`, `scav`, `rap`) from internal name.
2. Let `rest` be the remainder. Iterate candidate pairs of character positions:
   a. `(rest[0], rest[1])` — first two consonants preferred, falling back to first two letters if fewer than two consonants.
   b. `(rest[0], rest[2])`, `(rest[0], rest[3])`, ..., then `(rest[1], rest[2])`, etc.
3. For each candidate, produce the 2-char code as title-cased `Aa` (first letter upper, second lower) — gives a stable case pattern without requiring case-sensitive readability.
4. Accept the first candidate that does not collide with any already-assigned code.
5. If no 2-char candidate is available after exhausting all pairs, fall back to 3 chars using the same walk on triples, then assign a `Aaa` title-cased form.

**Pass 2 — stability**: Before writing the generated table, load the previous `UnitLabels.generated.fs` (if present). For each unit that existed in the previous generation, if its assigned code is still achievable (i.e. not taken by another previously-existing unit), keep the old code — only reassign when a genuine collision forces it. New units fill any remaining codes using Pass 1 logic.

**Rationale**:
- Deterministic iteration order + deterministic candidate walk guarantees byte-identical output for the same inputs (FR-022).
- Stability pass satisfies SC-006 (≥ 95% preserved): the only reassignments are forced collisions where an incumbent's old code is stolen by a more-senior incumbent whose previous slot was taken by a new unit — rare in practice.
- Title-case `Aa` form visually reads as a mnemonic (e.g. `Pw` for Pawn, `Ck` for Construction Kbot, `Mh` for Moho) without forcing the observer to mentally case-normalize.

**Alternatives considered**:
- **Hash-based codes** (e.g. first 2 chars of a stable hash of the name): fast and deterministic but produces unreadable `X7` / `Qz` codes that hurt observer recall. Rejected.
- **Case-sensitive 62² pool** (`Pw` vs `pW`): doubles the pool but hurts readability and adds a case-confusion failure mode at any font size below 16 px. Rejected.
- **Length-minimization ILP**: solves for optimal 2-char assignment globally. Overkill for the scale (953 units) and opaque to debug. Rejected.

**Acceptance evidence**: Unit tests in `tests/FSBar.Viz.Tests/UnitLabelsGeneratorTests.fs`:
1. Running the generator twice on the same `BarData` version produces byte-identical output (FR-022).
2. Every produced code is unique (FR-020).
3. ≥ 90% of produced codes are 2 characters (SC-002).
4. Adding a fabricated new unit to the input set preserves ≥ 95% of previously-assigned codes (SC-006).

---

## R4 — Shape dispatch: where does the `movementClass` rule stack live?

**Question**: The spec commits (Q2) to a rule stack for resolving `movementClass` → one of six shapes. Where does this rule stack live, and how is it tested in isolation from the renderer?

**Decision**: Pure function `UnitGlyph.classifyShape : BarData.UnitDef -> MovementShape` lives in `UnitGlyph.fs`, exposed via `UnitGlyph.fsi`. Unit tests hit it directly with fabricated `UnitDef`-shaped inputs (no rendering involved). The renderer calls it once per `UnitDisplay` at scene-build time.

**Rationale**:
- Separates classification from drawing, so the renderer can be tested for composition while the classifier is tested for rule correctness.
- The classifier is pure and side-effect-free except for a one-shot warning on unknown classes; the warning channel is a function parameter so tests can capture it.

**Alternatives considered**:
- **Inline the rule stack into the renderer** — tangles concerns and makes unit testing awkward; rejected.
- **Lookup table keyed on `movementClass` string** — doesn't handle the `!canMove` → building and `canFly` → air priority rules, which are derived from other fields; rejected.

---

## R5 — Feature-flag legacy path vs. hard cutover

**Question**: `SceneBuilder.buildUnits` at `src/FSBar.Viz/SceneBuilder.fs:128` is the current legacy renderer. Do we replace it in-place or keep both paths?

**Decision**: Keep both paths behind a `VizConfig.UseGlyphRenderer: bool` flag defaulting to `true` for new sessions. The legacy path is marked `obsolete` in `VizTypes.fsi` but not removed. The legacy path's existing tests (`MetalPulseSceneTests`, the unit-rendering assertions inside `SceneBuilderTests`) continue to pass. A follow-up feature removes the legacy path once every consumer (`LiveSession`, `PreviewSession`, REPL examples) is explicitly on the glyph path.

**Rationale**:
- Non-breaking for Constitution principle II — the existing public `SceneBuilder.buildScene` signature doesn't change.
- Mitigates the risk of regressions in unrelated tests (`LiveSessionIntegrationTests`, `SyntheticVizTests`) that happen to exercise the old unit path.
- The flag's default of `true` makes the glyph path the new default for anything that doesn't override `VizConfig`, which is the desired behavior.

**Alternatives considered**:
- **Hard replace**: simpler but risks cascading test churn on day one. Rejected.
- **New top-level function `buildGlyphScene`**: forces every caller to choose, duplicating scene-assembly logic. Rejected.

---

## R6 — Overlay implementation for `W L C N`

**Question**: The four MVP overlays need distinct stroke styles / text rendering so they compose without occluding each other (FR-016). What are the concrete styles?

**Decision**:
- **`W` weapon ranges**: stroked circle at each weapon's max range, solid 1 px stroke, faction stroke color with 0.6 alpha, drawn at the unit's world position.
- **`L` sight**: stroked circle at `sightDistance`, dashed 2-4 stroke pattern, neutral light-gray with 0.5 alpha. Dash pattern distinguishes it from `W` even when both ranges overlap.
- **`C` command queue**: polyline through queued waypoints, 1.5 px stroke, color-coded by order type (move=cyan, attack=red, patrol=yellow, guard=green, build=blue, reclaim=magenta). Current order gets 2x stroke width; remaining queued orders get base width.
- **`N` full names**: text element drawn beside the unit, 10 px font, label layer (above base layer, below UI). Bypasses the zoom-threshold label suppression that applies to default 2-char labels.

**Rationale**: These styles match the existing `SkiaViewer.Scene` primitives (`Scene.ellipse`, `Scene.path`, `Scene.text`) and use the stroke/dash/alpha channels that SkiaSharp already supports in the raster backend. No GPU features required.

**Alternatives considered**:
- **Glowing shaders for overlays**: burns the Scene-primitive budget and hurts SC-004. Deferred to the follow-up that ships the animated overlays (`E T`).

---

## R7 — Performance envelope for 200 units at 30 fps

**Question**: Can the new renderer meet SC-004 given the ~10 primitives per unit budget?

**Decision**: At 200 units × 10 primitives = 2,000 Scene primitives per frame. Existing `SceneBuilder` under the legacy renderer produces ~1,000–1,500 primitives on comparable scenes with the base terrain layer + unit dots + grid and runs at 60+ fps per the `PreviewSessionCyclingTests` evidence. Doubling the unit-side primitive count keeps us comfortably under the observed ceiling. Verify empirically during implementation by adding a dedicated `UnitGlyphPerfTest` that measures frame time on a 200-unit synthetic scene.

**Rationale**: SkiaSharp's raster path in this environment is CPU-bound on path rasterization, not on Scene-tree walks. Ten simple primitives per unit is well inside budget.

**Alternatives considered**:
- **Pre-composited per-unit bitmap sprites**: faster draw but blurs on zoom and complicates the color-per-team story. Rejected.
- **Aggressive LOD at mid zoom** (drop arc, pip, label): deferred to Story 4 (P3 zoom/declutter).

---

## Unknowns now resolved

| From Technical Context | Status | Resolved by |
|---|---|---|
| `BarData.UnitDef` F# field shape | Resolved | R1 (verify empirically at T001) |
| Missing heading/buildProgress on `TrackedUnit` | Resolved | R2 (`UnitDisplay` in `VizTypes`) |
| Label-generator algorithm | Resolved | R3 (two-pass with stability) |
| Where classification lives | Resolved | R4 (pure `UnitGlyph.classifyShape`) |
| Legacy path coexistence | Resolved | R5 (feature flag, default new path) |
| Overlay stroke styles | Resolved | R6 |
| Performance envelope | Resolved | R7 |

No `NEEDS CLARIFICATION` markers remain.
