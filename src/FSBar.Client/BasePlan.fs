namespace FSBar.Client

type PositionChooser =
    | NearestMetalSpot of spotIndex: int
    | NearCommander of dx: float32 * dz: float32
    | NearBaseCentre of dx: float32 * dz: float32
    | AtChokepointHead of chokepointIndex: int
    | AtLiteralPosition of x: float32 * y: float32 * z: float32

type PlanSlot =
    { Name: string
      DefName: string
      Chooser: PositionChooser
      BuilderDefName: string
      ClearanceMargin: float32
      MaxRetries: int }

type BasePlan =
    { Name: string
      Strategy: string
      Slots: PlanSlot list }

type SlotFailure =
    | TerrainNotBuildable of reason: string
    | ClearanceCollision of againstSlotName: string
    | OutOfBuilderReach of builderDefName: string * distanceElmos: float32
    | OffMap
    | WouldWallIn of unreachableStructureNames: string list
    | UnresolvedDependency of chokepointIndex: int
    | NoMetalSpot of requestedIndex: int
    | RetryBudgetExhausted of lastReason: string

type ResolvedSlot =
    { Slot: PlanSlot
      Position: (float32 * float32 * float32) option
      BuildableNow: bool
      Failure: SlotFailure option }

type PlanProgress =
    { ConsumedSlots: Set<string>
      InFlight: Set<string>
      Unfulfillable: Map<string, SlotFailure> }

type ResolveContext =
    { Grid: MapGrid
      BaseCentre: float32 * float32 * float32
      CommanderPos: float32 * float32 * float32
      MetalSpotsNearest: (float32 * float32 * float32 * float32) array
      Chokepoints: Chokepoint list
      UnitDefs: UnitDefCache
      ExistingStructures: OwnStructureFootprint list
      Progress: PlanProgress }

