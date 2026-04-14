// 13-plan.fsx — Resolve the default Armada opening plan against Avalanche 3.4
// using FSBar.Client.BasePlan. Prints slot resolutions with [ok] / [fail] prefixes.
// Requires BAR installed; no running engine needed.
//
// Usage: dotnet fsi scripts/examples/13-plan.fsx

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

    // Derive a toy metal-spot array from the SMF metal map. Real deployments
    // would use the engine's getMetalSpots callback; for this offline demo we
    // just pick a handful of (x, z) cells whose metal value exceeds a threshold.
    let metalSpots =
        let mw = Array2D.length1 smf.MetalMap
        let mh = Array2D.length2 smf.MetalMap
        let found = ResizeArray<float32 * float32 * float32 * float32>()
        // metalMap is at half heightmap resolution (1 cell = 16 elmos).
        for z in 0 .. mh - 1 do
            for x in 0 .. mw - 1 do
                let v = smf.MetalMap.[x, z]
                if v > 30uy then
                    let wx = float32 x * 16.0f + 8.0f
                    let wz = float32 z * 16.0f + 8.0f
                    found.Add((wx, 0.0f, wz, float32 v / 100.0f))
        // Sort by distance from base (500, 397) and take the top 4.
        let baseX, baseZ = 500.0f, 397.0f
        found
        |> Seq.sortBy (fun (x, _, z, _) ->
            let dx = x - baseX
            let dz = z - baseZ
            dx * dx + dz * dz)
        |> Seq.truncate 4
        |> Seq.toArray
    printfn "  metal spots (top 4): %d" metalSpots.Length
    for (x, _, z, v) in metalSpots do
        printfn "    (%.0f, %.0f) metal=%.2f" x z v

    let context : ResolveContext =
        { Grid = grid
          BaseCentre = (500.0f, 0.0f, 397.0f)
          CommanderPos = (500.0f, 0.0f, 397.0f)
          MetalSpotsNearest = metalSpots
          Chokepoints = []
          UnitDefs = UnitDefCache.empty
          ExistingStructures = []
          Progress = BasePlan.emptyPlanProgress }

    printfn "\nresolvePlan defaultArmadaOpening:"
    let resolved = BasePlan.resolvePlan BasePlan.defaultArmadaOpening context
    for r in resolved do
        match r.Position, r.Failure with
        | Some (x, _, z), None ->
            printfn "  [ok]   %s (%s) @ (%.0f, %.0f)" r.Slot.Name r.Slot.DefName x z
        | _, Some f ->
            printfn "  [fail] %s (%s) — %A" r.Slot.Name r.Slot.DefName f
        | None, None ->
            printfn "  [skip] %s (%s) — already consumed" r.Slot.Name r.Slot.DefName
