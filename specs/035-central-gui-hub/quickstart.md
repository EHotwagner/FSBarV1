# Quickstart — Central GUI Hub App

**Feature**: 035-central-gui-hub
**Date**: 2026-04-17

This quickstart walks a developer through bringing up the hub end-to-end
after the feature is implemented. It is the **per-user-story verification
script** that the `/speckit.tasks` phase will turn into concrete test + manual-
check tasks, and that a reviewer runs to sign off on the PR.

## Prerequisites

- BAR installed at `$HOME/.local/state/Beyond All Reason/` with at least one
  `engine/recoil_*/` subdir.
- A sibling HighBarV2 checkout built (only needed for maintainers refreshing
  the bundled proxy — not for plain users running the hub).
- `dotnet` SDK 10.0 on PATH.
- `DISPLAY` and `XDG_RUNTIME_DIR` set in the shell (required by Silk.NET /
  GLFW — see `CLAUDE.md`).

## Build & launch

```bash
cd ~/projects/FSBarV1
dotnet build FSBarV1.slnx
dotnet run --project src/FSBar.Hub.App
```

The hub window opens with the Setup tab focused. On first run, the First-Run
Wizard is presented instead (US2).

---

## US1 — One-click BAR session from the hub (P1)

1. Select Map = **"Avalanche 3.4"** in the Setup tab.
2. Ally Team 0 → add AI seat, AI = **"HighBarV2"**, Side = **Armada**.
3. Enemy Team 1 → add AI seat, AI = **"BARb"**, Side = **Cortex**.
4. Mode = **Skirmish**, Speed = **1.0x**.
5. Click **Launch**.

**Expected**:
- Status bar transitions `Idle → Starting → Running` within 30 s (SC-002).
- Viewer tab auto-switches and shows the map + both teams' starting units
  within one engine frame after Running (AS-3.1).
- Status bar exposes the Speed slider, Pause, and End-Session controls
  (FR-015b).

**Failure modes to probe**:
- Pick a map name not present under `dataDir/maps/` — Launch button
  disables and a clear message names the missing asset (AS-1.3).
- Click Launch twice — the second click prompts to replace the running
  session (AS-1.4).

---

## US2 — First-run setup (P1)

1. Start fresh (remove `$XDG_CONFIG_HOME/fsbar-hub/settings.json` if it
   exists; optionally also remove `$HOME/.local/state/Beyond All
   Reason/devmode.txt` and the `AI/Skirmish/HighBarV2/` tree for a full
   reset).
2. Launch the hub.

**Expected** (FR-006 / FR-007 / FR-008):
- Wizard step 1 shows the detected BAR data dir; user confirms or overrides.
- Wizard step 2 shows the detected active engine (newest `recoil_*`).
- Wizard step 3 shows the bundled proxy version and asks permission to
  install. On confirm:
  - Copies `proxy/bundled/<v>/{libSkirmishAI.so, AIInfo.lua, AIOptions.lua}`
    into `<engineDir>/AI/Skirmish/HighBarV2/<v>/`.
  - Creates `$dataDir/devmode.txt` if absent.
  - Edits `$dataDir/LuaMenu/Config/IGL_data.lua` to set
    `simpleAiList = false` via the targeted edit (R5).
- Wizard closes; hub lands on the Setup tab with the first-run banner cleared.
- Launching Chobby **outside the hub** shows "HighBarV2" in the skirmish AI
  dropdown (SC-007).

**Idempotency check** (SC-008): re-run the wizard on a machine that was
hand-configured per `docs/bar-info.md`. The wizard should report every step
as **Skipped** (no changes needed), and the file diff for `IGL_data.lua`
should be empty byte-for-byte.

---

## US3 — Embedded live viewer (P2)

With the US1 session running:
1. Press `W`, `L`, `C`, `N` — overlays toggle as in the standalone viewer.
2. Click the corresponding overlay buttons in the hub chrome — same effect,
   and the button-pressed state stays in sync with the hotkey state (FR-017).
3. Switch to Encyclopedia tab, then to Configurator tab, then back to
   Viewer — the viewer resumes without restarting (AS-3.4). The frame pump
   and gRPC stream must not pause.

