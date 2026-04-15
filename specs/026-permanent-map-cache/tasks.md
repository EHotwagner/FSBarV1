---
description: "Task list for 026-permanent-map-cache"
---

# Tasks: Permanent, Committed Map Cache

**Input**: Design documents from `/specs/026-permanent-map-cache/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/MapCacheFile.fsi`, `contracts/error-cases.md`, `quickstart.md`

**Tests**: INCLUDED. Constitution §III requires test evidence for any behavior-changing code; the plan's Phase 1 design enumerates the xUnit test suite explicitly and the `contracts/error-cases.md` file defines nine failure-mode tests plus three positive-case tests that must exist.

**Organization**: Tasks are grouped by user story. Phase 2 (Foundational) creates the skeleton of `MapCacheFile` so each user story can proceed in TDD order; each story then has its own tests-first / implementation-second slice.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: `[US1]`, `[US2]`, `[US3]` — maps to user stories in `spec.md`

## Path Conventions

Single F# library layout (FSBarV1). Paths used below:

- `src/FSBar.Client/` — library source with `.fs`/`.fsi` pairs
- `src/FSBar.Client.Tests/` — existing pure-unit-test xUnit project (feature-007 convention — note `src/`, not `tests/`)
- `bots/trainer/` — trainer bot scripts and map-cache directory
- `scripts/examples/` — numbered FSI example scripts

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Untrack the cache directory and prepare file-level scaffolding so subsequent work can commit artifacts into it.

- [X] T001 Remove the `bots/trainer/map-cache/*.json` exclusion from `.gitignore:26` (the only line change in that file). Keep the surrounding comment but update it to reference feature 026 and note that cache files are now tracked.
- [X] T002 Create `bots/trainer/map-cache/` directory in the working tree if it does not already exist. (Empty until Phase 3 commits the first cache file.)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Introduce the `MapCacheFile` module skeleton and wire it into the library and test projects. At the end of this phase, the project compiles with the new public surface declared but the `write`/`read`/`formatLoadError` function bodies left as `failwith "T###"` stubs. All downstream TDD test tasks can now write red tests against this skeleton.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete — every US3/US2/US1 task that touches `MapCacheFile.fs` depends on the skeleton existing.

