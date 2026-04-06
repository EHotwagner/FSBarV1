namespace FSBar.Client

open System.IO
open System.Net.Sockets

type EngineDisconnectedException =
    inherit IOException
    new: message: string * ?lastFrameNumber: uint32 * ?innerException: exn -> EngineDisconnectedException
    member LastFrameNumber: uint32 option

module Connection =

    /// Create a Unix domain socket listener bound to the given path.
    /// Removes any stale socket file before binding.
    val createListener: socketPath: string -> Socket

    /// Accept a single connection from the listener within the given timeout (ms).
    /// Returns the accepted client socket and a NetworkStream wrapping it.
    val acceptConnection: listener: Socket -> timeoutMs: int -> readTimeoutMs: int -> Socket * NetworkStream

    /// Send a protobuf-serialized message as a length-prefixed frame:
    /// 4-byte little-endian length header followed by the payload bytes.
    val sendMessage: stream: NetworkStream -> data: byte[] -> unit

    /// Receive a length-prefixed frame and return the raw payload bytes.
    /// Reads a 4-byte little-endian length header, then reads that many bytes.
    val recvBytes: stream: NetworkStream -> byte[]

    /// Clean up the socket and remove the socket file from disk.
    val cleanup: socketPath: string -> Socket option -> unit
