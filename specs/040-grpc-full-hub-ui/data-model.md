# Phase 1 Data Model: gRPC parity for Hub UI

**Feature**: 040-grpc-full-hub-ui
**Date**: 2026-04-18

Fields and relationships for the in-memory entities the feature introduces or widens. Persistence-layer formats (settings.json, preset JSON files) are unchanged from pre-feature and not re-specified here.

---

## HubStateStore (new, in-memory)

Central UI-state holder in `FSBar.Hub.HubStateStore`. Wraps an atomic reference to a `HubState` record; every public mutator returns a `SubmitOutcome` and emits `HubEvent`s via `HubEventBus`.

```
HubState:
  ActiveTab:      HubTab                  // Setup | Viewer | Units | Style | Cfg | Grpc
  VizConfig:      FSBar.Viz.VizConfig
  Camera:         ViewerCamera
  Lobby:          FSBar.Hub.LobbyConfig   // authoritative only when SessionManager.State = Idle
  Encyclopedia:   EncyclopediaSelection
  PresetList:     string list             // cache; invalidated on save/delete
  Settings:       FSBar.Hub.HubSettings   // mirror of the persisted snapshot

ViewerCamera:
  Scale:          float32
  OriginX:        float32
  OriginY:        float32
  AutoFit:        bool

EncyclopediaSelection:
  FactionFilter:  Set<FactionFilterKey>   // Armada | Cortex | Legion | Raptors | Scavengers | Neutral
  SelectedDefId:  int option              // None = no selection
```

**Validation rules** (enforced by mutators):

- `Camera.Scale` must be finite and in `[0.05, 100.0]`.
- `Camera.OriginX` / `Camera.OriginY` must be finite.
- `Lobby` mutation rejected (`FAILED_PRECONDITION`) when `SessionManager.State` is not `Idle`; otherwise validated via `LobbyConfig.validate`.
- `VizConfig` mutation validated field-by-field via `FSBar.Viz.ConfigDescriptors` (range / enum membership / color format).
- `PresetList` is read-only externally — populated by `PresetFacade` and invalidated on save/delete.

**State transitions**: pure overwrite on every successful mutation; `HubEvent` emitted with old + new value of the mutated field (or a coarse `VizConfigChanged` for the whole config when the client submits a full config replacement).

**Concurrency**: single-cell atomic update via `Interlocked.CompareExchange` on the `HubState` record reference. Under contention the loser re-reads and retries (bounded retry — three attempts, then `Rejected "write contention"`). Each success emits exactly one `HubEvent` sequence per mutation, per FR-016.

---

## HeadlessRenderer (new)

Encapsulates one off-screen render pipeline per subscribed scripting client.

```
RenderSubscription:
  Id:              Guid
  ClientLabel:     string
  TargetHz:        int                    // 1..30 (caller requested; capped server-side)
  Format:          ImageFormat            // PNG | JPEG
  ViewportWidth:   int                    // default 1024
  ViewportHeight:  int                    // default 768
  JpegQuality:     int                    // 10..100, default 80, ignored for PNG
  Channel:         BoundedChannel<RenderFrameMessage>   // capacity 16, DropOldest
  CumulativeDrops: int                    // monitored; detach at 32 per R5
  Worker:          Task                   // encode loop

RenderFrameMessage:
  ImageBytes:         byte[]
  Format:             ImageFormat
  RenderedAtUnixMs:   int64               // server render complete
  EncodedAtUnixMs:    int64               // encode complete, before enqueue
  ClientSequence:     uint64
  ViewportWidth:      int
  ViewportHeight:     int
  Quality:            int                 // 0 for PNG
```

**Lifecycle**: created on first `StreamRenderFrames` call, per client; destroyed when the client disconnects, detaches via overflow, or explicitly closes. Hub-wide cap of 8 concurrent subscriptions (configurable via `HubSettings.MaxRenderFrameSubscribers`, default 8).

