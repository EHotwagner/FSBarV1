# Python scripting-client example

Runnable Python reference for the `fsbar.hub.scripting.v1` gRPC surface
— feature 047, sibling of `scripts/examples/24-hub-full-client.fsx`.
See `docs/scripting-polyglot.md` for the polyglot on-ramp.

## Prerequisites

- Python 3.10+ and `pip`.
- A built + running Hub (via the `/hub-run` skill). The Hub scripting service listens on
  `127.0.0.1:5021` loopback.

## Install

```bash
cd scripts/examples/python
python3 -m venv .venv
.venv/bin/pip install -r requirements.txt
```

## Regenerate bindings (optional — `generated/` is committed)

From the repo root:

```bash
python -m grpc_tools.protoc \
  -I proto \
  --python_out=scripts/examples/python/generated \
  --grpc_python_out=scripts/examples/python/generated \
  proto/hub/scripting.proto proto/highbar/*.proto
```

Regenerate after any `proto/` change.

## Run

```bash
# Hub must be running first (see prerequisites).
.venv/bin/python scripts/examples/python/hub_full_client.py
```

### Expected output

```text
[channel] connected to 127.0.0.1:5021 (64 MiB caps)
[launch]  session=S-… map=Avalanche 3.4
[tick 001] friendly=1 enemy=1 events=0
…
[tick 010] friendly=3 enemy=2 events=1
[map]     Avalanche 3.4 — 16384x10240, 12 metal spots
[unitdef] armcom — cost(metal=2500, energy=46000) sight=600 builds=74
[batch]   forwarded_at_frame=… outcomes=1 (accepted=1)
[stop]    session -> Idle
```

Exits 0 on success.

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `could not reach 127.0.0.1:5021 — is the Hub running?` | Start the Hub (`/hub-run` or the `/hub-run` skill). |
| `RESOURCE_EXHAUSTED: Received message larger than max` | Channel cap not set — the script already sets `grpc.max_receive_message_length = 64*1024*1024` (FR-004); verify if you forked it. |
| Ctrl-C leaves the Hub busy | The SIGINT handler calls `StopSession` before exit; allow ≤15 s for the Hub session to return to Idle (SC-006). |
| `ModuleNotFoundError: hub` | Regenerate bindings (see above); `generated/` must sit next to the script. |

## Files

| Path | Purpose |
|---|---|
| `hub_full_client.py` | Linear walkthrough of the five capability families (≤200 LOC). |
| `requirements.txt` | `grpcio` + `grpcio-tools` pins (runtime only needs `grpcio`). |
| `generated/` | Committed `*_pb2.py` / `*_pb2_grpc.py` for zero-setup runs. |
