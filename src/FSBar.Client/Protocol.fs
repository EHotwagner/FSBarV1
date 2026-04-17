namespace FSBar.Client

open System
open System.Collections.Generic
open System.IO
open System.Net.Sockets
open System.Runtime.CompilerServices
open FsGrpc.Protobuf
open Highbar

type HandshakeInfo = {
    ProtocolVersion: uint32
    EngineVersion: string
    GameId: string
    MapName: string
    ModName: string
    TeamId: int
    AllyTeamId: int
    PlayerCount: int
}

/// Raised by Protocol.sendCallback when CallbackResponse.RequestId does not
/// match the in-flight request id. A distinct exception from
/// EngineDisconnectedException so callers can tell "proxy sent the wrong
/// response" apart from "socket went away".
///
/// See HighBarV2 specs/031-fix-callback-event-drop/contracts/
/// callback-frame-interleaving.md for the normative contract.
type ProtocolMismatchException(message: string, ?innerException: exn) =
    inherit exn(message, defaultArg innerException null)

module Protocol =

    let protocolVersion = 1u
    let mutable nextRequestId = 1u

    /// When <c>false</c>, <c>sendCallback</c> silently drops events from
    /// interleaved <c>Frame</c> messages (the pre-031 behaviour). When
    /// <c>true</c>, interleaved frames are stashed in the per-stream replay
    /// buffer and drained by the next <c>receiveFrame</c>.
    ///
    /// Default <c>false</c> so batch-callback sequences during warmup
    /// (notably <c>UnitDefCache.loadFromEngine</c>'s ~2500-callback pull)
    /// don't accumulate tens of thousands of stale frames in the buffer
    /// — that would force the bot to re-process every warmup frame at the
    /// start of the main loop, while the engine continues advancing and
    /// the proxy's socket write buffer fills up, OOM'ing the Lua VM.
    ///
    /// <c>BarClient.Start()</c> flips this to <c>true</c> after warmup and
    /// unit-def loading so mid-game callbacks (e.g. <c>getUnitPos</c> inside
    /// a bot tactics tick) preserve the <c>UnitFinished</c> events that the
    /// bot's opening helper needs.
    let mutable replayBufferEnabled = false

    // ---------------------------------------------------------------------
    // Callback/Frame interleaving replay buffer
    //
    // Per HighBarV2 normative contract (031-fix-callback-event-drop), the
    // proxy MAY emit one or more `Frame` messages between a `CallbackRequest`
    // and the matching `CallbackResponse` on the same socket. A conforming
    // client must (a) acknowledge every interleaved `Frame` with an empty
    // `FrameResponse` so the proxy doesn't stall, and (b) preserve the
    // frame's number + events in a FIFO replay buffer that the next
    // frame-consuming call drains BEFORE touching the socket.
    //
    // The replay buffer is keyed by `NetworkStream` identity via a
    // `ConditionalWeakTable` so the public `Protocol.{receiveFrame,
    // sendCallback}` signatures stay unchanged — the buffer is per-stream
    // (one BarClient ↔ one stream ↔ one buffer) without forcing a Tier 1
    // surface change. Disposing the stream releases the buffer via the
    // weak table's normal lifetime semantics.
    // ---------------------------------------------------------------------

    /// A frame decoded while sendCallback was waiting for a CallbackResponse.
    /// Stashed verbatim and replayed by the next receiveFrame on the same stream.
    type PendingFrame = {
        FrameNumber: uint32
        Events: GameEvent list
    }

    /// Per-stream replay buffer. Lives as long as the stream object is reachable.
    let replayBuffers = ConditionalWeakTable<NetworkStream, Queue<PendingFrame>>()

    let bufferFor (stream: NetworkStream) : Queue<PendingFrame> =
        let mutable existing = Unchecked.defaultof<Queue<PendingFrame>>
        if replayBuffers.TryGetValue(stream, &existing) then existing
        else
            let q = Queue<PendingFrame>()
            replayBuffers.Add(stream, q)
            q

    let tryDequeueReplay (stream: NetworkStream) : GameFrame option =
        let buf = bufferFor stream
        if buf.Count = 0 then None
        else
            let pending = buf.Dequeue()
            Some {
                FrameNumber = pending.FrameNumber
                Events = pending.Events
            }

    /// Perform handshake: receive Handshake from proxy, validate, send HandshakeResponse.
    let handshake (stream: NetworkStream) : HandshakeInfo =
        let bytes = Connection.recvBytes stream
        let proxyMsg = decode<ProxyMessage> bytes

        match proxyMsg.Message with
        | ProxyMessage.MessageCase.Handshake hs ->
            let accepted = hs.ProtocolVersion = protocolVersion

            let resp : AIMessage = {
                Message = AIMessage.MessageCase.HandshakeResponse {
                    Accepted = accepted
                    ProtocolVersion = protocolVersion
                }
            }
            Connection.sendMessage stream (encode resp)

            if not accepted then
                failwith $"Protocol version mismatch: expected {protocolVersion}, got {hs.ProtocolVersion}"

            {
                ProtocolVersion = hs.ProtocolVersion
                EngineVersion = hs.EngineVersion
                GameId = hs.GameId
                MapName = hs.MapName
                ModName = hs.ModName
                TeamId = hs.TeamId
                AllyTeamId = hs.AllyTeamId
                PlayerCount = hs.PlayerCount
            }
        | _ -> failwith "Expected Handshake message from proxy"

    /// Receive one frame from the proxy.
    ///
    /// Drains the per-stream replay buffer FIFO-first. Replay buffer entries
    /// are frames the proxy sent during a previous <c>sendCallback</c> wait —
    /// per the HighBarV2 callback/frame interleaving contract, the client
    /// stashes those frames verbatim and replays them here before touching
    /// the socket. Once the buffer is empty, reads the next message from
    /// the wire normally.
    ///
    /// Returns <c>Some frame</c> for a normal game frame, or <c>Some</c> a
    /// synthetic terminal frame carrying a single <c>GameEvent.Shutdown</c>
    /// event when the proxy delivers the standalone Shutdown envelope on
    /// game-over (the proxy sends it as a top-level <c>ProxyMessage</c> after
    /// its final <c>send_frame</c>, then closes the socket). The synthetic
    /// frame has <c>FrameNumber = 0u</c> as a sentinel; callers that need the
    /// last real game-frame number must rewrite it before dispatching.
    ///
    /// Returns <c>None</c> only for legacy code paths that still expected the
    /// old "Shutdown = None" behaviour — which is now unreachable from the
    /// proxy. A clean socket close without a Shutdown envelope still raises
    /// <see cref="T:FSBar.Client.EngineDisconnectedException"/> from the
    /// underlying read.
    ///
    /// See ../HighBarV2/specs/030-proxy-contract-docs/contracts/shutdown-wire-shape.md
    /// for the authoritative wire-shape contract and
    /// ../HighBarV2/specs/031-fix-callback-event-drop/contracts/callback-frame-interleaving.md
    /// for the replay-buffer contract.
    let rec receiveFrame (stream: NetworkStream) : GameFrame option =
        match tryDequeueReplay stream with
        | Some replayed -> Some replayed
        | None ->

        let bytes = Connection.recvBytes stream
        let proxyMsg = decode<ProxyMessage> bytes

        match proxyMsg.Message with
        | ProxyMessage.MessageCase.Frame frame ->
            let events =
                frame.Events
                |> List.map Events.fromProto
            Some {
                FrameNumber = frame.FrameNumber
                Events = events
            }
        | ProxyMessage.MessageCase.Shutdown sd ->
            // Inline the ShutdownReason→string mapping rather than calling
            // Events.shutdownReasonToString (which is not in Events.fsi, so
            // exposing it here would force a Tier 1 surface change).
            let reason =
                match sd.Reason with
                | Highbar.ShutdownReason.GameOver -> "GameOver"
                | Highbar.ShutdownReason.Disconnect -> "Disconnect"
                | Highbar.ShutdownReason.Error -> "Error"
                | _ -> "Unknown"
            Some {
                FrameNumber = 0u
                Events = [ GameEvent.Shutdown reason ]
            }
        | ProxyMessage.MessageCase.SaveRequest _ ->
            // Respond with empty save state
            let resp : AIMessage = {
                Message = AIMessage.MessageCase.SaveResponse {
                    StateData = FsGrpc.Bytes.Empty
                }
            }
            Connection.sendMessage stream (encode resp)
            // Continue to next message
            receiveFrame stream
        | _ ->
            // Skip unknown messages, try next
            receiveFrame stream

    /// Send a frame response with commands.
    let sendFrameResponse (stream: NetworkStream) (commands: AICommand list) : unit =
        let frameResp : FrameResponse = {
            Commands = commands
            TeamId = 0
        }
        let resp : AIMessage = {
            Message = AIMessage.MessageCase.FrameResponse frameResp
        }
        Connection.sendMessage stream (encode resp)

    /// Send a callback request and wait for the response.
    ///
    /// Per HighBarV2 normative contract `031-fix-callback-event-drop`:
    /// every interleaved <c>Frame</c> message arriving during the wait is
    /// decoded into a <see cref="T:FSBar.Client.GameFrame"/>, stashed in the
    /// per-stream replay buffer, and acknowledged with an empty
    /// <c>FrameResponse</c> so the proxy does not stall. The next call to
    /// <see cref="M:FSBar.Client.Protocol.receiveFrame"/> on the same stream
    /// drains the buffer FIFO-first, so no events are lost.
    ///
    /// Verifies that <c>CallbackResponse.RequestId</c> matches the in-flight
    /// request id. Mismatch raises <c>ProtocolMismatchException</c> rather
    /// than silently masking a proxy bug.
    ///
    /// There is intentionally NO attempt cap on the inner read loop: the
    /// proxy at 100× headless game speed routinely interleaves hundreds of
    /// frames per callback round-trip, and a fixed cap would either fire on
    /// the happy path or leave a half-completed callback (the
    /// <c>CallbackRequest</c> on the wire, no <c>CallbackResponse</c> read)
    /// that desynchronises every subsequent <c>sendCallback</c> with an
    /// off-by-one <c>RequestId</c> mismatch. Termination is enforced by
    /// <see cref="P:System.Net.Sockets.NetworkStream.ReadTimeout"/> on the
    /// underlying stream — a genuinely stuck proxy raises
    /// <see cref="T:FSBar.Client.EngineDisconnectedException"/> from
    /// <see cref="M:FSBar.Client.Connection.recvBytes"/>, not from a counter.
    let sendCallback (stream: NetworkStream) (callbackId: uint32) (paramList: CallbackParam list) : CallbackResponse =
        let reqId = nextRequestId
        nextRequestId <- nextRequestId + 1u

        let req : CallbackRequest = {
            RequestId = reqId
            CallbackId = callbackId
            Params = paramList
        }
        let msg : AIMessage = {
            Message = AIMessage.MessageCase.CallbackRequest req
        }
        Connection.sendMessage stream (encode msg)

        let buf = bufferFor stream

        let rec readUntilCallback () =
            let respBytes = Connection.recvBytes stream
            let proxyMsg = decode<ProxyMessage> respBytes
            match proxyMsg.Message with
            | ProxyMessage.MessageCase.CallbackResponse resp ->
                if resp.RequestId <> reqId then
                    raise (ProtocolMismatchException(
                        sprintf "CallbackResponse request_id mismatch: expected %d, got %d"
                            reqId resp.RequestId))
                resp
            | ProxyMessage.MessageCase.Frame frame ->
                // Engine pushed a frame while we're waiting for the callback
                // response. Ack with empty commands so the proxy doesn't stall,
                // then either stash the frame for later replay (mid-game) or
                // drop its events on the floor (warmup batch load).
                if replayBufferEnabled then
                    let events =
                        frame.Events
                        |> List.map Events.fromProto
                    buf.Enqueue({ FrameNumber = frame.FrameNumber; Events = events })
                sendFrameResponse stream []
                readUntilCallback ()
            | ProxyMessage.MessageCase.SaveRequest _ ->
                let saveResp : AIMessage = {
                    Message = AIMessage.MessageCase.SaveResponse {
                        StateData = FsGrpc.Bytes.Empty
                    }
                }
                Connection.sendMessage stream (encode saveResp)
                readUntilCallback ()
            | other -> failwith $"Expected CallbackResponse, got {other}"
        readUntilCallback ()
