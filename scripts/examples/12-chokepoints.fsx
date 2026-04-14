// 12-chokepoints.fsx — Find chokepoints on Avalanche 3.4 offline via
// FSBar.Client.Chokepoints. Prints the top results sorted by distance from
// base. Operator-verified chokepoint positions feed back into the SC-003
// reference constant in ChokepointsTests.fs.
//
// Usage: dotnet fsi scripts/examples/12-chokepoints.fsx

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

    let baseCentre = (500.0f, 0.0f, 397.0f)
    // Realistic macro-bot query: Avalanche canyons are ~80–240 elmos wide; search
    // radius covers most of the map from the Player-1 corner (~5200 elmos diagonal).
    let q =
        { Chokepoints.defaultChokepointQuery MoveType.Kbot with
            MaxWidthElmos = 240.0f
            SearchRadiusElmos = 5500.0f }

    printfn "\nfindChokepoints base=(%.0f, %.0f) maxWidth=%.0f radius=%.0f ..."
        (let (x, _, _) = baseCentre in x)
        (let (_, _, z) = baseCentre in z)
        q.MaxWidthElmos q.SearchRadiusElmos

    let sw = System.Diagnostics.Stopwatch.StartNew()
    let cps = Chokepoints.findChokepoints grid baseCentre q
    sw.Stop()
    printfn "  %d chokepoints in %dms" cps.Length sw.ElapsedMilliseconds

    for cp in cps |> List.truncate 10 do
        let (px, _, pz) = cp.Position
        printfn "  Id=%A pos=(%.0f, %.0f) width=%.0f distFromBase=%.0f outward=(%.2f, %.2f)"
            cp.Id px pz cp.WidthElmos cp.DistanceFromBase
            (fst cp.OutwardDir) (snd cp.OutwardDir)
