namespace FSBar.Client

open System.Net.Sockets

type UnitDefInfo = {
    DefId: int
    Name: string
    Cost: float32
    BuildSpeed: float32
    MaxWeaponRange: float32
    BuildOptions: int array
}

type UnitDefCache =
    { ById: Map<int, UnitDefInfo>
      ByName: Map<string, int> }

module UnitDefCache =
    let empty : UnitDefCache =
        { ById = Map.empty; ByName = Map.empty }

    let ofSeq (defs: UnitDefInfo seq) : UnitDefCache =
        let mutable byId = Map.empty
        let mutable byName = Map.empty
        for info in defs do
            byId <- Map.add info.DefId info byId
            byName <- Map.add info.Name info.DefId byName
        { ById = byId; ByName = byName }

    let loadFromEngine (stream: NetworkStream) : UnitDefCache =
        let defIds = Callbacks.getUnitDefs stream 5000
        let mutable byId = Map.empty
        let mutable byName = Map.empty
        for defId in defIds do
            let name = Callbacks.getUnitDefName stream defId
            if name <> "" then
                let info =
                    { DefId = defId
                      Name = name
                      Cost = Callbacks.getUnitDefCost stream defId
                      BuildSpeed = Callbacks.getBuildSpeed stream defId
                      MaxWeaponRange = Callbacks.getMaxWeaponRange stream defId
                      BuildOptions = Callbacks.getBuildOptions stream defId }
                byId <- Map.add defId info byId
                byName <- Map.add name defId byName
        { ById = byId; ByName = byName }

    let tryFindById (cache: UnitDefCache) (defId: int) : UnitDefInfo option =
        Map.tryFind defId cache.ById

    let tryFindByName (cache: UnitDefCache) (name: string) : UnitDefInfo option =
        match Map.tryFind name cache.ByName with
        | Some defId -> Map.tryFind defId cache.ById
        | None -> None

    let all (cache: UnitDefCache) : UnitDefInfo seq =
        cache.ById |> Map.toSeq |> Seq.map snd
