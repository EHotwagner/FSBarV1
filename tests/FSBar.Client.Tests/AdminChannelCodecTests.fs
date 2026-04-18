module FSBar.Client.Tests.AdminChannelCodecTests

open System
open System.Text
open Xunit
open FSBar.Client
open FSBar.Client.AdminChannel

// Feature 039 — round-trip codec tests for the autohost text protocol.
// The engine treats inbound autohost UDP datagrams as UTF-8 strings,
// dispatched through PushAction when prefixed with '/'. Bare text
// becomes server chat. Outbound (engine→hub) datagrams use the original
// 1-byte action header + payload format.

let private datagramText (bytes: byte[]) = Encoding.UTF8.GetString(bytes)

// -----------------------------------------------------------------------------
// Outbound (hub → engine) — encodeCommandToDatagrams

[<Fact>]
let ``KillServer encodes to /kill`` () =
    let dg = encodeCommandToDatagrams KillServer
    Assert.Equal(1, dg.Length)
    Assert.Equal("/kill", datagramText dg.[0])

[<Fact>]
let ``Pause true encodes to /pause 1`` () =
    let dg = encodeCommandToDatagrams (Pause true)
    Assert.Equal(1, dg.Length)
    Assert.Equal("/pause 1", datagramText dg.[0])

[<Fact>]
let ``Pause false encodes to /pause 0`` () =
    let dg = encodeCommandToDatagrams (Pause false)
    Assert.Equal(1, dg.Length)
    Assert.Equal("/pause 0", datagramText dg.[0])

[<Fact>]
let ``SetGameSpeed expands to setminspeed plus setmaxspeed`` () =
    let dg = encodeCommandToDatagrams (SetGameSpeed 2.0f)
    Assert.Equal(2, dg.Length)
    Assert.Equal("/setminspeed 2", datagramText dg.[0])
    Assert.Equal("/setmaxspeed 2", datagramText dg.[1])

[<Fact>]
let ``SetGameSpeed fractional formats without trailing zeros`` () =
    let dg = encodeCommandToDatagrams (SetGameSpeed 0.5f)
    Assert.Equal("/setminspeed 0.5", datagramText dg.[0])
    Assert.Equal("/setmaxspeed 0.5", datagramText dg.[1])

[<Fact>]
let ``SayMessage emits raw text without slash prefix`` () =
    let dg = encodeCommandToDatagrams (SayMessage "hello")
    Assert.Equal(1, dg.Length)
    Assert.Equal("hello", datagramText dg.[0])

[<Fact>]
let ``SayMessage preserves unicode`` () =
    let msg = "phase-start — start builders"
    let dg = encodeCommandToDatagrams (SayMessage msg)
    Assert.Equal(msg, datagramText dg.[0])

[<Fact>]
let ``encodeCommand returns first datagram for backwards compatibility`` () =
    let primary = encodeCommand (Pause true)
    Assert.Equal("/pause 1", datagramText primary)

// -----------------------------------------------------------------------------
// Inbound (engine → hub) — decodeEvent

[<Fact>]
let ``decode_ServerStarted_from_code_0`` () =
    Assert.Equal(ServerStarted, decodeEvent [| 0uy |])

[<Fact>]
let ``decode_ServerQuit_empty_reason_from_code_1`` () =
    match decodeEvent [| 1uy |] with
    | ServerQuit reason -> Assert.Equal("", reason)
    | other -> failwithf "expected ServerQuit, got %A" other

[<Fact>]
let ``decode_ServerQuit_preserves_trailing_bytes_as_reason`` () =
    let payload =
        Array.append [| 1uy |] (Encoding.UTF8.GetBytes("timeout"))
    match decodeEvent payload with
    | ServerQuit reason -> Assert.Equal("timeout", reason)
    | other -> failwithf "expected ServerQuit, got %A" other

[<Fact>]
let ``decode_ServerStartPlaying_from_code_2`` () =
    Assert.Equal(ServerStartPlaying, decodeEvent [| 2uy; 0uy; 0uy |])

[<Fact>]
let ``decode_ServerGameOver_from_code_3`` () =
    Assert.Equal(ServerGameOver, decodeEvent [| 3uy; 7uy |])

[<Fact>]
let ``decode_PlayerChat_from_code_13_extracts_player_and_text`` () =
    let header = [| 13uy; 42uy; 0uy |]
    let body = Encoding.UTF8.GetBytes("gg")
    let payload = Array.append header body
    match decodeEvent payload with
    | PlayerChat(playerId, text) ->
        Assert.Equal(42, playerId)
        Assert.Equal("gg", text)
    | other -> failwithf "expected PlayerChat, got %A" other

[<Fact>]
let ``decode_GameWarning_from_code_5`` () =
    let payload =
        Array.append [| 5uy |] (Encoding.UTF8.GetBytes("speed clamped"))
    match decodeEvent payload with
    | GameWarning text -> Assert.Equal("speed clamped", text)
    | other -> failwithf "expected GameWarning, got %A" other

[<Fact>]
let ``decode_unknown_action_code_becomes_Unknown`` () =
    // Code 60 = GAME_TEAMSTAT — explicitly routed through Unknown so
    // future engine revisions don't silently vanish.
    let payload = [| 60uy; 1uy; 2uy; 3uy |]
    match decodeEvent payload with
    | Unknown(code, bytes) ->
        Assert.Equal(60uy, code)
        Assert.Equal<byte[]>([| 1uy; 2uy; 3uy |], bytes)
    | other -> failwithf "expected Unknown, got %A" other

[<Fact>]
let ``decode_empty_bytes_returns_unknown_zero`` () =
    match decodeEvent [||] with
    | Unknown(code, bytes) ->
        Assert.Equal(0uy, code)
        Assert.Empty(bytes)
    | other -> failwithf "expected Unknown(0, []), got %A" other
