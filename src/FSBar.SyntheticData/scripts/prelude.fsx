// FSBar.SyntheticData FSI Prelude
// Usage: #load "src/FSBar.SyntheticData/scripts/prelude.fsx"

#r "../bin/Debug/net10.0/FSBar.Client.dll"
#r "../bin/Debug/net10.0/FSBar.Proto.dll"
#r "../bin/Debug/net10.0/FSBar.SyntheticData.dll"

open FSBar.Client
open FSBar.SyntheticData

let generate = Scenes.generate
let generateAll = Scenes.generateAll
let validate = Validation.validate
let validateContinuity = Validation.validateContinuity

printfn "FSBar.SyntheticData prelude loaded."
printfn "  generate SceneA / SceneB / SceneC"
printfn "  generateAll ()"
printfn "  validate scene"
printfn "  validateContinuity scene"
