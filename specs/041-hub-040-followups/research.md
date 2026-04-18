# Phase 0 Research — Feature 041 (Hub 040 follow-ups)

**Date**: 2026-04-18 · **Branch**: `041-hub-040-followups`

The Technical Context in `plan.md` carries no `NEEDS CLARIFICATION`
markers. The three deferred decisions enumerated below are all
implementation-shape questions that the spec's clarifications already
answered at the contract level — research here pins down the
implementation choices that fall out of those clarifications.

---

## R1 — `setminspeed` vs `setmaxspeed` send order

**Question**: The current `AdminChannel.encodeCommandToDatagrams`
sends `/setmaxspeed N` first then `/setminspeed N`, with an in-code
comment claiming this is required so a 5x→0.5x change isn't rejected
by an `argUserSpeed > 0.2f` gate against the OLD min. The
`AdminChannelCodecTests.SetGameSpeed expands to setminspeed plus
setmaxspeed` test asserts the opposite order:
`dg.[0] = "/setminspeed 2"`, `dg.[1] = "/setmaxspeed 2"`. Which is
correct against the live engine?

**Decision**: Send `setminspeed` FIRST, `setmaxspeed` SECOND — match
the test order, change the implementation.

**Rationale**:

- The engine's `argUserSpeed > 0.2f` gate on `/setminspeed` clamps
  *upward* speed changes against the existing min, not the existing
  max. Sending `setminspeed N` first with `N > 0.2` always passes
  this gate when N > 0.2 (which covers every supported speed in the
  Viewer-tab toolbar: 0.5, 1, 2, 5, 10).
- For DOWNWARD changes (e.g. 5x → 0.5x), `setminspeed 0.5` lowers the
  floor immediately; the subsequent `setmaxspeed 0.5` clamps the
  ceiling. The previous "max-first" reasoning was concerned about the
  inverse case (raising speed past the old max), but `setmaxspeed`
  has no symmetric clamp — it just sets the ceiling, so order does
  not matter for upward changes either.
- Live verification on the dev box (admin live tests) shows the
  engine accepts either order as long as both datagrams arrive within
  the same engine tick. Choosing `setminspeed` first is the safer
  default because it makes the engine's effective speed observably
  monotonic for downward changes (no transient ceiling-lowered-but-
  floor-still-high state).
- Aligning with the existing test removes the test/impl conflict
  without inventing new test fixtures.

**Alternatives considered**:

- *Keep max-first, change the test*: rejected because the test was
  written from a deliberate spec contract review (feature 039 plan)
  and the in-code comment that justifies max-first cites a gate that
  doesn't actually fire for the supported speed range.
- *Send both orders (4 datagrams)*: rejected as wasted bandwidth
  with no observable benefit.

---

## R2 — Where to fetch the overlay snapshot inside `HeadlessRenderer`

**Question**: `HeadlessRenderer.create` already takes
`overlays: OverlayLayerStore.T`. Does the per-frame snapshot get
fetched on the encode worker thread, or does the worker take a
callback / channel from the GUI thread?

**Decision**: Fetch the snapshot on the encode worker thread, inside
the per-frame draw closure, immediately after the base-scene encode
prep (before `SKCanvas.DrawPicture` finalizes). One
`OverlayLayerStore.snapshot overlays` call per frame.

**Rationale**:

- `OverlayLayerStore.snapshot` is documented O(total-layers) and
  returns an immutable record (`OverlayLayerSnapshot`). Its layer
  list is pre-sorted by `(ownerId, zHint, uploadedAt)` so the
  renderer can iterate without re-sorting (FR-004).
- Fetching on the worker keeps `OverlayLayerStore`'s thread-safety
  story unchanged — the GUI thread never has to marshal a snapshot.
  The cost (a single immutable copy of the layer index) is bounded
  by FR-026's caps (16 layers/client × N clients × ~32 B per entry),
  which is well under 1 KB even with the FR-026 maximum subscriber
  count (32) — sub-microsecond.
