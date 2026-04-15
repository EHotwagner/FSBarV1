# Implementation Plan: Permanent, Committed Map Cache

**Branch**: `026-permanent-map-cache` | **Date**: 2026-04-15 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/026-permanent-map-cache/spec.md`

## Summary

Turn the trainer's map analysis cache from a gitignored, manually-baked scratch artifact into a committed, versioned artifact that every clone already has. Concretely: stop ignoring `bots/trainer/map-cache/*.json`, commit a cache file for each supported map, and gate loading on a manual `codeVersion` constant that contributors bump whenever map-analysis semantics change. Mismatches hard-abort the trainer (FR-006) with an actionable error pointing at the refresh command. No CI enforcement — the runtime abort plus contributor discipline is the backstop.

The technical approach factors out the existing duplicated serialization logic (inline in `scripts/examples/14-cache-map-analysis.fsx` and in `bots/trainer/bot_macro.fsx:1230-1285`) into a new public `FSBar.Client.MapCacheFile` module with a `.fsi` contract. Both the generator script and the trainer loader then become thin callers of that module. This eliminates the drift-prone parallel implementation, makes the cache format unit-testable, and keeps the new surface visible in the surface-area baseline per Constitution §II.

## Technical Context

**Language/Version**: F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints)
**Primary Dependencies**: Existing in-repo only — `FSBar.Client` (`MapGrid`, `SmfParser`, `Chokepoints`, `BasePlan`, `MapQuery`), BCL `System.IO.Compression` (already used for gzipped blobs), BCL `System.Text.Json` (already used by `14-cache-map-analysis.fsx`). **No new NuGet dependencies.**
**Storage**: Filesystem. Committed JSON files under `bots/trainer/map-cache/<safe-name>.json`, one per supported map. Each file is a self-describing record containing schema version, `codeVersion`, analysis parameters, source map identity, and gzip+base64 blobs for heightmap / slope map / resource map. Typical size 500 KB – 1 MB per map per the feature 025 notes; capped at ~1.5 MB/map and ~15 MB total by SC-005.
**Testing**: xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x. New `MapCacheFile*Tests.fs` files are added to the **existing** `src/FSBar.Client.Tests/` project (note the `src/` location, an existing convention from feature 007). The project already contains `SyntheticMapGrid.fs`, `SurfaceAreaTests.fs`, and `Baselines/`, so surface-area baseline enforcement for the new public module is already wired — only the baseline file itself needs updating.
**Target Platform**: Linux (Beyond All Reason dev environment, Arch Linux container image). Same as all prior features.
**Project Type**: Single F# library (`FSBar.Client`) plus its test projects and the `bots/trainer/` scripting tree. No new projects required.
**Performance Goals**: <25 ms cache-load time end-to-end in the trainer warmup path (SC-002), versus the ~250 ms on-demand path today. Refresh-command runtime is not a hot path — ~5 s per map is acceptable.
**Constraints**: <1.5 MB per committed cache file on average; <15 MB total contribution to the repository across the supported-map set (SC-005). Deterministic serialization so that two back-to-back refreshes produce zero git diff (SC-004). No network calls at load time.
**Scale/Scope**: Today's supported-map set is `{Avalanche 3.4}` (one file). Designed to scale smoothly to ~10 maps without requiring Git LFS or any repo-layout changes.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|---|---|---|
| I. Spec-First Delivery | ✅ PASS | Spec exists (`specs/026-permanent-map-cache/spec.md`), clarified, three user stories with acceptance criteria, ten FRs, six SCs. This plan is Tier 1 (new public API surface) and will produce the full artifact chain. |
| II. Compiler-Enforced Structural Contracts | ✅ PASS | The new `MapCacheFile` module will ship with a `MapCacheFile.fsi` signature file and the public surface-area baseline for `FSBar.Client` will be refreshed in the same PR. No symbols leak without signature declaration. |
| III. Test Evidence Is Mandatory | ✅ PASS | Phase 1 contracts/tests cover roundtrip (US1), `codeVersion` mismatch abort (US2), schema-version mismatch abort (edge case), corrupted-file abort (edge case), missing-file abort (edge case), and deterministic regeneration (SC-004). All six tests fail against the current code (no module exists) and pass once implemented. |
| IV. Observability and Safe Failure Handling | ✅ PASS | FR-006 is a hard abort with a structured error that names file, mismatch kind, expected vs. found values, and the refresh command. The loader returns a discriminated-union error type so callers can render diagnostics uniformly. No silent fallback anywhere. |
| V. Scripting Accessibility | ✅ PASS | The new module is part of the public `FSBar.Client` surface, so it is usable from FSI out of the box. `scripts/examples/14-cache-map-analysis.fsx` will be rewritten as a thin caller of `MapCacheFile.write`, preserving its numbered-example role and serving as living documentation. |

**Engineering constraints check**:

- F# on .NET exclusively: ✅ (no new language)
- Every public `.fs` module has a `.fsi`: ✅ (`MapCacheFile.fsi` is in scope)
- Surface-area baselines: ✅ (`FSBar.Client` baseline refreshed in this PR)
- Dependencies minimized: ✅ (zero new NuGet)
- `dotnet pack` to local NuGet store: ✅ (uses existing `pack-dev.sh`)
- gRPC/OpenAPI tooling: N/A (no new service surface)

No complexity-tracking entries required; no gate violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/026-permanent-map-cache/
├── plan.md              # This file
├── spec.md              # Clarified feature spec
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output — MapCacheFile record, error type, codeVersion
├── quickstart.md        # Phase 1 output — refresh / load flows for contributors
├── contracts/
│   ├── MapCacheFile.fsi # Target signature (authoritative contract)
│   └── error-cases.md   # Loader error-path acceptance table
└── tasks.md             # /speckit.tasks will produce this
```

### Source Code (repository root)

```text
src/FSBar.Client/
├── MapCacheFile.fs       # NEW — deterministic JSON codec, schema + codeVersion guards
├── MapCacheFile.fsi      # NEW — public contract (record shape, write/read, error DU)
├── MapCache.fs / .fsi    # UNCHANGED — in-memory session cache (distinct concern)
├── Chokepoints.fs        # UNCHANGED — source of chokepoint list
├── MapGrid.fs            # UNCHANGED — source of heightmap / slope / resource arrays
└── ...

src/FSBar.Client.Tests/                 # EXISTING xUnit project (not under tests/)
├── FSBar.Client.Tests.fsproj           # ADD new files to <ItemGroup>
├── SyntheticMapGrid.fs                 # REUSED for small-grid roundtrip fixtures
├── MapCacheFileRoundtripTests.fs       # NEW — write → read yields equal MapGrid + chokepoints
├── MapCacheFileVersionTests.fs         # NEW — schema mismatch + codeVersion mismatch aborts
├── MapCacheFileCorruptionTests.fs      # NEW — truncated / malformed JSON / blob corruption aborts
├── MapCacheFileDeterminismTests.fs     # NEW — two writes of same inputs are byte-identical
├── SurfaceAreaTests.fs                 # UNCHANGED — enforces the refreshed baseline
└── Baselines/
    └── FSBar.Client.baseline           # UPDATED — adds MapCacheFile public surface

tests/FSBar.LiveTests/                  # UNCHANGED in scope; no new tests here
└── ...

scripts/examples/
└── 14-cache-map-analysis.fsx           # REWRITTEN — thin caller of MapCacheFile.write

bots/trainer/
├── bot_macro.fsx                       # UPDATED — warmup uses MapCacheFile.read
├── map-cache/                          # NOW TRACKED (was ignored)
│   ├── README.md                       # NEW — how to refresh, when to refresh
│   └── avalanche_3_4.json              # NEW — committed cache for the P1 supported map
└── ...

bots/trainer/map-cache/refresh-all.sh   # NEW — single entry point per FR-004
.gitignore                              # MODIFIED — remove line 26 (cache ignore rule)
```

**Structure Decision**: Single F# library (`FSBar.Client`) plus its existing test and scripting infrastructure — same structure as every prior feature. The new `MapCacheFile` module extracts the serialization logic currently duplicated in two places into one public surface with a `.fsi` contract. The `bots/trainer/map-cache/` directory transitions from "gitignored scratch space" to "tracked artifact directory" via one `.gitignore` edit plus the committed `avalanche_3_4.json`. One new shell script `refresh-all.sh` is added; it loops over `MapCacheFile.supportedMaps` and calls the existing per-map FSI script for each. Tests live in the existing `src/FSBar.Client.Tests/` project (feature-007 convention) — no new test project is introduced, and the existing `SurfaceAreaTests.fs` + `Baselines/` infrastructure already gates the new public surface.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

_No violations. No entries._
