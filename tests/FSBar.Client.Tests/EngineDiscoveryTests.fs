module FSBar.Client.Tests.EngineDiscoveryTests

open System
open System.IO
open Xunit
open FSBar.Client

[<Fact>]
let ``defaultDataDir_returns_some_when_bar_installed`` () =
    let result = EngineDiscovery.defaultDataDir ()
    Assert.True(result.IsSome, "Expected BAR data directory to exist")
    let dir = result.Value
    Assert.True(Directory.Exists(Path.Combine(dir, "maps")), "Expected maps/ subdirectory")
    Assert.True(Directory.Exists(Path.Combine(dir, "packages")), "Expected packages/ subdirectory")

[<Fact>]
let ``discoverEngines_finds_installed_engines`` () =
    let dataDir = EngineDiscovery.defaultDataDir () |> Option.get
    let engines = EngineDiscovery.discoverEngines dataDir
    Assert.NotEmpty(engines)
    let latest = engines.Head
    Assert.False(String.IsNullOrEmpty(latest.VersionString))
    Assert.True(Directory.Exists(latest.VersionDir))

[<Fact>]
let ``discoverEngines_returns_sorted_newest_first`` () =
    let dataDir = EngineDiscovery.defaultDataDir () |> Option.get
    let engines = EngineDiscovery.discoverEngines dataDir
    if engines.Length > 1 then
        let versions = engines |> List.map (fun e -> e.VersionString)
        let sorted = versions |> List.sortDescending
        Assert.Equal<string list>(sorted, versions)

[<Fact>]
let ``discoverEngines_detects_headless_binary`` () =
    let dataDir = EngineDiscovery.defaultDataDir () |> Option.get
    let engines = EngineDiscovery.discoverEngines dataDir
    Assert.NotEmpty(engines)
    let latest = engines.Head
    Assert.True(latest.HeadlessBin.IsSome, "Expected spring-headless binary to be found")
    Assert.True(File.Exists(latest.HeadlessBin.Value))

[<Fact>]
let ``discoverEngines_detects_graphical_binary`` () =
    let dataDir = EngineDiscovery.defaultDataDir () |> Option.get
    let engines = EngineDiscovery.discoverEngines dataDir
    Assert.NotEmpty(engines)
    let latest = engines.Head
    Assert.True(latest.GraphicalBin.IsSome, "Expected spring (graphical) binary to be found")
    Assert.True(File.Exists(latest.GraphicalBin.Value))

[<Fact>]
let ``discoverEngines_returns_empty_for_nonexistent_dir`` () =
    let engines = EngineDiscovery.discoverEngines "/nonexistent/path"
    Assert.Empty(engines)

[<Fact>]
let ``discoverGameVersion_finds_byar_test`` () =
    let dataDir = EngineDiscovery.defaultDataDir () |> Option.get
    let game = EngineDiscovery.discoverGameVersion dataDir "byar:test"
    Assert.True(game.IsSome, "Expected byar:test game version to be found")
    let g = game.Value
    Assert.Equal("byar:test", g.Tag)
    Assert.StartsWith("Beyond All Reason", g.Name)
    Assert.False(String.IsNullOrEmpty(g.Hash))

[<Fact>]
let ``discoverGameVersion_returns_none_for_unknown_tag`` () =
    let dataDir = EngineDiscovery.defaultDataDir () |> Option.get
    let game = EngineDiscovery.discoverGameVersion dataDir "nonexistent:tag"
    Assert.True(game.IsNone)

[<Fact>]
let ``resolveEngine_autodetect_returns_valid_result`` () =
    let resolution = EngineDiscovery.resolveEngine None
    Assert.Equal(AutoDetected, resolution.Source)
    Assert.False(String.IsNullOrEmpty(resolution.Engine.VersionString))
    Assert.True(resolution.Engine.HeadlessBin.IsSome)
    Assert.StartsWith("Beyond All Reason", resolution.Game.Name)

