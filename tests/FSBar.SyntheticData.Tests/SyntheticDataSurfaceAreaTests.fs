module FSBar.SyntheticData.Tests.SurfaceAreaTests

open System.IO
open Xunit
open FSBar.Tests.Common

let srcDir =
    Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "FSBar.SyntheticData") |> Path.GetFullPath

let baselinesDir =
    Path.Combine(__SOURCE_DIRECTORY__, "Baselines") |> Path.GetFullPath

let moduleNames : obj[] seq =
    SurfaceAreaHelper.enumerateBaselineModules baselinesDir

[<Theory>]
[<MemberData(nameof moduleNames)>]
let ``baseline_matches_fsi_surface`` (moduleName: string) =
    SurfaceAreaHelper.verifyModule srcDir baselinesDir moduleName

[<Fact>]
let ``no_orphaned_baselines_exist`` () =
    SurfaceAreaHelper.verifyNoOrphanedBaselines srcDir baselinesDir
