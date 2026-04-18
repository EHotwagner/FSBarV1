namespace FSBar.Hub

open System
open System.Text
open System.Threading
open System.Threading.Channels

module HubLog =

    type LogSeverity =
        | Debug
        | Info
        | Warning
        | Error

    type LogCategory =
        | SessionManager
        | AdminChannel
        | ScriptingHub
        | ProxyInstall
        | HeadlessRenderer
        | HubStateStore
        | PresetPersistence
        | Lobby
        | Settings

    type CorrelationId = CorrelationId.CorrelationId

    type LogFilter = {
        Categories: Set<LogCategory> option
        MinSeverity: LogSeverity
        PresetName: string option
    }

    let defaultFilter: LogFilter = {
        Categories = None
        MinSeverity = Info
        PresetName = None
    }

    // Severity ordering: Debug=0 < Info=1 < Warning=2 < Error=3.
    let private severityOrder (s: LogSeverity) =
        match s with
        | Debug -> 0
        | Info -> 1
        | Warning -> 2
        | Error -> 3

    let private allCategories =
        Set.ofList [
            SessionManager
            AdminChannel
            ScriptingHub
            ProxyInstall
            HeadlessRenderer
            HubStateStore
            PresetPersistence
            Lobby
            Settings
        ]

    // Hub-shipped presets (R5). Keys canonical-lowercase.
    let availablePresets: Map<string, LogFilter> =
        Map.ofList [
            "session-lifecycle",
                { Categories = Some (Set.ofList [ SessionManager; AdminChannel; ProxyInstall ])
                  MinSeverity = Info
                  PresetName = Some "session-lifecycle" }
            "admin-channel",
                { Categories = Some (Set.singleton AdminChannel)
                  MinSeverity = Debug
                  PresetName = Some "admin-channel" }
            "scripting-wire",
                { Categories = Some (Set.singleton ScriptingHub)
                  MinSeverity = Debug
                  PresetName = Some "scripting-wire" }
        ]

    let resolveFilter
            (categories: LogCategory list)
            (minSeverity: LogSeverity option)
            (presetName: string option)
            : Result<LogFilter, string> =
        // Preset lookup is case-insensitive on the human-supplied name.
        // Note: LogSeverity.Error shadows Result.Error at module scope, so
        // we use the fully-qualified Result.Error constructor explicitly.
        let presetLookup : Result<(string * LogFilter) option, string> =
            match presetName with
            | None -> Ok None
            | Some raw when String.IsNullOrEmpty(raw) -> Ok None
            | Some raw ->
                let key = raw.ToLowerInvariant()
                match Map.tryFind key availablePresets with
                | Some f -> Ok (Some (key, f))
                | None -> Result.Error (sprintf "unknown preset '%s'" raw)
        match presetLookup with
        | Result.Error e -> Result.Error e
        | Ok presetMatch ->
            // Explicit categories always win. When no explicit categories
            // AND a preset supplied → use preset's categories. When neither
            // → `None` meaning "all".
            let explicitSet =
                match categories with
                | [] -> None
                | xs -> Some (Set.ofList xs)
            let resolvedCategories =
                match explicitSet, presetMatch with
                | Some xs, _ -> Some xs
                | None, Some (_, preset) -> preset.Categories
                | None, None -> None
            let resolvedSeverity =
                match minSeverity, presetMatch with
                | Some s, _ -> s
                | None, Some (_, preset) -> preset.MinSeverity
                | None, None -> defaultFilter.MinSeverity
            let resolvedPresetName =
                match presetMatch with
                | Some (key, _) -> Some key
                | None -> None
            Ok
                { Categories = resolvedCategories
                  MinSeverity = resolvedSeverity
                  PresetName = resolvedPresetName }

    type LogEntry = {
        TimestampUnixMs: int64
        Severity: LogSeverity
        Category: LogCategory
        Message: string
        CorrelationId: CorrelationId option
        SessionId: Guid option
        ScriptingClientId: Guid option
    }

    // Marker uses the canonical byte sequence documented in R6.
    // `…` is U+2026 (three bytes in UTF-8).
    let private markerPrefixBytes = Encoding.UTF8.GetBytes(" …[truncated ")
    let private markerSuffixBytes = Encoding.UTF8.GetBytes(" bytes]")

    // Byte-safe UTF-8 truncation at 8 KiB.
    let private maxMessageBytes = 8192

    let truncateUtf8 (message: string) : string =
        if isNull message then ""
        else
            let byteCount = Encoding.UTF8.GetByteCount(message)
            if byteCount <= maxMessageBytes then message
            else
                let droppedBytes = byteCount - 0 // placeholder; computed after cut
                // Compute marker byte-count including the decimal N.
                // We need to budget for the *actual* dropped-byte count. We
                // iteratively shrink the prefix until prefix + marker fits.
                let messageBytes = Encoding.UTF8.GetBytes(message)
                // Begin with a conservative cut ceiling.
                let mutable cutBytes = maxMessageBytes - markerPrefixBytes.Length - markerSuffixBytes.Length - 20
                if cutBytes < 0 then cutBytes <- 0
                // Walk back to a UTF-8 lead-byte boundary. Continuation bytes
                // have the high bits `10xxxxxx`.
                let isContinuation (b: byte) = (b &&& 0xC0uy) = 0x80uy
                while cutBytes > 0 && isContinuation messageBytes.[cutBytes] do
                    cutBytes <- cutBytes - 1
                // Iterate to ensure final byte count ≤ maxMessageBytes even
                // as the decimal N digit count varies.
                let mutable finalCut = cutBytes
                let mutable done_ = false
                while not done_ do
                    let dropped = byteCount - finalCut
                    let droppedDigits = (string dropped).Length
                    let totalBytes =
                        finalCut
                        + markerPrefixBytes.Length
                        + droppedDigits
                        + markerSuffixBytes.Length
                    if totalBytes <= maxMessageBytes then
                        done_ <- true
                    else
                        finalCut <- finalCut - 1
                        while finalCut > 0 && isContinuation messageBytes.[finalCut] do
                            finalCut <- finalCut - 1
                        if finalCut <= 0 then
                            finalCut <- 0
                            done_ <- true
                let prefix = Encoding.UTF8.GetString(messageBytes, 0, finalCut)
                let dropped = byteCount - finalCut
                sprintf "%s …[truncated %d bytes]" prefix dropped

    type Subscription = {
        Id: Guid
        Reader: ChannelReader<LogEntry>
        Dispose: unit -> unit
    }

    type AttachOutcome =
        | Attached of Subscription
        | Rejected of reason: string

    // Internal per-subscriber registration. Kept out of the .fsi so the
    // Channel + cancellation infrastructure doesn't leak to consumers.
    type private Subscriber = {
        Id: Guid
        ClientLabel: string
        Filter: LogFilter ref
        Channel: Channel<LogEntry>
        Cancellation: CancellationTokenSource
        CancellationRegistration: CancellationTokenRegistration option ref
        mutable DroppedSinceLast: int
        mutable NextSequence: uint64
        AttachedAtUnixMs: int64
    }

    let private subscriberCapacity = 256

    [<Sealed>]
    type T
        internal
        (events: HubEvents.IHubEventSink,
         getSettings: unit -> HubSettings.HubSettings) =

        let attachLock = obj()
        let mutable subscribers: Subscriber[] = Array.empty
        let mutable subscriberCount_ = 0
        let mutable disposed = 0
        let mutable globalSequence = 0L
        let busCts = new CancellationTokenSource()

        let isDisposed () = Volatile.Read(&disposed) <> 0

        let detachInternal (id: Guid) =
            lock attachLock (fun () ->
                let existing = subscribers
                let filtered = existing |> Array.filter (fun s -> s.Id <> id)
                if filtered.Length <> existing.Length then
                    subscribers <- filtered
                    Volatile.Write(&subscriberCount_, filtered.Length)
                    let sub = existing |> Array.find (fun s -> s.Id = id)
                    try sub.Channel.Writer.TryComplete() |> ignore with _ -> ()
                    try sub.Cancellation.Cancel() with _ -> ()
                    match !(sub.CancellationRegistration) with
                    | Some reg ->
                        try reg.Dispose() with _ -> ()
                        sub.CancellationRegistration := None
                    | None -> ())

        let unixMillis () =
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()

        member internal this.SubscriberCountValue =
            Volatile.Read(&subscriberCount_)

        member internal _.TryAttach(clientLabel: string, filter: LogFilter, ct: CancellationToken) : AttachOutcome =
            if isDisposed () then
                Rejected "HubLog has been disposed"
            else
                lock attachLock (fun () ->
                    if isDisposed () then
                        Rejected "HubLog has been disposed"
                    else
                        let cap =
                            try (getSettings ()).MaxLogStreamSubscribers
                            with _ -> 8
                        let current = subscribers
                        if current.Length >= cap then
                            Rejected
                                (sprintf "max log-stream subscribers (%d) reached" cap)
                        else
                            let chOpts =
                                BoundedChannelOptions(subscriberCapacity,
                                    FullMode = BoundedChannelFullMode.DropOldest,
                                    SingleReader = true,
                                    SingleWriter = true)
                            assert (subscriberCapacity = 256)
                            let channel = Channel.CreateBounded<LogEntry>(chOpts)
                            let cts = CancellationTokenSource.CreateLinkedTokenSource(busCts.Token, ct)
                            let id = Guid.NewGuid()
                            let sub : Subscriber = {
                                Id = id
                                ClientLabel = clientLabel
                                Filter = ref filter
                                Channel = channel
                                Cancellation = cts
                                CancellationRegistration = ref None
                                DroppedSinceLast = 0
                                NextSequence = 0UL
                                AttachedAtUnixMs = unixMillis ()
                            }
                            subscribers <- Array.append current [| sub |]
                            Volatile.Write(&subscriberCount_, subscribers.Length)
                            let reg = cts.Token.Register(Action(fun () -> detachInternal id))
                            sub.CancellationRegistration := Some reg
                            let disposeAction () = detachInternal id
                            Attached
                                { Id = id
                                  Reader = channel.Reader
                                  Dispose = disposeAction })

        member internal _.UpdateFilter(id: Guid, newFilter: LogFilter) : Result<unit, string> =
            let snapshot = Volatile.Read(&subscribers)
            match snapshot |> Array.tryFind (fun s -> s.Id = id) with
            | Some sub ->
                Interlocked.Exchange(sub.Filter, newFilter) |> ignore
                Ok ()
            | None -> Result.Error (sprintf "subscriber %O has been detached" id)

        member internal _.AcceptCount(category: LogCategory, severity: LogSeverity) =
            let snapshot = Volatile.Read(&subscribers)
            let mutable count = 0
            for sub in snapshot do
                let f = !sub.Filter
                let categoryOk =
                    match f.Categories with
                    | None -> true
                    | Some set -> Set.contains category set
                if categoryOk
                   && severityOrder severity >= severityOrder f.MinSeverity then
                    count <- count + 1
            count

        member internal _.Deliver(entry: LogEntry) =
            let snapshot = Volatile.Read(&subscribers)
            for sub in snapshot do
                let f = !sub.Filter
                let categoryOk =
                    match f.Categories with
                    | None -> true
                    | Some set -> Set.contains entry.Category set
                if categoryOk
                   && severityOrder entry.Severity >= severityOrder f.MinSeverity then
                    // Under BoundedChannelFullMode.DropOldest the writer's
                    // TryWrite still returns true when the channel was at
                    // capacity — it silently evicts the oldest item. We
                    // observe "at capacity before enqueue" via the reader
                    // count, same pattern as ScriptingHub's frame fan-out.
                    if sub.Channel.Reader.Count >= subscriberCapacity then
                        Interlocked.Increment(&sub.DroppedSinceLast) |> ignore
                    sub.Channel.Writer.TryWrite(entry) |> ignore

        member internal _.NextSequenceFor(id: Guid) : uint64 =
            let snapshot = Volatile.Read(&subscribers)
            match snapshot |> Array.tryFind (fun s -> s.Id = id) with
            | Some sub ->
                let next = Interlocked.Increment(&sub.NextSequence)
                uint64 next
            | None -> 0UL

        member internal _.ExchangeDroppedSinceLast(id: Guid) : int =
            let snapshot = Volatile.Read(&subscribers)
            match snapshot |> Array.tryFind (fun s -> s.Id = id) with
            | Some sub ->
                Interlocked.Exchange(&sub.DroppedSinceLast, 0)
            | None -> 0

        member this.SubscriberCount = this.SubscriberCountValue

        member _.Dispose() =
            if Interlocked.Exchange(&disposed, 1) = 0 then
                try busCts.Cancel() with _ -> ()
                lock attachLock (fun () ->
                    let snapshot = subscribers
                    subscribers <- Array.empty
                    Volatile.Write(&subscriberCount_, 0)
                    for sub in snapshot do
                        try sub.Channel.Writer.TryComplete() |> ignore with _ -> ()
                        try sub.Cancellation.Cancel() with _ -> ()
                        match !(sub.CancellationRegistration) with
                        | Some reg ->
                            try reg.Dispose() with _ -> ()
                        | None -> ())
                try busCts.Dispose() with _ -> ()

        interface IDisposable with
            member this.Dispose() = this.Dispose()

    let create
            (events: HubEvents.IHubEventSink)
            (settings: unit -> HubSettings.HubSettings)
            : T =
        new T(events, settings)

    let attach
            (bus: T)
            (clientLabel: string)
            (filter: LogFilter)
            (cancellationToken: CancellationToken)
            : AttachOutcome =
        bus.TryAttach(clientLabel, filter, cancellationToken)

    let updateFilter (bus: T) (subscriberId: Guid) (newFilter: LogFilter) =
        bus.UpdateFilter(subscriberId, newFilter)

    let emit
            (bus: T)
            (category: LogCategory)
            (severity: LogSeverity)
            (sessionId: Guid option)
            (scriptingClientId: Guid option)
            (buildMessage: unit -> string)
            : unit =
        // R1 zero-overhead gate: volatile read + early return.
        if bus.SubscriberCountValue = 0 then () else
        if bus.AcceptCount(category, severity) = 0 then () else
        // Build the entry only after confirming at least one subscriber
        // accepts the (category, severity) pair.
        let raw = buildMessage ()
        let truncated = truncateUtf8 raw
        let corrOpt = CorrelationId.current ()
        let entry: LogEntry = {
            TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            Severity = severity
            Category = category
            Message = truncated
            CorrelationId = corrOpt
            SessionId = sessionId
            ScriptingClientId = scriptingClientId
        }
        bus.Deliver(entry)

    let emitSimple
            (bus: T)
            (category: LogCategory)
            (severity: LogSeverity)
            (buildMessage: unit -> string)
            : unit =
        emit bus category severity None None buildMessage

    let emitFromDiagnosticsLine
            (bus: T)
            (category: LogCategory)
            (severity: HubEvents.Severity)
            (sessionId: Guid option)
            (scriptingClientId: Guid option)
            (message: string)
            : unit =
        let mapped =
            match severity with
            | HubEvents.Info -> Info
            | HubEvents.Warning -> Warning
            | HubEvents.Error -> Error
        emit bus category mapped sessionId scriptingClientId (fun () -> message)

    let subscriberCount (bus: T) = bus.SubscriberCount

    let nextSequenceFor (bus: T) (subscriberId: Guid) : uint64 =
        bus.NextSequenceFor(subscriberId)

    let exchangeDroppedSinceLast (bus: T) (subscriberId: Guid) : int =
        bus.ExchangeDroppedSinceLast(subscriberId)
