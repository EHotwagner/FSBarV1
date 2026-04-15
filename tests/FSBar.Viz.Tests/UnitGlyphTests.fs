module FSBar.Viz.Tests.UnitGlyphTests

open Xunit
open FSBar.Viz

// --- classifyShape (T014) ---------------------------------------------------

let private noLog (_: string) = ()

let private capture () =
    let misses = ResizeArray<string>()
    let log (m: string) = misses.Add m
    misses, log

[<Fact>]
let ``classifyShape: !canMove => Building`` () =
    let shape = UnitGlyph.classifyShape false false None noLog
    Assert.Equal(MovementShape.Building, shape)

[<Fact>]
let ``classifyShape: canFly => Air`` () =
    let shape = UnitGlyph.classifyShape true true (Some "AIRPLANE") noLog
    Assert.Equal(MovementShape.Air, shape)

[<Fact>]
let ``classifyShape: KBOT prefix => Bot`` () =
    let shape = UnitGlyph.classifyShape true false (Some "KBOT2") noLog
    Assert.Equal(MovementShape.Bot, shape)

[<Fact>]
let ``classifyShape: ARMBOT prefix => Bot`` () =
    Assert.Equal(MovementShape.Bot, UnitGlyph.classifyShape true false (Some "ARMBOT") noLog)

[<Fact>]
let ``classifyShape: BOT prefix => Bot`` () =
    Assert.Equal(MovementShape.Bot, UnitGlyph.classifyShape true false (Some "BOT3") noLog)

[<Fact>]
let ``classifyShape: TANK prefix => Vehicle`` () =
    Assert.Equal(MovementShape.Vehicle, UnitGlyph.classifyShape true false (Some "TANK3") noLog)

[<Fact>]
let ``classifyShape: VEHICLE prefix => Vehicle`` () =
    Assert.Equal(MovementShape.Vehicle, UnitGlyph.classifyShape true false (Some "VEHICLE4") noLog)

[<Fact>]
let ``classifyShape: ATV prefix => Vehicle`` () =
    Assert.Equal(MovementShape.Vehicle, UnitGlyph.classifyShape true false (Some "ATV") noLog)

[<Fact>]
let ``classifyShape: HOVER prefix => Hover`` () =
    Assert.Equal(MovementShape.Hover, UnitGlyph.classifyShape true false (Some "HOVER3") noLog)

[<Fact>]
let ``classifyShape: BOAT prefix => Ship`` () =
    Assert.Equal(MovementShape.Ship, UnitGlyph.classifyShape true false (Some "BOAT4") noLog)

[<Fact>]
let ``classifyShape: UBOAT prefix => Ship`` () =
    Assert.Equal(MovementShape.Ship, UnitGlyph.classifyShape true false (Some "UBOAT") noLog)

[<Fact>]
let ``classifyShape: SHIP prefix => Ship`` () =
    Assert.Equal(MovementShape.Ship, UnitGlyph.classifyShape true false (Some "SHIP") noLog)

[<Fact>]
let ``classifyShape: unknown class => Unknown + logMiss once`` () =
    UnitGlyph.resetSession()
    let misses, log = capture()
    let shape = UnitGlyph.classifyShape true false (Some "WAGGLER42") log
    Assert.Equal(MovementShape.Unknown, shape)
    Assert.Equal(1, misses.Count)

[<Fact>]
let ``classifyShape: unknown class logged only on first occurrence`` () =
    UnitGlyph.resetSession()
    let misses, log = capture()
    let _ = UnitGlyph.classifyShape true false (Some "BLORBO") log
    let _ = UnitGlyph.classifyShape true false (Some "BLORBO") log
    let _ = UnitGlyph.classifyShape true false (Some "BLORBO") log
    Assert.Equal(1, misses.Count)

// --- classifyTier (T015) ----------------------------------------------------

[<Fact>]
let ``classifyTier: customParams techlevel=2 => T2`` () =
    let cp = Map.ofList [ "techlevel", "2" ]
    Assert.Equal(Tier.T2, UnitGlyph.classifyTier cp None noLog)

[<Fact>]
let ``classifyTier: customParams techlevel=3 => T3`` () =
    let cp = Map.ofList [ "techlevel", "3" ]
    Assert.Equal(Tier.T3, UnitGlyph.classifyTier cp None noLog)

[<Fact>]
let ``classifyTier: customParams techlevel=1 => T1`` () =
    let cp = Map.ofList [ "techlevel", "1" ]
    Assert.Equal(Tier.T1, UnitGlyph.classifyTier cp None noLog)

[<Fact>]
let ``classifyTier: no customParams, category LEVEL3 => T3`` () =
    Assert.Equal(Tier.T3, UnitGlyph.classifyTier Map.empty (Some "WEAPON LEVEL3 MOBILE") noLog)

[<Fact>]
let ``classifyTier: no customParams, category LEVEL2 => T2`` () =
    Assert.Equal(Tier.T2, UnitGlyph.classifyTier Map.empty (Some "LEVEL2 TANK") noLog)

[<Fact>]
let ``classifyTier: no customParams, no category => T1 + logMiss once`` () =
    UnitGlyph.resetSession()
    let misses, log = capture()
    let tier = UnitGlyph.classifyTier Map.empty None log
    Assert.Equal(Tier.T1, tier)
    Assert.Equal(1, misses.Count)

// --- classifyFaction (T016) -------------------------------------------------

[<Fact>]
let ``classifyFaction: subfolder armada/tanks => Armada`` () =
    Assert.Equal(FactionId.Armada, UnitGlyph.classifyFaction "Units/armada/tanks" "armtank" noLog)

[<Fact>]
let ``classifyFaction: subfolder cortex/bots => Cortex`` () =
    Assert.Equal(FactionId.Cortex, UnitGlyph.classifyFaction "Units/cortex/bots" "corbot" noLog)

[<Fact>]
let ``classifyFaction: subfolder legion/buildings => Legion`` () =
    Assert.Equal(FactionId.Legion, UnitGlyph.classifyFaction "Units/legion/buildings" "legbase" noLog)

[<Fact>]
let ``classifyFaction: subfolder raptors/alpha => Raptors`` () =
    Assert.Equal(FactionId.Raptors, UnitGlyph.classifyFaction "Units/raptors/alpha" "raptora" noLog)

[<Fact>]
let ``classifyFaction: subfolder scavengers => Scavengers`` () =
    Assert.Equal(FactionId.Scavengers, UnitGlyph.classifyFaction "Units/scavengers/tanks" "scavtank" noLog)

[<Fact>]
let ``classifyFaction: empty subfolder, name prefix corcom => Cortex`` () =
    Assert.Equal(FactionId.Cortex, UnitGlyph.classifyFaction "" "corcom" noLog)

[<Fact>]
let ``classifyFaction: unknown both => Neutral + logMiss once`` () =
    UnitGlyph.resetSession()
    let misses, log = capture()
    let f = UnitGlyph.classifyFaction "" "zorb" log
    Assert.Equal(FactionId.Neutral, f)
    Assert.Equal(1, misses.Count)
