// 05-unit-glyph.fsx — Feature 028-unit-viz-language demo.
//
// Walks a `FSBar.SyntheticData` scene through the unit-glyph renderer
// with increasing overlay composition (permanent only → W → W+L → W+L+C).
// Run from the repo root:
//   dotnet build tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj
//   dotnet fsi src/FSBar.Viz/scripts/examples/05-unit-glyph.fsx

#r "../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Proto.dll"
#r "../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Client.dll"
#r "../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.SyntheticData.dll"
#r "../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Viz.dll"
#r "../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaSharp.dll"
#r "../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaViewer.dll"

open FSBar.Client
open FSBar.Viz
open FSBar.SyntheticData

let scene = Scenes.generate SceneId.SceneA
let frame = scene.Frames.[0]

printfn "Scene: %s  frame 0 has %d units" scene.Name (Map.count frame.Units)

let displays = SyntheticDataAdapter.toUnitDisplays scene frame |> Seq.toList
printfn "Display records built: %d" (List.length displays)

let style = UnitGlyphPalettes.defaults

// Walk overlay compositions and report primitive counts.
let scenarios =
    [ "permanent only", Set.empty
      "+ W", Set.singleton OverlayKind.WeaponRanges
      "+ W L", Set.ofList [ OverlayKind.WeaponRanges; OverlayKind.SightRanges ]
      "+ W L C", Set.ofList [ OverlayKind.WeaponRanges; OverlayKind.SightRanges; OverlayKind.CommandQueue ]
      "+ W L C N",
        Set.ofList
            [ OverlayKind.WeaponRanges
              OverlayKind.SightRanges
              OverlayKind.CommandQueue
              OverlayKind.FullNames ] ]

for label, overlays in scenarios do
    UnitGlyph.resetSession()
    let elements = UnitGlyph.buildUnitsGlyph displays style overlays
    let statusLine = UnitGlyph.statusLine overlays
    printfn "%-12s  primitives=%d  statusLine=%s" label (List.length elements) statusLine
