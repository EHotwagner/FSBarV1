# Contract — Admin-Channel SetGameSpeed Wire Format

**Module**: `FSBar.Client.AdminChannel`
**Feature ref**: 041 US2 (FR-007 / FR-008 / FR-009 / FR-010)
**Authoritative spec**: `specs/039-hub-admin-channel/contracts/autohost-wire.md`

This document is the contract for the fix-forward `SetGameSpeed`
encoding in feature 041. Decoder semantics on inbound autohost
events are unchanged.

---

## Scope

`AdminChannel.encodeCommandToDatagrams (SetGameSpeed speed)` MUST
produce a 2-element `byte[][]` whose UTF-8 decoding satisfies the
following invariants.

The public surface in `src/FSBar.Client/AdminChannel.fsi` does NOT
change. This is a behaviour-only fix.

---

## Datagram Order (FR-008)

```
result.[0] = "/setminspeed " + speedText
result.[1] = "/setmaxspeed " + speedText
```

`setminspeed` MUST be sent FIRST, `setmaxspeed` SECOND. Rationale
in `research.md` §R1: matches the existing test contract and
preserves observable monotonicity for downward speed changes.

---

## `speedText` Format (FR-007)

The shortest textual representation that round-trips to the same
`float32`. Equivalent to F#'s `sprintf "%g"` applied to the
`float32` value, with the additional invariant that whole-number
values render WITHOUT a decimal point or trailing zeros.

| Input `speed: float32` | `speedText` |
|---|---|
| `0.5f` | `"0.5"` |
| `1.0f` | `"1"` |
| `1.5f` | `"1.5"` |
| `2.0f` | `"2"` |
| `5.0f` | `"5"` |
| `10.0f` | `"10"` |
| `0.25f` | `"0.25"` |
| `0.1f` | `"0.1"` (NOT `"0.10000000149011612"`) |

Negative speeds and `NaN` / `Infinity` are out of scope for the hub
(the toolbar only emits 0.5 / 1 / 2 / 5 / 10) and the encoder MAY
produce `%g`'s default rendering for those values without explicit
contract.

---

## Surface-Area Baseline (FR-009)

`tests/FSBar.Client.Tests/Baselines/AdminChannel.baseline` MUST be
regenerated via `SURFACE_AREA_UPDATE=1 dotnet test
tests/FSBar.Client.Tests --filter "FullyQualifiedName~SurfaceAreaTests"`
once after this feature's source edits land. The regenerated
baseline MUST match `src/FSBar.Client/AdminChannel.fsi`
byte-for-byte. CI subsequently asserts this on every build.

The currently-red `SurfaceAreaTests AdminChannel` test passes
once the regenerated baseline is committed. No `.fsi` changes are
required.

---

## Decoder (FR-010)

`AdminChannel.decodeEvent` is unchanged for inbound events
(`ServerStarted`, `ServerQuit`, `ServerStartPlaying`,
`ServerGameOver`, `GameWarning`, `PlayerChat`, `Unknown`). These
are inbound (engine → hub), not the outbound text the encoder
produces, so they are unaffected by this fix.

For the OUTBOUND wire (hub → engine), there is no decoder in the
hub's process — the engine parses the UTF-8 datagram via
`PushAction`. The "decoder rejects old format with `ParseError`"
clarification in spec §FR-010 applies to ANY future codec round-
trip helper; this feature does not introduce one. Pure fix-forward
means: every hub in the fleet upgrades atomically via the
`FSBar.Hub` / `FSBar.Client` package bump that ships with this
feature, and any hub still emitting the old `setmaxspeed`-first
ordering produces wire bytes that the engine still accepts (engine
parses both orders today; the choice is about which order WE
canonically emit).

---

## Test Evidence

The fix is covered by the existing test file
`tests/FSBar.Client.Tests/AdminChannelCodecTests.fs`, no new tests
required:

- ``KillServer encodes to /kill`` — already green
- ``Pause true encodes to /pause 1`` — already green
- ``Pause false encodes to /pause 0`` — already green
- ``SetGameSpeed expands to setminspeed plus setmaxspeed`` — turns
  green after this fix (asserts min-then-max order with `2` not
  `2.0`)
- ``SetGameSpeed fractional formats without trailing zeros`` —
  turns green after this fix (asserts `0.5` for `0.5f`)
- ``SayMessage emits raw text without slash prefix`` — already green
- ``SayMessage preserves unicode`` — already green
- ``encodeCommand returns first datagram for backwards compatibility``
  — flips meaning: the "first" datagram is now `/setminspeed N`,
  not `/setmaxspeed N`. The test currently asserts only on
  `Pause true`, so the `encodeCommand` contract for `SetGameSpeed`
  is unspecified by this single existing test. We add no new
  behavioural test for `encodeCommand SetGameSpeed` because
  callers needing both datagrams MUST use
  `encodeCommandToDatagrams` per the existing in-code comment.

Surface-area test: `tests/FSBar.Client.Tests/SurfaceAreaTests.fs`
covers `AdminChannel` already; baseline regeneration is the only
action needed to flip it green.
