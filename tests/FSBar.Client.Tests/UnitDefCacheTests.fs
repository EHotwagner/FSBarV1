module FSBar.Client.Tests.UnitDefCacheTests

open Xunit
open FSBar.Client

let sampleInfo1 =
    { DefId = 42; Name = "armcom"; Cost = 500.0f; BuildSpeed = 10.0f; MaxWeaponRange = 200.0f; BuildOptions = [| 1; 2; 3 |] }

let sampleInfo2 =
    { DefId = 55; Name = "armmex"; Cost = 50.0f; BuildSpeed = 0.0f; MaxWeaponRange = 0.0f; BuildOptions = [||] }

let singleCache () = UnitDefCache.ofSeq [ sampleInfo1 ]
let multiCache () = UnitDefCache.ofSeq [ sampleInfo1; sampleInfo2 ]

[<Fact>]
let ``empty_cache_has_no_definitions`` () =
    let cache = UnitDefCache.empty
    Assert.Empty(UnitDefCache.all cache)

[<Fact>]
let ``empty_cache_tryFindById_returns_none`` () =
    let cache = UnitDefCache.empty
    Assert.True((UnitDefCache.tryFindById cache 1).IsNone)

[<Fact>]
let ``empty_cache_tryFindByName_returns_none`` () =
    let cache = UnitDefCache.empty
    Assert.True((UnitDefCache.tryFindByName cache "armcom").IsNone)

[<Fact>]
let ``tryFindById_returns_correct_info`` () =
    let cache = singleCache ()
    let result = UnitDefCache.tryFindById cache 42
    Assert.True(result.IsSome)
    Assert.Equal("armcom", result.Value.Name)
    Assert.Equal(500.0f, result.Value.Cost)
    Assert.Equal(10.0f, result.Value.BuildSpeed)
    Assert.Equal(200.0f, result.Value.MaxWeaponRange)
    Assert.Equal<int array>([| 1; 2; 3 |], result.Value.BuildOptions)

[<Fact>]
let ``tryFindById_nonexistent_returns_none`` () =
    let cache = singleCache ()
    Assert.True((UnitDefCache.tryFindById cache 999).IsNone)

[<Fact>]
let ``tryFindByName_returns_correct_info`` () =
    let cache = singleCache ()
    let result = UnitDefCache.tryFindByName cache "armcom"
    Assert.True(result.IsSome)
    Assert.Equal(42, result.Value.DefId)

[<Fact>]
let ``tryFindByName_nonexistent_returns_none`` () =
    let cache = singleCache ()
    Assert.True((UnitDefCache.tryFindByName cache "corcom").IsNone)

[<Fact>]
let ``all_returns_full_set`` () =
    let cache = multiCache ()
    let all = UnitDefCache.all cache |> Seq.toList
    Assert.Equal(2, all.Length)

[<Fact>]
let ``ofSeq_creates_valid_cache`` () =
    let cache = multiCache ()
    Assert.True((UnitDefCache.tryFindByName cache "armcom").IsSome)
    Assert.True((UnitDefCache.tryFindByName cache "armmex").IsSome)
    Assert.True((UnitDefCache.tryFindById cache 42).IsSome)
    Assert.True((UnitDefCache.tryFindById cache 55).IsSome)
