# Phase 0 Research — 024 Tactical Map Primitives

**Branch**: `024-tactical-map-primitives`
**Date**: 2026-04-13
**Purpose**: Resolve the three technical unknowns flagged during plan-phase Technical Context drafting. Each research topic maps to at least one FR group from the spec.

## R1 — Pathing algorithm family

**Question**: What graph-search algorithm satisfies FR-001..FR-006a at the 512×512-cell / 50 ms-budget scale we need?

### Decision

**A\*** over heightmap cells, with an octile (8-direction) neighbour model, Manhattan-weighted heuristic, and edge cost = `distance × (1 + slope × slopeCost)`. The `ownStructures` mask (Q3) becomes impassable cells alongside cliffs and water.

### Rationale

- **Scale fits the budget**. Avalanche 3.4 = 512×512 = 262144 heightmap cells. A realistic A\* run with a good heuristic expands ~1–5% of the cell set for a typical cross-map query → ~3k–13k cell expansions. At a few microseconds per expansion on a modern CPU, that's well under 50 ms wall-clock even in interpreted F#.
- **Slope weighting is a natural fit**. A\*'s edge cost is scalar; we just multiply `distance` (either 1.0 for cardinal or √2 for diagonal) by `1 + slope × slopeCost`. The admissibility invariant (h(n) ≤ true cost) holds as long as the heuristic uses the minimum possible edge cost (distance × 1.0), which it does.
- **Deterministic ordering is trivial**. Use a priority queue keyed by `(f, tie-breaker)` where the tie-breaker is the cell's linearised index. Two identical inputs → identical traversal order → identical path. Satisfies FR-003.
- **Budget enforcement is one line**. Check `Stopwatch.ElapsedMilliseconds > budget` every N expansions; if over, abort with the best partial-path found so far and the `budgetExhausted` flag. Satisfies FR-005.
- **Passability is externally supplied**. The `MapGrid.passability grid moveType` helper already produces `bool[,]` of the exact shape we need; the `ownStructures` mask is overlaid by rasterising each footprint into the same index space and flipping cells to false. No custom graph construction — we walk the 2D array directly.

### Alternatives considered

- **Jump Point Search (JPS)**. Faster in theory for uniform-cost grids, but the slope-weighted edges break the core invariant (JPS assumes cost is purely distance-based so it can skip cells along straight lines). Would need JPS+ or a uniform-cost variant, which negates the main win. Rejected.
- **Dijkstra**. Strictly slower than A\* with an admissible heuristic, with no other benefit. Rejected.
- **Hierarchical Pathing (HPA\*)**. Precompute region graph → solve at the coarse level → refine. Excellent for very large maps (2048×2048+) but the upfront precompute cost doesn't pay back at 512×512 scale and adds a whole new data structure (regions) to maintain across `ownStructures` changes. Rejected as overkill.
- **Navigation meshes**. Natural for agent path planning but require a meshing pass over the terrain (CDT or similar). Would double the scope of this feature. Rejected.
- **Flow field pathing**. Cheap multi-agent pathing from a single goal (e.g., attack-move for 12 peewees toward one target). Attractive for the attack-phase use case. **Deferred**: mention in `tasks.md` as a possible optimisation for US5 if the per-unit A\* runs prove too slow when 12 units each solve independently. Initial implementation uses plain A\* per unit.

### Implementation sketch (for Phase 1 + tasks)

```text
module Pathing =
    let private neighbourOffsets = [| (-1,-1); (-1,0); (-1,1); (0,-1); (0,1); (1,-1); (1,0); (1,1) |]
    let private stepCost (dx,dy) slope slopeCost =
        let d = if dx*dx + dy*dy = 1 then 1.0f else 1.4142135f
        d * (1.0f + slope * slopeCost)

    let findPath (grid: MapGrid) (moveType: MoveType) (ownStructures: OwnStructureFootprint seq) start goal : Path =
        let passable = overlayFootprints (MapGrid.passability grid moveType) ownStructures
        let openSet = PriorityQueue<int, float32>()  // keyed by (g+h, linearised idx)
        ...
```

Default `slopeCost = 2.0f` (so a 0.5 slope cell costs 2× a flat cell). Exposed as an optional parameter for tuning.

## R2 — Chokepoint detection algorithm

**Question**: What algorithm produces stable, width-annotated chokepoint descriptors satisfying FR-007..FR-011?

### Decision

**Distance-transform ridges**. Compute a distance-to-nearest-impassable transform over the passability grid, then identify cells where the transform value is locally maximal along ridges that separate inside-base-radius from outside. Width estimate = `2 × distanceTransform[ridgeCell]` (in elmos, after multiplying by the 8-elmo cell size).

