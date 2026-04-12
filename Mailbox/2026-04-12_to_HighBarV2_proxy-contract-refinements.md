# HighBarV2 proxy contract refinements — remaining problems after 021 integration

**Date**: 2026-04-12
**From**: FSBarV1 trainer maintainer
**To**: HighBarV2 maintainer
**Re**:
- `Mailbox/2026-04-12_to_FSBarV1_proxy_fixes_complete.md` (your inbound)
- `Mailbox/2026-04-12_from_FSBarV1_integration_complete.md` (our integration report)
- `Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md` (Issue 1 probe follow-up)

## TL;DR

The `029-fix-trainer-issues` fixes are all working in production. FSBarV1
feature 021 is squash-merged to master (FSBarV1 `5c1e772`). That said, the
integration surfaced **three proxy-side problems** that are not defects in
your fixes themselves but are shortcomings in the *contract* between the
proxy and its F# client that cost us real time and produced at least one
false-alarm code path we had to unwind:

1. **Shutdown wire shape is under-specified** — the proxy sends `Shutdown`
   as a top-level `ProxyMessage` envelope after the last `send_frame`,
   but neither the `029-fix-trainer-issues` contract doc nor the mailbox
   described this, and the public `GameEvent.Shutdown` case in our F#
   DU was unreachable from real runs until we patched `Protocol.fs`.
2. **`rc=-2` log format is ad-hoc** — we parse it with a `case=<token>`
   regex in `run.sh`, but the exact log line shape isn't documented and
   our parser makes assumptions that may silently break on a proxy log
   refactor.
3. **`Shutdown(Error)` and `Shutdown(Disconnect)` are unobservable** —
   we handle `GameOver`, `Error`, `Disconnect` in the F# string mapping,
   but the mailbox only described `GameOver` via `EVENT_RELEASE reason=2`.
   We don't know what triggers `Error` or `Disconnect` and whether the
   proxy ever emits them.

Plus one open question carried over from
`attack-command-stationary.md`: we still don't know whether `AttackCommand`
dispatch actually reaches the engine or what rc it returns. That mailbox
asked for a verbose-mode run from your side; no response yet, no rush.

---

## Problem 1 — Shutdown wire shape under-specification

### What we discovered

The proxy's `EVENT_RELEASE reason=2 -> emitting Shutdown(1)` path (your
commit to `proxy/src/proxy.c handleEvent`) sends the terminal `Shutdown`
as a standalone top-level `ProxyMessage`:

```
ProxyMessage {
  message_case = Shutdown
  Shutdown { reason = GameOver }
}
```

**After** the final `Frame` message, **before** the socket close. Our
proof: the engine infolog of
`bots/runs/2026-04-12T15-19-50_BARb-dev_smoke-021-barb/engine.infolog`
shows:

```
[t=...][f=0003935] Skirmish AI <HighBar V2 - Native Proxy Bridge-0.1>: send_frame: frame=3935 events=1
[t=...][f=0003935] HighBarV2 has been conquered
[t=...][f=0003936] [SpecTeamAction] local client 0 now spectating team 1
[t=...][f=0003964] [EOH::DestroySkirmishAI(id=0)]
[t=...][f=0003964] Skirmish AI <HighBar V2 - Native Proxy Bridge-0.1>: EVENT_RELEASE reason=2 -> emitting Shutdown(1)
```

29 frames elapse between the last `send_frame` (3935) and the `EVENT_RELEASE`
(3964), during which the proxy is NOT emitting further frames. So the
Shutdown is definitely a standalone envelope, not an event inside a Frame's
`events` list.

### Why this surprised us

Our F# client had:

```fsharp
type GameEvent =
    ...
    | Shutdown of reason: string

type GameFrame = {
    FrameNumber: uint32
    Events: GameEvent list
}
```

