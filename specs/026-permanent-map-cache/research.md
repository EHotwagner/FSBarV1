# Phase 0 Research: Permanent, Committed Map Cache

**Feature**: 026-permanent-map-cache
**Date**: 2026-04-15

This document records the ground-truth investigation that feeds into the Phase 1 data model, contracts, and tasks. Each finding is followed by the design decision it drives and the alternatives considered.

---

## R1. Current cache format vs. SC-004 (byte-identical regeneration)

**Investigation**: Read `scripts/examples/14-cache-map-analysis.fsx:212-233` and the warmup loader at `bots/trainer/bot_macro.fsx:1220-1270`.

**Finding**: The current JSON payload contains **two fields that break byte-for-byte determinism across machines and across runs**:

1. **`sd7Path`** (line 215) — a fully qualified path like `/home/developer/.local/state/Beyond All Reason/maps/avalanche_3.4.sd7`. Two contributors on differently-named accounts produce two different cache files from identical source code and identical `.sd7` bytes.
2. **`generatedAtUtc`** (line 227) — an ISO-8601 timestamp of the moment the script ran. Every single regeneration produces a different file, even on the same machine within the same minute.

Both fields are **informational only** — neither the warmup loader (`bot_macro.fsx:1220-1270`) nor the chokepoint-consuming code reads them. They exist purely for human diagnostics.

**Decision**: The new schema (schemaVersion ≥ 2) **drops `sd7Path` and `generatedAtUtc` entirely**. The only map-identifying information retained is the canonical `mapName` and the parsed-out dimensions (`widthHeightmap`, `heightHeightmap`, `widthElmos`, `heightElmos`) which are a function of the map itself, not of who generated the file when.

**Rationale**: SC-004 requires two back-to-back refreshes on an unchanged source tree to produce a zero-byte git diff. Removing the two nondeterministic fields is the most direct path. Human diagnostics are not lost: the file name already tells you which map it is, and `git log` already tells you who regenerated it when.

**Alternatives considered**:
- *Keep `sd7Path` but normalize to a relative path or basename.* Rejected — still couples the file to the contributor's directory layout and doesn't help anyone; the `mapName` already identifies the source uniquely within the supported-map set.
- *Keep `generatedAtUtc` but zero it out to `"1970-01-01T00:00:00Z"` in deterministic mode.* Rejected — a constant literal is worse than removing the field.
- *Put both in a sibling `diagnostics.json` that is gitignored.* Rejected — two files to manage for no benefit.

---

## R2. JSON key ordering and deterministic serialization

