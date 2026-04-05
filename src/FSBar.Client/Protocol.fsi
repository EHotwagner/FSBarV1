namespace FSBar.Client

open System.Net.Sockets

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
    /// Perform handshake with the proxy. Returns handshake info on success.
    val handshake: stream: NetworkStream -> HandshakeInfo

    /// Receive one frame from the proxy. Returns None on Shutdown.
    val receiveFrame: stream: NetworkStream -> GameFrame option

    /// Send a frame response with commands.
    val sendFrameResponse: stream: NetworkStream -> commands: Highbar.AICommand list -> unit

    /// Send a callback request and wait for the response.
    val sendCallback: stream: NetworkStream -> callbackId: uint32 -> paramList: Highbar.CallbackParam list -> Highbar.CallbackResponse
