module FSBar.Client.Tests.ScriptGeneratorAutohostTests

open Xunit
open FSBar.Client

// Feature 039 T006 — assert that `ScriptGenerator.generateSpringSettings`
// emits the autohost interface lines iff `EngineConfig.AutohostPort` is set.

[<Fact>]
let ``autohost_lines_emitted_when_port_set`` () =
    let config = { EngineConfig.defaultConfig () with AutohostPort = Some 12345 }
    let text = ScriptGenerator.generateSpringSettings config
    let lines = text.Split('\n') |> Array.filter (fun l -> l.Length > 0)
    let ipMatches =
        lines |> Array.filter (fun l -> l = "AutohostIP=127.0.0.1")
    let portMatches =
        lines |> Array.filter (fun l -> l = "AutohostPort=12345")
    Assert.Equal(1, ipMatches.Length)
    Assert.Equal(1, portMatches.Length)

[<Fact>]
let ``autohost_lines_absent_when_port_none`` () =
    let config = { EngineConfig.defaultConfig () with AutohostPort = None }
    let text = ScriptGenerator.generateSpringSettings config
    Assert.DoesNotContain("AutohostIP", text)
    Assert.DoesNotContain("AutohostPort", text)

[<Fact>]
let ``springsettings_always_forces_windowed_mode`` () =
    let withPort = ScriptGenerator.generateSpringSettings
                       { EngineConfig.defaultConfig () with AutohostPort = Some 42 }
    let withoutPort = ScriptGenerator.generateSpringSettings
                          { EngineConfig.defaultConfig () with AutohostPort = None }
    for text in [ withPort; withoutPort ] do
        Assert.Contains("Fullscreen=0", text)
        Assert.Contains("XResolutionWindowed=1980", text)
        Assert.Contains("YResolutionWindowed=1024", text)
        Assert.Contains("WindowBorderless=0", text)
