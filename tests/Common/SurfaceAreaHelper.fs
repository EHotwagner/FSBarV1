module FSBar.Tests.Common.SurfaceAreaHelper

open System
open System.IO
open Xunit

let isUpdateMode () =
    let legacy = Environment.GetEnvironmentVariable("UPDATE_BASELINES")
    let current = Environment.GetEnvironmentVariable("SURFACE_AREA_UPDATE")
    let isTrue (v: string) =
        not (isNull v)
        && (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v = "1")
    isTrue legacy || isTrue current

let lineDiff (expected: string) (actual: string) =
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

/// Verify that `<srcDir>/<moduleName>.fsi` matches `<baselinesDir>/<moduleName>.baseline`.
/// In update mode (UPDATE_BASELINES=true or SURFACE_AREA_UPDATE=1) overwrites the baseline
/// from the .fsi source.
let verifyModule (srcDir: string) (baselinesDir: string) (moduleName: string) : unit =
    let fsiPath = Path.Combine(srcDir, sprintf "%s.fsi" moduleName)
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
            sprintf "Missing baseline for %s at %s. Run SURFACE_AREA_UPDATE=1 dotnet test to generate." moduleName baselinePath
        )
        let baselineContent = File.ReadAllText(baselinePath)
        if baselineContent <> fsiContent then
            let diff = lineDiff baselineContent fsiContent
            Assert.Fail(
                sprintf "Surface area changed for module %s.\n\nDiff (baseline vs current .fsi):\n%s\n\nRun SURFACE_AREA_UPDATE=1 dotnet test to update baselines after reviewing changes."
                    moduleName diff
            )

/// Verify that every `.fsi` under `srcDir` (recursive) has a matching `.baseline`.
/// In update mode creates any that are missing.
let verifyAllModulesHaveBaselines (srcDir: string) (baselinesDir: string) : unit =
    let fsiFiles =
        Directory.GetFiles(srcDir, "*.fsi", SearchOption.AllDirectories)
        |> Array.map (fun f -> Path.GetFileNameWithoutExtension(f))
        |> Array.sort

    if isUpdateMode () then
        if not (Directory.Exists(baselinesDir)) then
            Directory.CreateDirectory(baselinesDir) |> ignore
        let generated = ResizeArray<string>()
        for moduleName in fsiFiles do
            let baselinePath = Path.Combine(baselinesDir, sprintf "%s.baseline" moduleName)
            if not (File.Exists(baselinePath)) then
                // Recursive search — locate the .fsi file's true path
                let fsiPath =
                    Directory.GetFiles(srcDir, sprintf "%s.fsi" moduleName, SearchOption.AllDirectories)
                    |> Array.head
                let fsiContent = File.ReadAllText(fsiPath)
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
            sprintf "Missing baselines for: %s. Run SURFACE_AREA_UPDATE=1 dotnet test to generate."
                (String.Join(", ", missing))
        )

/// Enumerate existing `.baseline` files under `baselinesDir` as `[| box name |]` rows,
/// suitable for `[<MemberData>]` input to an xUnit Theory.
let enumerateBaselineModules (baselinesDir: string) : obj[] seq =
    if Directory.Exists(baselinesDir) then
        Directory.GetFiles(baselinesDir, "*.baseline")
        |> Array.map (fun f -> [| box (Path.GetFileNameWithoutExtension f) |])
        |> Array.sortBy (fun row -> row.[0] :?> string)
        |> Array.toSeq
    else
        Seq.empty

/// Fail if a `.baseline` exists without a matching `.fsi` under `srcDir` (recursive).
let verifyNoOrphanedBaselines (srcDir: string) (baselinesDir: string) : unit =
    if not (Directory.Exists(baselinesDir)) then ()
    else
        let fsiModules =
            Directory.GetFiles(srcDir, "*.fsi", SearchOption.AllDirectories)
            |> Array.map (fun f -> Path.GetFileNameWithoutExtension(f))
            |> Set.ofArray

        let baselineFiles =
            Directory.GetFiles(baselinesDir, "*.baseline")
            |> Array.map (fun f -> Path.GetFileNameWithoutExtension(f))
            |> Array.sort

        let orphaned =
            baselineFiles
            |> Array.filter (fun m -> not (fsiModules.Contains(m)))

        Assert.True(
            orphaned.Length = 0,
            sprintf "Orphaned baselines found: %s. Remove baselines for deleted modules."
                (String.Join(", ", orphaned))
        )
