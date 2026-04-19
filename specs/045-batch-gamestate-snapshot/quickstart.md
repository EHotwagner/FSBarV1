# Quickstart: Batched GameState snapshot

## 1. Upgrade the proxy

Ensure the installed HighBarV2 proxy is `>= 0.1.5`. Pre-0.1.5 proxies
are unsupported — the client will raise
`ProxyVersionMismatchException` at session warmup.

## 2. Regenerate proto

Any change to `proto/highbar/callbacks.proto` must be followed by:

```
/proto-regen
```

which regenerates `src/FSBar.Proto/Generated/highbar/callbacks.proto.gen.fs`.

## 3. Build + test

```
dotnet build FSBarV1.slnx
SURFACE_AREA_UPDATE=1 dotnet test FSBarV1.slnx      # after public-surface changes only
dotnet test FSBarV1.slnx
```

## 4. Run live integration tests

```
./tests/run-all.sh
```

The live suite exercises `GetGameStateSnapshotTests` which spawns a
small mixed army via `cheat-spawn` and verifies snapshot equivalence
against engine ground truth for ≥60 ticks.

## 5. Use from FSI

```fsharp
#load "scripts/prelude.fsx"
open FSBar.Client

let client = BarClient.connect ()     // will fail fast if proxy < 0.1.5
let snap = Callbacks.getGameStateSnapshot client.Stream
printfn "f=%d friendlies=%d los=%d radar=%d M=%.0f E=%.0f"
    snap.Frame
    snap.Friendlies.Length
    snap.LosEnemies.Length
    snap.RadarOnlyEnemies.Length
    snap.Economy.MetalCurrent
    snap.Economy.EnergyCurrent
```

See `scripts/examples/23-gamestate-snapshot.fsx` for the full walkthrough.

## 6. Engine override env var

- **Preferred**: `export FSBAR_TEST_ENGINE=/path/to/spring-headless`
- **Legacy (still accepted)**: `HIGHBAR_TEST_ENGINE=...`
- If both are set to different values, `FSBAR_TEST_ENGINE` wins and a
  warning is emitted on first resolution.

## 7. Success criteria quick-check

| Criterion | How to verify |
|-----------|---------------|
| SC-002: exactly one `CALLBACK_GAME_GET_STATE` per `Update` | Wire-level counter in `GetGameStateSnapshotTests` |
| SC-003: snapshot matches engine ground truth | Live correctness test over ≥300 ticks |
| SC-005: hard-error on pre-0.1.5 proxy | Negative test using legacy proxy binary |
| SC-006: `FSBAR_TEST_ENGINE` alone works | `FSBAR_TEST_ENGINE=... ./tests/check-prerequisites.sh` |
