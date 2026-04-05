namespace FSBar.LiveTests

open System
open System.Diagnostics
open System.IO
open Xunit
open FSBar.Client

/// Manages the lifecycle of a headless BAR engine instance for integration tests.
/// Creates a BarClient, starts the engine, captures warm-up frames, and tears down cleanly.
type EngineFixture() =
    let mutable client: BarClient option = None
    let mutable initialFrames: GameFrame list = []
    let mutable initialEvents: GameEvent list = []

    let testsDir =
        let assemblyDir = Path.GetDirectoryName(typeof<EngineFixture>.Assembly.Location)
        let testProjectDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", ".."))
        Path.GetFullPath(Path.Combine(testProjectDir, ".."))

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

        // Parse JSON to extract engine path and datadir for BarClient config
        let json = stdout.Trim()
        let engineIdx = json.IndexOf("\"engine\":\"")
        let datadirIdx = json.IndexOf("\"datadir\":\"")

        let extractValue (s: string) (prefix: string) =
            let start = s.IndexOf(prefix) + prefix.Length
            let endIdx = s.IndexOf("\"", start)
            s.Substring(start, endIdx - start)

        let enginePath = extractValue json "\"engine\":\""
        let dataDir = extractValue json "\"datadir\":\""
        (enginePath, dataDir)

    /// The shared BarClient connected to the engine. Use this in tests.
    member _.Client =
        client |> Option.defaultWith (fun () -> failwith "Client not initialized")

    /// Frames captured during initial warm-up (first 30 frames after handshake).
    member _.InitialFrames = initialFrames

    /// All events from the initial warm-up frames (Init, UnitCreated, etc.).
    member _.InitialEvents = initialEvents

    /// Check if the engine process is still alive.
    member _.IsEngineAlive =
        match client with
        | Some c -> c.State = Connected || c.State = Running
        | None -> false

    /// Get diagnostic info for debugging test failures.
    member _.GetDiagnostics() =
        match client with
        | Some c ->
            let config = c.Config
            let sessionDir = EngineLauncher.getSessionDir config
            let mutable output = $"Session: {sessionDir}\nSocket: {config.SocketPath}\nState: {c.State}\n"
            for logFile in ["stdout.log"; "stderr.log"; "infolog.txt"] do
                let path = Path.Combine(sessionDir, logFile)
                if File.Exists(path) then
                    let lines = File.ReadAllLines(path)
                    let tail = lines |> Array.skip (max 0 (lines.Length - 30))
                    output <- output + $"\n--- {logFile} (last {tail.Length} lines) ---\n"
                    output <- output + (String.Join("\n", tail)) + "\n"
            output
        | None -> "No client initialized."

    interface IAsyncLifetime with
        member _.InitializeAsync() = task {
            let (enginePath, dataDir) = checkPrerequisites ()

            let config =
                { EngineConfig.defaultConfig () with
                    EngineBin = enginePath
                    SpringDataDir = Some dataDir }

            let c = new BarClient(config)
            c.Start()

            // Warm-up: capture first 30 frames with one-time events
            let warmupFrames = ResizeArray<GameFrame>()
            for _ in 1..30 do
                let frame = c.Step()
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

            // Clean up session directory
            sessionDir |> Option.iter (fun dir ->
                if Directory.Exists(dir) then
                    try Directory.Delete(dir, true) with _ -> ()
            )

            Threading.Tasks.Task.CompletedTask


/// xUnit collection definition that serializes all engine-dependent tests
/// against a single shared EngineFixture instance.
[<CollectionDefinition("Engine")>]
type EngineCollection() =
    interface ICollectionFixture<EngineFixture>
