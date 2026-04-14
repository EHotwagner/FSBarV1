# Contract: `FSBar.Client.BasePlan`

**FR link**: FR-012, FR-013, FR-014, FR-015, FR-016, FR-017, FR-018, FR-023 (anti wall-in integration)
**Tier**: 1 — compiled module, curated `.fsi`, surface-area baseline required.
**Clarifications**: Q3 (ownStructures model), Q4 (clearance = additive margin over footprint edge).

## Purpose

Declarative building layout. Replaces `bots/trainer/helpers/opening_build.fsx`'s hardcoded offset list with a named, typed plan whose slots are resolved against a `MapGrid` + a current set of placed structures, honouring terrain, clearance margin, builder reach, and wall-in constraints.

## Public API surface (`BasePlan.fsi`)

```fsharp
namespace FSBar.Client

type PositionChooser =
    | NearestMetalSpot of spotIndex:int
    | NearCommander of dx:float32 * dz:float32
    | NearBaseCentre of dx:float32 * dz:float32
    | AtChokepointHead of chokepointIndex:int
    | AtLiteralPosition of x:float32 * y:float32 * z:float32

type PlanSlot = {
    Name: string
    DefName: string
    Chooser: PositionChooser
    BuilderDefName: string
    /// Additive edge-to-edge clearance margin in elmos (FR-013, Q4).
    ClearanceMargin: float32
    MaxRetries: int
}

type BasePlan = {
    Name: string
    Strategy: string
    Slots: PlanSlot list
}

type SlotFailure =
    | TerrainNotBuildable of reason:string
    | ClearanceCollision of againstSlotName:string
    | OutOfBuilderReach of builderDefName:string * distanceElmos:float32
    | OffMap
    | WouldWallIn of unreachableStructureNames:string list
    | UnresolvedDependency of chokepointIndex:int
    | NoMetalSpot of requestedIndex:int
    | RetryBudgetExhausted of lastReason:string

type ResolvedSlot = {
    Slot: PlanSlot
    Position: (float32 * float32 * float32) option
    BuildableNow: bool
    Failure: SlotFailure option
}

type PlanProgress = {
    ConsumedSlots: Set<string>
    InFlight: Set<string>
    Unfulfillable: Map<string, SlotFailure>
}

/// Inputs to resolvePlan beyond the plan itself. Packaged so the
/// caller can pre-compute expensive values (chokepoints, metal spots)
/// once and reuse them across multiple resolvePlan calls.
type ResolveContext = {
    Grid: MapGrid
    BaseCentre: float32 * float32 * float32
    CommanderPos: float32 * float32 * float32
    MetalSpotsNearest: (float32 * float32 * float32 * float32) array  // sorted by distance
    Chokepoints: Chokepoint list
    UnitDefs: UnitDefCache
    ExistingStructures: OwnStructureFootprint list
    Progress: PlanProgress
}

module BasePlan =

    /// Built-in plan matching feature 023's opening sequence (FR-016).
    /// 2 armmex (nearest metal spots) + 2 armsolar (NearBaseCentre ±200, 0)
    /// + 1 armlab (NearBaseCentre 0, 350).
    val defaultArmadaOpening : BasePlan

    /// Empty progress — use at match start.
    val emptyPlanProgress : PlanProgress

    /// Resolve every slot in `plan` against the `context`. Returns one
    /// ResolvedSlot per PlanSlot in input order. A slot whose
    /// `Progress.ConsumedSlots` already contains its name is returned
    /// with `BuildableNow = false, Failure = None` — the caller should
    /// skip it.
    val resolvePlan :
        plan: BasePlan ->
        context: ResolveContext ->
        ResolvedSlot list

    /// Mark a slot as consumed (its structure is built or under
    /// construction). Returns an updated PlanProgress.
    val markConsumed :
        progress: PlanProgress ->
        slotName: string ->
        PlanProgress

    /// Mark a slot as in-flight (BuildCommand issued, awaiting
    /// UnitFinished). Returns an updated PlanProgress.
    val markInFlight :
        progress: PlanProgress ->
        slotName: string ->
        PlanProgress

    /// Mark a slot as permanently unfulfillable (retry budget
    /// exhausted, or terrain-blocked with no relocation available).
    val markUnfulfillable :
        progress: PlanProgress ->
        slotName: string ->
        reason: SlotFailure ->
        PlanProgress
```

## Semantics

### `resolvePlan` pipeline

For each `PlanSlot` in order:

1. **Skip if already consumed**: if `progress.ConsumedSlots` contains the slot name, emit a ResolvedSlot with `Position = None, BuildableNow = false, Failure = None`. The caller knows to skip it.
2. **Position chooser**:
   - `NearestMetalSpot n` → `context.MetalSpotsNearest.[n]` if `n < length`, else `Failure = NoMetalSpot n`.
   - `NearCommander (dx, dz)` → `commanderPos + (dx, 0, dz)`.
   - `NearBaseCentre (dx, dz)` → `baseCentre + (dx, 0, dz)`.
   - `AtChokepointHead k` → head of `context.Chokepoints.[k]`; `Failure = UnresolvedDependency k` if `k >= length`.
   - `AtLiteralPosition (x, y, z)` → direct.
