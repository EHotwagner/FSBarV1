module FSBar.SyntheticData.Tests.ValidationTests

open Xunit
open FSBar.Client
open FSBar.SyntheticData

[<Fact>]
let ``Scene A passes validation`` () =
    let scene = Scenes.generate SceneA
    let errors = Validation.validate scene
    let msg = errors |> String.concat "; "
    Assert.True(errors.IsEmpty, $"Validation errors: {msg}")

[<Fact>]
let ``Scene B passes validation`` () =
    let scene = Scenes.generate SceneB
    let errors = Validation.validate scene
    let msg = errors |> String.concat "; "
    Assert.True(errors.IsEmpty, $"Validation errors: {msg}")

[<Fact>]
let ``Scene C passes validation`` () =
    let scene = Scenes.generate SceneC
    let errors = Validation.validate scene
    let msg = errors |> String.concat "; "
    Assert.True(errors.IsEmpty, $"Validation errors: {msg}")
