module FSBar.Viz.Tests.SurfaceBaselineTests

open System
open System.IO
open Xunit

let private vizSrcDir =
    Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "FSBar.Viz") |> Path.GetFullPath

let private baselinesDir =
    Path.Combine(__SOURCE_DIRECTORY__, "Baselines") |> Path.GetFullPath

let private isUpdateMode () =
    let v = Environment.GetEnvironmentVariable("UPDATE_BASELINES")
    not (isNull v) && v.Equals("true", StringComparison.OrdinalIgnoreCase)

let private lineDiff (expected: string) (actual: string) =
    let expectedLines = expected.Replace("\r\n", "\n").Split('\n')
    let actualLines = actual.Replace("\r\n", "\n").Split('\n')
    let maxLen = max expectedLines.Length actualLines.Length
    let diffs = ResizeArray<string>()
    for i in 0 .. maxLen - 1 do
        let eLine = if i < expectedLines.Length then Some expectedLines.[i] else None
        let aLine = if i < actualLines.Length then Some actualLines.[i] else None
        match eLine, aLine with
        | Some e, Some a when e = a -> ()
        | Some e, Some a ->
            diffs.Add(sprintf "  Line %d:" (i + 1))
            diffs.Add(sprintf "  - %s" e)
            diffs.Add(sprintf "  + %s" a)
        | Some e, None ->
            diffs.Add(sprintf "  Line %d:" (i + 1))
            diffs.Add(sprintf "  - %s" e)
        | None, Some a ->
            diffs.Add(sprintf "  Line %d:" (i + 1))
            diffs.Add(sprintf "  + %s" a)
        | None, None -> ()
    String.Join("\n", diffs)

[<Theory>]
[<InlineData("VizTypes")>]
[<InlineData("ColorMaps")>]
[<InlineData("LayerRenderer")>]
[<InlineData("SceneBuilder")>]
[<InlineData("GameViz")>]
let ``baseline_matches_fsi_surface`` (moduleName: string) =
    let fsiPath = Path.Combine(vizSrcDir, sprintf "%s.fsi" moduleName)
    let baselinePath = Path.Combine(baselinesDir, sprintf "%s.baseline" moduleName)
    let fsiContent = File.ReadAllText(fsiPath)

    if isUpdateMode () then
        if not (Directory.Exists(baselinesDir)) then
            Directory.CreateDirectory(baselinesDir) |> ignore
        let needsUpdate =
            not (File.Exists(baselinePath))
            || File.ReadAllText(baselinePath) <> fsiContent
        if needsUpdate then
            File.WriteAllText(baselinePath, fsiContent)
            Assert.True(true, sprintf "Baseline updated for %s" moduleName)
        else
            Assert.True(true, sprintf "Baseline already current for %s" moduleName)
    else
        Assert.True(
            File.Exists(baselinePath),
            sprintf "Missing baseline for %s. Run UPDATE_BASELINES=true dotnet test to generate." moduleName
        )
        let baselineContent = File.ReadAllText(baselinePath)
        if baselineContent <> fsiContent then
            let diff = lineDiff baselineContent fsiContent
            Assert.Fail(
                sprintf "Surface area changed for module %s.\n\nDiff (baseline vs current .fsi):\n%s\n\nRun UPDATE_BASELINES=true dotnet test to update baselines after reviewing changes."
                    moduleName diff
            )

[<Fact>]
let ``all_fsi_modules_have_baselines`` () =
    let fsiFiles =
        Directory.GetFiles(vizSrcDir, "*.fsi")
        |> Array.map (fun f -> Path.GetFileNameWithoutExtension(f))
        |> Array.sort

    if isUpdateMode () then
        if not (Directory.Exists(baselinesDir)) then
            Directory.CreateDirectory(baselinesDir) |> ignore
        let generated = ResizeArray<string>()
        for moduleName in fsiFiles do
            let baselinePath = Path.Combine(baselinesDir, sprintf "%s.baseline" moduleName)
            if not (File.Exists(baselinePath)) then
                let fsiContent = File.ReadAllText(Path.Combine(vizSrcDir, sprintf "%s.fsi" moduleName))
                File.WriteAllText(baselinePath, fsiContent)
                generated.Add(moduleName)
        if generated.Count > 0 then
            Assert.True(true, sprintf "Generated baselines for: %s" (String.Join(", ", generated)))
        else
            Assert.True(true, "All baselines already exist")
    else
        let missing =
            fsiFiles
            |> Array.filter (fun m ->
                not (File.Exists(Path.Combine(baselinesDir, sprintf "%s.baseline" m))))
        Assert.True(
            missing.Length = 0,
            sprintf "Missing baselines for: %s. Run UPDATE_BASELINES=true dotnet test to generate."
                (String.Join(", ", missing))
        )

[<Fact>]
let ``no_orphaned_baselines_exist`` () =
    if not (Directory.Exists(baselinesDir)) then ()
    else
        let baselineFiles =
            Directory.GetFiles(baselinesDir, "*.baseline")
            |> Array.map (fun f -> Path.GetFileNameWithoutExtension(f))
            |> Array.sort

        let orphaned =
            baselineFiles
            |> Array.filter (fun m ->
                not (File.Exists(Path.Combine(vizSrcDir, sprintf "%s.fsi" m))))

        Assert.True(
            orphaned.Length = 0,
            sprintf "Orphaned baselines found: %s. Remove baselines for deleted modules."
                (String.Join(", ", orphaned))
        )
