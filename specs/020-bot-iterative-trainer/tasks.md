---
description: "Task list for feature 020-bot-iterative-trainer implementation"
---

# Tasks: Iterative AI Bot Trainer with Helper Library

**Branch**: `020-bot-iterative-trainer`
**Input**: Design documents from `/specs/020-bot-iterative-trainer/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Test tasks are included ONLY for the compiled F# changes in `src/FSBar.Client` (Constitution §III mandates test evidence for behaviour-changing code and `.fsi` surface changes). The bot scripts themselves are operator-verified via `quickstart.md` §3 as the spec explicitly chose.

**Organization**: Phase-ordered, with user-story phases in the order US1 (P1 — infrastructure floor) → US2 (P1 — iteration loop) → US4 (P1 — helper library) → US3 (P2 — ladder escalation). US4 is intentionally scheduled before US3 because the helper library is the primary deliverable and a no-op-opponent run is sufficient to seed helper extractions; ladder escalation then exercises the library on harder rungs.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: task can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: which user story this task belongs to (US1, US2, US3, US4)
- File paths are absolute from repo root `/home/developer/projects/FSBarV1/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the repository structure and ignores before any code lands.

- [ ] T001 Verify the current git branch is `020-bot-iterative-trainer` and working tree is clean; abort implementation if either fails
- [ ] T002 Add `bots/runs/` to `.gitignore` at `/home/developer/projects/FSBarV1/.gitignore`
- [ ] T003 [P] Create the in-repo trainer tree skeleton: `mkdir -p /home/developer/projects/FSBarV1/bots/trainer/helpers /home/developer/projects/FSBarV1/bots/trainer/engine-patches` (directories only; files follow in later phases)
- [ ] T004 [P] Create an empty `/home/developer/projects/FSBarV1/bots/runs/` marker so the gitignored directory exists on disk for the runner to use

**Checkpoint**: Repository is ready to receive code changes on the feature branch.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extend the `FSBar.Client` public API (Tier 1 change per plan.md Constitution Check) and install the BARb difficulty profile patch. These MUST complete before any user story phase because US1's runner calls `ScriptGenerator` with the new fields and US3's ladder uses BARb difficulty profiles.

**⚠️ CRITICAL**: Constitution §II requires the `.fsi` mirror and surface-area baseline to land in the same commit as the `.fs` change. Constitution §III requires test evidence to land in the same commit as the behaviour change.

