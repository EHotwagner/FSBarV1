# Feature Specification: Permanent, Committed Map Cache

**Feature Branch**: `026-permanent-map-cache`
**Created**: 2026-04-15
**Status**: Draft
**Input**: User description: "create a permanent map cache that is not .gitignored."

## Clarifications

### Session 2026-04-15

- Q: How should the loader detect that a committed cache file is stale with respect to current analysis code? → A: A manual integer `codeVersion` constant, declared in one well-known location in the source, bumped by the contributor whenever analysis semantics change. The generator stamps this value into every cache file; the loader compares exactly and refuses any mismatch.
- Q: What should the trainer do at runtime when the committed cache for a supported map is missing, corrupted, or has a mismatched `codeVersion`? → A: Hard abort — trainer startup fails with a clear error naming the file, the mismatch, and the refresh command. No on-demand fallback, no opt-out environment variable. Drift is caught loudly, not silently absorbed by a slower warmup path.
- Q: Should a CI job actively enforce cache freshness (regenerate + diff on every PR), or is the runtime hard-abort plus contributor discipline sufficient? → A: No dedicated CI job for this feature. The runtime hard-abort from FR-006 is the backstop; any dev loop or bot-match run on a machine with the affected supported map will fail loudly on drift, and that window is short enough that adding a CI job (which would require provisioning every `.sd7` in the CI environment) is premature. Revisit only if drift incidents actually occur.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Trainer starts on a clean checkout without a manual bake step (Priority: P1)

A developer clones the repository, installs the usual BAR prerequisites, and runs the trainer against the required map set (today: Avalanche 3.4). The trainer bot warms up, loads its precomputed map analysis from a file that is already present in the working tree, and begins playing — without the developer first having to run `scripts/examples/14-cache-map-analysis.fsx` by hand or understand that a cache needs to be baked at all.

**Why this priority**: This is the entire point of the feature. Today the trainer hard-fails on a fresh checkout for its target-set maps because the cache directory is `.gitignored` (`.gitignore:26`) and has never been committed. Every new contributor, CI runner, and container rebuild rediscovers this friction. Making the cache a committed, versioned artifact eliminates the cold-start problem in one step and delivers value on its own — none of the other stories below are required for this to be useful.

**Independent Test**: Delete any local cache, check out the feature branch on a machine that has never built it before, and launch the trainer against Avalanche 3.4. The warmup log reports that a cached map analysis was found and loaded; the bot reaches its main loop without raising the "cache missing" hard-fail.

**Acceptance Scenarios**:

1. **Given** a freshly cloned repository with no locally generated cache files, **When** the trainer bot starts against a supported map, **Then** it finds a committed cache file under the map-cache directory and completes warmup without invoking the generator script.
2. **Given** a freshly cloned repository, **When** the contributor runs `git status` after building and starting the trainer, **Then** no map-cache files appear as untracked or ignored changes (the cache directory is tracked by git, and the files inside are visible to `git status`).
3. **Given** the trainer loads a committed cache file, **When** warmup completes, **Then** the map primitives loaded from disk produce the same base plan and chokepoint list that a fresh in-engine analysis on the same map would produce.

---

### User Story 2 - Contributor refreshes the cache after changing map-analysis logic (Priority: P2)

A contributor modifies one of the map-analysis primitives in `FSBar.Client` — for example, changing the slope threshold used by chokepoint detection, or fixing a bug in `BasePlan`. After their change, the committed cache files for the supported maps become stale: the bytes on disk no longer match what the new code would compute. The contributor regenerates the cache for each supported map with a single documented command, commits the updated cache files alongside the code change, and the review diff makes it visible that the cache was refreshed.

**Why this priority**: Without this, story 1 degrades silently over time — contributors will forget to re-bake, the committed cache will drift from the code, and the trainer will load stale analysis that looks correct but isn't. Story 2 keeps the cache honest once story 1 is in place.

**Independent Test**: On a branch that also modifies a map-analysis primitive, run the documented refresh command for every supported map, commit the result, and verify that (a) the committed JSON files changed, (b) the trainer loads the new cache successfully, and (c) the loader refuses to load any cache file whose recorded schema version or analysis parameters don't match the current code.

**Acceptance Scenarios**:

1. **Given** a working tree with a modified map-analysis primitive, **When** the contributor runs the documented refresh command, **Then** every cache file for a supported map is rewritten deterministically from the current source.
2. **Given** a committed cache file whose schema version or recorded analysis parameters no longer match the loader's expectations, **When** the trainer attempts to load it, **Then** the loader fails fast with a clear message naming the mismatched field and pointing at the refresh command — rather than silently loading stale data.
3. **Given** two contributors independently run the refresh command on the same source tree, **When** they compare the resulting cache files, **Then** the files are byte-identical (the generation step is deterministic).

