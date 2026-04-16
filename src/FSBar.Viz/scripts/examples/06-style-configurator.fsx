// 06-style-configurator.fsx — Feature 033-viz-style-configurator demo.
//
// Exercises the configurator panel programmatically: builds a preset from
// a modified VizConfig, saves/loads it from disk, and prints the descriptor
// inventory. Run from the repo root:
//   dotnet build tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj
//   dotnet fsi src/FSBar.Viz/scripts/examples/06-style-configurator.fsx

#r "../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Proto.dll"
#r "../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Client.dll"
#r "../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.SyntheticData.dll"
#r "../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Viz.dll"
#r "../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaSharp.dll"
#r "../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaViewer.dll"

open FSBar.Viz
open SkiaSharp

// 1. Descriptor inventory ---------------------------------------------------

printfn "Descriptors: %d total" ConfigDescriptors.all.Length
for cat in ConfigDescriptors.categoryOrder do
    let n =
        ConfigDescriptors.all
        |> List.filter (fun d -> d.Category = cat)
        |> List.length
    printfn "  %-14s  %d" (ConfigDescriptors.categoryLabel cat) n

// 2. Build a modified config ------------------------------------------------

let baseCfg = VizDefaults.defaultConfig
let modifiedCfg =
    { baseCfg with
        UnitMarkerSize = 12.0f
        BackgroundColor = SKColor(20uy, 30uy, 60uy)
        LabelColor = SKColor(255uy, 220uy, 120uy)
        ShowGridLines = not baseCfg.ShowGridLines
        OverlayOpacity = 0.6f }

printfn "\nBefore preset roundtrip:"
printfn "  UnitMarkerSize  = %.1f" modifiedCfg.UnitMarkerSize
printfn "  ShowGridLines   = %b" modifiedCfg.ShowGridLines

// 3. Save and reload --------------------------------------------------------

let presetName = "example-06-demo"
let preset = StylePreset.fromConfig presetName modifiedCfg

match StylePreset.save preset with
| Result.Ok path -> printfn "\nSaved preset to %s" path
| Result.Error msg -> printfn "\nSAVE FAILED: %s" msg

match StylePreset.load presetName with
| Result.Ok loaded ->
    let restored = StylePreset.applyToConfig loaded baseCfg
    printfn "\nAfter load+applyToConfig(defaults):"
    printfn "  UnitMarkerSize  = %.1f" restored.UnitMarkerSize
    printfn "  ShowGridLines   = %b" restored.ShowGridLines
    printfn "  isDirty vs defaults = %b"
        (ConfigDescriptors.isDirty restored baseCfg)
| Result.Error msg ->
    printfn "\nLOAD FAILED: %s" msg

// 4. Clean up ---------------------------------------------------------------

match StylePreset.delete presetName with
| Result.Ok _ -> printfn "\nPreset '%s' deleted." presetName
| Result.Error msg -> printfn "\nDELETE FAILED: %s" msg

printfn "\nDone. In the live viewer, press P to toggle the configurator panel."