- [ ] T005 Add `OpponentAIOptions: Map<string, string>` (default `Map.empty`) and `DeathMode: string` (default `"com"`) as record fields on `EngineConfig` in `/home/developer/projects/FSBarV1/src/FSBar.Client/EngineConfig.fs`; update `defaultConfig` to set `DeathMode = "com"` and `OpponentAIOptions = Map.empty`
- [ ] T006 Mirror the two new fields in `/home/developer/projects/FSBarV1/src/FSBar.Client/EngineConfig.fsi` with XML doc comments explaining purpose and defaults (Constitution §II)
- [ ] T007 Update `/home/developer/projects/FSBarV1/src/FSBar.Client/ScriptGenerator.fs` to render `deathmode={config.DeathMode}` instead of the hard-coded `neverend`, and to emit an `[AI1].[OPTIONS]` block containing one `key=value;` line per entry in `config.OpponentAIOptions` when the map is non-empty; empty map produces no `[OPTIONS]` block (backwards-compatible)
- [ ] T008 [P] Extend `/home/developer/projects/FSBarV1/tests/FSBar.Client.Tests/ScriptGeneratorTests.fs` (create the file if missing) with three xUnit tests: (a) default config emits `deathmode=neverend` is replaced by `deathmode=com`, (b) non-empty `OpponentAIOptions` produces an `[AI1]` `[OPTIONS]` block with each pair rendered as `key=value;`, (c) empty `OpponentAIOptions` produces no `[OPTIONS]` block at all (string must not contain `[OPTIONS]` under `[AI1]`)
- [ ] T009 Build `/home/developer/projects/FSBarV1/src/FSBar.Client/FSBar.Client.fsproj` with `dotnet build -c Debug` and fix any signature or type errors until the build is green
- [ ] T010a Locate the existing `FSBar.Client` surface-area baseline: `ls /home/developer/projects/FSBarV1/tests/FSBar.Client.Tests/baselines/ 2>&1 || find /home/developer/projects/FSBarV1/tests/FSBar.Client.Tests -iname '*baseline*' 2>&1`; read the baseline test file (likely under `tests/FSBar.Client.Tests/SurfaceAreaTests.fs` or similar) to determine the exact regeneration invocation (e.g., an `UPDATE_BASELINES=1 dotnet test ...` env var, or a `--update-baselines` flag, or a manual rewrite pattern); record the command verbatim in the T012 commit message so future readers do not repeat the discovery
- [ ] T010b Run the command discovered in T010a, verify the baseline file now reflects the `EngineConfig.OpponentAIOptions` and `EngineConfig.DeathMode` additions, and confirm the baseline test (`dotnet test tests/FSBar.Client.Tests`) is green; commit the updated baseline alongside T005–T009 as part of the same focused commit described in T012 (Constitution §II)
- [ ] T011 Run `dotnet test /home/developer/projects/FSBarV1/tests/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug` and confirm all tests including T008 and the surface-area baseline test are green
- [ ] T012 Commit T005–T011 as one focused commit on `020-bot-iterative-trainer`: `"020: EngineConfig.OpponentAIOptions + DeathMode, ScriptGenerator emits [AI1].[OPTIONS] and configurable deathmode"` and push to `origin`
- [ ] T012a [P] Create a new numbered example script at `/home/developer/projects/FSBarV1/src/FSBar.Client/scripts/examples/NN-engine-opponent-options.fsx` (replace `NN` with the next unused number in that directory — run `ls /home/developer/projects/FSBarV1/src/FSBar.Client/scripts/examples/` to determine) demonstrating `EngineConfig.OpponentAIOptions` and `EngineConfig.DeathMode`: construct an `EngineConfig` with `DeathMode = "com"` and `OpponentAIOptions = Map.ofList [ "profile", "easy" ]`, call `ScriptGenerator.generate`, and `printfn` the resulting script text so a reader can see the rendered `[AI1].[OPTIONS]` block and `deathmode=com` line without launching an engine; verify the example is runnable with `dotnet fsi <script path>`; commit separately as `"020: example script for EngineConfig opponent options + death mode"` and push (Constitution §V)
- [ ] T013 [P] Copy `/home/developer/.local/state/Beyond All Reason/engine/recoil_*/AI/Skirmish/BARb/stable/AIOptions.lua` into `/home/developer/projects/FSBarV1/bots/trainer/engine-patches/BARb_AIOptions.lua` and uncomment the `easy`, `medium`, and `hard` profile `items` entries; leave `dev` in place
- [ ] T014 [P] Write `/home/developer/projects/FSBarV1/bots/trainer/engine-patches/install-barb-profiles.sh` as an idempotent bash installer that: (a) globs `~/.local/state/Beyond All Reason/engine/recoil_*/AI/Skirmish/BARb/stable/AIOptions.lua`, (b) for each match, compares against `BARb_AIOptions.lua` and skips if identical, (c) otherwise copies the patched file into place with a `.bak` backup of the original, (d) prints a summary; ensure `chmod +x` after writing
- [ ] T015 Run `bash /home/developer/projects/FSBarV1/bots/trainer/engine-patches/install-barb-profiles.sh` and verify that BARb's installed `AIOptions.lua` now has `easy`/`medium`/`hard` profile entries uncommented; re-run to verify idempotence (no-op on second invocation)
- [ ] T016 Commit T013–T015 as one focused commit: `"020: BARb difficulty profile patch + idempotent installer"` and push to `origin`

