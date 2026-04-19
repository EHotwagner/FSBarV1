namespace FSBar.Viz

module EncyclopediaData =

    type EncyclopediaEntry = {
        DefId: int
        InternalName: string
        HumanName: string option
        Subfolder: string
        Faction: FactionId
        Tier: Tier
        Shape: MovementShape
        MetalCost: int
        EnergyCost: int
        BuildTime: int
        Health: int
        FootprintX: int
        FootprintZ: int
        SightRangeElmo: float32
        WeaponRangesElmo: float32 list
        MovementClass: string option
    }

    let private concreteFloat (v: BarData.ValueOrExpr<float>) (fallback: float) : float =
        match v with
        | BarData.ValueOrExpr.Concrete x -> x
        | _ -> fallback

    let private buildEntry (idx: int) (d: BarData.UnitDef) : EncyclopediaEntry =
        // Encyclopedia has historically treated "canMove" as "can fly or
        // has a named movement class" — this is what users see in the
        // encyclopedia glyph and has been accepted as the correct glyph
        // reference per spec 038 FR-001/002. The adapter path reuses
        // the same derivation so live units classify identically.
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
        let shape = UnitGlyph.classifyShape canMove canFly mClass ignore
        let faction = UnitGlyph.classifyFaction d.subfolder d.name ignore
        let tier = UnitGlyph.classifyTier d.customParams d.category ignore
        let weaponRanges =
            match d.weapons with
            | Some ws ->
                ws
                |> List.choose (fun w ->
                    match w.range with
                    | Some v -> Some (float32 (concreteFloat v 0.0))
                    | None -> None)
                |> List.filter (fun r -> r > 0.0f)
            | None -> []
        { DefId = idx
          InternalName = d.name
          HumanName = d.printableName
          Subfolder = d.subfolder
          Faction = faction
          Tier = tier
          Shape = shape
          MetalCost = int (concreteFloat d.metalCost 0.0)
          EnergyCost = int (concreteFloat d.energyCost 0.0)
          BuildTime = int (concreteFloat d.buildTime 0.0)
          Health = int (concreteFloat d.health 0.0)
          FootprintX = max 1 (int d.footprintX)
          FootprintZ = max 1 (int d.footprintZ)
          SightRangeElmo = float32 (concreteFloat d.sightDistance 0.0)
          WeaponRangesElmo = weaponRanges
          MovementClass = mClass }

    let buildFromBarData () : EncyclopediaEntry list =
        BarData.AllUnitDefs.all
        |> List.mapi (fun i (_, _, d) -> buildEntry i d)
        |> List.sortBy (fun e -> e.InternalName)
