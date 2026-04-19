# Quickstart — Polyglot scripting client (Python)

Reader profile: Python dev, no F# exposure. Goal: first live frame in
under 30 minutes (SC-001).

## 1. Prerequisites

- Python 3.10+ and `pip` on `PATH`.
- A built FSBar Hub from this repo (one-time: `dotnet build
  FSBarV1.slnx`). Launch via the `/hub-run` skill or
  `dotnet run --project src/FSBar.Hub.App`.
- Avalanche 3.4 (or any map you prefer — the example defaults to
  Avalanche 3.4 for SC-002 reproducibility).

## 2. Install Python deps

```bash
cd scripts/examples/python
pip install -r requirements.txt    # grpcio + grpcio-tools
```

## 3. Generate bindings (skip if `generated/` is committed)

From the repo root:

```bash
python -m grpc_tools.protoc \
  -I proto \
  --python_out=scripts/examples/python/generated \
  --grpc_python_out=scripts/examples/python/generated \
  proto/hub/scripting.proto proto/highbar/*.proto
```

## 4. Start the Hub

```bash
# via the skill (recommended):
# /hub-run
# or manually:
dotnet run --project src/FSBar.Hub.App
```

The Hub scripting service listens on `127.0.0.1:5021` (insecure,
loopback-only). Confirm from the Hub GUI's Log tab that
`ScriptingService` is registered.

## 5. Run the Python client

```bash
python scripts/examples/python/hub_full_client.py
```

Expected output (abridged):

```text
[channel] connected to 127.0.0.1:5021 (64 MiB caps)
[launch]  session=S-… map=Avalanche 3.4
[tick 001] friendly=1 enemy=1 events=0
[tick 002] friendly=1 enemy=1 events=0
…
[tick 010] friendly=3 enemy=2 events=1
[map]     Avalanche 3.4 — 16384×10240, 12 metal spots
[unitdef] armcom — cost(metal=2500, energy=46000) sight=600 builds=74
[batch]   forwarded_at_frame=… outcomes=1 (accepted=1)
[stop]    session -> Idle
```

Exits 0 on success.

## 6. What to read next

- `docs/scripting-polyglot.md` — RPC catalog for the five capability
  families.
- `scripts/examples/24-hub-full-client.fsx` — F# sibling for
  cross-reference.
- `proto/hub/scripting.proto` + `proto/highbar/*.proto` — the
  authoritative wire contract.

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `could not reach 127.0.0.1:5021 — is the Hub running?` | Start the Hub (step 4). |
| `RESOURCE_EXHAUSTED: Received message larger than max` | Channel cap not set — ensure `grpc.max_receive_message_length = 64*1024*1024` (FR-004). |
| Ctrl-C leaves the Hub busy | Wait ≤15 s for the Hub to return to Idle (SC-006). If still stuck, check Hub logs — but the example's signal handler should call `StopSession` before exit. |
| Bindings fail to import after a `git pull` | Re-run step 3; proto surface may have moved. |