3. **Bounds check**: position outside `[0, grid.WidthElmos] × [0, grid.HeightElmos]` → `Failure = OffMap`.
4. **Terrain check**: `MapQuery.terrainAtElmo grid x z` must be `Land` (not `Water` or `Cliff`). Otherwise → `Failure = TerrainNotBuildable reason`.
5. **Clearance check** (FR-013, Q4): compute the structure's footprint radius from `UnitDefs` (or a fallback table of known BAR unit footprints). For every existing `OwnStructureFootprint` in `context.ExistingStructures` **and** every previously-resolved slot in this `resolvePlan` call, check that `distance(proposedCentre, otherCentre) >= (proposedFootprintRadius + otherFootprintRadius + ClearanceMargin)`. First violation → `Failure = ClearanceCollision otherName`.
6. **Builder reach check**: `distance(commanderPos, proposedCentre)` (if the slot's builder is the commander) or `distance(nearest available builder, proposedCentre)` must be ≤ builder's max reach (from `UnitDefs.MaxWeaponRange` or a helper lookup). Exceeded → `Failure = OutOfBuilderReach (builderDefName, distance)`.
7. **Wall-in check** (FR-023): call `WallIn.wouldWallIn` with the proposed placement added to `context.ExistingStructures`. If `Fails (DisconnectsStructures names)` → `Failure = WouldWallIn names`. If `Fails EnclosesBase` → `Failure = WouldWallIn ["<base>"]`.
8. **Success**: emit `ResolvedSlot { Slot; Position = Some p; BuildableNow = true; Failure = None }`.

### Determinism

`resolvePlan` is pure over `(plan, context)`. Identical inputs → identical output list. Deterministic metal-spot selection requires `context.MetalSpotsNearest` to be sorted deterministically before the call (see quickstart.md §4 for the caller pattern).

### `defaultArmadaOpening` (FR-016)

```text
BasePlan {
    Name = "defaultArmadaOpening"
    Strategy = "armada-opening"
    Slots = [
        { Name = "mex#1";     DefName = "armmex";   Chooser = NearestMetalSpot 0;             BuilderDefName = "armcom"; ClearanceMargin = 16.0f; MaxRetries = 3 }
        { Name = "mex#2";     DefName = "armmex";   Chooser = NearestMetalSpot 1;             BuilderDefName = "armcom"; ClearanceMargin = 16.0f; MaxRetries = 3 }
        { Name = "solar#1";   DefName = "armsolar"; Chooser = NearBaseCentre(200.0f, 0.0f);   BuilderDefName = "armcom"; ClearanceMargin = 16.0f; MaxRetries = 3 }
        { Name = "solar#2";   DefName = "armsolar"; Chooser = NearBaseCentre(-200.0f, 0.0f);  BuilderDefName = "armcom"; ClearanceMargin = 16.0f; MaxRetries = 3 }
        { Name = "factory";   DefName = "armlab";   Chooser = NearBaseCentre(0.0f, 350.0f);   BuilderDefName = "armcom"; ClearanceMargin = 32.0f; MaxRetries = 2 }
    ]
}
```

Matches the 023 iter 026 opening exactly at construction-order level. The `ClearanceMargin` values are chosen so that the 023 offsets remain valid (the solars were placed 200 elmos apart from the mexes in iter 006 and worked; a 16-elmo margin on a ~32-elmo footprint is comfortable).

## Test strategy

**Unit tests** (synthetic `MapGrid` + synthetic `ResolveContext`):

- `resolvePlan defaultArmadaOpening` on a flat 64×64 synthetic grid with 2 metal spots at known positions → 5 ResolvedSlot records, all `BuildableNow = true, Failure = None`, positions deterministic across two calls.
- Slot with `NearestMetalSpot 2` on a grid with only 2 metal spots → `Failure = NoMetalSpot 2`.
- Clearance collision: two slots at the same `NearBaseCentre` offset → second slot returns `Failure = ClearanceCollision "first slot name"`.
- Wall-in: synthetic one-corridor base where the factory slot would close the corridor → `Failure = WouldWallIn [...]`.
- `markConsumed` / `markInFlight` / `markUnfulfillable` round-trip through `PlanProgress` and subsequent `resolvePlan` calls correctly skip / retry / permanently reject.
- Off-map check: slot with `NearBaseCentre(-10000, 0)` on a 2048-elmo map → `Failure = OffMap`.

**Integration tests** (SMF-parsed Avalanche 3.4):

- SC-004: `resolvePlan defaultArmadaOpening` produces 5 non-failure ResolvedSlots on Avalanche 3.4 from base `(500, 397)`.
- `AtChokepointHead 0` slot resolves against a real chokepoint list from the Chokepoints module.

**Live bot tests**:

- US5 integration: `bot_macro.fsx` drives its opening via `resolvePlan defaultArmadaOpening` → all 5 opening items finish construction → `[commander-idle-defect]` never fires.

## Surface-area baseline

`tests/FSBar.Client.Tests/Baselines/BasePlan.baseline` — validated by the existing harness.
