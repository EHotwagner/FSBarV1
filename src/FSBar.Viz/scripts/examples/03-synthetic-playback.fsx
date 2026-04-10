// Example 03: Play back a synthetic scene with PreviewSession
// Requires DISPLAY environment variable for windowed rendering
#load "../prelude.fsx"
open Prelude
open FSBar.SyntheticData

printfn "Generating SceneA..."
let handle = playScene SceneId.SceneA 30
printfn "Playback started at 30 FPS. Press Enter to stop."
System.Console.ReadLine() |> ignore
handle.Dispose()
printfn "Done."
