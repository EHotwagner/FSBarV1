namespace FSBar.Hub.App

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.Runtime.InteropServices
open System.Threading
open FSBar.Hub

module ProcessLifetime =

    [<RequireQualifiedAccess>]
    type CloseDecision =
        | AllowClose
        | RequireConfirm of message: string

    let requestClose (sessionState: SessionManager.SessionState) : CloseDecision =
        match sessionState with
        | SessionManager.Idle
        | SessionManager.Failed _
        | SessionManager.Ending _ ->
            CloseDecision.AllowClose
        | SessionManager.Running _ ->
            CloseDecision.RequireConfirm
                "A session is running. Closing the hub will terminate the BAR engine. Close anyway?"
        | SessionManager.Starting _ ->
            CloseDecision.RequireConfirm
                "A session is starting. Closing now will abort the engine warmup. Close anyway?"

    // --- PID registry ----------------------------------------------------

    let private trackedPids = ConcurrentDictionary<int, byte>()

    let register (pid: int) : unit =
        if pid > 0 then trackedPids.[pid] <- 0uy

    let unregister (pid: int) : unit =
        trackedPids.TryRemove(pid) |> ignore

    let tracked () : int list =
        trackedPids.Keys |> List.ofSeq |> List.sort

    // --- Signal + child reaper -------------------------------------------

    [<DllImport("libc", SetLastError = true)>]
    extern int kill(int pid, int signal)

    let private SIGTERM = 15
    let private SIGKILL = 9

    let private sendSignal (pid: int) (sig_: int) : bool =
        try kill(pid, sig_) = 0
        with _ -> false

    /// Wait up to `deadlineUtc` for a single PID to exit. Uses
    /// `Process.GetProcessById` — not `waitpid` — since the tracked
    /// processes are not direct children of this F# process (they
    /// were spawned by `EngineLauncher.launchHeadless` which
    /// reparents them), so reaping via waitpid is unavailable.
    let private waitForExit (pid: int) (deadlineUtc: DateTime) : bool =
        let rec loop () =
            if DateTime.UtcNow >= deadlineUtc then false
            else
                let exited =
                    try
                        use p = Process.GetProcessById(pid)
                        p.HasExited
                    with
                    // ArgumentException / InvalidOperationException
                    // when the PID is no longer running.
                    | _ -> true
                if exited then true
                else
                    Thread.Sleep(50)
                    loop ()
        loop ()

    let private sweepChildEnginesCore (gracePeriodMs: int) : unit =
        let grace = gracePeriodMs
        let snapshot = tracked ()
        if snapshot.IsEmpty then () else
        eprintfn "[hub lifetime] sending SIGTERM to %d child PID(s): %A"
            snapshot.Length snapshot
        for pid in snapshot do
            sendSignal pid SIGTERM |> ignore
        // Grace wait.
        let deadline = DateTime.UtcNow.AddMilliseconds(float grace)
        for pid in snapshot do
            if not (waitForExit pid deadline) then
                eprintfn "[hub lifetime] SIGKILL'ing surviving PID %d" pid
                sendSignal pid SIGKILL |> ignore
        // Remove everything we just swept.
        for pid in snapshot do
            trackedPids.TryRemove(pid) |> ignore

    let sweepChildEngines (gracePeriodMs: int option) : unit =
        sweepChildEnginesCore (defaultArg gracePeriodMs 3000)

    // --- Signal-handler installation -------------------------------------

    let private handlersInstalled = ref 0

    let installSignalHandlers
            (onShutdown: unit -> unit)
            (gracePeriodMs: int option)
            : unit =
        if Interlocked.Exchange(handlersInstalled, 1) <> 0 then () else
        let grace = defaultArg gracePeriodMs 3000
        let runCleanup (reason: string) =
            eprintfn "[hub lifetime] %s — starting cleanup" reason
            try onShutdown ()
            with ex -> eprintfn "[hub lifetime] onShutdown raised: %s" ex.Message
            sweepChildEnginesCore grace
        // CancelKeyPress = Ctrl-C from the terminal.
        Console.CancelKeyPress.Add(fun args ->
            args.Cancel <- false
            runCleanup "SIGINT")
        // ProcessExit fires on normal shutdown + SIGTERM.
        AppDomain.CurrentDomain.ProcessExit.Add(fun _ ->
            runCleanup "ProcessExit")
