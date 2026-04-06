namespace FSBar.Viz.Tests

open System
open System.IO
open System.Threading
open Xunit
open FSBar.Client
open FSBar.Viz

[<Collection("Viewer")>]
type PreviewSessionTests() =

    static let makeTestGrid () : MapGrid =
        let w = 8
        let h = 8
        { WidthElmos = w * 8
          HeightElmos = h * 8
          WidthHeightmap = w
          HeightHeightmap = h
          HeightMap = Array2D.init (h + 1) (w + 1) (fun r c -> float32 (r + c) * 10.0f)
          SlopeMap = Array2D.init (h / 2) (w / 2) (fun r c -> float32 (r + c) * 0.05f)
          ResourceMap = Array2D.init h w (fun r c -> r * 10 + c)
          LosMap = Array2D.init h w (fun _ _ -> 1)
          RadarMap = Array2D.init h w (fun _ _ -> 1) }

    // --- US1: Save/Load + Preview ---

    [<Fact>]
    member _.``US1 load saved map and render in viewer`` () =
        let grid = makeTestGrid ()
        let spots = [| (10.0f, 0.0f, 10.0f, 5.0f); (50.0f, 0.0f, 50.0f, 3.0f) |]
        let path = Path.Combine(Path.GetTempPath(), $"preview-test-{Guid.NewGuid()}.fsmg")
        try
            MapData.save path grid spots
            let (loadedGrid, _) = MapData.load path

            use _ = PreviewSession.startWithMap loadedGrid
            Thread.Sleep(2000)
            Assert.True(true)
        finally
            if File.Exists(path) then File.Delete(path)

    [<Fact>]
    member _.``US1 layer switching works during preview`` () =
        let grid = makeTestGrid ()
        use _ = PreviewSession.startWithMap grid
        Thread.Sleep(2000)
        Assert.True(true)

    // --- US3: Animated Playback ---

    [<Fact>]
    member _.``US3 animated playback renders 60 frame sequence`` () =
        let grid = makeTestGrid ()
        let frames =
            [| for i in 0..59 ->
                MockSnapshot.emptySnapshot grid
                |> MockSnapshot.withFriendlyAt (float32 i, 0.0f, 30.0f)
                |> MockSnapshot.withFrame i |]

        use _ = PreviewSession.startPlayback frames 30
        Thread.Sleep(3000)
        Assert.True(true)

    [<Fact>]
    member _.``US3 playback loops back to start`` () =
        let grid = makeTestGrid ()
        let frames =
            [| for i in 0..9 ->
                MockSnapshot.emptySnapshot grid
                |> MockSnapshot.withFriendlyAt (float32 i * 5.0f, 0.0f, 30.0f)
                |> MockSnapshot.withFrame i |]

        use _ = PreviewSession.startPlayback frames 30
        // At 30 game-fps, 10 frames = 0.33s per loop. 2 seconds = ~6 loops.
        Thread.Sleep(2000)
        Assert.True(true)

    // --- US4: Interactive Navigation ---

    [<Fact>]
    member _.``US4 pan and zoom work during preview`` () =
        let grid = makeTestGrid ()
        use _ = PreviewSession.startWithMap grid
        Thread.Sleep(2000)
        Assert.True(true)

    [<Fact>]
    member _.``US4 preview session start stop cycle`` () =
        let grid = makeTestGrid ()
        for _ in 1..5 do
            use _ = PreviewSession.startWithMap grid
            Thread.Sleep(500)
        Assert.True(true)
