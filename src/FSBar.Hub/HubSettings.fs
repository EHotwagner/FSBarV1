namespace FSBar.Hub

open System
open System.IO
open System.Text.Json

module HubSettings =

    type HubSettings = {
        BarDataDirOverride: string option
        EngineVersionOverride: string option
        GrpcPort: int
        LaunchGraphicalViewerDefault: bool
        StartPausedDefault: bool
        MaxRenderFrameSubscribers: int
        SchemaVersion: int
    }

    let defaults: HubSettings = {
        BarDataDirOverride = None
        EngineVersionOverride = None
        GrpcPort = 5021
        LaunchGraphicalViewerDefault = false
        StartPausedDefault = true
        MaxRenderFrameSubscribers = 8
        SchemaVersion = 2
    }

    let private minPort = 1024
    let private maxPort = 65535
    let private minRenderSubscribers = 1
    let private maxRenderSubscribers = 32

    let settingsPath () =
        let xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
        let configRoot =
            if String.IsNullOrWhiteSpace(xdgConfigHome) then
                let home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                Path.Combine(home, ".config")
            else
                xdgConfigHome
        Path.Combine(configRoot, "fsbar-hub", "settings.json")

    // Hand-rolled JSON round-trip. System.Text.Json's reflection-based
    // serializer does not pick up properties on F# module-nested DTO
    // types (they compile as internal-visible to the STJ reflection
    // walker even when declared at module scope), and CLIMutable F#
    // records trip the "no parameterless constructor" path on .NET 10.
    // Five fields is well under the threshold where a custom writer is
    // cheaper than importing FSharp.SystemTextJson.

    let private writeString (w: Utf8JsonWriter) (name: string) (value: string option) =
        match value with
        | Some v -> w.WriteString(name, v)
        | None -> ()

    let private serialize (s: HubSettings) : string =
        use stream = new MemoryStream()
        let writerOpts = JsonWriterOptions(Indented = true)
        use writer = new Utf8JsonWriter(stream, writerOpts)
        writer.WriteStartObject()
        writeString writer "barDataDirOverride" s.BarDataDirOverride
        writeString writer "engineVersionOverride" s.EngineVersionOverride
        writer.WriteNumber("grpcPort", s.GrpcPort)
        writer.WriteBoolean("launchGraphicalViewerDefault", s.LaunchGraphicalViewerDefault)
        writer.WriteBoolean("startPausedDefault", s.StartPausedDefault)
        writer.WriteNumber("maxRenderFrameSubscribers", s.MaxRenderFrameSubscribers)
        writer.WriteNumber("schemaVersion", s.SchemaVersion)
        writer.WriteEndObject()
        writer.Flush()
        System.Text.Encoding.UTF8.GetString(stream.ToArray())

    let private parseOptionalString (root: JsonElement) (name: string) : string option =
        match root.TryGetProperty(name) with
        | true, el when el.ValueKind = JsonValueKind.String ->
            let v = el.GetString()
            if String.IsNullOrEmpty(v) then None else Some v
        | _ -> None

    let private parseInt (root: JsonElement) (name: string) (fallback: int) : int =
        match root.TryGetProperty(name) with
        | true, el when el.ValueKind = JsonValueKind.Number ->
            let mutable v = 0
            if el.TryGetInt32(&v) then v else fallback
        | _ -> fallback

    let private parseBool (root: JsonElement) (name: string) (fallback: bool) : bool =
        match root.TryGetProperty(name) with
        | true, el when el.ValueKind = JsonValueKind.True || el.ValueKind = JsonValueKind.False ->
            el.GetBoolean()
        | _ -> fallback

    let private deserialize (json: string) : HubSettings =
        use doc = JsonDocument.Parse(json)
        let root = doc.RootElement
        if root.ValueKind <> JsonValueKind.Object then defaults
        else
            let rawPort = parseInt root "grpcPort" defaults.GrpcPort
            let port =
                if rawPort < minPort || rawPort > maxPort then
                    eprintfn
                        "[HubSettings] grpcPort=%d outside [%d, %d]; using default %d"
                        rawPort minPort maxPort defaults.GrpcPort
                    defaults.GrpcPort
                else
                    rawPort
            let schemaVersion = parseInt root "schemaVersion" defaults.SchemaVersion
            let rawRenderSubscribers =
                parseInt root "maxRenderFrameSubscribers" defaults.MaxRenderFrameSubscribers
            let renderSubscribers =
                // Missing field (v1 file) or out-of-range → clamp to default.
                if rawRenderSubscribers < minRenderSubscribers
                   || rawRenderSubscribers > maxRenderSubscribers then
                    defaults.MaxRenderFrameSubscribers
                else
                    rawRenderSubscribers
            { BarDataDirOverride = parseOptionalString root "barDataDirOverride"
              EngineVersionOverride = parseOptionalString root "engineVersionOverride"
              GrpcPort = port
              LaunchGraphicalViewerDefault = parseBool root "launchGraphicalViewerDefault" defaults.LaunchGraphicalViewerDefault
              StartPausedDefault = parseBool root "startPausedDefault" defaults.StartPausedDefault
              MaxRenderFrameSubscribers = renderSubscribers
              // v1 → v2 migration: any load below v2 is rewritten as v2 on
              // the next `save` (the record already carries the upgraded
              // value, and `serialize` always emits the current version).
              SchemaVersion =
                  if schemaVersion <= 0 then defaults.SchemaVersion
                  elif schemaVersion < 2 then defaults.SchemaVersion
                  else schemaVersion }

    let load () : HubSettings =
        let path = settingsPath ()
        if not (File.Exists(path)) then defaults
        else
            try
                let json = File.ReadAllText(path)
                deserialize json
            with ex ->
                eprintfn "[HubSettings] failed to read %s: %s — using defaults" path ex.Message
                defaults

    let save (settings: HubSettings) : Result<unit, string> =
        if settings.GrpcPort < minPort || settings.GrpcPort > maxPort then
            Error (sprintf "grpcPort=%d outside [%d, %d]" settings.GrpcPort minPort maxPort)
        elif settings.MaxRenderFrameSubscribers < minRenderSubscribers
             || settings.MaxRenderFrameSubscribers > maxRenderSubscribers then
            Error
                (sprintf "maxRenderFrameSubscribers=%d outside [%d, %d]"
                    settings.MaxRenderFrameSubscribers
                    minRenderSubscribers
                    maxRenderSubscribers)
        else
            let path = settingsPath ()
            try
                let dir = Path.GetDirectoryName(path)
                if not (String.IsNullOrEmpty(dir)) then
                    Directory.CreateDirectory(dir) |> ignore
                let json = serialize settings
                let tmp = path + ".tmp"
                File.WriteAllText(tmp, json)
                // File.Move with overwrite is the atomic-rename primitive on
                // POSIX; on .NET 10 it maps to rename(2).
                File.Move(tmp, path, overwrite = true)
                Ok ()
            with ex ->
                Error (sprintf "%s: %s" (ex.GetType().Name) ex.Message)

    let updateStartPausedDefault (settings: HubSettings) (value: bool) : HubSettings =
        { settings with StartPausedDefault = value }

    let updateLaunchGraphicalViewerDefault (settings: HubSettings) (value: bool) : HubSettings =
        { settings with LaunchGraphicalViewerDefault = value }

    let updateMaxRenderFrameSubscribers
        (settings: HubSettings)
        (value: int)
        : Result<HubSettings, string> =
        if value < minRenderSubscribers || value > maxRenderSubscribers then
            Error
                (sprintf "maxRenderFrameSubscribers=%d outside [%d, %d]"
                    value minRenderSubscribers maxRenderSubscribers)
        else
            Ok { settings with MaxRenderFrameSubscribers = value }
