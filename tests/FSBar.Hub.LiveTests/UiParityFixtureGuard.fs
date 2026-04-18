namespace FSBar.Hub.LiveTests

// Feature 041 T020 — fixture-guard helper for the UiParity live-test
// matrix (data-model §5 / R4). Tests call `skipIfMissing` at the top
// of their body with a list of `FixtureRequirement`s; missing
// fixtures raise `Xunit.SkipException` so the test counts as a SKIP
// rather than a FAIL — matching SC-004's "0 failures, N skips" budget.

open System
open System.IO
open Xunit
open FSBar.Client
open FSBar.Hub

[<RequireQualifiedAccess>]
type FixtureRequirement =
    | Engine
    | AiBinary of name: string
    | MapArchive of fileName: string

[<RequireQualifiedAccess>]
type FixtureCheck =
    | Available
    | Missing of FixtureRequirement list

module UiParityFixtureGuard =

    let private defaultDataDir =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local/state/Beyond All Reason")

    let private detectInstall () : Result<BarInstall.BarInstall, string> =
        if not (Directory.Exists(defaultDataDir)) then
            Result.Error (sprintf "BAR data dir not found at %s" defaultDataDir)
        else
            let settings =
                { HubSettings.defaults with BarDataDirOverride = Some defaultDataDir }
            BarInstall.detect settings
            |> Result.mapError BarInstall.formatError

    let private hasAi (install: BarInstall.BarInstall) (name: string) : bool =
        BarInstall.listSkirmishAis install.ActiveEngine
        |> List.contains name

    let private hasMap (install: BarInstall.BarInstall) (fileName: string) : bool =
        File.Exists(Path.Combine(install.DataDir, "maps", fileName))

    /// Check every requirement against the live filesystem; returns
    /// either `Available` or `Missing` with the list of unmet
    /// requirements (so the skip message can name every one).
    let check (requirements: FixtureRequirement list) : FixtureCheck =
        match detectInstall () with
        | Result.Error _ ->
            // No engine ⇒ everything is missing; collapse to a single
            // "Engine" requirement so the skip message stays short.
            FixtureCheck.Missing [ FixtureRequirement.Engine ]
        | Ok install ->
            let unmet =
                requirements
                |> List.filter (fun r ->
                    match r with
                    | FixtureRequirement.Engine ->
                        not install.ActiveEngine.HasHeadlessBin
                    | FixtureRequirement.AiBinary name ->
                        not (hasAi install name)
                    | FixtureRequirement.MapArchive fileName ->
                        not (hasMap install fileName))
            if List.isEmpty unmet then FixtureCheck.Available
            else FixtureCheck.Missing unmet

    let private describeRequirement (r: FixtureRequirement) : string =
        match r with
        | FixtureRequirement.Engine -> "engine (spring-headless)"
        | FixtureRequirement.AiBinary name -> sprintf "AI:%s" name
        | FixtureRequirement.MapArchive fileName -> sprintf "map:%s" fileName

    /// Raise `Xunit.SkipException` (via `Xunit.SkippableFact`) when any
    /// requirement is missing. Tests should call this at the top of
    /// their body before any other setup.
    let skipIfMissing (requirements: FixtureRequirement list) : unit =
        match check requirements with
        | FixtureCheck.Available -> ()
        | FixtureCheck.Missing missing ->
            let message =
                missing
                |> List.map describeRequirement
                |> String.concat ", "
            raise (Xunit.SkipException (sprintf "missing fixtures: %s" message))