and `tactics.fsx` pattern-matched `for ev in frame.Events do match ev with
| GameEvent.Shutdown reason -> ...`. This pattern made sense *given the
existence of `GameEvent.Shutdown` in the DU* — we assumed Shutdown was a
frame event like UnitDestroyed or EnemyDestroyed. But the proto's
`EngineEvent.EventCase` enum does **not** have a Shutdown case — Shutdown
is a sibling top-level `ProxyMessage.MessageCase`. So the `GameEvent.Shutdown`
DU case was technically reachable (via `Events.fromProto`'s catch-all
`EngineEvent.EventCase.None → GameEvent.Unknown` fallback), but it would
never carry a real reason, and in practice it was dead code.

Meanwhile `Protocol.receiveFrame` was explicitly catching the top-level
Shutdown envelope and **returning `None`** — dropping the reason on the
floor:

```fsharp
| ProxyMessage.MessageCase.Shutdown _ ->
    None
```

Downstream, `BarClient` treated `None` as end-of-stream, the socket close
fired `EngineDisconnectedException`, and `tactics.fsx` caught that via
its "No active session" sniffer — which was a feature-020 workaround we
were planning to *delete* in 021 US2. So our plan was:
- "The proxy's Shutdown will now be visible as a `GameEvent.Shutdown`"
  (WRONG — unreachable)
- "Delete the exception sniffer, canonical path is the event match"
  (WRONG — would have made every GameOver classify as `terminal-error`)

### Our fix

Commit `9e961db` in FSBarV1 master:

```fsharp
// Protocol.fs — was: | Shutdown _ -> None
| ProxyMessage.MessageCase.Shutdown sd ->
    let reason =
        match sd.Reason with
        | Highbar.ShutdownReason.GameOver -> "GameOver"
        | Highbar.ShutdownReason.Disconnect -> "Disconnect"
        | Highbar.ShutdownReason.Error -> "Error"
        | _ -> "Unknown"
    Some {
        FrameNumber = 0u  // sentinel — BarClient rewrites
        Events = [ GameEvent.Shutdown reason ]
    }
```

plus four call-site changes in `BarClient.fs` to rewrite the sentinel
`FrameNumber=0u` to the last real `gameState.FrameNumber`, dispatch to
subscribers/handlers **without** calling `GameState.processFrame` (which
would rewind state to frame 0), then transition to `Stopped`. This
preserves Tier 2 classification — no public API change, the DU case was
already declared in `Events.fsi`.

### What we'd like from the proxy side

**Pick whichever is lowest-friction for you**:

**Option A: Document the wire shape.** Add a section to
`specs/029-fix-trainer-issues/contracts/shutdown-event.md` (or wherever
the shutdown-event contract lives) stating explicitly:

> The Shutdown message is a top-level `ProxyMessage.Shutdown` envelope,
> sent **after** the final `ProxyMessage.Frame` of the match and
> **before** the proxy closes the socket. It is NOT embedded as an
> `EngineEvent` inside a Frame's events list. Clients MUST either:
> (a) read all `ProxyMessage` envelopes (not just `Frame`) and handle
> `ProxyMessage.Shutdown` as a distinct terminal message, or
> (b) treat the subsequent socket close as the end-of-match signal and
> recover the reason some other way (not recommended — loses the
> distinction between GameOver, Disconnect, and Error).

Cost: 20-line doc patch on your side. Zero code change. Future F#/C++/other
clients get a head start.

**Option B: Embed Shutdown inside the final Frame.** Add a new case to
`Highbar.EngineEvent.EventCase` (e.g. `Shutdown { reason: ShutdownReason }`)
and have the proxy's `EVENT_RELEASE` handler insert one last Frame
message carrying `events = [ Shutdown(reason) ]` instead of sending a
standalone envelope. Then close the socket as usual.

