"""Polyglot scripting-client reference — feature 047.

End-to-end Python walkthrough of the five fsbar.hub.scripting.v1
capability families (session lifecycle, state+events stream, map data,
unit-def query, batch command). Mirrors scripts/examples/24-hub-full-client.fsx.

Run:
    pip install -r requirements.txt
    # If generated/ is absent, from repo root:
    #   python -m grpc_tools.protoc -I ../../../proto \
    #     --python_out=generated --grpc_python_out=generated \
    #     ../../../proto/hub/scripting.proto ../../../proto/highbar/*.proto
    python hub_full_client.py
"""

from __future__ import annotations

import os
import signal
import sys
import time

sys.stdout.reconfigure(line_buffering=True)
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "generated"))

import grpc  # noqa: E402

from hub import scripting_pb2 as pb  # noqa: E402
from hub import scripting_pb2_grpc as pb_grpc  # noqa: E402
from highbar import commands_pb2 as cmd_pb  # noqa: E402

ENDPOINT = "127.0.0.1:5021"
MAX_MSG = 64 * 1024 * 1024  # FR-004: 64 MiB channel cap


def die_unreachable() -> None:
    print(f"could not reach {ENDPOINT} — is the Hub running? (run: /skill hub-run)",
          file=sys.stderr)
    sys.exit(1)


def main() -> int:
    # --- 1. channel setup ---
    channel = grpc.insecure_channel(
        ENDPOINT,
        options=[("grpc.max_receive_message_length", MAX_MSG),
                 ("grpc.max_send_message_length", MAX_MSG)],
    )
    stub = pb_grpc.ScriptingServiceStub(channel)
    print(f"[channel] connected to {ENDPOINT} (64 MiB caps)")

    stream_call = None

    def shutdown(_signum=None, _frame=None) -> None:
        try:
            if stream_call is not None:
                stream_call.cancel()
            stub.StopSession(pb.StopSessionRequest(), timeout=5)
        except grpc.RpcError:
            pass
        sys.exit(0)

    signal.signal(signal.SIGINT, shutdown)

    try:
        # --- 2. ConfigureLobby + LaunchSession ---
        lobby = pb.LobbyConfigWire(
            map_name="Avalanche 3.4",
            mode=pb.LOBBY_MODE_SKIRMISH,
            engine_speed=1.0,
            launch_graphical_viewer=False,
            teams=[
                pb.TeamWire(ally_team_id=0, seats=[
                    pb.SeatWire(kind=pb.SEAT_KIND_AI, side="Armada", ai_name="HighBarV2")]),
                pb.TeamWire(ally_team_id=1, seats=[
                    pb.SeatWire(kind=pb.SEAT_KIND_AI, side="Cortex", ai_name="BARb")]),
            ],
        )
        try:
            cfg = stub.ConfigureLobby(pb.ConfigureLobbyRequest(lobby=lobby), timeout=10)
        except grpc.RpcError as e:
            if e.code() == grpc.StatusCode.UNAVAILABLE:
                die_unreachable()
            raise
        if cfg.result.outcome == pb.SUBMIT_OUTCOME_REJECTED:
            print(f"[configure] rejected: {cfg.result.reason}", file=sys.stderr)
            return 2

        launch = stub.LaunchSession(pb.LaunchSessionRequest(start_paused=False,
                                                            launch_graphical_viewer=False))
        if launch.result.outcome == pb.SUBMIT_OUTCOME_REJECTED:
            print(f"[launch] rejected: {launch.result.reason}", file=sys.stderr)
            return 3
        # Launch is async — poll until state=RUNNING (FR-005 state machine).
        deadline = time.monotonic() + 30
        while time.monotonic() < deadline:
            st = stub.GetSessionStatus(pb.GetSessionStatusRequest())
            if st.state == 3:  # RUNNING
                sid = st.active_session.session_id if st.HasField("active_session") else "?"
                print(f"[launch]  session={sid} map=Avalanche 3.4")
                break
            if st.state == 5:  # FAILED
                print(f"[launch] FAILED: {st.failure.reason}", file=sys.stderr)
                return 3
            time.sleep(0.5)
        else:
            print("[launch] timed out waiting for RUNNING", file=sys.stderr)
            return 3

        # --- 3. StreamGameFrames x 10 ticks ---
        stream_call = stub.StreamGameFrames(
            pb.StreamGameFramesRequest(client_label="py-full-client",
                                       close_on_session_end=False))
        ticks = 0
        for msg in stream_call:
            if msg.HasField("game_state"):
                gs = msg.game_state
                print(f"[tick {gs.frame_number:03d}] "
                      f"friendly={len(gs.friendlies)} enemy={len(gs.enemies)} "
                      f"events={len(msg.game_events)}")
                ticks += 1
                if ticks >= 10:
                    break
        stream_call.cancel()

        # --- 4. map + unit-def queries ---
        mi = stub.GetMapInfo(pb.GetMapInfoRequest())
        spots = stub.ListMetalSpots(pb.ListMetalSpotsRequest())
        print(f"[map]     {mi.map_name} — {mi.width}x{mi.height}, "
              f"{len(spots.spots)} metal spots")
        ud = stub.GetUnitDefExtended(pb.GetUnitDefRequest(internal_name="armcom"))
        if ud.HasField("unit_def"):
            u = ud.unit_def
            print(f"[unitdef] armcom — cost(metal={u.cost.metal:.0f}, "
                  f"energy={u.cost.energy:.0f}) sight={u.sight_range_elmo:.0f} "
                  f"builds={len(u.build_options)}")

        # --- 5. batch + stop ---
        noop = cmd_pb.AICommand()
        batch = stub.SendCommandBatch(pb.SendCommandBatchRequest(commands=[noop]))
        accepted = sum(1 for o in batch.outcomes if o.accepted)
        print(f"[batch]   forwarded_at_frame={batch.forwarded_at_frame} "
              f"outcomes={len(batch.outcomes)} (accepted={accepted})")

        stub.StopSession(pb.StopSessionRequest())
        print("[stop]    session -> Idle")
        return 0
    finally:
        try:
            stub.StopSession(pb.StopSessionRequest(), timeout=5)
        except grpc.RpcError:
            pass
        channel.close()


if __name__ == "__main__":
    sys.exit(main())
