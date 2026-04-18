module FSBar.Hub.Tests.EncyclopediaFilterTests

open System.Collections.Generic
open Xunit
open FSBar.Hub
open FSBar.Viz

let private mk
        (name: string)
        (faction: FactionId)
        (tier: Tier)
        (shape: MovementShape)
        : EncyclopediaData.EncyclopediaEntry =
    { DefId = abs (name.GetHashCode())
      InternalName = name
      HumanName = None
      Subfolder = ""
      Faction = faction
      Tier = tier
      Shape = shape
      MetalCost = 0
      EnergyCost = 0
      BuildTime = 0
      Health = 0
      FootprintX = 2
      FootprintZ = 2
      SightRangeElmo = 300.0f
      WeaponRangesElmo = []
      MovementClass = None }

let private sample () =
    [ mk "armcom" FactionId.Armada Tier.T1 MovementShape.Bot      // commander + ground
      mk "armpw"  FactionId.Armada Tier.T1 MovementShape.Bot
      mk "armfark" FactionId.Armada Tier.T2 MovementShape.Bot
      mk "corcom" FactionId.Cortex Tier.T1 MovementShape.Bot      // commander
      mk "corraid" FactionId.Cortex Tier.T1 MovementShape.Vehicle
      mk "corvamp" FactionId.Cortex Tier.T2 MovementShape.Air
      mk "armbomb" FactionId.Armada Tier.T2 MovementShape.Air
      mk "legllt" FactionId.Legion Tier.T1 MovementShape.Building ]

// --- T010 cases --------------------------------------------------------

[<Fact>]
let ``empty selection passes every entry`` () =
    let entries = sample ()
    let got = EncyclopediaFilter.apply EncyclopediaSelection.defaults entries
    Assert.Equal<IEnumerable<_>>(entries, got)

[<Fact>]
let ``single faction chip filters to that faction`` () =
    let sel =
        { EncyclopediaSelection.defaults with
            FactionFilter = Set.ofList [ FactionFilterKey.Armada ] }
    let got = EncyclopediaFilter.apply sel (sample ())
    Assert.All(got, fun e -> Assert.Equal(FactionId.Armada, e.Faction))
    Assert.Equal(4, List.length got)

[<Fact>]
let ``two faction chips OR within the faction category`` () =
    let sel =
        { EncyclopediaSelection.defaults with
            FactionFilter = Set.ofList [ FactionFilterKey.Armada; FactionFilterKey.Cortex ] }
    let got = EncyclopediaFilter.apply sel (sample ())
    Assert.Equal(7, List.length got)

[<Fact>]
let ``faction and tier AND across categories`` () =
    let sel =
        { EncyclopediaSelection.defaults with
            FactionFilter = Set.ofList [ FactionFilterKey.Armada ]
            TierFilter = Set.ofList [ TierFilterKey.T2 ] }
    let got = EncyclopediaFilter.apply sel (sample ())
    Assert.All(got, fun e ->
        Assert.Equal(FactionId.Armada, e.Faction)
        Assert.Equal(Tier.T2, e.Tier))
    Assert.Equal(2, List.length got)  // armfark, armbomb

[<Fact>]
let ``empty category semantics pass all in that category`` () =
    let sel =
        { EncyclopediaSelection.defaults with
            MobilityFilter = Set.empty
            TierFilter = Set.ofList [ TierFilterKey.T2 ] }
    let got = EncyclopediaFilter.apply sel (sample ())
    Assert.All(got, fun e -> Assert.Equal(Tier.T2, e.Tier))

[<Fact>]
let ``commander chip matches commanders only`` () =
    let sel =
        { EncyclopediaSelection.defaults with
            TierFilter = Set.ofList [ TierFilterKey.Commander ] }
    let got = EncyclopediaFilter.apply sel (sample ())
    Assert.Equal<IEnumerable<_>>(
        [ "armcom"; "corcom" ],
        got |> List.map (fun e -> e.InternalName))

// --- T017 cases (US2 search) ------------------------------------------

[<Fact>]
let ``air plus bomb search intersects both`` () =
    let sel =
        { EncyclopediaSelection.defaults with
            MobilityFilter = Set.ofList [ MobilityFilterKey.Air ]
            SearchText = "bomb" }
    let got = EncyclopediaFilter.apply sel (sample ())
    Assert.Equal<IEnumerable<_>>(
        [ "armbomb" ],
        got |> List.map (fun e -> e.InternalName))

