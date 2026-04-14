// 10-smf.fsx — Parse a BAR .sd7 archive offline via FSBar.Client.SmfParser and
// print dimensions + heightmap range. Requires BAR to be installed at the standard
// path; no running engine needed.
//
// Usage: dotnet fsi scripts/examples/10-smf.fsx

#load "../prelude.fsx"

open System
open System.IO
open FSBar.Client

let home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
let avalanche = Path.Combine(home, ".local", "state", "Beyond All Reason", "maps", "avalanche_3.4.sd7")

printfn "Installed .sd7 archives:"
SmfParser.listInstalledMaps ()
|> List.iter (fun p -> printfn "  %s" p)

printfn "\nParsing %s ..." avalanche
match SmfParser.parseSd7 avalanche with
| Result.Error e ->
    printfn "  Error: %A" e
    exit 1
| Result.Ok smf ->
    printfn "  Dimensions: %d x %d heightmap (%d x %d elmos)"
        smf.WidthHeightmap smf.HeightHeightmap smf.WidthElmos smf.HeightElmos

    let mutable mn = Single.MaxValue
    let mutable mx = Single.MinValue
    let w = Array2D.length1 smf.HeightMap
    let h = Array2D.length2 smf.HeightMap
    for x in 0 .. w - 1 do
        for z in 0 .. h - 1 do
            let v = smf.HeightMap.[x, z]
            if v < mn then mn <- v
            if v > mx then mx <- v
    printfn "  Heightmap range: min=%.1f max=%.1f (%d samples)" mn mx (w * h)

    let grid = SmfParser.toMapGrid smf
    printfn "  MapGrid: %O" grid
