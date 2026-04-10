// Example: Generate and inspect a synthetic scene
// Usage: dotnet fsi src/FSBar.SyntheticData/scripts/examples/01-generate-scene.fsx

#load "../prelude.fsx"

open FSBar.Client
open FSBar.SyntheticData

// Generate Scene A
let scene = generate SceneA
printfn "Scene: %s (%gx%g)" scene.Name scene.MapWidth scene.MapHeight
printfn "Frames: %d" scene.Frames.Length

// Inspect first and last frame
let first = scene.Frames.[0]
let last = scene.Frames.[299]
printfn ""
printfn "Frame 1: %d units, %d enemies" (Map.count first.Units) (Map.count first.Enemies)
printfn "  Metal: %.0f/%.0f (income=%.1f, usage=%.1f)" first.Metal.Current first.Metal.Storage first.Metal.Income first.Metal.Usage
printfn "  Energy: %.0f/%.0f (income=%.1f, usage=%.1f)" first.Energy.Current first.Energy.Storage first.Energy.Income first.Energy.Usage
printfn ""
printfn "Frame 300: %d units, %d enemies" (Map.count last.Units) (Map.count last.Enemies)
printfn "  Metal: %.0f/%.0f (income=%.1f, usage=%.1f)" last.Metal.Current last.Metal.Storage last.Metal.Income last.Metal.Usage
printfn "  Energy: %.0f/%.0f (income=%.1f, usage=%.1f)" last.Energy.Current last.Energy.Storage last.Energy.Income last.Energy.Usage

// Validate
let errors = validate scene
let contErrors = validateContinuity scene
printfn ""
printfn "Validation: %s" (if errors.IsEmpty then "PASS" else $"FAIL ({errors.Length} errors)")
printfn "Continuity: %s" (if contErrors.IsEmpty then "PASS" else $"FAIL ({contErrors.Length} errors)")

// Show unit types
printfn ""
printfn "Unit types in cache:"
scene.UnitDefs |> UnitDefCache.all |> Seq.iter (fun d ->
    printfn "  [%d] %s (cost=%.0f, range=%.0f)" d.DefId d.Name d.Cost d.MaxWeaponRange)
