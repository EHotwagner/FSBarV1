namespace FSBar.Client

open System
open System.IO
open System.Net.Sockets

module Connection =

    /// Create a Unix domain socket listener bound to the given path.
    /// Removes any stale socket file before binding.
    let createListener (socketPath: string) : Socket =
        if File.Exists(socketPath) then
            File.Delete(socketPath)

        let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified)
        let endpoint = Net.Sockets.UnixDomainSocketEndPoint(socketPath)
        socket.Bind(endpoint)
        socket.Listen(1)
        socket

    /// Accept a single connection from the listener within the given timeout (ms).
    /// Returns the accepted client socket and a NetworkStream wrapping it.
    let acceptConnection (listener: Socket) (timeoutMs: int) : Socket * NetworkStream =
        if not (listener.Poll(timeoutMs * 1000, SelectMode.SelectRead)) then
            failwith $"No connection received within {timeoutMs}ms timeout"

        let client = listener.Accept()
        let stream = new NetworkStream(client, ownsSocket = false)
        (client, stream)

    /// Read exactly `count` bytes from the stream.
    /// Raises on premature end-of-stream.
    let private readExact (stream: NetworkStream) (count: int) : byte[] =
        let buffer = Array.zeroCreate<byte> count
        let mutable offset = 0

        while offset < count do
            let bytesRead = stream.Read(buffer, offset, count - offset)

            if bytesRead = 0 then
                failwith "Connection closed while reading data"

            offset <- offset + bytesRead

        buffer

    /// Send a protobuf-serialized message as a length-prefixed frame:
    /// 4-byte little-endian length header followed by the payload bytes.
    let sendMessage (stream: NetworkStream) (data: byte[]) : unit =
        let lengthHeader = BitConverter.GetBytes(uint32 data.Length)

        if not BitConverter.IsLittleEndian then
            Array.Reverse(lengthHeader)

        stream.Write(lengthHeader, 0, 4)
        stream.Write(data, 0, data.Length)
        stream.Flush()

    /// Receive a length-prefixed frame and return the raw payload bytes.
    /// Reads a 4-byte little-endian length header, then reads that many bytes.
    let recvBytes (stream: NetworkStream) : byte[] =
        let headerBytes = readExact stream 4

        let length =
            if BitConverter.IsLittleEndian then
                BitConverter.ToInt32(headerBytes, 0)
            else
                BitConverter.ToInt32(Array.rev headerBytes, 0)

        if length <= 0 then
            failwith $"Invalid message length: {length}"

        readExact stream length

    /// Clean up the socket and remove the socket file from disk.
    let cleanup (socketPath: string) (socket: Socket option) : unit =
        socket
        |> Option.iter (fun s ->
            try
                s.Shutdown(SocketShutdown.Both)
            with
            | _ -> ()

            s.Close()
            s.Dispose())

        if File.Exists(socketPath) then
            try
                File.Delete(socketPath)
            with
            | _ -> ()
