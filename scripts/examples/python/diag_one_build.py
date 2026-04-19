"""Diagnostic: launch a fresh session, issue ONE unary SendCommand to
build a single armmex on the closest metal spot, then print every
friendly unit per tick for 60 seconds to see if the commander responds.
"""
from __future__ import annotations
import os, sys, time
sys.stdout.reconfigure(line_buffering=True)
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "generated"))
import grpc
from hub import scripting_pb2 as pb, scripting_pb2_grpc as pb_grpc
from highbar import commands_pb2 as cmd_pb, common_pb2 as common_pb

INTERNAL_ORDER = 8
MAX_TIMEOUT = 2147483647

ch = grpc.insecure_channel("127.0.0.1:5021",
    options=[("grpc.max_receive_message_length", 64*1024*1024),
             ("grpc.max_send_message_length", 64*1024*1024)])
s = pb_grpc.ScriptingServiceStub(ch)

lobby = pb.LobbyConfigWire(
    map_name="Avalanche 3.4", mode=pb.LOBBY_MODE_SKIRMISH, engine_speed=1.0,
    teams=[pb.TeamWire(ally_team_id=0, seats=[pb.SeatWire(
        kind=pb.SEAT_KIND_AI, side="Armada", ai_name="HighBarV2")]),
           pb.TeamWire(ally_team_id=1, seats=[pb.SeatWire(
        kind=pb.SEAT_KIND_AI, side="Cortex", ai_name="BARb")])])
s.ConfigureLobby(pb.ConfigureLobbyRequest(lobby=lobby))
s.LaunchSession(pb.LaunchSessionRequest())
for _ in range(60):
    if s.GetSessionStatus(pb.GetSessionStatusRequest()).state == 3: break
    time.sleep(0.5)
print("[launch] running")

armmex = s.GetUnitDefExtended(pb.GetUnitDefRequest(internal_name="armmex")).unit_def.def_id
spots = list(s.ListMetalSpots(pb.ListMetalSpotsRequest()).spots)
print(f"[defs] armmex={armmex}  spots={len(spots)}")

stream = s.StreamGameFrames(pb.StreamGameFramesRequest(client_label="diag"))
ordered = False
last_frame = 0
for msg in stream:
    if not msg.HasField("game_state"): continue
    gs = msg.game_state
    last_frame = gs.frame_number
    if gs.friendlies:
        frs = [(f.unit_id, f.def_id, f.finished, f.idle,
                f"{f.position.x:.0f},{f.position.z:.0f}") for f in gs.friendlies]
        print(f"f={gs.frame_number} friendlies={frs}")
    if not ordered and gs.friendlies:
        c = gs.friendlies[0]
        spots.sort(key=lambda sp: (sp.x-c.position.x)**2 + (sp.z-c.position.z)**2)
        sp0 = spots[0]
        build = cmd_pb.AICommand(build_unit=cmd_pb.BuildUnitCommand(
            unit_id=c.unit_id, to_build_unit_def_id=armmex,
            build_position=common_pb.Vector3(x=sp0.x, y=sp0.y, z=sp0.z),
            facing=0, options=INTERNAL_ORDER, timeout=MAX_TIMEOUT))
        r = s.SendCommand(pb.SendCommandRequest(command=build))
        print(f"[order] SendCommand unary → forwarded_at_frame={r.forwarded_at_frame} "
              f"(build mex at {sp0.x:.0f},{sp0.z:.0f} via commander {c.unit_id})")
        ordered = True
    if last_frame >= 1500: break

print(f"[done] last frame={last_frame}")
stream.cancel()
s.StopSession(pb.StopSessionRequest())
