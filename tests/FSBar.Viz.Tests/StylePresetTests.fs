module FSBar.Viz.Tests.StylePresetTests

open System
open System.IO
open Xunit
open SkiaSharp
open FSBar.Viz

let cfg = VizDefaults.defaultConfig

// Use a unique subdirectory of the project's preset directory for isolation.
let testPresetName (suffix: string) =
    sprintf "unit-test-%s-%s" suffix (Guid.NewGuid().ToString("N").Substring(0, 8))

let cleanup (names: string list) =
    for n in names do
        StylePreset.delete n |> ignore

[<Fact>]
let ``isValidName accepts alphanumeric, spaces, hyphens, underscores`` () =
    Assert.True(StylePreset.isValidName "simple")
    Assert.True(StylePreset.isValidName "with spaces")
    Assert.True(StylePreset.isValidName "hyphen-name")
    Assert.True(StylePreset.isValidName "under_score")
    Assert.True(StylePreset.isValidName "Abc123 x-y_z")

[<Fact>]
let ``isValidName rejects empty, whitespace, special chars`` () =
    Assert.False(StylePreset.isValidName "")
    Assert.False(StylePreset.isValidName "   ")
    Assert.False(StylePreset.isValidName "bad/slash")
    Assert.False(StylePreset.isValidName "bad\\back")
    Assert.False(StylePreset.isValidName "bad:colon")
    Assert.False(StylePreset.isValidName "bad.dot")

[<Fact>]
let ``fromConfig produces preset with expected key count`` () =
    let preset = StylePreset.fromConfig "test" cfg
    Assert.Equal("test", preset.Name)
    Assert.Equal(ConfigDescriptors.all.Length, preset.Values.Count)

[<Fact>]
let ``save and load roundtrip preserves all values`` () =
    let name = testPresetName "roundtrip"
    try
        let preset = StylePreset.fromConfig name cfg
        match StylePreset.save preset with
        | Result.Ok path ->
            Assert.True(File.Exists path)
            match StylePreset.load name with
            | Result.Ok loaded ->
                Assert.Equal(preset.Name, loaded.Name)
                // Every key present, values equal
                for KeyValue(k, v) in preset.Values do
                    Assert.True(loaded.Values.ContainsKey(k), sprintf "missing key %s" k)
                    Assert.Equal(v, loaded.Values.[k])
            | Result.Error msg -> Assert.Fail(sprintf "load failed: %s" msg)
        | Result.Error msg -> Assert.Fail(sprintf "save failed: %s" msg)
    finally
        cleanup [name]

[<Fact>]
let ``applyToConfig restores full config from preset`` () =
    let name = testPresetName "apply"
    try
        // Modify config, save, then apply back to defaults
        let modified =
            { cfg with
                UnitMarkerSize = 12.5f
                BackgroundColor = SKColor(80uy, 40uy, 20uy)
                ShowGridLines = not cfg.ShowGridLines }
        let preset = StylePreset.fromConfig name modified
        match StylePreset.save preset with
        | Result.Ok _ -> ()
        | Result.Error msg -> Assert.Fail(sprintf "save failed: %s" msg)
        match StylePreset.load name with
        | Result.Ok loaded ->
            let restored = StylePreset.applyToConfig loaded cfg
            Assert.Equal(12.5f, restored.UnitMarkerSize)
            Assert.Equal(modified.BackgroundColor, restored.BackgroundColor)
            Assert.Equal(modified.ShowGridLines, restored.ShowGridLines)
        | Result.Error msg -> Assert.Fail(sprintf "load failed: %s" msg)
    finally
        cleanup [name]

[<Fact>]
let ``partial preset with missing keys preserves current values`` () =
    // Construct preset with only one key
    let partial =
        { Name = "partial"
          CreatedAt = DateTimeOffset.UtcNow
          Values = Map.ofList [ "sizes.unitMarker", PresetValue.FloatVal 20.0f ] }
    let applied = StylePreset.applyToConfig partial cfg
    Assert.Equal(20.0f, applied.UnitMarkerSize)
    // Other fields retain defaults
    Assert.Equal(cfg.ShowGridLines, applied.ShowGridLines)
    Assert.Equal(cfg.BackgroundColor, applied.BackgroundColor)

[<Fact>]
let ``preset with unknown keys silently skips them`` () =
    let withUnknown =
        { Name = "unknown"
          CreatedAt = DateTimeOffset.UtcNow
          Values = Map.ofList [
              "no.such.key", PresetValue.FloatVal 99.0f
              "sizes.unitMarker", PresetValue.FloatVal 14.0f ] }
    let applied = StylePreset.applyToConfig withUnknown cfg
    Assert.Equal(14.0f, applied.UnitMarkerSize)

[<Fact>]
let ``load returns Error for missing file`` () =
    let result = StylePreset.load "does-not-exist-preset-xyz"
    match result with
    | Result.Error _ -> ()
    | Result.Ok _ -> Assert.Fail("Expected error for missing preset")

[<Fact>]
let ``save rejects invalid names`` () =
    let bad = StylePreset.fromConfig "bad/name" cfg
    match StylePreset.save bad with
    | Result.Error _ -> ()
    | Result.Ok _ -> Assert.Fail("Expected error for invalid preset name")

[<Fact>]
let ``listNames includes saved preset and excludes deleted`` () =
    let name = testPresetName "list"
    try
        let preset = StylePreset.fromConfig name cfg
        match StylePreset.save preset with
        | Result.Ok _ ->
            let names = StylePreset.listNames()
            Assert.Contains(name, names)
        | Result.Error msg -> Assert.Fail(sprintf "save failed: %s" msg)
    finally
        cleanup [name]
    let afterDelete = StylePreset.listNames()
    Assert.DoesNotContain(name, afterDelete)

[<Fact>]
let ``delete returns Error for missing preset`` () =
    match StylePreset.delete "no-such-preset-for-delete-test" with
    | Result.Error _ -> ()
    | Result.Ok _ -> Assert.Fail("Expected error for missing preset")

[<Fact>]
let ``color roundtrip preserves all 4 channels`` () =
    let name = testPresetName "color"
    try
        let cfg' =
            { cfg with
                BackgroundColor = SKColor(10uy, 200uy, 100uy, 250uy) }
        let preset = StylePreset.fromConfig name cfg'
        match StylePreset.save preset with
        | Result.Ok _ -> ()
        | Result.Error msg -> Assert.Fail(sprintf "save failed: %s" msg)
        match StylePreset.load name with
        | Result.Ok loaded ->
            let restored = StylePreset.applyToConfig loaded cfg
            Assert.Equal(10uy, restored.BackgroundColor.Red)
            Assert.Equal(200uy, restored.BackgroundColor.Green)
            Assert.Equal(100uy, restored.BackgroundColor.Blue)
            Assert.Equal(250uy, restored.BackgroundColor.Alpha)
        | Result.Error msg -> Assert.Fail(sprintf "load failed: %s" msg)
    finally
        cleanup [name]
