module FSBar.Client.Tests.SmfParserTests

open System
open System.IO
open Xunit
open FSBar.Client

// ---------------------------------------------------------------------------
// Helpers: build a minimal valid SMF blob in memory.
// ---------------------------------------------------------------------------

let private smfMagic =
    [| byte 's'; byte 'p'; byte 'r'; byte 'i'; byte 'n'; byte 'g'
       byte ' '; byte 'm'; byte 'a'; byte 'p'; byte ' '; byte 'f'
       byte 'i'; byte 'l'; byte 'e'; 0uy |]

let private writeInt32 (buf: byte[]) (offset: int) (value: int) =
    buf.[offset] <- byte (value &&& 0xFF)
    buf.[offset + 1] <- byte ((value >>> 8) &&& 0xFF)
    buf.[offset + 2] <- byte ((value >>> 16) &&& 0xFF)
    buf.[offset + 3] <- byte ((value >>> 24) &&& 0xFF)

let private writeFloat32 (buf: byte[]) (offset: int) (value: float32) =
    let bytes = BitConverter.GetBytes(value)
    Array.blit bytes 0 buf offset 4

let private writeUInt16 (buf: byte[]) (offset: int) (value: uint16) =
    buf.[offset] <- byte (value &&& 0xFFus)
    buf.[offset + 1] <- byte ((value >>> 8) &&& 0xFFus)

/// Build a minimal valid 8×8 SMF blob. Heightmap is constant mid-range
/// (raw=32768), metal map is all zeros, type map is all zeros.
let private buildMinimalSmf (version: int) (mapx: int) (mapy: int) : byte[] =
    let headerSize = 76
    let hmCount = (mapx + 1) * (mapy + 1)
    let hmBytes = hmCount * 2
    let halfCount = (mapx / 2) * (mapy / 2)
    let heightMapPtr = headerSize
    let typeMapPtr = heightMapPtr + hmBytes
    let metalMapPtr = typeMapPtr + halfCount
    let total = metalMapPtr + halfCount
    let buf = Array.zeroCreate total
    Array.blit smfMagic 0 buf 0 16
    writeInt32 buf 16 version
    writeInt32 buf 20 0              // mapid
    writeInt32 buf 24 mapx
    writeInt32 buf 28 mapy
    writeInt32 buf 32 8              // squareSize
    writeInt32 buf 36 8              // texelPerSquare
    writeInt32 buf 40 32             // tileSize
    writeFloat32 buf 44 0.0f         // minHeight
    writeFloat32 buf 48 256.0f       // maxHeight
    writeInt32 buf 52 heightMapPtr
    writeInt32 buf 56 typeMapPtr
    writeInt32 buf 60 0              // tilesPtr
    writeInt32 buf 64 0              // miniMapPtr
    writeInt32 buf 68 metalMapPtr
    writeInt32 buf 72 0              // numExtraHeaders
    // Fill heightmap with raw=32768 → height = (32768/65536) * 256 = 128.0
    for i in 0 .. hmCount - 1 do
        writeUInt16 buf (heightMapPtr + i * 2) 32768us
    // typeMap + metalMap zeros already from Array.zeroCreate
    buf

// ---------------------------------------------------------------------------
// Layer-1 unit tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``parseBytes minimal 8x8 SMF → Ok with expected dimensions`` () =
    let bytes = buildMinimalSmf 1 8 8
    match SmfParser.parseBytes "test.smf" bytes with
    | Result.Ok smf ->
        Assert.Equal(8, smf.WidthHeightmap)
        Assert.Equal(8, smf.HeightHeightmap)
        Assert.Equal(64, smf.WidthElmos)
        Assert.Equal(64, smf.HeightElmos)
        Assert.Equal(9, Array2D.length1 smf.HeightMap)
        Assert.Equal(9, Array2D.length2 smf.HeightMap)
        // All raw values = 32768 → 0 + (32768/65536) * 256 = 128.0
        Assert.InRange(smf.HeightMap.[4, 4], 127.9f, 128.1f)
        Assert.Equal("test.smf", smf.SourceArchive)
    | Result.Error e -> Assert.Fail(sprintf "Expected Ok, got Error %A" e)

