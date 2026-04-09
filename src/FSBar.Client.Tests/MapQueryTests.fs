module FSBar.Client.Tests.MapQueryTests

open Xunit
open FSBar.Client

[<Fact>]
let ``nearestMetalSpot_empty_array_returns_none`` () =
    let result = MapQuery.nearestMetalSpot [||] (100.0f, 0.0f, 100.0f)
    Assert.True(result.IsNone)

[<Fact>]
let ``nearestMetalSpot_single_spot_returns_it`` () =
    let spots = [| (200.0f, 0.0f, 300.0f, 1.0f) |]
    let result = MapQuery.nearestMetalSpot spots (100.0f, 0.0f, 100.0f)
    Assert.True(result.IsSome)
    let (x, _, z, _) = result.Value
    Assert.Equal(200.0f, x)
    Assert.Equal(300.0f, z)

[<Fact>]
let ``nearestMetalSpot_multiple_spots_returns_closest`` () =
    let spots = [|
        (1000.0f, 0.0f, 1000.0f, 1.0f)  // far
        (150.0f, 0.0f, 150.0f, 0.5f)    // closest
        (500.0f, 0.0f, 500.0f, 2.0f)    // medium
    |]
    let result = MapQuery.nearestMetalSpot spots (100.0f, 0.0f, 100.0f)
    Assert.True(result.IsSome)
    let (x, _, z, v) = result.Value
    Assert.Equal(150.0f, x)
    Assert.Equal(150.0f, z)
    Assert.Equal(0.5f, v)

[<Fact>]
let ``nearestMetalSpot_exact_position_match`` () =
    let spots = [| (100.0f, 0.0f, 100.0f, 1.0f) |]
    let result = MapQuery.nearestMetalSpot spots (100.0f, 0.0f, 100.0f)
    Assert.True(result.IsSome)
