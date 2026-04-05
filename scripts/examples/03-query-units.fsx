// 03-query-units.fsx — Query BarData unit definitions (no game needed)
// Usage: dotnet fsi scripts/examples/03-query-units.fsx

#load "../prelude.fsx"

printfn "Total units: %d" AllUnits.all.Length

let builders =
    AllUnits.all
    |> List.filter (fun u -> u.isBuilder && not u.canFly)
printfn "Ground builders: %d" builders.Length

let armedUnits =
    AllUnits.all
    |> List.filter (fun u -> u.isArmed && u.isMobile)
printfn "Mobile armed units: %d" armedUnits.Length

// Look up a specific unit
let armcom =
    AllUnits.all
    |> List.tryFind (fun u -> u.name = "armcom")
match armcom with
| Some u ->
    printfn "armcom: metal=%A energy=%A health=%A builder=%b" u.metalCost u.energyCost u.health u.isBuilder
| None ->
    printfn "armcom not found"