---

### User Story 3 - New supported map is added to the permanent cache (Priority: P3)

A contributor decides the trainer should also support a new map (e.g., Glitters 1.2). They add the map to the list of supported maps, run the refresh command once, and commit both the code change and the new cache file. Subsequent contributors and CI runs pick up the new map automatically.

**Why this priority**: Useful but strictly additive — the feature is valuable even if the supported-map list never grows. This story exists to confirm the design scales without special-casing.

**Independent Test**: Add one new map to the supported list, run the refresh command, verify a new cache file appears under the map-cache directory and loads cleanly, and confirm no existing cache files were touched by the addition.

**Acceptance Scenarios**:

1. **Given** a new map name added to the supported list, **When** the contributor runs the refresh command, **Then** a new cache file is produced for that map and no other cache file is modified.
2. **Given** a commit that adds a new supported map, **When** another contributor checks out the commit, **Then** the trainer can start against that new map on a clean checkout without any additional manual steps.

---

### Edge Cases

- **Source map file unavailable on the contributor's machine**: Cache generation relies on the `.sd7` archive being installed under the standard BAR data directory. If the contributor refreshing the cache does not have a given map installed, the refresh command must skip that map with a clear warning (not silently leave a stale file, and not crash).
- **Committed cache file becomes corrupted or truncated**: If a cache file on disk fails to parse as the expected schema, the loader must fail with a message that identifies the file and the refresh command, not a raw JSON exception.
- **Committed cache file matches schema but was generated from different source code**: The loader must detect this via a recorded code/schema version (or equivalent fingerprint) and refuse to use the stale cache rather than loading primitives that don't match the current `FSBar.Client`.
- **Large diffs on refresh**: Cache files are effectively binary blobs serialized as base64 inside JSON. A routine refresh may rewrite most bytes in each file. Reviewers must be able to tell "the cache was refreshed" at a glance without trying to read the diff line-by-line.
- **Repository size growth as supported maps are added**: Each map's cache is in the rough order of a megabyte. The feature must remain viable as the supported list grows to a handful of maps without pushing the repository into a regime that requires large-file storage.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The repository MUST ship a committed, version-controlled cache file for every map in the trainer's supported-map set, located under a dedicated cache directory that is tracked by git.
- **FR-002**: The repository's ignore rules MUST NOT exclude the cache directory or the committed cache files within it. (Today, `.gitignore:26` excludes `bots/trainer/map-cache/*.json`; this exclusion must be removed or narrowed so the committed files are tracked.)
- **FR-003**: The trainer warmup MUST load map analysis from the committed cache file when one is present for the current map, without invoking on-demand generation, and without requiring any manual pre-run step by the operator.
- **FR-004**: The project MUST provide a single, documented command that regenerates every committed cache file from current source code and current map archives, producing byte-identical output on repeated runs over the same inputs.
- **FR-005**: Every committed cache file MUST record enough metadata to detect staleness: (a) a schema version identifying the on-disk file format, (b) a manual integer `codeVersion` constant declared in one well-known source location and bumped by the contributor whenever analysis semantics change, (c) the exact analysis parameters used to produce the file (e.g. chokepoint query thresholds), and (d) the source map identity. The loader MUST compare the `codeVersion` value exactly against the currently declared constant and refuse any mismatch.
- **FR-006**: The cache loader MUST hard-abort trainer startup with a clear, actionable error message — naming the file, the exact mismatch (missing, parse failure, schema-version mismatch, or `codeVersion` mismatch with expected and found values), and the refresh command — when a committed cache for a supported map is missing, corrupted, or stale. It MUST NOT fall back to on-demand analysis, partial data, or synthetic data for a supported map, and MUST NOT expose an environment variable or flag to downgrade this abort to a warning. Drift is caught loudly at startup.
- **FR-007**: The refresh command MUST skip (with a clear warning, not a crash) any supported map whose source archive is not present on the contributor's machine, and MUST leave the existing committed cache file for that map untouched rather than deleting or truncating it.
- **FR-008**: The set of supported maps MUST be declared in a single place in the repository, so that adding or removing a map is a one-line change followed by a refresh.
- **FR-009**: Documentation MUST explain: where cached files live, how to refresh them, when a refresh is required, and what guarantees the committed cache gives operators on a clean checkout. The explanation MUST appear in both `bots/trainer/map-cache/README.md` (contributor-facing, in-place reference — the single source of truth) and `CLAUDE.md` (agent-facing, repository-wide context, which links to the README).
- **FR-010**: The feature MUST NOT regress existing behavior for maps outside the supported-map set: unsupported maps continue to fall back to whatever behavior the trainer previously exhibited (today: a synthetic skeleton). The committed cache is a strict addition for the supported set, not a replacement of the on-demand path.