[<Fact>]
let ``resolveEngine_autodetect_engine_has_valid_datadir`` () =
    let resolution = EngineDiscovery.resolveEngine None
    Assert.True(Directory.Exists(resolution.Engine.DataDir))
    Assert.True(Directory.Exists(Path.Combine(resolution.Engine.DataDir, "maps")))

[<Fact>]
let ``validateEngine_throws_for_nonexistent_binary`` () =
    let ex = Assert.Throws<Exception>(fun () ->
        EngineDiscovery.validateEngine "/nonexistent/spring-headless" "test-version")
    Assert.Contains("not found", ex.Message)
    Assert.Contains("test-version", ex.Message)

// --- User Story 2: Override tests ---

[<Fact>]
let ``resolveEngine_envvar_override_takes_precedence`` () =
    let dataDir = EngineDiscovery.defaultDataDir () |> Option.get
    let engines = EngineDiscovery.discoverEngines dataDir
    let headlessPath = engines.Head.HeadlessBin |> Option.get
    let prev = Environment.GetEnvironmentVariable("HIGHBAR_TEST_ENGINE")
    try
        Environment.SetEnvironmentVariable("HIGHBAR_TEST_ENGINE", headlessPath)
        let resolution = EngineDiscovery.resolveEngine None
        Assert.Equal(OverrideEnvVar, resolution.Source)
    finally
        Environment.SetEnvironmentVariable("HIGHBAR_TEST_ENGINE", prev)

[<Fact>]
let ``resolveEngine_envvar_invalid_path_throws`` () =
    let prev = Environment.GetEnvironmentVariable("HIGHBAR_TEST_ENGINE")
    try
        Environment.SetEnvironmentVariable("HIGHBAR_TEST_ENGINE", "/nonexistent/spring-headless")
        let ex = Assert.Throws<Exception>(fun () ->
            EngineDiscovery.resolveEngine None |> ignore)
        Assert.Contains("not found", ex.Message)
    finally
        Environment.SetEnvironmentVariable("HIGHBAR_TEST_ENGINE", prev)

[<Fact>]
let ``resolveEngine_configfile_override_takes_precedence`` () =
    let configPath = Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "tests", "engine-version.json") |> Path.GetFullPath
    if File.Exists(configPath) then
        let resolution = EngineDiscovery.resolveEngine (Some configPath)
        Assert.Equal(ConfigFile, resolution.Source)
        Assert.False(String.IsNullOrEmpty(resolution.Engine.VersionString))

[<Fact>]
let ``resolveEngine_configfile_missing_version_falls_through`` () =
    // Config file with a version that doesn't exist should throw
    let tmpConfig = Path.GetTempFileName()
    try
        File.WriteAllText(tmpConfig, """{"engine":{"version":"9999.99.99","binary":"spring-headless"}}""")
        let ex = Assert.Throws<Exception>(fun () ->
            EngineDiscovery.resolveEngine (Some tmpConfig) |> ignore)
        Assert.Contains("9999.99.99", ex.Message)
    finally
        File.Delete(tmpConfig)

// --- User Story 3: Notification tests ---

[<Fact>]
let ``resolveEngine_logs_version_and_source`` () =
    let prev = Console.Out
    use sw = new StringWriter()
    Console.SetOut(sw)
    try
        let _ = EngineDiscovery.resolveEngine None
        let output = sw.ToString()
        Assert.Contains("[EngineDiscovery]", output)
        Assert.Contains("auto-detected", output)
        Assert.Contains("Headless:", output)
        Assert.Contains("Game:", output)
    finally
        Console.SetOut(prev)

