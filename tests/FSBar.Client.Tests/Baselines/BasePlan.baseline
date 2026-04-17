namespace FSBar.Client

/// <summary>
/// Strategy for picking a slot's world position during plan resolution.
/// </summary>
type PositionChooser =
    /// <summary>Pick the nth nearest free metal spot to the base centre.</summary>
    | NearestMetalSpot of spotIndex: int
    /// <summary>Pick an offset (dx, dz) from the current commander position.</summary>
    | NearCommander of dx: float32 * dz: float32
    /// <summary>Pick an offset (dx, dz) from a pinned base centre.</summary>
    | NearBaseCentre of dx: float32 * dz: float32
    /// <summary>Place at the inward-facing head of a specific chokepoint.</summary>
    | AtChokepointHead of chokepointIndex: int
    /// <summary>Place at a literal absolute world position (primarily for tests / diagnostics).</summary>
    | AtLiteralPosition of x: float32 * y: float32 * z: float32

/// <summary>One structure slot in a named plan.</summary>
type PlanSlot =
    { /// <summary>Short symbolic name — e.g. "mex#1", "opening-factory".</summary>
      Name: string
      /// <summary>Structure def name (e.g. "armmex", "armsolar", "armlab").</summary>
      DefName: string
      /// <summary>Position chooser.</summary>
      Chooser: PositionChooser
      /// <summary>Def name of the builder required (e.g. "armcom", "armck").</summary>
      BuilderDefName: string
      /// <summary>Additive edge-to-edge clearance margin in elmos (clarification Q4).</summary>
      ClearanceMargin: float32
      /// <summary>Maximum retries before the slot is marked unfulfillable.</summary>
      MaxRetries: int }

/// <summary>A named, reusable base plan consumed by <c>resolvePlan</c>.</summary>
type BasePlan =
    { /// <summary>Plan identifier.</summary>
      Name: string
      /// <summary>Human-readable strategy tag (e.g. "armada-opening", "turtle").</summary>
      Strategy: string
      /// <summary>Slots in resolution order.</summary>
      Slots: PlanSlot list }

/// <summary>Reason a slot failed to resolve. Used for diagnostic logging and retry decisions.</summary>
type SlotFailure =
    | TerrainNotBuildable of reason: string
    | ClearanceCollision of againstSlotName: string
    | OutOfBuilderReach of builderDefName: string * distanceElmos: float32
    | OffMap
    | WouldWallIn of unreachableStructureNames: string list
    | UnresolvedDependency of chokepointIndex: int
    | NoMetalSpot of requestedIndex: int
    | RetryBudgetExhausted of lastReason: string

/// <summary>Output of <c>resolvePlan</c>: one record per input slot.</summary>
type ResolvedSlot =
    { /// <summary>The originating slot.</summary>
      Slot: PlanSlot
      /// <summary>Concrete resolved position, <c>None</c> on failure.</summary>
      Position: (float32 * float32 * float32) option
      /// <summary>True when the slot is buildable by its declared builder right now.</summary>
      BuildableNow: bool
      /// <summary>Failure reason when resolution fell through the pipeline.</summary>
      Failure: SlotFailure option }

/// <summary>Plan-consumption state. Immutable — replaced after each incremental resolve.</summary>
type PlanProgress =
    { /// <summary>Slot names already consumed (structure built or under construction).</summary>
      ConsumedSlots: Set<string>
      /// <summary>Slot names previously resolved but not yet consumed (in flight).</summary>
      InFlight: Set<string>
      /// <summary>Slot names marked permanently unfulfillable.</summary>
      Unfulfillable: Map<string, SlotFailure> }

/// <summary>
/// Inputs to <c>resolvePlan</c> packaged so callers can pre-compute expensive values
/// (chokepoints, metal spots) once and reuse them across successive resolve calls.
/// </summary>
type ResolveContext =
    { /// <summary>Map data.</summary>
      Grid: MapGrid
      /// <summary>Pinned base centre.</summary>
      BaseCentre: float32 * float32 * float32
      /// <summary>Current commander position.</summary>
      CommanderPos: float32 * float32 * float32
      /// <summary>Metal spots sorted by distance from base centre; each tuple is (x, y, z, metal).</summary>
      MetalSpotsNearest: (float32 * float32 * float32 * float32) array
      /// <summary>Pre-computed chokepoint list (may be empty).</summary>
      Chokepoints: Chokepoint list
      /// <summary>Unit definition cache for footprint / reach lookups.</summary>
      UnitDefs: UnitDefCache
      /// <summary>Existing placed structure footprints.</summary>
      ExistingStructures: OwnStructureFootprint list
      /// <summary>Plan consumption state.</summary>
      Progress: PlanProgress }

/// <summary>
/// Declarative building layout: resolves named slot lists into placement decisions that
/// honour terrain, clearance, builder reach, and wall-in constraints.
/// </summary>
module BasePlan =

    /// <summary>
    /// Built-in plan matching feature 023's iter 026 opening sequence:
    /// 2 armmex (nearest metal spots) + 2 armsolar (NearBaseCentre ±200, 0) + 1 armlab (NearBaseCentre 0, 350).
    /// </summary>
    val defaultArmadaOpening: BasePlan

    /// <summary>Empty progress — use at match start.</summary>
    val emptyPlanProgress: PlanProgress

    /// <summary>
    /// Resolve every slot in <paramref name="plan"/> against <paramref name="context"/>.
    /// Returns one <see cref="T:FSBar.Client.ResolvedSlot"/> per <c>PlanSlot</c> in input
    /// order. Pure; does not mutate inputs.
    /// </summary>
    val resolvePlan: plan: BasePlan -> context: ResolveContext -> ResolvedSlot list

    /// <summary>Mark a slot as consumed (structure built or under construction).</summary>
    val markConsumed: progress: PlanProgress -> slotName: string -> PlanProgress

    /// <summary>Mark a slot as in-flight (BuildCommand issued, awaiting UnitFinished).</summary>
    val markInFlight: progress: PlanProgress -> slotName: string -> PlanProgress

    /// <summary>Mark a slot as permanently unfulfillable with the given reason.</summary>
    val markUnfulfillable:
        progress: PlanProgress ->
        slotName: string ->
        reason: SlotFailure ->
            PlanProgress
