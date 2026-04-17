module FSBar.Client.Tests.MapCacheFileVersionTests

open System.IO
open System.Text.Json
open Xunit
open FSBar.Client

// Reuse the synthetic grid builder from Roundtrip tests.
let tinyGrid () : MapGrid =
    let w, h = 8, 8
    let heightMap = Array2D.init (w + 1) (h + 1) (fun i j -> float32 (i * 10 + j))
    let slopeMap = Array2D.init 4 4 (fun i j -> float32 (i + j) * 0.05f)
    let resourceMap = Array2D.init w h (fun i j -> i * 100 + j)
    { WidthElmos = w * 8
      HeightElmos = h * 8
      WidthHeightmap = w
      HeightHeightmap = h
      HeightMap = heightMap
      SlopeMap = slopeMap
      ResourceMap = resourceMap
      LosMap = Array2D.create w h 0
      RadarMap = Array2D.create w h 0 }

let tinySupported () : MapCacheFile.SupportedMap =
    { MapName = "Synth Tiny"
      Sd7FileStem = "synth_tiny"
      BaseCentre = (32.0f, 0.0f, 32.0f)
      ChokepointQuery =
        { Chokepoints.defaultChokepointQuery MoveType.Kbot with
            MaxWidthElmos = 40.0f
            SearchRadiusElmos = 200.0f } }

let writeValid () =
    let grid = tinyGrid ()
    let supported = tinySupported ()
    let cps = Chokepoints.findChokepoints grid supported.BaseCentre supported.ChokepointQuery
    let path = Path.GetTempFileName()
    MapCacheFile.write supported grid cps path
    path, supported

// Rewrite a single top-level numeric JSON field in place.
let rewriteIntField (path: string) (field: string) (newValue: int) =
    let text = File.ReadAllText path
    use doc = JsonDocument.Parse text
    use out = new MemoryStream()
    (
        let w = new Utf8JsonWriter(out, JsonWriterOptions(Indented = true))
        w.WriteStartObject()
        for prop in doc.RootElement.EnumerateObject() do
            if prop.Name = field then
                w.WriteNumber(field, newValue)
            else
                prop.WriteTo w
        w.WriteEndObject()
        w.Flush()
    )
    File.WriteAllBytes(path, out.ToArray())

let rewriteStringField (path: string) (field: string) (newValue: string) =
    let text = File.ReadAllText path
    use doc = JsonDocument.Parse text
    use out = new MemoryStream()
    (
        let w = new Utf8JsonWriter(out, JsonWriterOptions(Indented = true))
        w.WriteStartObject()
        for prop in doc.RootElement.EnumerateObject() do
            if prop.Name = field then
                w.WriteString(field, newValue)
            else
                prop.WriteTo w
        w.WriteEndObject()
        w.Flush()
    )
    File.WriteAllBytes(path, out.ToArray())

// Rewrite a numeric field inside the baseCentre subobject.
let rewriteBaseCentre (path: string) (x: float32) (y: float32) (z: float32) =
    let text = File.ReadAllText path
    use doc = JsonDocument.Parse text
    use out = new MemoryStream()
    (
        let w = new Utf8JsonWriter(out, JsonWriterOptions(Indented = true))
        w.WriteStartObject()
        for prop in doc.RootElement.EnumerateObject() do
            if prop.Name = "baseCentre" then
                w.WritePropertyName "baseCentre"
                w.WriteStartObject()
                w.WriteNumber("x", float x)
                w.WriteNumber("y", float y)
                w.WriteNumber("z", float z)
                w.WriteEndObject()
            else
                prop.WriteTo w
        w.WriteEndObject()
        w.Flush()
    )
    File.WriteAllBytes(path, out.ToArray())

[<Fact>]
let ``schemaVersion mismatch returns SchemaVersionMismatch with anchors`` () =
    let path, supported = writeValid ()
    try
        rewriteIntField path "schemaVersion" (MapCacheFile.schemaVersion + 1)
        match MapCacheFile.read supported path with
        | Result.Error (MapCacheFile.SchemaVersionMismatch(p, expected, found)) ->
            Assert.Equal(path, p)
            Assert.Equal(MapCacheFile.schemaVersion, expected)
            Assert.Equal(MapCacheFile.schemaVersion + 1, found)
            let msg = MapCacheFile.formatLoadError (MapCacheFile.SchemaVersionMismatch(p, expected, found))
            Assert.Contains(path, msg)
            Assert.Contains("schemaVersion", msg)
            Assert.Contains(sprintf "expected %d" MapCacheFile.schemaVersion, msg)
            Assert.Contains("refresh-all.sh", msg)
        | other -> Assert.Fail(sprintf "expected SchemaVersionMismatch, got %A" other)
    finally
        File.Delete path

[<Fact>]
let ``codeVersion mismatch returns CodeVersionMismatch with anchors`` () =
    let path, supported = writeValid ()
    try
        rewriteIntField path "codeVersion" (MapCacheFile.codeVersion + 1)
        match MapCacheFile.read supported path with
        | Result.Error (MapCacheFile.CodeVersionMismatch(p, expected, found)) ->
            Assert.Equal(path, p)
            Assert.Equal(MapCacheFile.codeVersion, expected)
            Assert.Equal(MapCacheFile.codeVersion + 1, found)
            let msg = MapCacheFile.formatLoadError (MapCacheFile.CodeVersionMismatch(p, expected, found))
            Assert.Contains(path, msg)
            Assert.Contains("codeVersion", msg)
            Assert.Contains(sprintf "expected codeVersion=%d" MapCacheFile.codeVersion, msg)
            Assert.Contains("refresh-all.sh", msg)
        | other -> Assert.Fail(sprintf "expected CodeVersionMismatch, got %A" other)
    finally
        File.Delete path

[<Fact>]
let ``mapName mismatch returns MapNameMismatch with anchors`` () =
    let path, supported = writeValid ()
    try
        rewriteStringField path "mapName" "Some Other Map"
        match MapCacheFile.read supported path with
        | Result.Error (MapCacheFile.MapNameMismatch(p, expected, found)) ->
            Assert.Equal(path, p)
            Assert.Equal(supported.MapName, expected)
            Assert.Equal("Some Other Map", found)
            let msg = MapCacheFile.formatLoadError (MapCacheFile.MapNameMismatch(p, expected, found))
            Assert.Contains(path, msg)
            Assert.Contains("mapName", msg)
            Assert.Contains(sprintf "expected \"%s\"" supported.MapName, msg)
            Assert.Contains("refresh-all.sh", msg)
        | other -> Assert.Fail(sprintf "expected MapNameMismatch, got %A" other)
    finally
        File.Delete path

[<Fact>]
let ``parameter mismatch on baseCentre returns ParametersMismatch with anchors`` () =
    let path, supported = writeValid ()
    try
        rewriteBaseCentre path 1.0f 2.0f 3.0f
        match MapCacheFile.read supported path with
        | Result.Error (MapCacheFile.ParametersMismatch(p, detail)) ->
            Assert.Equal(path, p)
            let msg = MapCacheFile.formatLoadError (MapCacheFile.ParametersMismatch(p, detail))
            Assert.Contains(path, msg)
            Assert.Contains("parameters changed", msg)
            Assert.Contains(detail, msg)
            Assert.Contains("refresh-all.sh", msg)
        | other -> Assert.Fail(sprintf "expected ParametersMismatch, got %A" other)
    finally
        File.Delete path