- FR-006 explicitly forbids retaining the snapshot across frames;
  fetching inside the per-frame closure keeps the snapshot's lifetime
  to one rasterization, satisfying that requirement automatically.

**Alternatives considered**:

- *Push snapshots from `OverlayLayerStore` into a per-renderer
  channel*: rejected as overengineered — the renderer already runs
  its own tick loop and "pull on tick" gives identical freshness
  without the channel plumbing.
- *Pre-build a snapshot per layer mutation*: rejected — the GUI
  thread would shoulder the cost, and the snapshot would be stale by
  the time the worker drew it; FR-006 forbids retention anyway.

**Implementation note**: The overrun warning (FR-006a) wraps the
composite call with `Stopwatch.GetTimestamp()` and emits a
`HubEvent.DiagnosticsLine Warning` only when the elapsed
ticks exceed the 5 ms threshold. No new `HubEvents` DU case is
required — the existing `DiagnosticsLine` is sufficient.

---

## R3 — World-coordinate transform for overlays

**Question**: How does `World`-space overlay rasterization stay in
lockstep with the base scene's camera so a circle uploaded at world
(200, 200) lands on the same pixel as a unit at (200, 200)?

**Decision**: Reuse the same `Scene.Transform.Translate` /
`Transform.Scale` chain that `SceneBuilder.buildSceneHeadlessView`
applies to the base scene's world-space group. Concretely: read
`HubStateStore.current().Camera` once at the top of the per-frame
draw closure, build a single `SKMatrix` matching the base scene's
world→screen transform (origin offset + uniform scale), and apply
that matrix to every `World` primitive's geometry before the Skia
draw call. `Screen` primitives draw with the identity transform
(viewport pixels).

**Rationale**:

- The base scene's `SceneBuilder` already documents the camera
  transform as `pixel = (world − Camera.Origin) * Camera.Scale`,
  with the canvas DPR applied uniformly. Mirroring that exact math
  in the overlay pass guarantees the SC-001 acceptance scenario
  (within 1 px tolerance for a same-coordinate primitive).
- Per-primitive matrix application keeps each primitive independent
  — we never `SKCanvas.SetMatrix` for `World` then forget to reset
  for `Screen`, which would silently corrupt subsequent screen
  primitives.
- For `Path` and `Polyline`/`Polygon`, the matrix is applied to each
  `OverlayPoint` once before constructing the `SKPath` / `SKPoint[]`
  — no `SKCanvas.Concat` round-trip per primitive.

**Alternatives considered**:

- *`SKCanvas.Save` + `SKCanvas.Concat` per primitive*: rejected as
  unnecessary state-machine churn — the world primitives are
  geometry-only, no fonts or images that depend on a matrix-aware
  Skia state.
- *Compute the matrix once and `SKCanvas.SetMatrix` for the whole
  world batch*: viable but requires sorting overlays by space first,
  losing the FR-004 `(ownerId, zHint, uploadedAt)` ordering across
  spaces. Per-primitive transform preserves the global order without
  extra bookkeeping.

---

## R4 — UiParity test category and skip semantics

**Question**: Live integration tests sometimes hard-fail on dev boxes
that lack BAR fixtures. How should each new SC-tagged live test skip
gracefully without polluting the "0 failures" criterion (SC-004)?

**Decision**: Each new test method is decorated with
`[<Trait("Category", "UiParity")>]` plus a precondition guard at the
top of the test body that calls into the existing `LiveSession`
fixture-detection helper (engine present, AI binaries present, map
archives present). When the guard returns `false`, the test calls
`Skip.If(true, "<reason>")` from the `Xunit.SkippableFact` extension
already in use elsewhere in `FSBar.Hub.LiveTests`. Skipped tests
count as "N skips" in SC-004; failing tests count as failures.

**Rationale**:

- `[<Trait("Category", "UiParity")>]` is the simplest filter that
  satisfies FR-011 — `dotnet test --filter "Category=UiParity"`
  selects exactly the new matrix and nothing else.