[<Fact>]
let ``search alone works without tag filters`` () =
    let sel = { EncyclopediaSelection.defaults with SearchText = "com" }
    let got = EncyclopediaFilter.apply sel (sample ())
    Assert.Contains(got, fun e -> e.InternalName = "armcom")
    Assert.Contains(got, fun e -> e.InternalName = "corcom")

[<Fact>]
let ``search is case-insensitive`` () =
    let a =
        EncyclopediaFilter.apply
            { EncyclopediaSelection.defaults with SearchText = "ARM" }
            (sample ())
    let b =
        EncyclopediaFilter.apply
            { EncyclopediaSelection.defaults with SearchText = "arm" }
            (sample ())
    Assert.Equal<IEnumerable<_>>(
        a |> List.map (fun e -> e.InternalName),
        b |> List.map (fun e -> e.InternalName))

// --- T020: store-level trim + 128-char cap -----------------------------

open FSBar.Viz

let private seedState () : HubState =
    { ActiveTab = HubTab.Setup
      VizConfig = VizDefaults.defaultConfig
      Camera = ViewerCamera.defaults
      Lobby = LobbyConfig.defaults
      Encyclopedia = EncyclopediaSelection.defaults
      PresetList = []
      Settings = HubSettings.defaults }

type private RecordingSink() =
    let events = System.Collections.Generic.List<HubEvents.HubEvent>()
    interface HubEvents.IHubEventSink with
        member _.Publish(ev) = events.Add ev
    member _.Events = events |> List.ofSeq

[<Fact>]
let ``setEncyclopedia trims whitespace from SearchText`` () =
    let sink = RecordingSink()
    let store = HubStateStore.create (sink :> HubEvents.IHubEventSink) (seedState ())
    let outcome =
        HubStateStore.setEncyclopedia store
            { EncyclopediaSelection.defaults with SearchText = "   bomb   " }
    Assert.Equal(Sent, outcome)
    let s = HubStateStore.current store
    Assert.Equal("bomb", s.Encyclopedia.SearchText)

[<Fact>]
let ``setEncyclopedia rejects SearchText longer than 128 chars`` () =
    let sink = RecordingSink()
    let store = HubStateStore.create (sink :> HubEvents.IHubEventSink) (seedState ())
    let longText = System.String('a', 129)
    let outcome =
        HubStateStore.setEncyclopedia store
            { EncyclopediaSelection.defaults with SearchText = longText }
    match outcome with
    | Rejected r -> Assert.Contains("128", r)
    | _ -> Assert.Fail("expected Rejected")
    // No EncyclopediaSelectionChanged event should have fired.
    let selectionEvents =
        sink.Events
        |> List.choose (function
            | HubEvents.EncyclopediaSelectionChanged _ -> Some ()
            | _ -> None)
    Assert.Empty(selectionEvents)

// --- T021: tab-switch round-trip persistence --------------------------

[<Fact>]
let ``setActiveTab round-trip preserves encyclopedia selection`` () =
    let sink = RecordingSink()
    let store = HubStateStore.create (sink :> HubEvents.IHubEventSink) (seedState ())
    let target =
        { EncyclopediaSelection.defaults with
            FactionFilter = Set.ofList [ FactionFilterKey.Armada ]
            SearchText = "bomb" }
    HubStateStore.setEncyclopedia store target |> ignore
    HubStateStore.setActiveTab store HubTab.Viewer |> ignore
    HubStateStore.setActiveTab store HubTab.Units |> ignore
    let got = (HubStateStore.current store).Encyclopedia
    Assert.Equal<IEnumerable<FactionFilterKey>>(target.FactionFilter, got.FactionFilter)
    Assert.Equal("bomb", got.SearchText)

[<Fact>]
let ``fresh store returns defaultSelection`` () =
    let sink = RecordingSink()
    let store = HubStateStore.create (sink :> HubEvents.IHubEventSink) (seedState ())
    Assert.Equal(
        EncyclopediaFilter.defaultSelection,
        (HubStateStore.current store).Encyclopedia)