---

## US4 — Optional original BAR graphical engine (P2)

1. In Setup, toggle **"Launch original BAR viewer"** ON.
2. Launch a session (same config as US1).

**Expected** (FR-014, AS-4.1):
- A separate windowed `spring` graphical window opens alongside the hub.
- Closing the `spring` window does NOT tear down the hub or the session.

**Failure probe** (AS-4.2): rename the engine's `spring` binary temporarily
to simulate it being missing. Launch with the toggle on — Launch must block
and surface a clear message naming the missing binary; session must not
start.

---

## US5 — Encyclopedia (P3)

1. Click the Encyclopedia tab with no session running.
2. Expected: every unit in the current `BarData.AllUnitDefs` appears as an
   entry (SC-003 — count assertion in tests).
3. Filter by faction "Armada" — only Armada units remain; glyphs use the
   Armada palette.
4. Select a unit — detail pane shows cost / health / weapons / build options
   plus the rendered glyph.

**Parity probe**: pick a unit present in the current session and compare the
encyclopedia glyph side-by-side with the viewer's glyph for a live instance
of the same DefId. The two must byte-match (SC-003).

---

## US6 — Configurator (P3)

1. With a session running, open the Configurator tab.
2. Change a color swatch — viewer updates within one frame (AS-6.1).
3. **Save preset** under name `hub-qa`. Close the hub. Re-open — the preset
   is still available and loadable under the same name (AS-6.2).

---

## US7 — gRPC scripting (P3)

Assuming the hub is running with a session active (from US1):

```bash
cd ~/projects/FSBarV1
dotnet fsi src/FSBar.Hub/scripts/examples/03-launch-and-stream.fsx
```

The example script:
1. Connects to `http://127.0.0.1:5021`.
2. Calls `GetSessionStatus` — prints state + client roster.
3. Opens `StreamGameFrames` and prints the first 5 frames' sequence numbers.
4. Sends one no-op command via `SendCommand`.
5. Disconnects cleanly.

**Expected**:
- First frame received ≤ 2 s after connection (SC-004).
- Disconnect does NOT affect the viewer or any other connected clients
  (FR-029, SC-006).

**Multi-client probe**: run five copies of the example script in parallel.
All five must receive every frame emitted after their respective connects;
viewer frame cadence unchanged (SC-005).

**Slow-client probe**: run a script that intentionally sleeps 500 ms between
frame reads. After cumulative drops exceed 32, the hub detaches that client
(`ScriptingClientDetached(OverflowDropLimit)` event visible in the Diagnostics
pane); other clients and the viewer are unaffected.

---

## Maintainer flow: refresh the bundled proxy

```bash
cd ~/projects/FSBarV1
scripts/refresh-bundled-proxy.sh 0.1.18
# reads ../HighBarV2/build/libSkirmishAI.so and proxy/data/AIInfo.lua + AIOptions.lua
# writes proxy/bundled/0.1.18/{libSkirmishAI.so,AIInfo.lua,AIOptions.lua}
# writes proxy/BUNDLED_VERSION ← "0.1.18\n"
git status
# Expect: proxy/bundled/0.1.18/ tracked, proxy/BUNDLED_VERSION modified
```

After commit, users pulling this revision get the new bundled proxy with
zero action on their side — next hub launch will detect the mismatch against
any previous install and offer a one-click remediation (FR-009).

---

## Diagnostics — where to look when things go wrong

| Symptom | First place to look |
|---------|---------------------|
| Hub refuses to launch session | Status bar message + Settings → Diagnostics pane (captures the `HubEvents.DiagnosticsLine` stream) |
| Session dies mid-game | Status bar shows `Failed`; Diagnostics pane includes the infolog excerpt (FR-031) |
| Proxy install step failed | Settings tab's Proxy Status row + Diagnostics pane's `ProxyInstallProgress` trail |
| gRPC client not receiving frames | gRPC tab shows the client row with its cumulative drop count + attached-at time |
| Viewer shows no units | Standard `CLAUDE.md` checklist: heightmap populated? proxy connected? MapGrid built? — same rules as the standalone viewer |
