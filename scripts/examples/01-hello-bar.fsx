// 01-hello-bar.fsx — Minimal example: start headless, receive events, stop
// Usage: dotnet fsi scripts/examples/01-hello-bar.fsx

#load "../prelude.fsx"

printfn "Starting headless BAR session..."
use client = BarClient.startHeadless()

printfn "Stepping 5 frames..."
for i in 1..5 do
    let frame = client.Step()
    printfn "  Frame %d: %d events" frame.FrameNumber frame.Events.Length
    for ev in frame.Events do
        match ev with
        | GameEvent.Init teamId -> printfn "    Init: team %d" teamId
        | GameEvent.UnitCreated(uid, bid) -> printfn "    UnitCreated: %d (builder: %d)" uid bid
        | GameEvent.UnitFinished uid -> printfn "    UnitFinished: %d" uid
        | GameEvent.Update f -> printfn "    Update: frame %d" f
        | _ -> ()

printfn "Stopping..."
client.Stop()
printfn "Done."
