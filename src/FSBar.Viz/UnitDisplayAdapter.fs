namespace FSBar.Viz

open FSBar.Client

module UnitDisplayAdapter =

    type private DefProps =
        { InternalName: string
          Shape: MovementShape
          Faction: FactionId
          Tier: Tier
          LabelCode: string
          FootprintW: float32
          FootprintH: float32
          WeaponRanges: float32 list
          SightRange: float32 }

    let private barDataByName =
        lazy (
            BarData.AllUnitDefs.all
            |> List.map (fun (_, _, d: BarData.UnitDef) -> d.name, d)
            |> Map.ofList)

    let private concreteFloat (v: BarData.ValueOrExpr<float>) (fallback: float) : float =
        match v with
        | BarData.ValueOrExpr.Concrete x -> x
        | _ -> fallback

    let private fallbackProps (name: string) : DefProps =
        { InternalName = name
          Shape = MovementShape.Bot
          Faction = FactionId.Neutral
          Tier = Tier.T1
          LabelCode = if System.String.IsNullOrEmpty name then "??" else UnitLabels.lookupOrFallback name
          FootprintW = 32.0f
          FootprintH = 32.0f
          WeaponRanges = []
          SightRange = 0.0f }

    let private resolveFromBarData (name: string) : DefProps =
        match Map.tryFind name barDataByName.Value with
        | None -> fallbackProps name
        | Some d ->
            // Encyclopedia's "canMove" derivation (feature 038 FR-002):
            // unify on `canFly || movementClass <> None` so live and
            // encyclopedia glyphs classify identically.
            let canMove =
                match d.movement with
                | Some m -> m.canFly || (m.movementClass <> None)
                | None -> false
            let canFly =
                match d.movement with
                | Some m -> m.canFly
                | None -> false
            let mClass =
                match d.movement with
                | Some m -> m.movementClass
                | None -> None
            let weaponRanges =
                match d.weapons with
                | Some ws ->
                    ws
                    |> List.choose (fun w ->
                        match w.range with
                        | Some r -> Some (float32 (concreteFloat r 0.0))
                        | None -> None)
                    |> List.filter (fun r -> r > 0.0f)
                | None -> []
            { InternalName = name
              Shape = UnitGlyph.classifyShape canMove canFly mClass ignore
              Faction = UnitGlyph.classifyFaction d.subfolder d.name ignore
              Tier = UnitGlyph.classifyTier d.customParams d.category ignore
              LabelCode = UnitLabels.lookupOrFallback name
              FootprintW = float32 d.footprintX * 16.0f
              FootprintH = float32 d.footprintZ * 16.0f
              WeaponRanges = weaponRanges
              SightRange = float32 (concreteFloat d.sightDistance 0.0) }

    let private resolveDef (cache: UnitDefCache) (defId: int) : DefProps =
        match UnitDefCache.tryFindById cache defId with
        | Some info -> resolveFromBarData info.Name
        | None -> fallbackProps (sprintf "def%d" defId)

    let private defaultStatus : StatusFlags =
        { IsUnderConstruction = false
          IsStunned = false
          JustDamagedWithinMs = None
          JustCompletedWithinMs = None
          IsCloaked = false }

    let ofTrackedUnit
            (defCache: UnitDefCache)
            (teamId: int)
            (unitId: int)
            (unit: TrackedUnit)
            : UnitDisplay =
        let props = resolveDef defCache unit.DefId
        let (px, py, pz) = unit.Position
        let isUnfinished = not unit.IsFinished
        { UnitId = unitId
          DefId = unit.DefId
          InternalName = props.InternalName
          Shape = props.Shape
          Faction = props.Faction
          Tier = props.Tier
          LabelCode = props.LabelCode
          FootprintWidthElmo = props.FootprintW
          FootprintHeightElmo = props.FootprintH
          TeamId = teamId
          PositionX = px
          PositionY = py
          PositionZ = pz
          HeadingRadians = 0.0f
          CurrentHealth = unit.Health
          MaxHealth = unit.MaxHealth
          BuildProgress = if isUnfinished then 0.5f else 1.0f
          Status =
              if isUnfinished then { defaultStatus with IsUnderConstruction = true }
              else defaultStatus
          WeaponRangesElmo = props.WeaponRanges
          SightRangeElmo = props.SightRange
          BuildRangeElmo = None
          CommandQueue = [] }

    let ofTrackedEnemy
            (defCache: UnitDefCache)
            (enemyId: int)
            (enemy: TrackedEnemy)
            : UnitDisplay =
        let defId = enemy.DefId |> Option.defaultValue 0
        let props = resolveDef defCache defId
        let (px, py, pz) = enemy.Position
        let hp = enemy.Health |> Option.defaultValue 0.0f
        { UnitId = enemyId
          DefId = defId
          InternalName = props.InternalName
          Shape = props.Shape
          Faction = props.Faction
          Tier = props.Tier
          LabelCode = props.LabelCode
          FootprintWidthElmo = props.FootprintW
          FootprintHeightElmo = props.FootprintH
          TeamId = -1
          PositionX = px
          PositionY = py
          PositionZ = pz
          HeadingRadians = 0.0f
          CurrentHealth = hp
          MaxHealth = max hp 1.0f
          BuildProgress = 1.0f
          Status = defaultStatus
          WeaponRangesElmo = props.WeaponRanges
          SightRangeElmo = props.SightRange
          BuildRangeElmo = None
          CommandQueue = [] }

    let ofEncyclopediaEntry
            (entry: EncyclopediaData.EncyclopediaEntry)
            (pinnedFootprint: float32)
            : UnitDisplay =
        { UnitId = 0
          DefId = entry.DefId
          InternalName = entry.InternalName
          Shape = entry.Shape
          Faction = entry.Faction
          Tier = entry.Tier
          LabelCode = UnitLabels.lookupOrFallback entry.InternalName
          FootprintWidthElmo = pinnedFootprint
          FootprintHeightElmo = pinnedFootprint
          TeamId = 0
          PositionX = 0.0f
          PositionY = 0.0f
          PositionZ = 0.0f
          HeadingRadians = 0.0f
          CurrentHealth = float32 entry.Health
          MaxHealth = float32 (max 1 entry.Health)
          BuildProgress = 1.0f
          Status = defaultStatus
          WeaponRangesElmo = entry.WeaponRangesElmo
          SightRangeElmo = entry.SightRangeElmo
          BuildRangeElmo = None
          CommandQueue = [] }