**Checkpoint**: `FSBar.Client` has the new engine fields, tests are green, baseline is updated, BARb profiles are patched. User story phases can now begin.

---

## Phase 3: User Story 1 — Run one full match and capture everything (Priority: P1) 🎯 MVP

**Goal**: One operator command (`bots/trainer/run.sh <rung> <iter_id>`) launches a single headless match, the bot plays to termination, and a conformant run directory (per `contracts/run-directory.md`) is produced containing `meta.json`, `bot.fsx.snapshot`, `ladder.snapshot.json`, `stdout.log`, `frames.jsonl`, `engine.stdout`, `engine.stderr`, `engine.infolog`, and `result.json`.

**Independent Test**: After completing this phase, running `bash bots/trainer/run.sh NullAI smoke` must produce a new directory under `bots/runs/` whose `result.json` has `outcome: "win"` (or at minimum a conformant `outcome` in the required enum — a win is not required for US1 to pass; conformance is).

### Implementation for User Story 1

- [ ] T017 [P] [US1] Write `/home/developer/projects/FSBarV1/bots/trainer/helpers/prelude.fsx` that issues `#r` directives for all DLLs required by the bot (`Google.Protobuf.dll`, `FsGrpc.dll`, `NodaTime.dll`, `BarData.dll`, `FSBar.Proto.dll`, `FSBar.Client.dll`) using paths relative to `src/FSBar.Client/bin/Debug/net10.0/`, and then `open`s `FSBar.Client`, `FSBar.Client.Commands`, `FSBar.Client.Callbacks`, `FSBar.Client.MapQuery`
- [ ] T018 [US1] Write `/home/developer/projects/FSBarV1/bots/trainer/helpers/log.fsx` defining `module Trainer.Log` with: (a) `Logger` type wrapping the run directory path, (b) `Logger.create : runDir:string -> Logger`, (c) `LogStart : EngineConfig -> unit` writing run start info to `stdout.log`, (d) an `EventDetail` record type (`type EventDetail = { Type: string; UnitId: int option; ActorId: int option; DefId: int option; Detail: string option }`) exported from `Trainer.Log`, (e) `LogFrame : reason:string -> frame:uint32 -> events:(string*int) list -> eventDetails:EventDetail list -> unitCount:int -> enemyCount:int -> metal:(float*float) -> energy:(float*float) -> commandsOut:int -> unit` appending one JSON line to `frames.jsonl` conforming to `contracts/frame.schema.json` (writing `event_details` only when the list is non-empty) AND calling `printfn` with a one-line human-readable description for each `EventDetail` so that `stdout.log` carries a readable chronological event trail (required by SC-002 — the operator must be able to identify specific events from the run directory without reading source), (f) `WriteResult : outcome:string -> frames:int -> cause:string -> victorySignal:string option -> errorMessage:string option -> telemetry:obj -> unit` writing `result.json` conforming to `contracts/result.schema.json`, (g) `WriteError : exn -> unit` writing a stub error `result.json`; use `System.Text.Json` from the BCL only
- [ ] T019 [US1] Write placeholder `/home/developer/projects/FSBarV1/bots/trainer/helpers/perception.fsx` containing just `module Trainer.Perception` plus a comment noting that helpers land here by extraction (no preemptive members — FR-020)
- [ ] T020 [US1] Write `/home/developer/projects/FSBarV1/bots/trainer/helpers/tactics.fsx` defining `module Trainer.Tactics` with a skeletal `TrainerLoop.run : BarClient -> Logger -> MatchResult` that: (a) warms up with `client.WaitFrames 60` capturing commander id from `UnitCreated`, (b) loops on `Protocol.receiveFrame client.Stream`, updating telemetry counters and calling `logger.LogFrame` on sampled frames (every 30 plus any frame with events), passing an `EventDetail list` built from `frame.Events` that translates each notable engine event (`UnitCreated`, `UnitFinished`, `UnitDestroyed`, `EnemyDestroyed`, `EnemyEnterLOS`, `Shutdown`) into an `EventDetail` record with a one-line human description — commander-relevant events (our commander damaged/destroyed, enemy commander spotted/destroyed) MUST be fully described so SC-002's "5 specific events" requirement can be met from `stdout.log` alone, (c) wraps every command-issuing call and every mid-frame callback query in a `try/with` that logs the exception type, message, and frame number to `stdout.log` via `printfn` and continues the loop without terminating the match; after **3 consecutive same-frame failures** it terminates the match with `outcome="error"` and `cause="repeated-frame-exception: <type>"` (implements the "commands on dead units" edge case from spec.md), (d) classifies termination based on `Shutdown` event + commander alive/dead + frame-limit, (e) returns a typed `MatchResult` record that the bot will pass to `logger.WriteResult`; this function deliberately inlines all match logic — extraction into perception/tactics helpers happens in later iterations under US4
- [ ] T021 [US1] Write `/home/developer/projects/FSBarV1/bots/trainer/bot.fsx` that: (a) `#load`s the four helper `.fsx` files in order (prelude → log → perception → tactics), (b) reads `BOT_OPPONENT`, `BOT_OPPONENT_OPTIONS`, `BOT_MAP`, `BOT_SEED`, `BOT_MAX_FRAMES`, `HIGHBAR_BOT_RUN_DIR` from env, (c) parses the opponent-options JSON into a `Map<string,string>`, (d) constructs `EngineConfig` with the new fields plus `DeathMode="com"`, `MapName`, `FixedRNGSeed`, (e) creates `BarClient`, `client.Start()`, calls `TrainerLoop.run`, catches exceptions and routes them to `logger.WriteError`, and always calls `client.Stop()` in a `finally`
- [ ] T022 [US1] Write `/home/developer/projects/FSBarV1/bots/trainer/ladder.json` with the minimum two rungs (NullAI + BARb/dev as the first competitive rung), the fixed map, and the fixed seed; values must validate against `contracts/ladder.schema.json`
- [ ] T023 [US1] Write `/home/developer/projects/FSBarV1/bots/trainer/run.sh` (bash, `chmod +x`) that: (a) takes `<rung_name>` and `<iter_id>` args, (b) ensures the feature branch is checked out, (c) runs `dotnet build -c Debug src/FSBar.Client/FSBar.Client.fsproj --nologo --verbosity quiet`, (d) reads `ladder.json` via `jq`, finds the matching rung, extracts `opponent`, `options`, `max_frames`, `map`, `seed`, (e) creates `bots/runs/<isoTimestamp>_<rungSlug>_<iterId>/` (slug = rung name with `/`→`-`), (f) writes `meta.json` conforming to `contracts/meta.schema.json` (engine version from `ls ~/.local/state/Beyond All Reason/engine/ | sort | tail -1`, git SHA from `git rev-parse --short HEAD`, host from `hostname`), (g) snapshots `bot.fsx` and `ladder.json` into the run dir, (h) exports the env vars the bot reads, (i) invokes `dotnet fsi bots/trainer/bot.fsx` with stdout/stderr redirected to `stdout.log`, (j) after bot exit, copies `stdout.log`/`stderr.log`/`infolog.txt` from the engine session dir (`/tmp/fsbar-*` matching the socket path) into the run dir as `engine.stdout`/`engine.stderr`/`engine.infolog`, (k) if `result.json` is missing writes a stub with `outcome:"error"`, `cause:"bot-exit-without-result"`, (l) prints a one-line summary from `result.json` using `jq`, (m) traps `SIGINT` to ensure engine cleanup and writes `outcome:"interrupted"`
- [ ] T024 [US1] Run `bash bots/trainer/run.sh NullAI smoke` from the repo root, inspect the produced run directory, and verify all required files from `contracts/run-directory.md` are present and `result.json` parses against `contracts/result.schema.json` (manual `jq` validation is sufficient; the spec does not require an automated schema validator)
- [ ] T025 [US1] Commit T017–T024 as one focused commit: `"020: trainer bot.fsx + helpers scaffold + run.sh runner (US1)"` and push to `origin`