**Session-absence behaviour**: when `SessionManager.State <> Running`, the worker emits nothing (stream stays open). Optional single "no-session placeholder" frame on subscribe can be toggled via request flag (default: silent).

---

## HubEvent (extended DU, in `FSBar.Hub.HubEvents`)

New cases added to the existing `HubEvent` DU. All cases are additive; consumers that pattern-match without a catch-all will get a warning and should be updated — documented in release notes.

```
HubEvent (new cases):
  ActiveTabChanged of HubTab
  VizConfigChanged of VizConfig                    // coarse; for full-config replacements
  VizAttributeChanged of key:string * oldValue:AttributeValue * newValue:AttributeValue
  CameraChanged of ViewerCamera
  LobbyChanged of LobbyConfig
  EncyclopediaSelectionChanged of EncyclopediaSelection
  PresetsChanged of PresetChange                   // Saved name | Deleted name | Loaded name
  HubSettingsChanged of HubSettings
```

The existing cases (`StateChanged`, `EngineSpeedChanged`, `SessionPaused`, `DiagnosticsLine`, `ScriptingClientConnected`, `ScriptingClientDetached`, `ProxyInstallProgress`, `AdminChannelStatusChanged`) remain unchanged.

---

## PresetFacade (new)

Hub-side thin wrapper over `FSBar.Viz.StylePreset`. Responsibilities: name validation (no traversal, no reserved chars, 1..64 UTF-8 code points), event emission on save/delete/load, invalidation of `HubStateStore.PresetList` cache.

```
PresetOperation:
  ListNames:  unit -> Result<string list, PresetError>
  Save:       name:string * VizConfig -> Result<unit, PresetError>
  Load:       name:string -> Result<VizConfig, PresetError>
  Delete:     name:string -> Result<unit, PresetError>

PresetError:
  InvalidName of reason:string
  NotFound of name:string
  IoError of message:string
```

---

## EncyclopediaFacade (new)

Hub-side thin wrapper over `FSBar.Viz.EncyclopediaData`. Caches entries at Hub startup; exposes filter and select operations.

```
EncyclopediaQuery:
  ListEntries:    factionFilter:Set<FactionFilterKey> -> EncyclopediaEntry list
  GetByDefId:     id:int -> EncyclopediaEntry option
  GetByName:      internalName:string -> EncyclopediaEntry option
  Select:         selection:EncyclopediaSelection -> SubmitOutcome      // delegates to HubStateStore
```

No new fields beyond the existing `FSBar.Viz.EncyclopediaData.EncyclopediaEntry`.

---

## HubSettings (existing, widened accessor surface)

The persisted fields are unchanged. New per-field helpers in `HubSettings.fsi`:

```
HubSettings (fields, unchanged):
  BarDataDirOverride:              string option
  EngineVersionOverride:            string option
  GrpcPort:                         int
  LaunchGraphicalViewerDefault:     bool
  StartPausedDefault:               bool
  MaxRenderFrameSubscribers:        int            // NEW — default 8, range [1, 32]
  SchemaVersion:                    int            // bumped to 2 when MaxRenderFrameSubscribers added

HubSettings module (new helpers):
  updateStartPausedDefault:          bool -> unit            // LWW + emit event + persist
  updateLaunchGraphicalViewerDefault:bool -> unit
  updateMaxRenderFrameSubscribers:   int -> Result<unit, string>
```

**Schema bump**: `MaxRenderFrameSubscribers` is a new persisted field; SchemaVersion 1 → 2. The loader defaults missing field to 8 when reading a v1 file, then saves as v2 on the next `save`.

**Why a setting and not a constant**: operators on constrained boxes may want a lower cap; SC-008 measurements could surface the need to tune.

---

## LobbyConfig (existing, no shape change)

Already defined in `FSBar.Hub.LobbyConfig`. The feature only adds:

- `HubStateStore.setLobby: LobbyConfig -> SubmitOutcome` mutator
- `HubStateStore.currentLobby: unit -> LobbyConfig` reader

