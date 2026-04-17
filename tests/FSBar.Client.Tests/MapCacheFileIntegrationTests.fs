module FSBar.Client.Tests.MapCacheFileIntegrationTests

open System
open System.Diagnostics
open System.IO
open Xunit
open Xunit.Abstractions
open FSBar.Client

let repoRoot () =
    // Test assembly lives at tests/FSBar.Client.Tests/bin/Debug/net10.0 → up 5 levels.
    let here = AppContext.BaseDirectory
    Path.GetFullPath(Path.Combine(here, "..", "..", "..", "..", ".."))

[<Fact>]
[<Trait("Category", "Committed")>]
let ``committed avalanche cache loads and materialises a real MapGrid`` () =
    let supported =
        MapCacheFile.tryFindSupportedMap "Avalanche 3.4"
        |> Option.defaultWith (fun () -> failwith "Avalanche 3.4 not in supportedMaps")
    let path = MapCacheFile.cachePathFor (repoRoot ()) supported
    Assert.True(File.Exists path, sprintf "Expected committed cache at %s" path)
    match MapCacheFile.read supported path with
    | Result.Error e -> Assert.Fail(sprintf "read returned Error: %s" (MapCacheFile.formatLoadError e))
    | Result.Ok loaded ->
        Assert.Equal("Avalanche 3.4", loaded.MapName)
        Assert.True(loaded.Grid.WidthHeightmap > 0)
        Assert.True(loaded.Grid.HeightHeightmap > 0)
        Assert.NotEmpty(loaded.Chokepoints)
        let hmW = Array2D.length1 loaded.Grid.HeightMap
        let hmH = Array2D.length2 loaded.Grid.HeightMap
        Assert.Equal(loaded.Grid.WidthHeightmap + 1, hmW)
        Assert.Equal(loaded.Grid.HeightHeightmap + 1, hmH)

type MapCacheFileLatencyTests(output: ITestOutputHelper) =

    [<Fact>]
    [<Trait("Category", "Committed")>]
    member _.``committed avalanche cache loads under 25 ms median (SC-002)`` () =
        let supported =
            MapCacheFile.tryFindSupportedMap "Avalanche 3.4"
            |> Option.defaultWith (fun () -> failwith "Avalanche 3.4 not in supportedMaps")
        let path = MapCacheFile.cachePathFor (repoRoot ()) supported
        Assert.True(File.Exists path, sprintf "Expected committed cache at %s" path)
        let samples = ResizeArray<int64>()
        for i in 0 .. 10 do
            let sw = Stopwatch.StartNew()
            match MapCacheFile.read supported path with
            | Result.Error e -> Assert.Fail(sprintf "read failed: %s" (MapCacheFile.formatLoadError e))
            | Result.Ok _ -> ()
            sw.Stop()
            if i > 0 then samples.Add sw.ElapsedMilliseconds
        let sorted = samples |> Seq.sort |> Seq.toArray
        let median = sorted.[sorted.Length / 2]
        output.WriteLine(sprintf "median load: %d ms (samples: %A)" median sorted)
        Assert.True(median < 25L, sprintf "SC-002 budget blown: median = %d ms" median)