**Checkpoint**: US1 is complete. A single command produces a conformant run directory. Infrastructure floor is in place.

---

## Phase 4: User Story 2 — Iterate the bot through a diagnose-improve loop (Priority: P1)

**Goal**: The operator has a documented decision tree for every iteration — read results, classify the failure, act (edit bot / fix repo / extract helper / write out-of-scope report), commit, push, advance. A history log records every iteration.

**Independent Test**: Starting from the bot produced in US1, deliberately break `bot.fsx` (e.g., remove the move command), run it, follow `PLAYBOOK.md`, and within ≤3 iterations restore a winning outcome. `HISTORY.md` shows the loss→edit→win lineage with distinct commits on the feature branch.

### Implementation for User Story 2

- [ ] T026 [US2] Write `/home/developer/projects/FSBarV1/bots/trainer/PLAYBOOK.md` capturing the operator procedure: (a) post-run analysis steps (`jq` commands to inspect `result.json`, tail `stdout.log`, tail `engine.infolog`), (b) classification decision tree with exact labels (bot-logic / repo-bug / helper-extraction / infrastructure-regression / out-of-scope / clean-win), (c) commit message templates per classification (`"trainer: bot iter N — <desc>"`, `"fix: <desc>"`, `"trainer: extract <name> helper"`, `"trainer: runner <desc>"`), (d) the exact `git add -A && git commit -m ... && git push origin 020-bot-iterative-trainer` sequence **and a push-failure recovery subsection** covering: detect a non-zero `git push` exit, preserve the local commit (do NOT amend, do NOT reset), annotate the most recent `HISTORY.md` line with an `[unpushed]` suffix, continue iterating locally if the operator chooses, and retry `git push origin 020-bot-iterative-trainer` before each subsequent iteration until it succeeds (implements FR-029), (e) the stall-check one-liner (see T028), (f) the rung-advance procedure, (g) halt conditions
- [ ] T027 [US2] Write `/home/developer/projects/FSBarV1/bots/trainer/HISTORY.md` with a header explaining the format (`iter_id | timestamp | rung_name | outcome | frames | commit_sha | run_dir_name | note`) and one seed line for the `smoke` iteration from T024
- [ ] T028 [US2] Add to `PLAYBOOK.md` a stall-detection one-liner using `jq`: iterate over the five most recent run directories matching the current rung slug, extract each `result.json.telemetry`, and print whether any of `frames_survived`, `enemy_units_killed`, `peak_metal`, `peak_energy`, `units_built` strictly increased across the five (manual inspection of the output by the operator, per research.md Decision 8)
- [ ] T029 [US2] Write `/home/developer/projects/FSBarV1/bots/trainer/README.md` with a short operator-facing intro: (a) what the trainer is, (b) pointer to `quickstart.md` in the spec dir, (c) the mantra "primary objective is the helper library; winning is the forcing function", (d) the feature's commit-and-push rule (no PR)
- [ ] T030 [US2] Deliberately regress `bot.fsx` (comment out the move command or similar), run `bots/trainer/run.sh NullAI 001`, confirm it produces a non-win conformant result, walk the PLAYBOOK to classify as `bot-logic`, revert/fix the bot, commit-and-push each step as its own iteration, and confirm after ≤3 iterations a `win` outcome on NullAI is restored; append lines to `HISTORY.md` for each iteration

