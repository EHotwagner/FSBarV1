module FSBar.Hub.Tests.BundledProxyTests

open System
open System.IO
open Xunit
open FSBar.Hub

/// Scope that points `FSBAR_HUB_BUNDLED_PROXY_DIR` at a fresh tmp dir
/// and cleans up on Dispose.
type private ProxyScope() =
    let previous = Environment.GetEnvironmentVariable("FSBAR_HUB_BUNDLED_PROXY_DIR")
    let tempDir =
        let p = Path.Combine(Path.GetTempPath(), "fsbar-proxy-test-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(p) |> ignore
        p
    do Environment.SetEnvironmentVariable("FSBAR_HUB_BUNDLED_PROXY_DIR", tempDir)
    member _.Root = tempDir
    member _.WriteVersion (contents: string) =
        File.WriteAllText(Path.Combine(tempDir, "BUNDLED_VERSION"), contents)
    member _.WriteBundle (version: string) (files: (string * string) list) =
        let dir = Path.Combine(tempDir, "bundled", version)
        Directory.CreateDirectory(dir) |> ignore
        for (name, content) in files do
            File.WriteAllText(Path.Combine(dir, name), content)
    interface IDisposable with
        member _.Dispose() =
            Environment.SetEnvironmentVariable("FSBAR_HUB_BUNDLED_PROXY_DIR", previous)
            try Directory.Delete(tempDir, recursive = true) with _ -> ()

let private fullBundle =
    [ "libSkirmishAI.so", "\x7fELF stub"
      "AIInfo.lua", "-- AIInfo stub"
      "AIOptions.lua", "-- AIOptions stub" ]

[<Fact>]
let ``resolve returns full info when bundle is complete`` () =
    use scope = new ProxyScope()
    scope.WriteVersion "0.1.17\n"
    scope.WriteBundle "0.1.17" fullBundle
    match BundledProxy.resolve () with
    | Ok info ->
        Assert.Equal("0.1.17", info.Version)
        Assert.StartsWith(scope.Root, info.BundleRoot)
        Assert.EndsWith(Path.Combine("0.1.17"), info.BundleRoot)
        Assert.True(File.Exists(info.LibSkirmishAiPath))
        Assert.True(File.Exists(info.AiInfoLuaPath))
        Assert.True(File.Exists(info.AiOptionsLuaPath))
    | Error e -> Assert.Fail(sprintf "resolve failed: %s" (BundledProxy.formatError e))

[<Fact>]
let ``VersionFileMissing when BUNDLED_VERSION absent`` () =
    use _scope = new ProxyScope()
    match BundledProxy.resolve () with
    | Error (BundledProxy.VersionFileMissing _) -> ()
    | other -> Assert.Fail(sprintf "expected VersionFileMissing, got %A" other)

[<Fact>]
let ``VersionFileMalformed when contents empty`` () =
    use scope = new ProxyScope()
    scope.WriteVersion ""
    match BundledProxy.resolve () with
    | Error (BundledProxy.VersionFileMalformed _) -> ()
    | other -> Assert.Fail(sprintf "expected VersionFileMalformed (empty), got %A" other)

[<Fact>]
let ``VersionFileMalformed when multiple non-blank lines`` () =
    use scope = new ProxyScope()
    scope.WriteVersion "0.1.17\n0.1.18\n"
    match BundledProxy.resolve () with
    | Error (BundledProxy.VersionFileMalformed _) -> ()
    | other -> Assert.Fail(sprintf "expected VersionFileMalformed (multi-line), got %A" other)

[<Fact>]
let ``BundleDirMissing when named version dir absent`` () =
    use scope = new ProxyScope()
    scope.WriteVersion "0.2.0\n"
    match BundledProxy.resolve () with
    | Error (BundledProxy.BundleDirMissing _) -> ()
    | other -> Assert.Fail(sprintf "expected BundleDirMissing, got %A" other)

[<Fact>]
let ``RequiredFileMissing when bundle dir lacks libSkirmishAI`` () =
    use scope = new ProxyScope()
    scope.WriteVersion "0.1.17\n"
    scope.WriteBundle "0.1.17"
        [ "AIInfo.lua", "-- only lua"
          "AIOptions.lua", "-- only lua" ]
    match BundledProxy.resolve () with
    | Error (BundledProxy.RequiredFileMissing path) ->
        Assert.EndsWith("libSkirmishAI.so", path)
    | other -> Assert.Fail(sprintf "expected RequiredFileMissing, got %A" other)

[<Fact>]
let ``RequiredFileMissing when bundle dir lacks AIOptions`` () =
    use scope = new ProxyScope()
    scope.WriteVersion "0.1.17\n"
    scope.WriteBundle "0.1.17"
        [ "libSkirmishAI.so", "stub"
          "AIInfo.lua", "stub" ]
    match BundledProxy.resolve () with
    | Error (BundledProxy.RequiredFileMissing path) ->
        Assert.EndsWith("AIOptions.lua", path)
    | other -> Assert.Fail(sprintf "expected RequiredFileMissing, got %A" other)

[<Fact>]
let ``BUNDLED_VERSION with surrounding whitespace is accepted`` () =
    use scope = new ProxyScope()
    scope.WriteVersion "  0.1.17   \n\n"
    scope.WriteBundle "0.1.17" fullBundle
    match BundledProxy.resolve () with
    | Ok info -> Assert.Equal("0.1.17", info.Version)
    | Error e -> Assert.Fail(sprintf "resolve failed: %s" (BundledProxy.formatError e))
