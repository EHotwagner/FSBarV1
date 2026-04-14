// 11-pathing.fsx — Parse Avalanche 3.4 offline via SmfParser, then run an A*
// query for a Kbot from the Player-1 start area to the diagonally-opposite
// corner. Prints waypoints, cost, and status. No running engine needed.
//
// Usage: dotnet fsi scripts/examples/11-pathing.fsx

#load "../prelude.fsx"

open System
open System.IO
open FSBar.Client

let home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
let avalanche = Path.Combine(home, ".local", "state", "Beyond All Reason", "maps", "avalanche_3.4.sd7")

if not (File.Exists avalanche) then
    printfn "Avalanche 3.4 not installed at %s" avalanche
    exit 1

printfn "Parsing %s ..." avalanche
match SmfParser.parseSd7 avalanche with
| Result.Error e ->
    printfn "  SMF parse failed: %A" e
    exit 1
| Result.Ok smf ->
    let grid = SmfParser.toMapGrid smf
    printfn "  %O" grid

    let start = (500.0f, 0.0f, 397.0f)
    let goal = (3699.0f, 0.0f, 3601.0f)
    let budget : PathBudget = { WallClockMs = 500; MaxExpansions = 500_000; SlopeCost = 2.0f }

    printfn "\nfindPath Kbot start=(%.0f, %.0f) goal=(%.0f, %.0f) ..."
        (let (x, _, z) = start in x) (let (x, _, z) = start in z)
        (let (x, _, z) = goal in x) (let (x, _, z) = goal in z)

    let sw = System.Diagnostics.Stopwatch.StartNew()
    match Pathing.findPath grid MoveType.Kbot Seq.empty start goal budget with
    | Result.Error e ->
        printfn "  findPath error: %A (%dms)" e sw.ElapsedMilliseconds
    | Result.Ok path ->
        printfn "  status=%A cost=%.1f elmos waypoints=%d (%dms)"
            path.Status path.EstimatedCost path.Waypoints.Length sw.ElapsedMilliseconds
        printfn "\n  First 5 waypoints:"
        path.Waypoints
        |> Array.truncate 5
        |> Array.iteri (fun i (x, y, z) ->
            printfn "    [%d] (%.0f, %.0f, %.0f)" i x y z)
        if path.Waypoints.Length > 5 then
            let (lx, ly, lz) = path.Waypoints.[path.Waypoints.Length - 1]
            printfn "    ..."
            printfn "    [%d] (%.0f, %.0f, %.0f)" (path.Waypoints.Length - 1) lx ly lz
