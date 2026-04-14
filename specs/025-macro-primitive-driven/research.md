# Phase 0 — Research: Macro Bot Primitive-Driven Command Path

**Feature**: 025-macro-primitive-driven
**Date**: 2026-04-14
**Inputs**: [spec.md](./spec.md), [plan.md](./plan.md)

Three research topics were opened by Phase 0 outline and are resolved below: R1 (MapGrid serialisation strategy for warmup load), R2 (Spring engine SHIFT_KEY option-bit value), R3 (findPath tactics-tick budget fit).

---

## R1 — MapGrid serialisation strategy for warmup load (US4, FR-014 / FR-015)

**Question**: Given the 100 ms warmup CPU budget (FR-015) and the hard-fail-on-cache-miss rule for the 025 target set (clarification Q2 / FR-014), how does the bot get a real `MapGrid` — with non-zero slope values and true heightmap — in place before `BasePlan.resolvePlan` runs? Two options on the table from the spec assumption (line 167): **(a) extend the offline cache file** (`bots/trainer/map-cache/<map>.json`) to carry the MapGrid blob alongside the chokepoint list, or **(b) inline re-parse** the `.sd7` via `SmfParser.parseSd7` at warmup on every iteration.

### Decision

**Extend the offline cache file** (`bots/trainer/map-cache/<map>.json`) with a base64-encoded gzip-compressed blob containing the heightmap, slope map, resource map, and map dimensions. Load + decompress + reconstruct the `MapGrid` at warmup from this blob. Keep `SmfParser.parseSd7` as the offline writer that populates the cache (already invoked by `scripts/examples/14-cache-map-analysis.fsx`) — the bot itself never calls `SmfParser` at warmup.

### Rationale

