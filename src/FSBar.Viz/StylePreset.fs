namespace FSBar.Viz

open System
open System.IO
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
open SkiaSharp

[<RequireQualifiedAccess>]
type PresetValue =
    | ColorVal of argb: uint32
    | FloatVal of v: float32
    | IntVal of v: int
    | BoolVal of v: bool
    | StringVal of v: string
    | StringSetVal of vs: Set<string>

type StylePreset =
    { Name: string
      CreatedAt: System.DateTimeOffset
      Values: Map<string, PresetValue> }

module StylePreset =

    let presetDirectory : string =
        // Walk up from the assembly location to find the repo root (a parent
        // containing a `viz-presets` folder). Fall back to CWD/viz-presets.
        let rec findRoot (dir: DirectoryInfo) =
            if isNull dir then None
            elif Directory.Exists(Path.Combine(dir.FullName, "viz-presets")) then
                Some (Path.Combine(dir.FullName, "viz-presets"))
            else findRoot dir.Parent
        let start =
            try
                let asmPath = System.Reflection.Assembly.GetExecutingAssembly().Location
                DirectoryInfo(Path.GetDirectoryName asmPath)
            with _ -> DirectoryInfo(Environment.CurrentDirectory)
        match findRoot start with
        | Some p -> p
        | None -> Path.Combine(Environment.CurrentDirectory, "viz-presets")

    let private nameRegex = Regex(@"^[A-Za-z0-9 _\-]+$", RegexOptions.Compiled)

    let isValidName (name: string) : bool =
        not (String.IsNullOrWhiteSpace name) && nameRegex.IsMatch name

    let private ensureDir () =
        if not (Directory.Exists presetDirectory) then
            Directory.CreateDirectory presetDirectory |> ignore

    let private filePath (name: string) =
        Path.Combine(presetDirectory, name + ".json")

    // --- Value ↔ PresetValue --------------------------------------------

    let private presetOfObj (o: obj) : PresetValue option =
        match o with
        | :? SKColor as c ->
            let a = uint32 c.Alpha
            let r = uint32 c.Red
            let g = uint32 c.Green
            let b = uint32 c.Blue
            Some (PresetValue.ColorVal((a <<< 24) ||| (r <<< 16) ||| (g <<< 8) ||| b))
        | :? float32 as f -> Some (PresetValue.FloatVal f)
        | :? int as i -> Some (PresetValue.IntVal i)
        | :? bool as b -> Some (PresetValue.BoolVal b)
        | :? string as s -> Some (PresetValue.StringVal s)
        | :? Set<string> as s -> Some (PresetValue.StringSetVal s)
        | _ -> None

    let private objOfPreset (p: PresetValue) : obj =
        match p with
        | PresetValue.ColorVal argb ->
            let a = byte ((argb >>> 24) &&& 0xFFu)
            let r = byte ((argb >>> 16) &&& 0xFFu)
            let g = byte ((argb >>> 8) &&& 0xFFu)
            let b = byte (argb &&& 0xFFu)
            box (SKColor(r, g, b, a))
        | PresetValue.FloatVal v -> box v
        | PresetValue.IntVal v -> box v
        | PresetValue.BoolVal v -> box v
        | PresetValue.StringVal v -> box v
        | PresetValue.StringSetVal v -> box v

    // --- JSON serialization ---------------------------------------------

    let private colorToHex (argb: uint32) : string =
        sprintf "#%08X" argb

    let private colorFromHex (s: string) : uint32 option =
        let s = s.Trim().TrimStart('#')
        match s.Length with
        | 8 ->
            match UInt32.TryParse(s, Globalization.NumberStyles.HexNumber, Globalization.CultureInfo.InvariantCulture) with
            | true, v -> Some v
            | _ -> None
        | 6 ->
            match UInt32.TryParse(s, Globalization.NumberStyles.HexNumber, Globalization.CultureInfo.InvariantCulture) with
            | true, v -> Some (0xFF000000u ||| v)
            | _ -> None
        | _ -> None

    let private serializeValue (writer: Utf8JsonWriter) (key: string) (v: PresetValue) =
        match v with
        | PresetValue.ColorVal argb -> writer.WriteString(key, colorToHex argb)
        | PresetValue.FloatVal f ->
            // Force a decimal point so the value roundtrips as a float,
            // not an integer, regardless of whether f is whole-valued.
            let s =
                let raw = (float f).ToString("R", Globalization.CultureInfo.InvariantCulture)
                if raw.Contains('.') || raw.Contains('e') || raw.Contains('E') then raw
                else raw + ".0"
            writer.WritePropertyName(key)
            writer.WriteRawValue(s, skipInputValidation = false)
        | PresetValue.IntVal i -> writer.WriteNumber(key, i)
        | PresetValue.BoolVal b -> writer.WriteBoolean(key, b)
        | PresetValue.StringVal s -> writer.WriteString(key, s)
        | PresetValue.StringSetVal vs ->
            writer.WriteStartArray(key)
            for s in vs do writer.WriteStringValue(s)
            writer.WriteEndArray()

    let private deserializeValue (el: JsonElement) : PresetValue option =
        match el.ValueKind with
        | JsonValueKind.String ->
            let s = el.GetString()
            match colorFromHex s with
            | Some argb -> Some (PresetValue.ColorVal argb)
            | None -> Some (PresetValue.StringVal s)
        | JsonValueKind.Number ->
            // Distinguish integer from float by looking at the raw JSON text.
            // System.Text.Json parses "9" and "9.0" both as valid int32, but
            // we want to preserve the author's intent so presets roundtrip.
            let raw = el.GetRawText()
            let looksFloat =
                raw.Contains('.') || raw.Contains('e') || raw.Contains('E')
            if looksFloat then
                match el.TryGetDouble() with
                | true, d -> Some (PresetValue.FloatVal (float32 d))
                | _ -> None
            else
                match el.TryGetInt32() with
                | true, i -> Some (PresetValue.IntVal i)
                | _ ->
                    match el.TryGetDouble() with
                    | true, d -> Some (PresetValue.FloatVal (float32 d))
                    | _ -> None
        | JsonValueKind.True -> Some (PresetValue.BoolVal true)
        | JsonValueKind.False -> Some (PresetValue.BoolVal false)
        | JsonValueKind.Array ->
            let items =
                el.EnumerateArray()
                |> Seq.choose (fun x ->
                    if x.ValueKind = JsonValueKind.String then Some (x.GetString())
                    else None)
                |> Set.ofSeq
            Some (PresetValue.StringSetVal items)
        | _ -> None

    let private writePreset (preset: StylePreset) : string =
        use ms = new MemoryStream()
        let opts = JsonWriterOptions(Indented = true)
        do
            use writer = new Utf8JsonWriter(ms, opts)
            writer.WriteStartObject()
            writer.WriteString("name", preset.Name)
            writer.WriteString("createdAt", preset.CreatedAt.ToString("o"))
            writer.WriteStartObject("values")
            for KeyValue(k, v) in preset.Values do
                serializeValue writer k v
            writer.WriteEndObject()
            writer.WriteEndObject()
        Encoding.UTF8.GetString(ms.ToArray())

    let private readPreset (json: string) : Result<StylePreset, string> =
        try
            use doc = JsonDocument.Parse(json)
            let root = doc.RootElement
            let name =
                match root.TryGetProperty("name") with
                | true, p when p.ValueKind = JsonValueKind.String -> p.GetString()
                | _ -> ""
            let createdAt =
                match root.TryGetProperty("createdAt") with
                | true, p when p.ValueKind = JsonValueKind.String ->
                    match DateTimeOffset.TryParse(p.GetString()) with
                    | true, dt -> dt
                    | _ -> DateTimeOffset.UtcNow
                | _ -> DateTimeOffset.UtcNow
            let values =
                match root.TryGetProperty("values") with
                | true, p when p.ValueKind = JsonValueKind.Object ->
                    p.EnumerateObject()
                    |> Seq.choose (fun prop ->
                        match deserializeValue prop.Value with
                        | Some v -> Some (prop.Name, v)
                        | None -> None)
                    |> Map.ofSeq
                | _ -> Map.empty
            Ok { Name = name; CreatedAt = createdAt; Values = values }
        with ex ->
            Error (sprintf "Failed to parse preset JSON: %s" ex.Message)

    // --- Public API -----------------------------------------------------

    let fromConfig (name: string) (config: VizConfig) : StylePreset =
        let values =
            ConfigDescriptors.all
            |> List.choose (fun d ->
                match presetOfObj (d.Get config) with
                | Some pv -> Some (d.Key, pv)
                | None -> None)
            |> Map.ofList
        { Name = name
          CreatedAt = DateTimeOffset.UtcNow
          Values = values }

    let applyToConfig (preset: StylePreset) (config: VizConfig) : VizConfig =
        preset.Values
        |> Map.fold (fun cfg key pv ->
            match ConfigDescriptors.tryFind key with
            | Some d ->
                try d.Set (objOfPreset pv) cfg
                with _ -> cfg
            | None -> cfg) config

    let save (preset: StylePreset) : Result<string, string> =
        if not (isValidName preset.Name) then
            Error (sprintf "Invalid preset name: '%s'. Must contain only letters, digits, spaces, hyphens, underscores." preset.Name)
        else
            try
                ensureDir()
                let path = filePath preset.Name
                let json = writePreset preset
                File.WriteAllText(path, json)
                Ok path
            with ex ->
                Error (sprintf "Failed to save preset '%s': %s" preset.Name ex.Message)

    let load (name: string) : Result<StylePreset, string> =
        if not (isValidName name) then
            Error (sprintf "Invalid preset name: '%s'" name)
        else
            let path = filePath name
            if not (File.Exists path) then
                Error (sprintf "Preset not found: %s" path)
            else
                try
                    let json = File.ReadAllText(path)
                    readPreset json
                with ex ->
                    Error (sprintf "Failed to load preset '%s': %s" name ex.Message)

    let listNames () : string list =
        if not (Directory.Exists presetDirectory) then []
        else
            Directory.EnumerateFiles(presetDirectory, "*.json")
            |> Seq.map Path.GetFileNameWithoutExtension
            |> Seq.sort
            |> Seq.toList

    let delete (name: string) : Result<unit, string> =
        if not (isValidName name) then
            Error (sprintf "Invalid preset name: '%s'" name)
        else
            let path = filePath name
            if not (File.Exists path) then
                Error (sprintf "Preset not found: %s" path)
            else
                try File.Delete path; Ok ()
                with ex ->
                    Error (sprintf "Failed to delete preset '%s': %s" name ex.Message)
