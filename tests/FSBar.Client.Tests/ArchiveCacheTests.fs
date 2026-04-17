module FSBar.Client.Tests.ArchiveCacheTests

open System.IO
open Xunit
open FSBar.Client

let private sampleCache = """
local archiveCache = {
    archives = {
        {
            name = "avalanche_3.4.sd7",
            path = "/example/maps/",
            modified = "1775074005",
            checksum = "0",
            archivedata = {
                author = "IceXuick",
                description = "Avalanche Remake",
                mapfile = "maps/Avalancher.smf",
                modtype = 3,
                name = "Avalanche 3.4",
                name_pure = "Avalanche",
                version = "3.4",
                depend = {
                    "Spring content v1",
                },
            },
        },
        {
            name = "09721a2e7c43dd5e7be4f2a73dd8eb01.sdp",
            path = "/example/packages/",
            modified = "1775968577",
            checksum = "0",
            archivedata = {
                description = "BYAR mutator",
                modtype = 5,
                name = "BYAR Chobby test-4509-0c4f743",
                name_pure = "BYAR Chobby",
                version = "test-4509-0c4f743",
                depend = { "Spring content v1", },
            },
        },
        {
            name = "all_that_smolders_v1.2.sd7",
            path = "/example/maps/",
            modified = "1111",
            checksum = "0",
            archivedata = {
                mapfile = "maps/ATS.smf",
                modtype = 3,
                name = "All That Smolders v1.2",
                name_pure = "All That Smolders",
                version = "1.2",
                depend = { "Map Helper v1", },
            },
        },
    },
}
"""

[<Fact>]
let ``parse_extracts_maps_by_modtype_3`` () =
    let entries = ArchiveCache.parse sampleCache
    Assert.Equal(2, entries.Length)
    let byStem = entries |> List.map (fun e -> e.FileStem, e) |> Map.ofList
    Assert.True(Map.containsKey "avalanche_3.4" byStem)
    Assert.True(Map.containsKey "all_that_smolders_v1.2" byStem)

[<Fact>]
let ``parse_preserves_engine_name_with_version_suffix`` () =
    let entries = ArchiveCache.parse sampleCache
    let avalanche = entries |> List.find (fun e -> e.FileStem = "avalanche_3.4")
    Assert.Equal("Avalanche 3.4", avalanche.EngineName)
    Assert.Equal("Avalanche", avalanche.NamePure)
    Assert.Equal(Some "3.4", avalanche.Version)

[<Fact>]
let ``parse_handles_nested_depend_table_without_truncating`` () =
    // The sample's `depend = { ... }` sits inside `archivedata = { ... }`.
    // A naive regex-only parser would close the outer block early and
    // miss the `version` field, since it lives after `depend`.
    let entries = ArchiveCache.parse sampleCache
    let smolders = entries |> List.find (fun e -> e.FileStem = "all_that_smolders_v1.2")
    Assert.Equal("All That Smolders v1.2", smolders.EngineName)
    Assert.Equal(Some "1.2", smolders.Version)

[<Fact>]
let ``parse_skips_non_map_archives`` () =
    let entries = ArchiveCache.parse sampleCache
    Assert.DoesNotContain(entries, fun e -> e.ArchiveFileName.EndsWith(".sdp"))

[<Fact>]
let ``loadMaps_returns_empty_for_missing_file`` () =
    let tempMiss = Path.Combine(Path.GetTempPath(), "does-not-exist-" + Path.GetRandomFileName())
    Assert.Empty(ArchiveCache.loadMaps tempMiss)

[<Fact>]
let ``loadMapsForDataDir_against_live_install_returns_known_map`` () =
    // Opportunistic: if a real BAR install exists on this dev machine,
    // the cache should name at least Avalanche correctly. Skip when
    // the cache is missing — we don't want a machine-configuration
    // dependency to fail an otherwise-clean test run.
    match EngineDiscovery.defaultDataDir () with
    | None -> ()
    | Some dataDir ->
        let cachePath = ArchiveCache.defaultCachePath dataDir
        if not (File.Exists cachePath) then ()
        else
            let entries = ArchiveCache.loadMapsForDataDir dataDir
            let avalanche = entries |> List.tryFind (fun e -> e.FileStem = "avalanche_3.4")
            match avalanche with
            | None -> () // not every dev box has this specific map
            | Some e ->
                Assert.Equal("Avalanche 3.4", e.EngineName)
