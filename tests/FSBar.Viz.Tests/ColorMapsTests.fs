module FSBar.Viz.Tests.ColorMapsTests

open Xunit
open SkiaSharp
open FSBar.Client
open FSBar.Viz

[<Fact>]
let ``grayscale maps 0.0 to near-black`` () =
    let c = ColorMaps.grayscale.MapValue 0.0f
    Assert.True(c.Red < 30uy, $"Red={c.Red} should be near 0")
    Assert.True(c.Green < 30uy, $"Green={c.Green} should be near 0")
    Assert.True(c.Blue < 30uy, $"Blue={c.Blue} should be near 0")

[<Fact>]
let ``grayscale maps 1.0 to near-white`` () =
    let c = ColorMaps.grayscale.MapValue 1.0f
    Assert.True(c.Red > 225uy, $"Red={c.Red} should be near 255")
    Assert.True(c.Green > 225uy, $"Green={c.Green} should be near 255")
    Assert.True(c.Blue > 225uy, $"Blue={c.Blue} should be near 255")

[<Fact>]
let ``terrain maps 0.0 to blue-ish`` () =
    let c = ColorMaps.terrain.MapValue 0.0f
    Assert.True(c.Blue > c.Red, $"Blue={c.Blue} should be > Red={c.Red}")
    Assert.True(c.Blue > c.Green, $"Blue={c.Blue} should be > Green={c.Green}")

[<Fact>]
let ``terrain maps 1.0 to white-ish`` () =
    let c = ColorMaps.terrain.MapValue 1.0f
    Assert.True(c.Red > 200uy, $"Red={c.Red} should be high")
    Assert.True(c.Green > 200uy, $"Green={c.Green} should be high")
    Assert.True(c.Blue > 200uy, $"Blue={c.Blue} should be high")

[<Fact>]
let ``heatMap maps 0.0 to blue-ish`` () =
    let c = ColorMaps.heatMap.MapValue 0.0f
    Assert.True(c.Blue > c.Red, $"Blue={c.Blue} should be > Red={c.Red}")

[<Fact>]
let ``heatMap maps 1.0 to red-ish`` () =
    let c = ColorMaps.heatMap.MapValue 1.0f
    Assert.True(c.Red > c.Blue, $"Red={c.Red} should be > Blue={c.Blue}")

[<Fact>]
let ``binary maps 0.3 to red`` () =
    let c = ColorMaps.binary.MapValue 0.3f
    Assert.Equal(SKColors.Red, c)

[<Fact>]
let ``binary maps 0.7 to green`` () =
    let c = ColorMaps.binary.MapValue 0.7f
    Assert.Equal(SKColors.Green, c)

[<Fact>]
let ``colorSchemeFor returns terrain for HeightMap`` () =
    let scheme = ColorMaps.colorSchemeFor LayerKind.HeightMap
    Assert.Equal("Terrain", scheme.Name)

[<Fact>]
let ``colorSchemeFor returns heatMap for SlopeMap`` () =
    let scheme = ColorMaps.colorSchemeFor LayerKind.SlopeMap
    Assert.Equal("HeatMap", scheme.Name)

[<Fact>]
let ``colorSchemeFor returns heatMap for ResourceMap`` () =
    let scheme = ColorMaps.colorSchemeFor LayerKind.ResourceMap
    Assert.Equal("HeatMap", scheme.Name)

[<Fact>]
let ``colorSchemeFor returns binary for LosMap`` () =
    let scheme = ColorMaps.colorSchemeFor LayerKind.LosMap
    Assert.Equal("Binary", scheme.Name)

[<Fact>]
let ``colorSchemeFor returns binary for RadarMap`` () =
    let scheme = ColorMaps.colorSchemeFor LayerKind.RadarMap
    Assert.Equal("Binary", scheme.Name)

[<Fact>]
let ``colorSchemeFor returns binary for Passability`` () =
    let scheme = ColorMaps.colorSchemeFor (LayerKind.Passability MoveType.Kbot)
    Assert.Equal("Binary", scheme.Name)

[<Fact>]
let ``all schemes handle boundary 0.0 without exception`` () =
    let schemes = [ ColorMaps.grayscale; ColorMaps.terrain; ColorMaps.heatMap; ColorMaps.binary ]
    for scheme in schemes do
        let _ = scheme.MapValue 0.0f
        ()

[<Fact>]
let ``all schemes handle boundary 0.5 without exception`` () =
    let schemes = [ ColorMaps.grayscale; ColorMaps.terrain; ColorMaps.heatMap; ColorMaps.binary ]
    for scheme in schemes do
        let _ = scheme.MapValue 0.5f
        ()

[<Fact>]
let ``all schemes handle boundary 1.0 without exception`` () =
    let schemes = [ ColorMaps.grayscale; ColorMaps.terrain; ColorMaps.heatMap; ColorMaps.binary ]
    for scheme in schemes do
        let _ = scheme.MapValue 1.0f
        ()
