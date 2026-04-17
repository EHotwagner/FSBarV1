module FSBar.Hub.Tests.ProxyInstallerIdempotencyTests

// Integration-style tests against a synthetic BAR install. Verifies
// tasks T036 (idempotency: every step Skipped, no mtime changes) and
// T036a (newer on-disk libSkirmishAI.so behavior without/with force).

open System
open System.Collections.Concurrent
open System.IO
open System.Threading
open Xunit
open FSBar.Hub

/// One fake BAR install + one fake bundled proxy layout, isolated
/// to a temp dir and cleaned up on Dispose.
type private Sandbox() =
    let root =
        let p = Path.Combine(Path.GetTempPath(), "fsbar-proxyinst-test-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(p) |> ignore
        p

    let dataDir = Path.Combine(root, "bar-data")
    let engineVersion = "2026.03.14"
    let engineDir = Path.Combine(dataDir, "engine", "recoil_" + engineVersion)
    let proxyRoot = Path.Combine(root, "proxy")
    let bundleVersion = "0.1.17"
    let bundleDir = Path.Combine(proxyRoot, "bundled", bundleVersion)
    let previousProxyEnv = Environment.GetEnvironmentVariable("FSBAR_HUB_BUNDLED_PROXY_DIR")

    do
        // Data dir — maps/, packages/ (for EngineDiscovery.defaultDataDir-
        // shaped validity), and a recoil_* engine with spring-headless.
        Directory.CreateDirectory(Path.Combine(dataDir, "maps")) |> ignore
        Directory.CreateDirectory(Path.Combine(dataDir, "packages")) |> ignore
        Directory.CreateDirectory(engineDir) |> ignore
        let hb = Path.Combine(engineDir, "spring-headless")
        File.WriteAllText(hb, "#!/bin/sh\nexit 0")
        File.SetUnixFileMode(
            hb,
            UnixFileMode.UserRead ||| UnixFileMode.UserWrite ||| UnixFileMode.UserExecute)

        // Bundled proxy root + BUNDLED_VERSION + files.
        Directory.CreateDirectory(bundleDir) |> ignore
        File.WriteAllBytes(Path.Combine(bundleDir, "libSkirmishAI.so"), [| 0x7fuy; 0x45uy; 0x4cuy; 0x46uy |])
        File.WriteAllText(Path.Combine(bundleDir, "AIInfo.lua"), "return { name = 'HighBarV2' }\n")
        File.WriteAllText(Path.Combine(bundleDir, "AIOptions.lua"), "return {}\n")
        File.WriteAllText(Path.Combine(proxyRoot, "BUNDLED_VERSION"), bundleVersion + "\n")

        Environment.SetEnvironmentVariable("FSBAR_HUB_BUNDLED_PROXY_DIR", proxyRoot)

    member _.DataDir = dataDir
    member _.EngineVersion = engineVersion
    member _.BundleVersion = bundleVersion
    member _.BundleDir = bundleDir

    /// Pre-install the proxy into the simulated BAR engine so
    /// subsequent install calls should be entirely Skipped.
    member _.PreInstallProxy() =
        let dest = Path.Combine(engineDir, "AI", "Skirmish", "HighBarV2", bundleVersion)
        Directory.CreateDirectory(dest) |> ignore
        for f in [ "libSkirmishAI.so"; "AIInfo.lua"; "AIOptions.lua" ] do
            File.Copy(Path.Combine(bundleDir, f), Path.Combine(dest, f), overwrite = true)
        // devmode.txt and IGL_data.lua already satisfied.
        File.WriteAllText(Path.Combine(dataDir, "devmode.txt"), "")
        let iglDir = Path.Combine(dataDir, "LuaMenu", "Config")
        Directory.CreateDirectory(iglDir) |> ignore
        File.WriteAllText(
            Path.Combine(iglDir, "IGL_data.lua"),
            "return {\n\t[\"Menu\"] = {\n\t\tsimpleAiList = false,\n\t},\n}\n")
        dest

    member this.Resolve() : BarInstall.BarInstall * BundledProxy.BundledProxyInfo =
        let settings = { HubSettings.defaults with BarDataDirOverride = Some this.DataDir }
        match BarInstall.detect settings with
        | Result.Error e -> failwith (BarInstall.formatError e)
        | Ok install ->
            match BundledProxy.resolve () with
            | Result.Error e -> failwith (BundledProxy.formatError e)
            | Ok bundled -> install, bundled

    interface IDisposable with
        member _.Dispose() =
            Environment.SetEnvironmentVariable("FSBAR_HUB_BUNDLED_PROXY_DIR", previousProxyEnv)
            try Directory.Delete(root, recursive = true) with _ -> ()

let private collectEvents () =
    let q = ConcurrentQueue<HubEvents.HubEvent>()
    let bus = HubEvents.create ()
    let sub =
        bus.Events.Subscribe(
            { new IObserver<HubEvents.HubEvent> with
                member _.OnNext(e) = q.Enqueue(e)
                member _.OnError(_) = ()
                member _.OnCompleted() = () })
    bus, sub, q

[<Fact>]
let ``install into empty install writes all three steps`` () =
    use fake = new Sandbox()
    let install, bundled = fake.Resolve()
    let bus, sub, events = collectEvents ()
    try
        match ProxyInstaller.install install bundled bus.Sink false with
        | Result.Error errs ->
            Assert.Fail(sprintf "install failed: %s" (String.concat "; " errs))
        | Ok status ->
            Assert.Equal(Some fake.BundleVersion, status.InstalledVersion)
            Assert.True(status.AiFilesPresent)
            Assert.True(status.DevModeFilePresent)
            // IGL_data.lua was absent — ToggleSimpleAiList skipped,
            // so SimpleAiListDisabled stays false. That's expected
            // until the user opens Chobby once.
            Assert.False(status.SimpleAiListDisabled)
        // Wait briefly for the async event pump.
        Thread.Sleep(100)
        let evts = events.ToArray()
        let outcomes =
            evts
            |> Array.choose (function
                | HubEvents.ProxyInstallProgress(step, outcome) -> Some (step, outcome)
                | _ -> None)
        // CopyAiFiles Performed + TouchDevMode Performed + ToggleSimpleAiList Skipped.
        Assert.Contains(outcomes, (fun (s, o) -> s = HubEvents.CopyAiFiles && o = HubEvents.Performed))
        Assert.Contains(outcomes, (fun (s, o) -> s = HubEvents.TouchDevMode && o = HubEvents.Performed))
        Assert.Contains(outcomes, (fun (s, o) -> s = HubEvents.ToggleSimpleAiList && o = HubEvents.Skipped))
    finally
        sub.Dispose()
        (bus :> IDisposable).Dispose()

[<Fact>]
let ``install on pre-installed proxy is entirely Skipped`` () =
    use fake = new Sandbox()
    let dest = fake.PreInstallProxy()
    let install, bundled = fake.Resolve()
    // Capture mtimes before.
    let beforeMtimes =
        [ for f in Directory.GetFiles(dest) do
              yield f, File.GetLastWriteTimeUtc(f) ]
    let bus, sub, events = collectEvents ()
    try
        match ProxyInstaller.install install bundled bus.Sink false with
        | Result.Error errs ->
            Assert.Fail(sprintf "install failed: %s" (String.concat "; " errs))
        | Ok status ->
            Assert.True(status.AiFilesPresent)
            Assert.True(status.DevModeFilePresent)
            Assert.True(status.SimpleAiListDisabled)
            Assert.True(status.MatchesBundled)
        Thread.Sleep(100)
        let outcomes =
            events.ToArray()
            |> Array.choose (function
                | HubEvents.ProxyInstallProgress(step, outcome) -> Some (step, outcome)
                | _ -> None)
        // Every step MUST report Skipped — SC-008 byte-idempotency.
        Assert.Contains(outcomes, (fun (s, o) -> s = HubEvents.CopyAiFiles && o = HubEvents.Skipped))
        Assert.Contains(outcomes, (fun (s, o) -> s = HubEvents.TouchDevMode && o = HubEvents.Skipped))
        Assert.Contains(outcomes, (fun (s, o) -> s = HubEvents.ToggleSimpleAiList && o = HubEvents.Skipped))
        // mtimes of every existing file are unchanged.
        for (f, before) in beforeMtimes do
            Assert.Equal(before, File.GetLastWriteTimeUtc(f))
    finally
        sub.Dispose()
        (bus :> IDisposable).Dispose()

[<Fact>]
let ``install refuses to overwrite a newer local libSkirmishAI.so without force`` () =
    use fake = new Sandbox()
    let dest = fake.PreInstallProxy()
    // Overwrite the destination with DIFFERENT bytes AND advance mtime
    // to the future so the "local is newer" branch fires.
    let libPath = Path.Combine(dest, "libSkirmishAI.so")
    File.WriteAllBytes(libPath, [| 0x7fuy; 0x45uy; 0x4cuy; 0x46uy; 0xffuy; 0xffuy |])
    File.SetLastWriteTimeUtc(libPath, DateTime.UtcNow.AddHours(1.0))
    let sizeBefore = FileInfo(libPath).Length
    let install, bundled = fake.Resolve()
    let bus, sub, events = collectEvents ()
    try
        // Without force — the copy step should fail / skip with a
        // warning; TouchDevMode + ToggleSimpleAiList still run.
        match ProxyInstaller.install install bundled bus.Sink false with
        | Result.Error errs ->
            Assert.Contains(errs, (fun e -> e.StartsWith("CopyAiFiles")))
        | Ok _ -> Assert.Fail("install should surface a step failure when local is newer")
        Assert.Equal(sizeBefore, FileInfo(libPath).Length)
        Thread.Sleep(100)
        let copyOutcomes =
            events.ToArray()
            |> Array.choose (function
                | HubEvents.ProxyInstallProgress(HubEvents.CopyAiFiles, outcome) -> Some outcome
                | _ -> None)
        Assert.Contains(copyOutcomes, function HubEvents.StepFailed reason -> reason.Contains("newer") | _ -> false)
    finally
        sub.Dispose()
        (bus :> IDisposable).Dispose()

[<Fact>]
let ``install with force overwrites a newer local libSkirmishAI.so`` () =
    use fake = new Sandbox()
    let dest = fake.PreInstallProxy()
    let libPath = Path.Combine(dest, "libSkirmishAI.so")
    File.WriteAllBytes(libPath, [| 0x7fuy; 0x45uy; 0x4cuy; 0x46uy; 0xffuy; 0xffuy |])
    File.SetLastWriteTimeUtc(libPath, DateTime.UtcNow.AddHours(1.0))
    let install, bundled = fake.Resolve()
    let bus, sub, events = collectEvents ()
    try
        match ProxyInstaller.install install bundled bus.Sink true with
        | Result.Error errs ->
            Assert.Fail(sprintf "install with force should succeed; got %s" (String.concat "; " errs))
        | Ok _ -> ()
        // After force overwrite, size matches the bundled file.
        Assert.Equal(
            FileInfo(Path.Combine(fake.BundleDir, "libSkirmishAI.so")).Length,
            FileInfo(libPath).Length)
        Thread.Sleep(100)
        let copyOutcomes =
            events.ToArray()
            |> Array.choose (function
                | HubEvents.ProxyInstallProgress(HubEvents.CopyAiFiles, outcome) -> Some outcome
                | _ -> None)
        Assert.Contains(copyOutcomes, (fun o -> o = HubEvents.Performed))
    finally
        sub.Dispose()
        (bus :> IDisposable).Dispose()

[<Fact>]
let ``install rewrites simpleAiList when IGL_data.lua exists with true`` () =
    use fake = new Sandbox()
    let iglDir = Path.Combine(fake.DataDir, "LuaMenu", "Config")
    Directory.CreateDirectory(iglDir) |> ignore
    let iglPath = Path.Combine(iglDir, "IGL_data.lua")
    File.WriteAllText(
        iglPath,
        "return {\n\t[\"Menu\"] = {\n\t\tsimpleAiList = true,\n\t},\n}\n")
    let install, bundled = fake.Resolve()
    let bus, sub, events = collectEvents ()
    try
        match ProxyInstaller.install install bundled bus.Sink false with
        | Result.Error errs ->
            Assert.Fail(sprintf "install failed: %s" (String.concat "; " errs))
        | Ok status ->
            Assert.True(status.SimpleAiListDisabled)
        Thread.Sleep(100)
        let contents = File.ReadAllText(iglPath)
        Assert.Contains("simpleAiList = false,", contents)
    finally
        sub.Dispose()
        (bus :> IDisposable).Dispose()
