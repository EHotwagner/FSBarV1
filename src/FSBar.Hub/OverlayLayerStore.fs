namespace FSBar.Hub

open System
open System.Collections.Generic
open System.Threading

type OverlayPoint = { X: float32; Y: float32 }

type CoordinateSpace =
    | World
    | Screen

type TextAlign =
    | Left
    | Center
    | Right

type PathVerb =
    | MoveTo of OverlayPoint
    | LineTo of OverlayPoint
    | CubicTo of c1: OverlayPoint * c2: OverlayPoint * p: OverlayPoint
    | Close

type OverlayStyle =
    { StrokeColorRgba: uint32
      StrokeWidth: float32
      FillColorRgba: uint32 option
      Opacity: float32
      Dash: float32 array option }

type OverlayPrimitive =
    | Line of ``from``: OverlayPoint * ``to``: OverlayPoint * style: OverlayStyle * space: CoordinateSpace
    | Polyline of points: OverlayPoint list * style: OverlayStyle * space: CoordinateSpace
    | Polygon of points: OverlayPoint list * style: OverlayStyle * space: CoordinateSpace
    | Rectangle of
        x: float32 *
        y: float32 *
        w: float32 *
        h: float32 *
        cornerRadius: float32 *
        style: OverlayStyle *
        space: CoordinateSpace
    | Circle of
        center: OverlayPoint *
        radius: float32 *
        style: OverlayStyle *
        space: CoordinateSpace
    | Path of
        verbs: PathVerb list *
        style: OverlayStyle *
        space: CoordinateSpace
    | Text of
        anchor: OverlayPoint *
        text: string *
        fontSize: float32 *
        fontFamily: string *
        align: TextAlign *
        style: OverlayStyle *
        space: CoordinateSpace
    | Image of
        anchor: OverlayPoint *
        width: int *
        height: int *
        bytes: byte array *
        space: CoordinateSpace

type OverlayLayer =
    { Name: string
      ZHint: int
      UploadedAtUnixMs: int64
      Primitives: OverlayPrimitive list }

type OverlayLayerDescriptor =
    { Name: string
      ZHint: int
      UploadedAtUnixMs: int64
      PrimitiveCount: int }

type CapKind =
    | LayersPerClient
    | PrimitivesPerLayer
    | BytesPerPush
    | ImageBytes
    | ImageDimensions

type PutLayerError =
    | InvalidName of reason: string
    | ValidationFailed of errors: string list
    | CapExceeded of cap: CapKind

type OverlayLayerSnapshot =
    { Entries: (Guid * OverlayLayer) array }

