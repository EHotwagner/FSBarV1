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

    let private protocolVersion = 1u
    let mutable private nextRequestId = 1u

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

    /// Receive one frame from the proxy. Returns None on Shutdown.
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
        | ProxyMessage.MessageCase.Shutdown _ ->
            None
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

        let respBytes = Connection.recvBytes stream
        let proxyMsg = decode<ProxyMessage> respBytes

        match proxyMsg.Message with
        | ProxyMessage.MessageCase.CallbackResponse resp -> resp
        | other -> failwith $"Expected CallbackResponse, got {other}"
