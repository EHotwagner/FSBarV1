namespace FSBar.Viz

open System.Threading
open FSBar.Client

module MockSnapshot =

    let private nextId = ref 0

    let private generateId () =
        Interlocked.Increment(nextId)

    let emptySnapshot (grid: MapGrid) : GameSnapshot =
        { FrameNumber = 0
          MapGrid = grid
          Units = Map.empty
          EventIndicators = []
          EconomyMetal = VizDefaults.defaultEconomy
          EconomyEnergy = VizDefaults.defaultEconomy
          MetalSpots = Array.empty
          Connected = true }

    let withUnits (units: UnitState list) (snapshot: GameSnapshot) : GameSnapshot =
        let unitsMap =
            units
            |> List.fold (fun acc u -> Map.add u.UnitId u acc) snapshot.Units
        { snapshot with Units = unitsMap }

    let withFriendlyAt (pos: float32 * float32 * float32) (snapshot: GameSnapshot) : GameSnapshot =
        let (x, y, z) = pos
        let id = generateId ()
        let unit: UnitState =
            { UnitId = id
              PositionX = x
              PositionY = y
              PositionZ = z
              TeamId = 0
              DefId = 1
              Health = 100.0f
              MaxHealth = 100.0f
              IsEnemy = false }
        { snapshot with Units = Map.add id unit snapshot.Units }

    let withEnemyAt (pos: float32 * float32 * float32) (snapshot: GameSnapshot) : GameSnapshot =
        let (x, y, z) = pos
        let id = generateId ()
        let unit: UnitState =
            { UnitId = id
              PositionX = x
              PositionY = y
              PositionZ = z
              TeamId = 1
              DefId = 1
              Health = 100.0f
              MaxHealth = 100.0f
              IsEnemy = true }
        { snapshot with Units = Map.add id unit snapshot.Units }

    let withEvent (kind: EventKind) (pos: float32 * float32 * float32) (frame: int) (snapshot: GameSnapshot) : GameSnapshot =
        let (x, y, z) = pos
        let evt: EventIndicator =
            { PositionX = x
              PositionY = y
              PositionZ = z
              Kind = kind
              FrameCreated = frame
              DurationFrames = 30 }
        { snapshot with EventIndicators = evt :: snapshot.EventIndicators }

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