**Checkpoint**: US2 is complete. The iteration loop mechanics are proven end-to-end on a real loss→fix→win cycle.

---

## Phase 5: User Story 4 — Grow a reusable helper library and infrastructure (Priority: P1)

**Goal**: The helper library accretes via explicit extractions driven by duplication in `bot.fsx`. By the end of this phase there MUST be at least three helpers in active use (logging from US1, plus at least two extracted under this phase). The run-directory contract, schemas, and playbook remain consistent with the code actually shipped.

**Independent Test**: Write a fresh minimal bot (`bots/trainer/bot.example.fsx`, temporary) that uses only the extracted helpers and the unchanged runner to produce a conformant run directory against NullAI. If the new bot works without any helper modifications, the library is usable.

### Implementation for User Story 4

- [ ] T031 [US4] During US2 iterations (or within this phase), identify perception logic that has appeared in two consecutive `bot.fsx` revisions (candidates: "find my commander unit id", "count idle constructors", "nearest enemy position", "terrain sample at position") and extract the first such duplication into `/home/developer/projects/FSBarV1/bots/trainer/helpers/perception.fsx` under `module Trainer.Perception`; update `bot.fsx` to call the helper; commit as `"trainer: extract <name> into perception helper"` and push
- [ ] T032 [US4] Identify command-issuing logic that has appeared in two consecutive `bot.fsx` revisions (candidates: "send commander to target with periodic refresh", "build one factory at the start position", "attack nearest visible enemy") and extract the first such duplication into `/home/developer/projects/FSBarV1/bots/trainer/helpers/tactics.fsx` under `module Trainer.Tactics` (not `TrainerLoop.run` itself — that already exists); update `bot.fsx` to call the helper; commit as `"trainer: extract <name> into tactics helper"` and push
- [ ] T033 [P] [US4] Update `/home/developer/projects/FSBarV1/bots/trainer/README.md` with a "Helper catalogue" section listing every helper module, its location, its currently exported members, and a one-sentence description — to be updated on every extraction commit
- [ ] T034 [US4] Write `/home/developer/projects/FSBarV1/bots/trainer/bot.example.fsx` as a minimal fresh bot that only composes the extracted helpers (prelude, log, perception, tactics) and contains no domain logic of its own; run it with a temporary invocation `BOT_OPPONENT=NullAI BOT_OPPONENT_OPTIONS='{}' BOT_MAP=... HIGHBAR_BOT_RUN_DIR=... dotnet fsi bots/trainer/bot.example.fsx` (or via a temporary `run.sh` variant) and verify it produces a conformant run directory against NullAI without modifying any helper
- [ ] T035 [US4] Delete `bot.example.fsx` after validation (it was a proof, not a shipped artifact); commit the validation evidence by adding a bullet to `HISTORY.md` noting the successful example-bot run with its run directory name
- [ ] T036 [US4] Verify SC-004: by this point the helper library MUST contain `log.fsx` plus at least two of {perception, tactics, map analysis, build orders}, all introduced via explicit extraction commits reachable from the feature branch; if the criterion is not met, continue US2 iterations until it is before proceeding to Phase 6

