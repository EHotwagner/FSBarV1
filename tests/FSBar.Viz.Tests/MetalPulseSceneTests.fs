module FSBar.Viz.Tests.MetalPulseSceneTests

open Xunit
open FSBar.Viz

[<Fact>]
let ``pulse alpha varies between frames with different elapsed seconds`` () =
    // Quarter-period apart: sin(0)=0 → alpha floor+mid, sin(π/2)=1 → alpha ceiling.
    let a = SceneBuilder.computePulseAlpha 0.0 1.5
    let b = SceneBuilder.computePulseAlpha 0.375 1.5
    Assert.NotEqual(a, b)

[<Fact>]
let ``metal marker alpha never reaches 0 or 255`` () =
    let mutable t = 0.0
    while t <= 3.0 do
        let a = SceneBuilder.computePulseAlpha t 1.5
        Assert.InRange(int a, 60, 220)
        t <- t + 0.05
