module FSBar.Client.Tests.SurfaceAreaTests

open System.IO
open Xunit
open FSBar.Tests.Common

let srcDir =
    Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "src", "FSBar.Client") |> Path.GetFullPath

let baselinesDir =
    Path.Combine(__SOURCE_DIRECTORY__, "Baselines") |> Path.GetFullPath

let moduleNames : obj[] seq =
    SurfaceAreaHelper.enumerateBaselineModules baselinesDir

[<Theory>]
[<MemberData(nameof moduleNames)>]
let ``baseline_matches_fsi_surface`` (moduleName: string) =
    SurfaceAreaHelper.verifyModule srcDir baselinesDir moduleName

[<Fact>]
let ``all_fsi_modules_have_baselines`` () =
    SurfaceAreaHelper.verifyAllModulesHaveBaselines srcDir baselinesDir

[<Fact>]
let ``no_orphaned_baselines_exist`` () =
    SurfaceAreaHelper.verifyNoOrphanedBaselines srcDir baselinesDir
