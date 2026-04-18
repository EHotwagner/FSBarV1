# Quickstart — Feature 041 (Hub 040 follow-ups)

**Date**: 2026-04-18 · **Branch**: `041-hub-040-followups`

End-to-end commands for a developer or reviewer to validate every
acceptance scenario in `spec.md` against a freshly built repo. Run
each section in order; later sections assume earlier sections have
left the working copy in a building state.

---

## Prerequisites

- Working copy clean on branch `041-hub-040-followups` after the
  feature's source edits land.
- `dotnet build FSBarV1.slnx` succeeds.
- `~/.local/state/Beyond All Reason/` populated with a recent
  engine, `HighBarV2` + `BARb` AIs, and the maps Avalanche 3.4 +
  Red Comet Remake 1.8 + Titan v2 (live tests skip on missing
  fixtures per FR-012).
- `XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0` exported for
  any GUI Hub launches.

---

## US2 — Admin-channel codec returns to green (≤ 30 s)

```bash
cd /home/developer/projects/FSBarV1
dotnet test tests/FSBar.Client.Tests \
  --filter "FullyQualifiedName~AdminChannelCodecTests" \
  --logger "console;verbosity=normal"
```

**Expected**: `Passed: 17, Failed: 0, Skipped: 0` (or whatever the
total count is after the codec fix; the spec-stated target is the
two formerly-red codec tests now green plus all previously-green
tests still green).

Then regenerate the surface baseline once and re-run the surface
test:

```bash
SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Client.Tests \
  --filter "FullyQualifiedName~SurfaceAreaTests"
git diff tests/FSBar.Client.Tests/Baselines/AdminChannel.baseline
dotnet test tests/FSBar.Client.Tests \
  --filter "FullyQualifiedName~SurfaceAreaTests"
```

**Expected**: baseline diff is non-empty if and only if `.fsi`
intent shifted (it should not in this feature); surface test green
on the second run.

Full `FSBar.Client.Tests` count target per SC-003: 272/272 green.

---

## US1 — Overlay primitives appear on rendered frames (≤ 5 min)

Start the Hub headlessly with the screenshot harness so we can
diff frames programmatically:

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
FSBAR_HUB_AUTO_LAUNCH=1 \
FSBAR_HUB_INITIAL_TAB=Viewer \
FSBAR_HUB_SCREENSHOT_DIR=/tmp/fsbar-041-overlay/ \
FSBAR_HUB_SCREENSHOT_WAIT_MS=8000 \
  dotnet run --project src/FSBar.Hub.App
```

Once the Hub exits (it self-terminates after the screenshot), drive
the overlay walkthrough script in a fresh shell:

```bash
dotnet fsi src/FSBar.Hub/scripts/examples/21-hub-overlay-layers.fsx
```

**Expected (per Acceptance Scenario 1)**: the script's first
`GetRenderFrame` capture (after `PutLayer` of a `World`-space
circle at world (200, 200)) contains a non-background pixel at the
camera-transformed location of (200, 200) within ±1 px and the
circle's color matches the uploaded `OverlayStyle.StrokeColorRgba`
within 2 RGB steps (SC-001).

**Expected (Scenario 2)**: pan the camera via `SetCamera`; the
script's next capture shows the screen-anchored label at pixel
(20, 20) unchanged.

**Expected (Scenario 3)**: ordering by `(ownerId, zHint,
uploadedAt)` matches the documented sort. Two layers from two
clients render with the lower-ownerId's layers first.

**Expected (Scenario 4)**: after `ClearLayers`, the next frame's
PNG MUST NOT contain any of the cleared client's primitives.

**Expected (Scenario 5 / SC-002)**: while subscribed at 10 Hz with
maximum legal load (16 layers × 500 primitives), watch the Hub's
stderr / `DiagnosticsLine` event stream. Occasional
`HeadlessRenderer overlay composite over budget` warnings are
acceptable; sustained warnings (> 5% of frames) indicate the cap
matrix needs revisit (out of scope for this feature, logged for
follow-up per spec Edge Case 1).

---

## US3 — UiParity live matrix runs to completion (≤ 20 min)

```bash
cd /home/developer/projects/FSBarV1
dotnet test FSBarV1.slnx \
  --filter "Category=UiParity" \
  --logger "console;verbosity=normal" \
  --logger "trx;LogFileName=uiparity-2026-04-18.trx"