**Checkpoint**: US4 is complete. Helper library has ≥3 active members and a second-author smoke test passed.

---

## Phase 6: User Story 3 — Escalate through an opponent ladder (Priority: P2)

**Goal**: Once NullAI is reliably beaten, the trainer runs against the first competitive rung (BARb/dev) and — per SC-011 — clears it at least once. Further BARb rungs are attempted best-effort.

**Independent Test**: Run `bots/trainer/run.sh BARb/dev <iter>`, observe a conformant run directory, iterate until the first win on that rung. History log shows a monotonic rung progression and commit lineage.

### Implementation for User Story 3

- [ ] T037 [P] [US3] Verify the BARb rung works in isolation: run `bash bots/trainer/run.sh BARb/dev smoke`, confirm the run directory is conformant (win or loss both acceptable at this stage — the check is infrastructure correctness, not outcome)
- [ ] T038 [US3] Extend `/home/developer/projects/FSBarV1/bots/trainer/ladder.json` with additional BARb rungs (`BARb/easy`, `BARb/medium`, `BARb/hard`) using the installed difficulty profiles from Phase 2; validate the file against `contracts/ladder.schema.json`
- [ ] T039 [US3] Iterate against `BARb/dev` using the PLAYBOOK from Phase 4 until the run produces `outcome: "win"` with `victory_signal: "engine-shutdown-gameover"`; each iteration becomes a commit on the feature branch; stall-check after every 5 iterations per FR-018; if the rung stalls before a win, write a stall note and PAUSE the phase rather than skipping ahead
- [ ] T040 [P] [US3] After the first BARb/dev win, attempt one run each against `BARb/easy`, `BARb/medium`, `BARb/hard` as best-effort (no iteration commitment); append each to `HISTORY.md`; any opportunistic helper extractions triggered by these runs go into Phase 5 territory and are committed under the same rules
- [ ] T041 [US3] Verify SC-011: `HISTORY.md` shows at least one `win` on the no-op rung (from Phase 3/4) AND at least one `win` on `BARb/dev`; the feature's completion criterion is now met

