# Contract — Extended `MapGridCache` JSON schema (v1)

**Feature**: 025-macro-primitive-driven
**Target file**: `bots/trainer/map-cache/<mapName>.json`
**Tier**: N/A (generated artifact; schema versioned in-band via `mapGrid.schemaVersion`)
**Maps to**: FR-014, FR-015; clarification Q2; research R1

## Rationale

Feature 024's map cache captures chokepoints (already-used subset) and a handful of metadata fields. Feature 025 needs the bot to load a real `MapGrid` at warmup — not the 024 synthetic skeleton — to drive `BasePlan.resolvePlan` terrain checks and `Pathing.findPath` attack routing. Per research R1, the cache-extension strategy is chosen because inline `SmfParser.parseSd7` at warmup blows the 100 ms FR-015 budget by 5–12×. This contract specifies the exact on-disk schema the cache writer emits and the reader consumes.

## Schema (v1)

```json
{
  "mapName": "string — e.g. 'Avalanche 3.4'",
  "sd7Path": "string — absolute or ~-expanded path to source .sd7",
  "widthHeightmap": "int — map heightmap cell count in x",
  "heightHeightmap": "int — map heightmap cell count in z",
  "widthElmos": "int — map width in elmos (widthHeightmap * 8)",
  "heightElmos": "int — map height in elmos (heightHeightmap * 8)",
  "baseCentre.x": "float — commander spawn or operator-picked base centre x",
  "baseCentre.y": "float — ground height at base centre",
  "baseCentre.z": "float — commander spawn or operator-picked base centre z",
  "query.maxWidthElmos": "float — Chokepoints.findChokepoints width parameter",
  "query.searchRadiusElmos": "float — Chokepoints.findChokepoints search radius",

  "chokepoints": [
    {
      "id": "uint32 — stable chokepoint id",
      "position.x": "float",
      "position.y": "float",
      "position.z": "float",
      "widthElmos": "float",
      "outwardDir.x": "float",
      "outwardDir.z": "float",
      "distanceFromBase": "float"
    }
  ],

  "mapGrid": {
    "schemaVersion": 1,
    "widthElmos": "int — MUST equal top-level widthElmos",
    "heightElmos": "int — MUST equal top-level heightElmos",
    "widthHeightmap": "int — MUST equal top-level widthHeightmap",
    "heightHeightmap": "int — MUST equal top-level heightHeightmap",
    "heightMap.gzip.b64": "string — base64 of gzip of float32[(widthHeightmap+1) * (heightHeightmap+1)] row-major little-endian, one value per heightmap corner vertex (matches engine's getCornersHeightMap)",
    "slopeMap.gzip.b64":  "string — base64 of gzip of float32[(widthHeightmap/2) * (heightHeightmap/2)] row-major little-endian, matches engine's getSlopeMap dimensions",
    "resourceMap.gzip.b64": "string — base64 of gzip of float32[(widthHeightmap/2) * (heightHeightmap/2)] row-major little-endian, metal map values 0..1"
  },

  "generatedAtUtc": "ISO-8601 timestamp — when 14-cache-map-analysis.fsx wrote this file",
  "sourceArchive": "string — e.g. 'avalanche_3.4.sd7'"
}
```

### Field invariants

- **Dimensions consistency**: `mapGrid.widthHeightmap` MUST equal the top-level `widthHeightmap`. Same for `heightHeightmap`. Mismatch is a hard-fail at cache load; the reader logs `[cache] dimension mismatch <detail>` and the bot aborts warmup per FR-014 hard-fail path.
- **`schemaVersion` check**: reader MUST verify `mapGrid.schemaVersion = 1` and reject unknown versions with a clear error. Forward compatibility is not required; future features increment the version when the schema changes semantically.
- **Array lengths after decompression**:
  - `heightMap` decompressed length MUST equal `(widthHeightmap + 1) * (heightHeightmap + 1) * 4` bytes (float32 per corner vertex).
  - `slopeMap` decompressed length MUST equal `(widthHeightmap / 2) * (heightHeightmap / 2) * 4` bytes.
  - `resourceMap` decompressed length MUST equal `(widthHeightmap / 2) * (heightHeightmap / 2) * 4` bytes.
  - Length mismatch is a hard-fail.
