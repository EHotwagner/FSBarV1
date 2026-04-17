# Quickstart — Hub Viewer Fixes (038)

Validates all four user stories end-to-end against a real BAR install.
Assumes you already have `spring-headless` and (for US3) `spring` under
`~/.local/state/Beyond All Reason/engine/recoil_*/`.

---

## 0. Build the hub

```bash
cd ~/projects/FSBarV1
dotnet build FSBarV1.slnx
```

Expect a clean build; surface-area baselines for `HubSettings`,
`SessionManager`, and `SceneBuilder` must be updated if any `.fsi`
changed — run `SURFACE_AREA_UPDATE=1 dotnet test FSBarV1.slnx` once
after the signature edits land.

---

## 1. Run the hub

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet run --project src/FSBar.Hub.App
```

Window opens on the Setup tab.

---

## 2. User Story 1 — Viewer-tab glyphs match encyclopedia

**Setup**
1. On Setup tab, pick a map and click **Launch**.
2. Wait ~10-30 s for the engine to warm up. Watch the status bar for
   `Running`.

**Verify**
3. Switch to **Viewer** tab. Confirm you see faction-coloured,
   tier-ringed, labelled glyphs rather than the old
   grey-Neutral-"??" blobs.
4. Identify a commander glyph. Switch to **Units** tab and search for
   `armcom` (Armada commander). Click it.
5. Side-by-side: the encyclopedia glyph at the bottom-right of the
   detail pane must have **identical** shape family, label code,
   tier ring, and faction colour to the Viewer-tab commander.
6. Repeat for a builder (`armck`), a combat unit (`armrock`), and a
   structure (`armmstor`). Every pair must match.

**Pass**: SC-001 satisfied — 100% of rendered glyphs match.

**Regression check**: Confirm the Units tab still renders correctly on
its own — encyclopedia selection, filtering, and detail rendering
must be unchanged from pre-038.

---

## 3. User Story 2 — Start paused + Viewer-tab pause button

**Verify default "start paused = on"**
1. Back on **Setup** tab, confirm the **Start paused** checkbox is
   checked (fresh-install default is ON).
2. Click **Launch**. Switch to **Viewer** tab.
3. Game clock should read the same small value (or zero) for 10+ s.
   No unit moves; nothing builds.
4. Click the **⏸ / ▶** button in the Viewer tab's top-right.
5. Clock advances, units begin moving.

**Verify persistence**
6. On Setup tab, uncheck **Start paused**.
7. Quit the hub (`Alt+F4` or close window).
8. Re-launch with the same `dotnet run` command.
9. Confirm the **Start paused** checkbox is still unchecked.
10. Click **Launch** and switch to Viewer. Clock advances immediately.

**Verify per-launch independence**
11. While a match is running unpaused, re-check **Start paused** on
    Setup.
12. Click **Launch** (the hub ends the current session and starts a
    new one). The new match starts paused, confirming FR-004.

**Pass**: SC-002 — clock at 0 for ≥10 s wall time when start-paused
is enabled.

**Known limitation** (per research.md §R2 pick A): if you type
`/pause` in the BAR graphical client while the hub is running, the
Viewer-tab button's displayed state will drift by one click until
you press it. Document the limitation; do not try to fix in 038.

---

## 4. User Story 3 — Graphical engine option

**Setup**
1. On **Setup** tab, check the **Launch graphical engine** (or
   "Launch BAR client") checkbox.
2. Click **Launch**.

**Verify**
3. A standard BAR game window opens in **windowed** mode (not
   fullscreen) per FR-006.
4. Simultaneously, the hub's **Viewer** tab still renders the same
   match with glyphs, overlays, and the same frame cadence as
   headless mode (FR-006a — no rate reduction, no focus suspension).
5. Issue a pause via the Hub's Viewer-tab button. Confirm the BAR
   client's clock also halts (both are the same engine instance).

**Verify failure path (FR-008)**
6. Quit the hub. Rename the graphical `spring` binary temporarily:
   ```bash
   mv ~/.local/state/Beyond\ All\ Reason/engine/recoil_*/spring \
      ~/.local/state/Beyond\ All\ Reason/engine/recoil_*/spring.bak
   ```
7. Re-launch the hub with **Launch graphical engine** still checked.
   Click **Launch**.
8. The Setup-tab status area shows a clear error ("graphical engine
   not installed at …"). **The match does NOT silently fall back to
   headless.**
9. Restore the binary: `mv spring.bak spring`.

**Verify persistence**: same as US2 step 6-9 but for the
graphical-engine checkbox. Factory default is **off** (headless).

**Verify default path unchanged (FR-007)**
10. Uncheck the graphical option. Launch. Confirm behaviour is
    identical to pre-038 — SC-005 satisfied.

**Pass**: SC-003 — graphical launch reachable in ≤3 clicks (tick
checkbox + Launch). Typically 2 clicks after restart.

---

## 5. User Story 4 — Direction triangle

**Static previews**
1. On **Units** tab, select any mobile unit (e.g. `armrock`). The
   encyclopedia glyph on the right shows a triangle whose apex points
   **up** (FR-010a).
2. Select a structure (e.g. `armmstor`). The direction triangle is
   either absent or in the neutral "up" orientation — never pointing
   in a random direction (FR-010).
3. Open the **Style** tab / configurator preview. Same unit glyph
   renders with triangle-up.

**Live heading**
4. Launch a match (US2 or US3 flow). On **Viewer** tab, pick a
   moving unit and watch it turn.
5. The triangle apex tracks the unit's facing direction through the
   cardinals: pointing north as it heads up the map, east as it
   heads right, etc.

**Pass**: SC-004 — cardinal facing identifiable at a glance for
100% of units with meaningful heading data.

---

## 6. Screenshot validation (automated)

Headless Hub screenshot environment variables (from CLAUDE.md) still
work and should cover US1 and US4:

```bash
FSBAR_HUB_SCREENSHOT_DIR=/tmp/hub-screenshots \
FSBAR_HUB_AUTO_LAUNCH=1 \
FSBAR_HUB_INITIAL_TAB=Viewer \
FSBAR_HUB_SCREENSHOT_WAIT_MS=15000 \
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
dotnet run --project src/FSBar.Hub.App
```

Compare output PNGs against baselines in
`tests/FSBar.Hub.Tests/Baselines/ViewerGlyph.*.png`. Glyph-parity
baseline must include at least one unit per faction per tier.

---

## 7. Unit / integration test commands

```bash
# UnitGlyph triangle orientation tests
dotnet test tests/FSBar.Viz.Tests/ --filter 'FullyQualifiedName~FacingTriangle'

# UnitDisplayAdapter shared-path tests
dotnet test tests/FSBar.Viz.Tests/ --filter 'FullyQualifiedName~UnitDisplayAdapter'

# HubSettings round-trip with new field
dotnet test tests/FSBar.Hub.Tests/ --filter 'FullyQualifiedName~HubSettings'

# SessionManager pause wiring (live, launches spring-headless)
dotnet test tests/FSBar.Hub.LiveTests/ --filter 'FullyQualifiedName~PauseLiveTest'
```

All four must pass before merging.
