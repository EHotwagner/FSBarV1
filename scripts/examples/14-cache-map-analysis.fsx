// 14-cache-map-analysis.fsx — Run map-analysis primitives once for a BAR map
// and write the result to a committed cache file so the trainer bot can load
// it at warmup via `FSBar.Client.MapCacheFile.read` instead of re-parsing the
// .sd7 every time.
//
// Usage:
//   dotnet fsi scripts/examples/14-cache-map-analysis.fsx ["Map Name"]
//
// If no map name is given, the script defaults to "Avalanche 3.4".
//
// Exit codes:
//   0 — refreshed successfully
//   2 — map not in MapCacheFile.supportedMaps, or general error
//   3 — map's .sd7 is not installed on this machine (skip)

#load "../prelude.fsx"

open System
open System.IO
open FSBar.Client

let cliArgs =
    fsi.CommandLineArgs
    |> Array.skip 1
    |> Array.toList

let requestedMap =
    match cliArgs with
    | name :: _ -> name
    | [] -> "Avalanche 3.4"

printfn "Caching tactical analysis for map: %s" requestedMap

let supported =
    match MapCacheFile.tryFindSupportedMap requestedMap with
    | Some s -> s
    | None ->
        printfn "ERROR: \"%s\" not in MapCacheFile.supportedMaps" requestedMap
        exit 2

let home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
let mapsDir = Path.Combine(home, ".local", "state", "Beyond All Reason", "maps")

let sanitise (s: string) : string =
    String(s.ToLowerInvariant()
           |> Seq.map (fun c -> if Char.IsLetterOrDigit(c) || c = '.' then c else '_')
           |> Seq.toArray)

let sd7Path =
    if not (Directory.Exists mapsDir) then
        printfn "[skip] map \"%s\" — maps directory %s does not exist" requestedMap mapsDir
        exit 3
    let target = sanitise supported.Sd7FileStem
    Directory.GetFiles(mapsDir, "*.sd7")
    |> Array.tryFind (fun path ->
        let stem = Path.GetFileNameWithoutExtension(path)
        sanitise stem = target
        || sanitise stem = target.Replace("_", "")
        || sanitise (stem.Replace("_", " ")) = sanitise requestedMap)
    |> function
        | Some p -> p
        | None ->
            printfn "[skip] map \"%s\" — no .sd7 under %s" requestedMap mapsDir
            exit 3

printfn "Found map archive: %s" sd7Path

printfn "Parsing SMF ..."
let parseSw = System.Diagnostics.Stopwatch.StartNew()
let smf =
    match SmfParser.parseSd7 sd7Path with
    | Result.Error e ->
        printfn "ERROR: SmfParser.parseSd7 failed: %A" e
        exit 2
    | Result.Ok v -> v
parseSw.Stop()
printfn "  Parsed in %dms — %dx%d heightmap" parseSw.ElapsedMilliseconds smf.WidthHeightmap smf.HeightHeightmap

let grid = SmfParser.toMapGrid smf

printfn "Running findChokepoints ..."
let sw = System.Diagnostics.Stopwatch.StartNew()
let cps = Chokepoints.findChokepoints grid supported.BaseCentre supported.ChokepointQuery
sw.Stop()
printfn "  Found %d chokepoints in %dms" cps.Length sw.ElapsedMilliseconds

let repoRoot =
    let here = __SOURCE_DIRECTORY__
    Path.GetFullPath(Path.Combine(here, "..", ".."))

let cacheFile = MapCacheFile.cachePathFor repoRoot supported
MapCacheFile.write supported grid cps cacheFile
printfn "\nWrote cache to %s" cacheFile
printfn "File size: %d bytes" ((FileInfo cacheFile).Length)
