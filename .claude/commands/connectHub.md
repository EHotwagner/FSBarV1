Open a persistent server-streaming connection to the FSBar Hub's
`ScriptingService` via the FSI MCP server. The stream stays attached to
the hub's client roster (visible in the Hub's gRPC tab) until explicitly
cancelled.

Prerequisite: the hub must already be running — if not, run `/startHub`
first and wait for it to listen on `127.0.0.1:5021`.

## Steps

1. Verify the hub is listening on `127.0.0.1:5021`:

   ```bash
   ss -tlnp 2>/dev/null | grep 5021
   ```

   If nothing is bound, stop and tell the user to run `/startHub` first.

2. Load the FSBar.Hub FSI prelude in the MCP FSI session and construct
   a `ScriptingService.Client` against the hub (send via
   `mcp__fsi-server__send_fsharp_code`):

   ```fsharp
   #load "/home/developer/projects/FSBarV1/src/FSBar.Hub/scripts/prelude.fsx"
   ;;
   open System
   open System.Threading
   open Grpc.Core
   open Grpc.Net.Client
   open Fsbar.Hub.Scripting.V1
   ;;
   let hubChannel = GrpcChannel.ForAddress("http://127.0.0.1:5021")
   let hubClient  = new ScriptingService.Client(hubChannel)
   ;;
   ```

   If `hubChannel` / `hubClient` already exist in the session (from a
   previous `/connectHub` invocation), skip this step to avoid
   redefining them.

3. Open a persistent streaming call with a drain task so the per-client
   channel (16-slot bounded buffer, drop-oldest) does not overflow:

   ```fsharp
   let hubStreamCts = new CancellationTokenSource()
   let hubStreamReq : StreamGameFramesRequest =
       { ClientLabel = "fsi-mcp"; CloseOnSessionEnd = false }
   let hubStreamCall =
       hubClient.StreamGameFramesAsync
           (CallOptions(cancellationToken = hubStreamCts.Token)) hubStreamReq
   let hubStreamLatest : GameFrameMessage option ref = ref None
   let hubStreamTask =
       System.Threading.Tasks.Task.Run(fun () ->
           try
               let stream = hubStreamCall.ResponseStream
               while stream.MoveNext(hubStreamCts.Token).GetAwaiter().GetResult() do
                   hubStreamLatest := Some stream.Current
           with _ -> ())
   ;;
   ```

   The latest received `GameFrameMessage` is kept in `hubStreamLatest`
   so follow-up work (e.g., inspecting events, issuing `SendCommand`)
   can pull frame context without re-subscribing.

4. Confirm the stream registered by calling `GetSessionStatus` and
   reporting the `Clients` roster and its `ClientLabel` + `ClientId`
   back to the user. Also report how to stop the stream:
   `hubStreamCts.Cancel()` in the FSI session.

## Notes

- The hub roster only tracks **streaming** RPCs. Unary calls
  (`GetSessionStatus`, `GetUnitDef`, `SendCommand`) never show up on the
  gRPC tab.
- Default per-client buffer is 16 frames (`ScriptingHub.defaults`). If
  cumulative drops exceed 32 the hub detaches the client — the drain
  task above prevents that.
- `CloseOnSessionEnd = false` keeps the stream attached across session
  restarts (FR-030). Use `true` if the caller wants the stream to close
  when the current session ends.
- Do not pass `--no-build` or any hub-side flags here — this command
  only connects, it does not (re)start the hub.
