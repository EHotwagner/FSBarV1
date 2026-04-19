"""Simple macro-oriented Python AI — Armada tank rush.

Strategy:
  1. Commander claims the 3 nearest metal spots with armmex
  2. Commander builds 1 armsolar + 1 armvp next to itself
  3. armvp repeatedly builds armstump tanks
  4. Finished tanks collect; every 5 form a squad and attack-move to
     the last-known enemy commander position

Uses the same fsbar.hub.scripting.v1 gRPC surface as hub_full_client.py.
"""

from __future__ import annotations

import math
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
from highbar import common_pb2 as common_pb  # noqa: E402

ENDPOINT = "127.0.0.1:5021"
MAX_MSG = 64 * 1024 * 1024
SQUAD_SIZE = 5
METAL_EXPANSIONS = 3


def v3(x: float, z: float, y: float = 0.0) -> common_pb.Vector3:
    return common_pb.Vector3(x=x, y=y, z=z)


INTERNAL_ORDER = 8   # Required on every AI-issued command.
SHIFT = 32           # Append to queue instead of replacing.
MAX_TIMEOUT = 2147483647  # Without this, commands expire immediately.


def build_cmd(unit_id: int, def_id: int, pos: common_pb.Vector3,
              queue: bool = False) -> cmd_pb.AICommand:
    return cmd_pb.AICommand(build_unit=cmd_pb.BuildUnitCommand(
        unit_id=unit_id, to_build_unit_def_id=def_id, build_position=pos, facing=0,
        options=INTERNAL_ORDER | (SHIFT if queue else 0), timeout=MAX_TIMEOUT))


def move_cmd(unit_id: int, pos: common_pb.Vector3) -> cmd_pb.AICommand:
    return cmd_pb.AICommand(move_unit=cmd_pb.MoveUnitCommand(
        unit_id=unit_id, to_position=pos,
        options=INTERNAL_ORDER, timeout=MAX_TIMEOUT))


def fight_cmd(unit_id: int, pos: common_pb.Vector3) -> cmd_pb.AICommand:
    return cmd_pb.AICommand(fight=cmd_pb.FightCommand(
        unit_id=unit_id, to_position=pos,
        options=INTERNAL_ORDER, timeout=MAX_TIMEOUT))


def dist2(a: common_pb.Vector3, b) -> float:
    return (a.x - b.x) ** 2 + (a.z - b.z) ** 2


