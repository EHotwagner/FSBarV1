namespace FSBar.Hub

open System
open System.IO
open System.Reflection

module BundledProxy =

    type BundledProxyInfo = {
        Version: string
        BundleRoot: string
        LibSkirmishAiPath: string
        AiInfoLuaPath: string
        AiOptionsLuaPath: string
    }

    type BundledProxyError =
        | VersionFileMissing of path: string
        | VersionFileMalformed of path: string
        | BundleDirMissing of path: string
        | RequiredFileMissing of path: string

    let formatError (err: BundledProxyError) =
        match err with
        | VersionFileMissing p -> sprintf "BUNDLED_VERSION file not found at %s" p
        | VersionFileMalformed p -> sprintf "BUNDLED_VERSION at %s is empty or multi-line" p
        | BundleDirMissing p -> sprintf "bundled proxy directory missing: %s" p
        | RequiredFileMissing p -> sprintf "required bundled file missing: %s" p

    // Return the proxy/ root the caller should read from. Tries the
    // env var first, then walks up from the running assembly location
    // looking for a proxy/ directory — matches how installed builds
    // ship: the hub exe lands somewhere like .../src/FSBar.Hub.App/bin/...
    // during development and `<install-prefix>/fsbar-hub/bin/` once
    // packaged, and in both cases `proxy/` is reachable by ascending.
    let private candidateRoots () =
        seq {
            match Environment.GetEnvironmentVariable("FSBAR_HUB_BUNDLED_PROXY_DIR") with
            | null | "" -> ()
            | path -> yield path

            let baseDir = AppContext.BaseDirectory
            let rec walk (dir: string) =
                seq {
                    if not (String.IsNullOrEmpty(dir)) then
                        yield Path.Combine(dir, "proxy")
                        let parent = Directory.GetParent(dir)
                        if not (isNull parent) then
                            yield! walk parent.FullName
                }
            // Cap the walk at 8 levels so a misconfigured layout fails
            // fast instead of scanning the whole filesystem.
            yield! walk baseDir |> Seq.truncate 8
        }

    let private selectRoot () =
        candidateRoots ()
        |> Seq.tryFind (fun dir ->
            File.Exists(Path.Combine(dir, "BUNDLED_VERSION")))

    let private readVersion (rootDir: string) =
        let versionFile = Path.Combine(rootDir, "BUNDLED_VERSION")
        if not (File.Exists(versionFile)) then
            Error (VersionFileMissing versionFile)
        else
            let raw = File.ReadAllText(versionFile)
            let nonBlank =
                raw.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
                |> Array.map (fun line -> line.Trim())
                |> Array.filter (fun line -> line.Length > 0)
            match nonBlank with
            | [| single |] -> Ok single
            | _ -> Error (VersionFileMalformed versionFile)

    let resolve () : Result<BundledProxyInfo, BundledProxyError> =
        let rootOpt =
            // When the env var is set but points at a missing file we want
            // the error to name that path, not fall through silently. Try
            // the env var explicitly first.
            match Environment.GetEnvironmentVariable("FSBAR_HUB_BUNDLED_PROXY_DIR") with
            | null | "" -> selectRoot ()
            | envPath -> Some envPath

        match rootOpt with
        | None ->
            // No BUNDLED_VERSION anywhere we looked — name the place the
            // assembly-relative walk started so the operator can see
            // where we expected to find proxy/.
            Error (VersionFileMissing (Path.Combine(AppContext.BaseDirectory, "proxy", "BUNDLED_VERSION")))
        | Some rootDir ->
            match readVersion rootDir with
            | Error e -> Error e
            | Ok version ->
                let bundleDir = Path.Combine(rootDir, "bundled", version)
                if not (Directory.Exists(bundleDir)) then
                    Error (BundleDirMissing bundleDir)
                else
                    let libPath = Path.Combine(bundleDir, "libSkirmishAI.so")
                    let aiInfoPath = Path.Combine(bundleDir, "AIInfo.lua")
                    let aiOptionsPath = Path.Combine(bundleDir, "AIOptions.lua")
                    let required = [ libPath; aiInfoPath; aiOptionsPath ]
                    match required |> List.tryFind (File.Exists >> not) with
                    | Some missing -> Error (RequiredFileMissing missing)
                    | None ->
                        Ok { Version = version
                             BundleRoot = bundleDir
                             LibSkirmishAiPath = libPath
                             AiInfoLuaPath = aiInfoPath
                             AiOptionsLuaPath = aiOptionsPath }