### Key Entities

- **Supported-map set**: The list of maps the trainer officially supports. Determines which maps get committed cache files. Today this set is effectively `{Avalanche 3.4}`.
- **Committed cache file**: A per-map, deterministic, self-describing serialization of the precomputed map primitives (heightmap, slope map, resource map, chokepoint list, and the parameters used to compute them). Lives under a tracked directory in the repository. One file per supported map.
- **Cache schema / code version**: Two identifiers recorded inside each cache file that the loader checks at load time. The **schema version** describes the on-disk file format; it changes only when the cache serialization layout changes. The **code version** is a manual integer constant declared in one well-known source location, bumped by the contributor whenever analysis semantics change; the loader compares it exactly and refuses any mismatch.
- **Refresh command**: A single documented entry point that, given the current source tree and the contributor's installed map archives, rewrites every committed cache file for the supported-map set.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A contributor starting from a freshly cloned repository can launch the trainer against a supported map and reach the main loop without running any manual cache-generation step. Measured by: the documented quickstart does not mention baking a cache, and a timed clean-checkout run of the trainer succeeds without human intervention between `git clone` and trainer startup.
- **SC-002**: Trainer warmup time on a supported map improves by at least an order of magnitude relative to the on-demand analysis path. Concretely, the committed-cache load path completes in under 25 ms, versus the ~250 ms on-demand parse path today.
- **SC-003**: A loader given a stale or corrupted committed cache produces a structured `LoadError` that, when formatted, names the offending file, the specific mismatch kind, and the command to refresh it. Measured by: the eight error-case tests in T020/T021 assert on both the `LoadError` constructor and the anchor strings from `contracts/error-cases.md`. (Wall-clock timing is subsumed by SC-002 — if the happy-path load is <25 ms, the error path is trivially under a second.)
- **SC-004**: Running the refresh command twice in a row on an unchanged source tree produces zero-byte diffs in git. Measured by: `git status` reports a clean working tree after a second invocation.
- **SC-005**: The committed cache contribution to repository size stays under a reasonable bound as the supported-map set grows — concretely, under ~1.5 MB per supported map on average, and under ~15 MB total for the foreseeable list (no more than ~10 maps).
- **SC-006**: A contributor modifying a map-analysis primitive is caught within one full dev-loop cycle if they forget to bump `codeVersion` and refresh the cache: the very next trainer startup on any supported map hard-aborts (per FR-006) with a message that names the mismatch and the refresh command. Measured by: an intentional experiment in which a primitive is changed and `codeVersion` is bumped without re-baking the cache — the trainer MUST fail to start on the affected map rather than silently loading stale analysis.

## Assumptions

- The trainer's supported-map set today is effectively a single map (Avalanche 3.4 — the map that hard-fails today when the cache is absent). The feature is designed around supporting a small set that grows slowly, not dozens of maps.
- Contributors who need to regenerate the cache have the relevant `.sd7` archives installed under the standard BAR data directory (`~/.local/state/Beyond All Reason/maps/`). Contributors who do not have a map installed are expected to skip that map during refresh, not block or fail the whole build.
- Cache generation is deterministic — running the existing generator twice over the same source tree and the same `.sd7` file produces the same bytes. The existing script in `scripts/examples/14-cache-map-analysis.fsx` is expected to already be deterministic (fixed arrays, fixed parameter set); if not, making it deterministic is in scope for this feature.
- Per-map cache size is in the rough order of a megabyte gzipped (observed: ~500 KB – 1 MB in the feature 025 notes), which is acceptable to commit directly to git without Large File Storage for the expected number of supported maps.
- The cache is treated as a generated, reproducible artifact that happens to be checked in for contributor and CI convenience — not as a hand-edited source of truth. Reviewers do not read cache diffs line-by-line; they verify the refresh was performed.
- The feature builds on the existing generator script (`scripts/examples/14-cache-map-analysis.fsx`) and the existing warmup-time loader in `bots/trainer/bot_macro.fsx`. Work is expected to focus on: removing the ignore rule, checking in the files, adding a stale-cache guard to the loader, recording a code/schema version in the file, and providing a "refresh everything" convenience wrapper over the per-map generator.
- No existing behavior for unsupported maps changes. The feature is strictly additive for the supported-map set.
- Cache freshness is enforced at runtime (FR-006 hard abort), not in CI. This feature does not add a CI job that regenerates or diffs the committed cache. Adding such a job would require provisioning every supported map's `.sd7` archive in the CI environment, which is out of scope. If drift incidents occur in practice, a follow-up feature can introduce a lightweight CI check (e.g., re-parse committed cache files and verify `codeVersion` matches the current source constant, without needing `.sd7` archives).
