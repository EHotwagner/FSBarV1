module FSBar.Client.Tests.MapCacheFileCorruptionTests

open System
open System.IO
open System.Text.Json
open Xunit
open FSBar.Client

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

// Replace the gzipB64 string inside the heightmap subobject with newValue.
let rewriteBlobB64 (path: string) (blobField: string) (newValue: string) =
    let text = File.ReadAllText path
    use doc = JsonDocument.Parse text
    use out = new MemoryStream()
    (
        let w = new Utf8JsonWriter(out, JsonWriterOptions(Indented = true))
        w.WriteStartObject()
        for prop in doc.RootElement.EnumerateObject() do
            if prop.Name = blobField then
                w.WritePropertyName blobField
                w.WriteStartObject()
                for sub in prop.Value.EnumerateObject() do
                    if sub.Name = "gzipB64" then
                        w.WriteString("gzipB64", newValue)
                    else
                        sub.WriteTo w
                w.WriteEndObject()
            else
                prop.WriteTo w
        w.WriteEndObject()
        w.Flush()
    )
    File.WriteAllBytes(path, out.ToArray())

[<Fact>]
let ``FileMissing returns FileMissing with anchors`` () =
    let supported = tinySupported ()
    let missing = Path.Combine(Path.GetTempPath(), sprintf "nonexistent_%d.json" (Guid.NewGuid().GetHashCode()))
    match MapCacheFile.read supported missing with
    | Result.Error (MapCacheFile.FileMissing p) ->
        Assert.Equal(missing, p)
        let msg = MapCacheFile.formatLoadError (MapCacheFile.FileMissing p)
        Assert.Contains(missing, msg)
        Assert.Contains("cache file not found", msg)
        Assert.Contains("codeVersion", msg)
        Assert.Contains("refresh-all.sh", msg)
    | other -> Assert.Fail(sprintf "expected FileMissing, got %A" other)

[<Fact>]
let ``ParseFailure on invalid JSON returns ParseFailure with anchors`` () =
    let path = Path.GetTempFileName()
    try
        File.WriteAllText(path, "not json at all {{{")
        let supported = tinySupported ()
        match MapCacheFile.read supported path with
        | Result.Error (MapCacheFile.ParseFailure(p, detail)) ->
            Assert.Equal(path, p)
            let msg = MapCacheFile.formatLoadError (MapCacheFile.ParseFailure(p, detail))
            Assert.Contains(path, msg)
            Assert.Contains("failed to parse", msg)
            Assert.Contains(detail, msg)
            Assert.Contains("refresh-all.sh", msg)
        | other -> Assert.Fail(sprintf "expected ParseFailure, got %A" other)
    finally
        File.Delete path

[<Fact>]
let ``BlobCorrupted size mismatch returns BlobCorrupted with size mismatch detail`` () =
    let path, supported = writeValid ()
    try
        // Produce a valid gzip blob whose decompressed length is deliberately
        // wrong for a heightMap rows*cols*4 of (8+1)*(8+1)*4 = 324 bytes.
        let tooFew = Array.zeroCreate<byte> 8
        use ms = new MemoryStream()
        (
            use gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionLevel.Optimal)
            gz.Write(tooFew, 0, tooFew.Length)
        )
        let b64 = Convert.ToBase64String(ms.ToArray())
        rewriteBlobB64 path "heightmap" b64
        match MapCacheFile.read supported path with
        | Result.Error (MapCacheFile.BlobCorrupted(p, field, detail)) ->
            Assert.Equal(path, p)
            Assert.Equal("heightMap", field)
            Assert.Contains("size mismatch", detail)
            let msg = MapCacheFile.formatLoadError (MapCacheFile.BlobCorrupted(p, field, detail))
            Assert.Contains(path, msg)
            Assert.Contains(field, msg)
            Assert.Contains("size mismatch", msg)
            Assert.Contains("refresh-all.sh", msg)
        | other -> Assert.Fail(sprintf "expected BlobCorrupted, got %A" other)
    finally
        File.Delete path

[<Fact>]
let ``BlobCorrupted gzip decode failure returns BlobCorrupted with gzip decode failure detail`` () =
    let path, supported = writeValid ()
    try
        // Replace the heightMap gzipB64 with non-gzip base64 (plain "hello world" base64).
        let bogus = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes "hello world, not gzip")
        rewriteBlobB64 path "heightmap" bogus
        match MapCacheFile.read supported path with
        | Result.Error (MapCacheFile.BlobCorrupted(p, field, detail)) ->
            Assert.Equal(path, p)
            Assert.Equal("heightMap", field)
            Assert.Contains("gzip decode failure", detail)
            let msg = MapCacheFile.formatLoadError (MapCacheFile.BlobCorrupted(p, field, detail))
            Assert.Contains(path, msg)
            Assert.Contains(field, msg)
            Assert.Contains("gzip decode failure", msg)
            Assert.Contains("refresh-all.sh", msg)
        | other -> Assert.Fail(sprintf "expected BlobCorrupted, got %A" other)
    finally
        File.Delete path
