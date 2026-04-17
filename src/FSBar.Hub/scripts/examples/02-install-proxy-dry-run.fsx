// src/FSBar.Hub/scripts/examples/02-install-proxy-dry-run.fsx
//
// Read-only sibling of the Settings tab's Install / Upgrade button.
// Runs `ProxyInstaller.checkStatus` + `health` and prints what
// `install` would do — without actually touching any file. Exits
// non-zero when the bundled proxy can't be resolved so callers
// can wire this into a pre-flight check.
//
// Usage:
//     dotnet fsi src/FSBar.Hub/scripts/examples/02-install-proxy-dry-run.fsx

#load "../prelude.fsx"

open System
open System.IO
open FSBar.Hub

// `BundledProxy.resolve()` walks up from AppContext.BaseDirectory —
// under `dotnet fsi` that points at the SDK's FSharp/ directory, so
// the assembly-relative fallback never reaches the repo's proxy/.
// Set the env var from this script's own location to make the
// resolver find the right bundle regardless of CWD.
if String.IsNullOrEmpty(Environment.GetEnvironmentVariable("FSBAR_HUB_BUNDLED_PROXY_DIR")) then
    // examples/ is 4 levels under the repo root
    let repoProxy =
        Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "..", "..", "proxy"))
    if Directory.Exists(repoProxy) then
        Environment.SetEnvironmentVariable("FSBAR_HUB_BUNDLED_PROXY_DIR", repoProxy)

let settings = HubSettings.load ()

match BarInstall.detect settings with
| Result.Error err ->
    eprintfn "✗ BAR install detection failed: %s" (BarInstall.formatError err)
    exit 1
| Ok install ->

match BundledProxy.resolve () with
| Result.Error err ->
    eprintfn "✗ bundled proxy not resolved: %s" (BundledProxy.formatError err)
    eprintfn "  (run scripts/refresh-bundled-proxy.sh <version> first)"
    exit 1
| Ok bundled ->

printfn "Active engine   : %s" install.ActiveEngine.Version
printfn "Bundled proxy   : %s (root %s)" bundled.Version bundled.BundleRoot

let status = ProxyInstaller.checkStatus install bundled
printfn ""
printfn "Current install status:"
printfn "  install path : %s" status.InstalledAtPath
printfn "  installed ver: %s"
    (status.InstalledVersion |> Option.defaultValue "(none)")
printfn "  files present: %b" status.AiFilesPresent
printfn "  devmode.txt  : %b" status.DevModeFilePresent
printfn "  simpleAiList : %b (false = proxy visible in Chobby)" status.SimpleAiListDisabled
printfn "  matches bundle: %b" status.MatchesBundled

printfn ""
printfn "Health: %s" (ProxyInstaller.formatHealth (ProxyInstaller.health status))

printfn ""
printfn "What `ProxyInstaller.install` would do:"
printfn "  CopyAiFiles        → %s"
    (if status.MatchesBundled then "Skipped (files byte-identical)"
     else "Performed (copy libSkirmishAI.so + AIInfo.lua + AIOptions.lua)")
printfn "  TouchDevMode       → %s"
    (if status.DevModeFilePresent then "Skipped"
     else sprintf "Performed (create %s/devmode.txt)" install.DataDir)
printfn "  ToggleSimpleAiList → %s"
    (if status.SimpleAiListDisabled then "Skipped"
     else "Performed (rewrite IGL_data.lua: simpleAiList=false)")