- **Endianness**: explicit little-endian. The serialiser uses `System.BitConverter` which is little-endian on every platform .NET 10 supports on Linux; the reader uses the same.
- **`LosMap` and `RadarMap` are NOT in the cache**: they are runtime-populated by engine callbacks per sim-second during live play, so baking them offline would ship stale values. The bot initialises both to `Array2D.zeroCreate w h` at `MapGridCache.loadFromJson` time — downstream consumers in this feature (`BasePlan.resolvePlan`, `Pathing.findPath`) don't read them.

### Absent-field semantics

Per FR-014 and clarification Q2:

| Case | Cache file | `mapGrid` block | Bot action |
|---|---|---|---|
| Target-set map, full cache | present | present | Load real `MapGrid`, proceed. |
| Target-set map, chokepoints only | present | absent | **Hard-fail warmup** with `[warmup] no MapGrid in cache at <path> — run scripts/examples/14-cache-map-analysis.fsx '<map>'`. Operator re-runs the cache writer. |
| Target-set map, no cache file | absent | — | **Hard-fail warmup** with same message. |
| Non-target-set map, full cache | present | present | Load real `MapGrid`, proceed. (Target-set expansion costs nothing on existing caches.) |
| Non-target-set map, chokepoints only | present | absent | Log `[cache-miss] WARN: US1/US2 will behave like 024 partial — run 14-cache-map-analysis.fsx`, fall back to 024 synthetic skeleton, proceed. |
| Non-target-set map, no cache file | absent | — | Same `[cache-miss] WARN` trace + synthetic skeleton fallback. |

## Writer contract (`scripts/examples/14-cache-map-analysis.fsx`)

The cache-writer script consumes `SmfParser.parseSd7` + `Chokepoints.findChokepoints` (024 surface) and now ALSO emits the `mapGrid` block. Writer-side logic sketch:

```fsharp
let smf = SmfParser.parseSd7 sd7Path |> Result.get  // existing 024 path
// smf gives us heightMap, slopeMap, resourceMap as float32 Array2Ds

let gzipAndBase64 (a: float32[,]) : string =
    let rows = Array2D.length1 a
    let cols = Array2D.length2 a
    let bytes = Array.zeroCreate<byte> (rows * cols * 4)
    let mutable k = 0
    for i in 0 .. rows - 1 do
        for j in 0 .. cols - 1 do
            let b = BitConverter.GetBytes(a.[i, j])
            Array.blit b 0 bytes k 4
            k <- k + 4
    use ms = new MemoryStream()
    use gz = new GZipStream(ms, CompressionMode.Compress)
    gz.Write(bytes, 0, bytes.Length)
    gz.Close()
    Convert.ToBase64String(ms.ToArray())

let mapGridJson =
    {| schemaVersion = 1
       widthElmos = smf.widthElmos
       heightElmos = smf.heightElmos
       widthHeightmap = smf.widthHeightmap
       heightHeightmap = smf.heightHeightmap
       ``heightMap.gzip.b64`` = gzipAndBase64 smf.heightMap
       ``slopeMap.gzip.b64``  = gzipAndBase64 smf.slopeMap
       ``resourceMap.gzip.b64`` = gzipAndBase64 smf.resourceMap |}
```

The writer MUST NOT change the existing top-level field names or `chokepoints[]` shape — the 024 reader would break. Field-addition only.

## Reader contract (`bots/trainer/bot_macro.fsx` local helper `MapGridCache.loadFromJson`)

The reader is a ~40 LOC local helper inside `bot_macro.fsx`. Not a new module in `FSBar.Client`. Sketch:

