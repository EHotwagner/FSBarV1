module FSBar.Hub.GrpcTests.AdminRpcClient

open System
open System.Threading.Tasks
open Grpc.Core
open Fsbar.Hub.Scripting.V1

type AdminRpcClient(stub: ScriptingService.Client, defaultTimeoutMs: int) =

    let opts () =
        CallOptions(deadline = Nullable(DateTime.UtcNow.AddMilliseconds(float defaultTimeoutMs)))

    member _.Pause() : Task<AdminSubmitResult> =
        task {
            let! resp = stub.PauseAsync(opts()) PauseRequest.empty
            return resp.Result |> Option.defaultValue AdminSubmitResult.empty
        }

    member _.Resume() : Task<AdminSubmitResult> =
        task {
            let! resp = stub.ResumeAsync(opts()) ResumeRequest.empty
            return resp.Result |> Option.defaultValue AdminSubmitResult.empty
        }

    member _.SetEngineSpeed(speed: float32) : Task<AdminSubmitResult> =
        task {
            let req = { SetEngineSpeedRequest.empty with Speed = speed }
            let! resp = stub.SetEngineSpeedAsync(opts()) req
            return resp.Result |> Option.defaultValue AdminSubmitResult.empty
        }

    member _.ForceEndMatch() : Task<AdminSubmitResult> =
        task {
            let! resp = stub.ForceEndMatchAsync(opts()) ForceEndMatchRequest.empty
            return resp.Result |> Option.defaultValue AdminSubmitResult.empty
        }

    member _.SendAdminMessage(message: string) : Task<AdminSubmitResult> =
        task {
            let req = { SendAdminMessageRequest.empty with Text = message }
            let! resp = stub.SendAdminMessageAsync(opts()) req
            return resp.Result |> Option.defaultValue AdminSubmitResult.empty
        }
