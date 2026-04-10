module FSBar.SyntheticData.Tests.ContinuityTests

open Xunit
open FSBar.SyntheticData

[<Fact>]
let ``Scene A passes continuity validation`` () =
    let scene = Scenes.generate SceneA
    let errors = Validation.validateContinuity scene
    let msg = errors |> String.concat "; "
    Assert.True(errors.IsEmpty, $"Continuity errors: {msg}")

[<Fact>]
let ``Scene B passes continuity validation`` () =
    let scene = Scenes.generate SceneB
    let errors = Validation.validateContinuity scene
    let msg = errors |> String.concat "; "
    Assert.True(errors.IsEmpty, $"Continuity errors: {msg}")

[<Fact>]
let ``Scene C passes continuity validation`` () =
    let scene = Scenes.generate SceneC
    let errors = Validation.validateContinuity scene
    let msg = errors |> String.concat "; "
    Assert.True(errors.IsEmpty, $"Continuity errors: {msg}")

[<Fact>]
let ``All scenes have different map dimensions`` () =
    let scenes = Scenes.generateAll ()
    let dims = scenes |> List.map (fun s -> (s.MapWidth, s.MapHeight)) |> Set.ofList
    Assert.Equal(3, dims.Count)
