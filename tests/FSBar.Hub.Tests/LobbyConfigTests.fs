module FSBar.Hub.Tests.LobbyConfigTests

open System
open System.IO
open Xunit
open FSBar.Hub
open FSBar.Hub.LobbyConfig

/// Synthetic BarInstall with controllable maps, engines, and AIs.
type private FakeInstall() =
    let tempDir =
        let p = Path.Combine(Path.GetTempPath(), "fsbar-lobby-test-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(p) |> ignore
        Directory.CreateDirectory(Path.Combine(p, "maps")) |> ignore
        p

    let mutable engineDir =
        let engDir = Path.Combine(tempDir, "engine", "recoil_2026.03.14")
        Directory.CreateDirectory(engDir) |> ignore
        // Make spring-headless executable so EngineDiscovery picks it up.
        let hb = Path.Combine(engDir, "spring-headless")
        File.WriteAllText(hb, "#!/bin/sh\nexit 0")
        File.SetUnixFileMode(
            hb,
            UnixFileMode.UserRead ||| UnixFileMode.UserWrite ||| UnixFileMode.UserExecute)
        engDir

    member _.DataDir = tempDir

    /// Create a fake <name>.sd7 in the data dir's maps/ subfolder.
    member _.AddMap(mapName: string) =
        let path = Path.Combine(tempDir, "maps", mapName + ".sd7")
        File.WriteAllText(path, "fake sd7")

    /// Create a fake skirmish AI directory.
    member _.AddAi(aiName: string) =
        let p = Path.Combine(engineDir, "AI", "Skirmish", aiName)
        Directory.CreateDirectory(p) |> ignore

    member _.AddGraphicalBinary() =
        let gb = Path.Combine(engineDir, "spring")
        File.WriteAllText(gb, "#!/bin/sh\nexit 0")
        File.SetUnixFileMode(
            gb,
            UnixFileMode.UserRead ||| UnixFileMode.UserWrite ||| UnixFileMode.UserExecute)

    member this.Resolve() =
        let settings = { HubSettings.defaults with BarDataDirOverride = Some this.DataDir }
        match BarInstall.detect settings with
        | Ok install -> install
        | Result.Error e -> failwith (BarInstall.formatError e)

    interface IDisposable with
        member _.Dispose() =
            try Directory.Delete(tempDir, recursive = true) with _ -> ()

/// Build a baseline LobbyConfig that should validate when the fixture
/// exposes the named map + HighBarV2 + BARb AIs.
let private happyLobby (mapName: string) =
    let armadaSeat =
        { Kind = AiSeat("HighBarV2", Map.empty); Side = "Armada"; Handicap = 0 }
    let cortexSeat =
        { Kind = AiSeat("BARb", Map.empty); Side = "Cortex"; Handicap = 0 }
    { MapName = mapName
      Mode = Skirmish
      EngineSpeed = 1.0f
      LaunchGraphicalViewer = false
      Teams =
        [ { Seats = [ armadaSeat ]; AllyTeamId = 0 }
          { Seats = [ cortexSeat ]; AllyTeamId = 1 } ]
      Spectators = [] }

[<Fact>]
let ``defaults fails validation when map is empty`` () =
    use fake = new FakeInstall()
    fake.AddAi("HighBarV2"); fake.AddAi("BARb")
    let install = fake.Resolve()
    match LobbyConfig.validate install LobbyConfig.defaults with
    | Result.Error errs ->
        Assert.Contains(errs, (fun e -> match e with MapNotInstalled _ -> true | _ -> false))
    | Ok _ -> Assert.Fail("defaults should not validate with empty map name")

[<Fact>]
let ``happy lobby validates against a complete install`` () =
    use fake = new FakeInstall()
    fake.AddMap("Avalanche 3.4")
    fake.AddAi("HighBarV2"); fake.AddAi("BARb")
    let install = fake.Resolve()
    match LobbyConfig.validate install (happyLobby "Avalanche 3.4") with
    | Ok _ -> ()
    | Result.Error errs ->
        Assert.Fail(sprintf "expected Ok; saw %A" (errs |> List.map LobbyConfig.formatError))

[<Fact>]
let ``validate returns every failure, not just the first`` () =
    use fake = new FakeInstall()
    fake.AddMap("Avalanche 3.4")
    // Deliberately do NOT add any AIs.
    let install = fake.Resolve()
    let lobby =
        let badSeat =
            { Kind = AiSeat("HighBarV2", Map.empty); Side = "Armada"; Handicap = 500 }
        { happyLobby "SomeOtherMap" with
            EngineSpeed = 0.0f
            Teams = [ { Seats = [ badSeat ]; AllyTeamId = 0 } ] }
    match LobbyConfig.validate install lobby with
    | Result.Error errs ->
        Assert.Contains(errs, function EngineSpeedOutOfRange _ -> true | _ -> false)
        Assert.Contains(errs, function MapNotInstalled _ -> true | _ -> false)
        Assert.Contains(errs, function NotEnoughTeams -> true | _ -> false)
        Assert.Contains(errs, function HandicapOutOfRange _ -> true | _ -> false)
        Assert.Contains(errs, function UnknownAi _ -> true | _ -> false)
    | Ok _ -> Assert.Fail("expected multiple validation errors")

[<Fact>]
let ``FFA with fewer than three teams fails`` () =
    use fake = new FakeInstall()
    fake.AddMap("Avalanche 3.4")
    fake.AddAi("HighBarV2"); fake.AddAi("BARb")
    let install = fake.Resolve()
    let lobby = { happyLobby "Avalanche 3.4" with Mode = FFA }
    match LobbyConfig.validate install lobby with
    | Result.Error errs ->
        Assert.Contains(errs, function FfaTooFewTeams -> true | _ -> false)
    | Ok _ -> Assert.Fail("FFA with 2 teams should fail")

[<Fact>]
let ``FFA with multi-seat team fails`` () =
    use fake = new FakeInstall()
    fake.AddMap("Avalanche 3.4")
    fake.AddAi("HighBarV2"); fake.AddAi("BARb"); fake.AddAi("NullAI")
    let install = fake.Resolve()
    let team0 =
        { Seats =
            [ { Kind = AiSeat("HighBarV2", Map.empty); Side = "Armada"; Handicap = 0 }
              { Kind = AiSeat("BARb", Map.empty); Side = "Armada"; Handicap = 0 } ]
          AllyTeamId = 0 }
    let team1 =
        { Seats = [ { Kind = AiSeat("NullAI", Map.empty); Side = "Cortex"; Handicap = 0 } ]
          AllyTeamId = 1 }
    let team2 =
        { Seats = [ { Kind = AiSeat("NullAI", Map.empty); Side = "Cortex"; Handicap = 0 } ]
          AllyTeamId = 2 }
    let lobby = { happyLobby "Avalanche 3.4" with Mode = FFA; Teams = [ team0; team1; team2 ] }
    match LobbyConfig.validate install lobby with
    | Result.Error errs ->
        Assert.Contains(errs, function FfaTeamHasMultipleSeats 0 -> true | _ -> false)
    | Ok _ -> Assert.Fail("FFA with multi-seat team should fail")

[<Fact>]
let ``LaunchGraphicalViewer without spring binary fails`` () =
    use fake = new FakeInstall()
    fake.AddMap("Avalanche 3.4")
    fake.AddAi("HighBarV2"); fake.AddAi("BARb")
    // Deliberately do NOT add a graphical binary.
    let install = fake.Resolve()
    let lobby = { happyLobby "Avalanche 3.4" with LaunchGraphicalViewer = true }
    match LobbyConfig.validate install lobby with
    | Result.Error errs ->
        Assert.Contains(errs, function GraphicalBinaryMissing _ -> true | _ -> false)
    | Ok _ -> Assert.Fail("missing graphical binary should fail when toggle is on")

[<Fact>]
let ``toEngineConfig happy path produces EngineConfig fields`` () =
    use fake = new FakeInstall()
    fake.AddMap("Avalanche 3.4")
    fake.AddAi("HighBarV2"); fake.AddAi("BARb")
    let install = fake.Resolve()
    let lobby = happyLobby "Avalanche 3.4"
    match LobbyConfig.toEngineConfig install lobby with
    | Ok ec ->
        Assert.Equal("Avalanche 3.4", ec.MapName)
        Assert.Equal("BARb", ec.OpponentAI)
        Assert.Equal("Cortex", ec.OpponentSide)
        Assert.Equal("Armada", ec.OurSide)
        Assert.Equal(1, ec.GameSpeed)
        Assert.Equal(Some install.DataDir, ec.SpringDataDir)
        Assert.StartsWith("/tmp/highbar-v2-", ec.SocketPath)
    | Result.Error err ->
        Assert.Fail(sprintf "toEngineConfig failed: %s" (LobbyConfig.formatError err))

[<Fact>]
let ``toEngineConfig rejects non-Skirmish modes`` () =
    use fake = new FakeInstall()
    let install = fake.Resolve()
    let lobby = { happyLobby "Any" with Mode = Team }
    match LobbyConfig.toEngineConfig install lobby with
    | Result.Error(AdapterUnsupportedShape _) -> ()
    | other -> Assert.Fail(sprintf "expected AdapterUnsupportedShape, got %A" other)

[<Fact>]
let ``toEngineConfig rejects >2 teams`` () =
    use fake = new FakeInstall()
    let install = fake.Resolve()
    let team =
        { Seats = [ { Kind = AiSeat("HighBarV2", Map.empty); Side = "Armada"; Handicap = 0 } ]
          AllyTeamId = 0 }
    let lobby = { happyLobby "Any" with Teams = [ team; team; team ] }
    match LobbyConfig.toEngineConfig install lobby with
    | Result.Error(AdapterUnsupportedShape _) -> ()
    | other -> Assert.Fail(sprintf "expected AdapterUnsupportedShape, got %A" other)

[<Fact>]
let ``toEngineConfig rejects team 0 non-HighBarV2 AI`` () =
    use fake = new FakeInstall()
    let install = fake.Resolve()
    let team0 =
        { Seats = [ { Kind = AiSeat("BARb", Map.empty); Side = "Cortex"; Handicap = 0 } ]
          AllyTeamId = 0 }
    let team1 =
        { Seats = [ { Kind = AiSeat("NullAI", Map.empty); Side = "Armada"; Handicap = 0 } ]
          AllyTeamId = 1 }
    let lobby = { happyLobby "Any" with Teams = [ team0; team1 ] }
    match LobbyConfig.toEngineConfig install lobby with
    | Result.Error(AdapterUnsupportedShape reason) ->
        Assert.Contains("HighBarV2", reason)
    | other -> Assert.Fail(sprintf "expected AdapterUnsupportedShape, got %A" other)
