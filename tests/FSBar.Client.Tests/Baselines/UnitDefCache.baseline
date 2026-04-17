namespace FSBar.Client

open System.Net.Sockets

/// Cached unit definition data loaded from the engine at initialization.
type UnitDefInfo = {
    DefId: int
    Name: string
    Cost: float32
    BuildSpeed: float32
    MaxWeaponRange: float32
    BuildOptions: int array
}

/// Cache of all unit definitions, providing instant lookup by ID or name.
type UnitDefCache

/// Functions for loading and querying unit definitions.
module UnitDefCache =
    /// Creates an empty cache with no definitions.
    val empty: UnitDefCache

    /// Creates a cache from a sequence of UnitDefInfo values.
    val ofSeq: defs: UnitDefInfo seq -> UnitDefCache

    /// Loads all unit definitions from the engine via callbacks. One-time operation.
    val loadFromEngine: stream: NetworkStream -> UnitDefCache

    /// Looks up a unit definition by its ID. Returns None if not found.
    val tryFindById: cache: UnitDefCache -> defId: int -> UnitDefInfo option

    /// Looks up a unit definition by name. Returns None if not found.
    val tryFindByName: cache: UnitDefCache -> name: string -> UnitDefInfo option

    /// Returns all cached unit definitions.
    val all: cache: UnitDefCache -> UnitDefInfo seq