- **`SmfParser.parseSd7` cannot fit the 100 ms warmup budget.** Feature 024's plan.md §Performance Goals documents SMF parse of Avalanche 3.4 at "<500 ms wall-clock including bsdtar shell-out, one-time per bot warmup." The 025 spec assumption line 167 records an observed 1.2 s end-to-end in practice (archive extraction dominates). Both numbers are 5–12× the FR-015 100 ms budget — a direct inline re-parse is not viable, full stop.
- **A serialised blob is single-digit ms to load.** Avalanche 3.4 is a 512×512 map. The extended cache payload is:
  - `heightMap`: 513×513 `float32` = 263169 × 4 bytes ≈ 1.0 MB raw
  - `slopeMap`: 256×256 `float32` = 65536 × 4 bytes ≈ 0.25 MB raw (half-resolution per Spring's `getSlopeMap` convention, already documented in 024 research.md)
  - `resourceMap`: 256×256 `float32` = 65536 × 4 bytes ≈ 0.25 MB raw
  - Total raw: ~1.5 MB. Post-gzip (heightmaps compress well — smooth terrain, lots of near-identical neighbours): ~300–600 KB on disk.
  - Base64 embedded in JSON: ~400–800 KB text. File read + base64 decode + gzip decompress + `Array2D` reconstruction: 10–30 ms wall-clock on a local SSD. Comfortably under 100 ms with headroom for `resolvePlan` (< 1 ms) and chokepoint cache load (already < 5 ms in the 024 shipped bot).
- **The cache pipeline is already in place.** `scripts/examples/14-cache-map-analysis.fsx` already parses the `.sd7`, computes chokepoints, and writes the JSON cache. Extending the writer to also emit the MapGrid blob is a ~40-LOC delta (base64+gzip a few `float32` arrays, attach to the JSON). The operator workflow ("run 14-cache-map-analysis.fsx whenever you add a new target map") is unchanged.
- **Hard-fail on target-set cache miss stays clean.** FR-014 hard-fails on target-set maps without a cache file. With the cache-extension strategy, "cache file exists" and "MapGrid is present in cache" are the same check — no partial-cache ambiguity. Maps outside the target set either have a cache or log `[cache-miss] WARN` and degrade to the 024 synthetic skeleton (clarification Q2).
- **Backward compatibility is trivial.** The existing `avalanche_3.4.json` is field-addition-compatible — new `mapGrid.*` fields can be added without breaking the 024 reader (which consumes only `chokepoints.*`). Old caches that predate 025 trip the extended-field absence check and hard-fail with the documented "run 14-cache-map-analysis.fsx" message, which is the correct outcome because the operator must re-bake the cache anyway.

### Alternatives considered

- **(b) Inline `SmfParser.parseSd7` at warmup on every iteration**. Rejected. The measured 500 ms – 1.2 s parse cost is incompatible with the 100 ms FR-015 budget. Warmup would blow the socket-backpressure threshold and trip the catch-up OOM that blocked feature 024's US5 live attempts in the first place.
- **(c) Raw binary sidecar file** (`avalanche_3.4.mapgrid.bin`). Slightly faster load (no base64, no JSON parsing), but splits the per-map cache into two files the operator must keep in sync. Adds one failure mode (`chokepoint cache present, mapgrid cache missing`) that the clean-file-per-map design currently avoids. Marginal performance win not worth the operational cost.
- **(d) Persist `MapGrid` as a `System.Text.Json` object with `float[][]` arrays** (no gzip, no base64). Avoids the binary-encoding complexity, but Avalanche 3.4's heightmap alone is 263169 numeric fields — serialising that as JSON text inflates the file to ~5 MB and the parser cost approaches 100 ms on its own. Rejected as too slow.
- **(e) Pre-load the MapGrid in a separate warmup thread concurrent with UnitDefCache build**. Parallelism would hide the 500ms parse behind other warmup work, but the frame-reading path is still single-threaded per `BarClient` session, so the parse thread would still starve the socket. Also does not fix the 1.2 s parse cost — only masks it. Rejected.

### Risks & residual unknowns

- **Gzip determinism across .NET versions**: `System.IO.Compression.GZipStream` output is not byte-identical across runtimes, so the cache file's exact bytes may drift. Mitigation: cache file commits are not expected to happen (the file is `bots/trainer/map-cache/*.json` — not gitignored but functionally a generated artifact per operator run), so bit-exact stability is not required; content equivalence after decompression is what matters.
- **Extended cache file size**: Avalanche 3.4 post-extension JSON is ~400–800 KB vs the current ~2 KB. Git-tracking that file becomes more expensive. Mitigation: add `bots/trainer/map-cache/*.json` to `.gitignore` and document the operator workflow in `PLAYBOOK.md` §13. The existing 024 cache file can be moved to gitignored with one git-rm + one-line .gitignore edit.
- **`ResourceMap` / `LosMap` / `RadarMap`**: the synthetic skeleton zeroes all four. US1 only consumes heightmap + slope for the terrain-buildable + clearance checks; US2 only consumes slope for pathing cost. The cache can safely zero `LosMap` and `RadarMap` (they are runtime-populated anyway) and populate only `ResourceMap` from the SMF metal-map. Dimensions must still match `MapGrid` shape.

---

## R2 — Spring engine SHIFT_KEY option-bit value (US2, FR-008 / FR-008a)

**Question**: What is the correct option-bit value for the "queue this command after the unit's existing orders" semantic in the `Highbar.AICommand.Options` uint32 bitfield, so that a queued `MoveCommand` appends to the unit's order queue rather than replacing the current order?

### Decision

**`SHIFT_KEY = 32u`**. The queued `MoveCommand` variant on `FSBar.Client.Commands` OR's `SHIFT_KEY` (32u) into `Options` alongside the existing `INTERNAL_ORDER` (8u), producing `Options = 40u` on queued commands and `Options = 8u` on unqueued commands.

### Rationale

- **HighBarV2's `docs/protocol.md` is authoritative**. Lines 208/210/214 of that file document the option-bit table in exact terms:
  ```
  | INTERNAL_ORDER | 8  | Programmatic AI order (used by default) |
  | SHIFT_KEY      | 32 | Queue command                            |
  ```
  The F# client sets `INTERNAL_ORDER` (8) on all commands by default. The queued variant additionally sets `SHIFT_KEY` (32).
- **The C++ bridge round-trips option bits untouched**. HighBarV2's `Mailbox/2026-04-12_from_HighBarV2_contract-docs-response.md` line 150 shows the server code `s.options = (short)c->options;  // 8 (INTERNAL_ORDER)` — the protobuf `options` field is cast straight to Spring's native `Command::options` short, no bit remapping. This means Spring's native option-bit layout (per Spring RTS `rts/Sim/Units/CommandAI/Command.h`) is what the wire format carries, and Spring's native `SHIFT_KEY = 32` applies unchanged.
- **`FSBar.Client.Commands.fs` already encodes the right value for `INTERNAL_ORDER`**. Line 7 of Commands.fs has `let INTERNAL_ORDER = 8u`, which matches the protocol.md table. The same file passing that literal into every command builder (`Options = INTERNAL_ORDER`) is the working behaviour the 023/024 bot has shipped and won on NullAI with — proving on-wire correctness end-to-end.
- **The `proto/highbar/common.proto:18` comment is stale and should not be trusted.** That comment reads `// Bits: SHIFT_KEY=1, CTRL_KEY=2, ALT_KEY=4, META_KEY=8, INTERNAL_ORDER=16, RIGHT_MOUSE_KEY=32`, which directly contradicts both `protocol.md` and the C++ bridge. If that bit layout were on the wire, `Commands.fs` would be sending `META_KEY=8` on every unit command instead of `INTERNAL_ORDER`, and the bot's commands would fail the engine's "only INTERNAL_ORDER commands bypass the AI shared-state gate" check — which they manifestly do not, since the bot wins cleanly. Fixing the stale comment is **out of scope** for 025 (would touch the 024-frozen `proto/` tree by FR-021 spirit; also a doc-only change with no behavioural effect). Leave a note in the integration commit's body flagging the comment discrepancy for a future cleanup feature.

### Alternatives considered

- **Treat the proto comment as authoritative and use `SHIFT_KEY = 1u`**. Rejected. The comment contradicts three independent sources (the C++ bridge, protocol.md, the observed behaviour of 023/024 bots shipping `INTERNAL_ORDER = 8u` successfully). Using `SHIFT_KEY = 1u` would either have no effect or set the wrong flag.
- **Empirically probe via a one-off FSI script that fires queued moves and observes unit behaviour**. Useful belt-and-suspenders check, but `docs/protocol.md` + the bridge code are enough to decide. The operator can still run `scripts/examples/NN-queued-move.fsx` (delivered in Phase 1) as a visual confirmation during the integration iteration.

### Risks & residual unknowns

- **Option-bit semantics when combined**. `INTERNAL_ORDER | SHIFT_KEY = 40u`. Spring's engine treats option bits as independent flags, so the combination means "this is a programmatic AI order AND it should be queued behind the unit's existing orders." No risk of one flag suppressing the other.
- **First queued command behaviour**. Per FR-008, the first waypoint's `MoveCommand` is issued **unqueued** (Options = 8u) so it replaces any existing order (critical when the unit is mid-dispatch from `helpers/production_queue.fsx` issuing `MoveCommand` to the attack rally point). Remaining waypoints are queued (Options = 40u) and append to the now-single-entry queue.
- **Staircasing interaction with engine pathfinder**. Engine-side pathfinding between consecutive waypoints will still use the engine's own path solver, not `Pathing.findPath`. This is intentional: `findPath` picks waypoint coordinates on walkable ground, and the engine path-solves the short segment between them with its own grid. If the engine's solver picks a suboptimal route for a given segment, the observable bot-level failure mode is a unit taking a slightly longer path between two valid waypoints — not unit loss. Acceptable for a behaviour-preserving refactor.

---

## R3 — findPath tactics-tick budget fit (US2, FR-007 / FR-009)

**Question**: Does calling `Pathing.findPath` once per attack launch (not once per combat unit) fit inside the macro bot's existing per-tactics-tick budget on Avalanche 3.4, given the 50 ms default wall-clock budget documented in 024's research.md?

### Decision

**Yes, with margin**. One `findPath` call per attack launch, cached as `AttackPathCache`, consumed by all 12 combat units in the launch, invalidated per Q3 rules (target death / attack-phase end / new target). Budget unchanged at 50 ms default. No budget bump for the first attack-launch tick required.

### Rationale

- **Single call, not per-unit.** FR-007/FR-009 pin the bot to exactly one `findPath` per attack launch. The worst case is 50 ms once at the launch tick — not 12 × 50 ms per tick. This matches 024 research.md R1's "~3k–13k cell expansions at a few microseconds each" estimate, which easily fits inside 50 ms for an Avalanche 3.4-sized 512×512 map.
- **Q3 invalidation cost is bounded.** Target death re-path runs `findPath` once more in the same tick that the death is noticed. Worst case: one 50 ms spend on the death tick, amortised over many ticks of cached reuse. Cumulative pathing budget across a match: ≤ 50 ms × (number of distinct targets chased), which is a small number on NullAI smokes (target stays alive until commander-death).
- **Q4 cadence is cheap.** resolvePlan at ~1 sim-second cadence is < 1 ms per call. Cumulative across a 60 s Opening phase: < 60 ms — less than one `findPath` call. No interaction with the attack-launch `findPath` budget because Attack phase post-dates Opening phase.
- **Q5 no-retry policy bounds worst-case.** If `findPath` returns `Partial`, the bot does not retry next tick. Total pathing cost for the attack launch stays at 50 ms regardless of terrain difficulty. Predictable tactics-tick latency.

### Alternatives considered

- **Per-unit `findPath`**. Rejected already in FR-007/FR-009 — would produce 12 × 50 ms = 600 ms of contention per attack-launch tick, blowing the socket-backpressure threshold exactly like feature 024's US5 partial attempt did on MapGrid load. The one-path-cached design is specifically to avoid this.
- **Bump `findPath` budget to 100 ms for the first attack-launch tick**. Considered as a belt-and-suspenders measure but rejected. The 50 ms default is already comfortable on Avalanche 3.4 per 024 R1 analysis, and bumping the budget for one tick introduces a special case with no measured need. If practice shows 50 ms is too tight on a future larger map, the budget is a per-call parameter and can be raised without a spec change.
- **Precompute attack-path at warmup (alongside the MapGrid load)**. Tempting — zero tactics-tick cost — but the attack target is not known at warmup (it depends on `pickAttackTarget` over live `GameState.Units`), and the combat-group centre-of-mass is not known until combat units exist. Cannot fold into warmup. Rejected.

### Risks & residual unknowns

- **Larger maps (> 512×512)**. Feature 025 targets Avalanche 3.4 only (Out of Scope line 185). A 1024×1024 map would quadruple the A\* search space and may need a budget bump. Non-issue for this feature; flag for the future when the target-set expands.
- **12 units all trying to walk the same narrow corridor**. Engine-level pathing between waypoints handles this (units spread out into a lane naturally), and 024 research.md R2's chokepoint width field gives the bot visibility into narrow-corridor geometry if future tuning wants to thin the attack group. Out of scope for 025.

---

## Summary

| Topic | Decision | Spec FR mapping |
|---|---|---|
| R1 MapGrid serialisation | Extend `bots/trainer/map-cache/<map>.json` with base64-gzipped MapGrid blob; bot loads from cache, never inline-parses `.sd7` at warmup | FR-014, FR-015 |
| R2 SHIFT_KEY bit value | `SHIFT_KEY = 32u` (authoritative per HighBarV2 `docs/protocol.md`); stale `common.proto` comment left alone (out of scope) | FR-008, FR-008a |
| R3 findPath budget fit | One call per attack launch at 50 ms default, cached as `AttackPathCache`, Q3-invalidated | FR-007, FR-009, FR-009a |

All NEEDS CLARIFICATION items resolved. Phase 1 proceeds with `data-model.md`, `contracts/`, and `quickstart.md`.