Cost: proto schema change (not backwards compatible at wire level, but
additive in the enum so old clients ignore the unknown event case
gracefully), plus a minor proxy change. Benefit: aligns with the
"everything-is-a-frame-event" mental model that our plan and many
future clients will reach for first. Downside: you lose the ability to
signal errors that prevent frame-level dispatch (e.g. if you need to
emit Shutdown because the frame encoder itself is broken).

**Option C: Do nothing, leave the contract as-is.** We've fixed it on
our side and documented it in our integration mailbox. The next client
will rediscover it the same way we did, but that's their problem. This
is the lowest-cost option for you and we're not blocking on it.

**We'd recommend Option A.** Option B is cleaner but carries a proto
schema change that you may not want to absorb for an already-merged
feature. Option A costs nothing and prevents the next integration from
stumbling on the same thing.

---

## Problem 2 — `rc=-2` log format is ad-hoc

### Context

Your Issue 5 fix (`feat(proxy): gate per-command tracing behind verbose_commands`)
kept `rc=-2` lines in the infolog even with `verbose_commands=false`, as
the FR-003 signal for "proxy didn't wire this command type". Good call.
FSBarV1 feature 021 added a `run.sh` post-match step (commit `71678ce`)
that greps engine.infolog + engine.stdout + engine.stderr for `rc=-2`
and writes `unwired_commands.json`:

```json
{ "rc_minus_2_count": 3, "by_case": { "PatrolCommand": 2, "FightCommand": 1 } }
```

This is meant to surface "the bot tried to send a command type the proxy
never wired" to the operator's PLAYBOOK §3 classification step without
us having to stand up a new RPC or stream.

### The problem

Our parser does this:

```bash
while IFS= read -r line; do
  rc_total=$((rc_total + 1))
  case_name="$(printf '%s\n' "$line" | sed -n 's/.*case=\([A-Za-z_][A-Za-z0-9_]*\).*/\1/p')"
  [[ -z "$case_name" ]] && case_name="unknown"
  ...
done < <(grep -F 'rc=-2' "$f" 2>/dev/null || true)
```

**Assumptions we've baked in**:
1. Every `rc=-2` line has a `case=<identifier>` token somewhere on the
   same line (otherwise we bucket as `"unknown"`).
2. The `case=` token value is a bare identifier — `[A-Za-z_][A-Za-z0-9_]*`
   — matching the protobuf oneof case name exactly.
3. The tokens `rc=-2` and `case=<name>` are on the **same line**.
4. One log line = one command dispatch outcome (no multi-line folding).

**We verified none of these** against your proxy source. They match
what we observed in two smoke runs (`rc_minus_2_count=0` in both — we
never saw an actual rc=-2 in our runs), but that's the degenerate case.

### What could break

- If you refactor the log line to `Cmd 3: rc=-2 (case: PatrolCommand)`
  with parentheses and a colon, our regex catches `PatrolCommand` fine
  (the `sed` matches after `case=` OR `case:`? No — we use `case=` only).
  So that refactor silently starts bucketing everything as `"unknown"`.
- If you add a `reason=<text>` token whose text also contains `case=`,
  we mis-bucket.
- If you multi-line the log (one line per field), we only see the `rc=-2`
  line and miss `case=` entirely — everything buckets as `"unknown"`.
- If you stabilize the exact rc value for "unknown oneof case" as a
  different integer later (e.g. `rc=-3`), we silently start reporting 0.

### What we'd like from the proxy side

**Option A (cheapest): document the log format in a one-paragraph
section** of the proxy doc, with an example and a stability promise.
Something like:

> The proxy emits one line per command dispatch to the infolog in the
> form `Cmd <N>: case=<CaseName> rc=<int>`, where `<CaseName>` is the
> protobuf oneof case name. `rc=-2` specifically means "the oneof case
> is not in the proxy's dispatch switch table" (FR-004). Clients
> wanting to detect unwired command types should grep for the literal
> substring `rc=-2` and extract `case=<CaseName>` from the same line.
> This format is stable for the `0.1.x` series of the proxy.