- `Skip.If` (via the `Xunit.SkippableFact` package already in
  `FSBar.Hub.LiveTests.fsproj` per feature 040) is the same skip
  mechanism the existing `LiveHeadlessOrchestrationTests` uses for
  missing-engine cases. Reusing it keeps the ergonomics consistent.
- Granular per-fixture guards (engine, AI, map) let CI dev volumes
  selectively cache fixtures without forcing the whole matrix to
  skip when only one map is missing.

**Alternatives considered**:

- *Single `[<Fact(Skip = "...")>]` blanket skip*: rejected —
  Skip-attributed tests don't run in environments where the fixtures
  ARE present (e.g. nightly CI), defeating the matrix's purpose.
- *Custom `[<UiParityFact>]` attribute that wraps `Skip.If`*:
  rejected — single-call-site skip checks are clearer than custom
  attribute reflection, and the new tests' skip reasons are all
  different (engine vs. AI vs. map).

---

## R5 — Pixel-diff thresholds for SC-003

**Question**: The SC-003 acceptance bar is "≥ 99% of pixels match
per frame". How does the test handle local-environment Skia
glyph-AA differences without flaking?

**Decision**: Drift ≤ 1% MUST pass; drift 1%–5% SHOULD warn (test
passes but emits an xUnit `Output.WriteLine` with the exact percent);
drift > 5% SHOULD skip (`Skip.If(true, "pixel drift > 5% — local
rendering environment mismatch")`). Pixel comparison uses simple
RGBA equality on the encoded PNG decoded back through SkiaSharp;
no perceptual diff library.

**Rationale**:

- FR-013 spells out exactly these three bands; this decision
  formalizes the implementation.
- Pixel-equal comparison after PNG roundtrip is fast and
  deterministic on identical Skia/Linux x86-64 environments. CI dev
  boxes are pinned to the same Skia version (2.88.6 via
  `SkiaSharp.NativeAssets.Linux`), so drift in CI should be 0%.
  The 1%–5% band exists for developer machines with different
  font-config (subpixel hint differences). The > 5% band catches
  truly broken renders (wrong viewport size, wrong scene contents).
- `Skip.If(true, ...)` keeps the test out of the "0 failures"
  count for SC-004 when the developer's local environment is the
  cause.

**Alternatives considered**:

- *Hard-pin AA-affecting settings (typeface hinting off, CIE
  rendering)*: rejected as out-of-scope for this feature — would
  affect the production Viewer rendering as a side effect.
- *Use `pixelmatch` / `image-diff` C# port*: rejected — adds a NuGet
  dependency for a single test path, violates the "no new
  dependencies" constraint.

---

## R6 — `let mutable` removal scope (US4 / SC-006)

**Question**: SC-006 says "zero `let mutable` bindings for fields that
live in `HubStateStore.HubState`". Does that include local function
scopes (e.g. `let mutable rowY = 0.0f` in `SettingsTab.render`'s
layout loop) and tab-internal "presentation-only" mutables (e.g.
`ConfiguratorTab`'s `LastPresetResult` toast which is a UI artifact,
not a HubState field)?

**Decision**: SC-006 applies only to `let mutable` bindings that
mirror a `HubStateStore.HubState` field. Specifically the four
top-level `Program.fs` mutables:

- `let mutable activeTab` → reads from
  `HubStateStore.current().ActiveTab`.
- `let mutable configuratorState` → record narrows to fields NOT in
  `HubStateStore` (Panel scroll position, LastPresetResult toast,
  ActivePreset name); the remaining mutable record is acceptable
  because none of its fields duplicate `HubStateStore.HubState`.
- `let mutable encyclopediaState` → record narrows: drop
  `FactionFilter` and `Selected` (they live in
  `HubStateStore.current().Encyclopedia`); the remaining
  `Entries` + `ListScroll` is presentation-only.
- `let mutable settingsState` → record narrows: drop nothing
  (Status/Health/LastInstallResult/InstallInFlight are all
  ProxyInstaller artifacts, not HubSettings) but wherever the tab
  reads HubSettings it must call `HubStateStore.current().Settings`,
  not a captured local copy.

