module FSBar.Hub.Tests.ProxyInstallerTests

// Pure tests for ProxyInstaller.rewriteSimpleAiList (feature
// 035-central-gui-hub T035). Golden fixture at
// tests/FSBar.Hub.Tests/fixtures/IGL_data.lua is committed to the
// repo so the roundtrip test is reproducible.

open System
open System.IO
open Xunit
open FSBar.Hub

let private loadFixture () =
    let path = Path.Combine(__SOURCE_DIRECTORY__, "fixtures", "IGL_data.lua")
    File.ReadAllText(path)

[<Fact>]
let ``rewriteSimpleAiList flips true to false and preserves the rest`` () =
    let original = loadFixture ()
    match ProxyInstaller.rewriteSimpleAiList original with
    | None -> Assert.Fail("expected a rewrite on fixture with simpleAiList = true")
    | Some rewritten ->
        Assert.Contains("simpleAiList = false,", rewritten)
        Assert.DoesNotContain("simpleAiList = true", rewritten)
        // Bytes around the edit should be preserved — check a few landmarks.
        Assert.Contains("-- Addon Custom Data", rewritten)
        Assert.Contains("analyticsSent = true,", rewritten)
        Assert.Contains("simplifiedSkirmishSetup = true,", rewritten)
        // Length changes by exactly 1 byte (true→false: +1 char). Not
        // a hard requirement of the contract but a strong sanity check
        // that we didn't accidentally reflow.
        Assert.Equal(original.Length + 1, rewritten.Length)

[<Fact>]
let ``rewriteSimpleAiList returns None when already false`` () =
    let original = loadFixture ()
    match ProxyInstaller.rewriteSimpleAiList original with
    | Some once ->
        match ProxyInstaller.rewriteSimpleAiList once with
        | None -> () // idempotent
        | Some _ -> Assert.Fail("second rewrite should be a no-op")
    | None -> Assert.Fail("initial rewrite should fire")

[<Fact>]
let ``rewriteSimpleAiList returns None when key absent`` () =
    let absent = "return {\n\t[\"Menu\"] = {\n\t\tsomeOtherKey = 42,\n\t},\n}\n"
    Assert.Equal<string option>(None, ProxyInstaller.rewriteSimpleAiList absent)

[<Fact>]
let ``rewriteSimpleAiList tolerates tabs and trailing-comma variations`` () =
    let variants =
        [ "\t\tsimpleAiList = true,\n"
          "\t\tsimpleAiList = true\n"            // no trailing comma
          "    simpleAiList = true  ,  \n"      // spaces + whitespace around comma
          "simpleAiList=true,\n" ]              // no surrounding whitespace
    for v in variants do
        match ProxyInstaller.rewriteSimpleAiList v with
        | Some rewritten ->
            Assert.Contains("false", rewritten)
            Assert.DoesNotContain("true", rewritten)
        | None -> Assert.Fail(sprintf "regex should match variant %A" v)

[<Fact>]
let ``rewriteSimpleAiList does not affect unrelated keys containing 'simpleAi'`` () =
    let input = "\tsimpleAiListHidden = true,\n\tsimpleAiList = true,\n"
    match ProxyInstaller.rewriteSimpleAiList input with
    | Some rewritten ->
        Assert.Contains("simpleAiListHidden = true,", rewritten)
        Assert.Contains("simpleAiList = false,", rewritten)
    | None -> Assert.Fail("should still match the simpleAiList line")