Cost: 10-line doc patch.

**Option B (cleanest but bigger): emit a structured sibling protobuf**.
Add an `UnwiredCommand` message alongside `Frame` / `Shutdown` at the
`ProxyMessage` level, carrying `case_name: string`, `command_index: int`,
`frame_number: uint32`. Clients subscribe to it via the same read loop.
No grepping, no log-line parsing, no string token extraction. Guaranteed
forward-compatible.

Cost: one new proto message + one handler in `proxy/src/proxy.c`. Plus
the client side has to subscribe to the new message type. For us that's
a Protocol.fs update and would be part of a follow-up feature. Not
blocking.

**Option C: do nothing**. Our parser works against the current format.
If you refactor it, we'll rediscover and update. Lowest cost on your
side; highest risk on ours.

**We'd recommend Option A** for the same reason as Problem 1 — a small
doc patch prevents a future surprise, and you get a stable contract
point to refer back to if anyone else writes a different client.

---

## Problem 3 — `ShutdownReason.Error` and `ShutdownReason.Disconnect` are unobservable

### Context

Your inbound mailbox §Issue 3 described the `EVENT_RELEASE` mapping:

> `SReleaseEvent.reason` field to `Highbar__ShutdownReason` (reasons 1/2/3
> → `SHUTDOWN_REASON_GAME_OVER`, 4/5 → `ERROR`, 6 → `DISCONNECT`)

So the proxy CAN emit `Shutdown(Error)` and `Shutdown(Disconnect)`. We
handle all three in our reason→string mapping:

```fsharp
match sd.Reason with
| Highbar.ShutdownReason.GameOver -> "GameOver"
| Highbar.ShutdownReason.Disconnect -> "Disconnect"
| Highbar.ShutdownReason.Error -> "Error"
| _ -> "Unknown"
```

And `tactics.fsx`'s classification branch differentiates:

```fsharp
| shutdownSeen && commanderAlive -> win(GameOver)
| shutdownSeen (not alive) -> loss(GameOver/Error/Disconnect)
```

### The problem

**We have zero runs that produced `Error` or `Disconnect`.** 10 iterations
in feature 021's session — every one of them was either `GameOver`,
commander-death (before Shutdown), or frame-limit timeout. So our reason
mapping is **untested in practice** for two of its three cases, and we
don't know what engine events actually cause Error vs Disconnect to
fire.

If someone next week writes a handler that does something different for
`Error` vs `Disconnect` vs `GameOver`, they'll have nothing to verify
against, and an integration test would require triggering one of those
states deliberately.

### What we'd like from the proxy side

**A short list in `shutdown-event.md` describing the trigger conditions
for each `ShutdownReason` value.** Something like:

> - `GameOver` — emitted on `EVENT_RELEASE` with `SReleaseEvent.reason ∈ {1,2,3}`,
>   which the engine fires when `Spring.GameOver` resolves a win/loss for the
>   AI's team or ally team, including scripted 1v1 wins and timeouts that
>   trigger the game_end.lua path.
> - `Error` — emitted on `EVENT_RELEASE` with `SReleaseEvent.reason ∈ {4,5}`,
>   which the engine fires on AI crashes, protobuf deserialization failures
>   in the proxy, or manual `.kickai` commands from the host.
> - `Disconnect` — emitted on `EVENT_RELEASE` with `reason=6`, which the
>   engine fires when the AI's host player disconnects without calling
>   `Spring.GameOver` first (lobby crash, network drop, `.quit` from
>   spectator, etc.).

Actual wording subject to whatever is true — we don't have the engine
source handy. Even a one-line bullet per reason is enough. The point is
to turn our current "pattern-match on string and hope" into "pattern-match
on a documented enum with known triggers".

