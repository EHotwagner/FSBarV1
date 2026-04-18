# Quickstart — Hub admin/host channel

**Feature**: 039-hub-admin-channel · **Date**: 2026-04-17

End-to-end walkthrough for exercising the admin channel once the
feature ships. Mirror these steps in the live-integration tests
under `tests/FSBar.LiveTests/LiveAdmin*Tests.fs`.

## Prerequisites

- Engine prerequisites pass: `tests/check-prerequisites.sh`.
- A BAR install resolvable by `FSBar.Client.BarInstall.locate`.
- For US4 (admin message), use the graphical engine (`spring`) since
  headless has no visible chat log.

## 1 · US1 — Real pause / resume

1. Start the Hub GUI:
   ```bash
   XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
     dotnet run --project src/FSBar.Hub.App
   ```
2. On the **Setup** tab, leave "Start paused" **unchecked**, click
   **Launch**. Wait for the Viewer-tab to show unit glyphs moving.
3. In the Viewer-tab toolbar (top-right) note the current game clock
   in the status bar. Click **⏸**.
4. Wait 15 s of wall time. The game clock MUST not advance; unit
   positions MUST not change.
5. Click **▶**. Clock advances, units resume motion within 1 s
   (SC-002).

## 2 · US1b — Start paused

1. On Setup, **check** "Start paused", click **Launch**.
2. When the Viewer tab opens, the game clock reads `0` (or the
   engine's lobby-zero equivalent) and stays there until you click
   **▶**.
3. Clicking **▶** starts the match at 1.0x.

## 3 · US2 — Engine speed

1. With a match running at default 1.0x, sample the game clock, wait
   10 s of wall time, sample again — delta ≈ 10 s.
2. Click the **5x** preset. Repeat the 10 s sample — delta ≈ 50 s
   (±10%, SC-003).
3. Type `2.5` into the numeric input, hit Enter. Clock advances at
   roughly 2.5× thereafter.
4. Type `-1` and hit Enter. The numeric field shows a validation
   error; the previous speed is preserved.

## 4 · US3 — Force-end

1. While a match is running, click the **⏹** (force-end) button.
2. Within 5 s the Hub's status bar reads "session ended", the engine
   process has exited, and the Viewer tab returns to the
   placeholder layout (SC-004).
3. Launch a new match — no residual pause-state or speed-state leaks
   from the prior session.

## 5 · US4 — Admin message (graphical engine only)

1. On Setup, enable **Launch graphical viewer** and **Launch**.
2. In the spring window, open the in-game chat log.
3. In the Hub's Viewer-tab admin toolbar, type `test-phase` into
   the message input, hit Enter.
4. `test-phase` appears in the chat log, attributed to
   "Autohost" / engine-native speaker (SC-005). No AI team name.

## 6 · Channel-unavailable surface (edge case, FR-009)

Manual: artificially cause a bind failure by occupying the loopback
port space (rare), or force-kill the engine mid-session.

1. After an engine force-kill, the admin toolbar's control buttons
   render disabled; the status line reads
   `Admin channel lost: socket error: …`.
2. Clicking a disabled button is a no-op; no engine crash; no
   silent "success" feedback.

## 7 · Scripting service

```bash
# From an FSI session with the Hub running:
dotnet fsi scripts/examples/NN-hub-admin.fsx
```

The example script opens a gRPC channel to `127.0.0.1:5021`, calls
`Pause`, `Resume`, `SetEngineSpeed(2.0)`, `SendAdminMessage("hi")`,
and `ForceEndMatch`, printing each `AdminSubmitResult`. Every action
is reproducible by the Viewer-tab controls and vice versa (SC-007).

## 8 · Live-test runner

```bash
cd tests
./run-all.sh --filter "Category=AdminChannel"
```

Asserts US1, US2, US3, and channel-loss recovery against a real
`spring-headless`. US4 is graphical-only and gated on
`FSBAR_GRAPHICAL_OK=1` (opt-in — graphical tests are finicky in CI).