```fsharp
let private base64GzipToFloat32Array2D (b64: string) (rows: int) (cols: int) : float32[,] =
    let compressed = Convert.FromBase64String b64
    use ms = new MemoryStream(compressed)
    use gz = new GZipStream(ms, CompressionMode.Decompress)
    let expected = rows * cols * 4
    let bytes = Array.zeroCreate<byte> expected
    let mutable off = 0
    while off < expected do
        let n = gz.Read(bytes, off, expected - off)
        if n = 0 then failwithf "gzip stream truncated: got %d, expected %d" off expected
        off <- off + n
    let result = Array2D.zeroCreate<float32> rows cols
    let mutable k = 0
    for i in 0 .. rows - 1 do
        for j in 0 .. cols - 1 do
            result.[i, j] <- BitConverter.ToSingle(bytes, k)
            k <- k + 4
    result

let MapGridCache_loadFromJson (fullPath: string) : MapGrid option =
    if not (File.Exists fullPath) then None
    else
        let doc = JsonDocument.Parse(File.ReadAllText fullPath)
        let root = doc.RootElement
        match root.TryGetProperty "mapGrid" with
        | true, mg when mg.GetProperty("schemaVersion").GetInt32() = 1 ->
            let wh = mg.GetProperty("widthHeightmap").GetInt32()
            let hh = mg.GetProperty("heightHeightmap").GetInt32()
            let we = mg.GetProperty("widthElmos").GetInt32()
            let he = mg.GetProperty("heightElmos").GetInt32()
            let hm = base64GzipToFloat32Array2D (mg.GetProperty("heightMap.gzip.b64").GetString()) (wh + 1) (hh + 1)
            let sm = base64GzipToFloat32Array2D (mg.GetProperty("slopeMap.gzip.b64").GetString()) (wh / 2) (hh / 2)
            let rm = base64GzipToFloat32Array2D (mg.GetProperty("resourceMap.gzip.b64").GetString()) (wh / 2) (hh / 2)
            Some { WidthElmos = we; HeightElmos = he
                   WidthHeightmap = wh; HeightHeightmap = hh
                   HeightMap = hm; SlopeMap = sm; ResourceMap = rm
                   LosMap = Array2D.zeroCreate wh hh
                   RadarMap = Array2D.zeroCreate wh hh }
        | _ -> None
```

Error-path traces are explicit — silent `None` is returned only for "file missing" or "schema absent"; every other failure is a `failwithf` that bubbles up to the warmup exception handler, which logs and then either hard-fails (target-set map) or falls through to the synthetic skeleton (non-target-set map).

## Migration / operator workflow

1. **Adding a new target-set map**: operator runs `dotnet fsi scripts/examples/14-cache-map-analysis.fsx '<map name>'` with the feature-025 updated cache writer. The new JSON cache includes the `mapGrid` block. The map name is then added to `MapTargetSet.contains` in `bot_macro.fsx`.
2. **Re-baking an existing cache after a map version bump**: same command, overwrites the JSON file. The `generatedAtUtc` changes so post-run analysis can spot staleness.
3. **Deleting a cache**: if the file is absent and the map is in the target set, the bot refuses to warmup. Operator must either re-bake or drop the map from the target set.
4. **Backporting to 024 bot**: the extended cache is field-addition-compatible. A 024-era bot reading the 025-written cache would ignore the `mapGrid` block and behave as before. No rollback pain.

## File-size expectation

For Avalanche 3.4 (512×512 heightmap):
- Raw payload: ~1.5 MB (513²×4 heightmap + 256²×4 slope + 256²×4 resource)
- Post-gzip: ~300–600 KB depending on terrain smoothness
- Base64 inflation: ×1.33 → ~400–800 KB of text
- Final JSON file: ~400–800 KB (dominated by the three base64 strings)

The file should be added to `.gitignore` as part of this feature: `bots/trainer/map-cache/*.json` (existing 024 cache file moves from tracked to gitignored in the same commit that extends the schema). This is consistent with "map cache is a generated artifact" per the 024 operator workflow.
