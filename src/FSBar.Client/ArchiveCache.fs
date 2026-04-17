namespace FSBar.Client

open System.IO
open System.Text.RegularExpressions

module ArchiveCache =

    type MapEntry = {
        ArchiveFileName: string
        FileStem: string
        EngineName: string
        NamePure: string
        Version: string option
    }

    let defaultCachePath (dataDir: string) : string =
        Path.Combine(dataDir, "cache", "ArchiveCache20.lua")

    let private rxQuoted (field: string) =
        Regex(sprintf "\\b%s\\s*=\\s*\"([^\"]*)\"" field, RegexOptions.Compiled)

    let private rxNameField = rxQuoted "name"
    let private rxNamePureField = rxQuoted "name_pure"
    let private rxVersionField = rxQuoted "version"
    let private rxModtype =
        Regex("\\bmodtype\\s*=\\s*(\\d+)", RegexOptions.Compiled)
    let private rxSd7Filename =
        Regex("\\bname\\s*=\\s*\"([^\"]+\\.sd7)\"", RegexOptions.Compiled)

    // Find each `archivedata = { ... }` block using brace-depth tracking
    // so nested tables (e.g. `depend = { ... }`) don't prematurely close
    // the match. Returns (adKeywordIndex, contentStart, contentEndExclusive)
    // — `contentEndExclusive` is the index of the matching `}`.
    let private archivedataBlocks (content: string) : (int * int * int) list =
        let marker = "archivedata = {"
        let results = ResizeArray<int * int * int>()
        let mutable cursor = 0
        let mutable keepGoing = true
        while keepGoing do
            let adIdx = content.IndexOf(marker, cursor)
            if adIdx < 0 then keepGoing <- false
            else
                let dataStart = adIdx + marker.Length
                let mutable depth = 1
                let mutable j = dataStart
                while depth > 0 && j < content.Length do
                    match content.[j] with
                    | '{' -> depth <- depth + 1
                    | '}' -> depth <- depth - 1
                    | _ -> ()
                    if depth > 0 then j <- j + 1
                if depth = 0 then
                    results.Add(adIdx, dataStart, j)
                    cursor <- j + 1
                else
                    keepGoing <- false
        List.ofSeq results

    let private extractField (rx: Regex) (data: string) : string option =
        let m = rx.Match(data)
        if m.Success then Some m.Groups.[1].Value else None

    let parse (content: string) : MapEntry list =
        let results = ResizeArray<MapEntry>()
        let mutable segmentStart = 0
        for (adIdx, dataStart, dataEnd) in archivedataBlocks content do
            let data = content.Substring(dataStart, dataEnd - dataStart)
            let mt = rxModtype.Match(data)
            if mt.Success && mt.Groups.[1].Value = "3" then
                // The outer archive entry's `name = "<filename>.sd7"` sits
                // before `archivedata = {`. Take the *last* `.sd7`-suffixed
                // quoted name in [segmentStart, adIdx) — that's the outer
                // entry's filename, not an earlier iteration's.
                let preceding = content.Substring(segmentStart, adIdx - segmentStart)
                let fnameMatches = rxSd7Filename.Matches(preceding)
                if fnameMatches.Count > 0 then
                    let fname = fnameMatches.[fnameMatches.Count - 1].Groups.[1].Value
                    match extractField rxNameField data with
                    | Some engineName when not (System.String.IsNullOrWhiteSpace engineName) ->
                        let namePure =
                            extractField rxNamePureField data
                            |> Option.defaultValue engineName
                        let version = extractField rxVersionField data
                        let stem = Path.GetFileNameWithoutExtension(fname)
                        results.Add(
                            { ArchiveFileName = fname
                              FileStem = stem
                              EngineName = engineName
                              NamePure = namePure
                              Version = version })
                    | _ -> ()
            segmentStart <- dataEnd + 1
        List.ofSeq results

    let loadMaps (cachePath: string) : MapEntry list =
        if not (File.Exists cachePath) then []
        else
            try parse (File.ReadAllText cachePath)
            with _ -> []

    let loadMapsForDataDir (dataDir: string) : MapEntry list =
        loadMaps (defaultCachePath dataDir)
