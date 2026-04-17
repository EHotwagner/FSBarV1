module FSBar.Client.Tests.ProtocolReplayTests

// Deterministic tests for the callback/frame interleaving replay buffer added
// in feature 024 follow-up to FSBarV1 Protocol.fs. Mirrors the HighBarV2
// reference client tests under
// HighBarV2/tests/integration/fsharp/CallbackFrameInterleavingTests.fs.
//
// Contract:
//   ../HighBarV2/specs/031-fix-callback-event-drop/contracts/callback-frame-interleaving.md

open System
open System.Net
open System.Net.Sockets
open System.Threading
open Xunit
open FsGrpc.Protobuf
open FSBar.Client
open Highbar

// ---------------------------------------------------------------------------
// Test plumbing
// ---------------------------------------------------------------------------

/// Set up a TCP loopback pair: returns (clientStream, serverSocket). The
/// `clientStream` is what `Protocol.{receiveFrame,sendCallback}` consumes;
/// the test acts as the proxy by writing length-prefixed `ProxyMessage`
/// envelopes to the matching server-side socket.
let tcpPair () : NetworkStream * Socket =
    let listener = new TcpListener(IPAddress.Loopback, 0)
    listener.Start()
    let port = (listener.LocalEndpoint :?> IPEndPoint).Port
    let client = new TcpClient()
    client.Connect(IPAddress.Loopback, port)
    let server = listener.AcceptSocket()
    listener.Stop()
    let clientStream = client.GetStream()
    clientStream, server

/// Send a length-prefixed ProxyMessage envelope from the test ("proxy" side).
let sendProxyMessage (server: Socket) (msg: ProxyMessage) =
    let bytes = encode msg
    let header = BitConverter.GetBytes(uint32 bytes.Length)
    if not BitConverter.IsLittleEndian then Array.Reverse(header)
    server.Send(header) |> ignore
    server.Send(bytes) |> ignore

/// Read whatever the F# client sent back so the test can drain its replies
/// (FrameResponse ack / SaveResponse). Returns the decoded AIMessage.
let recvAIMessage (server: Socket) : AIMessage =
    let header = Array.zeroCreate 4
    let mutable off = 0
    while off < 4 do
        off <- off + server.Receive(header, off, 4 - off, SocketFlags.None)
    if not BitConverter.IsLittleEndian then Array.Reverse(header)
    let len = BitConverter.ToInt32(header, 0)
    let body = Array.zeroCreate len
    let mutable boff = 0
    while boff < len do
        boff <- boff + server.Receive(body, boff, len - boff, SocketFlags.None)
    decode<AIMessage> body

/// Proxy-side helper: build a Frame ProxyMessage with the given frame
/// number and an empty event list.
let frameMsg (n: uint32) : ProxyMessage =
    let f : Frame = { FrameNumber = n; Events = []; TeamId = 0 }
    { Message = ProxyMessage.MessageCase.Frame f }

/// Proxy-side helper: a CallbackResponse with the given request id and a
/// dummy successful int result.
let callbackResponseMsg (reqId: uint32) : ProxyMessage =
    let result : CallbackResult = { Value = CallbackResult.ValueCase.IntValue 42 }
    let resp : CallbackResponse =
        { RequestId = reqId
          Success = true
          Result = Some result
          ErrorMessage = "" }
    { Message = ProxyMessage.MessageCase.CallbackResponse resp }

/// Read the F# client's outgoing CallbackRequest and extract its request id.
/// Used by the proxy worker so it can echo the id back in the matching response.
let readCallbackRequestId (server: Socket) : uint32 =
    let msg = recvAIMessage server
    match msg.Message with
    | AIMessage.MessageCase.CallbackRequest req -> req.RequestId
    | other -> failwithf "expected CallbackRequest, got %A" other

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``sendCallback drains interleaved Frames into replay buffer and returns matching response`` () =
    let previousState = Protocol.replayBufferEnabled
    Protocol.replayBufferEnabled <- true
    let clientStream, server = tcpPair ()
    try
        // Proxy worker:
        //   1. Reads the CallbackRequest the F# client sends and extracts its request id.
        //   2. Writes Frame(100), reads the empty FrameResponse ack.
        //   3. Writes Frame(110), reads the empty FrameResponse ack.
        //   4. Writes CallbackResponse echoing the captured request id.
        let proxyThread =
            Thread(fun () ->
                let reqId = readCallbackRequestId server
                sendProxyMessage server (frameMsg 100u)
                let _ack1 = recvAIMessage server
                sendProxyMessage server (frameMsg 110u)
                let _ack2 = recvAIMessage server
                sendProxyMessage server (callbackResponseMsg reqId))
        proxyThread.Start()

        let resp = Protocol.sendCallback clientStream 0u []
        proxyThread.Join()

        Assert.True(resp.Success)
        Assert.True(resp.RequestId > 0u)

        // After sendCallback returns, the next two receiveFrame calls
        // must drain the replay buffer in FIFO order.
        match Protocol.receiveFrame clientStream with
        | Some f -> Assert.Equal(100u, f.FrameNumber)
        | None -> Assert.Fail("expected frame 100 from replay buffer")
        match Protocol.receiveFrame clientStream with
        | Some f -> Assert.Equal(110u, f.FrameNumber)
        | None -> Assert.Fail("expected frame 110 from replay buffer")
    finally
        Protocol.replayBufferEnabled <- previousState
        clientStream.Dispose()
        server.Close()