**Investigation**: The current script builds payloads as `dict [...]` (F#'s `Dictionary<string, obj>` constructor). System.Text.Json's handling of `IDictionary<string, object>` iterates the dictionary in its natural enumeration order. In .NET 6+ `Dictionary<K,V>` enumeration happens to preserve insertion order in practice, but **this is an implementation detail, not a contractual guarantee** — the BCL docs explicitly state "The order in which the items are returned is undefined." A future runtime update could break SC-004 silently.

**Decision**: The new `MapCacheFile` module serializes **concrete F# record types**, not dictionaries of objects. Record field order is fixed at compile time by declaration order, and System.Text.Json serializes records in declaration order (`JsonPropertyOrder` is irrelevant when every property has a fixed declaration). The record type is defined once in `MapCacheFile.fsi` and the serializer is parameterless.

**Rationale**: Guaranteeing SC-004 requires guaranteed property order. Records give us that for free and also give us compile-time schema documentation; the data-model doc can literally reference the record declaration. As a bonus, swapping away from `Dictionary<string, obj>` removes the heterogeneous boxing that made the old code mildly awkward to read.

**Alternatives considered**:
- *SortedDictionary with a stable comparer.* Rejected — alphabetical order is less readable than logical/declaration order, and doesn't play nicely with the nested objects we want (R3).
- *Custom JsonWriter that emits fields in a hand-rolled order.* Rejected — no upside over records, much more code.
- *Pre-serialize to a canonical string and compare as strings.* Rejected — solves the symptom, not the cause.

---

## R3. Key naming: flat-with-dots vs. nested

**Investigation**: The current script uses flat keys with literal dots: `"baseCentre.x"`, `"heightMap.gzip.b64"`, `"position.x"` (script lines 132-140, 201-209, 220-222). The loader reads them the same way with `GetProperty("baseCentre.x")` (`bot_macro.fsx:1224`). This is unusual — it treats JSON property names as a flat namespace with dots used as pseudo-nesting punctuation. It works, but it's ugly, non-idiomatic, and makes manual inspection harder than it needs to be.

**Decision**: The new schema uses **real JSON nesting** via nested records: `{"baseCentre": {"x": ..., "y": ..., "z": ...}}`. This bumps the on-disk shape (hence `schemaVersion: 2`) but the previous schema was never committed to the repo, so there is zero legacy data to migrate — the very first committed cache already uses the new shape.

**Rationale**: Real nesting is cheap (records-in-records serialize to nested objects automatically), cleaner to read, and makes the data-model document one-to-one with the wire format. Since the feature also introduces the `codeVersion` field and drops two other fields, a schema bump is already required — rolling these improvements into the same bump costs nothing extra.

**Alternatives considered**:
- *Preserve flat-with-dots for backwards compatibility.* Rejected — no deployed consumer of the old format. The only "legacy" is a single script and a single loader inside this same repo, both rewritten in this feature.

---

## R4. `codeVersion` location and bump discipline

**Investigation**: Per the clarified spec, the loader compares a manual integer `codeVersion` and hard-aborts on mismatch. The question research needs to answer is: *where in the source tree does the constant live, and how is its authority established?*

**Decision**: The `codeVersion` constant is declared **exactly once**, in `src/FSBar.Client/MapCacheFile.fsi`, as a `val codeVersion: int` with an accompanying doc comment that describes what changes require a bump. The generator script reads it from the same module it writes through, so there is no way to produce a file with a `codeVersion` different from the one the loader expects at that moment. The loader reads the same constant. The contributor bumps the value in `MapCacheFile.fsi` (and mirrors it in `.fs`) as part of any PR that changes map-analysis semantics in `Chokepoints.fs`, `BasePlan.fs`, `MapQuery.fs`, `MapGrid.fs`, `SmfParser.fs`, or the `MapCacheFile` codec itself.

The doc comment enumerates the surface:

```fsharp
/// The current code version for map-analysis cache compatibility.
///
/// BUMP THIS INTEGER (by +1) in the same PR whenever you change any of:
///   - Chokepoints.fs / BasePlan.fs / WallIn.fs — analysis semantics
///   - MapGrid.fs / SmfParser.fs / MapQuery.fs — primitive extraction
///   - MapCacheFile.fs — the codec itself (write/read/blob format)
///
/// After bumping, run bots/trainer/map-cache/refresh-all.sh and commit
/// the regenerated cache files in the same PR.
val codeVersion: int
```

**Rationale**: The spec was explicit that the mechanism must be manual. The open question was "how do we make the manual bump hard to forget in code review." The answer is threefold: (1) put the constant in a single signature file so there is one authoritative diff to look for; (2) write the bump rule into the doc comment on the constant itself so touching it opens the rulebook; (3) rely on FR-006 (runtime hard-abort) as the ultimate backstop if the comment isn't enough.

**Alternatives considered**:
- *Put `codeVersion` in a separate `MapAnalysisVersion.fs` file.* Rejected — more indirection, no benefit.
- *Derive the version from the git history of specific files (`git log --oneline src/FSBar.Client/Chokepoints.fs | wc -l`).* Rejected — couples the source code to the build environment, produces different values in a shallow clone, impossible to reason about in code review.
- *Use a string like `"2026-04-15.a"` instead of an integer.* Rejected — strings invite free-form edits that don't compare correctly (`"2"` vs `"10"`). An integer is unambiguous.

---

## R5. Per-map inputs: base centre and chokepoint query parameters

**Investigation**: The current generator hardcodes `baseCentre = (500f, 0f, 397f)` and a chokepoint query with `MaxWidthElmos = 240f`, `SearchRadiusElmos = 5500f` (script lines 101-106). Comments explicitly call out that these are "a fixed function of the map + player-1 start slot" — a different map needs different values.

**Finding**: The supported-map set is currently a single map, so the hardcoding has not yet bitten anyone. But FR-008 requires the supported-map set to live "in a single place in the repository" so that *adding* a map is a one-line change. The per-map parameters must therefore travel with the map-name declaration, not be hardcoded in the generator.

**Decision**: Introduce a `SupportedMap` record in `MapCacheFile.fsi` with fields:

```fsharp
type SupportedMap = {
    MapName: string                        // canonical map name, e.g., "Avalanche 3.4"
    Sd7FileStem: string                    // filename stem for .sd7 lookup, e.g., "avalanche_3.4"
    BaseCentre: float32 * float32 * float32
    ChokepointQuery: Chokepoints.ChokepointQuery
}
```

and a `val supportedMaps: SupportedMap list` holding the canonical set. Adding a new supported map is a one-line addition to this list; removing one is a one-line removal; reviewing the set is reading one declaration. Both the refresh command and the trainer warmup consult this list as the single source of truth.

**Rationale**: This satisfies FR-008 cleanly and avoids inventing a separate `supported-maps.json` file (which would need its own parser and its own determinism guarantees). The F# list is both the declarative source and the runtime value.

**Alternatives considered**:
- *Separate `supported-maps.json` file.* Rejected — more moving parts, another parser.
- *Leave per-map parameters in the generator script.* Rejected — violates FR-008 the moment a second map is added.
- *Derive `BaseCentre` from the map's start positions automatically.* Rejected — start positions are map-defined but the "canonical top-left start area" heuristic the trainer uses today (spec comment, script line 99) is not mechanically derivable; it's a trainer-side convention that has to be declared, not computed.

---

## R6. Deterministic gzip output

**Investigation**: `CompressionLevel.Optimal` with `GZipStream` in .NET is deterministic for a fixed input byte sequence **and a fixed runtime version**. Between .NET major versions the exact gzip output can theoretically change (the zlib implementation underneath can be tuned), but FSBarV1 pins to .NET 10.0 via `<TargetFramework>net10.0</TargetFramework>` across every project, so the only cross-version risk is a future .NET upgrade — which would be a deliberate, tracked event.

**Decision**: Use `CompressionLevel.Optimal` for all compressed blobs (heightmap, slope map, resource map). Write all floats with `BitConverter.GetBytes(float32)` in the host's byte order (little-endian on all supported targets — x86_64 Linux and any .NET environment we care about). Record the endianness implicitly via the `codeVersion` constant: if we ever change the codec to enforce explicit little-endian for cross-architecture safety, that's a bump.

**Rationale**: Zero additional complexity, deterministic in practice, and the one theoretical risk (runtime version bump) is naturally covered by SC-004's own test (any drift would show up as a diff the moment it happened, and a `codeVersion` bump + refresh would heal it).

**Alternatives considered**:
- *Brotli or zstd for better compression.* Rejected — zero new dependencies is a hard constraint, and gzip at Optimal is already within the size budget.
- *Raw uncompressed float arrays.* Rejected — ~4× larger, blows past SC-005's 1.5 MB/map budget on 1024-square maps.

---

## R7. Test project location

**Investigation**: The repo already has a pure-unit-test xUnit project at `src/FSBar.Client.Tests/` (note: under `src/`, not `tests/` — this is an existing convention from feature 007). It already contains `SyntheticMapGrid.fs` (constructs fake `MapGrid` values in-memory for tests that don't need a real `.sd7`), `ChokepointsTests.fs`, `BasePlanTests.fs`, `SmfParserTests.fs`, `SurfaceAreaTests.fs` (enforces the `FSBar.Client` surface-area baselines from `src/FSBar.Client.Tests/Baselines/`), and references `FSBar.Client.fsproj` directly. Fast unit tests for pure-F# modules are already its job.

**Decision**: Add the new `MapCacheFile*Tests.fs` files to the existing `src/FSBar.Client.Tests/` project. No new test project is created. `SyntheticMapGrid.fs` is reused for the synthetic-grid test cases; the roundtrip test constructs a small grid, writes it, reads it back, and asserts equality.

**Rationale**: The existing project is already the designated home for this kind of test, already has surface-area baseline enforcement (so R8 requires only a baseline file update, not new test infrastructure), and already has a `Baselines/` directory. Inventing a parallel project would be pure churn.

**Alternatives considered**:
- *Add the tests to `FSBar.LiveTests`.* Rejected — gates fast tests behind `check-prerequisites.sh` and slows the feedback loop.
- *Create a new `tests/FSBar.Client.Tests` project under `tests/`.* Rejected — would duplicate the existing project and orphan the existing surface-area baseline infrastructure.

---

## R8. Impact on the `FSBar.Client` surface-area baseline

**Investigation**: Per Constitution §II, every public F# module has a curated `.fsi` signature file and a surface-area baseline. `MapCacheFile` will be a new public module, so the baseline will grow. The spec has no preferences about baseline churn beyond the usual "baselines must be updated in the same PR."

**Decision**: The implementation PR refreshes the `FSBar.Client` surface-area baseline file in the same commit as the new `MapCacheFile.fs`/`.fsi`. A failing baseline test before the refresh is expected and documented in tasks.

**Rationale**: Standard workflow for any new public surface in this repo.

---

## R9. Scope of committed cache on this feature branch

**Investigation**: Today zero cache files are on disk (the investigation earlier this session confirmed `bots/trainer/map-cache/` does not exist in the working tree). The P1 user story requires the committed cache to exist for at least one supported map (Avalanche 3.4).

**Decision**: The feature branch commits exactly one cache file: `bots/trainer/map-cache/avalanche_3_4.json`. The `refresh-all.sh` script supports regenerating any subset; adding a second supported map is covered by US3 and is explicitly a follow-up, not part of this feature.

**Rationale**: Minimizes the scope of the feature branch. The P1 story becomes testable on a clean checkout the moment that single file lands.

---

## Summary of decisions

| ID | Decision |
|---|---|
| R1 | Drop `sd7Path` and `generatedAtUtc` from the payload — both break SC-004 |
| R2 | Serialize concrete F# records (not `dict`s) for guaranteed field order |
| R3 | Use real JSON nesting instead of flat-with-dots keys |
| R4 | `codeVersion: int` declared in `MapCacheFile.fsi`, doc-commented with bump rules |
| R5 | `SupportedMap` record + `supportedMaps` list as the single source of truth |
| R6 | Gzip `Optimal` on little-endian float bytes; determinism pinned to .NET 10.0 |
| R7 | Tests go into the existing `src/FSBar.Client.Tests/` project (no new project) |
| R8 | Surface-area baseline refreshed in the same PR |
| R9 | Commit exactly one cache file (`avalanche_3_4.json`) on this branch |

No unresolved `[NEEDS CLARIFICATION]` remain.