### Rationale

- **Single-pass cost**. A distance transform over a 512×512 grid is O(N) with two-pass Felzenszwalb-Huttenlocher. That's ~260k cell visits, well under the 200 ms target.
- **Width falls out naturally**. Distance-transform value at a ridge cell = distance to the nearest impassable edge. Doubling it gives you the corridor's narrow-point width in grid cells; multiplying by 8 elmos/cell gives world-space width. No separate width-estimation pass needed.
- **Determinism is trivial**. The distance transform is purely functional over the passability grid. Ridge detection is a local-maximum filter along the transform — deterministic per input. FR-011 stable IDs = hash of `(ridgeCellIndex, gridVersion)` or simply the linearised ridge cell index (stable as long as the grid is).
- **"Primary route" filter is cheap** (FR-010). A ridge cell is a chokepoint if and only if removing it (marking as impassable) disconnects base-centre from a significant portion of the region the cell currently connects to. We can approximate this with a quick flood-fill-from-base-centre, comparing region sizes with-and-without each candidate ridge cell. For a radius-2500-elmo search, the candidate set is typically <50 cells → ~50 small flood fills → still fast.
- **Handles the "open terrain" edge case correctly** (FR-009). If no ridge cell has a transform value below the configurable `maxChokepointWidth` threshold (default 40 elmos = 5 cells), the filter drops all candidates and the function returns `[]`. No fabricated "least-wide point".

### Alternatives considered

