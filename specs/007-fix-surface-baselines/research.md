# Research: Fix Missing Baseline Surface FSI Coverage

## R1: Baseline Storage Format

**Decision**: Raw `.fsi` file content stored as `.baseline` text files.

**Rationale**: The `.fsi` files are already human-authored, curated, and serve as the canonical public API contract. Storing raw content means baselines are immediately readable, produce clean git diffs, and require no parsing infrastructure. The `.fsi` text IS the surface — no transformation needed.

**Alternatives considered**:
- Parsed/normalized representation (JSON, custom DSL): Adds complexity, requires FSharp.Compiler.Service dependency, harder to read in reviews. Over-engineered for file-to-file comparison.
- Reflection-based runtime extraction: Requires loading compiled assembly, adds runtime dependency, fragile across .NET versions.

## R2: Order-Independence (FR-007)

**Decision**: Store baselines as-is (preserving declaration order). FR-007 is satisfied by design because `.fsi` files have a fixed, authored order — reordering declarations in F# `.fsi` files changes compilation semantics and is therefore a genuine API surface change that SHOULD be detected.

**Rationale**: In F#, declaration order in signature files affects compilation (types must be declared before use). Reordering is not cosmetic — it's a structural change. The baseline correctly flags it.

**Alternatives considered**:
- Sorting declarations alphabetically before comparison: Would mask meaningful structural changes. F# is order-dependent.

## R3: Test Project Location

**Decision**: Add `SurfaceAreaTests.fs` to the existing `FSBar.Client.Tests` project.

**Rationale**: Surface-area baseline tests are pure file comparison — they read `.fsi` files from the source tree and compare against stored `.baseline` files. No live game server, no network, no external dependencies. They belong with the other unit tests. Adding a separate project would violate the constitution's dependency minimization principle.

**Alternatives considered**:
- New `FSBar.Client.SurfaceTests` project: Unnecessary project proliferation for a single test file.
- In `tests/FSBar.LiveTests/`: Wrong location — these tests don't need a live engine.

## R4: Baseline File Location

**Decision**: Store baseline files in `src/FSBar.Client.Tests/Baselines/{ModuleName}.baseline` (12 files, one per `.fsi` module).

**Rationale**: Co-locating baselines with the test project keeps related artifacts together. Using a `Baselines/` subdirectory avoids cluttering the test project root. Files are committed to git so all developers share the same reference point.

## R5: Baseline Regeneration Mechanism

**Decision**: Environment variable `UPDATE_BASELINES=true` during test execution causes baseline tests to overwrite stored baselines with current `.fsi` content instead of failing on mismatch.

**Rationale**: Simple, discoverable, requires no additional tooling. Developers run `UPDATE_BASELINES=true dotnet test` after intentional API changes. The updated baselines appear in `git diff` for review before commit.

**Alternatives considered**:
- Separate regeneration script: More ceremony for a simple file-copy operation.
- Test runner flag: xUnit doesn't support custom CLI flags; environment variable is the standard F#/.NET pattern.

## R6: Missing Baseline Detection (FR-005)

**Decision**: At test time, enumerate all `.fsi` files from the FSBar.Client project directory and verify each has a corresponding `.baseline` file. Missing baselines fail the test with a message naming the missing module and instructions to regenerate.

**Rationale**: This catches new modules added without baselines. The test discovers `.fsi` files dynamically rather than hardcoding the list, so it automatically covers future modules.

## R7: Module Count — 12, Not 11

**Decision**: Cover all 12 public modules (the spec lists 11 but omits ScriptGenerator, which has an `.fsi` file).

**Rationale**: ScriptGenerator.fsi declares `val generate: EngineConfig -> string` — a public function. It must have a baseline per constitution §II. The spec's list of 11 was an oversight; the implementation covers all `.fsi` files found in the project.

**Modules**: BarClient, Callbacks, Commands, Connection, EngineConfig, EngineLauncher, Events, MapCache, MapGrid, MapQuery, MapCache, Protocol, ScriptGenerator (12 total).
