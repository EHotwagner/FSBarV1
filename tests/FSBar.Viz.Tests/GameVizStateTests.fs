module FSBar.Viz.Tests.GameVizStateTests

open System
open Xunit
open FSBar.Client
open FSBar.Viz

let minimalMapGrid =
    { WidthElmos = 8192; HeightElmos = 8192; WidthHeightmap = 129; HeightHeightmap = 129
      HeightMap = Array2D.zeroCreate 129 129; SlopeMap = Array2D.zeroCreate 129 129
      ResourceMap = Array2D.zeroCreate 129 129; LosMap = Array2D.zeroCreate 129 129
      RadarMap = Array2D.zeroCreate 129 129 }

let makeGameState (units: (int * TrackedUnit) list) (enemies: (int * TrackedEnemy) list) (events: GameEvent list) (metal: EconomySnapshot) (energy: EconomySnapshot) frameNum teamId =
    { FrameNumber = frameNum
      TeamId = teamId
      Units = units |> Map.ofList
      Enemies = enemies |> Map.ofList
      Metal = metal
      Energy = energy
      UnitDefs = UnitDefCache.empty
      Events = events }

let defaultEcon : EconomySnapshot =
    { Current = 0.0f; Income = 0.0f; Usage = 0.0f; Storage = 0.0f }

[<Fact>]
let ``attachWithState and onFrameWithState with units`` () =
    GameViz.start None
    try
        GameViz.attachWithState minimalMapGrid [||] 0

        let units =
            [ 1, { UnitId = 1; DefId = 100; Position = (1000.0f, 50.0f, 2000.0f); Health = 1000.0f; MaxHealth = 1000.0f; IsFinished = true; IsIdle = false }
              2, { UnitId = 2; DefId = 101; Position = (3000.0f, 50.0f, 4000.0f); Health = 500.0f; MaxHealth = 800.0f; IsFinished = false; IsIdle = false }
              3, { UnitId = 3; DefId = 102; Position = (5000.0f, 50.0f, 6000.0f); Health = 200.0f; MaxHealth = 200.0f; IsFinished = true; IsIdle = true } ]

        let metal : EconomySnapshot = { Current = 500.0f; Income = 10.0f; Usage = 5.0f; Storage = 1000.0f }
        let energy : EconomySnapshot = { Current = 800.0f; Income = 20.0f; Usage = 15.0f; Storage = 2000.0f }

        let destroyedEvent = GameEvent.UnitDestroyed(99, None)

        let gs = makeGameState units [] [ destroyedEvent ] metal energy 100u 0

        GameViz.onFrameWithState gs minimalMapGrid

        // Allow a frame tick to process the snapshot
        System.Threading.Thread.Sleep(200)
    finally
        GameViz.stop ()

[<Fact>]
let ``onFrameWithState with empty units does not throw`` () =
    GameViz.start None
    try
        GameViz.attachWithState minimalMapGrid [||] 0
        let gs = makeGameState [] [] [] defaultEcon defaultEcon 1u 0
        GameViz.onFrameWithState gs minimalMapGrid
        System.Threading.Thread.Sleep(100)
    finally
        GameViz.stop ()

[<Fact>]
let ``onFrameWithState with enemies in LOS`` () =
    GameViz.start None
    try
        GameViz.attachWithState minimalMapGrid [||] 0

        let enemies =
            [ 10, { EnemyId = 10; DefId = Some 200; Position = (4000.0f, 50.0f, 4000.0f); Health = Some 500.0f; InLOS = true; InRadar = false }
              11, { EnemyId = 11; DefId = None; Position = (6000.0f, 50.0f, 6000.0f); Health = None; InLOS = false; InRadar = true } ]

        let gs = makeGameState [] enemies [GameEvent.EnemyEnterLOS 10] defaultEcon defaultEcon 50u 0

        GameViz.onFrameWithState gs minimalMapGrid
        System.Threading.Thread.Sleep(100)
    finally
        GameViz.stop ()