- **Articulation points on a region graph**. Partition the map into convex regions → build a region-adjacency graph → find articulation points (edges whose removal disconnects the graph). Elegant graph-theoretic answer but requires a region-partitioning pass (Tarjan's algorithm after graph construction), and the width estimate is not a natural output. Rejected as more plumbing than distance transform for the same quality.
- **Flood-fill from inside vs outside, intersect, narrow-band detection**. Cheap for "primary" detection (outside flood-fill hits the inside via the chokepoint) but doesn't directly give width. Would need a secondary distance-transform pass anyway. Rejected because it's strictly weaker than the distance-transform approach.
- **Voronoi partitioning of the passable region**. Edges of the Voronoi diagram of impassable cells are the "centres" of corridors. Classic computer-graphics chokepoint approach but requires a Voronoi library or significant custom code. Overkill. Rejected.
- **Human-annotated chokepoint data baked into map metadata**. Would require BAR to expose chokepoints as engine-side metadata (it doesn't). Rejected.

### Implementation sketch

```text
module Chokepoints =
    let findChokepoints
        (grid: MapGrid)
        (moveType: MoveType)
        (baseCentre: float32 * float32 * float32)
        (searchRadius: float32)
        : Chokepoint list =
        let passable = MapGrid.passability grid moveType
        let dt = distanceTransform passable             // O(N) two-pass
        let candidates =
            findLocalMaxima dt                          // ridge cells
            |> Array.filter (insideSearchRadius baseCentre searchRadius)
            |> Array.filter (widthBelowThreshold dt maxWidthElmos)
        let primary =
            candidates
            |> Array.filter (disconnectsBaseIfRemoved passable baseCentre)
        primary
        |> Array.sortBy (distanceFromBaseCentre baseCentre)
        |> Array.map toChokepointDescriptor
        |> Array.toList
```

Default `maxChokepointWidth = 40.0f` elmos (5 heightmap cells). Default `searchRadius = 2500.0f` elmos.

## R3 — 7-zip (`.sd7`) extraction mechanism

**Question**: How do we extract the `.smf` file from a BAR `.sd7` archive to satisfy FR-024 / FR-028 without adding a new NuGet dependency?

### Decision

**Shell out to `bsdtar`** (libarchive) via `System.Diagnostics.Process`. One process invocation per SMF parse call, extracting to a temp directory, reading the `.smf` bytes, and cleaning up the temp directory.

### Rationale

- **Already present on the dev image**. `bsdtar` was verified during Q2 investigation — `which bsdtar` returns `/usr/sbin/bsdtar` with libarchive-backed 7-zip support. Feature 012 (`minimal-container-setup`) confirms the container image also includes libarchive.
- **Zero new dependencies**. The constitution Engineering Constraints require each new NuGet dep to have "a stated need, version pinning strategy, and maintenance owner". Adding SharpCompress (the obvious managed 7-zip library) would need that paperwork; shelling out to a system tool that already ships with the OS does not.
- **`.sd7` = 7-zip is well-established**. Spring engine's `.sd7` maps are plain 7-zip archives with a canonical internal layout (`maps/<mapName>.smf` is the native map file). `bsdtar -xf <archive> -C <tempdir> 'maps/*.smf'` extracts exactly the one file we need, in <100 ms for Avalanche 3.4.
- **Failure mode is already covered**. `System.Diagnostics.Process` with exit-code check + stderr capture gives us the FR-027 "descriptive error" semantics for free: if `bsdtar` fails (missing file, bad archive, extraction error), we raise a `Failure` with the stderr output attached.
- **Matches existing patterns**. The trainer `run.sh` script already shells out to multiple system tools (`jq`, `dotnet`, `pkill`, engine binary). Adding one more process invocation inside a compiled F# module is not a category change.

### Alternatives considered

- **SharpCompress (managed 7-zip)**. Cross-platform pure-.NET, ~2 MB nupkg, stable. **Rejected** because it adds a new NuGet dependency subject to the constitution's "each new dep requires stated need + pinning strategy + maintenance owner" rule. The need is not sufficient vs. the `bsdtar` path.
- **SevenZipExtractor**. Windows-only COM wrapper. Rejected (Linux-only dev environment).
- **Pure F# 7-zip decoder**. LZMA and the 7-zip container format are complex enough that a from-scratch implementation would dwarf the rest of this feature. Rejected.
- **Pre-extract `.smf` files and commit them**. Would mean committing ~20–60 MB per map to the repo. Rejected — spec Q2 explicitly declined the binary-fixture approach in favour of on-the-fly parsing.
- **Use the `dotnet` built-in zip support**. `System.IO.Compression.ZipFile` handles plain ZIP but **not** 7-zip's LZMA-compressed containers. Rejected (wrong container format).

### Implementation sketch

```text
module SmfParser =
    let private extractSmfToTemp (sd7Path: string) : string =
        let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Directory.CreateDirectory(tempDir) |> ignore
        let psi = ProcessStartInfo(
                    FileName = "bsdtar",
                    ArgumentList = [| "-xf"; sd7Path; "-C"; tempDir; "maps/*.smf" |],
                    RedirectStandardError = true,
                    UseShellExecute = false)
        use p = Process.Start(psi)
        p.WaitForExit()
        if p.ExitCode <> 0 then
            let err = p.StandardError.ReadToEnd()
            failwithf "[SmfParser] bsdtar failed extracting '%s': %s" sd7Path err
        let smfPath =
            Directory.GetFiles(tempDir, "*.smf", SearchOption.AllDirectories)
            |> Array.tryHead
            |> Option.defaultWith (fun () ->
                failwithf "[SmfParser] no .smf file found inside %s" sd7Path)
        smfPath

    let parseSd7 (sd7Path: string) : SmfMap =
        let smfPath = extractSmfToTemp sd7Path
        try
            let bytes = File.ReadAllBytes(smfPath)
            parseSmfBytes bytes
        finally
            try Directory.Delete(Path.GetDirectoryName(smfPath), true) with _ -> ()
```

The SMF binary format itself (header + int16 heightmap + uint8 metal map + type map) is documented in the Spring engine source (`rts/Map/SMF/SMFFormat.h`) and is straightforward to parse with `BinaryReader`. Research detail is intentionally light here because the format is stable and well-known — the parser is a tasks.md-level implementation concern, not a design question.

### Slope map computation (FR-026)

The Spring engine derives the slope map from the heightmap via a 2×2 cell-corner averaging kernel. The formula, per `rts/Map/ReadMap.cpp`:

```text
for each 2x2 heightmap square:
    dx = (h[x+1,y] + h[x+1,y+1]) - (h[x,y] + h[x,y+1])
    dy = (h[x,y+1] + h[x+1,y+1]) - (h[x,y] + h[x+1,y])
    slope = sqrt(dx^2 + dy^2) / (2 * HEIGHTMAP_TO_ELMOS)
```

Output dimensions: `(widthHeightmap / 2) × (heightHeightmap / 2)`, matching the `getSlopeMap` callback's shape exactly. The SMF parser replicates this formula in F#. Any deviation from the engine's numerical output surfaces in SC-010 as a min/max diff greater than the ±1 elmo tolerance.

## Summary table

| Topic | Decision | FR coverage | SC impact |
|---|---|---|---|
| Pathing algorithm | A\* with octile neighbours + slope-weighted edges + `ownStructures` mask | FR-001..FR-006a | SC-001, SC-002 |
| Chokepoint detection | Distance-transform ridges with "primary route" filter + 40-elmo width threshold | FR-007..FR-011 | SC-003 |
| `.sd7` extraction | Shell out to `bsdtar` via `System.Diagnostics.Process` | FR-024, FR-027, FR-028 | SC-010 |

All three research tasks **resolved**; no remaining `NEEDS CLARIFICATION` markers.
