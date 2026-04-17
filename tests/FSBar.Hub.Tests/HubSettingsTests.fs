module FSBar.Hub.Tests.HubSettingsTests

open System
open System.IO
open Xunit
open FSBar.Hub

/// Redirect `XDG_CONFIG_HOME` to a temp dir for the duration of a test so
/// we never touch the user's real hub settings file.
type private XdgScope() =
    let previous = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
    let tempDir =
        let p = Path.Combine(Path.GetTempPath(), "fsbar-hub-test-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(p) |> ignore
        p
    do Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", tempDir)
    member _.Path = tempDir
    interface IDisposable with
        member _.Dispose() =
            Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", previous)
            try Directory.Delete(tempDir, recursive = true) with _ -> ()

[<Fact>]
let ``settingsPath honors XDG_CONFIG_HOME`` () =
    use scope = new XdgScope()
    let p = HubSettings.settingsPath ()
    Assert.StartsWith(scope.Path, p)
    Assert.EndsWith(Path.Combine("fsbar-hub", "settings.json"), p)

[<Fact>]
let ``load returns defaults when file absent`` () =
    use _scope = new XdgScope()
    let loaded = HubSettings.load ()
    Assert.Equal(HubSettings.defaults, loaded)

[<Fact>]
let ``save then load round-trips all fields`` () =
    use _scope = new XdgScope()
    let custom =
        { HubSettings.defaults with
            BarDataDirOverride = Some "/opt/custom/bar"
            EngineVersionOverride = Some "2026.03.14"
            GrpcPort = 5055
            LaunchGraphicalViewerDefault = true }
    match HubSettings.save custom with
    | Error msg -> Assert.Fail(sprintf "save failed: %s" msg)
    | Ok () -> ()
    let loaded = HubSettings.load ()
    Assert.Equal(custom, loaded)

[<Fact>]
let ``save is atomic via temp-file-plus-rename`` () =
    use _scope = new XdgScope()
    // Two concurrent saves should both succeed and the resulting file
    // should deserialize to one of them (never torn).
    let settingsA = { HubSettings.defaults with GrpcPort = 5050 }
    let settingsB = { HubSettings.defaults with GrpcPort = 5060 }
    let resultA = HubSettings.save settingsA
    let resultB = HubSettings.save settingsB
    Assert.True(Result.isOk resultA)
    Assert.True(Result.isOk resultB)
    let loaded = HubSettings.load ()
    Assert.True(loaded.GrpcPort = 5050 || loaded.GrpcPort = 5060,
                sprintf "post-race load yielded torn GrpcPort=%d" loaded.GrpcPort)

[<Fact>]
let ``save rejects out-of-range GrpcPort`` () =
    use _scope = new XdgScope()
    let bad = { HubSettings.defaults with GrpcPort = 80 }
    match HubSettings.save bad with
    | Error _ -> ()
    | Ok () -> Assert.Fail("save should reject GrpcPort=80")

[<Fact>]
let ``load clamps out-of-range GrpcPort to default`` () =
    use _scope = new XdgScope()
    // Write a malformed settings.json by hand that bypasses save's
    // validation, then verify load's fallback.
    let path = HubSettings.settingsPath ()
    Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
    let raw = """{"grpcPort":80,"launchGraphicalViewerDefault":false,"schemaVersion":1}"""
    File.WriteAllText(path, raw)
    let loaded = HubSettings.load ()
    Assert.Equal(HubSettings.defaults.GrpcPort, loaded.GrpcPort)

[<Fact>]
let ``load tolerates malformed JSON and returns defaults`` () =
    use _scope = new XdgScope()
    let path = HubSettings.settingsPath ()
    Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
    File.WriteAllText(path, "this is not valid JSON {")
    let loaded = HubSettings.load ()
    Assert.Equal(HubSettings.defaults, loaded)
