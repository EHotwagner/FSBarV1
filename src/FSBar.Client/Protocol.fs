namespace FSBar.Client

open System.IO
open System.Net.Sockets
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

module Protocol =

    let protocolVersion = 1u
    let mutable nextRequestId = 1u

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
    let rec receiveFrame (stream: NetworkStream) : GameFrame option =
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
    /// Skips any interleaved Frame messages (auto-responds with empty commands)
    /// since the engine may push frames asynchronously.
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

        let rec readUntilCallback (attempts: int) =
            if attempts > 100 then
                failwith "sendCallback: exceeded 100 attempts waiting for CallbackResponse"
            let respBytes = Connection.recvBytes stream
            let proxyMsg = decode<ProxyMessage> respBytes
            match proxyMsg.Message with
            | ProxyMessage.MessageCase.CallbackResponse resp -> resp
            | ProxyMessage.MessageCase.Frame _ ->
                // Engine sent a frame while we're waiting for callback response.
                // Respond with empty commands and keep reading. The frame's
                // events are intentionally dropped here — the caller is
                // expected to drive important event capture through the
                // regular frame-reading path (BarClient.WaitFrames) rather
                // than relying on bulk-callback batches like UnitDefCache
                // loading to preserve them.
                sendFrameResponse stream []
                readUntilCallback (attempts + 1)
            | ProxyMessage.MessageCase.SaveRequest _ ->
                let saveResp : AIMessage = {
                    Message = AIMessage.MessageCase.SaveResponse {
                        StateData = FsGrpc.Bytes.Empty
                    }
                }
                Connection.sendMessage stream (encode saveResp)
                readUntilCallback (attempts + 1)
            | other -> failwith $"Expected CallbackResponse, got {other}"
        readUntilCallback 0
