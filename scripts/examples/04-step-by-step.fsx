// 04-step-by-step.fsx — Frame-by-frame stepping and continuous run
// Usage: dotnet fsi scripts/examples/04-step-by-step.fsx

#load "../prelude.fsx"

printfn "Starting headless BAR session..."
use client = BarClient.startHeadless()

// Step through 5 frames one at a time
printfn "\n--- Stepping 5 frames ---"
client.WaitFrames 5 (fun frame ->
    let unitEvents =
        frame.Events
        |> List.filter (fun ev ->
            match ev with
            | GameEvent.UnitCreated _ | GameEvent.UnitFinished _ | GameEvent.UnitIdle _ -> true
            | _ -> false)
    if not unitEvents.IsEmpty then
        printfn "Frame %d: %A" frame.FrameNumber unitEvents)

// Run 100 frames continuously with a handler
printfn "\n--- Running 100 frames with handler ---"
let mutable frameCount = 0
client.WaitFrames 100 (fun frame ->
    frameCount <- frameCount + 1
    let cmds =
        frame.Events
        |> List.choose (fun ev ->
            match ev with
            | GameEvent.UnitIdle unitId ->
                printfn "  [Frame %d] Unit %d idle — sending move" frame.FrameNumber unitId
                Some (MoveCommand unitId 4096.0f 100.0f 4096.0f)
            | _ -> None
        )
    if not cmds.IsEmpty then
        client.SendCommands cmds)
printfn "Completed %d frames." frameCount

printfn "\nStopping..."
client.Stop()
printfn "Done."
