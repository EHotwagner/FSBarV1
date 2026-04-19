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

/// <summary>
/// Raised by <see cref="M:FSBar.Client.Protocol.sendCallback"/> when
/// <c>CallbackResponse.RequestId</c> does not match the in-flight
/// <c>CallbackRequest.RequestId</c>. Distinct from
/// <see cref="T:FSBar.Client.EngineDisconnectedException"/> so callers can tell
/// "proxy sent the wrong response" apart from "socket went away".
/// </summary>
type ProtocolMismatchException =
    inherit exn
    new: message: string * ?innerException: exn -> ProtocolMismatchException

/// <summary>
/// Raised when the HighBar proxy does not advertise a callback id that
/// FSBar requires (currently only <c>CALLBACK_GAME_GET_STATE = 15</c>
/// from spec 045). Pre-0.1.5 proxies reject callback 15 with
/// "Unknown callback id" and the session terminates — no legacy
/// fallback. <c>RequiredVersion</c> names the minimum HighBarV2 proxy
/// version in the message ("0.1.5").
/// </summary>
type ProxyVersionMismatchException =
    inherit exn
    new: message: string * requiredVersion: string * ?innerException: exn -> ProxyVersionMismatchException
    member RequiredVersion: string

module Protocol =
    /// <summary>
    /// When <c>false</c>, <c>sendCallback</c> silently drops events from
    /// interleaved <c>Frame</c> messages (the pre-031 behaviour). When
    /// <c>true</c>, interleaved frames are stashed in a per-stream replay
    /// buffer and drained by the next <c>receiveFrame</c> on the same stream.
    ///
    /// Default <c>false</c>. Set to <c>true</c> by <c>BarClient.Start()</c>
    /// after warmup + unit-def loading completes so mid-game callback
    /// round-trips preserve engine events.
    /// </summary>
    val mutable replayBufferEnabled: bool

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
