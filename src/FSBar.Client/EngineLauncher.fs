namespace FSBar.Client

open System
open System.Diagnostics
open System.IO

module EngineLauncher =

    /// Extract the guid portion from a socket path like /tmp/fsbar-abcd1234.sock
    let extractGuid (socketPath: string) =
        let fileName = Path.GetFileNameWithoutExtension(socketPath)

        if fileName.StartsWith("fsbar-") then
            fileName.Substring(6)
        else
            Guid.NewGuid().ToString("N").[..7]

    /// Get the session directory for a given config.
    let getSessionDir (config: EngineConfig) : string =
        let guid = extractGuid config.SocketPath
        $"/tmp/fsbar-{guid}"

    /// Auto-detect SPRING_DATADIR from the engine binary location or standard paths.
    /// First tries walking up from the binary location, then checks the standard BAR data directory.
    let detectSpringDataDir (engineBin: string) : string option =
        let tryBinaryParent () =
            try
                let psi =
                    ProcessStartInfo(
                        FileName = "which",
                        Arguments = engineBin,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    )

                use proc = Process.Start(psi)
                let output = proc.StandardOutput.ReadToEnd().Trim()
                proc.WaitForExit()

                if proc.ExitCode <> 0 || String.IsNullOrEmpty(output) then
                    None
                else
                    let binDir = Path.GetDirectoryName(output)
                    let parent = Directory.GetParent(binDir)

                    if isNull parent || isNull parent.Parent then
                        None
                    else
                        let candidate = parent.Parent.FullName
                        let hasMapDir = Directory.Exists(Path.Combine(candidate, "maps"))
                        let hasPackagesDir = Directory.Exists(Path.Combine(candidate, "packages"))

                        if hasMapDir && hasPackagesDir then
                            Some candidate
                        else
                            None
            with
            | _ -> None

        let tryStandardPath () =
            let candidate =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local/state/Beyond All Reason")
            if Directory.Exists(Path.Combine(candidate, "maps"))
               && Directory.Exists(Path.Combine(candidate, "packages")) then
                Some candidate
            else
                None

        tryBinaryParent () |> Option.orElseWith tryStandardPath

    /// Copy ArchiveCache20.lua from SPRING_DATADIR/cache/ to sessionDir/cache/ if available.
    let copyArchiveCache (springDataDir: string) (sessionDir: string) : unit =
        let sourcePath = Path.Combine(springDataDir, "cache", "ArchiveCache20.lua")

        if File.Exists(sourcePath) then
            let destDir = Path.Combine(sessionDir, "cache")
            Directory.CreateDirectory(destDir) |> ignore
            let destPath = Path.Combine(destDir, "ArchiveCache20.lua")
            File.Copy(sourcePath, destPath, overwrite = true)
            printfn "Copied ArchiveCache20.lua to %s" destPath

    /// Write the PID to a .pid file alongside the socket path.
    let writePidFile (socketPath: string) (pid: int) : unit =
        let pidPath = $"{socketPath}.pid"
        File.WriteAllText(pidPath, string pid)

    /// Launch an engine process with the given binary, config, and script content.
    let launchEngine (engineBinary: string) (config: EngineConfig) (scriptContent: string) : Process =
        let sessionDir = getSessionDir config
        Directory.CreateDirectory(sessionDir) |> ignore

        // Write script file
        let scriptPath = Path.Combine(sessionDir, "script.txt")
        File.WriteAllText(scriptPath, scriptContent)

        // Resolve SPRING_DATADIR
        let springDataDir =
            match config.SpringDataDir with
            | Some dir -> Some dir
            | None -> detectSpringDataDir engineBinary

        // Copy archive cache if available
        springDataDir |> Option.iter (fun dir -> copyArchiveCache dir sessionDir)

        // Write springsettings.cfg — forces windowed mode and, when
        // EngineConfig.AutohostPort is set, emits the autohost interface
        // lines so the engine dials back to the hub's pre-bound UDP
        // socket (feature 039 admin channel).
        let settingsPath = Path.Combine(sessionDir, "springsettings.cfg")
        File.WriteAllText(settingsPath, ScriptGenerator.generateSpringSettings config)

        // When FSBAR_ENGINE_WRAPPER is set to an executable path, run
        // the engine through it — the wrapper can install
        // prctl(PR_SET_PDEATHSIG, SIGTERM) so the engine is killed
        // by the kernel if the parent hub crashes (bypassing the
        // normal SIGTERM-on-exit path). scripts/hub-spawn-engine.sh
        // is the canonical wrapper; the trainer can opt in by
        // setting the same env var.
        let wrapper =
            match Environment.GetEnvironmentVariable("FSBAR_ENGINE_WRAPPER") with
            | null | "" -> None
            | path when File.Exists(path) -> Some path
            | path ->
                eprintfn "[EngineLauncher] FSBAR_ENGINE_WRAPPER=%s not found — launching directly" path
                None

        let psi =
            match wrapper with
            | Some w ->
                ProcessStartInfo(
                    FileName = w,
                    Arguments = sprintf "\"%s\" \"%s\"" engineBinary scriptPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                )
            | None ->
                ProcessStartInfo(
                    FileName = engineBinary,
                    Arguments = scriptPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                )

        psi.Environment.["HIGHBAR_SOCKET_PATH"] <- config.SocketPath
        psi.Environment.["SPRING_WRITEDIR"] <- sessionDir

        springDataDir
        |> Option.iter (fun dir -> psi.Environment.["SPRING_DATADIR"] <- dir)

        let proc = Process.Start(psi)

        // Redirect stdout/stderr to files in session dir
        let stdoutPath = Path.Combine(sessionDir, "stdout.log")
        let stderrPath = Path.Combine(sessionDir, "stderr.log")

        let stdoutWriter = new StreamWriter(stdoutPath, append = false)
        let stderrWriter = new StreamWriter(stderrPath, append = false)

        proc.OutputDataReceived.Add(fun args ->
            if not (isNull args.Data) then
                stdoutWriter.WriteLine(args.Data)
                stdoutWriter.Flush())

        proc.ErrorDataReceived.Add(fun args ->
            if not (isNull args.Data) then
                stderrWriter.WriteLine(args.Data)
                stderrWriter.Flush())

        proc.BeginOutputReadLine()
        proc.BeginErrorReadLine()

        // Write PID file
        writePidFile config.SocketPath proc.Id

        printfn "Engine started (PID %d)" proc.Id
        proc

    /// Launch the headless engine (spring-headless) with the given script content.
    let launchHeadless (config: EngineConfig) (scriptContent: string) : Process =
        launchEngine config.EngineBin config scriptContent

    /// Launch the graphical engine (AppImage) with the given script content.
    let launchGraphical (config: EngineConfig) (scriptContent: string) : Process =
        launchEngine config.AppImagePath config scriptContent

    /// Stop an engine process gracefully (SIGTERM), then forcefully (SIGKILL) after 5 seconds.
    /// Cleans up the socket file and PID file.
    let stopEngine (socketPath: string) (proc: Process) : unit =
        let pid = proc.Id

        if not proc.HasExited then
            // Send SIGTERM via kill command (Linux)
            try
                let killPsi =
                    ProcessStartInfo(
                        FileName = "kill",
                        Arguments = $"-TERM {pid}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    )

                use killProc = Process.Start(killPsi)
                killProc.WaitForExit()
            with
            | _ -> ()

            // Wait up to 5 seconds for graceful exit
            if not (proc.WaitForExit(5000)) then
                try
                    proc.Kill()
                with
                | _ -> ()

        // Clean up socket file
        if File.Exists(socketPath) then
            try
                File.Delete(socketPath)
            with
            | _ -> ()

        // Clean up PID file
        let pidPath = $"{socketPath}.pid"

        if File.Exists(pidPath) then
            try
                File.Delete(pidPath)
            with
            | _ -> ()

        printfn "Engine stopped (PID %d)" pid
