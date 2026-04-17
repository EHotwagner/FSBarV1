namespace FSBar.Viz

open FSBar.Client
open FSBar.SyntheticData

module SyntheticDataAdapter =

    // Classification table for the known synthetic DefIds. Derived from the
    // hardcoded list in FSBar.SyntheticData.UnitDefs.
    //   (defId, shape, faction, tier, footprintElmo)
    let classTable : Map<int, MovementShape * FactionId * Tier * float32> =
        Map.ofList [
            // Armada (faction id 1)
            UnitDefs.ArmCommander, (MovementShape.Bot,      FactionId.Armada, Tier.T3, 48.0f)
            UnitDefs.ArmMex,       (MovementShape.Building, FactionId.Armada, Tier.T1, 32.0f)
            UnitDefs.ArmSolar,     (MovementShape.Building, FactionId.Armada, Tier.T1, 32.0f)
            UnitDefs.ArmWind,      (MovementShape.Building, FactionId.Armada, Tier.T1, 32.0f)
            UnitDefs.ArmLab,       (MovementShape.Building, FactionId.Armada, Tier.T1, 64.0f)
            UnitDefs.ArmAdvLab,    (MovementShape.Building, FactionId.Armada, Tier.T2, 80.0f)
            UnitDefs.ArmStorage,   (MovementShape.Building, FactionId.Armada, Tier.T1, 32.0f)
            UnitDefs.ArmPeewee,    (MovementShape.Bot,      FactionId.Armada, Tier.T1, 20.0f)
            UnitDefs.ArmFlash,     (MovementShape.Vehicle,  FactionId.Armada, Tier.T1, 32.0f)
            UnitDefs.ArmRockko,    (MovementShape.Bot,      FactionId.Armada, Tier.T1, 24.0f)
            UnitDefs.ArmSamson,    (MovementShape.Vehicle,  FactionId.Armada, Tier.T1, 32.0f)
            UnitDefs.ArmFark,      (MovementShape.Bot,      FactionId.Armada, Tier.T1, 24.0f)
            UnitDefs.ArmZeus,      (MovementShape.Bot,      FactionId.Armada, Tier.T2, 40.0f)
            UnitDefs.ArmAnni,      (MovementShape.Building, FactionId.Armada, Tier.T2, 40.0f)
            // Cortex
            UnitDefs.CorCommander, (MovementShape.Bot,      FactionId.Cortex, Tier.T3, 48.0f)
            UnitDefs.CorLab,       (MovementShape.Building, FactionId.Cortex, Tier.T1, 64.0f)
            UnitDefs.CorAdvLab,    (MovementShape.Building, FactionId.Cortex, Tier.T2, 80.0f)
            UnitDefs.CorStorage,   (MovementShape.Building, FactionId.Cortex, Tier.T1, 32.0f)
            UnitDefs.CorGator,     (MovementShape.Vehicle,  FactionId.Cortex, Tier.T1, 32.0f)
            UnitDefs.CorThud,      (MovementShape.Bot,      FactionId.Cortex, Tier.T1, 24.0f)
            UnitDefs.CorStorm,     (MovementShape.Bot,      FactionId.Cortex, Tier.T1, 24.0f)
            UnitDefs.CorSumo,      (MovementShape.Bot,      FactionId.Cortex, Tier.T2, 48.0f)
            UnitDefs.CorGoliath,   (MovementShape.Vehicle,  FactionId.Cortex, Tier.T2, 64.0f)
        ]

    let defaultStatus : StatusFlags =
        { IsUnderConstruction = false
          IsStunned = false
          JustDamagedWithinMs = None
          JustCompletedWithinMs = None
          IsCloaked = false }

    let deriveHeading (unitId: int) (frame: int) : float32 =
        // Deterministic gentle rotation keyed on id+frame.
        let cycleLen = 300.0f
        let base' = float32 ((unitId * 37) % 360) * float32 (System.Math.PI) / 180.0f
        let spin = (float32 frame / cycleLen) * float32 (2.0 * System.Math.PI)
        base' + spin * 0.05f

    let fromTrackedUnit (scene: Scene) (frame: int) (unit': TrackedUnit) : UnitDisplay =
        let u = unit'
        let shape, faction, tier, footprint =
            match Map.tryFind u.DefId classTable with
            | Some t -> t
            | None -> MovementShape.Bot, FactionId.Neutral, Tier.T1, 24.0f
        let name =
            match UnitDefCache.tryFindById scene.UnitDefs u.DefId with
            | Some info -> info.Name
            | None -> sprintf "def%d" u.DefId
        let buildProgress = if u.IsFinished then 1.0f else 0.5f
        let px, py, pz = u.Position
        { UnitId = u.UnitId
          DefId = u.DefId
          InternalName = name
          Shape = shape
          Faction = faction
          Tier = tier
          LabelCode = UnitLabels.lookupOrFallback name
          FootprintWidthElmo = footprint
          FootprintHeightElmo = footprint
          TeamId =
              match faction with
              | FactionId.Armada -> 0
              | _ -> 1
          PositionX = px
          PositionY = py
          PositionZ = pz
          HeadingRadians = deriveHeading u.UnitId frame
          CurrentHealth = u.Health
          MaxHealth = u.MaxHealth
          BuildProgress = buildProgress
          Status =
              if u.IsFinished then defaultStatus
              else { defaultStatus with IsUnderConstruction = true }
          WeaponRangesElmo =
              match UnitDefCache.tryFindById scene.UnitDefs u.DefId with
              | Some info when info.MaxWeaponRange > 0.0f -> [ info.MaxWeaponRange ]
              | _ -> []
          SightRangeElmo = 400.0f
          BuildRangeElmo = None
          CommandQueue = [] }

    let toUnitDisplays (scene: Scene) (frame: GameState) : UnitDisplay seq =
        let frameIdx = int frame.FrameNumber
        frame.Units
        |> Map.toSeq
        |> Seq.map (fun (_, tu) -> fromTrackedUnit scene frameIdx tu)
