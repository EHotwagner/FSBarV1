module FSBar.Hub.GrpcTests.HubTestFixture

open System
open System.Diagnostics
open System.IO
open System.Net
open System.Net.Sockets
open System.Threading
open System.Threading.Tasks
open Grpc.Net.Client
open Grpc.Core
open Fsbar.Hub.Scripting.V1

let private pickFreePort () : int =
    let listener = new TcpListener(IPAddress.Loopback, 0)
    listener.Start()
    let port = (listener.LocalEndpoint :?> IPEndPoint).Port
    listener.Stop()
    port

let private repoRoot =
    let mutable dir = AppContext.BaseDirectory
    while not (File.Exists(Path.Combine(dir, "FSBarV1.slnx"))) && Directory.GetParent(dir) <> null do
        dir <- Directory.GetParent(dir).FullName
    dir

type HubTestFixture() =
    let mutable proc : Process option = None
    let mutable channel : GrpcChannel option = None
    let mutable grpcPort : int = 0
    let mutable tempConfigDir : string = ""
    let mutable client : ScriptingService.Client option = None

    member _.Port = grpcPort
    member _.Stub = client |> Option.defaultWith (fun () -> failwith "HubTestFixture not initialized")

    interface Xunit.IAsyncLifetime with

        member _.InitializeAsync() : Task =
            task {
                grpcPort <- pickFreePort ()
                tempConfigDir <- Path.Combine(Path.GetTempPath(), sprintf "fsbar-grpc-test-%d" grpcPort)
                Directory.CreateDirectory(tempConfigDir) |> ignore

                let psi = ProcessStartInfo()
                psi.FileName <- "dotnet"
                psi.Arguments <- "run --project src/FSBar.Hub.App --no-build"
                psi.WorkingDirectory <- repoRoot
                psi.UseShellExecute <- false
                psi.RedirectStandardOutput <- true
                psi.RedirectStandardError <- true
                psi.EnvironmentVariables["FSBAR_HUB_GRPC_PORT"] <- string grpcPort
                psi.EnvironmentVariables["DISPLAY"] <- ":0"
                psi.EnvironmentVariables["XDG_RUNTIME_DIR"] <- "/tmp/runtime-developer"
                psi.EnvironmentVariables["XDG_CONFIG_HOME"] <- tempConfigDir

                let p = new Process()
                p.StartInfo <- psi
                p.Start() |> ignore
                p.BeginOutputReadLine()
                p.BeginErrorReadLine()
                proc <- Some p

                let ch = GrpcChannel.ForAddress(sprintf "http://127.0.0.1:%d" grpcPort)
                channel <- Some ch
                let stub = ScriptingService.Client(ch)
                client <- Some stub

                let sw = Stopwatch.StartNew()
                let mutable ready = false
                while not ready && sw.ElapsedMilliseconds < 15000L do
                    try
                        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(2.0)))
                        let! _ = stub.GetHubStateAsync(opts) GetHubStateRequest.empty
                        ready <- true
                    with
                    | :? RpcException -> do! Task.Delay(500)

                if not ready then
                    failwith (sprintf "Hub did not become ready on port %d within 15s" grpcPort)
            } :> Task

        member _.DisposeAsync() : Task =
            task {
                match channel with
                | Some ch -> try ch.Dispose() with _ -> ()
                | None -> ()

                match proc with
                | Some p ->
                    try
                        if not p.HasExited then
                            p.Kill(entireProcessTree = true)
                            do! p.WaitForExitAsync(CancellationToken.None)
                        p.Dispose()
                    with _ -> ()
                | None -> ()

                if Directory.Exists(tempConfigDir) then
                    try Directory.Delete(tempConfigDir, true) with _ -> ()
            } :> Task
