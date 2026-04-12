module FSBar.Client.Tests.ProtocolTests

open System
open System.IO
open System.Net.Sockets
open Xunit
open FSBar.Client
open FsGrpc.Protobuf
open Highbar

/// Helper to create a pair of connected NetworkStreams via Unix domain socket
let private createStreamPair () =
    let guid = Guid.NewGuid().ToString("N").[..7]
    let path = $"/tmp/fsbar-test-{guid}.sock"
    if File.Exists(path) then File.Delete(path)
    let listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified)
    listener.Bind(UnixDomainSocketEndPoint(path))
    listener.Listen(1)
    let client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified)
    client.Connect(UnixDomainSocketEndPoint(path))
    let server = listener.Accept()
    let clientStream = new NetworkStream(client, ownsSocket = true)
    let serverStream = new NetworkStream(server, ownsSocket = true)
    listener.Close()
    if File.Exists(path) then File.Delete(path)
    (clientStream, serverStream)

[<Fact>]
let ``handshake_parses_correctly`` () =
    let (proxyStream, aiStream) = createStreamPair ()
    try
        // Send handshake from proxy side
        let hs : ProxyMessage = {
            Message = ProxyMessage.MessageCase.Handshake {
                ProtocolVersion = 1u
                EngineVersion = "test-engine"
                GameId = "game-123"
                MapName = "TestMap"
                ModName = "TestMod"
                TeamId = 0
                AllyTeamId = 0
                PlayerCount = 2
            }
        }
        Connection.sendMessage proxyStream (encode hs)

        // AI side performs handshake
        let info = Protocol.handshake aiStream

        Assert.Equal(1u, info.ProtocolVersion)
        Assert.Equal("test-engine", info.EngineVersion)
        Assert.Equal("game-123", info.GameId)
        Assert.Equal("TestMap", info.MapName)
        Assert.Equal("TestMod", info.ModName)
        Assert.Equal(0, info.TeamId)
        Assert.Equal(0, info.AllyTeamId)
        Assert.Equal(2, info.PlayerCount)

        // Verify proxy received the HandshakeResponse
        let respBytes = Connection.recvBytes proxyStream
        let resp = decode<AIMessage> respBytes
        match resp.Message with
        | AIMessage.MessageCase.HandshakeResponse hr ->
            Assert.True(hr.Accepted)
            Assert.Equal(1u, hr.ProtocolVersion)
        | _ -> Assert.Fail("Expected HandshakeResponse")
    finally
        proxyStream.Dispose()
        aiStream.Dispose()

[<Fact>]
let ``receiveFrame_deserializes_frame_correctly`` () =
    let (proxyStream, aiStream) = createStreamPair ()
    try
        // Send a frame from proxy side
        let frame : ProxyMessage = {
            Message = ProxyMessage.MessageCase.Frame {
                FrameNumber = 42u
                Events = [
                    { Event = EngineEvent.EventCase.Update { Frame = 42 } }
                ]
                TeamId = 0
            }
        }
        Connection.sendMessage proxyStream (encode frame)

        let result = Protocol.receiveFrame aiStream
        Assert.True(result.IsSome)
        let gameFrame = result.Value
        Assert.Equal(42u, gameFrame.FrameNumber)
        Assert.Equal(1, gameFrame.Events.Length)
        Assert.Equal(GameEvent.Update 42, gameFrame.Events.[0])

        // Read back the frame response sent by receiveFrame... wait, receiveFrame doesn't send a response.
        // The caller sends a response. So we're done.
    finally
        proxyStream.Dispose()
        aiStream.Dispose()

[<Fact>]
let ``receiveFrame_surfaces_shutdown_as_synthetic_frame`` () =
    // 021: Protocol.receiveFrame now synthesizes a terminal GameFrame
    // carrying GameEvent.Shutdown so the standalone top-level Shutdown
    // envelope is reachable from callers that pattern-match on
    // frame.Events. The sentinel FrameNumber=0u is rewritten by BarClient
    // before dispatch to subscribers.
    let (proxyStream, aiStream) = createStreamPair ()
    try
        let shutdownMsg : ProxyMessage = {
            Message = ProxyMessage.MessageCase.Shutdown {
                Reason = ShutdownReason.GameOver
            }
        }
        Connection.sendMessage proxyStream (encode shutdownMsg)

        let result = Protocol.receiveFrame aiStream
        match result with
        | Some frame ->
            Assert.Equal(0u, frame.FrameNumber)
            match frame.Events with
            | [ GameEvent.Shutdown reason ] ->
                Assert.Equal("GameOver", reason)
            | other ->
                Assert.Fail(sprintf "Expected single Shutdown event, got %A" other)
        | None ->
            Assert.Fail("Expected Some synthetic shutdown frame, got None")
    finally
        proxyStream.Dispose()
        aiStream.Dispose()