module BasePlan =

    /// Fallback footprint radius (elmos) for common BAR unit / structure def names.
    /// Used when the caller's `UnitDefCache` doesn't expose a footprint field — the
    /// existing `UnitDefCache.UnitDefInfo` record has `MaxWeaponRange` but not
    /// footprint size, so we table-lookup the handful of defs the opening plan uses.
    /// Unknown defs default to 24 elmos (a conservative medium structure).
    let private fallbackFootprintRadius (defName: string) : float32 =
        match defName.ToLowerInvariant() with
        | "armmex" | "cormex" -> 16.0f
        | "armsolar" | "corsolar" -> 16.0f
        | "armwin" | "corwin" -> 16.0f
        | "armlab" | "corlab" -> 32.0f
        | "armvp" | "corvp" -> 40.0f
        | "armllt" | "corllt" -> 12.0f
        | "armcom" | "corcom" -> 20.0f
        | "armck" | "corck" -> 12.0f
        | _ -> 24.0f

    /// Builder reach (elmos) fallback table. Commander reach is typically ~128;
    /// other builders ~90. Real deployments should override via `UnitDefCache`.
    let private fallbackBuilderReach (unitDefs: UnitDefCache) (defName: string) : float32 =
        match UnitDefCache.tryFindByName unitDefs defName with
        | Some info when info.MaxWeaponRange > 0.0f -> info.MaxWeaponRange
        | _ ->
            match defName.ToLowerInvariant() with
            | "armcom" | "corcom" -> 128.0f
            | "armck" | "corck" -> 90.0f
            | _ -> 96.0f

    let private distance2D (a: float32 * float32 * float32) (b: float32 * float32 * float32) : float32 =
        let (ax, _, az) = a
        let (bx, _, bz) = b
        let dx = ax - bx
        let dz = az - bz
        sqrt (dx * dx + dz * dz)

    let defaultArmadaOpening: BasePlan =
        { Name = "defaultArmadaOpening"
          Strategy = "armada-opening"
          Slots =
            [ { Name = "mex#1"
                DefName = "armmex"
                Chooser = NearestMetalSpot 0
                BuilderDefName = "armcom"
                ClearanceMargin = 16.0f
                MaxRetries = 3 }
              { Name = "mex#2"
                DefName = "armmex"
                Chooser = NearestMetalSpot 1
                BuilderDefName = "armcom"
                ClearanceMargin = 16.0f
                MaxRetries = 3 }
              { Name = "solar#1"
                DefName = "armsolar"
                Chooser = NearBaseCentre(200.0f, 0.0f)
                BuilderDefName = "armcom"
                ClearanceMargin = 16.0f
                MaxRetries = 3 }
              { Name = "solar#2"
                DefName = "armsolar"
                Chooser = NearBaseCentre(-200.0f, 0.0f)
                BuilderDefName = "armcom"
                ClearanceMargin = 16.0f
                MaxRetries = 3 }
              { Name = "factory"
                DefName = "armlab"
                Chooser = NearBaseCentre(0.0f, 350.0f)
                BuilderDefName = "armcom"
                ClearanceMargin = 32.0f
                MaxRetries = 2 } ] }

    let emptyPlanProgress: PlanProgress =
        { ConsumedSlots = Set.empty
          InFlight = Set.empty
          Unfulfillable = Map.empty }

    let markConsumed (progress: PlanProgress) (slotName: string) : PlanProgress =
        { progress with
            ConsumedSlots = Set.add slotName progress.ConsumedSlots
            InFlight = Set.remove slotName progress.InFlight }

    let markInFlight (progress: PlanProgress) (slotName: string) : PlanProgress =
        { progress with InFlight = Set.add slotName progress.InFlight }

    let markUnfulfillable
        (progress: PlanProgress)
        (slotName: string)
        (reason: SlotFailure)
        : PlanProgress =
        { progress with
            Unfulfillable = Map.add slotName reason progress.Unfulfillable
            InFlight = Set.remove slotName progress.InFlight }

    /// Dispatch a PositionChooser to a concrete position or an unresolved-dependency failure.
    let private resolveChooser
        (chooser: PositionChooser)
        (context: ResolveContext)
        : Result<float32 * float32 * float32, SlotFailure> =
        match chooser with
        | NearestMetalSpot idx ->
            if idx < 0 || idx >= context.MetalSpotsNearest.Length then
                Result.Error(NoMetalSpot idx)
            else
                let (x, y, z, _) = context.MetalSpotsNearest.[idx]
                Result.Ok(x, y, z)
        | NearCommander(dx, dz) ->
            let (cx, cy, cz) = context.CommanderPos
            Result.Ok(cx + dx, cy, cz + dz)
        | NearBaseCentre(dx, dz) ->
            let (bx, by, bz) = context.BaseCentre
            Result.Ok(bx + dx, by, bz + dz)
        | AtChokepointHead idx ->
            if idx < 0 || idx >= context.Chokepoints.Length then
                Result.Error(UnresolvedDependency idx)
            else
                let cp = List.item idx context.Chokepoints
                Result.Ok cp.Position
        | AtLiteralPosition(x, y, z) -> Result.Ok(x, y, z)

    let resolvePlan (plan: BasePlan) (context: ResolveContext) : ResolvedSlot list =
        let grid = context.Grid
        let widthElmos = float32 grid.WidthElmos
        let heightElmos = float32 grid.HeightElmos

        // Accumulate already-resolved slots so the clearance check can see them.
        let resolvedSoFar = ResizeArray<ResolvedSlot>()

        let skip (slot: PlanSlot) : ResolvedSlot =
            { Slot = slot
              Position = None
              BuildableNow = false
              Failure = None }

        let failed (slot: PlanSlot) (pos: (float32 * float32 * float32) option) (reason: SlotFailure) : ResolvedSlot =
            { Slot = slot
              Position = pos
              BuildableNow = false
              Failure = Some reason }

        let succeeded (slot: PlanSlot) (pos: float32 * float32 * float32) : ResolvedSlot =
            { Slot = slot
              Position = Some pos
              BuildableNow = true
              Failure = None }

        let results = ResizeArray<ResolvedSlot>()
        for slot in plan.Slots do
            let result =
                if Set.contains slot.Name context.Progress.ConsumedSlots then
                    skip slot
                elif Map.containsKey slot.Name context.Progress.Unfulfillable then
                    { Slot = slot
                      Position = None
                      BuildableNow = false
                      Failure = Some context.Progress.Unfulfillable.[slot.Name] }
                else
                    match resolveChooser slot.Chooser context with
                    | Result.Error f -> failed slot None f
                    | Result.Ok pos ->
                        let (px, _, pz) = pos
                        if px < 0.0f || px >= widthElmos || pz < 0.0f || pz >= heightElmos then
                            failed slot (Some pos) OffMap
                        else
                            match MapQuery.terrainAtElmo grid (int px) (int pz) with
                            | Result.Error msg -> failed slot (Some pos) (TerrainNotBuildable msg)
                            | Result.Ok terrain ->
                                let buildable =
                                    match terrain with
                                    | Terrain.Land _ -> None
                                    | Terrain.Water d -> Some(sprintf "water depth %.1f" d)
                                    | Terrain.Cliff s -> Some(sprintf "cliff slope %.2f" s)
                                match buildable with
                                | Some reason -> failed slot (Some pos) (TerrainNotBuildable reason)
                                | None ->
                                    // Clearance check against existing + previously-resolved slots.
                                    let thisR = fallbackFootprintRadius slot.DefName
                                    let existingCollision =
                                        context.ExistingStructures
                                        |> List.tryFind (fun s ->
                                            let sep = distance2D pos s.Centre
                                            let needed = thisR + s.RadiusElmos + slot.ClearanceMargin
                                            sep < needed)
                                    let resolvedCollision () =
                                        resolvedSoFar
                                        |> Seq.tryFind (fun r ->
                                            match r.Position with
                                            | None -> false
                                            | Some otherPos ->
                                                let otherR = fallbackFootprintRadius r.Slot.DefName
                                                let sep = distance2D pos otherPos
                                                let needed = thisR + otherR + slot.ClearanceMargin
                                                sep < needed)
                                    match existingCollision with
                                    | Some s ->
                                        let tag = s.Tag |> Option.defaultValue "<existing>"
                                        failed slot (Some pos) (ClearanceCollision tag)
                                    | None ->
                                        match resolvedCollision () with
                                        | Some r -> failed slot (Some pos) (ClearanceCollision r.Slot.Name)
                                        | None ->
                                            // Builder-reach check (straight-line from commander).
                                            let reach = fallbackBuilderReach context.UnitDefs slot.BuilderDefName
                                            let distFromBuilder =
                                                if slot.BuilderDefName = "armcom" || slot.BuilderDefName = "corcom" then
                                                    distance2D context.CommanderPos pos
                                                else
                                                    // For other builders, use the commander position as the
                                                    // "nearest available builder" proxy — callers with richer
                                                    // information can override via `AtLiteralPosition`.
                                                    distance2D context.CommanderPos pos
                                            // Slot reach budget: builder reach + a generous movement tolerance.
                                            // The commander can always walk to a slot in the opening plan; the
                                            // check mostly matters for long-range ad-hoc placements.
                                            let reachBudget = max reach 2500.0f
                                            if distFromBuilder > reachBudget then
                                                failed slot (Some pos) (OutOfBuilderReach(slot.BuilderDefName, distFromBuilder))
                                            else
                                                // Wall-in check (FR-023) against existing structures + the
                                                // slots we've already placed in this resolvePlan call.
                                                let combined =
                                                    let fromResolved =
                                                        resolvedSoFar
                                                        |> Seq.choose (fun r ->
                                                            match r.Position with
                                                            | Some p ->
                                                                Some
                                                                    { Centre = p
                                                                      RadiusElmos = fallbackFootprintRadius r.Slot.DefName
                                                                      Tag = Some r.Slot.Name }
                                                            | None -> None)
                                                        |> Seq.toList
                                                    context.ExistingStructures @ fromResolved
                                                let proposedFp : OwnStructureFootprint =
                                                    { Centre = pos
                                                      RadiusElmos = thisR
                                                      Tag = Some slot.Name }
                                                let wallInQuery : WallInQuery =
                                                    { MoveType = MoveType.Kbot
                                                      RequireMapEdgeExit = true }
                                                match WallIn.wouldWallIn grid context.BaseCentre combined proposedFp wallInQuery with
                                                | Fails(DisconnectsStructures names) ->
                                                    failed slot (Some pos) (WouldWallIn names)
                                                | Fails EnclosesBase ->
                                                    failed slot (Some pos) (WouldWallIn [ "<base>" ])
                                                | Passes -> succeeded slot pos
            results.Add(result)
            if result.BuildableNow then resolvedSoFar.Add(result)
        results |> List.ofSeq