- [X] T003 Create `src/FSBar.Client/MapCacheFile.fsi` by copying `specs/026-permanent-map-cache/contracts/MapCacheFile.fsi` verbatim. This is the authoritative public signature: `schemaVersion`, `codeVersion`, `SupportedMap`, `supportedMaps`, `tryFindSupportedMap`, `LoadError`, `LoadedMap`, `write`, `read`, `cachePathFor`, `formatLoadError`.
- [X] T004 Create `src/FSBar.Client/MapCacheFile.fs` implementing the types declared in `MapCacheFile.fsi` (concrete F# records `Vec3`, `ChokepointQuerySnapshot`, `ChokepointEntry`, `GzipBlob`, `Contents`, `SupportedMap`, `LoadedMap`; discriminated union `LoadError`), the constants `schemaVersion = 2` and `codeVersion = 1`, and stubs for every function that throw `failwith "T### not yet implemented"`. The file MUST compile against the signature.
- [X] T005 Implement `MapCacheFile.supportedMaps : SupportedMap list` in `src/FSBar.Client/MapCacheFile.fs` with a single initial entry for Avalanche 3.4. Copy the base-centre `(500.0f, 0.0f, 397.0f)` and chokepoint-query overrides (`MaxWidthElmos = 240.0f`, `SearchRadiusElmos = 5500.0f`) from `scripts/examples/14-cache-map-analysis.fsx:101-106`. Also implement `tryFindSupportedMap` (simple `List.tryFind` by `MapName`) and the `sanitise` helper + `cachePathFor` (mirroring the existing sanitiser at `scripts/examples/14-cache-map-analysis.fsx:56-59`).
- [X] T006 [P] Add two `<Compile Include="..." />` entries to `src/FSBar.Client/FSBar.Client.fsproj` — `MapCacheFile.fsi` MUST appear first, immediately followed by `MapCacheFile.fs` (the F# compiler requires signature-before-implementation order within the `<ItemGroup>`). Both entries are placed after `Chokepoints.fs` and `BasePlan.fs` so MapCacheFile's type references resolve.
- [X] T007 [P] Update the `FSBar.Client` surface-area baseline at `src/FSBar.Client.Tests/Baselines/FSBar.Client.baseline` (or whatever filename the existing `SurfaceAreaTests.fs` enforces — check by running the test once and observing the expected filename in the failure) to include the newly exposed `MapCacheFile` public symbols.
- [X] T008 Run `dotnet build src/FSBar.Client/FSBar.Client.fsproj` and confirm the project compiles with the new module. Then run `dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj --filter "FullyQualifiedName~SurfaceArea"` and confirm the refreshed baseline passes.

**Checkpoint**: The `MapCacheFile` skeleton compiles, the surface-area baseline is green, and the stubs are ready for tests to drive real implementations.

---

## Phase 3: User Story 1 - Trainer starts on a clean checkout without a manual bake step (Priority: P1) 🎯 MVP

**Goal**: A freshly cloned repository ships with a committed `avalanche_3_4.json` cache file and a trainer warmup that reads it through `MapCacheFile.read`. A developer can `git clone … && ./pack-dev.sh && ./run-trainer.sh` and reach the main loop without running any cache-generation script.

**Independent Test**: Delete any local cache directory contents outside what's committed, clean-check-out the branch on a machine that has never built it, build, and launch the trainer against Avalanche 3.4. The warmup log shows `[mapcache] loaded bots/trainer/map-cache/avalanche_3_4.json in <N>ms (codeVersion=1)` and the bot reaches its main loop without raising the "cache missing" hard-fail.

### Tests for User Story 1 (TDD — write these first, ensure they fail before moving to implementation)

- [X] T009 [P] [US1] Create `src/FSBar.Client.Tests/MapCacheFileRoundtripTests.fs` with a `[<Fact>]` that constructs a small synthetic `MapGrid` using `SyntheticMapGrid.fs` helpers (or an inline tiny grid: 8×8 heightmap, 4×4 slope map, 8×8 resource map, pre-seeded with distinct values), runs `Chokepoints.findChokepoints` on it to produce a chokepoint list, calls `MapCacheFile.write` to a temp path, calls `MapCacheFile.read` on the same path, and asserts: (a) `LoadedMap.Grid.HeightMap` equals the original cell-for-cell, (b) `LoadedMap.Grid.SlopeMap` equals the original, (c) `LoadedMap.Grid.ResourceMap` equals the original, (d) `LoadedMap.Chokepoints` equals the list passed to `write` (structural equality), (e) `LoadedMap.BaseCentre` equals the `SupportedMap.BaseCentre`. Add the file to `FSBar.Client.Tests.fsproj`. Test MUST fail (write/read are stubs).
- [X] T010 [P] [US1] Create `src/FSBar.Client.Tests/MapCacheFileIntegrationTests.fs` with a `[<Fact>]` that reads the committed `bots/trainer/map-cache/avalanche_3_4.json` file (located via `MapCacheFile.cachePathFor` from a repoRoot derived from the test assembly location) and asserts: (a) the read succeeds (`Ok`), (b) `LoadedMap.MapName = "Avalanche 3.4"`, (c) `LoadedMap.Grid.WidthHeightmap > 0` and `HeightHeightmap > 0`, (d) `LoadedMap.Chokepoints` is non-empty, (e) `LoadedMap.Grid.HeightMap` dimensions match `(WidthHeightmap+1) * (HeightHeightmap+1)`. Mark the test `[<Trait("Category", "Committed")>]` so it can be filtered out in contexts where the committed file has not yet been produced. Add the file to `FSBar.Client.Tests.fsproj`. Test MUST fail until T016 runs.

### Implementation for User Story 1

- [X] T011 [US1] Implement `MapCacheFile.write` in `src/FSBar.Client/MapCacheFile.fs`. The function: (a) builds a `Contents` record by copying fields from the supplied `SupportedMap`, `MapGrid`, and chokepoint list; (b) gzips each of the three blobs (`HeightMap`, `SlopeMap` as `float32[,]`, `ResourceMap` as `int[,]`) via a shared helper that writes bytes in row-major order using `BitConverter.GetBytes` (little-endian on all supported platforms), compresses with `GZipStream(CompressionLevel.Optimal)`, and base64-encodes; (c) serializes the `Contents` record via `System.Text.Json.JsonSerializer.Serialize` with `JsonSerializerOptions(WriteIndented = true)` and a camelCase property naming policy; (d) writes the resulting string to the target path with `File.WriteAllText` (UTF-8, no BOM). Delete the inline `gzipFloat32Array2D` / `gzipInt32Array2D` helpers from `scripts/examples/14-cache-map-analysis.fsx` once this is in place (they're replaced).
- [X] T012 [US1] Implement `MapCacheFile.read` in `src/FSBar.Client/MapCacheFile.fs` with the seven-step validation pipeline from `data-model.md`: (1) `File.Exists` → else `Error (FileMissing path)`. (2) `JsonSerializer.Deserialize<Contents>` → else `Error (ParseFailure(path, exn.Message))`. (3) `contents.SchemaVersion = schemaVersion` → else `Error (SchemaVersionMismatch(path, schemaVersion, contents.SchemaVersion))`. (4) `contents.CodeVersion = codeVersion` → else `Error (CodeVersionMismatch(path, codeVersion, contents.CodeVersion))`. (5) `contents.MapName = supported.MapName` → else `Error (MapNameMismatch(...))`. (6) Compare `contents.BaseCentre` and `contents.ChokepointQuery` against `supported` — else `Error (ParametersMismatch(path, detail))` where `detail` names the first mismatching field. (7) For each of the three blobs, decompress + verify `decompressedBytes.Length = rows * cols * 4` (else `BlobCorrupted(path, "heightMap" / "slopeMap" / "resourceMap", "size mismatch")`), then reshape into `Array2D.init rows cols (fun i j -> BitConverter.ToSingle(...))` for float32 blobs or `BitConverter.ToInt32` for int32 blobs. Wrap any gzip-decode exception as `BlobCorrupted(path, field, "gzip decode failure: " + exn.Message)`. On full success, build a `MapGrid` (`LosMap` / `RadarMap` zero-initialized via `Array2D.zeroCreate widthHeightmap heightHeightmap`), convert `ChokepointEntry` list back to `Chokepoints.Chokepoint` list preserving order, and return `Ok LoadedMap`.
- [X] T013 [US1] Implement `MapCacheFile.formatLoadError` in `src/FSBar.Client/MapCacheFile.fs` as an exhaustive pattern match over `LoadError`. Each branch produces a multi-line string that — per `contracts/error-cases.md` — contains the path (when present), a concise mismatch kind, expected vs found values (where applicable), and the literal string `bots/trainer/map-cache/refresh-all.sh`. Example for `CodeVersionMismatch(path, expected, found)`: `$"map-cache: stale codeVersion at {path}\n  expected codeVersion={expected}, found {found}\n  run: bots/trainer/map-cache/refresh-all.sh"`. Test anchors from the contract must all appear verbatim.
- [X] T014 [US1] Run the roundtrip test `dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj --filter "FullyQualifiedName~MapCacheFileRoundtripTests"`. It MUST pass now that `write` + `read` are implemented. Fix any shape bugs revealed.
- [X] T015 [US1] Rewrite `scripts/examples/14-cache-map-analysis.fsx` as a thin caller of `MapCacheFile.write`. Keep the CLI signature (`dotnet fsi scripts/examples/14-cache-map-analysis.fsx "Avalanche 3.4"`), the `.sd7` discovery under the BAR maps directory, and the `SmfParser.parseSd7 → toMapGrid` parse path. Replace the inline `dict`-based serialization (lines 120-243) with: `let supported = MapCacheFile.tryFindSupportedMap requestedMap |> Option.defaultWith (fun () -> printfn "ERROR: %s not in supportedMaps" requestedMap; exit 2)`, then `let cps = Chokepoints.findChokepoints grid supported.BaseCentre supported.ChokepointQuery`, then `MapCacheFile.write supported grid cps (MapCacheFile.cachePathFor repoRoot supported)`. The script becomes ~40 lines.
- [X] T016 [US1] Run `dotnet fsi scripts/examples/14-cache-map-analysis.fsx "Avalanche 3.4"` to produce `bots/trainer/map-cache/avalanche_3_4.json`. Verify the file exists, check its size is in the 500 KB – 1.5 MB range (within SC-005 budget), and commit it to the branch. This is the P1 deliverable: the single file whose presence makes the story testable on a clean checkout.
- [X] T017 [US1] Run the integration test `dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj --filter "FullyQualifiedName~MapCacheFileIntegrationTests"`. It MUST pass now that the committed file exists.
- [X] T018 [US1] Update the trainer warmup in `bots/trainer/bot_macro.fsx:1216-1270` to replace the inline JSON parsing with `MapCacheFile.read`. The new flow: (a) look up `let supported = MapCacheFile.tryFindSupportedMap mapName` — if `None` for a map in `mapTargetSet`, hard-fail as today; if `None` for other maps, preserve the existing synthetic-skeleton fallback unchanged. (b) `let path = MapCacheFile.cachePathFor repoRoot supported.Value`. (c) `match MapCacheFile.read supported.Value path with | Ok loaded -> pinnedChokepoints <- loaded.Chokepoints; mapGrid <- Some loaded.Grid | Error err -> failwith (MapCacheFile.formatLoadError err)`. Remove the now-dead helper functions (`readFloat32Array2D`, `readInt32Array2D`, `mapGridCacheLoad`, the chokepoint-parsing block at lines 1220-1234).
- [X] T019 [US1] Create `bots/trainer/map-cache/README.md` with a 30-line note explaining (a) these files are generated, deterministic artifacts committed to git, (b) how to refresh (point at `refresh-all.sh` — which Phase 4 creates), (c) when to refresh (point at the `codeVersion` bump rules in `MapCacheFile.fsi`), (d) that the directory is no longer gitignored and why.

**Checkpoint**: On a clean checkout, `./pack-dev.sh && dotnet build && dotnet fsi bots/trainer/… run-trainer-equivalent` loads the committed `avalanche_3_4.json` via `MapCacheFile.read` and reaches the trainer's main loop. The MVP is complete.

---

## Phase 4: User Story 2 - Contributor refreshes the cache after changing map-analysis logic (Priority: P2)

**Goal**: A contributor who modifies a map-analysis primitive bumps `MapCacheFile.codeVersion`, runs a single `refresh-all.sh` command, and the committed cache files are regenerated deterministically. If they forget to bump or forget to refresh, the loader hard-aborts the trainer with a clear error naming the file, the mismatch, and the refresh command.

**Independent Test**: Deliberately bump `codeVersion` to 2 without regenerating the cache, start the trainer against Avalanche 3.4, and confirm the trainer fails with a message that contains the file path, `codeVersion`, `expected 2`, `found 1`, and `refresh-all.sh`. Then run `refresh-all.sh`, confirm `git diff` shows exactly the one regenerated file, reset `codeVersion` back, run the script again, confirm `git status` is clean.

### Tests for User Story 2 (TDD — write these first, ensure they fail before moving to implementation)

- [X] T020 [P] [US2] Create `src/FSBar.Client.Tests/MapCacheFileVersionTests.fs` with four `[<Fact>]` methods — one each for `SchemaVersionMismatch`, `CodeVersionMismatch`, `MapNameMismatch`, `ParametersMismatch`. Each test writes a valid cache file with `MapCacheFile.write`, then **hand-edits the JSON file** to produce the mismatch (e.g., for `CodeVersionMismatch`: use `JsonDocument` to read the file, rewrite `codeVersion` field to `codeVersion + 1`, write back), then calls `MapCacheFile.read` and asserts the returned `Error` case matches the expected `LoadError` constructor with the expected payload. Then calls `MapCacheFile.formatLoadError` on the error and asserts that each of the message anchors from `contracts/error-cases.md` rows E3–E6 appears as a substring. Add to `FSBar.Client.Tests.fsproj`. Tests MUST fail until implementation is complete (they will fail because at start of this phase, the anchor strings aren't in `formatLoadError` yet — or if T013 already handled them, these tests simply verify the contract).
- [X] T021 [P] [US2] Create `src/FSBar.Client.Tests/MapCacheFileCorruptionTests.fs` with four `[<Fact>]` methods covering error-case rows E1, E2, E7, E8: (a) `FileMissing`: call `read` with a non-existent temp path → assert `Error (FileMissing _)` and anchors from E1. (b) `ParseFailure`: write `"not json at all"` to a temp file → assert `Error (ParseFailure _)` and anchors from E2. (c) `BlobCorrupted_SizeMismatch`: write a valid cache, then hand-edit one of the `GzipB64` fields to a base64-encoded byte string whose decompressed length does not match `rows * cols * 4` → assert `Error (BlobCorrupted(_, _, detail))` with `detail` containing `"size mismatch"` (anchors from E7). (d) `BlobCorrupted_GzipDecodeFailure`: write a valid cache, then overwrite one `GzipB64` field with random non-gzip base64 (e.g., the base64 of `"hello world"`) → assert `Error (BlobCorrupted(_, _, detail))` with `detail` containing `"gzip decode failure"` (anchors from E8). Add to `FSBar.Client.Tests.fsproj`.
- [X] T022 [P] [US2] Create `src/FSBar.Client.Tests/MapCacheFileDeterminismTests.fs` with a `[<Fact>]` that writes the same synthetic `MapGrid` + chokepoint list to two distinct temp paths and asserts `File.ReadAllBytes path1 = File.ReadAllBytes path2` (byte-identical). This pins SC-004. Add a second `[<Fact>]` that does the same but with a larger synthetic grid (e.g., 64×64) to catch any iteration-order dependence that might only manifest at scale. Add to `FSBar.Client.Tests.fsproj`.

### Implementation for User Story 2

- [X] T023 [US2] Run the three new test files. Any failures indicate bugs in T011/T012/T013 implementations surfaced by the error-path and determinism coverage — fix them in `src/FSBar.Client/MapCacheFile.fs`. Common likely fixes: (a) if `formatLoadError` strings don't contain the message anchors, update them; (b) if the determinism test fails, replace the `JsonSerializerOptions` dictionary-style payload construction in T011 with a concrete `Contents` record and ensure no `DateTime.UtcNow`, `Environment.MachineName`, or absolute path leaks into the serialized output. All tests MUST pass before moving on.
- [X] T024 [US2] Update `scripts/examples/14-cache-map-analysis.fsx` (already touched in T015) so that when the requested map's `.sd7` is not installed on the contributor's machine, it prints a clear `[skip] map "$NAME" — no .sd7 under ~/.local/state/Beyond All Reason/maps/` and exits with code `3` (distinct from code `2` "other error"). The `refresh-all.sh` wrapper (T026) checks exit codes: `3` counts as a skip (with a warning), `0` counts as refreshed, anything else propagates as a failure.
- [X] T025 [US2] Create `scripts/examples/15-list-supported-maps.fsx`. A few lines: preload `FSBar.Client` via `#load "../prelude.fsx"`, iterate `MapCacheFile.supportedMaps`, `printfn "%s" m.MapName` for each. This is the single source of truth for the refresh wrapper — `refresh-all.sh` has no hand-maintained map list, satisfying FR-008.
- [X] T026 [US2] Create `bots/trainer/map-cache/refresh-all.sh` (executable). The script: (a) resolves the repo root via `cd "$(dirname "$0")/../../.." && pwd`. (b) invokes `dotnet fsi scripts/examples/15-list-supported-maps.fsx` (created in T025) to print the canonical map list, one name per line. (c) loops over the map names, calling `dotnet fsi scripts/examples/14-cache-map-analysis.fsx "$name"` for each, capturing the per-call exit code. (d) tracks `refreshed` (exit 0) and `skipped` (exit 3 — the "missing .sd7" code from T024) separately; any other exit code is treated as a hard failure that aborts the loop. (e) exits non-zero if `refreshed == 0` so the contributor notices when their working tree was unchanged.
- [X] T027 [US2] Manual validation of the refresh determinism property (SC-004): run `./bots/trainer/map-cache/refresh-all.sh` twice in a row on a clean working tree, confirm `git status` reports no modified files after the second invocation. If any file shows as modified, the root cause is a non-determinism in `write` (timestamp, path, machine-specific string) — fix it and re-run T022.

**Checkpoint**: A contributor can refresh all supported maps with one command; stale caches (any mismatch) hard-abort the trainer with actionable messages.

---

## Phase 5: User Story 3 - New supported map is added to the permanent cache (Priority: P3)

**Goal**: Adding a new supported map is a one-line change to `MapCacheFile.supportedMaps` plus one refresh invocation. No code, refresh-script, trainer, or documentation changes are required beyond appending the new element.

**Independent Test**: Append a second `SupportedMap` record (synthetic, using the existing `SyntheticMapGrid` helpers so it does not depend on a real `.sd7`) to a test-only variant of `supportedMaps`, round-trip it through `write` / `read`, and confirm the `avalanche_3_4.json` cache on disk is unchanged by the operation.

### Tests for User Story 3 (TDD — write these first, ensure they fail before moving to implementation)

- [X] T028 [P] [US3] Extend `src/FSBar.Client.Tests/MapCacheFileRoundtripTests.fs` with a `[<Fact>]` that constructs **two** distinct synthetic `SupportedMap` values (one 8×8 and one 16×16, different `BaseCentre` values, different `ChokepointQuery` thresholds), writes each to its own temp path via `MapCacheFile.write`, reads each back via `MapCacheFile.read`, and asserts: (a) each roundtrip is correct on its own, (b) reading map-A's file with map-B's `SupportedMap` yields `Error (MapNameMismatch _)` (proves cross-contamination is impossible), (c) reading map-A's file with map-A's `SupportedMap` is `Ok`.
- [X] T029 [P] [US3] Add a `[<Fact>]` to `src/FSBar.Client.Tests/MapCacheFileRoundtripTests.fs` (reusing the existing file rather than misfiling the test under "Version") that pins the caller-side unsupported-map lookup path: call `MapCacheFile.tryFindSupportedMap "Nonexistent Map 99"` → assert `None`. Then call `MapCacheFile.tryFindSupportedMap "Avalanche 3.4"` → assert `Some s` with `s.MapName = "Avalanche 3.4"`. This pins FR-008 (the single-source-of-truth lookup works both ways) and FR-010 (unsupported maps surface as `None`, letting callers preserve their own fallback path). There is no corresponding `read` call — unsupported-map lookup is deliberately outside `LoadError`; `read` only runs when the caller has already resolved a valid `SupportedMap`.

### Implementation for User Story 3

- [X] T030 [US3] Run the US3 tests. They should pass without any further implementation changes — the US3 story is validated entirely by tests because the production code does not need any new functions, only a new data entry in `supportedMaps`, and the test suite proves the data-entry path is correct. If a test fails, the fix lives in `MapCacheFile.write` / `read` (a data-dependent bug that the simpler US1 tests didn't catch) — fix it in-place.

**Checkpoint**: Adding a new map is provably a one-line change. The feature is complete.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validate the quickstart end-to-end, refresh project-level documentation, and confirm no existing tests regressed.

- [X] T031 [P] Update `CLAUDE.md` "Active Technologies" section — the 026 entry auto-added by `update-agent-context.sh` is fine, but append a brief "Map analysis caching" subsection under `## Active Technologies` that points at `src/FSBar.Client/MapCacheFile.fsi` as the authoritative contract and at `bots/trainer/map-cache/refresh-all.sh` as the refresh entry point.
- [X] T032 [P] Review `specs/026-permanent-map-cache/quickstart.md` against the final implementation and fix any drift (command names, expected log lines, file paths). Any correction discovered during Phase 3–5 work lands here.
- [X] T033 Run `dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj --nologo` — confirm all existing tests in the project still pass (no regressions from the `FSBar.Client.fsproj` compile-order change, the surface-area baseline refresh, or any shared test fixtures touched by new files).
- [X] T034 Run `dotnet test tests/FSBar.LiveTests/FSBar.LiveTests.fsproj --filter "FullyQualifiedName~ConnectionTests" --nologo` — confirm the 6 ConnectionTests still pass. (Sanity check that nothing in the build graph got disturbed.)
- [X] T035 [P] Validate SC-002 (load-path latency): add a `[<Fact>]` to `src/FSBar.Client.Tests/MapCacheFileIntegrationTests.fs` (marked `[<Trait("Category", "Committed")>]`) that reads `bots/trainer/map-cache/avalanche_3_4.json` 11 times in a tight loop, discards the first iteration as JIT warm-up, computes the median elapsed time across the remaining 10 via `System.Diagnostics.Stopwatch`, logs the measured value via xUnit's `ITestOutputHelper` (so the measurement is visible in test output for future regression detection), and asserts the median is under 25 ms. Locate the committed file via `MapCacheFile.cachePathFor` from a repoRoot derived from the test assembly location, consistent with T010.
- [ ] T036 Validate SC-006 end-to-end: on a local branch, bump `MapCacheFile.codeVersion` from 1 to 2 in **both** `src/FSBar.Client/MapCacheFile.fsi` and `src/FSBar.Client/MapCacheFile.fs` (the values must stay in sync or the project won't compile), rebuild via `./pack-dev.sh`, launch the trainer against Avalanche 3.4, and confirm the trainer aborts with a message containing `codeVersion`, `expected 2`, `found 1`, and `refresh-all.sh`. Then revert both bumps. This is a manual experiment, not an automated test — document the observation in a short paragraph appended to `bots/trainer/map-cache/README.md` or in the PR description.
- [X] T037 Validate SC-005 (size budget): `ls -lah bots/trainer/map-cache/` and confirm `avalanche_3_4.json` is under ~1.5 MB. If it's larger, investigate whether `CompressionLevel.Optimal` is doing its job or whether the schema is storing redundant data.
- [ ] T038 Final quickstart walkthrough: on a second, freshly-cloned working copy of the branch, run the exact commands from `specs/026-permanent-map-cache/quickstart.md` "For first-time contributors" section. Confirm the output matches the expected log lines and the trainer reaches its main loop. If the walkthrough diverges from reality, update the quickstart (not the code).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — T001 and T002 can start immediately and run in parallel.
- **Phase 2 (Foundational)**: Depends on Phase 1. T003 → T004 → T005 are strictly sequential (same file, `.fs` must follow `.fsi`, `supportedMaps` depends on types). T006 and T007 can run in parallel with each other once T005 lands. T008 gates everything downstream.
- **Phase 3 (US1 MVP)**: Depends on Phase 2. Within the phase, T009 and T010 can be written in parallel before implementation. T011 → T012 → T013 are strictly sequential (same `MapCacheFile.fs`). T014 validates. T015 → T016 → T017 are sequential (script rewrite → run it → verify committed file). T018 (trainer warmup switch) and T019 (README) can run in parallel at the end.
- **Phase 4 (US2)**: Depends on Phase 3 completion — specifically, the `write`/`read`/`formatLoadError` implementations from T011–T013 must exist so the error-path tests can probe them. T020, T021, T022 can be written in parallel. T023 is sequential (may touch `MapCacheFile.fs`). Then T024 → T025 → T026 → T027 are sequential: T024 fixes the per-map script's exit codes (needed by T026's exit-code branching); T025 creates the helper `.fsx` that T026 invokes; T026 wires the shell wrapper; T027 validates determinism across the whole stack.
- **Phase 5 (US3)**: Depends on Phase 3. T028 and T029 can run in parallel. T030 follows.
- **Phase 6 (Polish)**: Depends on all previous phases. T031 ∥ T032 ∥ T035 (three independent tasks: CLAUDE.md doc update, quickstart review, benchmark test). T033 ∥ T034 (regression runs). T036 → T037 → T038 are manual validations — sequential.

### User Story Dependencies

- **US1 (P1)** is the MVP. It depends on Phase 1 + Phase 2 only. Delivering it stops the trainer's cold-start failure.
- **US2 (P2)** depends on US1's `write` + `read` implementations (the error-case tests probe them). Until US1 lands, there is no `write` to corrupt and no `read` to misfeed.
- **US3 (P3)** depends on US1. The synthetic-second-map test reuses the `write`/`read` path.

### Within Each User Story

- Tests MUST be written first (T009/T010, T020–T022, T028/T029 come before the implementation tasks in their phases) and MUST fail before the corresponding implementation task lands.
- Within a story, tasks that touch `src/FSBar.Client/MapCacheFile.fs` are sequential with each other; tests and docs and scripts can parallelize.

### Parallel Opportunities

- Phase 1: T001 ∥ T002.
- Phase 2: T006 ∥ T007 (after T005).
- Phase 3: T009 ∥ T010 (tests first). T018 ∥ T019 (end of phase).
- Phase 4: T020 ∥ T021 ∥ T022 (all three test files first).
- Phase 5: T028 ∥ T029.
- Phase 6: T031 ∥ T032 ∥ T035 (docs + benchmark are independent), then T033 ∥ T034 (regression runs).

---

## Parallel Example: Phase 3 US1 tests

```bash
# Write both US1 tests in parallel before moving to implementation:
Task: "Create src/FSBar.Client.Tests/MapCacheFileRoundtripTests.fs — synthetic-grid write→read roundtrip, must fail initially"
Task: "Create src/FSBar.Client.Tests/MapCacheFileIntegrationTests.fs — reads committed avalanche_3_4.json, Category=Committed trait, must fail until T016"
```

## Parallel Example: Phase 4 US2 tests

```bash
# Write all three US2 test files in parallel before the refresh script work:
Task: "Create src/FSBar.Client.Tests/MapCacheFileVersionTests.fs — 4 Facts for schema/code/name/params mismatch"
Task: "Create src/FSBar.Client.Tests/MapCacheFileCorruptionTests.fs — 4 Facts for FileMissing/ParseFailure/size mismatch/gzip decode failure"
Task: "Create src/FSBar.Client.Tests/MapCacheFileDeterminismTests.fs — 2 Facts for byte-identical rewrite at small and large grid sizes"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1 (Setup — 2 tasks, ~5 min).
2. Complete Phase 2 (Foundational — 6 tasks, ~2–3 hours). The project compiles with stubbed bodies.
3. Complete Phase 3 (US1 — 11 tasks, ~1 day). This is the MVP: fresh clone → trainer reads committed cache → main loop.
4. **STOP and VALIDATE**: Run the quickstart "For first-time contributors" walkthrough. If the trainer starts cleanly against Avalanche 3.4 on a clean checkout, US1 is done and merge-worthy. Ship as an incremental PR if desired.

### Incremental Delivery

1. Phase 1 + Phase 2 + Phase 3 → merge PR #1 (MVP: "fresh clones boot cleanly").
2. Phase 4 → merge PR #2 ("refresh workflow and stale-cache hard abort").
3. Phase 5 → merge PR #3 ("new-map workflow — proven by test"). Often sized to roll into PR #2 if the codebase is small.
4. Phase 6 → polish PR, shippable with any of the above.

Each phase produces a strictly-additive, independently-testable increment. The constitution's Tier 1 artifact chain (spec / plan / `.fsi` / baselines / tests / docs) is complete after Phase 6.

### Parallel Team Strategy

With two or more developers after Phase 2 checkpoint:

- Developer A: Phase 3 US1 (T009 → T019). Owns `MapCacheFile.fs` sequentially.
- Developer B: Starts Phase 4 US2 test scaffolding (T020 → T022 — write red tests) in parallel, then picks up refresh-script work (T024 → T026) once A's implementation lands.
- Either developer: Phase 5 + Phase 6 once 3 and 4 settle.

---

## Notes

- Every user-story task is tagged `[US1]` / `[US2]` / `[US3]` so traceability back to the spec holds even after refactoring.
- The `.fs`/`.fsi` pair for `MapCacheFile` is the single authoritative declaration of `codeVersion`. Any PR touching map-analysis semantics must bump both sides in the same commit.
- The 026 feature does not delete or touch `src/FSBar.Client/MapCache.fs` (the in-memory session cache). Name collision is handled by the suffix `-File` on the new module.
- If T007 (surface-area baseline update) fails with "extra symbol" or "missing symbol" — run `SurfaceAreaTests` once locally to see the expected file contents, copy them into the baseline, commit. Do NOT hand-edit the baseline file by guessing.
- If the refresh script (T024) cannot find a map because the contributor doesn't have the `.sd7`, it MUST warn and skip, not fail the whole refresh. The rule is: if at least one map refreshed, exit 0; if zero maps refreshed, exit 1 so the contributor notices their working tree is unchanged.
- Stop at any checkpoint (end of Phase 3 / Phase 4 / Phase 5) to validate the story independently before moving on.
