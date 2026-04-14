# Contract — `FSBar.Client.Commands` queued MoveCommand variant

**Feature**: 025-macro-primitive-driven
**Target file (signature)**: `src/FSBar.Client/Commands.fsi`
**Target file (implementation)**: `src/FSBar.Client/Commands.fs`
**Tier**: 1 (public API surface change on `FSBar.Client.Commands`)
**Maps to**: FR-008, FR-008a; clarification Q1

## Rationale

Feature 024's `Pathing.findPath` returns a list of waypoints. Feature 025 needs the macro bot to dispatch a combat unit through those waypoints so it traces the intended path rather than cutting directly to the target and wasting units on engine-side pathfinding failures. The Spring engine's `SHIFT_KEY` command-option bit (value `32u`, authoritative per HighBarV2 `docs/protocol.md`) is the mechanism: a command issued with `SHIFT_KEY` set appends to the unit's order queue rather than overwriting the current order. The existing `Commands.MoveCommand` helper sets only `INTERNAL_ORDER = 8u` and exposes no way to queue — so research R2 mandates a new public API entry point for this feature.

## Decision

Add **one** new function to `FSBar.Client.Commands`: `MoveCommandQueued`. Keep `MoveCommand` unchanged so every existing call site (023 attack launcher, 023 defend routing, 023 upgrade-phase movement, every non-waypoint call site throughout the codebase) retains byte-identical wire behaviour.

**Rejected alternative**: add an optional `queue:bool` parameter to the existing `MoveCommand`. Would require touching every call site (~30+ references per the existing Commands module usage pattern) and expands the Tier 1 diff for no behavioural benefit. The two-function approach keeps the diff narrow and makes the call-site intent self-documenting at the integration site.

## `.fsi` delta (`src/FSBar.Client/Commands.fsi`)

Insert after the existing `MoveCommand` declaration (currently at line 6):

```fsharp
/// Create a queued move command for a unit to a position. The command is
/// appended to the unit's existing order queue rather than replacing the
/// current order. Use this for waypoint traversal when you need the unit to
/// execute a sequence of moves in order (e.g., routing through a Pathing
/// findPath result's waypoints).
///
/// Wire-level effect: sets both INTERNAL_ORDER (8u) and SHIFT_KEY (32u) on
/// the command's Options bitfield, producing Options = 40u. The first
/// waypoint of a sequence should use the unqueued MoveCommand so it
/// replaces any existing order; subsequent waypoints use MoveCommandQueued.
val MoveCommandQueued: unitId: int -> x: float32 -> y: float32 -> z: float32 -> Highbar.AICommand
```

## `.fs` delta (`src/FSBar.Client/Commands.fs`)

Insert a new `SHIFT_KEY` literal and a new `MoveCommandQueued` function builder after the existing `INTERNAL_ORDER` literal (line 7) and the existing `MoveCommand` builder (lines 14–21) respectively.

```fsharp
    /// SHIFT_KEY flag (bit 5) — queues the command behind the unit's existing orders
    [<Literal>]
    let SHIFT_KEY = 32u

    /// Create a queued move command for a unit to a position (appends to order queue)
    let MoveCommandQueued (unitId: int) (x: float32) (y: float32) (z: float32) : Highbar.AICommand =
        { Command = Highbar.AICommand.CommandCase.MoveUnit {
            UnitId = unitId
            GroupId = 0
            Options = INTERNAL_ORDER ||| SHIFT_KEY
            Timeout = MAX_TIMEOUT
            ToPosition = Some { X = x; Y = y; Z = z }
        }}
```

## Surface-area baseline delta

Refresh the surface-area baseline for `FSBar.Client.Commands`. The baseline file is discovered via the existing baseline test harness on first run; the task to refresh it runs `dotnet test` in `src/FSBar.Client.Tests` with the baseline-update environment variable set (or whatever mechanism the 024-refreshed harness currently uses — see `tests/FSBar.Client.Tests/SurfaceAreaTests.fs` or the equivalent file for the invocation pattern). Expected baseline delta: one new symbol `Commands.MoveCommandQueued` with the signature above. No existing symbols renamed or removed.

## Unit test delta

**File**: `src/FSBar.Client.Tests/CommandsTests.fs` (existing file, new test added).

**New test**: asserts that the queued variant emits `Options = INTERNAL_ORDER ||| SHIFT_KEY = 40u` and that the regular `MoveCommand` still emits `Options = INTERNAL_ORDER = 8u`.

```fsharp
[<Fact>]
let ``MoveCommandQueued sets INTERNAL_ORDER and SHIFT_KEY bits`` () =
    let cmd = Commands.MoveCommandQueued 42 100.0f 0.0f 200.0f
    match cmd.Command with
    | Highbar.AICommand.CommandCase.MoveUnit m ->
        Assert.Equal(Commands.INTERNAL_ORDER ||| Commands.SHIFT_KEY, m.Options)
        Assert.Equal(40u, m.Options)
    | _ -> Assert.Fail "Expected MoveUnit command case"

[<Fact>]
let ``MoveCommand does NOT set SHIFT_KEY bit`` () =
    let cmd = Commands.MoveCommand 42 100.0f 0.0f 200.0f
    match cmd.Command with
    | Highbar.AICommand.CommandCase.MoveUnit m ->
        Assert.Equal(Commands.INTERNAL_ORDER, m.Options)
        Assert.Equal(0u, m.Options &&& Commands.SHIFT_KEY)
    | _ -> Assert.Fail "Expected MoveUnit command case"
```

**Test ordering**: the test MUST fail before the `.fs` edit lands (TDD gate per Constitution §III). The test file already has multiple `MoveCommand`-returns-valid tests — the new tests slot in alongside those.

## Wire-level contract summary

| Function | `Options` bitmask | Intent |
|---|---|---|
| `MoveCommand` | `INTERNAL_ORDER = 8u` | Replaces any existing order; first waypoint of a queued sequence uses this |
| `MoveCommandQueued` | `INTERNAL_ORDER ||| SHIFT_KEY = 40u` | Appends to the unit's order queue; subsequent waypoints of a sequence use this |

## Compatibility impact

- **Backwards compatible**: no existing `Commands` API signature changes. Every current caller of `MoveCommand` continues to compile and behave identically.
- **New symbol only**: surface-area baseline gains one row; no row removed or renamed.
- **Migration guidance**: callers that want waypoint semantics MUST switch from `[ MoveCommand uid x y z ]` to `[ MoveCommand uid firstX firstY firstZ; for (x,y,z) in rest -> MoveCommandQueued uid x y z ]`. This feature uses the pattern at exactly one call site (the attack launcher in `bot_macro.fsx`).
- **No `fsdoc` action required mid-feature**: the `fsdoc` refresh runs after feature-end per Workflow gate 7. Tier 1 change; one new symbol to document.
