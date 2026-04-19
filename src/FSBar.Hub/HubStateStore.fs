namespace FSBar.Hub

open System.Threading
open FSBar.Viz

type HubState =
    { ActiveTab: HubTab
      VizConfig: VizConfig
      Camera: ViewerCamera
      Lobby: LobbyConfig.LobbyConfig
      Encyclopedia: EncyclopediaSelection
      PresetList: string list
      Settings: HubSettings.HubSettings }

module HubStateStore =

    [<Sealed>]
    type T(events: HubEvents.IHubEventSink, initial: HubState) =
        let mutable state : HubState = initial

        member _.Events = events

        member _.Current() : HubState = Volatile.Read(&state)

        /// Atomic CAS update helper. `transform` is pure; returns either
        /// `Ok newState` to commit or `Error reason` to abort. Retries up
        /// to 3 times on CAS contention before returning
        /// `Rejected "write contention"`.
        member _.TryUpdate(transform: HubState -> Result<HubState, string>) : Result<HubState * HubState, string> =
            let mutable attempts = 0
            let mutable result : Result<HubState * HubState, string> = Error "unreached"
            let mutable keepGoing = true
            while keepGoing do
                let before = Volatile.Read(&state)
                match transform before with
                | Error reason ->
                    result <- Error reason
                    keepGoing <- false
                | Ok after ->
                    let swapped = Interlocked.CompareExchange(&state, after, before)
                    if obj.ReferenceEquals(swapped, before) then
                        result <- Ok (before, after)
                        keepGoing <- false
                    else
                        attempts <- attempts + 1
                        if attempts >= 3 then
                            result <- Error "write contention"
                            keepGoing <- false
            result

    let create (events: HubEvents.IHubEventSink) (initial: HubState) : T =
        T(events, initial)

    let current (store: T) : HubState = store.Current()

    let private tryApply
            (store: T)
            (transform: HubState -> Result<HubState, string>)
            : Result<HubState * HubState, string> =
        store.TryUpdate transform

    // Feature 041 R7 / FR-023a — every Rejected outcome from a public
    // mutator emits exactly one DiagnosticsLine Warning before returning,
    // so callers can silently roll back with `HubStateStore.current` and
    // operators still see the rejection in the diagnostics stream.
    let private emitRejected (store: T) (mutator: string) (reason: string) : unit =
        let msg = sprintf "HubStateStore.%s rejected: %s" mutator reason
        try store.Events.Publish(HubEvents.DiagnosticsLine(HubEvents.Warning, msg))
        with _ -> ()

    let private rejectWith (store: T) (mutator: string) (reason: string) : SubmitOutcome =
        emitRejected store mutator reason
        Rejected reason

    let private commit
            (store: T)
            (mutator: string)
            (transform: HubState -> Result<HubState, string>)
            (onSuccess: HubState -> HubState -> unit)
            : SubmitOutcome =
        match tryApply store transform with
        | Ok (before, after) ->
            onSuccess before after
            Sent
        | Error reason ->
            emitRejected store mutator reason
            Rejected reason

    let setActiveTab (store: T) (tab: HubTab) : SubmitOutcome =
        commit store "setActiveTab"
            (fun s -> Ok { s with ActiveTab = tab })
            (fun _ after -> store.Events.Publish(HubEvents.ActiveTabChanged after.ActiveTab))

    let setCamera (store: T) (camera: ViewerCamera) : SubmitOutcome =
        match ViewerCamera.validate camera with
        | Error reason -> rejectWith store "setCamera" reason
        | Ok validated ->
            commit store "setCamera"
                (fun s -> Ok { s with Camera = validated })
                (fun _ after -> store.Events.Publish(HubEvents.CameraChanged after.Camera))

    let setLobby (store: T) (lobby: LobbyConfig.LobbyConfig) : SubmitOutcome =
        commit store "setLobby"
            (fun s -> Ok { s with Lobby = lobby })
            (fun _ after -> store.Events.Publish(HubEvents.LobbyChanged after.Lobby))

    let setVizConfig (store: T) (config: VizConfig) : SubmitOutcome =
        commit store "setVizConfig"
            (fun s -> Ok { s with VizConfig = config })
            (fun _ after -> store.Events.Publish(HubEvents.VizConfigChanged after.VizConfig))

    // Box/unbox helpers mapping AttributeValue ↔ the loose `obj` used by
    // ConfigDescriptors. Phase 2 just needs this to compile; US3 task
    // T049 refines the coercion rules (type-mismatch handling etc).
    let private attributeValueToObj (value: AttributeValue) : obj =
        match value with
        | BoolValue b -> box b
        | IntValue i -> box i
        | FloatValue f -> box (float32 f)
        | StringValue s -> box s
        | ColorRgbaValue c ->
            // ConfigDescriptors.Set uses unbox<SKColor>; must convert uint32 RRGGBBAA → SKColor.
            box (SkiaSharp.SKColor(byte (c >>> 24), byte (c >>> 16), byte (c >>> 8), byte c))
        | StringListValue xs -> box (xs |> List.toArray)

    let private objToAttributeValue (value: obj) : AttributeValue =
        match value with
        | :? bool as b -> BoolValue b
        | :? int as i -> IntValue i
        | :? int64 as i -> IntValue (int i)
        | :? float32 as f -> FloatValue (float f)
        | :? float as f -> FloatValue f
        | :? string as s -> StringValue s
        | :? uint32 as c -> ColorRgbaValue c
        | :? (string array) as xs -> StringListValue (List.ofArray xs)
        | _ ->
            // Fall back to the runtime type name so diagnostic output at
            // least names what came back. US3 will tighten this.
            StringValue (value.GetType().FullName)

    let setVizAttribute
            (store: T) (key: string) (value: AttributeValue) : SubmitOutcome =
        match ConfigDescriptors.tryFind key with
        | None -> rejectWith store "setVizAttribute" (sprintf "unknown key: %s" key)
        | Some desc ->
            let rawValue = attributeValueToObj value
            commit store "setVizAttribute"
                (fun s ->
                    let updated = desc.Set rawValue s.VizConfig
                    Ok { s with VizConfig = updated })
                (fun before after ->
                    let oldAttr = objToAttributeValue (desc.Get before.VizConfig)
                    let newAttr = objToAttributeValue (desc.Get after.VizConfig)
                    store.Events.Publish(
                        HubEvents.VizAttributeChanged(key, oldAttr, newAttr)))

    // The feature-040 skeleton uses a descriptor-key mapping for overlay
    // toggles; US3 (T050) refines the mapping and guarantees every key
    // resolves to a real `VizConfig` boolean. Until then, unknown keys
    // reject cleanly so the rest of the store stays usable.
    let private overlayKeyToDescriptorKey (key: OverlayKind) : string =
        match key with
        | OverlayKind.Units -> "overlays.units"
        | OverlayKind.Events -> "overlays.events"
        | OverlayKind.Grid -> "overlays.grid"
        | OverlayKind.MetalSpots -> "overlays.metalSpots"
        | OverlayKind.EconomyHud -> "overlays.economyHud"
        | OverlayKind.WeaponRanges -> "overlays.weaponRanges"
        | OverlayKind.SightRanges -> "overlays.sightRanges"
        | OverlayKind.CommandQueue -> "overlays.commandQueue"
        | OverlayKind.FullNames -> "overlays.fullNames"

    let toggleOverlay
            (store: T) (key: OverlayKind) (target: ToggleTarget)
            : SubmitOutcome * bool =
        let descriptorKey = overlayKeyToDescriptorKey key
        match ConfigDescriptors.tryFind descriptorKey with
        | None ->
            rejectWith store "toggleOverlay" (sprintf "unknown overlay key: %A" key), false
        | Some desc ->
            let before = store.Current()
            let currentRaw = desc.Get before.VizConfig
            let currentBool =
                match currentRaw with
                | :? bool as b -> b
                | _ -> false
            let desired =
                match target with
                | Toggle -> not currentBool
                | On -> true
                | Off -> false
            let outcome = setVizAttribute store descriptorKey (BoolValue desired)
            outcome, desired

    let setEncyclopedia
            (store: T) (selection: EncyclopediaSelection) : SubmitOutcome =
        // Feature 044 FR-008a — sanitize SearchText at the store
        // boundary so the predicate module can assume it arrives
        // trimmed and length-capped.
        let trimmed =
            if isNull selection.SearchText then ""
            else selection.SearchText.Trim()
        if trimmed.Length > 128 then
            rejectWith store "setEncyclopedia" "search text > 128 chars"
        else
            let sanitized = { selection with SearchText = trimmed }
            commit store "setEncyclopedia"
                (fun s -> Ok { s with Encyclopedia = sanitized })
                (fun _ after ->
                    store.Events.Publish(
                        HubEvents.EncyclopediaSelectionChanged after.Encyclopedia))

    let updatePresetList (store: T) (names: string list) : unit =
        // Facade-only; no event. Three-retry CAS to converge under
        // contention with concurrent save/delete mutations that also
        // invalidate this list.
        let mutable attempts = 0
        let mutable keepGoing = true
        while keepGoing do
            match store.TryUpdate(fun s -> Ok { s with PresetList = names }) with
            | Ok _ -> keepGoing <- false
            | Error _ when attempts < 3 -> attempts <- attempts + 1
            | Error _ -> keepGoing <- false

    let setSettings
            (store: T) (settings: HubSettings.HubSettings) : SubmitOutcome =
        commit store "setSettings"
            (fun s -> Ok { s with Settings = settings })
            (fun _ after -> store.Events.Publish(HubEvents.HubSettingsChanged after.Settings))
