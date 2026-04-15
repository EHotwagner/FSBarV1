// gen-unit-labels.fsx — build-time label-table generator
// Feature 028-unit-viz-language. Regenerate the committed
// src/FSBar.Viz/UnitLabels.generated.fs against the current `BarData`:
//
//   cd /path/to/FSBarV1
//   dotnet build src/FSBar.Viz/FSBar.Viz.fsproj
//   dotnet fsi src/FSBar.Viz/scripts/gen-unit-labels.fsx
//
// Exits non-zero if an existing label would change without a genuine
// collision (SC-006 tripwire).

#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/BarData.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Viz.dll"

open System
open System.IO

let barDataVersion = "1.0.3"

// ---------------------------------------------------------------------------
// Collect unit names from BarData.AllUnitDefs (full UnitDef list covering
// all 953 entries). See research.md R1 and the agent reflection output in
// session logs.

open FSBar.Viz

// Each item carries the shape and faction we'll render it as so the
// generator can uniquify labels per (shape, faction) bucket rather than
// globally. Everything else — stroke colour, shape — already
// distinguishes duplicates across buckets.
let private itemOf (def: BarData.UnitDef) : string * MovementShape * FactionId =
    let canMove = match def.movement with Some m -> m.canMove | None -> false
    let canFly = match def.movement with Some m -> m.canFly | None -> false
    let mClass = match def.movement with Some m -> m.movementClass | None -> None
    let shape = UnitGlyph.classifyShape canMove canFly mClass ignore
    let faction = UnitGlyph.classifyFaction def.subfolder def.name ignore
    def.name, shape, faction

let items : (string * MovementShape * FactionId) list =
    BarData.AllUnitDefs.all
    |> List.map (fun (_, _, def) -> itemOf def)
    |> List.distinctBy (fun (n, _, _) -> n)
    |> List.sortBy (fun (n, _, _) -> n)

let names = items |> List.map (fun (n, _, _) -> n)
printfn "Loaded %d unit names from BarData %s" (List.length names) barDataVersion

// ---------------------------------------------------------------------------
// Load the previously committed labels (if present) for stability pass.

let repoRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", ".."))
let outPath = Path.Combine(repoRoot, "src", "FSBar.Viz", "UnitLabels.generated.fs")

let parsePrevious (path: string) : Map<string, string> =
    if not (File.Exists path) then Map.empty
    else
        let lines = File.ReadAllLines path
        let mutable inMap = false
        let entries = ResizeArray<string * string>()
        for line in lines do
            let trimmed = line.Trim()
            if trimmed.StartsWith "Map.ofList" then inMap <- true
            elif inMap then
                // Looking for entries of the form `"name", "Xx"`
                let l = trimmed.TrimEnd(';').Trim()
                if l.StartsWith "\"" && l.Contains ", " then
                    let parts = l.Split([| ", " |], StringSplitOptions.RemoveEmptyEntries)
                    if parts.Length = 2 then
                        let a = parts.[0].Trim('"', ' ')
                        let b = parts.[1].Trim('"', ' ', ']', ')')
                        if a.Length > 0 && b.Length > 0 then
                            entries.Add(a, b)
        entries |> Seq.toList |> Map.ofList

let cleanRegenerate =
    fsi.CommandLineArgs |> Array.exists (fun a -> a = "--clean")

let previous =
    if cleanRegenerate then
        printfn "Clean regeneration requested — ignoring previous labels."
        Map.empty
    else
        parsePrevious outPath
printfn "Loaded %d previous labels from %s" (Map.count previous) outPath

// ---------------------------------------------------------------------------
// Generate.

let labels = FSBar.Viz.UnitLabelsGenerator.generate items (Some previous)
printfn "Generated %d labels" (Map.count labels)

// Rate: how many labels are a single glyph.
let oneChar =
    labels |> Map.toList |> List.filter (fun (_, v) -> v.Length = 1) |> List.length
let oneCharRate = float oneChar / float (Map.count labels)
printfn "1-char rate: %.1f%% (%d / %d)" (oneCharRate * 100.0) oneChar (Map.count labels)

// Stability check.
let preserved =
    previous
    |> Map.toList
    |> List.filter (fun (k, v) -> Map.tryFind k labels = Some v)
    |> List.length
let changed =
    previous
    |> Map.toList
    |> List.filter (fun (k, v) ->
        match Map.tryFind k labels with
        | Some nv -> nv <> v
        | None -> true)
if Map.count previous > 0 then
    let rate = float preserved / float (Map.count previous)
    printfn "Preserved: %.1f%% (%d / %d)" (rate * 100.0) preserved (Map.count previous)
    if not (List.isEmpty changed) then
        printfn "CHANGED labels (first 10):"
        changed |> List.truncate 10 |> List.iter (fun (k, v) -> printfn "  %s: %s -> %s" k v (Map.find k labels))

// ---------------------------------------------------------------------------
// Emit.

let sb = System.Text.StringBuilder()
let nl = "\n"
sb.Append("// Generated — do not edit by hand. Regenerate via scripts/gen-unit-labels.fsx.") |> ignore
sb.Append(nl) |> ignore
sb.Append("namespace FSBar.Viz") |> ignore
sb.Append(nl) |> ignore
sb.Append(nl) |> ignore
sb.Append("module UnitLabels =") |> ignore
sb.Append(nl) |> ignore
sb.Append(nl) |> ignore
sb.Append(sprintf "    let BarDataVersion = \"%s\"" barDataVersion) |> ignore
sb.Append(nl) |> ignore
sb.Append(sprintf "    let GeneratedAtUtc = \"%s\"" (DateTime.UtcNow.ToString("o"))) |> ignore
sb.Append(nl) |> ignore
sb.Append(nl) |> ignore
sb.Append("    let Labels : Map<string, string> =") |> ignore
sb.Append(nl) |> ignore
sb.Append("        Map.ofList [") |> ignore
sb.Append(nl) |> ignore
for KeyValue(k, v) in labels do
    sb.Append(sprintf "            \"%s\", \"%s\"" k v) |> ignore
    sb.Append(nl) |> ignore
sb.Append("        ]") |> ignore
sb.Append(nl) |> ignore
sb.Append(nl) |> ignore
sb.Append("    let tryLookup (internalName: string) : string option =") |> ignore
sb.Append(nl) |> ignore
sb.Append("        Map.tryFind internalName Labels") |> ignore
sb.Append(nl) |> ignore
sb.Append(nl) |> ignore
sb.Append("    let lookupOrFallback (internalName: string) : string =") |> ignore
sb.Append(nl) |> ignore
sb.Append("        match tryLookup internalName with") |> ignore
sb.Append(nl) |> ignore
sb.Append("        | Some code -> code") |> ignore
sb.Append(nl) |> ignore
sb.Append("        | None -> \"??\"") |> ignore
sb.Append(nl) |> ignore

File.WriteAllText(outPath, sb.ToString())
printfn "Wrote %s" outPath

if Map.count previous > 0 && float preserved / float (Map.count previous) < 0.95 then
    printfn "ERROR: preservation rate below 95%%"
    exit 1

exit 0
