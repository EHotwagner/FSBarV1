# Mid-game callback RPCs silently drop engine events (including UnitFinished)

**Date**: 2026-04-14
**From**: FSBarV1 trainer maintainer (feature 024 tactical-map-primitives)
**To**: HighBarV2 maintainer
**Severity**: HIGH — breaks FSBarV1's `023-trainer-builder-economy` macro bot post feature 022 integration
**Feature context**: blocks 024 US5 live-iteration validation (T060–T068)
**Related**: `Mailbox/2026-04-12_to_HighBarV2_proxy-contract-refinements.md` (earlier-contract-issues list)

## TL;DR

When the F# client calls any mid-game `Callbacks.*` RPC that goes through
`Protocol.sendCallback`, the proxy's `CallbackResponse` frequently arrives
interleaved with one or more `Frame` messages from the streaming event path.
The F# client's `readUntilCallback` loop handles that by responding with
empty commands and **discarding the frames' events** — a behaviour that
was added as an acknowledged shortcut in FSBarV1 commit `775c73f` (2026-04-12)
and whose comment now reads:

```fsharp
// Engine sent a frame while we're waiting for callback response.
// Respond with empty commands and keep reading. The frame's
// events are intentionally dropped here — the caller is
// expected to drive important event capture through the
// regular frame-reading path (BarClient.WaitFrames) rather
// than relying on bulk-callback batches like UnitDefCache
// loading to preserve them.
```

The FSBarV1 side of the fix is our problem, but the underlying reason the
shortcut exists at all is that the proxy **does not multiplex a response
and a continuous frame stream on the same socket** — it interleaves them,
so the F# client has no way to ask "give me the `CallbackResponse` that
belongs to request id X" without also racing every pending frame.

We are reporting this because:

1. It's a broken FSBarV1↔HighBarV2 contract, not a one-sided client bug.
   The protocol forces us to either block the frame stream (deadlocking
   the engine) or drop events. We chose drop to avoid deadlock.
2. It reproduces on the latest master of both sides and **already broke a
   previously-verified clean-win iteration** (023 iter 026, commit
   `0f03be1` on FSBarV1 master). See §"Reproduction" below.
3. It blocks FSBarV1 feature 024 from live-validating its `bot_macro.fsx`
   deep integration (US5). The 024 static artefacts ship regardless, but
   SC-006 / SC-007 live assertions are stalled until this is fixed.

## Reproduction

Branch: FSBarV1 `024-tactical-map-primitives` head `a9e8423`.
Engine: BAR `recoil_2025.06.19`, content `test-29907-d3b337a`.
Map: `Avalanche 3.4`, rung `NullAI`.

```bash
# Regression-control run: the 023 squash-merge base, same bot.fsx as
# the iter-026 clean-win commit 0f03be1. No 024 content.
git stash
BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 024-baseline-pre-us5
git stash pop
```

**Expected** (per 023 HISTORY iter 026): clean win at f≈21000 with
`cause=commander-death-win-after-upgrade`, `victory_signal=engine-shutdown-gameover`.

**Observed**: `outcome=timeout frames=36010`, `commands_total=1`,
`units_built=0`. `grep -c UnitFinished bots/runs/<dir>/frames.jsonl` returns
**0** across the full 36000-frame match.

Run directory: `bots/runs/2026-04-14T08-29-09_NullAI_024-baseline-pre-us5/`.

The rush bot (`bot.fsx`, which does NOT issue mid-game callbacks) wins
cleanly on the same branch at f=12563 — see `024-rush-smoke` run. The
FR-030 invariant holds, which also rules out engine or proxy setup bugs.

## Root-cause analysis

### 1. The proxy emits `UnitFinished` correctly

Topic counts from `engine.infolog` show the proxy's own
`handleEvent` counter for `EVENT_UNIT_FINISHED` (topic id 6) does increment
between frames 1100 and 1150 of the stalled run:

```
[f=0001000] Event topics at frame 1000: 1=1 3=1001 5=2 6=1 13=8 15=8 25=8 26=8
                                                            ^^^ UnitCreated fired (armmex u14835)
[f=0001100] Event topics at frame 1100: 1=1 3=1101 5=2 6=1 13=8 15=8 25=8 26=8
[f=0001150] Event topics at frame 1150: 1=1 3=1151 5=2 6=2 13=8 15=8 25=8 26=8
                                                            ^^^ UnitFinished fired
```

### 2. The proxy packs `UnitFinished` into a `send_frame`

Sibling `send_frame` log lines show the corresponding batched frame carried
more than one event — i.e., the proxy serialized the UnitFinished and put
it on the wire:

```
[f=0001130] send_frame: frame=1130 events=1
[f=0001140] send_frame: frame=1140 events=1
[f=0001142] send_frame: frame=1142 events=2   ← 1×UPDATE + 1×UnitFinished
[f=0001150] send_frame: frame=1150 events=1
[f=0001157] send_frame: frame=1157 events=3   ← more events packed later
```

### 3. The bot's engine-metrics agree

`bots/runs/<dir>/frames.jsonl` shows the mex actually finished constructing
— metal income jumps from `2` to `3.99` at f=1500 (one mex producing
~2/tick, added to the commander's base income of ~2):

```jsonl
{"frame":1110,"reason":"sampled", ...,"metal":{"current":968.50,"income":2},...}
{"frame":1500,"reason":"sampled", ...,"metal":{"current":1010.87,"income":3.99},...}
{"frame":1890,"reason":"sampled", ...,"metal":{"current":1050.00,"income":3.99},...}
```

The construction **completed**. The `UnitFinished` event fired. The proxy
packed and sent it. The bot never saw it.

### 4. The F# client drops it during a `sendCallback` wait

FSBarV1 `src/FSBar.Client/Protocol.fs` lines 143–169:

```fsharp
let rec readUntilCallback (attempts: int) =
    if attempts > 100 then
        failwith "sendCallback: exceeded 100 attempts waiting for CallbackResponse"
    let respBytes = Connection.recvBytes stream
    let proxyMsg = decode<ProxyMessage> respBytes
    match proxyMsg.Message with
    | ProxyMessage.MessageCase.CallbackResponse resp -> resp
    | ProxyMessage.MessageCase.Frame _ ->
        // Engine sent a frame while we're waiting for callback response.
        // Respond with empty commands and keep reading. The frame's
        // events are intentionally dropped here — the caller is
        // expected to drive important event capture through the
        // regular frame-reading path (BarClient.WaitFrames) rather
        // than relying on bulk-callback batches like UnitDefCache
        // loading to preserve them.
        sendFrameResponse stream []
        readUntilCallback (attempts + 1)
    ...
```

The `| Frame _ ->` branch pattern-matches with a wildcard, extracts
nothing, calls `sendFrameResponse stream []` (empty commands), and
**never invokes `Events.fromProto` on the frame's events**. Any events
carried on that frame are gone.

`sendCallback` can loop up to 100 times discarding interleaved frames
before giving up. In a 100x-speed game, frames arrive every ~1–2ms, so
any callback that takes more than a few milliseconds to round-trip may
consume dozens to hundreds of frames of events.

### 5. The firing sequence that stalls the macro bot

`bots/trainer/bot_macro.fsx` emits an `FR-002 commander-idle-defect`
probe that calls `Callbacks.getUnitPos` when the commander hasn't
received a follow-up command within 300 frames of the last BuildCommand.
For Avalanche Player-1 start on NullAI:

| frame | what happened |
|------:|---------------|
|   ~60 | bot warmup complete, `openingProgress.LastCommandFrame = 60` ish |
|  ~720 | bot issues `BuildCommand armmex (760, 350, 168)` for mex#1 |
|   962 | `UnitCreated u14835` arrives via the normal frame path (proxy sent it in a regular `send_frame events=N`) |
|  1031 | `elapsed = 1031 − 720 = 311 > 300` → idle-defect probe emits `[probe-idle]`, calls `Callbacks.getUnitPos` → `sendCallback` starts |
| ~1142 | proxy's `send_frame frame=1142 events=2` carries `UnitFinished u14835`. The F# client is still inside `readUntilCallback`. Frame is consumed, events silently dropped, empty commands responded. |
|  1500 | bot engine-metrics snapshot: `metal.income = 3.99` — the mex physically exists and is producing |
|  1500 | `openingProgress.AwaitingCreated` is STILL true because the helper advances only on `GameEvent.UnitFinished` |
| 36010 | game hits `max_frames`, trainer writes `outcome=timeout` |

The bot is stuck at `openingIndex=0` for the remaining 35000 frames,
emitting one `[probe-periodic]` per 600 frames (each of which re-opens
the same `sendCallback` drop window for whatever fires in the next
~1–2ms), never able to see any engine event again in the probe windows.

### 6. Why iter 026 (`0f03be1`) won anyway

The drop was introduced on 2026-04-12 in commit `775c73f`. Iter 026
(`0f03be1`) ran on 2026-04-13 — after the drop existed in
`Protocol.sendCallback`. The win was a race-condition lucky path: in
iter 026 the commander's mex finished **before** `elapsed > 300`, so
the idle-defect probe never fired and no `sendCallback` was issued
during the critical window. The 024 replay starts the commander along a
slightly different Bresenham path (same build target, same BAR content
version) that happens to put it on the wrong side of the 300-frame
threshold. Both are within the spec's behaviour envelope — this is a
race that iter 026 won by luck and that re-rolls every run.

## Impact

On the FSBarV1 side this affects **every** caller that does a mid-game
RPC after the 60-frame mini-warmup added in `775c73f`:

- `Callbacks.getUnitPos` (bot_macro.fsx `[probe-idle]`, `[probe-periodic]`)
- `Callbacks.getResourceUsage`
- `Callbacks.getMetalSpots` (if called mid-game — currently only at warmup)
- `Callbacks.getUnitDefName`
- `MapGrid.loadFromEngine` / `refreshLos` / `refreshRadar`
- **Every new feature-024 caller** that wanted to refresh `MapGrid` or
  re-query chokepoints live (we avoided this by pinning at warmup, but
  it closes off a class of legitimate designs)

Effectively: **any call into `sendCallback` in a running match is a gamble
that races the engine's event stream.** The comment in `775c73f` says
"the caller is expected to drive important event capture through the
regular frame-reading path" — but there is no way to do that reliably
while also making mid-game callback RPCs on the same socket.

## Possible fixes — proxy side

None of these is a hard requirement on HighBarV2; we list them so the
maintainer can pick the one that best fits the proxy's architecture.

### Option A — Separate request/response socket

Add a second `PROXY_CALLBACK` socket that only carries
`CallbackRequest`/`CallbackResponse` pairs. The frame stream stays on
the primary socket. The F# client has two `NetworkStream`s and can do
synchronous callback RPCs without blocking or racing the frame stream.

**Pro**: clean separation, no protocol changes beyond a second
`listen()` on a second path.
**Con**: new socket-lifecycle edge cases; two fds to tear down on
shutdown.

### Option B — Batch-with-response semantics

Introduce a new `ProxyMessage.MessageCase.FrameWithResponses` envelope
that carries any pending `CallbackResponse`s packed **inside** the next
`Frame` message. The F# client reads one message per frame, applies the
frame's events, and then dispatches each packed response to the
caller that requested it (by `request_id`).

**Pro**: single socket, no new transport plumbing.
**Con**: request/response becomes asynchronous — callers need to hold
futures and wait for them across WaitFrames ticks. Significant FSBarV1
client rewrite.

### Option C — Request-id routing on the existing socket

Keep the existing socket, but let the F# client **consume incoming
Frame messages during `readUntilCallback` as if they arrived via the
normal path**. Two things need to happen:

1. The proxy must tag each `CallbackResponse` with the `request_id`
   from the originating `CallbackRequest` (already present in the
   current protocol) so the client can correlate out-of-order responses.
2. The F# client needs a per-stream frame buffer so Frame messages read
   during a callback wait are stashed and replayed from the buffer on
   the next `WaitFrames` call, with the bot's normal
   `Events.fromProto` pipeline handling them. No events are dropped.

This is closest to the existing architecture and the one we'd prefer on
the FSBarV1 side. The proxy already has `request_id` in
`CallbackRequest` (see `proto/highbar.proto`), so the only proxy change
is to echo it in `CallbackResponse` if that isn't already the case.

### Option D — Don't interleave

Change the proxy to pause the frame stream between a
`CallbackRequest` arriving and its `CallbackResponse` being sent — i.e.,
serialize the streams at the proxy end instead of the client end. The
client then never sees an interleaved Frame message during a callback.

**Pro**: the simplest client model.
**Con**: introduces engine-side latency — the proxy can't deliver
frames to the client until pending callbacks are answered, which may
delay the F# client's command emission by up to one frame per callback.
At 30 fps that's ≤ 33ms — probably fine for BAR, which already tolerates
100ms-scale command latency.

Our recommendation is **Option C** — it keeps the socket architecture
simple, matches the `request_id` the proto already carries, and doesn't
require a protocol revision on the proxy side if `CallbackResponse`
already includes the id. We can do the FSBarV1 buffer-and-replay part
ourselves.

## Possible fixes — FSBarV1 side (without proxy changes)

If the proxy can't be changed in the short term, we have two workarounds
on our side:

1. **Replay instead of drop.** Change `readUntilCallback`'s `| Frame`
   branch to stash the frame's events in a `ConcurrentQueue<Frame>` on
   the `BarClient`. The next `WaitFrames` call drains the queue before
   reading the socket, so no events are lost. Latency penalty: one
   frame per callback.

2. **No mid-game callbacks.** Ban `Callbacks.*` RPCs from running
   inside `tacticsFn`. All map/unit/game state must be read from the
   `GameState` materialised from `WaitFrames`'s event stream. Bot
   observability probes (`[probe-idle]`, `[probe-periodic]`) can log
   cached `GameState.Units.[cid].Position` but must not call
   `getUnitPos` live.

We will likely do **option 1** in a follow-up FSBarV1 feature so the
existing bot code doesn't need to be audited for mid-game callback use,
but the proper fix is on the proxy side.

## Verified artefacts

Each of the following is reproducible on FSBarV1 master at
`a9e8423`:

- `bots/runs/2026-04-14T08-25-47_NullAI_024-rush-smoke/` — rush bot win f=12563 (FR-030 control)
- `bots/runs/2026-04-14T08-26-32_NullAI_024-macro-smoke/` — 024 US5 macro timeout f=36010
- `bots/runs/2026-04-14T08-29-09_NullAI_024-baseline-pre-us5/` — 023 baseline macro timeout f=36010 (control, pre-US5 code)
- `bots/trainer/HISTORY.md` — iter 024-macro-smoke and 024-baseline-pre-us5 both labelled `[infrastructure-regression] [pre-existing]` with the `BLOCKER:` mailbox line at the bottom of the table

All three run directories contain the full `engine.infolog`, `frames.jsonl`,
and `result.json` — we can ship them over if direct inspection helps.

## Asks

1. **Confirm or reject the root-cause analysis.** Does the proxy in
   fact interleave `CallbackResponse` with `Frame` messages on the
   primary socket under load? If not, where's the actual race?
2. **Pick a preferred fix path** (Options A–D or something we haven't
   thought of) so we can coordinate the FSBarV1 side.
3. **Stopgap acknowledgement**: is it acceptable for FSBarV1 to ship
   the "replay instead of drop" workaround (option 1 above) as a
   patch, and then remove it when the proxy fix lands?

Thanks — we'd like to unblock 024 US5 live iterations before the
feature-024 merge, but we also don't want to misdiagnose a proxy
behaviour that was deliberate.

— FSBarV1 trainer maintainer
