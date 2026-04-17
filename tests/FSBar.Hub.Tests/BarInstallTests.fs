module FSBar.Hub.Tests.BarInstallTests

open System
open System.IO
open Xunit
open FSBar.Hub

/// Creates a synthetic BAR data directory layout in a temp location and
/// cleans up on Dispose. Tests configure which engines, binaries, and
/// AIs are present.
type private FakeBarInstall() =
    let tempDir =
        let p = Path.Combine(Path.GetTempPath(), "fsbar-barinstall-test-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(p) |> ignore
        p

    /// Touch maps/ and packages/ so EngineDiscovery.defaultDataDir-style
    /// checks would see a complete install.
    do
        Directory.CreateDirectory(Path.Combine(tempDir, "maps")) |> ignore
        Directory.CreateDirectory(Path.Combine(tempDir, "packages")) |> ignore

    member _.DataDir = tempDir

    /// Add a recoil_<version> subdirectory with optional executable
    /// spring-headless / spring binaries. Returns the engine dir path.
    member _.AddEngine (version: string, ?withHeadless: bool, ?withGraphical: bool) =
        let withHeadless = defaultArg withHeadless true
        let withGraphical = defaultArg withGraphical false
        let engineDir = Path.Combine(tempDir, "engine", "recoil_" + version)
        Directory.CreateDirectory(engineDir) |> ignore
        let makeExec (path: string) =
            File.WriteAllText(path, "#!/bin/sh\nexit 0")
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead ||| UnixFileMode.UserWrite ||| UnixFileMode.UserExecute)
        if withHeadless then makeExec (Path.Combine(engineDir, "spring-headless"))
        if withGraphical then makeExec (Path.Combine(engineDir, "spring"))
        engineDir

    /// Add a skirmish AI directory under the given engine version.
    member this.AddSkirmishAi (version: string, aiName: string) =
        let engineDir = Path.Combine(tempDir, "engine", "recoil_" + version)
        let aiDir = Path.Combine(engineDir, "AI", "Skirmish", aiName)
        Directory.CreateDirectory(aiDir) |> ignore

    interface IDisposable with
        member _.Dispose() =
            try Directory.Delete(tempDir, recursive = true) with _ -> ()

let private settingsWith (dataDirOverride: string option) (engineOverride: string option) =
    { HubSettings.defaults with
        BarDataDirOverride = dataDirOverride
        EngineVersionOverride = engineOverride }

[<Fact>]
let ``detect returns DataDirNotFound for missing override`` () =
    let bogus = "/tmp/fsbar-barinstall-nope-" + Guid.NewGuid().ToString("N")
    let settings = settingsWith (Some bogus) None
    match BarInstall.detect settings with
    | Error (BarInstall.DataDirNotFound p) -> Assert.Equal(bogus, p)
    | other -> Assert.Fail(sprintf "expected DataDirNotFound, got %A" other)

[<Fact>]
let ``detect returns EngineSubdirMissing when data dir lacks engine/`` () =
    use fake = new FakeBarInstall()
    // fake has maps/ and packages/ but no engine/ subdir yet.
    let settings = settingsWith (Some fake.DataDir) None
    match BarInstall.detect settings with
    | Error (BarInstall.EngineSubdirMissing p) ->
        Assert.EndsWith(Path.Combine(fake.DataDir, "engine"), p)
    | other -> Assert.Fail(sprintf "expected EngineSubdirMissing, got %A" other)

[<Fact>]
let ``detect returns NoEngineVersions when engine/ empty`` () =
    use fake = new FakeBarInstall()
    Directory.CreateDirectory(Path.Combine(fake.DataDir, "engine")) |> ignore
    let settings = settingsWith (Some fake.DataDir) None
    match BarInstall.detect settings with
    | Error (BarInstall.NoEngineVersions _) -> ()
    | other -> Assert.Fail(sprintf "expected NoEngineVersions, got %A" other)