Local-scope mutables inside `render` / `handleMouse` (loop counters,
chip rect builders) are unaffected — they're presentation-layer
plumbing, not state.

**Rationale**:

- The spec's intent (FR-017 .. FR-023a) is about eliminating drift
  between gRPC writes and GUI reads, not about banning mutability
  in general. Local layout loops can't drift; they reset every
  frame.
- Narrowing the per-tab record (vs. removing it entirely) keeps the
  presentation-layer scratchpad (toasts, scroll positions, status
  spinners) where it belongs while moving the AUTHORITATIVE state
  to `HubStateStore`.

**Alternatives considered**:

- *Remove all per-tab state records*: rejected — toast messages and
  scroll positions are genuinely transient view state with no
  business in `HubState`. Forcing them through the store would add
  events to fan out per scroll wheel tick, which is wasteful.
- *Move scroll positions to `HubState`*: deferred — no requirement
  in this feature, and remote clients have no use for live scroll
  positions today.

---

## R7 — `DiagnosticsLine` emit for rejected mutations (FR-023a)

**Question**: When `HubStateStore.set*` returns `Rejected reason`,
where does the `DiagnosticsLine Warning` originate — the store, or
each tab call site?

**Decision**: Emit from the `HubStateStore` mutator itself,
immediately before returning `Rejected`. The warning carries the
mutator's name plus the reason: e.g.
`DiagnosticsLine (Warning, "HubStateStore.setVizAttribute rejected:
unknown key: foo.bar")`. Tabs do not need to add their own warning;
they only need to silently re-render with the store's authoritative
value (FR-023a's "silent rollback" semantics).

**Rationale**:

- One emit point per rejection avoids double-warning in cases where
  multiple call sites share a mutator.
- The store has the typed `reason` string already; surfacing it
  directly is the highest-fidelity diagnostic.
- Keeps tab code free of `match ... with | Rejected _ -> emit
  warning` boilerplate — each tab just ignores the outcome.

**Alternatives considered**:

- *Tab-side emit with control name*: viable but invites missing-emit
  bugs; the control name can be inferred operationally from the
  immediate-prior `HubEvent` trail (e.g. an `ActiveTabChanged Setup`
  immediately followed by a `DiagnosticsLine Warning` from
  `setVizAttribute` makes the source obvious).
- *Promote `Rejected` to a typed event case
  (`HubEvent.MutationRejected`)*: rejected — would require a
  proto-side mirror for `StreamHubStateEvents`, which expands the
  feature scope to a wire-contract change.

---

## R8 — Coverage-audit (FR-024) source of truth

**Question**: How is the GUI-action → RPC mapping in
`coverage-audit.md` produced — by hand, from a script, or from
existing docs?

**Decision**: Hand-curated table in Markdown form, derived by a
walkthrough of each tab's `handleMouse` / `handleScroll` / keyboard
handler in `src/FSBar.Hub.App/Tabs/*.fs`, cross-referenced against
the RPC list in `proto/hub/scripting.proto`. Each row carries:
`(tab, action label, code site, RPC, FR ref)`. Unmapped actions
explicitly tagged "no RPC — intentional" (e.g. ConfiguratorTab's
local toast dismissal) or "no RPC — gap" (logged for future
features).

**Rationale**:

- A scripted scrape of `handleMouse` action discriminators would
  miss keyboard handlers and chrome-level handlers; hand audit is
  more reliable for a one-time deliverable.
- Storing as Markdown keeps it review-friendly and grep-able.
- The audit is a feature-040 contract artifact, not a build product
  — no need for ongoing automation.

**Alternatives considered**:

- *Generate from `[<Trait("UiAction")>]` test attributes*: rejected
  — would require retrofitting every tab handler with attributes
  for a one-time audit.
- *Embed the table in the proto file as comments*: rejected — proto
  comments are awkward for tabular data and don't survive `buf
  generate` round-trips.

---

## Summary

All Technical Context unknowns resolved. Implementation can proceed
to Phase 1 (data model + contracts + quickstart).
