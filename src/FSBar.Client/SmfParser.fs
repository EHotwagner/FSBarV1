namespace FSBar.Client

open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions

type SmfMap =
    { WidthHeightmap: int
      HeightHeightmap: int
      WidthElmos: int
      HeightElmos: int
      HeightMap: float32[,]
      SlopeMap: float32[,]
      MetalMap: uint8[,]
      TypeMap: uint8[,]
      SourceArchive: string }

type SmfParseError =
    | ArchiveNotFound of path: string
    | ExtractionFailed of archive: string * stderr: string
    | NoSmfInArchive of archive: string
    | InvalidMagic of actualHex: string
    | UnsupportedVersion of version: int
    | Truncated of atOffset: int * expectedBytes: int * availableBytes: int

module SmfParser =

    // "spring map file\0" = 16 bytes
    let private smfMagic =
        [| byte 's'; byte 'p'; byte 'r'; byte 'i'; byte 'n'; byte 'g'
           byte ' '; byte 'm'; byte 'a'; byte 'p'; byte ' '; byte 'f'
           byte 'i'; byte 'l'; byte 'e'; 0uy |]

    let private bytesToHex (bytes: byte[]) =
        bytes |> Array.map (sprintf "%02x") |> String.concat ""

    let private readInt32LE (buf: byte[]) (offset: int) : int =
        int buf.[offset]
        ||| (int buf.[offset + 1] <<< 8)
        ||| (int buf.[offset + 2] <<< 16)
        ||| (int buf.[offset + 3] <<< 24)

    let private readFloat32LE (buf: byte[]) (offset: int) : float32 =
        BitConverter.ToSingle(buf, offset)

    let private readUInt16LE (buf: byte[]) (offset: int) : uint16 =
        uint16 buf.[offset] ||| (uint16 buf.[offset + 1] <<< 8)

    /// Spring 2×2-corner slope kernel. Output shape (mapx/2) × (mapy/2).
    let private computeSlopeMap
        (heightMap: float32[,])
        (mapx: int)
        (mapy: int)
        (squareSize: int)
        : float32[,] =
        let outW = mapx / 2
        let outH = mapy / 2
        let invDen = 1.0f / (2.0f * float32 squareSize)
        Array2D.init outW outH (fun ox oz ->
            let x = ox * 2
            let z = oz * 2
            let h00 = heightMap.[x, z]
            let h10 = heightMap.[x + 1, z]
            let h01 = heightMap.[x, z + 1]
            let h11 = heightMap.[x + 1, z + 1]
            let dx = (h10 + h11) - (h00 + h01)
            let dz = (h01 + h11) - (h00 + h10)
            sqrt (dx * dx + dz * dz) * invDen)

    /// Internal parser that accepts an optional (minHeight, maxHeight) override
    /// extracted from mapinfo.lua's `smf = { minheight = ...; maxheight = ... }` block.
    /// When present, overrides the values baked into the SMF header — matching the
    /// behaviour of the live engine, which applies the mapinfo override before
    /// computing real-world heights from the raw uint16 heightmap values.
    let private parseBytesCore
        (sourceName: string)
        (bytes: byte[])
        (heightOverride: (float32 * float32) option)
        : Result<SmfMap, SmfParseError> =
        if bytes.Length < 16 then
            Result.Error(Truncated(0, 16, bytes.Length))
        else
            let magicSlice = Array.sub bytes 0 16
            if magicSlice <> smfMagic then
                Result.Error(InvalidMagic(bytesToHex magicSlice))
            // Spring SMF v1 header (76 bytes):
            //   16  int32 version
            //   20  int32 mapid
            //   24  int32 mapx             (heightmap squares)
            //   28  int32 mapy
            //   32  int32 squareSize       (elmos/square, typically 8)
            //   36  int32 texelPerSquare
            //   40  int32 tileSize
            //   44  float minHeight
            //   48  float maxHeight
            //   52  int32 heightMapPtr
            //   56  int32 typeMapPtr
            //   60  int32 tilesPtr
            //   64  int32 miniMapPtr
            //   68  int32 metalMapPtr
            //   72  int32 numExtraHeaders
            elif bytes.Length < 76 then
                Result.Error(Truncated(16, 60, bytes.Length - 16))
            else
                let version = readInt32LE bytes 16
                if version <> 1 then
                    Result.Error(UnsupportedVersion version)
                else
                    let mapx = readInt32LE bytes 24
                    let mapy = readInt32LE bytes 28
                    let squareSize = readInt32LE bytes 32
                    let headerMin = readFloat32LE bytes 44
                    let headerMax = readFloat32LE bytes 48
                    let minHeight, maxHeight =
                        match heightOverride with
                        | Some(mn, mx) -> mn, mx
                        | None -> headerMin, headerMax
                    let heightMapPtr = readInt32LE bytes 52
                    let typeMapPtr = readInt32LE bytes 56
                    let metalMapPtr = readInt32LE bytes 68

                    let hmW = mapx + 1
                    let hmH = mapy + 1
                    let hmCount = hmW * hmH
                    let hmBytes = hmCount * 2
                    if heightMapPtr < 0 || heightMapPtr + hmBytes > bytes.Length then
                        Result.Error(Truncated(heightMapPtr, hmBytes, max 0 (bytes.Length - heightMapPtr)))
                    else
                        let heightSpan = maxHeight - minHeight
                        let invUInt16 = 1.0f / 65536.0f
                        let heightMap =
                            Array2D.init hmW hmH (fun x z ->
                                let raw = readUInt16LE bytes (heightMapPtr + (z * hmW + x) * 2)
                                minHeight + float32 raw * invUInt16 * heightSpan)

                        let halfW = mapx / 2
                        let halfH = mapy / 2
                        let halfCount = halfW * halfH
                        if metalMapPtr < 0 || metalMapPtr + halfCount > bytes.Length then
                            Result.Error(Truncated(metalMapPtr, halfCount, max 0 (bytes.Length - metalMapPtr)))
                        elif typeMapPtr < 0 || typeMapPtr + halfCount > bytes.Length then
                            Result.Error(Truncated(typeMapPtr, halfCount, max 0 (bytes.Length - typeMapPtr)))
                        else
                            let metalMap =
                                Array2D.init halfW halfH (fun x z ->
                                    bytes.[metalMapPtr + z * halfW + x])
                            let typeMap =
                                Array2D.init halfW halfH (fun x z ->
                                    bytes.[typeMapPtr + z * halfW + x])
                            let slopeMap = computeSlopeMap heightMap mapx mapy squareSize
                            Result.Ok
                                { WidthHeightmap = mapx
                                  HeightHeightmap = mapy
                                  WidthElmos = mapx * squareSize
                                  HeightElmos = mapy * squareSize
                                  HeightMap = heightMap
                                  SlopeMap = slopeMap
                                  MetalMap = metalMap
                                  TypeMap = typeMap
                                  SourceArchive = sourceName }

    let parseBytes (sourceName: string) (bytes: byte[]) : Result<SmfMap, SmfParseError> =
        parseBytesCore sourceName bytes None

    /// Extract `smf.minheight` / `smf.maxheight` overrides from a mapinfo.lua file.
    /// Returns `Some (min, max)` only when both fields are present; otherwise `None`.
    /// Deliberately regex-based (no Lua interpreter) — the relevant fields are simple
    /// numeric assignments and we do not need to evaluate the whole file.
    let private parseMapinfoHeightOverride (luaText: string) : (float32 * float32) option =
        let rxMin = Regex(@"minheight\s*=\s*(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase)
        let rxMax = Regex(@"maxheight\s*=\s*(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase)
        let mMin = rxMin.Match(luaText)
        let mMax = rxMax.Match(luaText)
        if mMin.Success && mMax.Success then
            match Single.TryParse(mMin.Groups.[1].Value), Single.TryParse(mMax.Groups.[1].Value) with
            | (true, mn), (true, mx) -> Some(mn, mx)
            | _ -> None
        else
            None

    let private extractSmfToTemp (sd7Path: string) : Result<string * string, SmfParseError> =
        let tempDir =
            Path.Combine(Path.GetTempPath(), "fsbar-smf-" + Path.GetRandomFileName())
        Directory.CreateDirectory(tempDir) |> ignore
        let psi = ProcessStartInfo()
        psi.FileName <- "bsdtar"
        psi.ArgumentList.Add("-xf")
        psi.ArgumentList.Add(sd7Path)
        psi.ArgumentList.Add("-C")
        psi.ArgumentList.Add(tempDir)
        // Extract both the SMF subtree and mapinfo.lua (if present). bsdtar accepts
        // multiple include patterns; missing entries are silently skipped.
        psi.ArgumentList.Add("maps/")
        psi.ArgumentList.Add("mapinfo.lua")
        psi.RedirectStandardError <- true
        psi.RedirectStandardOutput <- true
        psi.UseShellExecute <- false
        psi.CreateNoWindow <- true
        use p = Process.Start(psi)
        let stderr = p.StandardError.ReadToEnd()
        p.WaitForExit()
        if p.ExitCode <> 0 then
            try Directory.Delete(tempDir, true) with _ -> ()
            Result.Error(ExtractionFailed(sd7Path, stderr))
        else
            let smfPath =
                Directory.GetFiles(tempDir, "*.smf", SearchOption.AllDirectories)
                |> Array.tryHead
            match smfPath with
            | None ->
                try Directory.Delete(tempDir, true) with _ -> ()
                Result.Error(NoSmfInArchive sd7Path)
            | Some path -> Result.Ok(path, tempDir)

    let parseSd7 (sd7Path: string) : Result<SmfMap, SmfParseError> =
        if not (File.Exists sd7Path) then
            Result.Error(ArchiveNotFound sd7Path)
        else
            match extractSmfToTemp sd7Path with
            | Result.Error e -> Result.Error e
            | Result.Ok(smfPath, tempDir) ->
                let result =
                    try
                        let bytes = File.ReadAllBytes(smfPath)
                        let heightOverride =
                            // mapinfo.lua lives at the root of the archive when present.
                            let candidate = Path.Combine(tempDir, "mapinfo.lua")
                            if File.Exists candidate then
                                try
                                    parseMapinfoHeightOverride (File.ReadAllText candidate)
                                with _ -> None
                            else
                                None
                        parseBytesCore sd7Path bytes heightOverride
                    with ex ->
                        Result.Error(ExtractionFailed(sd7Path, sprintf "read error: %s" ex.Message))
                try Directory.Delete(tempDir, true) with _ -> ()
                result

    let toMapGrid (smf: SmfMap) : MapGrid =
        let resW = Array2D.length1 smf.MetalMap
        let resH = Array2D.length2 smf.MetalMap
        // MapGrid.ResourceMap is int[,] at heightmap resolution in engine-loaded grids.
        // SMF metal is half resolution; upsample nearest-neighbour so callers can index
        // with heightmap coordinates consistently with loadFromEngine output.
        let resourceMap =
            Array2D.init smf.WidthHeightmap smf.HeightHeightmap (fun x z ->
                let sx = min (x / 2) (resW - 1)
                let sz = min (z / 2) (resH - 1)
                int smf.MetalMap.[sx, sz])
        let losMap = Array2D.zeroCreate smf.WidthHeightmap smf.HeightHeightmap
        let radarMap = Array2D.zeroCreate smf.WidthHeightmap smf.HeightHeightmap
        { WidthElmos = smf.WidthElmos
          HeightElmos = smf.HeightElmos
          WidthHeightmap = smf.WidthHeightmap
          HeightHeightmap = smf.HeightHeightmap
          HeightMap = smf.HeightMap
          SlopeMap = smf.SlopeMap
          ResourceMap = resourceMap
          LosMap = losMap
          RadarMap = radarMap }

    let listInstalledMaps () : string list =
        let home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        let mapsDir = Path.Combine(home, ".local", "state", "Beyond All Reason", "maps")
        if not (Directory.Exists mapsDir) then
            []
        else
            Directory.GetFiles(mapsDir, "*.sd7")
            |> Array.sort
            |> Array.toList
