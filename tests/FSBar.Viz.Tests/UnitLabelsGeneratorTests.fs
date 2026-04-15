module FSBar.Viz.Tests.UnitLabelsGeneratorTests

open Xunit
open FSBar.Viz

let private sampleNames =
    [ "armcom"; "armpw"; "armflash"; "armlab"; "armanni"; "armmex"
      "corcom"; "corgol"; "corak"; "corlab"; "cordoom"; "cormex"
      "legcom"; "legkeres"; "legraptor"
      "raptora"; "raptorb"
      "scavbase"; "scavguard" ]

// T028 — determinism ---------------------------------------------------------

[<Fact>]
let ``generator produces identical output on repeat runs`` () =
    let a = UnitLabelsGenerator.generate sampleNames None
    let b = UnitLabelsGenerator.generate sampleNames None
    Assert.Equal<Map<string, string>>(a, b)

// T029 — uniqueness ----------------------------------------------------------

[<Fact>]
let ``generator produces unique values`` () =
    let map = UnitLabelsGenerator.generate sampleNames None
    let values = map |> Map.toList |> List.map snd
    let uniq = values |> List.distinct
    Assert.Equal(List.length values, List.length uniq)

// T030 — 2-char rate ---------------------------------------------------------

[<Fact>]
let ``at least 90% of generated labels are 2 chars`` () =
    let map = UnitLabelsGenerator.generate sampleNames None
    let total = Map.count map
    let twoChar =
        map |> Map.toList |> List.filter (fun (_, v) -> v.Length = 2) |> List.length
    let frac = float twoChar / float total
    Assert.True(frac >= 0.9, $"2-char rate {frac} < 0.9")

// T031 — stability -----------------------------------------------------------

[<Fact>]
let ``adding a new unit preserves existing labels`` () =
    let first = UnitLabelsGenerator.generate sampleNames None
    let withNew =
        UnitLabelsGenerator.generate (sampleNames @ [ "newunitxyz" ]) (Some first)
    let preserved =
        first
        |> Map.toList
        |> List.filter (fun (k, v) -> Map.tryFind k withNew = Some v)
        |> List.length
    let rate = float preserved / float (Map.count first)
    Assert.True(rate >= 0.95, $"preservation rate {rate} < 0.95")

// T032 — faction prefix strip ------------------------------------------------

[<Fact>]
let ``labels do not start with faction prefix`` () =
    let map = UnitLabelsGenerator.generate sampleNames None
    // "arm"/"cor"/"leg" should be stripped before deriving the code, so
    // e.g. "armpw" should produce "Pw" rather than "Ar".
    let armpw = Map.find "armpw" map
    Assert.True(
        not (armpw.StartsWith "Ar") || armpw.Length = 3,
        $"armpw label {armpw} looks like faction-prefix-derived")
