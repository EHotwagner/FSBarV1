// 23-gamestate-snapshot.fsx — Feature 045 walkthrough.
//
// Demonstrates the single-RPC CALLBACK_GAME_GET_STATE=15 snapshot that
// replaces the legacy per-unit / per-enemy / per-resource refresh loop
// in FSBar.Client.GameState.
//
// Prereqs:
//   - HighBarV2 proxy >= 0.1.5 installed (BarClient.connect preflights
//     the snapshot and raises ProxyVersionMismatchException otherwise).
//   - Engine auto-discovered or FSBAR_TEST_ENGINE / HIGHBAR_TEST_ENGINE set.
//
//   dotnet fsi scripts/examples/23-gamestate-snapshot.fsx

#load "../prelude.fsx"

open FSBar.Client

let client = BarClient.startHeadless ()
try
    // Let the game tick for a moment so there's something to observe.
    client.WaitFrames 120 (fun _ -> ())

    let snap = Callbacks.getGameStateSnapshot client.Stream
    printfn "frame=%d friendlies=%d los=%d radar=%d M=%.0f E=%.0f"
        snap.Frame
        snap.Friendlies.Length
        snap.LosEnemies.Length
        snap.RadarOnlyEnemies.Length
        snap.Economy.MetalCurrent
        snap.Economy.EnergyCurrent

    // Show the per-friendly breakdown for the first few units.
    snap.Friendlies
    |> List.truncate 5
    |> List.iter (fun f ->
        let (x, _y, z) = f.Position
        printfn "  friendly id=%d def=%d pos=(%.1f, %.1f) hp=%.0f" f.UnitId f.UnitDefId x z f.Health)
finally
    client.Stop ()
