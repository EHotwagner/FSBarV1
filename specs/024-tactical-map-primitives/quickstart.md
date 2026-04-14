# Quickstart — 024 Tactical Map Primitives

**Branch**: `024-tactical-map-primitives`
**Audience**: operator running the feature end-to-end, or a fresh subagent doing the SC-008 second-operator exercise.

Feature 024 ships five new `FSBar.Client` modules plus a deep refactor of `bot_macro.fsx`. This walkthrough is the shortest path from "repo freshly checked out" to "bot_macro wins on NullAI via the new primitives".

## 0. Prerequisites

- F# / .NET 10.0 SDK installed.
- BAR game installed at `~/.local/state/Beyond All Reason/maps/*.sd7` (feature 023's trainer already required this).
- `bsdtar` (libarchive) on `PATH`. Verify: `which bsdtar` → `/usr/sbin/bsdtar` or similar.
- Repo built: `dotnet build src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug`.
- On the 024 branch: `git checkout 024-tactical-map-primitives`.

## 1. Parse a BAR map offline (SMF parser)

The cheapest thing to verify is that the SMF parser reads Avalanche 3.4 without a running engine.

```fsharp
// scripts/examples/10-smf.fsx (shipped in the final feature — see that file
// for the canonical version).
#load "../prelude.fsx"
open System
open System.IO
open FSBar.Client

let sd7 =
    let home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    Path.Combine(home, ".local", "state", "Beyond All Reason", "maps", "avalanche_3.4.sd7")

match SmfParser.parseSd7 sd7 with
| Result.Ok smf ->
    printfn "Avalanche 3.4: %dx%d heightmap (%dx%d elmos)"
        smf.WidthHeightmap smf.HeightHeightmap smf.WidthElmos smf.HeightElmos
    let mutable hmin = System.Single.MaxValue
    let mutable hmax = System.Single.MinValue
    let w = Array2D.length1 smf.HeightMap
    let h = Array2D.length2 smf.HeightMap
    for x in 0 .. w - 1 do
        for z in 0 .. h - 1 do
            let v = smf.HeightMap.[x, z]
            if v < hmin then hmin <- v
            if v > hmax then hmax <- v
    printfn "Height range: [%.1f, %.1f] elmos" hmin hmax
| Result.Error e ->
    printfn "Parse failed: %A" e
```

> The `Result.Ok` / `Result.Error` qualification is mandatory — `FSBar.Client`
> declares its own `SessionState.Error` DU case, which shadows the unqualified
> `Error` constructor under `open FSBar.Client`.

**Expected output** (Avalanche 3.4):

```text
Avalanche 3.4: 512x512 heightmap (4096x4096 elmos)
Height range: [130.0, 700.0] elmos
```

These values match the 2026-04-06 HighBarV2 extraction report (`Mailbox/2026-04-06_highbarv2_map_data_extraction_report.md`) and the 2026-04-13 live-engine probe (see `CLAUDE.md` correction commit). If they don't match, the parser is broken — do not proceed to the pathing step.

## 2. Compute a path on a parsed map

```fsharp
// scripts/examples/11-pathing.fsx
#load "../prelude.fsx"
open FSBar.Client

let sd7 = "/home/developer/.local/state/Beyond All Reason/maps/avalanche_3.4.sd7"
let smf =
    match SmfParser.parseSd7 sd7 with
    | Result.Ok v -> v
    | Result.Error e -> failwithf "SmfParser.parseSd7: %A" e
let grid = SmfParser.toMapGrid smf

let start = (500.0f, 0.0f, 397.0f)          // commander start (y is ignored)
let goal  = (3699.0f, 0.0f, 3601.0f)        // enemy commander (corcom) on NullAI

// For a cross-map query on a 512x512 heightmap, the default 50ms / 50000-
// expansion budget is too tight — bump both by 10x so the run completes
// within a single call.
let budget : PathBudget =
    { WallClockMs = 500
      MaxExpansions = 500_000
      SlopeCost = 2.0f }

match Pathing.findPath grid MoveType.Kbot Seq.empty start goal budget with
| Result.Ok path ->
    printfn "Path: %d waypoints, cost=%.1f, status=%A"
        path.Waypoints.Length path.EstimatedCost path.Status
    for (x, _, z) in path.Waypoints do printfn "  (%.0f, %.0f)" x z
| Result.Error err ->
    printfn "No path: %A" err
```

**Expected output** (Avalanche 3.4, Kbot, no structures in the way, the
11-pathing.fsx script observed value):

```text
status=Complete cost=642.0 elmos waypoints=3 (~150ms)
  [0] (500, 397)       -- start
  [1] (3700, 3580)     -- single detour waypoint through Avalanche
  [2] (3700, 3604)     -- goal
```

The waypoint list is compressed via the greedy line-of-sight coalescer
so two adjacent waypoints are always line-walkable; the 3-waypoint
result is not a bug. If you see `Partial true`, bump `WallClockMs` or
`MaxExpansions` further — the cross-map run may need more under heavy
system load.

## 3. Detect chokepoints near a base

```fsharp
// scripts/examples/NN-chokepoints.fsx
#load "../prelude.fsx"
open FSBar.Client

let smf = SmfParser.parseSd7 "/home/developer/.local/state/Beyond All Reason/maps/avalanche_3.4.sd7" |> Result.toOption |> Option.get
let grid = SmfParser.toMapGrid smf
let baseCentre = (500.0f, 350.0f, 397.0f)

let query = Chokepoints.defaultChokepointQuery MoveType.Kbot
let chokepoints = Chokepoints.findChokepoints grid baseCentre query
printfn "Found %d chokepoint(s) within %.0f elmos of base"
    (List.length chokepoints) query.SearchRadiusElmos
for cp in chokepoints do
    let (x, _, z) = cp.Position
    let (dx, dz) = cp.OutwardDir
    printfn "  id=%A pos=(%.0f, %.0f) width=%.0f outward=(%.2f, %.2f) dist=%.0f"
        cp.Id x z cp.WidthElmos dx dz cp.DistanceFromBase
```

**Expected output** (Avalanche 3.4 from `(500, 397)`, after the
`2026-04-14` union-find bridge rewrite, with `MaxWidthElmos = 240` and
`SearchRadiusElmos = 5500`):

```text
Found 3 chokepoint(s) within 5500 elmos of base
  id=ChokepointId 3376981511u pos=(84, 2844)  width=61  outward=(-0.17, 0.99)  dist=2482
  id=ChokepointId 4255790005u pos=(4028, 236) width=160 outward=(1.00, -0.05) dist=3532
  id=ChokepointId 2129007315u pos=(1196, 4052) width=112 outward=(0.19, 0.98)  dist=3721
```

These three correspond to the real canyon approaches on Avalanche 3.4
reachable from the Player-1 corner. The algorithm runs in ~250 ms —
`defaultChokepointQuery` alone returns zero results because its 40-elmo
maximum width is below Avalanche's canyon widths. For the NullAI rung
you want the wider query shown above.

## 4. Resolve a building plan

See `scripts/examples/13-plan.fsx` for the canonical runnable version.
The important wiring is:

```fsharp
#load "../prelude.fsx"
open FSBar.Client

let smf =
    match SmfParser.parseSd7 sd7 with
    | Result.Ok v -> v
    | Result.Error e -> failwithf "%A" e
let grid = SmfParser.toMapGrid smf

let commanderPos = (500.0f, 0.0f, 397.0f)

// Derive metal spots directly from the SMF metal map — there is no
// Callbacks.getMetalSpots_offline helper; the live Callbacks.getMetalSpots
// is the only runtime source, and this is its offline equivalent.
// (13-plan.fsx picks a handful above a threshold; for production use a
// proper peak-finder.)
let metalSpots =
    let mw = Array2D.length1 smf.MetalMap
    let mh = Array2D.length2 smf.MetalMap
    [| for z in 0 .. mh - 1 do
        for x in 0 .. mw - 1 do
            let v = smf.MetalMap.[x, z]
            if v > 30uy then
                let wx = float32 x * 16.0f + 8.0f
                let wz = float32 z * 16.0f + 8.0f
                yield (wx, 0.0f, wz, float32 v / 100.0f) |]
    |> Array.sortBy (fun (x, _, z, _) ->
        let (cx, _, cz) = commanderPos
        let dx = x - cx
        let dz = z - cz
        dx * dx + dz * dz)
    |> Array.truncate 4

let context : ResolveContext =
    { Grid = grid
      BaseCentre = commanderPos
      CommanderPos = commanderPos
      MetalSpotsNearest = metalSpots
      Chokepoints = []        // no chokepoint-dependent slots in defaultArmadaOpening
      UnitDefs = UnitDefCache.empty   // offline plans use a stub; real bot passes the live cache
      ExistingStructures = []
      Progress = BasePlan.emptyPlanProgress }

let resolved = BasePlan.resolvePlan BasePlan.defaultArmadaOpening context
for r in resolved do
    match r.Position, r.Failure with
    | Some (x, _, z), None ->
        printfn "[ok]   %-12s %-10s → (%.0f, %.0f)" r.Slot.Name r.Slot.DefName x z
    | _, Some failure ->
        printfn "[fail] %-12s %-10s → %A" r.Slot.Name r.Slot.DefName failure
    | _ -> ()
```

**Expected output** (Avalanche 3.4 with the default opening): all 5
slots `[ok]`.

```text
  [ok]   mex#1        armmex     @ (776, 248)
  [ok]   mex#2        armmex     @ (216, 248)
  [ok]   solar#1      armsolar   @ (700, 397)
  [ok]   solar#2      armsolar   @ (300, 397)
  [ok]   factory      armlab     @ (500, 747)
```

SC-004 verification.

## 5. Anti wall-in check

```fsharp
// Inside any of the examples above, once you have a resolved plan:
let existingFootprints =
    resolved
    |> List.choose (fun r ->
        match r.Position with
        | Some pos -> Some { Centre = pos; RadiusElmos = 32.0f; Tag = Some r.Slot.Name }
        | None -> None)

let proposed = {
    Centre = (500.0f, 350.0f, 397.0f)  // commander's own spot — silly proposal, for demo
    RadiusElmos = 48.0f
    Tag = Some "demo-wall"
}

match WallIn.wouldWallIn grid baseCentre existingFootprints proposed WallIn.defaultWallInQuery with
| WallIn.Passes -> printfn "demo-wall placement is safe"
| WallIn.Fails r -> printfn "demo-wall would wall in: %A" r
```

A proposal that covers the commander's own cell will fail `RequireMapEdgeExit` on most maps. A proposal 200 elmos east on open terrain should `Passes`.

## 6. End-to-end: run the refactored macro bot

**Prerequisite**: the map-cache JSON must exist. Run once per map
before the first bot iteration:

```bash
dotnet fsi scripts/examples/14-cache-map-analysis.fsx "Avalanche 3.4"
# Writes bots/trainer/map-cache/avalanche_3.4.json (~1.2 KB,
# 3 chokepoints precomputed).
```

Without this, the bot warmup logs
`[chokepoint] no cache at … — run scripts/examples/14-cache-map-analysis.fsx …`
and the `[defend]` chokepoint-target path falls back to the 023
nearest-enemy behaviour.

Then:

```bash
BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 024-smoke
```

**Expected run-dir artifacts** (reference: `024-macro-smoke-final` run,
commit `e6d39d1`):

- `phase_transitions.jsonl` contains the canonical
  `Opening → Production → Upgrade → Attack` sequence (same as 023 iter
  026). Frame numbers on Avalanche 3.4: `Opening→Production ≈ f=2725`,
  `Production→Upgrade ≈ f=10321`, `Upgrade→Attack ≈ f=16560`.
- `stdout.log` contains:
  - `[chokepoint] loaded N chokepoints from cache bots/trainer/map-cache/avalanche_3.4.json`
  - `[chokepoint] pos=(…) width=… id=… distFromBase=…` (top-5)
  - `[plan] resolved 5 slots (5 buildable now)`
  - `[plan] slot mex#1 (armmex) resolved @ (…)` through factory
  - `[attack] launching 12 combat units at target (3699,3601)`
  - `[attack] findPath skipped (no MapGrid)` — expected in the default
    runtime path (findPath is logged but not used for command emission)
  - `[defend] chokepoint pos=(X, Y) width=W` — fires if and only if a
    defend interrupt triggers during the run (NullAI critters usually
    don't approach; BARb/dev raiders definitely will)
- `result.json.cause = "commander-death-win-after-upgrade"` (matches
  023 SC-004 and 024 SC-006).
- `result.json.victory_signal = "engine-shutdown-gameover"`.
- `result.json.frames ≈ 20 000 – 21 000` on NullAI (within 5 % of
  iter 026's 21 013).

If the refactor surfaces a regression, each subsequent iteration MUST fix exactly one issue at a time per the 023 PLAYBOOK §2c rule — explicitly carried forward in clarification Q5.

## 7. Run the full test suite

```bash
dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug
```

**Expected results**:
- All synthetic-fixture unit tests pass.
- SMF integration tests pass if `~/.local/state/Beyond All Reason/maps/avalanche_3.4.sd7` exists.
- Surface-area baseline tests pass for all 5 new modules.
- Rush bot smoke (invoked separately via `run.sh`) still wins on NullAI.

## 8. Verify the rush bot didn't regress (FR-030)

```bash
bash bots/trainer/run.sh NullAI 024-rush-smoke
```

Must produce `outcome=win, cause=engine shutdown (reason=...), commander alive`. If not, the integration broke an assumption (usually a transitive type change) and the commit is rejected.

---

## Troubleshooting pointers

| Symptom | Likely cause | Fix location |
|---|---|---|
| `SmfParser.parseSd7 → Error ArchiveNotFound` | BAR not installed | Install BAR or use a synthetic fixture |
| `Error ExtractionFailed` | `bsdtar` missing | `apt install libarchive-tools` (Debian) or install a managed 7-zip lib (plan-phase swap) |
| `Error InvalidMagic` | Wrong file under `maps/*.smf` inside the archive | Check archive integrity; re-download from BAR rapid |
| `Pathing.findPath → Error NoRoute` on a map you expect to be reachable | `ownStructures` list has a collision bug | Log the footprints before the call; verify start/goal aren't inside a footprint |
| `findChokepoints → []` on a map with obvious chokepoints | Search radius too small or width threshold too tight | Bump `query.SearchRadiusElmos` or `query.MaxWidthElmos` |
| `resolvePlan` reports `WouldWallIn` unexpectedly | A previous slot was placed too aggressively | Check the slot order; earlier slots wall in later ones |
| Macro bot wins on NullAI but loses on BARb/dev | Not a feature-024 regression per se — expected for the first BARb/dev iteration. Classify via PLAYBOOK §12.2 | `bots/trainer/HISTORY.md` |

## Further reading

- Spec: `specs/024-tactical-map-primitives/spec.md` — US1..US5, FR-001..FR-031, SC-001..SC-010
- Research: `specs/024-tactical-map-primitives/research.md` — R1 (pathing), R2 (chokepoints), R3 (7-zip)
- Data model: `specs/024-tactical-map-primitives/data-model.md` — every entity's F# shape
- Contracts: `specs/024-tactical-map-primitives/contracts/*.md` — per-module `.fsi` contracts
- 023 PLAYBOOK §12: `bots/trainer/PLAYBOOK.md` — macro archetype + classification labels (the feature this builds on)
- Constitution: `.specify/memory/constitution.md` — Tier 1 obligations