[<Fact>]
let ``sendCallback raises ProtocolMismatchException on request_id mismatch`` () =
    let clientStream, server = tcpPair ()
    try
        // Proxy reads the CallbackRequest, then deliberately sends the
        // WRONG request id back so the F# client raises.
        let proxyThread =
            Thread(fun () ->
                let _reqId = readCallbackRequestId server
                sendProxyMessage server (callbackResponseMsg 0xDEADBEEFu))
        proxyThread.Start()

        let ex =
            Assert.Throws<ProtocolMismatchException>(fun () ->
                Protocol.sendCallback clientStream 0u [] |> ignore)
        Assert.Contains("request_id mismatch", ex.Message)

        proxyThread.Join()
    finally
        clientStream.Dispose()
        server.Close()

[<Fact>]
let ``sendCallback acknowledges interleaved SaveRequest without losing buffered frames`` () =
    let previousState = Protocol.replayBufferEnabled
    Protocol.replayBufferEnabled <- true
    let clientStream, server = tcpPair ()
    try
        let saveReqMsg : ProxyMessage =
            { Message = ProxyMessage.MessageCase.SaveRequest SaveRequest.Unused }
        let proxyThread =
            Thread(fun () ->
                let reqId = readCallbackRequestId server
                sendProxyMessage server (frameMsg 100u)
                let _ack = recvAIMessage server
                sendProxyMessage server saveReqMsg
                let _saveAck = recvAIMessage server
                sendProxyMessage server (callbackResponseMsg reqId))
        proxyThread.Start()

        let _resp = Protocol.sendCallback clientStream 0u []
        proxyThread.Join()

        // Frame 100 is still in the replay buffer.
        match Protocol.receiveFrame clientStream with
        | Some f -> Assert.Equal(100u, f.FrameNumber)
        | None -> Assert.Fail("expected frame 100 from replay buffer")
    finally
        Protocol.replayBufferEnabled <- previousState
        clientStream.Dispose()
        server.Close()

[<Fact>]
let ``receiveFrame on a fresh stream with empty buffer reads from the wire`` () =
    let clientStream, server = tcpPair ()
    try
        let proxyThread =
            Thread(fun () -> sendProxyMessage server (frameMsg 555u))
        proxyThread.Start()

        match Protocol.receiveFrame clientStream with
        | Some f -> Assert.Equal(555u, f.FrameNumber)
        | None -> Assert.Fail("expected frame 555 from socket read")

        proxyThread.Join()
    finally
        clientStream.Dispose()
        server.Close()

[<Fact>]
let ``sendCallback in drop mode does not accumulate frames (warmup batch-load behaviour)`` () =
    // Regression control: with replayBufferEnabled=false (the default used
    // during BarClient.Start warmup + UnitDefCache.loadFromEngine), interleaved
    // frames are acknowledged and their events discarded. The replay buffer
    // stays empty so the bot's main loop does not spend its first thousands
    // of frames draining stale warmup-window frames while the engine OOMs.
    let previousState = Protocol.replayBufferEnabled
    Protocol.replayBufferEnabled <- false
    let clientStream, server = tcpPair ()
    try
        let proxyThread =
            Thread(fun () ->
                let reqId = readCallbackRequestId server
                sendProxyMessage server (frameMsg 200u)
                let _ack = recvAIMessage server
                sendProxyMessage server (frameMsg 210u)
                let _ack = recvAIMessage server
                sendProxyMessage server (callbackResponseMsg reqId)
                // Proxy now sends a live frame; the bot should read this one,
                // not something from an internal buffer.
                sendProxyMessage server (frameMsg 555u))
        proxyThread.Start()

        let resp = Protocol.sendCallback clientStream 0u []
        Assert.True(resp.Success)

        // In drop mode the replay buffer is empty after sendCallback, so the
        // next receiveFrame reads frame 555 straight off the wire, not 200.
        match Protocol.receiveFrame clientStream with
        | Some f -> Assert.Equal(555u, f.FrameNumber)
        | None -> Assert.Fail("expected frame 555 from wire (buffer should be empty in drop mode)")

        proxyThread.Join()
    finally
        Protocol.replayBufferEnabled <- previousState
        clientStream.Dispose()
        server.Close()