[<Fact>]
let ``detect sorts engines newest-first`` () =
    use fake = new FakeBarInstall()
    fake.AddEngine("2026.01.10") |> ignore
    fake.AddEngine("2026.03.14") |> ignore
    fake.AddEngine("2026.02.22") |> ignore
    let settings = settingsWith (Some fake.DataDir) None
    match BarInstall.detect settings with
    | Ok install ->
        let versions = install.Engines |> List.map (fun e -> e.Version)
        Assert.Equal<string list>([ "2026.03.14"; "2026.02.22"; "2026.01.10" ], versions)
        Assert.Equal("2026.03.14", install.ActiveEngine.Version)
    | Error e -> Assert.Fail(sprintf "detect failed: %s" (BarInstall.formatError e))

[<Fact>]
let ``detect honours EngineVersionOverride`` () =
    use fake = new FakeBarInstall()
    fake.AddEngine("2026.03.14") |> ignore
    fake.AddEngine("2026.01.10") |> ignore
    let settings = settingsWith (Some fake.DataDir) (Some "2026.01.10")
    match BarInstall.detect settings with
    | Ok install -> Assert.Equal("2026.01.10", install.ActiveEngine.Version)
    | Error e -> Assert.Fail(sprintf "detect failed: %s" (BarInstall.formatError e))

[<Fact>]
let ``detect returns OverriddenEngineNotFound for unknown override`` () =
    use fake = new FakeBarInstall()
    fake.AddEngine("2026.03.14") |> ignore
    let settings = settingsWith (Some fake.DataDir) (Some "2099.12.31")
    match BarInstall.detect settings with
    | Error (BarInstall.OverriddenEngineNotFound v) -> Assert.Equal("2099.12.31", v)
    | other -> Assert.Fail(sprintf "expected OverriddenEngineNotFound, got %A" other)

[<Fact>]
let ``detect reports headless + graphical binary presence`` () =
    use fake = new FakeBarInstall()
    fake.AddEngine("2026.03.14", withHeadless = true, withGraphical = true) |> ignore
    fake.AddEngine("2026.02.22", withHeadless = true, withGraphical = false) |> ignore
    let settings = settingsWith (Some fake.DataDir) None
    match BarInstall.detect settings with
    | Ok install ->
        let byVersion = install.Engines |> List.map (fun e -> e.Version, e.HasHeadlessBin, e.HasGraphicalBin)
        Assert.Contains(("2026.03.14", true, true), byVersion)
        Assert.Contains(("2026.02.22", true, false), byVersion)
    | Error e -> Assert.Fail(sprintf "detect failed: %s" (BarInstall.formatError e))

[<Fact>]
let ``listSkirmishAis returns sorted AI directory names`` () =
    use fake = new FakeBarInstall()
    fake.AddEngine("2026.03.14") |> ignore
    fake.AddSkirmishAi("2026.03.14", "HighBarV2")
    fake.AddSkirmishAi("2026.03.14", "BARb")
    fake.AddSkirmishAi("2026.03.14", "NullAI")
    let settings = settingsWith (Some fake.DataDir) None
    match BarInstall.detect settings with
    | Ok install ->
        let ais = BarInstall.listSkirmishAis install.ActiveEngine
        Assert.Equal<string list>([ "BARb"; "HighBarV2"; "NullAI" ], ais)
    | Error e -> Assert.Fail(sprintf "detect failed: %s" (BarInstall.formatError e))

[<Fact>]
let ``listSkirmishAis returns empty list when AI/Skirmish missing`` () =
    use fake = new FakeBarInstall()
    fake.AddEngine("2026.03.14") |> ignore
    let settings = settingsWith (Some fake.DataDir) None
    match BarInstall.detect settings with
    | Ok install ->
        Assert.Empty(BarInstall.listSkirmishAis install.ActiveEngine)
    | Error e -> Assert.Fail(sprintf "detect failed: %s" (BarInstall.formatError e))

[<Fact>]
let ``DataDirIsDefault is false for explicit override`` () =
    use fake = new FakeBarInstall()
    fake.AddEngine("2026.03.14") |> ignore
    let settings = settingsWith (Some fake.DataDir) None
    match BarInstall.detect settings with
    | Ok install -> Assert.False(install.DataDirIsDefault)
    | Error e -> Assert.Fail(sprintf "detect failed: %s" (BarInstall.formatError e))