**Checkpoint**: US3 is complete. The ladder works and the first competitive rung has been cleared.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Lock in documentation, close out loose ends, run the `fsdoc` agent if public API changes landed.

- [ ] T042 [P] Verify every commit made during Phases 3–6 has been pushed to `origin/020-bot-iterative-trainer` (`git status` and `git log origin/020-bot-iterative-trainer..HEAD` should both be clean)
- [ ] T043 [P] Run `dotnet build src/FSBar.Client/FSBar.Client.fsproj -c Debug` and `dotnet test tests/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug` one more time to confirm the feature branch is green end-to-end
- [ ] T044 [P] Update `/home/developer/projects/FSBarV1/CLAUDE.md` "Recent Changes" section with a one-line summary of this feature if the `update-agent-context.sh` script has not already done so (the script ran during `/speckit.plan` but verify the line is present for 020)
- [ ] T045 Run the `fsdoc` agent (`Agent` tool, `subagent_type=FSDOC_AGENT`) scoped to `src/FSBar.Client` since `EngineConfig` changed — this updates the documentation site / rendered signature files per Constitution Workflow §7
- [ ] T046 Read through `specs/020-bot-iterative-trainer/spec.md` §Success Criteria and confirm each of SC-001, SC-002, SC-003, SC-004, SC-005, SC-006 (second-operator smoke), SC-007, SC-008, SC-009, SC-010, SC-011 is satisfied by evidence in the feature branch (commits, run dirs, HISTORY entries, documentation); record any unmet criteria in a final note in `HISTORY.md` so the user can decide whether to keep iterating or close the feature
- [ ] T047 Verify the feature is spec-complete: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/*`, `quickstart.md`, `tasks.md`, `checklists/requirements.md` all present; run `git diff origin/master -- specs/020-bot-iterative-trainer/` and confirm no spec file was accidentally left unstaged

---

## Dependencies & Execution Order

### Phase ordering (hard dependencies)

```
Phase 1 (Setup) ─▶ Phase 2 (Foundational: FSBar.Client extensions + BARb patch)
                                │
                                ▼
                        Phase 3 (US1: Runner + bot skeleton)
                                │
                                ▼
                        Phase 4 (US2: Playbook + diagnose-improve loop)
                                │
                                ▼
                        Phase 5 (US4: Helper extractions — primary objective)
                                │
                                ▼
                        Phase 6 (US3: Ladder escalation)
                                │
                                ▼
                        Phase 7 (Polish, fsdoc, SC verification)
```

### Story-level dependencies

- **US1 (Phase 3)** depends on Phase 2 (needs `OpponentAIOptions` / `DeathMode` in `ScriptGenerator`).
- **US2 (Phase 4)** depends on US1 (needs a working runner and a run directory to diagnose).
- **US4 (Phase 5)** depends on US2 (helper extractions are driven by the iteration loop producing duplicated snippets).
- **US3 (Phase 6)** depends on US1 and on Phase 2 (BARb patch must be installed for difficulty profiles to work).
- US3 is scheduled AFTER US4 deliberately — the primary deliverable is the helper library, and BARb escalation is the best ground for driving additional extractions.

### Parallel opportunities within phases

- **Phase 1**: T003 and T004 can run in parallel (different directories).
- **Phase 2**: T008 (tests) can be authored in parallel with T007 (ScriptGenerator changes) since they touch different files. T013 (lua patch) and T014 (installer script) can run in parallel. T005→T006 must be sequential (same file edit order: .fs then .fsi is the safer order when using `dotnet build` to discover signature mismatches).
- **Phase 3**: T017 (prelude), T018 (log), T019 (perception stub), T020 (tactics), T022 (ladder), T023 (runner) are mostly independent files and can be authored in parallel; T021 (bot.fsx) depends on T017–T020 and must follow them; T024 (smoke run) is terminal.
- **Phase 4**: T026 (PLAYBOOK) and T027 (HISTORY) and T029 (README) are independent files and parallelizable. T028 extends T026. T030 is terminal (requires all the others).
- **Phase 5**: T031 and T032 touch different files and are parallelizable in principle, but the spec requires each extraction to arise from real duplication in prior iterations — so they will typically land sequentially in iteration order. T033 is a doc edit and parallel-safe.
- **Phase 6**: T037 is an independent smoke. T038 is a ladder edit. T039 is a multi-commit iteration loop and serial. T040 is best-effort and can be interleaved with T039 work.
- **Phase 7**: T042, T043, T044 can run in parallel. T045 and T046 run after. T047 is terminal.

---

## Implementation Strategy

**MVP scope**: Phases 1, 2, and 3. Delivers a working runner that produces a conformant run directory against the no-op opponent. This is SC-001 and a complete US1. If the user wants to stop here, they have a working infrastructure floor on the feature branch.

**Primary-objective scope**: Phases 1–5. Delivers the helper library with ≥3 active members (SC-004) and a second-author smoke test (SC-006). This is the spec's stated main objective.

**Full feature scope**: Phases 1–7. Clears the no-op rung and the first competitive rung (SC-011), runs `fsdoc`, and verifies all success criteria.

**Incremental delivery**: every Phase 3–6 task is its own commit pushed to `origin/020-bot-iterative-trainer`. The user can halt at any checkpoint and still have a coherent, tested, pushed feature branch — exactly as FR-025 through FR-030 require.

---

## Task Count Summary

| Phase | Task count | Story |
|---|---|---|
| Phase 1 — Setup | 4 (T001–T004) | — |
| Phase 2 — Foundational | 14 (T005–T009, T010a, T010b, T011, T012, T012a, T013–T016) | — |
| Phase 3 — US1 Runner | 9 (T017–T025) | US1 |
| Phase 4 — US2 Iteration loop | 5 (T026–T030) | US2 |
| Phase 5 — US4 Helper library | 6 (T031–T036) | US4 |
| Phase 6 — US3 Ladder escalation | 5 (T037–T041) | US3 |
| Phase 7 — Polish | 6 (T042–T047) | — |
| **Total** | **49** | |

**Per-story coverage**:
- US1 (P1): 9 tasks, fully independent after Phase 2.
- US2 (P1): 5 tasks, depends on US1.
- US3 (P2): 5 tasks, depends on US1 + Phase 2 (BARb patch).
- US4 (P1): 6 tasks, depends on US2. Primary deliverable.

**Parallel opportunities**: 13 tasks are marked `[P]` for parallel execution where the underlying files and prerequisites allow.

**Independent test criteria** are captured in each phase's "Goal" and "Independent Test" paragraphs and map directly to the user-story independent tests from `spec.md`.
