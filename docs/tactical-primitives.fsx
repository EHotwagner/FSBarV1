(**
---
title: Tactical Map Primitives
category: Tutorials
categoryindex: 2
index: 7
description: Slope-aware pathing, chokepoint detection, base-plan resolution, wall-in checks, and SMF parsing — the five FSBar.Client modules shipped in feature 024.
---
*)

(**
# Tactical Map Primitives

Feature `024-tactical-map-primitives` ships five new Tier-1 modules on
`FSBar.Client` that let a bot reason about BAR terrain without a running
engine:

| Module | Purpose |
|---|---|
| `SmfParser` | Parse a BAR `.sd7` archive into a `MapGrid` directly from disk. |
| `Pathing` | A\* over `MapGrid.passability` with slope-weighted edges and a friendly-structure mask. |
| `Chokepoints` | Union-find bridge detection — O(N log N) identification of narrow corridors that separate a base from the rest of the map. |
| `WallIn` | Pure connectivity predicate sharing passability rules with `Pathing`. |
| `BasePlan` | Declarative structure-slot layout with `resolvePlan` validating terrain, clearance, builder reach, and wall-in constraints. |

The detailed spec, data model, and contracts live under
[`specs/024-tactical-map-primitives/`](https://github.com/EHotwagner/FSBarV1/tree/master/specs/024-tactical-map-primitives).
This page is the operator-facing walkthrough — it links to the spec
tree rather than duplicating it.

<div class="alert alert-info">
<strong>Offline pipeline:</strong> Maps are fixed, so any analysis that
depends only on the <code>.sd7</code> file should be precomputed. The
companion script
<a href="https://github.com/EHotwagner/FSBarV1/blob/master/scripts/examples/14-cache-map-analysis.fsx"><code>scripts/examples/14-cache-map-analysis.fsx</code></a>
runs <code>findChokepoints</code> once per map into
<code>bots/trainer/map-cache/&lt;name&gt;.json</code>. Running the same
analysis live during a bot's warmup will starve the frame-reading path
and OOM the engine's Lua VM at 100× headless speed — see
<a href="https://github.com/EHotwagner/FSBarV1/blob/master/bots/trainer/PLAYBOOK.md">PLAYBOOK.md §13</a>
for the diagnosis.
</div>

## SmfParser — reading a BAR map offline

*)

#r "../src/FSBar.Client/bin/Debug/net10.0/FSBar.Client.dll"

open System
open System.IO
open FSBar.Client

(**
`SmfParser.parseSd7` shells out to `bsdtar` to extract the `.smf` and
`mapinfo.lua` from a `.sd7` archive, parses the Spring Map File header,
decodes the corner-heightmap + half-resolution slope / metal / type
layers, and returns an `SmfMap` record. Returns `Result.Error` for every
bounded failure (missing archive, bad magic, unsupported version,
truncated buffer).
*)

(*** do-not-eval ***)
let home =
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
let avalanche =
    Path.Combine(home, ".local", "state", "Beyond All Reason",
                 "maps", "avalanche_3.4.sd7")

match SmfParser.parseSd7 avalanche with
| Result.Error err ->
    printfn "parse failed: %A" err
| Result.Ok smf ->
    printfn "%dx%d heightmap (%d x %d elmos)"
        smf.WidthHeightmap smf.HeightHeightmap
        smf.WidthElmos smf.HeightElmos

(**
The heightmap is decoded against the `mapinfo.lua` `smf.minheight` /
`smf.maxheight` overrides when they are present, so the output matches
the live-engine `getCornersHeightMap` callback within ±1 elmo. On
Avalanche 3.4 this reproduces `min = 130.0` and `max = 700.0` — the
same values the HighBar proxy returns in a running game.

`SmfParser.toMapGrid` lifts an `SmfMap` into a `MapGrid` record
(zero-filled LOS / radar since offline parsing has no runtime visibility
data), which is what the other tactical modules consume.

Full example: [`scripts/examples/10-smf.fsx`](https://github.com/EHotwagner/FSBarV1/blob/master/scripts/examples/10-smf.fsx).

## Pathing — slope-aware A\*

`Pathing.findPath` runs an octile-8-neighbour A\* over
`MapGrid.passability` with slope-weighted edge costs, friendly
structures rasterised into the passability grid, and diagonal
corner-cut prevention. It is pure over its inputs and respects a
wall-clock + expansion-count budget.
*)

(*** do-not-eval ***)
let budget : PathBudget =
    { WallClockMs = 50
      MaxExpansions = 50_000
      SlopeCost = 2.0f }

let grid : MapGrid = failwith "load via SmfParser.toMapGrid or MapGrid.loadFromEngine"
let start : float32 * float32 * float32 = (500.0f, 0.0f, 397.0f)
let goal : float32 * float32 * float32 = (3699.0f, 0.0f, 3601.0f)

match Pathing.findPath grid MoveType.Kbot Seq.empty start goal budget with
| Result.Ok path ->
    printfn "status=%A cost=%.0f waypoints=%d"
        path.Status path.EstimatedCost path.Waypoints.Length
| Result.Error err ->
    printfn "no path: %A" err

(**
Edge cost is `distance × (1 + slope × SlopeCost)` with octile distance
for the heuristic (admissible under the minimum edge cost of 1.0).
Friendly structures are supplied as `OwnStructureFootprint seq` and
rasterised into an impassable overlay — the caller owns cache
invalidation.

When the budget expires the search returns `Ok` with
`Status = Partial true` and a best-effort partial path reconstructed
from the lowest-heuristic node observed so far. This is important for
real-time use: a bot never blocks longer than its tick budget.

Full example: [`scripts/examples/11-pathing.fsx`](https://github.com/EHotwagner/FSBarV1/blob/master/scripts/examples/11-pathing.fsx).

## Chokepoints — bridge detection via union-find

`Chokepoints.findChokepoints` identifies narrow corridors between a
base centre and the rest of the reachable map. The algorithm is
reverse-order union-find bridge detection:

1. Compute the distance transform (DT) of the passability grid.
2. Sort passable cells by DT descending.
3. Pre-activate the base cell as its own component.
4. Iterate widest-first; for each cell, union with active neighbours.
   When adding a cell grows base's component by more than 1, that
   cell is a bridge — the growth counts the cells isolated behind it.
5. Filter bridges by `dt ≤ maxDtForWidth`, search radius, and a minimum
   impact threshold (default 50 cells) to reject trivial pockets.

The whole pipeline is O(N log N) for the sort plus O(N α(N)) for the
union-find. Avalanche 3.4 (252 943 passable cells) returns 3 real
canyon entrances in ~250 ms — down from 34 minutes for the earlier
brute-force primary-route filter this replaced.
*)

(*** do-not-eval ***)
let query =
    { Chokepoints.defaultChokepointQuery MoveType.Kbot with
        MaxWidthElmos = 240.0f
        SearchRadiusElmos = 5500.0f }

let baseCentre : float32 * float32 * float32 = (500.0f, 0.0f, 397.0f)

let chokepoints = Chokepoints.findChokepoints grid baseCentre query
for cp in chokepoints do
    let (px, _, pz) = cp.Position
    printfn "pos=(%.0f,%.0f) width=%.0f distFromBase=%.0f"
        px pz cp.WidthElmos cp.DistanceFromBase

(**
Each `Chokepoint` carries a stable `ChokepointId` (FNV-1a hash of grid
dimensions + ridge cell linearised index) so callers can reference the
same corridor across successive queries — the id is unchanged as long
as the map is unchanged.

For maps used by a trainer bot this call should land in a precomputed
cache, not in live warmup. Run
`scripts/examples/14-cache-map-analysis.fsx` once per map to populate
`bots/trainer/map-cache/<name>.json`, then load the JSON in ~10 ms at
bot warmup.

Full example: [`scripts/examples/12-chokepoints.fsx`](https://github.com/EHotwagner/FSBarV1/blob/master/scripts/examples/12-chokepoints.fsx).

## WallIn — connectivity predicate

`WallIn.wouldWallIn` is a pure predicate that answers "would placing
this new structure disconnect the base from any currently-reachable
structure or from the map edge?" It shares its passability evaluation
with `Pathing.findPath` — a placement that passes the `WallIn` check
is guaranteed not to cut off a `findPath` route that was previously
valid.
*)

(*** do-not-eval ***)
let existing : OwnStructureFootprint list = [
    { Centre = (600.0f, 0.0f, 400.0f)
      RadiusElmos = 16.0f
      Tag = Some "factory" }
]

let proposed : OwnStructureFootprint =
    { Centre = (500.0f, 0.0f, 400.0f)
      RadiusElmos = 32.0f
      Tag = Some "plug" }

let q : WallInQuery =
    { MoveType = MoveType.Kbot
      RequireMapEdgeExit = true }

match WallIn.wouldWallIn grid baseCentre existing proposed q with
| Passes -> printfn "placement is safe"
| Fails (DisconnectsStructures names) ->
    printfn "would cut off: %A" names
| Fails EnclosesBase ->
    printfn "would enclose the base (no map-edge exit)"

(**
The check uses two 8-connected flood fills (pre-placement +
post-placement) and a per-structure "is there any reachable cell
within radius + 1 of the structure's centre" lookup — structures
whose own footprint makes their centre cell impassable would
otherwise always look disconnected.

`WallIn.reachableCells` is also exposed so callers like `BasePlan` can
reuse a single flood fill across multiple slot checks.

## BasePlan — declarative structure layout

`BasePlan` is the highest-level consumer of the four primitives above.
A `BasePlan` is a list of named `PlanSlot` records describing where
each structure should go; `resolvePlan` validates every slot against
the current context and returns `ResolvedSlot` records with either a
concrete position or a `SlotFailure` diagnostic.

The built-in `defaultArmadaOpening` mirrors the trainer's 023
iter-026 opening:
*)

(*** do-not-eval ***)
BasePlan.defaultArmadaOpening.Slots
|> List.iter (fun slot ->
    printfn "%s -> %s (chooser=%A clearance=%.0f)"
        slot.Name slot.DefName slot.Chooser slot.ClearanceMargin)

(**
Available position choosers:

- `NearestMetalSpot spotIndex` — pick the nth nearest free metal spot from `ResolveContext.MetalSpotsNearest`
- `NearCommander (dx, dz)` — offset from the current commander position
- `NearBaseCentre (dx, dz)` — offset from a pinned base centre
- `AtChokepointHead chokepointIndex` — place at the inward-facing side of a specific chokepoint from the pinned list
- `AtLiteralPosition (x, y, z)` — direct world coordinate

The `resolvePlan` pipeline for each slot runs:

1. Skip if `Progress.ConsumedSlots` contains the slot name.
2. Dispatch the `PositionChooser` to get a proposed world position.
3. Bounds-check against the `MapGrid` dimensions.
4. Terrain check via `MapQuery.terrainAtElmo` — only `Land` is buildable.
5. Clearance check (per clarification Q4): additive edge-to-edge margin against existing footprints AND previously-resolved slots.
6. Builder reach check — straight-line distance from commander to proposed centre.
7. Wall-in check (FR-023) — `WallIn.wouldWallIn` with the proposed footprint added to `context.ExistingStructures`.

`PlanProgress` lets callers mark slots as in-flight, consumed, or
permanently unfulfillable, and `resolvePlan` replays correctly against
that state on every call.

Full example: [`scripts/examples/13-plan.fsx`](https://github.com/EHotwagner/FSBarV1/blob/master/scripts/examples/13-plan.fsx).

## Offline cache pipeline

<div class="alert alert-warning">
<strong>Always run map analysis offline.</strong> Running
<code>findChokepoints</code> or <code>MapGrid.loadFromEngine</code>
during a bot's warmup blocks the frame-reading path for hundreds of
milliseconds. At 100× headless game speed the engine produces ~6 000
game frames per wall-clock second, so a 250 ms block fills the proxy's
socket write buffer, trips "Socket not writable, dropping frame" in
<code>engine.infolog</code>, and eventually OOMs the Lua VM.
</div>

The `scripts/examples/14-cache-map-analysis.fsx` script is the
canonical offline pipeline:
*)

(*** do-not-eval ***)
// Runs once per map, at operator discretion.
// Writes bots/trainer/map-cache/<safe-name>.json — 1.2 KB per map.
//
// $ dotnet fsi scripts/examples/14-cache-map-analysis.fsx "Avalanche 3.4"

(**
The runtime bot reads the JSON with `File.ReadAllText` +
`JsonDocument.Parse` in well under 10 ms and stashes the chokepoint
list for the defend-interrupt path.

See [`bots/trainer/PLAYBOOK.md §13 "Tactical primitives integration"`](https://github.com/EHotwagner/FSBarV1/blob/master/bots/trainer/PLAYBOOK.md)
for the bot-side consumption pattern, the stdout trace reference, and
the `BasePlan` extension procedure. The 023 `bot_macro.fsx` deep
integration lives there — this page intentionally stops at the module
boundary.

## Further reading

- **Spec tree**: [`specs/024-tactical-map-primitives/`](https://github.com/EHotwagner/FSBarV1/tree/master/specs/024-tactical-map-primitives) — spec, plan, data-model, contracts, quickstart
- **Bot playbook**: [`bots/trainer/PLAYBOOK.md §13`](https://github.com/EHotwagner/FSBarV1/blob/master/bots/trainer/PLAYBOOK.md) — how `bot_macro.fsx` consumes the primitives in a live iteration
- **Cross-repo mailboxes**: [`Mailbox/2026-04-14_to_HighBarV2_mid-game-callback-event-drop.md`](https://github.com/EHotwagner/FSBarV1/blob/master/Mailbox) — the proxy-contract fix that unblocked 024 live-iteration validation
- **FSI examples**: `scripts/examples/10-smf.fsx` through `14-cache-map-analysis.fsx`
*)
