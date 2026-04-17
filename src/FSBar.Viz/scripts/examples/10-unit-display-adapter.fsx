// Feature 038 US1 — UnitDisplayAdapter demo.
//
// Builds a `UnitDisplay` from one BarData encyclopedia entry and
// renders its glyph to stdout as a summary. Run from the repo root:
//
//     dotnet fsi src/FSBar.Viz/scripts/examples/10-unit-display-adapter.fsx
//
// Requires: `dotnet build FSBarV1.slnx` has produced the FSBar.Viz DLL
// under src/FSBar.Viz/bin/Debug/net10.0/.

#r "../../bin/Debug/net10.0/FSBar.Client.dll"
#r "../../bin/Debug/net10.0/FSBar.Viz.dll"
#r "../../bin/Debug/net10.0/BarData.dll"

open FSBar.Viz

let entries = EncyclopediaData.buildFromBarData ()

printfn "encyclopedia: %d entries" entries.Length

// Pick the Armada commander and render a preview UnitDisplay.
match entries |> List.tryFind (fun e -> e.InternalName = "armcom") with
| None ->
    printfn "armcom not found — BarData may be missing the Armada commander."
| Some armcom ->
    // Static preview size — the Units tab does this exact call.
    let display = UnitDisplayAdapter.ofEncyclopediaEntry armcom 32.0f
    printfn "UnitDisplay for %s:" display.InternalName
    printfn "  Shape   = %A" display.Shape
    printfn "  Faction = %A" display.Faction
    printfn "  Tier    = %A" display.Tier
    printfn "  Label   = %s" display.LabelCode
    printfn "  Sight   = %.0f elmos" display.SightRangeElmo
    printfn "  Weapons = %A" display.WeaponRangesElmo
    printfn ""
    printfn "HeadingRadians = %f (0.0 = canonical east-facing; FR-010a)" display.HeadingRadians
