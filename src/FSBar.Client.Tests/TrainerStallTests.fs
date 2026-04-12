module FSBar.Client.Tests.TrainerStallTests

open Xunit
open FSBar.Client.Tests.TrainerStallHelper

/// Baseline record used as the starting point for each case. Individual
/// tests override only the fields they care about.
let private baseline : StallTelemetry = {
    FramesSurvived = 100
    EnemyKilled = 3
    UnitsBuilt = 5
    PeakMetal = Some 500.0
    PeakEnergy = Some 300.0
}

[<Fact>]
let ``strict improvement on an int field is improvement`` () =
    let prior = baseline
    let current = { baseline with FramesSurvived = 200 }
    Assert.True(improvedOverPrior prior current)

[<Fact>]
let ``all ints stagnant and both peaks None yields no improvement`` () =
    let bothPeaksNone = { baseline with PeakMetal = None; PeakEnergy = None }
    Assert.False(improvedOverPrior bothPeaksNone bothPeaksNone)

[<Fact>]
let ``current peaks becoming None with stagnant ints is no improvement`` () =
    let prior = baseline
    let current = { baseline with PeakMetal = None; PeakEnergy = None }
    Assert.False(improvedOverPrior prior current)

[<Fact>]
let ``prior peak None and current peak Some counts as improvement`` () =
    let prior = { baseline with PeakMetal = None }
    let current = { prior with PeakMetal = Some 500.0 }
    Assert.True(improvedOverPrior prior current)

[<Fact>]
let ``numeric PeakMetal increase is improvement even with everything else stagnant`` () =
    let prior = baseline
    let current = { baseline with PeakMetal = Some 600.0 }
    Assert.True(improvedOverPrior prior current)

[<Fact>]
let ``PeakMetal regression with stagnant ints is not improvement`` () =
    let prior = baseline
    let current = { baseline with PeakMetal = Some 400.0 }
    Assert.False(improvedOverPrior prior current)
