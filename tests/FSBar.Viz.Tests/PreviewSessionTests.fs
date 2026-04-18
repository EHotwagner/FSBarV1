module FSBar.Viz.Tests.PreviewSessionTests

open System
open Xunit
open FSBar.Client
open FSBar.Viz
open FSBar.SyntheticData
open FSBar.Viz.Tests.VizEngineFixture

[<Fact>]
let ``startWithSnapshot opens and stops without exception`` () =
    let grid = SyntheticMapGrid.build {  width = 32; height = 32; seed = None  }
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withFriendlyAt (100.0f, 0.0f, 100.0f)
        |> MockSnapshot.withFriendlyAt (300.0f, 0.0f, 200.0f)
        |> MockSnapshot.withEnemyAt (500.0f, 0.0f, 400.0f)
        |> MockSnapshot.withEconomy 500.0f 10.0f 5.0f 1000.0f
        |> MockSnapshot.withEnergyEconomy 800.0f 20.0f 15.0f 1000.0f
    let config =
        { VizDefaults.defaultConfig with
            ActiveOverlays = Set.ofList [ OverlayKind.Units; OverlayKind.EconomyHud ] }
    GameViz.setConfig config
    use handle = PreviewSession.startWithSnapshot snap
    System.Threading.Thread.Sleep(2000)

[<Fact>]
let ``startWithMap works`` () =
    let grid = SyntheticMapGrid.build {  width = 64; height = 64; seed = None  }
    use handle = PreviewSession.startWithMap grid
    System.Threading.Thread.Sleep(2000)

[<Fact>]
let ``startPlayback with synthetic data runs and stops cleanly`` () =
    let synScene = FSBar.SyntheticData.Scenes.generate FSBar.SyntheticData.SceneId.SceneA
    let mapW = int synScene.MapWidth / 8
    let mapH = int synScene.MapHeight / 8
    let grid = SyntheticMapGrid.build {  width = mapW; height = mapH; seed = None  }
    let snapshots =
        synScene.Frames
        |> Array.map (fun gs ->
            let units =
                let friendlies =
                    gs.Units |> Map.toList |> List.map (fun (uid, u: TrackedUnit) ->
                        let (px, py, pz) = u.Position
                        uid, ({ UnitId = uid; PositionX = px; PositionY = py; PositionZ = pz
                                TeamId = 0; DefId = u.DefId; Health = u.Health
                                MaxHealth = u.MaxHealth; IsEnemy = false } : UnitState))
                let enemies =
                    gs.Enemies |> Map.toList |> List.map (fun (eid, e: TrackedEnemy) ->
                        let (px, py, pz) = e.Position
                        eid, ({ UnitId = eid; PositionX = px; PositionY = py; PositionZ = pz
                                TeamId = 1; DefId = (e.DefId |> Option.defaultValue 0)
                                Health = (e.Health |> Option.defaultValue 100.0f)
                                MaxHealth = 100.0f; IsEnemy = true } : UnitState))
                (friendlies @ enemies) |> Map.ofList
            { FrameNumber = int gs.FrameNumber; MapGrid = grid; Units = units
              DisplayUnits = Map.empty; EventIndicators = []
              EconomyMetal = { Current = gs.Metal.Current; Income = gs.Metal.Income; Usage = gs.Metal.Usage; Storage = gs.Metal.Storage }
              EconomyEnergy = { Current = gs.Energy.Current; Income = gs.Energy.Income; Usage = gs.Energy.Usage; Storage = gs.Energy.Storage }
              MetalSpots = [||]; Connected = true })
    use handle = PreviewSession.startPlayback snapshots 30
    // Let playback run for 3 seconds — should render several frames visibly
    System.Threading.Thread.Sleep(3000)
