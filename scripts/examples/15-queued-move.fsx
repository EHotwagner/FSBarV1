// 15-queued-move.fsx — FSI walkthrough of Commands.MoveCommandQueued
// (feature 025, Tier 1 public-API addition).
//
// Demonstrates the wire-level difference between the unqueued
// MoveCommand (Options = INTERNAL_ORDER = 8u, replaces any existing
// order) and the queued variant (Options = INTERNAL_ORDER ||| SHIFT_KEY
// = 40u, appends to the unit's order queue). The first waypoint of a
// findPath-derived sequence should use MoveCommand to replace any
// existing order; subsequent waypoints use MoveCommandQueued so the
// unit traces the entire path in order.
//
// Usage:
//   dotnet fsi scripts/examples/15-queued-move.fsx

// Direct DLL reference rather than #load prelude.fsx, because the
// prelude's `#r "nuget: FSBar.Viz, *-*"` pulls in the latest packed
// FSBar.Client via FSBar.Viz's transitive closure, and NuGet's
// version-resolution cache can lag behind the debug DLL. For a
// one-function demo we point directly at the just-built debug DLL
// so the demo always exercises the current tree.

// Reference the test-output directory which has all transitive
// dependencies flattened (FsGrpc, FSBar.Proto, FSBar.Client) in the
// same folder — avoids having to list every dependency manually and
// avoids NuGet-cache staleness on the local dev-pack feed.
#r "../../tests/FSBar.Client.Tests/bin/Debug/net10.0/FsGrpc.dll"
#r "../../tests/FSBar.Client.Tests/bin/Debug/net10.0/FSBar.Proto.dll"
#r "../../tests/FSBar.Client.Tests/bin/Debug/net10.0/FSBar.Client.dll"

open FSBar.Client
open FSBar.Client.Commands
open Highbar

let unitId = 42
let waypoints =
    [| 100.0f, 0.0f, 200.0f
       300.0f, 0.0f, 450.0f
       550.0f, 0.0f, 700.0f |]

// Build the command sequence the way bot_macro.fsx does it inside
// emitWaypointCommands: first waypoint unqueued, remaining queued.
let commands =
    [ let (fx, fy, fz) = waypoints.[0]
      yield "first  (unqueued)", MoveCommand unitId fx fy fz
      for i in 1 .. waypoints.Length - 1 do
          let (wx, wy, wz) = waypoints.[i]
          yield sprintf "w%d (queued)  " i, MoveCommandQueued unitId wx wy wz ]

printfn "Commands.INTERNAL_ORDER = %du" Commands.INTERNAL_ORDER
printfn "Commands.SHIFT_KEY      = %du" Commands.SHIFT_KEY
printfn "expected unqueued Options = %du" Commands.INTERNAL_ORDER
printfn "expected queued   Options = %du" (Commands.INTERNAL_ORDER ||| Commands.SHIFT_KEY)
printfn ""
printfn "%-18s %-10s %-20s %-12s" "label" "options" "position" "ref"
for (label, cmd) in commands do
    match cmd.Command with
    | AICommand.CommandCase.MoveUnit m ->
        let pos = m.ToPosition |> Option.get
        let optStr = sprintf "%du" m.Options
        let shiftFlag =
            if m.Options &&& Commands.SHIFT_KEY <> 0u then "SHIFT"
            else "      "
        printfn "%-18s %-10s (%.0f, %.0f, %.0f) %s"
            label optStr pos.X pos.Y pos.Z shiftFlag
    | _ -> ()

printfn ""
printfn "Integration pattern (from bot_macro.fsx emitWaypointCommands):"
printfn "  first waypoint unqueued → replaces any existing order;"
printfn "  remaining waypoints queued → appended to the unit's order queue;"
printfn "  optional trailing MoveCommandQueued to the real target so"
printfn "  Partial budget-exhausted paths still engage at the goal."
