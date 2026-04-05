module FSBar.Client.Tests.ConnectionTests

open System
open System.IO
open System.Net.Sockets
open Xunit
open FSBar.Client

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
    (clientStream, serverStream, path)

[<Fact>]
let ``sendMessage_recvBytes_roundtrip`` () =
    let (clientStream, serverStream, _) = createStreamPair ()
    try
        let data = [| 1uy; 2uy; 3uy; 4uy; 5uy |]
        Connection.sendMessage clientStream data
        let received = Connection.recvBytes serverStream
        Assert.Equal<byte[]>(data, received)
    finally
        clientStream.Dispose()
        serverStream.Dispose()

[<Fact>]
let ``sendMessage_writes_length_prefix_header`` () =
    let (clientStream, serverStream, _) = createStreamPair ()
    try
        let data = [| 10uy; 20uy; 30uy |]
        Connection.sendMessage clientStream data
        // Read raw bytes: 4-byte LE header + 3-byte payload
        let buffer = Array.zeroCreate 7
        let mutable offset = 0
        while offset < 7 do
            let n = serverStream.Read(buffer, offset, 7 - offset)
            offset <- offset + n
        // Little-endian length = 3
        let length = BitConverter.ToInt32(buffer, 0)
        Assert.Equal(3, length)
        Assert.Equal(10uy, buffer.[4])
        Assert.Equal(20uy, buffer.[5])
        Assert.Equal(30uy, buffer.[6])
    finally
        clientStream.Dispose()
        serverStream.Dispose()

[<Fact>]
let ``sendMessage_recvBytes_large_payload`` () =
    let (clientStream, serverStream, _) = createStreamPair ()
    try
        let data = Array.init 10000 (fun i -> byte (i % 256))
        Connection.sendMessage clientStream data
        let received = Connection.recvBytes serverStream
        Assert.Equal<byte[]>(data, received)
    finally
        clientStream.Dispose()
        serverStream.Dispose()

[<Fact>]
let ``sendMessage_recvBytes_single_byte_payload`` () =
    let (clientStream, serverStream, _) = createStreamPair ()
    try
        let data = [| 42uy |]
        Connection.sendMessage clientStream data
        let received = Connection.recvBytes serverStream
        Assert.Equal<byte[]>(data, received)
    finally
        clientStream.Dispose()
        serverStream.Dispose()

[<Fact>]
let ``sendMessage_recvBytes_multiple_messages`` () =
    let (clientStream, serverStream, _) = createStreamPair ()
    try
        let msg1 = [| 1uy; 2uy |]
        let msg2 = [| 3uy; 4uy; 5uy |]
        Connection.sendMessage clientStream msg1
        Connection.sendMessage clientStream msg2
        let recv1 = Connection.recvBytes serverStream
        let recv2 = Connection.recvBytes serverStream
        Assert.Equal<byte[]>(msg1, recv1)
        Assert.Equal<byte[]>(msg2, recv2)
    finally
        clientStream.Dispose()
        serverStream.Dispose()

[<Fact>]
let ``createListener_creates_socket_file`` () =
    let guid = Guid.NewGuid().ToString("N").[..7]
    let path = $"/tmp/fsbar-test-{guid}.sock"
    let listener = Connection.createListener path
    try
        Assert.True(File.Exists(path))
    finally
        listener.Close()
        Connection.cleanup path None

[<Fact>]
let ``createListener_removes_stale_socket`` () =
    let guid = Guid.NewGuid().ToString("N").[..7]
    let path = $"/tmp/fsbar-test-{guid}.sock"
    // Create a stale file
    File.WriteAllText(path, "stale")
    let listener = Connection.createListener path
    try
        Assert.True(File.Exists(path))
    finally
        listener.Close()
        Connection.cleanup path None

[<Fact>]
let ``cleanup_removes_socket_file`` () =
    let guid = Guid.NewGuid().ToString("N").[..7]
    let path = $"/tmp/fsbar-test-{guid}.sock"
    File.WriteAllText(path, "test")
    Connection.cleanup path None
    Assert.False(File.Exists(path))

[<Fact>]
let ``acceptConnection_timeout_throws`` () =
    let guid = Guid.NewGuid().ToString("N").[..7]
    let path = $"/tmp/fsbar-test-{guid}.sock"
    let listener = Connection.createListener path
    try
        Assert.Throws<exn>(fun () ->
            Connection.acceptConnection listener 1 |> ignore
        ) |> ignore
    finally
        listener.Close()
        Connection.cleanup path None
