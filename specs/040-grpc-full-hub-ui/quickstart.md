# Quickstart: gRPC parity for Hub UI and rendered viewer

**Feature**: 040-grpc-full-hub-ui
**Date**: 2026-04-18

End-to-end walkthrough: launch a headless Hub-driven session from a scripting client, receive rendered viewer frames, and mutate Viz state without touching the GUI.

## Prerequisites

- Linux host (or dev container) with `DISPLAY` available. The Hub still needs a windowing environment for its own Viewer surface (Assumption in spec; out-of-scope for 040).
- `dotnet` on PATH. F# 9 / .NET 10 SDK.
- Local NuGet store populated (`./scripts/check-deps.sh`).
- BAR install discoverable at `~/.local/state/Beyond All Reason/` (the Hub auto-detects).

## 1. Start the Hub

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet run --project src/FSBar.Hub.App
```

The Hub listens for scripting-service calls on `127.0.0.1:5021` (see `HubSettings.GrpcPort`).

For a headless-user-input smoke test (Hub still gets a DISPLAY, but no human clicks):

```bash
FSBAR_HUB_AUTO_LAUNCH=1 XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet run --project src/FSBar.Hub.App
```

## 2. From a scripting client — US1: configure + launch

New example script: `scripts/examples/17-hub-lobby-launch.fsx`.

```fsharp
#load "scripts/prelude.fsx"
open FSBar.Hub.Scripting

let client = Scripting.connect "127.0.0.1" 5021

// 1. Discover available maps
let maps = client.ListMaps() |> _.Maps
printfn "Maps: %d" maps.Count

// 2. Build a lobby
let lobby =
    Lobby.defaults
    |> Lobby.withMap (maps |> Seq.head |> _.Name)
    |> Lobby.withMode LobbyMode.Skirmish
    |> Lobby.withEngineSpeed 1.0f
    |> Lobby.withAi 0 "Armada" "HighBarV2"
    |> Lobby.withAi 1 "Cortex" "BARb"

// 3. Push lobby through gRPC — local GUI's Setup tab mirrors it live.
let cfg = client.ConfigureLobby(ConfigureLobbyRequest(Lobby = lobby))
if cfg.Result.Outcome <> SubmitOutcome.Sent then
    failwithf "configure failed: %s" cfg.Result.Reason

// 4. Launch paused + headless.
let launch = client.LaunchSession(LaunchSessionRequest(StartPaused = true, LaunchGraphicalViewer = false))
printfn "Session %s launching" launch.SessionId.Value
```

## 3. US2: subscribe to the render-frame stream

New example: `scripts/examples/18-hub-render-frames.fsx`.

```fsharp
let req =
    StreamRenderFramesRequest(
        ClientLabel = "demo",
        TargetHz = 10,
        Format = ImageFormat.Png,
        ViewportWidth = 1024,
        ViewportHeight = 768)

let stream = client.StreamRenderFrames(req)

use _ = stream.ResponseStream.ReadToEndAsync(fun frame ->
    // frame.ImageBytes is a complete PNG — save, display, or diff.
    File.WriteAllBytes($"frame-{frame.ClientSequence:D6}.png", frame.ImageBytes.ToByteArray())
)
```

On a fresh session with no frames yet, the stream stays silent. Once the
engine starts emitting frames, PNGs flow at ~10 Hz. `frame.RenderedAtUnixMs`
and `frame.EncodedAtUnixMs` let the caller verify SC-008's P95 ≤ 200 ms
budget.

Single-shot variant (for CI screenshot assertions):

```fsharp
let shot = client.GetRenderFrame(GetRenderFrameRequest(Format = ImageFormat.Png))
File.WriteAllBytes("snapshot.png", shot.Frame.ImageBytes.ToByteArray())
```

## 4. US3: drive viz + camera live

New example: `scripts/examples/19-hub-vizconfig-drive.fsx`.

```fsharp
// Toggle the weapon-range overlay (W hotkey equivalent).
client.ToggleOverlay(
    ToggleOverlayRequest(
        Overlay = OverlayKey.WeaponRanges,
        Target = OverlayTargetState.On))
|> ignore

// Push a single attribute (reuses ConfigDescriptors registry keys).
client.SetVizAttribute(
    SetVizAttributeRequest(
        Key = "UnitMarkerSize",
        Value = VizAttributeValue(FloatValue = 1.4)))
|> ignore

// Pan + zoom.
client.SetCamera(
    SetCameraRequest(
        Camera = ViewerCameraWire(
            Scale = 1.5f, OriginX = 0.0f, OriginY = 0.0f, AutoFit = false)))
|> ignore
```

## 5. US4: preset round-trip + encyclopedia

```fsharp
// Save current VizConfig as preset "demo".
let state = client.GetHubState(GetHubStateRequest())
client.SavePreset(SavePresetRequest(Name = "demo", VizConfig = state.VizConfig)) |> ignore

// List presets and pick one back.
let presets = client.ListPresets(ListPresetsRequest()).Presets
let chosen = presets |> Seq.find (fun p -> p.Name = "demo")
client.LoadPreset(LoadPresetRequest(Name = chosen.Name)) |> ignore

