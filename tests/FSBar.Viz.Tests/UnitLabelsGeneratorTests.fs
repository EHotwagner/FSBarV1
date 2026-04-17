module FSBar.Viz.Tests.UnitLabelsGeneratorTests

open Xunit
open FSBar.Viz

// Sample input — a mix of Armada and Cortex units across two shapes so
// the generator actually exercises per-bucket allocation. All entries
// fit comfortably inside the single-char pool.
let sampleItems : (string * MovementShape * FactionId) list =
    [ "armpw",    MovementShape.Bot,      FactionId.Armada
      "armck",    MovementShape.Bot,      FactionId.Armada
      "armflash", MovementShape.Vehicle,  FactionId.Armada
      "armcom",   MovementShape.Bot,      FactionId.Armada
      "armlab",   MovementShape.Building, FactionId.Armada
      "armanni",  MovementShape.Building, FactionId.Armada
      "armmex",   MovementShape.Building, FactionId.Armada
      "corcom",   MovementShape.Bot,      FactionId.Cortex
      "corgol",   MovementShape.Vehicle,  FactionId.Cortex
      "corak",    MovementShape.Bot,      FactionId.Cortex
      "corlab",   MovementShape.Building, FactionId.Cortex
      "cordoom",  MovementShape.Building, FactionId.Cortex
      "cormex",   MovementShape.Building, FactionId.Cortex
      "legcom",   MovementShape.Bot,      FactionId.Legion
      "legkeres", MovementShape.Vehicle,  FactionId.Legion
      "legraptor",MovementShape.Bot,      FactionId.Legion ]

let sampleNames = sampleItems |> List.map (fun (n, _, _) -> n)

// T028 — determinism ---------------------------------------------------------

[<Fact>]
let ``generator produces identical output on repeat runs`` () =
    let a = UnitLabelsGenerator.generate sampleItems None
    let b = UnitLabelsGenerator.generate sampleItems None
    Assert.Equal<Map<string, string>>(a, b)

// T029 — uniqueness (per bucket) ---------------------------------------------

[<Fact>]
let ``generator produces unique labels within each (shape, faction) bucket`` () =
    let map = UnitLabelsGenerator.generate sampleItems None
    let bucketed =
        sampleItems
        |> List.groupBy (fun (_, s, f) -> s, f)
        |> List.map (fun (k, xs) -> k, xs |> List.map (fun (n, _, _) -> Map.find n map))
    for (key, labels) in bucketed do
        let uniq = labels |> List.distinct
        Assert.True(
            List.length labels = List.length uniq,
            $"Collision in bucket {key}: {labels}")

// T030 — single-glyph rate ---------------------------------------------------

[<Fact>]
let ``at least 90% of generated labels are a single character`` () =
    let map = UnitLabelsGenerator.generate sampleItems None
    let total = Map.count map
    let oneChar =
        map |> Map.toList |> List.filter (fun (_, v) -> v.Length = 1) |> List.length
    let frac = float oneChar / float total
    Assert.True(frac >= 0.9, $"1-char rate {frac} < 0.9")

// T031 — stability -----------------------------------------------------------

[<Fact>]
let ``adding a new unit preserves existing labels`` () =
    let first = UnitLabelsGenerator.generate sampleItems None
    let extended =
        sampleItems @ [ "newunitxyz", MovementShape.Bot, FactionId.Armada ]
    let withNew = UnitLabelsGenerator.generate extended (Some first)
    let preserved =
        first
        |> Map.toList
        |> List.filter (fun (k, v) -> Map.tryFind k withNew = Some v)
        |> List.length
    let rate = float preserved / float (Map.count first)
    Assert.True(rate >= 0.95, $"preservation rate {rate} < 0.95")

// T032 — faction prefix strip ------------------------------------------------

[<Fact>]
let ``labels do not start from faction prefix`` () =
    let map = UnitLabelsGenerator.generate sampleItems None
    // Prefix stripping: "armpw" should derive from "pw" → 'P' (first
    // consonant of the bare stem), never from the faction prefix 'A'.
    let armpw = Map.find "armpw" map
    Assert.Equal("P", armpw)