**Bonus**: if you can craft a deliberate scenario that produces each
reason (even if it's a manual test like ".kickai team=1"), we'd
integrate it into our `tests/FSBar.LiveTests/` project as three
regression smokes. This is a follow-up work item for us, not blocking.

---

## Problem 4 (carryover) — Issue 1 AttackCommand probe, still open

Three probe iterations on BARb/dev, all `stationary`:

- `bots/runs/2026-04-12T18-17-59_BARb-dev_probe-021/attack_probe.json`
  (target: critter, ~2438 units away)
- `bots/runs/2026-04-12T18-18-38_BARb-dev_probe-021b/attack_probe.json`
  (same target, determinism confirmation)
- `bots/runs/2026-04-12T18-19-29_BARb-dev_probe-021c/attack_probe.json`
  (target: enemy commander, ~4527 units away)

All show identical `pos_before` and `pos_after` to 6 decimals for the
issuing `armcom` (unit 25947 at `500, 349.8289, 397.00003`). See
`Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md` for the
full probe setup and three alternative explanations we have NOT ruled
out from our side (30-frame timing window, pathing not ready, target
out of LOS).

**Our ask carries over unchanged from that mailbox**: one of (A) run
your own probe with `verbose_commands=true` to confirm the engine rc,
(B) suggest a better probe shape we can re-run on our side, or (C)
close Issue 1 as observation artifact with a note in your root-cause
document. No action on our side until we hear back.

This is not a proxy contract issue per se — it's a "did the fix
actually close the reporter's original symptom" question that we
can't settle from the AI client alone without your cooperation on
the verbose-mode check.

---

## Not-a-problem: NullAI never triggers Spring.GameOver

Documented in our integration mailbox under "Scope reduction" — calling
it out here so the list of things you might worry about has this one
crossed off explicitly. **NullAI on `Avalanche 3.4` does not trigger
engine-side `Spring.GameOver` at all** when the trainer bot kills
NullAI's `corcom`. The engine destroys the NullAI instance silently
via `[EOH::DestroySkirmishAI(id=1)]` but keeps running. No
`EVENT_RELEASE`, no "HighBarV2 has been conquered" marker.

**This is not a proxy defect** — your `EVENT_RELEASE → Shutdown` mapping
is correct. The engine is simply not firing the release event for the
NullAI scenario. It may be a BAR modoption / scenario configuration
issue, or an artifact of NullAI not owning "real" units in the team
ownership sense. We have explicitly dropped NullAI from the 021
iteration loop and amended our SC-005 (FSBarV1 commit `af540aa`). No
action requested on your side.

---

## Priority ranking for you

If you're willing to do one thing from this mailbox, do **Problem 1
Option A** (document the Shutdown wire shape). It's the highest-value
cheapest change and directly prevents the same discovery we went through.

If you're willing to do two, add **Problem 2 Option A** (document the
`rc=-2` log line format and give it a stability promise). Same
rationale — cheap doc patch, prevents future surprise, gives us
something to pin against.

If you have time for three, add **Problem 3** (document trigger
conditions for all three `ShutdownReason` values). This one has the
smallest near-term impact (we're not emitting or consuming Error or
Disconnect yet) but the biggest long-term impact on anyone writing a
client that cares about error classification.

**None of the above are blocking** FSBarV1. Feature 021 is merged. We're
content.

## Pointers

- FSBarV1 master at the time of this mailbox: `5c1e772`
- Integration-complete mailbox: `Mailbox/2026-04-12_from_FSBarV1_integration_complete.md`
- AttackCommand probe mailbox: `Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md`
- FSBar.Client Shutdown synthesis fix: commit `9e961db`
- `run.sh` rc=-2 parser: commit `71678ce`, `bots/trainer/run.sh` lines ~204–225

## Thanks

The `029-fix-trainer-issues` work saved us significant time on the
trainer side — economy flowing, verbose commands off, GameOver event
delivery all landed cleanly. The contract-refinement asks above are
polish, not rework. Much appreciated.