module OverlayLayerStore =

    // --- Caps (FR-026) ------------------------------------------------
    let private maxLayersPerClient = 16
    let private maxPrimitivesPerLayer = 500
    let private maxBytesPerPush = 1_048_576          // 1 MiB
    let private maxImageBytes = 262_144              // 256 KiB
    let private maxImageDimension = 2048
    let private maxTextBytes = 4096
    let private maxNameCodepoints = 64

    // --- Validation helpers ------------------------------------------

    let private isFinite (f: float32) =
        not (System.Single.IsNaN(f)) && not (System.Single.IsInfinity(f))

    let private validateName (name: string) : Result<unit, PutLayerError> =
        if System.String.IsNullOrEmpty(name) then
            Error (InvalidName "name is empty")
        elif name.Length > maxNameCodepoints then
            Error (InvalidName (sprintf "name exceeds %d characters" maxNameCodepoints))
        elif name.Contains('/') || name.Contains('\\') then
            Error (InvalidName "name contains path separator")
        elif name |> Seq.exists System.Char.IsControl then
            Error (InvalidName "name contains control character")
        else
            Ok ()

    let private validateStyle (style: OverlayStyle) : string list =
        let errs = ResizeArray<string>()
        if not (isFinite style.StrokeWidth) || style.StrokeWidth <= 0.0f || style.StrokeWidth > 1000.0f then
            errs.Add "strokeWidth outside (0, 1000]"
        if not (isFinite style.Opacity) || style.Opacity < 0.0f || style.Opacity > 1.0f then
            errs.Add "opacity outside [0, 1]"
        List.ofSeq errs

    let private validatePoint (p: OverlayPoint) (label: string) : string list =
        if not (isFinite p.X) || not (isFinite p.Y) then
            [ sprintf "%s has non-finite coordinates" label ]
        else []

    let private validatePrimitive (p: OverlayPrimitive) : string list * int =
        // Returns (errors, approximate-bytes).
        match p with
        | Line (f, t, style, _) ->
            let errs =
                List.concat [
                    validatePoint f "line.from"
                    validatePoint t "line.to"
                    validateStyle style ]
            errs, 32
        | Polyline (pts, style, _) ->
            let errs =
                if List.length pts < 2 then
                    [ "polyline needs >= 2 points" ]
                else
                    pts
                    |> List.mapi (fun i p -> validatePoint p (sprintf "polyline[%d]" i))
                    |> List.concat
            errs @ validateStyle style, 16 + 8 * List.length pts
        | Polygon (pts, style, _) ->
            let errs =
                if List.length pts < 3 then
                    [ "polygon needs >= 3 points" ]
                else
                    pts
                    |> List.mapi (fun i p -> validatePoint p (sprintf "polygon[%d]" i))
                    |> List.concat
            errs @ validateStyle style, 16 + 8 * List.length pts
        | Rectangle (x, y, w, h, r, style, _) ->
            let errs = ResizeArray<string>()
            if not (isFinite x) || not (isFinite y) then errs.Add "rectangle has non-finite position"
            if not (isFinite w) || w < 0.0f then errs.Add "rectangle.w < 0"
            if not (isFinite h) || h < 0.0f then errs.Add "rectangle.h < 0"
            if not (isFinite r) || r < 0.0f then errs.Add "rectangle.cornerRadius < 0"
            (List.ofSeq errs) @ validateStyle style, 32
        | Circle (c, r, style, _) ->
            let errs = ResizeArray<string>()
            errs.AddRange(validatePoint c "circle.center")
            if not (isFinite r) || r < 0.0f then errs.Add "circle.radius < 0"
            (List.ofSeq errs) @ validateStyle style, 24
        | Path (verbs, style, _) ->
            let errs = ResizeArray<string>()
            match verbs with
            | MoveTo _ :: _ -> ()
            | _ -> errs.Add "path first verb must be MoveTo"
            (List.ofSeq errs) @ validateStyle style, 16 + 16 * List.length verbs
        | Text (anchor, text, fontSize, _, _, style, _) ->
            let errs = ResizeArray<string>()
            errs.AddRange(validatePoint anchor "text.anchor")
            if System.Text.Encoding.UTF8.GetByteCount(text) > maxTextBytes then
                errs.Add (sprintf "text exceeds %d bytes" maxTextBytes)
            if not (isFinite fontSize) || fontSize <= 0.0f then
                errs.Add "text.fontSize <= 0"
            (List.ofSeq errs) @ validateStyle style,
            32 + System.Text.Encoding.UTF8.GetByteCount(text)
        | Image (anchor, w, h, bytes, _) ->
            let errs = ResizeArray<string>()
            errs.AddRange(validatePoint anchor "image.anchor")
            if w <= 0 || w > maxImageDimension || h <= 0 || h > maxImageDimension then
                errs.Add (sprintf "image dimensions outside (0, %d]" maxImageDimension)
            if bytes.Length > maxImageBytes then
                errs.Add (sprintf "image bytes exceeds %d" maxImageBytes)
            // PNG magic: 89 50 4E 47 ; JPEG magic: FF D8 FF
            let ok =
                bytes.Length >= 4
                && ((bytes.[0] = 0x89uy && bytes.[1] = 0x50uy &&
                     bytes.[2] = 0x4Euy && bytes.[3] = 0x47uy)
                    || (bytes.[0] = 0xFFuy && bytes.[1] = 0xD8uy && bytes.[2] = 0xFFuy))
            if not ok then errs.Add "image bytes: unrecognised magic (PNG/JPEG required)"
            List.ofSeq errs, 24 + bytes.Length

    [<Sealed>]
    type T(events: HubEvents.IHubEventSink) =
        let lock = new ReaderWriterLockSlim()
        let owners = Dictionary<Guid, Dictionary<string, OverlayLayer>>()
        let mutable disconnectSubscription : IDisposable option = None

        member _.Events = events

        member _.PutLayer (clientId: Guid) (layer: OverlayLayer) : Result<unit, PutLayerError> =
            // 1. Name validation (cheap, no lock).
            match validateName layer.Name with
            | Error e -> Error e
            | Ok () ->
                // 2. Primitive count cap.
                if List.length layer.Primitives > maxPrimitivesPerLayer then
                    Error (CapExceeded PrimitivesPerLayer)
                else
                    // 3. Validate each primitive + compute approximate
                    // serialized size.
                    let allErrs = ResizeArray<string>()
                    let mutable totalBytes = 0
                    let mutable capHit : CapKind option = None
                    for p in layer.Primitives do
                        let errs, bytes = validatePrimitive p
                        allErrs.AddRange(errs)
                        totalBytes <- totalBytes + bytes
                        // Image-specific caps.
                        match p with
                        | Image (_, _, _, b, _) when b.Length > maxImageBytes ->
                            capHit <- Some ImageBytes
                        | Image (_, w, h, _, _)
                            when w > maxImageDimension || h > maxImageDimension
                              || w <= 0 || h <= 0 ->
                            capHit <- Some ImageDimensions
                        | _ -> ()
                    if capHit.IsSome then
                        Error (CapExceeded capHit.Value)
                    elif totalBytes > maxBytesPerPush then
                        Error (CapExceeded BytesPerPush)
                    elif allErrs.Count > 0 then
                        Error (ValidationFailed (List.ofSeq allErrs))
                    else
                        // 4. Commit under write lock.
                        lock.EnterWriteLock()
                        try
                            let clientLayers =
                                match owners.TryGetValue(clientId) with
                                | true, existing -> existing
                                | false, _ ->
                                    let fresh = Dictionary<string, OverlayLayer>()
                                    owners.[clientId] <- fresh
                                    fresh
                            // Layer cap — only count layers-per-client on
                            // the CREATE path; replacing an existing
                            // named layer does not count.
                            if not (clientLayers.ContainsKey(layer.Name))
                               && clientLayers.Count >= maxLayersPerClient then
                                Error (CapExceeded LayersPerClient)
                            else
                                clientLayers.[layer.Name] <- layer
                                Ok ()
                        finally
                            lock.ExitWriteLock()

        member _.DeleteLayer (clientId: Guid) (layerName: string) : SubmitOutcome =
            lock.EnterWriteLock()
            try
                match owners.TryGetValue(clientId) with
                | true, existing ->
                    existing.Remove(layerName) |> ignore
                    Sent
                | false, _ ->
                    Sent
            finally
                lock.ExitWriteLock()

        member _.ListLayers (clientId: Guid) : OverlayLayerDescriptor list =
            lock.EnterReadLock()
            try
                match owners.TryGetValue(clientId) with
                | true, existing ->
                    existing.Values
                    |> Seq.map (fun layer ->
                        { Name = layer.Name
                          ZHint = layer.ZHint
                          UploadedAtUnixMs = layer.UploadedAtUnixMs
                          PrimitiveCount = List.length layer.Primitives })
                    |> List.ofSeq
                | false, _ -> []
            finally
                lock.ExitReadLock()

        member _.ClearLayers (clientId: Guid) : int =
            lock.EnterWriteLock()
            try
                match owners.TryGetValue(clientId) with
                | true, existing ->
                    let count = existing.Count
                    existing.Clear()
                    count
                | false, _ -> 0
            finally
                lock.ExitWriteLock()

        member _.RemoveClient (clientId: Guid) : unit =
            lock.EnterWriteLock()
            try
                owners.Remove(clientId) |> ignore
            finally
                lock.ExitWriteLock()

        member _.Snapshot () : OverlayLayerSnapshot =
            lock.EnterReadLock()
            try
                let pairs =
                    owners
                    |> Seq.collect (fun kv ->
                        kv.Value.Values |> Seq.map (fun layer -> kv.Key, layer))
                    |> Seq.sortBy (fun (ownerId, layer) ->
                        ownerId, layer.ZHint, layer.UploadedAtUnixMs)
                    |> Array.ofSeq
                { Entries = pairs }
            finally
                lock.ExitReadLock()

        member this.WireDisconnectCleanup (source: IObservable<HubEvents.HubEvent>) : unit =
            match disconnectSubscription with
            | Some _ -> ()
            | None ->
                let observer =
                    { new IObserver<HubEvents.HubEvent> with
                        member _.OnNext(evt) =
                            match evt with
                            | HubEvents.ScriptingClientDetached(clientId, _) ->
                                this.RemoveClient(clientId)
                            | _ -> ()
                        member _.OnError(_) = ()
                        member _.OnCompleted() = () }
                let sub = source.Subscribe(observer)
                disconnectSubscription <- Some sub

    let create (events: HubEvents.IHubEventSink) : T = T(events)

    let wireDisconnectCleanup (store: T) (source: IObservable<HubEvents.HubEvent>) : unit =
        store.WireDisconnectCleanup(source)

    let putLayer (store: T) (clientId: Guid) (layer: OverlayLayer) : Result<unit, PutLayerError> =
        store.PutLayer clientId layer

    let deleteLayer (store: T) (clientId: Guid) (layerName: string) : SubmitOutcome =
        store.DeleteLayer clientId layerName

    let listLayers (store: T) (clientId: Guid) : OverlayLayerDescriptor list =
        store.ListLayers clientId

    let clearLayers (store: T) (clientId: Guid) : int =
        store.ClearLayers clientId

    let removeClient (store: T) (clientId: Guid) : unit =
        store.RemoveClient clientId

    let snapshot (store: T) : OverlayLayerSnapshot = store.Snapshot()