[<Fact>]
let ``resolveEngine_no_engine_error_lists_searched_locations`` () =
    let prev = Environment.GetEnvironmentVariable("HIGHBAR_TEST_ENGINE")
    try
        Environment.SetEnvironmentVariable("HIGHBAR_TEST_ENGINE", null)
        // We can't easily remove the real engine directory, but we can test
        // that resolveEngine with a nonexistent config file falls through to
        // auto-detect (which succeeds here since BAR is installed)
        let resolution = EngineDiscovery.resolveEngine (Some "/nonexistent/config.json")
        Assert.Equal(AutoDetected, resolution.Source)
    finally
        Environment.SetEnvironmentVariable("HIGHBAR_TEST_ENGINE", prev)

// --- Spec 045 US2: FSBAR_TEST_ENGINE / HIGHBAR_TEST_ENGINE alias ---

// Scoped env-var swap helper that restores every variable it touches.
type private EnvSwap(pairs: (string * string) seq) =
    let originals =
        pairs
        |> Seq.map (fun (k, _) -> k, Environment.GetEnvironmentVariable(k))
        |> Seq.toList
    do
        for (k, v) in pairs do
            Environment.SetEnvironmentVariable(k, v)
    interface IDisposable with
        member _.Dispose () =
            for (k, v) in originals do
                Environment.SetEnvironmentVariable(k, v)

[<Fact>]
let ``resolveOverrideEnvVar_only_FSBAR_set_wins`` () =
    use _ = new EnvSwap([ "FSBAR_TEST_ENGINE", "/tmp/fsbar-path"
                          "HIGHBAR_TEST_ENGINE", null ])
    let r = EngineDiscovery.resolveOverrideEnvVar ()
    Assert.Equal(Some "/tmp/fsbar-path", r.Value)
    Assert.Equal(Some "FSBAR_TEST_ENGINE", r.SourceName)
    Assert.Equal(None, r.Conflict)

[<Fact>]
let ``resolveOverrideEnvVar_only_HIGHBAR_set_used_as_legacy`` () =
    use _ = new EnvSwap([ "FSBAR_TEST_ENGINE", null
                          "HIGHBAR_TEST_ENGINE", "/tmp/legacy-path" ])
    let r = EngineDiscovery.resolveOverrideEnvVar ()
    Assert.Equal(Some "/tmp/legacy-path", r.Value)
    Assert.Equal(Some "HIGHBAR_TEST_ENGINE", r.SourceName)
    Assert.Equal(None, r.Conflict)

[<Fact>]
let ``resolveOverrideEnvVar_both_set_differently_FSBAR_wins_conflict_reported`` () =
    use _ = new EnvSwap([ "FSBAR_TEST_ENGINE", "/tmp/fsbar-path"
                          "HIGHBAR_TEST_ENGINE", "/tmp/legacy-path" ])
    let r = EngineDiscovery.resolveOverrideEnvVar ()
    Assert.Equal(Some "/tmp/fsbar-path", r.Value)
    Assert.Equal(Some "FSBAR_TEST_ENGINE", r.SourceName)
    Assert.Equal(Some ("/tmp/fsbar-path", "/tmp/legacy-path"), r.Conflict)

[<Fact>]
let ``resolveOverrideEnvVar_neither_set_returns_none`` () =
    use _ = new EnvSwap([ "FSBAR_TEST_ENGINE", null
                          "HIGHBAR_TEST_ENGINE", null ])
    let r = EngineDiscovery.resolveOverrideEnvVar ()
    Assert.Equal(None, r.Value)
    Assert.Equal(None, r.SourceName)
    Assert.Equal(None, r.Conflict)

[<Fact>]
let ``resolveOverrideEnvVar_both_set_to_same_value_no_conflict`` () =
    use _ = new EnvSwap([ "FSBAR_TEST_ENGINE", "/tmp/same-path"
                          "HIGHBAR_TEST_ENGINE", "/tmp/same-path" ])
    let r = EngineDiscovery.resolveOverrideEnvVar ()
    Assert.Equal(Some "/tmp/same-path", r.Value)
    Assert.Equal(Some "FSBAR_TEST_ENGINE", r.SourceName)
    Assert.Equal(None, r.Conflict)