[<Fact>]
let ``sendFrameResponse_serializes_commands`` () =
    let (proxyStream, aiStream) = createStreamPair ()
    try
        let commands = [
            Commands.MoveCommand 1 10.0f 20.0f 30.0f
            Commands.StopCommand 2
        ]
        Protocol.sendFrameResponse aiStream commands

        // Proxy side reads the response
        let respBytes = Connection.recvBytes proxyStream
        let resp = decode<AIMessage> respBytes
        match resp.Message with
        | AIMessage.MessageCase.FrameResponse fr ->
            Assert.Equal(2, fr.Commands.Length)
        | _ -> Assert.Fail("Expected FrameResponse")
    finally
        proxyStream.Dispose()
        aiStream.Dispose()

[<Fact>]
let ``sendFrameResponse_empty_commands`` () =
    let (proxyStream, aiStream) = createStreamPair ()
    try
        Protocol.sendFrameResponse aiStream []

        let respBytes = Connection.recvBytes proxyStream
        let resp = decode<AIMessage> respBytes
        match resp.Message with
        | AIMessage.MessageCase.FrameResponse fr ->
            Assert.Equal(0, fr.Commands.Length)
        | _ -> Assert.Fail("Expected FrameResponse")
    finally
        proxyStream.Dispose()
        aiStream.Dispose()

[<Fact>]
let ``receiveFrame_with_multiple_events`` () =
    let (proxyStream, aiStream) = createStreamPair ()
    try
        let frame : ProxyMessage = {
            Message = ProxyMessage.MessageCase.Frame {
                FrameNumber = 100u
                Events = [
                    { Event = EngineEvent.EventCase.Init { TeamId = 0 } }
                    { Event = EngineEvent.EventCase.UnitCreated { UnitId = 1; BuilderId = 0 } }
                    { Event = EngineEvent.EventCase.UnitFinished { UnitId = 1 } }
                ]
                TeamId = 0
            }
        }
        Connection.sendMessage proxyStream (encode frame)

        let result = Protocol.receiveFrame aiStream
        Assert.True(result.IsSome)
        let gameFrame = result.Value
        Assert.Equal(100u, gameFrame.FrameNumber)
        Assert.Equal(3, gameFrame.Events.Length)
        Assert.Equal(GameEvent.Init 0, gameFrame.Events.[0])
        Assert.Equal(GameEvent.UnitCreated(1, 0), gameFrame.Events.[1])
        Assert.Equal(GameEvent.UnitFinished 1, gameFrame.Events.[2])
    finally
        proxyStream.Dispose()
        aiStream.Dispose()

[<Fact>]
let ``receiveFrame_handles_save_request_transparently`` () =
    let (proxyStream, aiStream) = createStreamPair ()
    try
        // Send a SaveRequest followed by a Frame
        let saveReq : ProxyMessage = {
            Message = ProxyMessage.MessageCase.SaveRequest SaveRequest.Unused
        }
        Connection.sendMessage proxyStream (encode saveReq)

        let frame : ProxyMessage = {
            Message = ProxyMessage.MessageCase.Frame {
                FrameNumber = 50u
                Events = []
                TeamId = 0
            }
        }
        Connection.sendMessage proxyStream (encode frame)

        // receiveFrame should skip the SaveRequest and return the Frame
        let result = Protocol.receiveFrame aiStream
        Assert.True(result.IsSome)
        Assert.Equal(50u, result.Value.FrameNumber)

        // Verify the AI sent a SaveResponse for the SaveRequest
        let respBytes = Connection.recvBytes proxyStream
        let resp = decode<AIMessage> respBytes
        match resp.Message with
        | AIMessage.MessageCase.SaveResponse _ -> ()
        | _ -> Assert.Fail("Expected SaveResponse")
    finally
        proxyStream.Dispose()
        aiStream.Dispose()
