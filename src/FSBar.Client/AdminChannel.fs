namespace FSBar.Client

open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading

module AdminChannel =

    type AdminCommandOut =
        | Pause of paused: bool
        | SetGameSpeed of speed: float32
        | SayMessage of text: string
        | KillServer

    type AdminEventIn =
        | ServerStarted
        | ServerQuit of reason: string
        | ServerStartPlaying
        | ServerGameOver
        | PlayerChat of playerId: int * text: string
        | GameWarning of text: string
        | Unknown of code: byte * payload: byte[]

    type ChannelState =
        | NotBound
        | Bound of port: int
        | ReceivingFrom of engineEndpoint: System.Net.IPEndPoint
        | Closed of reason: string

    // -------------------------------------------------------------------
    // Wire codec — authoritative reference at
    // specs/039-hub-admin-channel/contracts/autohost-wire.md
    //
    // The engine treats inbound autohost UDP datagrams as raw UTF-8
    // chat strings (verified against Recoil rts/Net/AutohostInterface.cpp
    // GetChatMessage + GameServer.cpp Update dispatcher). Strings with
    // a leading '/' are parsed as slash commands via PushAction;
    // anything else is broadcast as chat from SERVER_PLAYER.
    //
    // Outbound (hub → engine) chat-command translations:
    //   KillServer        → "/kill"
    //   Pause true        → "/pause 1"
    //   Pause false       → "/pause 0"
    //   SetGameSpeed N    → ["/setminspeed N"; "/setmaxspeed N"]   (min FIRST — see R1)
    //   SayMessage text   → text (bare, no leading '/')
    //
    // Inbound (engine → hub) action codes (1-byte action + payload):
    //   0 SERVER_STARTED, 1 SERVER_QUIT, 2 SERVER_STARTPLAYING,
    //   3 SERVER_GAMEOVER, 4 SERVER_MESSAGE (text), 5 SERVER_WARNING (text),
    //   10 PLAYER_JOINED, 11 PLAYER_LEFT, 12 PLAYER_READY,
    //   13 PLAYER_CHAT, 14 PLAYER_DEFEATED,
    //   20 GAME_LUAMSG, 60 GAME_TEAMSTAT.

    /// Translate an outbound command into one or more UTF-8 datagrams.
    /// `SetGameSpeed` requires two datagrams (setminspeed + setmaxspeed)
    /// to lock the engine's effective speed at the requested multiplier.
    let encodeCommandToDatagrams (cmd: AdminCommandOut) : byte[][] =
        let toBytes (s: string) = Encoding.UTF8.GetBytes(s)
        match cmd with
        | KillServer -> [| toBytes "/kill" |]
        | Pause paused ->
            // Spring's PushAction("pause") accepts an optional flag.
            // Send the flag explicitly so we never accidentally toggle
            // the wrong way after a hub-side state mismatch.
            [| toBytes (if paused then "/pause 1" else "/pause 0") |]
        | SetGameSpeed speed ->
            // The engine has no /setspeed — only /setminspeed + /setmaxspeed.
            // Each caps the argument at the OTHER bound before assigning
            // (setminspeed N → min = min(N, currentMax); setmaxspeed N →
            // max = max(N, currentMin)), then calls UserSpeedChange which
            // clamps current speed to the new [min, max] range.
            //
            // To raise speed past the current ceiling OR lower it below
            // the current floor, we have to relax the blocking bound
            // first. Sending three datagrams — setmaxspeed, setminspeed,
            // setmaxspeed — is direction-agnostic and always collapses
            // [min, max] to [N, N], clamping current speed to N.
            let s = sprintf "%g" speed
            [| toBytes (sprintf "/setmaxspeed %s" s)
               toBytes (sprintf "/setminspeed %s" s)
               toBytes (sprintf "/setmaxspeed %s" s) |]
        | SayMessage text -> [| toBytes text |]

    /// Backwards-compatible single-datagram encode. For commands that
    /// expand to multiple datagrams, returns the first only — callers
    /// that need both should use `encodeCommandToDatagrams`.
    let encodeCommand (cmd: AdminCommandOut) : byte[] =
        (encodeCommandToDatagrams cmd).[0]

    let decodeEvent (bytes: byte[]) : AdminEventIn =
        if isNull bytes || bytes.Length = 0 then
            Unknown(0uy, [||])
        else
            let code = bytes.[0]
            let tail () = bytes.[1..]
            let textTail () =
                if bytes.Length > 1 then
                    Encoding.UTF8.GetString(tail ()).TrimEnd([| '\000' |])
                else ""
            match code with
            | 0uy -> ServerStarted
            | 1uy -> ServerQuit (textTail ())
            | 2uy -> ServerStartPlaying
            | 3uy -> ServerGameOver
            | 5uy -> GameWarning (textTail ())
            | 13uy ->
                // PLAYER_CHAT layout: playernumber(1 B) + destination(1 B) + text(UTF-8)
                if bytes.Length >= 3 then
                    let playerId = int bytes.[1]
                    let text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3).TrimEnd([| '\000' |])
                    PlayerChat(playerId, text)
                elif bytes.Length = 2 then
                    PlayerChat(int bytes.[1], "")
                else
                    PlayerChat(0, "")
            | other ->
                Unknown(other, tail ())

    // -------------------------------------------------------------------
    // Live channel implementation

    type private EventFanOut() =
        let observers = ResizeArray<IObserver<AdminEventIn>>()
        let sync = obj ()

        let snapshot () =
            lock sync (fun () -> observers.ToArray())

        interface IObservable<AdminEventIn> with
            member _.Subscribe(observer: IObserver<AdminEventIn>) =
                if isNull (box observer) then
                    raise (ArgumentNullException("observer"))
                lock sync (fun () -> observers.Add(observer))
                { new IDisposable with
                    member _.Dispose() =
                        lock sync (fun () -> observers.Remove(observer) |> ignore) }

        member _.Publish(evt: AdminEventIn) =
            for obs in snapshot () do
                try obs.OnNext(evt)
                with _ -> ()

        member _.CompleteAll() =
            let all = snapshot ()
            for obs in all do
                try obs.OnCompleted()
                with _ -> ()

        member _.ErrorAll(ex: exn) =
            let all = snapshot ()
            for obs in all do
                try obs.OnError(ex)
                with _ -> ()

    [<Sealed>]
    type AdminChannel internal (udp: UdpClient, localPort: int) =
        let mutable state: ChannelState = Bound localPort
        let mutable observedEngine: IPEndPoint option = None
        let fanOut = EventFanOut()
        let sync = obj ()
        let mutable disposed = 0
        let cts = new CancellationTokenSource()

        let transitionToClosed (reason: string) =
            lock sync (fun () -> state <- Closed reason)
            fanOut.CompleteAll()

        let receiveLoop () =
            let mutable running = true
            while running && not cts.IsCancellationRequested do
                try
                    let mutable remote = IPEndPoint(IPAddress.Any, 0)
                    let bytes = udp.Receive(&remote)
                    eprintfn "[AdminChannel] RECV from %O len=%d code=%d bytes=%s"
                        remote bytes.Length
                        (if bytes.Length > 0 then int bytes.[0] else -1)
                        (System.BitConverter.ToString(bytes))
                    lock sync (fun () ->
                        match state with
                        | Bound _ | ReceivingFrom _ ->
                            state <- ReceivingFrom remote
                            observedEngine <- Some remote
                        | _ -> ())
                    let evt = decodeEvent bytes
                    fanOut.Publish(evt)
                    match evt with
                    | ServerQuit _ ->
                        transitionToClosed "engine quit"
                        running <- false
                    | _ -> ()
                with
                | :? ObjectDisposedException ->
                    running <- false
                | :? SocketException as sx ->
                    transitionToClosed (sprintf "socket error: %s" sx.Message)
                    running <- false
                | ex ->
                    transitionToClosed (sprintf "receive loop error: %s" ex.Message)
                    running <- false

        let pumpThread =
            let t = Thread(ThreadStart(receiveLoop))
            t.IsBackground <- true
            t.Name <- "FSBar.AdminChannel.Recv"
            t.Start()
            t

        member _.State =
            lock sync (fun () -> state)

        /// The hub's loopback port — written into
        /// `springsettings.cfg.AutohostPort` so the engine sends its
        /// notifications here.
        member _.LocalPort =
            lock sync (fun () ->
                match state with
                | NotBound -> None
                | Bound _ -> Some localPort
                | ReceivingFrom _ -> Some localPort
                | Closed _ -> None)

        member _.Events: IObservable<AdminEventIn> = fanOut :> IObservable<AdminEventIn>

        member this.Send(cmd: AdminCommandOut) : Result<unit, string> =
            let snapState = this.State
            match snapState with
            | Closed r -> Result.Error (sprintf "channel closed: %s" r)
            | NotBound -> Result.Error "channel not bound"
            | Bound _ ->
                Result.Error "admin channel not yet attached (engine has not sent any datagram)"
            | ReceivingFrom ep ->
                try
                    let datagrams = encodeCommandToDatagrams cmd
                    let mutable lastError = None
                    for bytes in datagrams do
                        let text = Encoding.UTF8.GetString(bytes)
                        eprintfn "[AdminChannel] SEND len=%d to %O text=%s"
                            bytes.Length ep text
                        let sent = udp.Send(bytes, bytes.Length, ep)
                        if sent <> bytes.Length then
                            lastError <-
                                Some (sprintf "partial UDP write (%d of %d)" sent bytes.Length)
                    match lastError with
                    | Some e -> Result.Error e
                    | None -> Ok ()
                with ex ->
                    Result.Error (sprintf "send failed: %s" ex.Message)

        interface IDisposable with
            member _.Dispose() =
                if Interlocked.Exchange(&disposed, 1) = 0 then
                    try cts.Cancel() with _ -> ()
                    try udp.Close() with _ -> ()
                    try (udp :> IDisposable).Dispose() with _ -> ()
                    lock sync (fun () ->
                        match state with
                        | Closed _ -> ()
                        | _ -> state <- Closed "disposed")
                    fanOut.CompleteAll()

    let bind () : Result<AdminChannel, string> =
        try
            // The engine sends notifications TO `AutohostIP:AutohostPort`,
            // so the hub binds that loopback port and waits. The hub
            // sends commands back to the engine's observed source
            // endpoint once the first inbound datagram arrives.
            let endpoint = IPEndPoint(IPAddress.Loopback, 0)
            let udp = new UdpClient(endpoint)
            let localPort = (udp.Client.LocalEndPoint :?> IPEndPoint).Port
            Ok (new AdminChannel(udp, localPort))
        with ex ->
            Result.Error (sprintf "UDP bind failed: %s" ex.Message)
