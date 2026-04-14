// 14-cache-map-analysis.fsx — Run the 024 tactical-map-primitives analysis
// for a BAR map ONCE and write the result to disk so the trainer bot can
// load it at warmup in < 10 ms instead of spending ~250 ms running
// findChokepoints at every match start.
//
// Maps are static, so any analysis that depends only on the .sd7 file is
// a pure function of the file and can be cached. At runtime the bot reads
// bots/trainer/map-cache/<safe-name>.json and uses the pinned chokepoint
// list directly — the live engine never sees the findChokepoints CPU cost.
//
// Usage:
//   dotnet fsi scripts/examples/14-cache-map-analysis.fsx ["Map Name"]
//
// If no map name is given, the script defaults to "Avalanche 3.4".
// The cache file is written to bots/trainer/map-cache/<safe-name>.json
// (non-alphanumeric chars are replaced with _ to match the trainer's
// file-name sanitiser in bot_macro.fsx).
//
// Prereq: the .sd7 file must be installed under the standard BAR maps path
// (~/.local/state/Beyond All Reason/maps/). The script walks that directory
// and matches the first .sd7 whose name, normalised, equals the requested
// map. If you have multiple versions installed, rename or pass the full
// filename explicitly.

#load "../prelude.fsx"

open System
open System.IO
open System.Text.Json
open FSBar.Client

// ---------------------------------------------------------------------------
// CLI args
// ---------------------------------------------------------------------------

let cliArgs =
    fsi.CommandLineArgs
    |> Array.skip 1  // drop script path
    |> Array.toList

let requestedMap =
    match cliArgs with
    | name :: _ -> name
    | [] -> "Avalanche 3.4"

printfn "Caching tactical analysis for map: %s" requestedMap

// ---------------------------------------------------------------------------
// Find the .sd7
// ---------------------------------------------------------------------------

let home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
let mapsDir = Path.Combine(home, ".local", "state", "Beyond All Reason", "maps")

let sanitise (s: string) : string =
    String(s.ToLowerInvariant()
           |> Seq.map (fun c -> if Char.IsLetterOrDigit(c) || c = '.' then c else '_')
           |> Seq.toArray)

let sd7Path =
    if not (Directory.Exists mapsDir) then
        printfn "ERROR: %s does not exist" mapsDir
        exit 2
    let target = sanitise requestedMap
    Directory.GetFiles(mapsDir, "*.sd7")
    |> Array.tryFind (fun path ->
        let stem = Path.GetFileNameWithoutExtension(path)
        sanitise stem = target
        || sanitise stem = target.Replace("_", "")
        || sanitise (stem.Replace("_", " ")) = sanitise requestedMap)
    |> Option.defaultWith (fun () ->
        printfn "ERROR: no .sd7 in %s matches '%s'. Available:" mapsDir requestedMap
        Directory.GetFiles(mapsDir, "*.sd7")
        |> Array.iter (fun p -> printfn "  %s" (Path.GetFileName p))
        exit 2)

printfn "Found map archive: %s" sd7Path

// ---------------------------------------------------------------------------
// Parse + analyse
// ---------------------------------------------------------------------------

printfn "Parsing SMF ..."
let parseSw = System.Diagnostics.Stopwatch.StartNew()
let smf =
    match SmfParser.parseSd7 sd7Path with
    | Result.Error e ->
        printfn "ERROR: SmfParser.parseSd7 failed: %A" e
        exit 3
    | Result.Ok v -> v
parseSw.Stop()
printfn "  Parsed in %dms — %dx%d heightmap" parseSw.ElapsedMilliseconds smf.WidthHeightmap smf.HeightHeightmap

let grid = SmfParser.toMapGrid smf

// Base centre is a fixed function of the map + player-1 start slot. For
// Avalanche 3.4 and the rest of the ladder it's the canonical top-left
// start area around (500, 397) in elmo coords. Other maps may need an
// override — the cache key includes the base centre so it's not lost.
let baseCentre : float32 * float32 * float32 = (500.0f, 0.0f, 397.0f)

let query =
    { Chokepoints.defaultChokepointQuery MoveType.Kbot with
        MaxWidthElmos = 240.0f
        SearchRadiusElmos = 5500.0f }

printfn "Running findChokepoints ..."
let sw = System.Diagnostics.Stopwatch.StartNew()
let cps = Chokepoints.findChokepoints grid baseCentre query
sw.Stop()
printfn "  Found %d chokepoints in %dms" cps.Length sw.ElapsedMilliseconds

for cp in cps do
    let (px, _, pz) = cp.Position
    printfn "    id=%A pos=(%.0f,%.0f) width=%.0f distFromBase=%.0f"
        cp.Id px pz cp.WidthElmos cp.DistanceFromBase

// ---------------------------------------------------------------------------
// Serialise to JSON
// ---------------------------------------------------------------------------

let (bcx, bcy, bcz) = baseCentre

let chokepointEntries =
    cps
    |> List.map (fun cp ->
        let (px, py, pz) = cp.Position
        let (ox, oz) = cp.OutwardDir
        let (ChokepointId idNum) = cp.Id
        dict [
            "id", box idNum
            "position.x", box px
            "position.y", box py
            "position.z", box pz
            "widthElmos", box cp.WidthElmos
            "outwardDir.x", box ox
            "outwardDir.z", box oz
            "distanceFromBase", box cp.DistanceFromBase
        ])

let payload =
    dict [
        "mapName", box requestedMap
        "sd7Path", box sd7Path
        "widthHeightmap", box smf.WidthHeightmap
        "heightHeightmap", box smf.HeightHeightmap
        "widthElmos", box smf.WidthElmos
        "heightElmos", box smf.HeightElmos
        "baseCentre.x", box bcx
        "baseCentre.y", box bcy
        "baseCentre.z", box bcz
        "query.maxWidthElmos", box query.MaxWidthElmos
        "query.searchRadiusElmos", box query.SearchRadiusElmos
        "chokepoints", box chokepointEntries
        "generatedAtUtc", box (DateTime.UtcNow.ToString("o"))
    ]

let json =
    let opts = JsonSerializerOptions()
    opts.WriteIndented <- true
    JsonSerializer.Serialize(payload, opts)

let cacheFile =
    let safe = sanitise requestedMap
    let repoRoot =
        let here = __SOURCE_DIRECTORY__
        Path.GetFullPath(Path.Combine(here, "..", ".."))
    Path.Combine(repoRoot, "bots", "trainer", "map-cache", safe + ".json")

Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)) |> ignore
File.WriteAllText(cacheFile, json)
printfn "\nWrote cache to %s" cacheFile
printfn "File size: %d bytes" ((FileInfo cacheFile).Length)
