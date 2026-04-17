module FSBar.Client.Tests.MapQueryMetalSpotsTests

open Xunit
open FSBar.Client

let makeGrid (width: int) (height: int) (resource: int[,]) (heights: float32[,]) : MapGrid =
    { WidthElmos = width * 8
      HeightElmos = height * 8
      WidthHeightmap = width
      HeightHeightmap = height
      HeightMap = heights
      SlopeMap = Array2D.init (max 1 (height / 2)) (max 1 (width / 2)) (fun _ _ -> 0.0f)
      ResourceMap = resource
      LosMap = Array2D.init height width (fun _ _ -> 0)
      RadarMap = Array2D.init height width (fun _ _ -> 0) }

[<Fact>]
let ``empty resource map returns empty array`` () =
    let w, h = 4, 4
    let resource = Array2D.init h w (fun _ _ -> 0)
    let heights = Array2D.init h w (fun _ _ -> 5.0f)
    let grid = makeGrid w h resource heights
    let spots = MapQuery.metalSpotsFromResourceMap grid
    Assert.Empty(spots)

[<Fact>]
let ``single isolated non-zero cell returns one spot at elmos coordinates`` () =
    let w, h = 6, 6
    let resource = Array2D.init h w (fun _ _ -> 0)
    // cell at [z=2, x=3] with value 100
    resource.[2, 3] <- 100
    let heights = Array2D.init h w (fun z x -> float32 (z * 10 + x))
    let grid = makeGrid w h resource heights
    let spots = MapQuery.metalSpotsFromResourceMap grid
    Assert.Equal(1, spots.Length)
    let (wx, wy, wz, _richness) = spots.[0]
    Assert.Equal(3.0f * 8.0f, wx)
    Assert.Equal(2.0f * 8.0f, wz)
    Assert.Equal(heights.[2, 3], wy)

[<Fact>]
let ``two disjoint 3-cell clusters return exactly two spots`` () =
    let w, h = 10, 10
    let resource = Array2D.init h w (fun _ _ -> 0)
    // cluster A: (0,0), (0,1), (1,0)
    resource.[0, 0] <- 50
    resource.[0, 1] <- 50
    resource.[1, 0] <- 50
    // cluster B: far away at (5,5), (5,6), (6,5)
    resource.[5, 5] <- 80
    resource.[5, 6] <- 80
    resource.[6, 5] <- 80
    let heights = Array2D.init h w (fun _ _ -> 10.0f)
    let grid = makeGrid w h resource heights
    let spots = MapQuery.metalSpotsFromResourceMap grid
    Assert.Equal(2, spots.Length)

[<Fact>]
let ``diagonally adjacent cells are in the same cluster`` () =
    let w, h = 6, 6
    let resource = Array2D.init h w (fun _ _ -> 0)
    // Two cells that share only a diagonal corner — must be one cluster (8-conn).
    resource.[1, 1] <- 10
    resource.[2, 2] <- 10
    let heights = Array2D.init h w (fun _ _ -> 0.0f)
    let grid = makeGrid w h resource heights
    let spots = MapQuery.metalSpotsFromResourceMap grid
    Assert.Equal(1, spots.Length)

[<Fact>]
let ``is deterministic across calls`` () =
    let w, h = 8, 8
    let resource = Array2D.init h w (fun _ _ -> 0)
    resource.[1, 2] <- 30
    resource.[1, 3] <- 30
    resource.[4, 5] <- 60
    resource.[5, 5] <- 60
    resource.[5, 6] <- 60
    let heights = Array2D.init h w (fun z x -> float32 (z + x))
    let grid = makeGrid w h resource heights
    let spots1 = MapQuery.metalSpotsFromResourceMap grid
    let spots2 = MapQuery.metalSpotsFromResourceMap grid
    Assert.Equal<(float32 * float32 * float32 * float32) array>(spots1, spots2)