def main() -> int:
    channel = grpc.insecure_channel(ENDPOINT, options=[
        ("grpc.max_receive_message_length", MAX_MSG),
        ("grpc.max_send_message_length", MAX_MSG)])
    stub = pb_grpc.ScriptingServiceStub(channel)
    print(f"[channel] connected to {ENDPOINT}")

    stream = None

    def shutdown(*_):
        try:
            if stream is not None:
                stream.cancel()
            stub.StopSession(pb.StopSessionRequest(), timeout=5)
        except grpc.RpcError:
            pass
        sys.exit(0)

    signal.signal(signal.SIGINT, shutdown)

    try:
        # --- launch ---
        lobby = pb.LobbyConfigWire(
            map_name="Avalanche 3.4", mode=pb.LOBBY_MODE_SKIRMISH, engine_speed=1.0,
            teams=[
                pb.TeamWire(ally_team_id=0, seats=[pb.SeatWire(
                    kind=pb.SEAT_KIND_AI, side="Armada", ai_name="HighBarV2")]),
                pb.TeamWire(ally_team_id=1, seats=[pb.SeatWire(
                    kind=pb.SEAT_KIND_AI, side="Cortex", ai_name="BARb")]),
            ])
        cfg = stub.ConfigureLobby(pb.ConfigureLobbyRequest(lobby=lobby))
        if cfg.result.outcome == pb.SUBMIT_OUTCOME_REJECTED:
            print(f"[configure] rejected: {cfg.result.reason}", file=sys.stderr); return 2
        launch = stub.LaunchSession(pb.LaunchSessionRequest())
        if launch.result.outcome == pb.SUBMIT_OUTCOME_REJECTED:
            print(f"[launch] rejected: {launch.result.reason}", file=sys.stderr); return 3
        deadline = time.monotonic() + 30
        while time.monotonic() < deadline:
            if stub.GetSessionStatus(pb.GetSessionStatusRequest()).state == 3:
                break
            time.sleep(0.5)
        else:
            print("[launch] timeout", file=sys.stderr); return 3
        print("[launch]  session running")

        # --- resolve def ids ---
        def def_id(name: str) -> int:
            r = stub.GetUnitDefExtended(pb.GetUnitDefRequest(internal_name=name))
            if not r.HasField("unit_def"):
                raise RuntimeError(f"unknown unit def: {name}")
            return r.unit_def.def_id
        armmex_id = def_id("armmex")
        armsolar_id = def_id("armsolar")
        armvp_id = def_id("armvp")
        armstump_id = def_id("armstump")
        print(f"[defs]    mex={armmex_id} solar={armsolar_id} vp={armvp_id} stump={armstump_id}")

        spots = list(stub.ListMetalSpots(pb.ListMetalSpotsRequest()).spots)
        print(f"[map]     {len(spots)} metal spots")

        # --- state ---
        commander_id: int | None = None
        commander_pos: common_pb.Vector3 | None = None
        enemy_pos: common_pb.Vector3 | None = None
        vp_id: int | None = None
        tanks_pending_squad: list[int] = []
        squads_sent = 0
        mex_queued = False
        base_built = False

        stream = stub.StreamGameFrames(pb.StreamGameFramesRequest(
            client_label="simple-tank-ai", close_on_session_end=True))

        for msg in stream:
            if not msg.HasField("game_state"):
                continue
            gs = msg.game_state

            # Find commander (first friendly with health > 0 is the starter).
            if commander_id is None and gs.friendlies:
                c = gs.friendlies[0]
                commander_id = c.unit_id
                commander_pos = common_pb.Vector3(x=c.position.x, y=c.position.y, z=c.position.z)
                print(f"[init]    commander={commander_id} at ({commander_pos.x:.0f},{commander_pos.z:.0f})")

            # Snapshot enemy position for targeting.
            if enemy_pos is None:
                for e in gs.enemies:
                    if e.HasField("health") and e.health > 0:
                        enemy_pos = common_pb.Vector3(x=e.position.x, y=0, z=e.position.z)
                        print(f"[init]    enemy commander at ({enemy_pos.x:.0f},{enemy_pos.z:.0f})")
                        break

            # --- opening: mex + solar + vp ---
            if commander_id is not None and commander_pos is not None and not mex_queued:
                spots.sort(key=lambda s: dist2(commander_pos, s))
                cmds: list[cmd_pb.AICommand] = []
                for i, s in enumerate(spots[:METAL_EXPANSIONS]):
                    cmds.append(build_cmd(commander_id, armmex_id,
                                          v3(s.x, s.z, s.y), queue=(i > 0)))
                cmds.append(build_cmd(commander_id, armsolar_id,
                                      v3(commander_pos.x + 96, commander_pos.z + 96), queue=True))
                cmds.append(build_cmd(commander_id, armvp_id,
                                      v3(commander_pos.x - 128, commander_pos.z + 128), queue=True))
                r = stub.SendCommandBatch(pb.SendCommandBatchRequest(commands=cmds))
                ok = sum(1 for o in r.outcomes if o.accepted)
                print(f"[opening] queued {ok}/{len(cmds)} build cmds on commander")
                mex_queued = True

            # Detect completed vehicle plant.
            if vp_id is None:
                for f in gs.friendlies:
                    if f.def_id == armvp_id and f.finished:
                        vp_id = f.unit_id
                        print(f"[build]   vp finished: unit={vp_id}")
                        break

            # Keep VP producing stumpies (one at a time; queue again on idle).
            if vp_id is not None:
                for f in gs.friendlies:
                    if f.unit_id == vp_id and f.idle:
                        stub.SendCommandBatch(pb.SendCommandBatchRequest(commands=[
                            build_cmd(vp_id, armstump_id, v3(commander_pos.x - 200, commander_pos.z + 200))
                        ]))
                        print(f"[produce] vp queued stumpy at frame {gs.frame_number}")
                        break

            # Collect finished tanks into squads.
            tank_ids_now = {f.unit_id for f in gs.friendlies
                            if f.def_id == armstump_id and f.finished}
            for tid in tank_ids_now:
                if tid not in tanks_pending_squad and not any(
                        tid in sq for sq in getattr(main, "_sent_squads", [])):
                    tanks_pending_squad.append(tid)

            if len(tanks_pending_squad) >= SQUAD_SIZE and enemy_pos is not None:
                squad = tanks_pending_squad[:SQUAD_SIZE]
                tanks_pending_squad = tanks_pending_squad[SQUAD_SIZE:]
                main._sent_squads = getattr(main, "_sent_squads", []) + [set(squad)]
                cmds = [fight_cmd(uid, enemy_pos) for uid in squad]
                r = stub.SendCommandBatch(pb.SendCommandBatchRequest(commands=cmds))
                ok = sum(1 for o in r.outcomes if o.accepted)
                squads_sent += 1
                print(f"[squad {squads_sent}] {ok}/{len(cmds)} tanks fight-moving to "
                      f"({enemy_pos.x:.0f},{enemy_pos.z:.0f}) — ids={squad}")

            if gs.frame_number % 300 == 0:
                print(f"[status] f={gs.frame_number} friendlies={len(gs.friendlies)} "
                      f"pending_squad={len(tanks_pending_squad)} sent={squads_sent} "
                      f"M={gs.economy.metal_current:.0f}+{gs.economy.metal_income:.1f}/s "
                      f"E={gs.economy.energy_current:.0f}+{gs.economy.energy_income:.1f}/s")
        return 0
    finally:
        try:
            stub.StopSession(pb.StopSessionRequest(), timeout=5)
        except grpc.RpcError:
            pass
        channel.close()


if __name__ == "__main__":
    sys.exit(main())
