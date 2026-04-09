// 02-graphical-game.fsx — Launch graphical BAR and control from REPL
// Usage: dotnet fsi scripts/examples/02-graphical-game.fsx

#load "../prelude.fsx"

printfn "Starting graphical BAR session..."
open FSBar

let client = BarClient.startGraphical()

printfn "Running 300 frames (observe the game window)..."
client.WaitFrames 300 (fun frame ->
    let cmds =
        frame.Events
        |> List.choose (fun ev ->
            match ev with
            | GameEvent.UnitIdle unitId ->
                Some (MoveCommand unitId 4096.0f 100.0f 4096.0f)
            | _ -> None
        )
    if not cmds.IsEmpty then
        client.SendCommands cmds)

printfn "Stopping..."
client.Stop()
printfn "Done."