[<Fact>]
let ``parseBytes bad magic → Error InvalidMagic`` () =
    let bytes = buildMinimalSmf 1 8 8
    bytes.[0] <- byte 'x'
    match SmfParser.parseBytes "bad.smf" bytes with
    | Result.Error (InvalidMagic _) -> ()
    | other -> Assert.Fail(sprintf "Expected Error InvalidMagic, got %A" other)

[<Fact>]
let ``parseBytes version=2 → Error UnsupportedVersion`` () =
    let bytes = buildMinimalSmf 2 8 8
    match SmfParser.parseBytes "v2.smf" bytes with
    | Result.Error (UnsupportedVersion 2) -> ()
    | other -> Assert.Fail(sprintf "Expected Error UnsupportedVersion 2, got %A" other)

[<Fact>]
let ``parseBytes truncated heightmap → Error Truncated`` () =
    let full = buildMinimalSmf 1 8 8
    // Chop off the last 100 bytes so heightMapPtr + hmBytes exceeds the buffer.
    let truncated = Array.sub full 0 (full.Length - 100)
    match SmfParser.parseBytes "trunc.smf" truncated with
    | Result.Error (Truncated _) -> ()
    | other -> Assert.Fail(sprintf "Expected Error Truncated, got %A" other)

[<Fact>]
let ``toMapGrid preserves heightmap dimensions`` () =
    let bytes = buildMinimalSmf 1 8 8
    match SmfParser.parseBytes "rt.smf" bytes with
    | Result.Ok smf ->
        let grid = SmfParser.toMapGrid smf
        Assert.Equal(smf.WidthHeightmap, grid.WidthHeightmap)
        Assert.Equal(smf.HeightHeightmap, grid.HeightHeightmap)
        Assert.Equal(smf.WidthElmos, grid.WidthElmos)
        Assert.Equal(Array2D.length1 smf.HeightMap, Array2D.length1 grid.HeightMap)
        Assert.Equal(Array2D.length2 smf.HeightMap, Array2D.length2 grid.HeightMap)
    | Result.Error e -> Assert.Fail(sprintf "parse failed: %A" e)

// ---------------------------------------------------------------------------
// Layer-2 integration tests — skipped when the BAR install is absent.
// ---------------------------------------------------------------------------

let private avalanchePath =
    let home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    Path.Combine(home, ".local", "state", "Beyond All Reason", "maps", "avalanche_3.4.sd7")

let private avalancheAvailable () = File.Exists avalanchePath

[<Fact>]
let ``parseSd7 Avalanche 3.4 → Ok with 512x512 heightmap`` () =
    if not (avalancheAvailable ()) then () else
    match SmfParser.parseSd7 avalanchePath with
    | Result.Ok smf ->
        Assert.Equal(512, smf.WidthHeightmap)
        Assert.Equal(512, smf.HeightHeightmap)
        Assert.Equal(513, Array2D.length1 smf.HeightMap)
        Assert.Equal(513, Array2D.length2 smf.HeightMap)
    | Result.Error e -> Assert.Fail(sprintf "parseSd7 failed: %A" e)

[<Fact>]
let ``parseSd7 Avalanche 3.4 heightmap range matches live engine reference within ±1 elmo`` () =
    // SC-010: live-engine probe on Avalanche 3.4 reports min=130.0 / max=700.0
    // (CLAUDE.md FSI MCP notes, verified 2026-04-13).
    if not (avalancheAvailable ()) then () else
    match SmfParser.parseSd7 avalanchePath with
    | Result.Ok smf ->
        let mutable mn = System.Single.MaxValue
        let mutable mx = System.Single.MinValue
        let w = Array2D.length1 smf.HeightMap
        let h = Array2D.length2 smf.HeightMap
        for x in 0 .. w - 1 do
            for z in 0 .. h - 1 do
                let v = smf.HeightMap.[x, z]
                if v < mn then mn <- v
                if v > mx then mx <- v
        Assert.InRange(mn, 129.0f, 131.0f)
        Assert.InRange(mx, 699.0f, 701.0f)
    | Result.Error e -> Assert.Fail(sprintf "parseSd7 failed: %A" e)

[<Fact>]
let ``listInstalledMaps returns at least avalanche when BAR is installed`` () =
    if not (avalancheAvailable ()) then () else
    let maps = SmfParser.listInstalledMaps ()
    Assert.Contains(avalanchePath, maps)