Wire projection in `contracts/scripting.proto` uses a lossless `LobbyConfigWire` message mirroring every field.

---

## SessionManager (existing, widened)

New members on the existing class:

```
SessionManager (new members):
  Stop: unit -> SubmitOutcome                    // abort current session
  IsLobbyEditable: unit -> bool                  // true when State = Idle
```

Existing members (`State`, `Launch`, `TogglePause`, `Pause`, `Resume`, `SetEngineSpeed`, `ForceEnd`, `SendAdminMessage`, `IsPaused`, `AdminStatus`, `Frames`) unchanged.

---

## Wire → F# mapping summary

| Wire message | F# record / DU | Module |
|-------------|----------------|--------|
| `HubStateSnapshot` | projection of `HubState` | `HubStateStore` |
| `HubStateEvent` | projection of `HubEvent` | `HubEvents` |
| `ViewerCameraWire` | `ViewerCamera` | `HubStateStore` |
| `VizConfigWire` (map<string, VizAttributeValue>) | via `ConfigDescriptors.applyValues` / `extractValues` | `FSBar.Viz.ConfigDescriptors` |
| `VizAttributeValue` (oneof) | `ConfigDescriptors.AttributeValue` | `FSBar.Viz.ConfigDescriptors` |
| `LobbyConfigWire` | `LobbyConfig` | `FSBar.Hub.LobbyConfig` |
| `EncyclopediaSelectionWire` | `EncyclopediaSelection` | `HubStateStore` |
| `EncyclopediaEntryWire` | `EncyclopediaEntry` | `FSBar.Viz.EncyclopediaData` |
| `RenderFrameMessage` | `RenderFrameMessage` | `HubStateStore.HeadlessRenderer` |
| `HubSettingsWire` | `HubSettings` | `FSBar.Hub.HubSettings` |
| `PresetDescriptor` | `{ name: string; modifiedAt: DateTime }` | `PresetFacade` |

---

## OverlayLayerStore (new, in-memory, per-client scoped)

Per-client named overlay state. Lives in `FSBar.Hub.OverlayLayerStore`, distinct from `HubStateStore` because overlays are not part of the Hub's shared UI state (clients cannot see each other's layers, per FR-021).

```
OverlayLayerStore:
  Layers: Dictionary<clientId: Guid, Dictionary<layerName: string, OverlayLayer>>
  Lock:   ReaderWriterLockSlim

OverlayLayer:
  Name:         string              // 1..64 UTF-8 code points, no path separators
  ZHint:        int                 // render order within a client's layer set
  UploadedAt:   int64               // unix ms; tie-break for equal zHint
  Primitives:   OverlayPrimitive list  // ≤ 500

OverlayPrimitive =
  | Line       of from:Point * to:Point * style:OverlayStyle * space:CoordinateSpace
  | Polyline   of points:Point list * style:OverlayStyle * space:CoordinateSpace
  | Polygon    of points:Point list * style:OverlayStyle * space:CoordinateSpace
  | Rectangle  of x:float * y:float * w:float * h:float *
                  cornerRadius:float * style:OverlayStyle * space:CoordinateSpace
  | Circle     of center:Point * radius:float * style:OverlayStyle * space:CoordinateSpace
  | Path       of verbs:PathVerb list * style:OverlayStyle * space:CoordinateSpace
  | Text       of anchor:Point * text:string * fontSize:float * fontFamily:string *
                  align:TextAlign * style:OverlayStyle * space:CoordinateSpace
  | Image      of anchor:Point * width:int * height:int *
                  bytes:byte[] * space:CoordinateSpace

OverlayStyle:
  StrokeColorRgba:  uint32
  StrokeWidth:      float32       // > 0, ≤ 1000
  FillColorRgba:    uint32 option // None = no fill
  Opacity:          float32       // [0, 1]
  Dash:             float32[] option // dash pattern (elmo/pixel lengths); None = solid

CoordinateSpace = World | Screen

PathVerb =
  | MoveTo of Point
  | LineTo of Point
  | CubicTo of c1:Point * c2:Point * p:Point
  | Close

Point: { X: float32; Y: float32 }
TextAlign = Left | Center | Right
```

