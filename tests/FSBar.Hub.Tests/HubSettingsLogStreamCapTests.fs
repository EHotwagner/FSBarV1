module FSBar.Hub.Tests.HubSettingsLogStreamCapTests

open System
open System.IO
open Xunit
open FSBar.Hub

type private XdgScope() =
    let previous = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
    let tempDir =
        let p =
            Path.Combine(Path.GetTempPath(), "fsbar-hub-logcap-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(p) |> ignore
        p
    do Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", tempDir)
    interface IDisposable with
        member _.Dispose() =
            Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", previous)
            try Directory.Delete(tempDir, recursive = true) with _ -> ()

// T008 — default value 8.
[<Fact>]
let ``defaults MaxLogStreamSubscribers is 8`` () =
    Assert.Equal(8, HubSettings.defaults.MaxLogStreamSubscribers)

[<Fact>]
let ``defaults schema version is 3`` () =
    Assert.Equal(3, HubSettings.defaults.SchemaVersion)

[<Fact>]
let ``updateMaxLogStreamSubscribers rejects 0`` () =
    match HubSettings.updateMaxLogStreamSubscribers HubSettings.defaults 0 with
    | Ok _ -> Assert.Fail("expected error for 0")
    | Result.Error msg -> Assert.Contains("outside", msg)

[<Fact>]
let ``updateMaxLogStreamSubscribers rejects 33`` () =
    match HubSettings.updateMaxLogStreamSubscribers HubSettings.defaults 33 with
    | Ok _ -> Assert.Fail("expected error for 33")
    | Result.Error msg -> Assert.Contains("outside", msg)

[<Fact>]
let ``updateMaxLogStreamSubscribers accepts 1 and 32`` () =
    match HubSettings.updateMaxLogStreamSubscribers HubSettings.defaults 1 with
    | Result.Error e -> Assert.Fail(sprintf "expected Ok for 1: %s" e)
    | Ok s -> Assert.Equal(1, s.MaxLogStreamSubscribers)
    match HubSettings.updateMaxLogStreamSubscribers HubSettings.defaults 32 with
    | Result.Error e -> Assert.Fail(sprintf "expected Ok for 32: %s" e)
    | Ok s -> Assert.Equal(32, s.MaxLogStreamSubscribers)

// v2 JSON without MaxLogStreamSubscribers loads with default 8, re-saves as v3.
[<Fact>]
let ``v2 json without MaxLogStreamSubscribers loads default and re-saves as v3`` () =
    use _scope = new XdgScope()
    let path = HubSettings.settingsPath ()
    Directory.CreateDirectory(Path.GetDirectoryName(path)) |> ignore
    // Write a v2-shaped file missing the new field entirely.
    let v2Json = """{
  "grpcPort": 5021,
  "launchGraphicalViewerDefault": false,
  "startPausedDefault": true,
  "maxRenderFrameSubscribers": 8,
  "schemaVersion": 2
}"""
    File.WriteAllText(path, v2Json)
    let loaded = HubSettings.load ()
    Assert.Equal(8, loaded.MaxLogStreamSubscribers)

    match HubSettings.save loaded with
    | Result.Error msg -> Assert.Fail(sprintf "save failed: %s" msg)
    | Ok () -> ()
    let raw = File.ReadAllText(path)
    Assert.Contains("\"maxLogStreamSubscribers\": 8", raw)
    Assert.Contains("\"schemaVersion\": 3", raw)
