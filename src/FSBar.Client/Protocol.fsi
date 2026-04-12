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

    /// Receive one frame from the proxy.
    ///
    /// Returns <c>Some frame</c> for a normal Frame message. Also returns
    /// <c>Some</c> a synthetic terminal frame when the proxy delivers its
    /// standalone Shutdown envelope: the synthetic frame has
    /// <c>FrameNumber = 0u</c> and carries a single
    /// <c>GameEvent.Shutdown</c> event. Callers that need the last real
    /// game-frame number must rewrite the sentinel before dispatching.
    ///
    /// A clean socket close without a Shutdown envelope raises
    /// <see cref="T:FSBar.Client.EngineDisconnectedException"/> from the
    /// underlying read.
    ///
    /// See ../HighBarV2/specs/030-proxy-contract-docs/contracts/shutdown-wire-shape.md
    /// for the upstream wire-shape contract.
    val receiveFrame: stream: NetworkStream -> GameFrame option

    /// Send a frame response with commands.
    val sendFrameResponse: stream: NetworkStream -> commands: Highbar.AICommand list -> unit

    /// Send a callback request and wait for the response.
    val sendCallback: stream: NetworkStream -> callbackId: uint32 -> paramList: Highbar.CallbackParam list -> Highbar.CallbackResponse