**Validation rules** (enforced on `putLayer` / `putLayers`):

- Layer name: 1..64 UTF-8 code points, no path separators (`/`, `\`), no control chars.
- `ZHint`: any 32-bit int; not validated beyond type.
- Per-layer primitive count ≤ 500 (FR-026).
- Per-client layer count ≤ 16 (FR-026).
- Total serialized `PutLayer` request size ≤ 1 MB (FR-026).
- Coordinates (`Point.X`, `Point.Y`, radii, widths, heights, cornerRadius, fontSize): finite.
- `StrokeWidth`: `> 0` and `≤ 1000`.
- `Opacity`: in `[0, 1]`.
- `Polyline`: ≥ 2 points. `Polygon`: ≥ 3 points.
- `Path`: at least one verb; first verb must be `MoveTo`.
- `Image.width` / `height`: in `(0, 2048]`; `Image.bytes` ≤ 256 KB; bytes must start with a PNG or JPEG magic header (cheap peek, no full decode in validation).
- `Text.text`: UTF-8, length ≤ 4096 bytes; empty text ignored (primitive dropped silently during render, not rejected).

**State transitions**:

- `putLayer(clientId, layerName, layer)` — inserts or replaces atomically; emits no `HubEvent` (overlays are per-client, not in the shared state stream).
- `deleteLayer(clientId, layerName)` — removes the layer if present; no-op if absent.
- `clearLayers(clientId)` — removes every layer for the client.
- `listLayers(clientId)` — returns layer descriptors (name, zHint, uploadedAt, primitive count) for the client.
- `removeClient(clientId)` — called from `ScriptingClientDetached` event handler; drops every layer for that client.
- `snapshot(): OverlayLayerSnapshot` — immutable per-frame projection: every layer from every client, pre-sorted by (ownerId, zHint, uploadedAt). Called once per render frame by `HeadlessRenderer`.

**Concurrency**: `ReaderWriterLockSlim`. Write RPCs and `removeClient` take the write lock. `snapshot` takes the read lock, copies into an immutable record, releases before any drawing.

**Per-client isolation**: the render pipeline composites every client's layers into the frame (FR-027), but no RPC exposes another client's layer name, content, or existence. `listLayers` filters by the caller's `clientId`.

---

## Wire → F# mapping summary (overlay additions)

| Wire message | F# type | Module |
|-------------|---------|--------|
| `OverlayLayerWire` | `OverlayLayer` | `OverlayLayerStore` |
| `OverlayPrimitive` (oneof) | `OverlayPrimitive` (DU) | `OverlayLayerStore` |
| `OverlayStyle` | `OverlayStyle` | `OverlayLayerStore` |
| `CoordinateSpace` | `CoordinateSpace` | `OverlayLayerStore` |
| `PathVerb` (oneof) | `PathVerb` (DU) | `OverlayLayerStore` |
| `PointWire` | `Point` | `OverlayLayerStore` |
| `TextAlign` | `TextAlign` | `OverlayLayerStore` |

## State-transition summary (external observer's view)

1. Client calls `GetHubState` → receives a `HubStateSnapshot`.
2. Client subscribes to `StreamHubStateEvents` → receives every subsequent mutation as a `HubStateEvent`.
3. Any mutation RPC (e.g. `SetCamera`, `SetVizAttribute`, `SetActiveTab`) goes through `HubStateStore`'s atomic update, emits exactly one `HubEvent` case, which fans out to all subscribers plus the local GUI.
4. Session lifecycle events (`StateChanged`, `AdminChannelStatusChanged`, `ProxyInstallProgress`) continue to flow through `HubEventBus` unchanged and are included in `StreamHubStateEvents` as wrapped `HubStateEvent` cases.