// Filter encyclopedia by faction and select an entry.
let units = client.ListUnits(ListUnitsRequest(FactionFilter = [| "Armada" |]))
client.SelectUnit(SelectUnitRequest(DefId = units.Entries.[0].DefId)) |> ignore
```

## 6. US5: observe state changes from two clients

New example: `scripts/examples/20-hub-state-observer.fsx`.

```fsharp
// Subscribe to state events (future-only deltas).
let events = client.StreamHubStateEvents(StreamHubStateEventsRequest(ClientLabel = "observer"))

async {
    for! e in events.ResponseStream do
        match e.ChangeCase with
        | HubStateEvent.ChangeOneofCase.VizAttribute ->
            printfn "[%s] viz attr %s changed" e.Source e.VizAttribute.Key
        | HubStateEvent.ChangeOneofCase.ActiveTab ->
            printfn "[%s] tab -> %O" e.Source e.ActiveTab
        | HubStateEvent.ChangeOneofCase.SessionStatus ->
            printfn "[%s] session -> %O" e.Source e.SessionStatus.State
        | _ -> ()
} |> Async.Start

// To rehydrate after a disconnect, call GetHubState first, then resubscribe.
let snapshot = client.GetHubState(GetHubStateRequest())
printfn "Starting state: active tab = %O, lobby map = %s"
    snapshot.ActiveTab snapshot.Lobby.MapName
```

## 7. US6: upload overlay primitives to decorate the Hub Viewer

New example: `scripts/examples/21-hub-overlay-layers.fsx`.

```fsharp
// Build a layer containing two primitives:
// - a world-anchored red circle at map (200, 200)
// - a screen-anchored label in the top-left of the viewport
//
// Note: world coordinates are in BAR engine elmo units (same units
// as MapGrid.width / MapGrid.height and unit positions). Screen
// coordinates are viewport pixels with origin at the top-left.
let red = uint32 0xFF_00_00_FFu
let yellow = uint32 0xFF_FF_00_FFu
let white = uint32 0xFF_FF_FF_FFu

let style color width =
    OverlayStyle(
        StrokeColorRgba = color,
        StrokeWidth = width,
        HasFill = false,
        Opacity = 1.0f)

let worldCircle =
    OverlayPrimitive(
        Space = CoordinateSpace.World,
        Style = style red 2.0f,
        Circle = CirclePrimitive(
            Center = OverlayPoint(X = 200.0f, Y = 200.0f),
            Radius = 15.0f))

let screenLabel =
    OverlayPrimitive(
        Space = CoordinateSpace.Screen,
        Style = style white 1.0f,
        Text = TextPrimitive(
            Anchor = OverlayPoint(X = 20.0f, Y = 20.0f),
            Text = "demo overlay",
            FontSize = 14.0f,
            Align = TextAlign.Left))

let put =
    client.PutLayer(
        PutLayerRequest(
            Layer = OverlayLayerWire(
                Name = "demo",
                ZHint = 0,
                Primitives = [ worldCircle; screenLabel ])))

if put.Result.Outcome <> SubmitOutcome.Sent then
    failwithf "put-layer rejected: %s %s" put.Result.Reason put.ExceededCap

// Frames streamed via US2 now contain the circle (moving with the camera)
// and the label (fixed in the viewport corner).

// Replace atomically with a single polygon:
client.PutLayer(
    PutLayerRequest(
        Layer = OverlayLayerWire(
            Name = "demo",
            ZHint = 0,
            Primitives = [
                OverlayPrimitive(
                    Space = CoordinateSpace.World,
                    Style = style yellow 3.0f,
                    Polygon = PolygonPrimitive(
                        Points = [
                            OverlayPoint(X = 100.0f, Y = 100.0f)
                            OverlayPoint(X = 300.0f, Y = 100.0f)
                            OverlayPoint(X = 200.0f, Y = 280.0f)
                        ]))
            ]))) |> ignore

// Drop the layer:
client.DeleteLayer(DeleteLayerRequest(Name = "demo")) |> ignore

// When the client disconnects, any remaining layers auto-clean.
```

## 8. Tear down

```fsharp
client.StopSession(StopSessionRequest()) |> ignore
```

The Hub window remains open; the session terminates and the Setup tab is
editable again (`SessionManager.IsLobbyEditable = true`).

## Verifying SC-007 (existing clients keep working)

`scripts/examples/16-hub-admin.fsx` is a feature-039 example. Run it against
the updated Hub unchanged:

```bash
dotnet fsi scripts/examples/16-hub-admin.fsx
```

Exit code 0 + expected `Pause` / `Resume` / `SetEngineSpeed` / `ForceEnd`
outputs = SC-007 satisfied.

## Reference

- Proto additions: `specs/040-grpc-full-hub-ui/contracts/scripting.proto`
- F# signature sketches: `specs/040-grpc-full-hub-ui/contracts/fsi/*.fsi`
- Data model details: `specs/040-grpc-full-hub-ui/data-model.md`
- Phase 0 decisions: `specs/040-grpc-full-hub-ui/research.md`
