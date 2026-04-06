namespace FSBar.Viz

open System.Threading
open FSBar.Client

module MockSnapshot =

    let private nextId = ref 0

    let private genId () = Interlocked.Increment(nextId)

    let emptySnapshot (grid: MapGrid) : GameSnapshot =
        { FrameNumber = 0
          MapGrid = grid
          Units = Map.empty
          EventIndicators = []
          EconomyMetal = VizDefaults.defaultEconomy
          EconomyEnergy = VizDefaults.defaultEconomy
          MetalSpots = [||]
          Connected = true }

    let withUnits (units: UnitState list) (snapshot: GameSnapshot) : GameSnapshot =
        let m = units |> List.map (fun u -> u.UnitId, u) |> Map.ofList
        { snapshot with Units = m }

    let withFriendlyAt (pos: float32 * float32 * float32) (snapshot: GameSnapshot) : GameSnapshot =
        let (x, y, z) = pos
        let id = genId ()
        let u: UnitState =
            { UnitId = id
              PositionX = x
              PositionY = y
              PositionZ = z
              TeamId = 0
              DefId = 1
              Health = 1000.0f
              MaxHealth = 1000.0f
              IsEnemy = false }
        { snapshot with Units = snapshot.Units.Add(id, u) }

    let withEnemyAt (pos: float32 * float32 * float32) (snapshot: GameSnapshot) : GameSnapshot =
        let (x, y, z) = pos
        let id = genId ()
        let u: UnitState =
            { UnitId = id
              PositionX = x
              PositionY = y
              PositionZ = z
              TeamId = -1
              DefId = 1
              Health = 1000.0f
              MaxHealth = 1000.0f
              IsEnemy = true }
        { snapshot with Units = snapshot.Units.Add(id, u) }

    let withEvent (kind: EventKind) (pos: float32 * float32 * float32) (frame: int) (snapshot: GameSnapshot) : GameSnapshot =
        let (x, y, z) = pos
        let indicator: EventIndicator =
            { PositionX = x
              PositionY = y
              PositionZ = z
              Kind = kind
              FrameCreated = frame
              DurationFrames = 60 }
        { snapshot with EventIndicators = indicator :: snapshot.EventIndicators }

    let withEconomy (current: float32) (income: float32) (usage: float32) (storage: float32) (snapshot: GameSnapshot) : GameSnapshot =
        { snapshot with
            EconomyMetal =
                { Current = current
                  Income = income
                  Usage = usage
                  Storage = storage } }

    let withEnergyEconomy (current: float32) (income: float32) (usage: float32) (storage: float32) (snapshot: GameSnapshot) : GameSnapshot =
        { snapshot with
            EconomyEnergy =
                { Current = current
                  Income = income
                  Usage = usage
                  Storage = storage } }

    let withMetalSpots (spots: (float32 * float32 * float32 * float32) array) (snapshot: GameSnapshot) : GameSnapshot =
        { snapshot with MetalSpots = spots }

    let withFrame (frame: int) (snapshot: GameSnapshot) : GameSnapshot =
        { snapshot with FrameNumber = frame }
