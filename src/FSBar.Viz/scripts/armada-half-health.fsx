// Show every Armada unit at 50% health in a SkiaViewer window.
// Usage (from repo root):
//   dotnet fsi src/FSBar.Viz/scripts/armada-half-health.fsx
// or load in the FSI MCP session.

#load "prelude.fsx"

open FSBar.Client
open FSBar.Viz
open FSBar.SyntheticData

let private armadaDefIds =
    [ UnitDefs.ArmCommander
      UnitDefs.ArmMex
      UnitDefs.ArmSolar
      UnitDefs.ArmWind
      UnitDefs.ArmLab
      UnitDefs.ArmPeewee
      UnitDefs.ArmFlash
      UnitDefs.ArmRockko
      UnitDefs.ArmSamson
      UnitDefs.ArmFark
      UnitDefs.ArmAdvLab
      UnitDefs.ArmZeus
      UnitDefs.ArmAnni
      UnitDefs.ArmStorage ]

let mapWidth = 2048.0f
let mapHeight = 2048.0f
let cache = UnitDefs.sceneC

let private cols = 5
let private spacing = 300.0f
let private originX = 400.0f
let private originZ = 400.0f

let private mkUnit (index: int) (defId: int) : int * UnitState =
    let col = index % cols
    let row = index / cols
    let x = originX + float32 col * spacing
    let z = originZ + float32 row * spacing
    let maxHp = UnitDefs.maxHealthFor defId cache
    let unit =
        { UnitId = index + 1
          PositionX = x
          PositionY = 0.0f
          PositionZ = z
          TeamId = 0
          DefId = defId
          Health = maxHp * 0.5f
          MaxHealth = maxHp
          IsEnemy = false }
    unit.UnitId, unit

let units = armadaDefIds |> List.mapi mkUnit |> Map.ofList

let w = int mapWidth / 8
let h = int mapHeight / 8
let grid : MapGrid =
    { WidthElmos = int mapWidth
      HeightElmos = int mapHeight
      WidthHeightmap = w
      HeightHeightmap = h
      HeightMap = Array2D.zeroCreate (h + 1) (w + 1)
      SlopeMap = Array2D.zeroCreate (h / 2) (w / 2)
      ResourceMap = Array2D.zeroCreate h w
      LosMap = Array2D.create h w 1
      RadarMap = Array2D.create h w 1 }

let zeroEcon : EconomyData =
    { Current = 0.0f; Income = 0.0f; Usage = 0.0f; Storage = 1000.0f }

let snapshot : GameSnapshot =
    { FrameNumber = 0
      MapGrid = grid
      Units = units
      EventIndicators = []
      EconomyMetal = zeroEcon
      EconomyEnergy = zeroEcon
      MetalSpots = [||]
      Connected = true }

printfn "Showing %d Armada units at 50%% health." (Map.count units)
for (_, u) in Map.toList units do
    printfn "  def=%2d  hp=%6.0f / %6.0f  pos=(%.0f, %.0f)"
        u.DefId u.Health u.MaxHealth u.PositionX u.PositionZ

let session = PreviewSession.startWithSnapshot snapshot
