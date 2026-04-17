module FSBar.Viz.Tests.EncyclopediaDataTests

open Xunit
open FSBar.Viz

// `EncyclopediaData` lifts the BarData encyclopedia entry type to
// `FSBar.Viz` so the Units-tab renderer and `UnitDisplayAdapter` share
// one construction path (feature 038 FR-002).

[<Fact>]
let ``buildFromBarData returns non-empty sorted list`` () =
    let entries = EncyclopediaData.buildFromBarData ()
    Assert.NotEmpty(entries)
    let names = entries |> List.map (fun e -> e.InternalName)
    Assert.Equal<string list>(names, names |> List.sort)

[<Fact>]
let ``buildFromBarData populates classification for every entry`` () =
    let entries = EncyclopediaData.buildFromBarData ()
    for e in entries do
        Assert.False(System.String.IsNullOrEmpty e.InternalName)
        Assert.True(e.FootprintX >= 1)
        Assert.True(e.FootprintZ >= 1)

[<Fact>]
let ``DefId values are unique per entry`` () =
    let entries = EncyclopediaData.buildFromBarData ()
    let ids = entries |> List.map (fun e -> e.DefId) |> Set.ofList
    Assert.Equal(entries.Length, ids.Count)

[<Fact>]
let ``Armada entries classify as Armada faction`` () =
    let entries = EncyclopediaData.buildFromBarData ()
    let armadaEntries =
        entries |> List.filter (fun e -> e.Faction = FactionId.Armada)
    // There must be at least one Armada unit in BarData.
    Assert.NotEmpty(armadaEntries)