```

**Expected (SC-004 of this spec)**: ≤ 20 minutes wall-clock,
0 failures, N skips counting only tests whose fixtures are absent.
Per-test expectations:

- `LiveHeadlessOrchestrationTests` `[<Theory>]` over the three
  reference maps: ≥ 19/20 successful launches per map (SC-001).
- `LiveRenderFrameStreamTests`: 20 captured frames at 10 Hz with
  ≥ 99% pixel match vs the local Viewer's identical-time captures
  (SC-003); P95 stream-end-to-end latency ≤ 200 ms (SC-008).
- `LiveHubStateEventTests` two-client convergence: a third actor's
  `SetVizAttribute` arrives in both client streams within one
  render frame (SC-005).
- `LiveOverlayLayerTests` `PutLayer → frame contains primitive` ≤
  100 ms (SC-009); disconnect cleanup ≤ 2 frames (SC-010).
- `LivePresetRoundtripTests` save → load round-trip < 500 ms
  (SC-004 of feature 040).

---

## US4 — Tab-state routes through `HubStateStore` (≤ 2 min)

Start the Hub graphically and a scripting client side by side:

```bash
# Terminal 1
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
FSBAR_HUB_INITIAL_TAB=Style \
  dotnet run --project src/FSBar.Hub.App

# Terminal 2 (after Hub is up on port 5021)
dotnet fsi src/FSBar.Hub/scripts/examples/19-hub-vizconfig-drive.fsx
```

**Expected (Acceptance Scenario 1)**: when the script calls
`SetVizAttribute("overlays.weaponRanges", true)`, the Style tab's
weapon-ranges toggle reads "on" within one frame and stays "on"
across subsequent frames (no GUI revert).

Repeat for `SelectUnit` and `SetHubSettings` per Scenarios 2 and 3.

**SC-006 grep**:

```bash
grep -n "let mutable" \
  src/FSBar.Hub.App/Tabs/ConfiguratorTab.fs \
  src/FSBar.Hub.App/Tabs/EncyclopediaTab.fs \
  src/FSBar.Hub.App/Tabs/SettingsTab.fs \
  src/FSBar.Hub.App/Program.fs
```

**Expected**: zero matches whose right-hand side is a field that
lives in `HubStateStore.HubState` (R6 from research.md scopes this
narrowly — local layout counters are fine). Manual review confirms.

**SC-007 probe**: see `sc-006-probe.md` for the recorded
line-count exercise.

---

## US5 — Polish audits (≤ 30 min review time)

Reviewer opens the four Markdown deliverables:

```bash
cd /home/developer/projects/FSBarV1/specs/041-hub-040-followups
ls -la \
  coverage-audit.md \
  sc-006-probe.md \
  quickstart-walkthrough.md
```

For the fsdoc refresh:

```bash
# Per CLAUDE.md / Constitution §7
# Run the FSDOC_AGENT skill against the widened modules.
# Verify zero "missing doc" warnings (SC-009).
```

**Expected**:

- `coverage-audit.md` lists every user-facing tab action mapped
  to a feature-040 RPC, with every unmapped action explicitly
  categorized (FR-024 / SC-008).
- `sc-006-probe.md` records the exact lines-changed count for the
  one-attribute extensibility exercise (FR-025 / SC-007).
- fsdoc output covers 100% of `HubStateStore`, `HeadlessRenderer`,
  `OverlayLayerStore`, `ScriptingHub`, `SessionManager`,
  `HubEvents`, `HubSettings` public members (FR-026 / SC-009).
- `quickstart-walkthrough.md` documents the manual run of feature
  040's `quickstart.md` with timings (FR-027).

---

## Cleanup

```bash
rm -rf /tmp/fsbar-041-overlay/
git checkout tests/FSBar.Client.Tests/Baselines/AdminChannel.baseline
# (only if SURFACE_AREA_UPDATE=1 left an unintended diff)
```

---

## Failure Signals

- Any `AdminChannelCodecTests` red after the codec fix → revisit
  R1 / FR-007 / FR-008 ordering decisions.
- Sustained `DiagnosticsLine Warning` from `HeadlessRenderer` →
  R2 snapshot strategy may be wrong, or the per-primitive Skia
  draws are slower than projected; profile via `dotnet-trace`
  collected over a 60-second 10 Hz capture at max load.
- GUI tab reverts a remote write within a frame → US4 refactor
  missed a tab-local mirror; grep for `HubStateStore.current`
  call sites in the affected tab and add the missing read.
- UiParity matrix takes > 20 minutes → check whether
  `LiveRenderFrameStreamTests` is over-sampling frames; the
  20-frame budget is sufficient for SC-003 / SC-008.
