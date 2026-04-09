namespace FSBar.Viz.Tests

open System
open System.Diagnostics
open System.IO
open Xunit
open FSBar.Client

/// Manages the lifecycle of a headless BAR engine instance for viz integration tests.
type VizEngineFixture() =
    let mutable client: BarClient option = None
    let mutable initialFrames: GameFrame list = []
    let mutable initialEvents: GameEvent list = []

    let testsDir =
        let assemblyDir = Path.GetDirectoryName(typeof<VizEngineFixture>.Assembly.Location)
        let testProjectDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", ".."))
        // Navigate to tests/ directory which contains check-prerequisites.sh
        Path.GetFullPath(Path.Combine(testProjectDir, "..", "..", "tests"))

    let checkPrereqScript = Path.Combine(testsDir, "check-prerequisites.sh")

    let checkPrerequisites () =
        let psi = ProcessStartInfo()
        psi.FileName <- "/usr/bin/env"
        psi.ArgumentList.Add("bash")
        psi.ArgumentList.Add(checkPrereqScript)
        psi.ArgumentList.Add("--json")
        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true

        use proc = Process.Start(psi)
        let stdout = proc.StandardOutput.ReadToEnd()
        let stderr = proc.StandardError.ReadToEnd()
        proc.WaitForExit()

        if proc.ExitCode = 2 then
            failwith $"Prerequisites check script error: {stderr}{stdout}"
        elif proc.ExitCode <> 0 then
            failwith $"Prerequisites not met — skipping live engine tests.\n{stdout}"

        let json = stdout.Trim()

        let extractValue (s: string) (prefix: string) =
            let start = s.IndexOf(prefix) + prefix.Length
            let endIdx = s.IndexOf("\"", start)
            s.Substring(start, endIdx - start)

        let enginePath = extractValue json "\"engine\":\""
        let dataDir = extractValue json "\"datadir\":\""
        (enginePath, dataDir)

    member _.Client =
        client |> Option.defaultWith (fun () -> failwith "Client not initialized")

    member _.InitialFrames = initialFrames
    member _.InitialEvents = initialEvents

    interface IAsyncLifetime with
        member _.InitializeAsync() = task {
            let (enginePath, dataDir) = checkPrerequisites ()

            let config =
                { EngineConfig.defaultConfig () with
                    EngineBin = enginePath
                    SpringDataDir = Some dataDir }

            let c = new BarClient(config)
            c.Start()

            let warmupFrames = ResizeArray<GameFrame>()
            for frame in c.Frames |> Seq.truncate 30 do
                warmupFrames.Add(frame)

            initialFrames <- warmupFrames |> Seq.toList
            initialEvents <- initialFrames |> List.collect (fun f -> f.Events)
            client <- Some c
        }

        member _.DisposeAsync() =
            let sessionDir =
                client |> Option.map (fun c -> EngineLauncher.getSessionDir c.Config)

            client |> Option.iter (fun c ->
                try c.Stop() with _ -> ()
            )
            client <- None

            sessionDir |> Option.iter (fun dir ->
                if Directory.Exists(dir) then
                    try Directory.Delete(dir, true) with _ -> ()
            )

            Threading.Tasks.Task.CompletedTask


[<CollectionDefinition("VizEngine")>]
type VizEngineCollection() =
    interface ICollectionFixture<VizEngineFixture>
