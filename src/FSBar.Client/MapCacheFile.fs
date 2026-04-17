namespace FSBar.Client

open System
open System.IO
open System.IO.Compression
open System.Text
open System.Text.Json

module MapCacheFile =

    let schemaVersion: int = 2

    let codeVersion: int = 1

    type SupportedMap = {
        MapName: string
        Sd7FileStem: string
        BaseCentre: float32 * float32 * float32
        ChokepointQuery: ChokepointQuery
    }

    let supportedMaps: SupportedMap list = [
        { MapName = "Avalanche 3.4"
          Sd7FileStem = "avalanche_3.4"
          BaseCentre = (500.0f, 0.0f, 397.0f)
          ChokepointQuery =
            { Chokepoints.defaultChokepointQuery MoveType.Kbot with
                MaxWidthElmos = 240.0f
                SearchRadiusElmos = 5500.0f } }
        { MapName = "Altair Crossing 4.1"
          Sd7FileStem = "altair_crossing_v4.1"
          BaseCentre = (500.0f, 0.0f, 500.0f)
          ChokepointQuery =
            { Chokepoints.defaultChokepointQuery MoveType.Kbot with
                MaxWidthElmos = 240.0f
                SearchRadiusElmos = 5500.0f } }
        { MapName = "All That Glitters 2.2.3"
          Sd7FileStem = "all_that_glitters_v2.2.3"
          BaseCentre = (500.0f, 0.0f, 500.0f)
          ChokepointQuery =
            { Chokepoints.defaultChokepointQuery MoveType.Kbot with
                MaxWidthElmos = 240.0f
                SearchRadiusElmos = 5500.0f } }
        { MapName = "Onyx Cauldron 2.2.2"
          Sd7FileStem = "onyx_cauldron_2.2.2"
          BaseCentre = (500.0f, 0.0f, 500.0f)
          ChokepointQuery =
            { Chokepoints.defaultChokepointQuery MoveType.Kbot with
                MaxWidthElmos = 240.0f
                SearchRadiusElmos = 5500.0f } }
    ]

    let tryFindSupportedMap (mapName: string) : SupportedMap option =
        supportedMaps |> List.tryFind (fun m -> m.MapName = mapName)

    let sanitise (s: string) : string =
        String(
            s.ToLowerInvariant()
            |> Seq.map (fun c -> if Char.IsLetterOrDigit(c) || c = '.' then c else '_')
            |> Seq.toArray)

    type LoadError =
        | FileMissing of path: string
        | ParseFailure of path: string * detail: string
        | SchemaVersionMismatch of path: string * expected: int * found: int
        | CodeVersionMismatch of path: string * expected: int * found: int
        | MapNameMismatch of path: string * expected: string * found: string
        | ParametersMismatch of path: string * detail: string
        | BlobCorrupted of path: string * field: string * detail: string

    type LoadedMap = {
        MapName: string
        Grid: MapGrid
        Chokepoints: Chokepoint list
        BaseCentre: float32 * float32 * float32
    }

    // ---------------------------------------------------------------------
    // Serialization helpers
    // ---------------------------------------------------------------------

    let gzipBytes (bytes: byte[]) : string =
        use ms = new MemoryStream()
        (use gz = new GZipStream(ms, CompressionLevel.Optimal)
         gz.Write(bytes, 0, bytes.Length))
        Convert.ToBase64String(ms.ToArray())

    let gunzipBytes (b64: string) : byte[] =
        let compressed = Convert.FromBase64String b64
        use msIn = new MemoryStream(compressed)
        use gz = new GZipStream(msIn, CompressionMode.Decompress)
        use msOut = new MemoryStream()
        gz.CopyTo msOut
        msOut.ToArray()

    let encodeFloat32Bytes (a: float32[,]) : int * int * byte[] =
        let rows = Array2D.length1 a
        let cols = Array2D.length2 a
        let bytes = Array.zeroCreate<byte> (rows * cols * 4)
        let mutable k = 0
        for i in 0 .. rows - 1 do
            for j in 0 .. cols - 1 do
                let b = BitConverter.GetBytes(a.[i, j])
                Array.blit b 0 bytes k 4
                k <- k + 4
        rows, cols, bytes

    let encodeInt32Bytes (a: int[,]) : int * int * byte[] =
        let rows = Array2D.length1 a
        let cols = Array2D.length2 a
        let bytes = Array.zeroCreate<byte> (rows * cols * 4)
        let mutable k = 0
        for i in 0 .. rows - 1 do
            for j in 0 .. cols - 1 do
                let b = BitConverter.GetBytes(a.[i, j])
                Array.blit b 0 bytes k 4
                k <- k + 4
        rows, cols, bytes

    let writeBlob (w: Utf8JsonWriter) (rows: int) (cols: int) (bytes: byte[]) =
        w.WriteStartObject()
        w.WriteNumber("rows", rows)
        w.WriteNumber("cols", cols)
        w.WriteString("gzipB64", gzipBytes bytes)
        w.WriteEndObject()

    let writeVec3 (w: Utf8JsonWriter) (propName: string) (x: float32) (y: float32) (z: float32) =
        w.WritePropertyName propName
        w.WriteStartObject()
        w.WriteNumber("x", float x)
        w.WriteNumber("y", float y)
        w.WriteNumber("z", float z)
        w.WriteEndObject()

    let querySnapshotFields (q: ChokepointQuery) =
        (string q.MoveType, q.MaxWidthElmos, q.SearchRadiusElmos)

    let write
        (supported: SupportedMap)
        (grid: MapGrid)
        (chokepoints: Chokepoint list)
        (path: string)
        : unit =
        let (bcx, bcy, bcz) = supported.BaseCentre
        let (qMoveType, qMaxWidth, qSearchRadius) = querySnapshotFields supported.ChokepointQuery
        let hmRows, hmCols, hmBytes = encodeFloat32Bytes grid.HeightMap
        let smRows, smCols, smBytes = encodeFloat32Bytes grid.SlopeMap
        let rmRows, rmCols, rmBytes = encodeInt32Bytes grid.ResourceMap

        use ms = new MemoryStream()
        (
            let opts = JsonWriterOptions(Indented = true)
            use w = new Utf8JsonWriter(ms, opts)
            w.WriteStartObject()
            w.WriteNumber("schemaVersion", schemaVersion)
            w.WriteNumber("codeVersion", codeVersion)
            w.WriteString("mapName", supported.MapName)
            w.WriteNumber("widthElmos", grid.WidthElmos)
            w.WriteNumber("heightElmos", grid.HeightElmos)
            w.WriteNumber("widthHeightmap", grid.WidthHeightmap)
            w.WriteNumber("heightHeightmap", grid.HeightHeightmap)
            writeVec3 w "baseCentre" bcx bcy bcz

            w.WritePropertyName "chokepointQuery"
            w.WriteStartObject()
            w.WriteString("moveType", qMoveType)
            w.WriteNumber("maxWidthElmos", float qMaxWidth)
            w.WriteNumber("searchRadiusElmos", float qSearchRadius)
            w.WriteEndObject()

            w.WritePropertyName "chokepoints"
            w.WriteStartArray()
            for cp in chokepoints do
                let (ChokepointId idNum) = cp.Id
                let (px, py, pz) = cp.Position
                let (ox, oz) = cp.OutwardDir
                w.WriteStartObject()
                w.WriteNumber("id", uint64 idNum)
                writeVec3 w "position" px py pz
                w.WriteNumber("widthElmos", float cp.WidthElmos)
                w.WriteNumber("outwardDirX", float ox)
                w.WriteNumber("outwardDirZ", float oz)
                w.WriteNumber("distanceFromBase", float cp.DistanceFromBase)
                w.WriteEndObject()
            w.WriteEndArray()

            w.WritePropertyName "heightmap"
            writeBlob w hmRows hmCols hmBytes
            w.WritePropertyName "slopeMap"
            writeBlob w smRows smCols smBytes
            w.WritePropertyName "resourceMap"
            writeBlob w rmRows rmCols rmBytes

            w.WriteEndObject()
            w.Flush()
        )
        let dir = Path.GetDirectoryName path
        if not (String.IsNullOrEmpty dir) && not (Directory.Exists dir) then
            Directory.CreateDirectory dir |> ignore
        File.WriteAllBytes(path, ms.ToArray())

    // ---------------------------------------------------------------------
    // Read helpers
    // ---------------------------------------------------------------------

    let tryGetProp (el: JsonElement) (name: string) : JsonElement option =
        let mutable v = Unchecked.defaultof<JsonElement>
        if el.TryGetProperty(name, &v) then Some v else None

    let getProp (el: JsonElement) (name: string) : JsonElement =
        el.GetProperty name

    let decodeBlobBytes (path: string) (field: string) (el: JsonElement) : Result<int * int * byte[], LoadError> =
        try
            let rows = (getProp el "rows").GetInt32()
            let cols = (getProp el "cols").GetInt32()
            let b64 = (getProp el "gzipB64").GetString()
            let bytes =
                try Result.Ok (gunzipBytes b64)
                with ex -> Result.Error (BlobCorrupted(path, field, "gzip decode failure: " + ex.Message))
            match bytes with
            | Result.Error e -> Result.Error e
            | Result.Ok bs ->
                let expected = rows * cols * 4
                if bs.Length <> expected then
                    Result.Error (BlobCorrupted(path, field, sprintf "size mismatch: expected %d bytes, got %d" expected bs.Length))
                else
                    Result.Ok (rows, cols, bs)
        with ex ->
            Result.Error (BlobCorrupted(path, field, "shape error: " + ex.Message))

    let bytesToFloat32Array2D (rows: int) (cols: int) (bytes: byte[]) : float32[,] =
        let out = Array2D.zeroCreate rows cols
        let mutable k = 0
        for i in 0 .. rows - 1 do
            for j in 0 .. cols - 1 do
                out.[i, j] <- BitConverter.ToSingle(bytes, k)
                k <- k + 4
        out

    let bytesToInt32Array2D (rows: int) (cols: int) (bytes: byte[]) : int[,] =
        let out = Array2D.zeroCreate rows cols
        let mutable k = 0
        for i in 0 .. rows - 1 do
            for j in 0 .. cols - 1 do
                out.[i, j] <- BitConverter.ToInt32(bytes, k)
                k <- k + 4
        out

    let read
        (supported: SupportedMap)
        (path: string)
        : Result<LoadedMap, LoadError> =
        if not (File.Exists path) then
            Result.Error (FileMissing path)
        else
            let parsed : Result<JsonDocument, LoadError> =
                try
                    let txt = File.ReadAllText path
                    Result.Ok (JsonDocument.Parse txt)
                with ex ->
                    Result.Error (ParseFailure(path, ex.Message))
            match parsed with
            | Result.Error e -> Result.Error e
            | Result.Ok doc ->
                use _ = doc
                try
                    let root = doc.RootElement
                    if root.ValueKind <> JsonValueKind.Object then
                        Result.Error (ParseFailure(path, "root is not a JSON object"))
                    else
                        let schemaV = (getProp root "schemaVersion").GetInt32()
                        let codeV = (getProp root "codeVersion").GetInt32()
                        let mapName = (getProp root "mapName").GetString()
                        if schemaV <> schemaVersion then
                            Result.Error (SchemaVersionMismatch(path, schemaVersion, schemaV))
                        elif codeV <> codeVersion then
                            Result.Error (CodeVersionMismatch(path, codeVersion, codeV))
                        elif mapName <> supported.MapName then
                            Result.Error (MapNameMismatch(path, supported.MapName, mapName))
                        else
                            let widthElmos = (getProp root "widthElmos").GetInt32()
                            let heightElmos = (getProp root "heightElmos").GetInt32()
                            let widthHeightmap = (getProp root "widthHeightmap").GetInt32()
                            let heightHeightmap = (getProp root "heightHeightmap").GetInt32()

                            let baseCentreEl = getProp root "baseCentre"
                            let bcx = (getProp baseCentreEl "x").GetSingle()
                            let bcy = (getProp baseCentreEl "y").GetSingle()
                            let bcz = (getProp baseCentreEl "z").GetSingle()
                            let (ebcx, ebcy, ebcz) = supported.BaseCentre
                            if bcx <> ebcx || bcy <> ebcy || bcz <> ebcz then
                                let detail =
                                    sprintf "baseCentre: expected (%g,%g,%g), found (%g,%g,%g)"
                                        ebcx ebcy ebcz bcx bcy bcz
                                Result.Error (ParametersMismatch(path, detail))
                            else
                                let queryEl = getProp root "chokepointQuery"
                                let qMove = (getProp queryEl "moveType").GetString()
                                let qMax = (getProp queryEl "maxWidthElmos").GetSingle()
                                let qRadius = (getProp queryEl "searchRadiusElmos").GetSingle()
                                let (eMove, eMax, eRadius) = querySnapshotFields supported.ChokepointQuery
                                if qMove <> eMove || qMax <> eMax || qRadius <> eRadius then
                                    let detail =
                                        sprintf "chokepointQuery: expected moveType=%s maxWidthElmos=%g searchRadiusElmos=%g, found moveType=%s maxWidthElmos=%g searchRadiusElmos=%g"
                                            eMove eMax eRadius qMove qMax qRadius
                                    Result.Error (ParametersMismatch(path, detail))
                                else
                                    match decodeBlobBytes path "heightMap" (getProp root "heightmap") with
                                    | Result.Error e -> Result.Error e
                                    | Result.Ok (hmR, hmC, hmB) ->
                                    match decodeBlobBytes path "slopeMap" (getProp root "slopeMap") with
                                    | Result.Error e -> Result.Error e
                                    | Result.Ok (smR, smC, smB) ->
                                    match decodeBlobBytes path "resourceMap" (getProp root "resourceMap") with
                                    | Result.Error e -> Result.Error e
                                    | Result.Ok (rmR, rmC, rmB) ->
                                        let hm = bytesToFloat32Array2D hmR hmC hmB
                                        let sm = bytesToFloat32Array2D smR smC smB
                                        let rm = bytesToInt32Array2D rmR rmC rmB
                                        let grid =
                                            { WidthElmos = widthElmos
                                              HeightElmos = heightElmos
                                              WidthHeightmap = widthHeightmap
                                              HeightHeightmap = heightHeightmap
                                              HeightMap = hm
                                              SlopeMap = sm
                                              ResourceMap = rm
                                              LosMap = Array2D.zeroCreate widthHeightmap heightHeightmap
                                              RadarMap = Array2D.zeroCreate widthHeightmap heightHeightmap }
                                        let cpsEl = getProp root "chokepoints"
                                        let cps =
                                            [ for e in cpsEl.EnumerateArray() do
                                                let idNum = (getProp e "id").GetUInt32()
                                                let posEl = getProp e "position"
                                                let px = (getProp posEl "x").GetSingle()
                                                let py = (getProp posEl "y").GetSingle()
                                                let pz = (getProp posEl "z").GetSingle()
                                                yield
                                                    { Id = ChokepointId idNum
                                                      Position = (px, py, pz)
                                                      WidthElmos = (getProp e "widthElmos").GetSingle()
                                                      OutwardDir =
                                                        ((getProp e "outwardDirX").GetSingle(),
                                                         (getProp e "outwardDirZ").GetSingle())
                                                      DistanceFromBase = (getProp e "distanceFromBase").GetSingle() } ]
                                        Result.Ok
                                            { MapName = mapName
                                              Grid = grid
                                              Chokepoints = cps
                                              BaseCentre = (bcx, bcy, bcz) }
                with ex ->
                    Result.Error (ParseFailure(path, ex.Message))

    let cachePathFor (repoRoot: string) (supported: SupportedMap) : string =
        let safe = sanitise supported.MapName
        Path.Combine(repoRoot, "bots", "trainer", "map-cache", safe + ".json")

    let formatLoadError (error: LoadError) : string =
        match error with
        | FileMissing path ->
            sprintf "map-cache: cache file not found at %s\n  codeVersion=%d required\n  run: bots/trainer/map-cache/refresh-all.sh" path codeVersion
        | ParseFailure(path, detail) ->
            sprintf "map-cache: failed to parse %s\n  detail: %s\n  run: bots/trainer/map-cache/refresh-all.sh" path detail
        | SchemaVersionMismatch(path, expected, found) ->
            sprintf "map-cache: schemaVersion mismatch at %s\n  expected %d, found %d\n  run: bots/trainer/map-cache/refresh-all.sh" path expected found
        | CodeVersionMismatch(path, expected, found) ->
            sprintf "map-cache: stale codeVersion at %s\n  expected codeVersion=%d, found %d\n  run: bots/trainer/map-cache/refresh-all.sh" path expected found
        | MapNameMismatch(path, expected, found) ->
            sprintf "map-cache: mapName mismatch at %s\n  expected \"%s\", found \"%s\"\n  run: bots/trainer/map-cache/refresh-all.sh" path expected found
        | ParametersMismatch(path, detail) ->
            sprintf "map-cache: parameters changed at %s\n  detail: %s\n  run: bots/trainer/map-cache/refresh-all.sh" path detail
        | BlobCorrupted(path, field, detail) ->
            sprintf "map-cache: corrupted blob at %s\n  field: %s\n  detail: %s\n  run: bots/trainer/map-cache/refresh-all.sh" path field detail
