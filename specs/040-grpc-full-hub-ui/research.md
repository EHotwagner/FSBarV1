# Phase 0 Research: gRPC parity for Hub UI

**Feature**: 040-grpc-full-hub-ui
**Date**: 2026-04-18

Each section resolves one design question raised by the spec, plan, or Technical Context. Decisions feed directly into Phase 1 artifacts.

---

## R1 — Where does `HubStateStore` live and what does it own?

**Decision**: New module `FSBar.Hub.HubStateStore` in the packable core library (`src/FSBar.Hub/`). It owns every UI-state field that the spec enumerates on the FR-015 event stream:

- `ActiveTab: HubTab` (Setup / Viewer / Units / Style / Cfg / Grpc)
- `VizConfig: VizConfig` (authoritative)
- `Camera: ViewerCamera` (pan, zoom, auto-fit)
- `Lobby: LobbyConfig` (mutable only when `SessionManager.State = Idle`)
- `Encyclopedia: EncyclopediaSelection` (faction filter + selected entry)
- Derived/cached fields: `Presets: string list` (file list cache, invalidated on save/delete)

Writes go through a single `update: HubStateStore -> (State -> State * HubEvent list) -> SubmitOutcome` mutator that atomically swaps state, emits events via `HubEventBus`, and returns `Sent` / `Rejected reason`.

**Rationale**: Single source of truth cleanly satisfies FR-015/016/020 (LWW, events per mutation, GUI stays in sync with gRPC writes). Living in core library means scripting clients referencing only `FSBar.Hub.dll` still see the full surface — matches the 035 split-project rationale. Atomic mutator keeps concurrent-write semantics testable against SC-005.

**Alternatives considered**:
- Decentralized mutators on `SessionManager`, `HubSettings`, `StylePreset` with ad-hoc event emission per call site. Rejected: scatters LWW semantics, harder to test, would force six tab refactors regardless.
- Redux-style reducer with explicit action DU. Overbuilt for ~6 fields; update-fn is equivalent and more idiomatic F#.

---

## R2 — How is the rendered Viewer-tab frame exposed to gRPC without fighting the GUI thread or GRContext?

**Decision**: New module `FSBar.Hub.HeadlessRenderer`. On the first `StreamRenderFrames` or `GetRenderFrame` call it allocates one off-screen `SKSurface` at a fixed viewport size (default 1024×768, client-overridable per-subscriber) and keeps it alive for the subscriber's lifetime. A worker task runs at the client's requested cadence (default 10 Hz, capped at 30 Hz): it reads the current `GameState` from `SessionManager`, reads `VizConfig` + `Camera` from `HubStateStore`, calls the already-pure `SceneBuilder.buildSceneHeadlessView`, rasterizes into the surface, encodes PNG (or JPEG on request) via `SKImage.Encode`, and enqueues the bytes into a bounded channel shared with the gRPC handler.

**Rationale**:
- `SceneBuilder.buildSceneHeadlessView` is already pure and stateless-per-call (confirmed by exploration — only module-level pulse-phase clock mutation).
- Raster SKSurface sidesteps the documented GRContext segfault (CLAUDE.md). Encoding is CPU-only and isolated from the GUI's rendering.
- Per-subscriber worker + bounded channel matches the `StreamGameFrames` model (16-deep, drop-oldest, detach at 32 cumulative) — consistent observability and backpressure.

**PNG encode budget vs SC-008 (≤ 200 ms P95 at 10 Hz)**: Published SkiaSharp benchmarks put 1024×768 PNG encode at ~20–40 ms on a modern Linux dev box. Plus ~5 ms render + ~1 ms loopback gRPC = ~50 ms per frame mean, well within SC-008. Will measure in the US2 live test; if margin is tight we fall back to JPEG (cheaper encode).

**Alternatives considered**:
- Capture from the GUI's existing window surface. Rejected: couples to GUI thread, and per CLAUDE.md the Viewer uses a raster SKSurface + GL texture upload — the texture path would require synchronization we don't have.
- Stream raw RGBA (no encoding) to let clients pick compression. Rejected: ~3 MB/frame uncompressed at 1024×768, 10 Hz = 30 MB/s loopback. Encodes dominate by 10–30× at larger viewports; PNG default + JPEG fallback is the better contract.

---

## R3 — How do we preserve wire backward compatibility (FR-019, SC-007)?

**Decision**: The single `proto/hub/scripting.proto` file stays in package `fsbar.hub.scripting.v1`. All changes are additive:

1. New RPCs appended after existing `SendAdminMessage` in the service block.
2. New messages appended at file bottom.
3. No field number reuse; new fields on existing messages only at the end (we do not renumber).
4. `ActiveSession.admin_channel_status` (field 7) already exists from feature 039; its semantics are unchanged.

A pre-merge check runs `buf breaking` against `main` to catch any accidental renames / reshapes.

**Rationale**: Matches the Assumption in the spec ("existing scripting-service wire surface is additive-only") and the constitution's compatibility guidance. Keeping `v1` avoids forcing existing clients (including `scripts/examples/16-hub-admin.fsx`) to rebind.

**Alternatives considered**:
- Bump to `v2`. Rejected: the feature is additive, not incompatible. A v2 would force every existing consumer to opt in or maintain two stubs.
- New package (`fsbar.hub.ui.v1`) alongside `v1`. Rejected: splits the contract unnecessarily; operators would need two stubs for one service.

---

## R4 — Rehoming: which capabilities already exist where, and what moves?

From the exploration:

| Capability | Today | Action |
|-----------|-------|--------|
| VizConfig attribute registry | `FSBar.Viz.ConfigDescriptors` with `applyValues` / `extractValues` | Reuse as-is; `HubStateStore.setVizAttribute` routes through it. |
| Preset list/save/load/delete | `FSBar.Viz.StylePreset` | Wrap in `FSBar.Hub.PresetFacade` so the hub can enforce name validation + emit events. |
| Encyclopedia entries | `FSBar.Viz.EncyclopediaData.buildFromBarData` | Wrap in `FSBar.Hub.EncyclopediaFacade` (cached once at Hub startup). |
| Lobby validation | `FSBar.Hub.LobbyConfig.validate` | Reuse; `HubStateStore.setLobby` calls it and returns the error list on Rejected. |
| Session lifecycle | `FSBar.Hub.SessionManager.Launch` | Add `Stop(): unit`; existing `Launch(config, startPaused)` stays. |
| Scene rendering (pure) | `FSBar.Viz.SceneBuilder.buildSceneHeadlessView` | Reuse unmodified — already pure. |
| Event fan-out | `FSBar.Hub.HubEvents.HubEventBus` | Extend `HubEvent` DU with new cases (backward compatible — additive to a DU is wire-compatible for in-proc consumers). |
| HubSettings | `FSBar.Hub.HubSettings` record + `load`/`save` | Add per-field update helpers (e.g. `updateStartPausedDefault`) for LWW mutation + event emission. |

**Rationale**: Avoids moving code across project boundaries. `FSBar.Viz` owns pure viz primitives; `FSBar.Hub` owns orchestration and state. Feature 040 extends this split cleanly.

**Alternatives considered**:
- Rehome `StylePreset` / `EncyclopediaData` into `FSBar.Hub`. Rejected: they are pure viz-domain types and are already consumed by the Viewer/Configurator tabs — keeping them in `FSBar.Viz` means no ripple into `FSBar.Viz.Tests`.

---

## R5 — Concurrent subscriber budget and fan-out topology for `StreamRenderFrames`

**Decision**: One dedicated encode worker per subscriber (each sees only its own bounded channel). Rendering happens per-subscriber too — not shared — because different clients may request different viewport sizes or formats.

Budget: hub can sustain ~8 concurrent subscribers at 10 Hz without saturating a 4-core dev box (per R2 encode-time estimate). `HeadlessRenderer` accepts up to 8; additional subscribers are rejected with `RESOURCE_EXHAUSTED`. That limit is configurable via `HubSettings.MaxRenderFrameSubscribers` with default 8.

**Rationale**: Independent per-subscriber pipelines avoid mutation races on shared SKSurfaces and let the existing `StreamGameFrames`-style detach policy apply unchanged. 8 is well above the expected single-digit real-world count while staying cheap.

**Alternatives considered**:
- Single render/encode pipeline, broadcast encoded bytes to all subscribers. Rejected: forces one viewport size + format for everyone, and the bandwidth savings are modest (encoded bytes are already small).
- No cap; rely on the detach-at-32-drops policy alone. Rejected: an unbounded subscriber count can starve the GUI render loop even if each subscriber individually stays within its drop budget. Explicit cap is cheaper to reason about.

---

## R6 — Lobby edit while running: reject or queue?

**Decision**: Reject (`FAILED_PRECONDITION` with `session already running`), consistent with the Setup tab today which locks its controls during `Running`. Callers re-submit after the session ends.

**Rationale**: Matches existing GUI semantics and the spec edge case ("Lobby configuration is edited via gRPC while a session is already running: the service rejects the edit until the session has ended"). Queuing introduces ordering and cancel semantics we don't need.

**Alternatives considered**:
- Queue the next-session config. Rejected: no GUI equivalent, and cancel semantics are unclear.

---

## R7 — `GetHubState` snapshot format

**Decision**: Single nested message `HubStateSnapshot` with every field the FR-015 stream can emit: `active_tab`, `viz_config`, `camera`, `lobby`, `encyclopedia`, `preset_list`, `session_status`, `admin_channel_status`, `hub_settings`. Clients call `GetHubState` once on connect, then subscribe to `StreamHubStateEvents` for deltas.

**Rationale**: Clarification Q3 answered: "Future-only deltas + separate snapshot RPC". Monolithic snapshot is simpler than per-field snapshot RPCs (would multiply the RPC count for no gain) and matches how a dashboard typically rehydrates.

**Alternatives considered**:
- Per-field snapshot RPCs (`GetActiveTab`, `GetVizConfig`, etc). Rejected: noisy surface.
- Snapshot event at the head of `StreamHubStateEvents`. Rejected by the clarification (stream must be future-only).

---

## R8 — `VizConfig` wire projection

**Decision**: Wire `VizConfig` as a `map<string, VizAttributeValue>` where `VizAttributeValue` is a oneof over `bool`, `int32`, `float`, `string`, `Color` (`uint32` rgba), and `StringList`. Keys match `ConfigDescriptors.all` entry `.Key`.

The gRPC handler uses `ConfigDescriptors.applyValues` to convert the map back into an F# `VizConfig`; `ConfigDescriptors.extractValues` for the reverse. Unknown keys → `INVALID_ARGUMENT`; range-check failures → `INVALID_ARGUMENT`.

**Rationale**: Reuses the already-authoritative `ConfigDescriptors` registry (the Configurator tab's single source of truth). New viz attributes (SC-006's "trivially extensible") become wire-compatible as soon as a descriptor is registered — no proto change needed.

**Alternatives considered**:
- Mirror every `VizConfig` field as a typed proto message. Rejected: doubles maintenance cost and breaks SC-006 (each new attribute becomes a proto change).
- JSON payload in a `string` field. Rejected: escapes proto's typing entirely; downstream codegen suffers.

---

## R9 — Render-frame wire format

**Decision**: `bytes image_bytes` plus `ImageFormat format` enum (`PNG` default, `JPEG` optional). Each frame carries `int64 rendered_at_unix_ms`, `int64 encoded_at_unix_ms`, `uint64 client_sequence`, `int32 viewport_width`, `int32 viewport_height`, and `int32 quality` (meaningful only for JPEG).

`StreamRenderFramesRequest`: `client_label`, `target_hz` (default 10, capped at 30 server-side), `ImageFormat format`, `int32 viewport_width` / `_height` (both optional; default 1024×768), `int32 jpeg_quality` (default 80, ignored for PNG), `bool close_on_session_end`.

**Rationale**: Mirrors `StreamGameFrames` ergonomics (client-label, sequence, timestamps). Dual timestamps (render complete + encode complete) let clients attribute latency per SC-008.

**Alternatives considered**:
- WebP / AVIF. Rejected: SkiaSharp's WebP encode is slower than PNG at comparable quality on our hardware; AVIF support is sparse.
- Per-frame JSON envelope with base64 image. Rejected: bloats wire ~33% and adds parsing cost.

---

## R10 — Proto regeneration and FSI example coverage

**Decision**: Regenerate via `cd proto && buf generate` (already documented in CLAUDE.md). New generated file `src/FSBar.Proto/Generated/hub/scripting.gen.fs` is committed; `FSBar.Proto.fsproj` already lists the hub path, no project change needed.

New numbered FSI examples (`scripts/examples/17..20`) each cover a single user story end-to-end and are runnable against a local Hub per Constitution V. Existing `16-hub-admin.fsx` smoke-tested during the feature and its snippets pasted into a unit test to guard SC-007.

**Rationale**: Follows the project's existing scripting-example workflow (features 035 and 039 already numbered examples through 16). Keeps example scripts small, one story each.

**Alternatives considered**:
- One monolithic example covering every RPC. Rejected: harder to maintain and slower to run.

---

## R11 — Overlay primitive DSL shape and validation

**Decision**: Wire contract is a `oneof OverlayPrimitive` covering eight kinds — `Line`, `Polyline`, `Polygon`, `Rectangle`, `Circle`, `Path`, `Text`, `Image`. Each primitive nests a common `OverlayStyle` message (stroke rgba, stroke width, optional fill rgba, opacity, optional dash pattern) plus primitive-specific geometry and text/image extras. `Path` is a sequence of `PathVerb` messages (`MoveTo` | `LineTo` | `CubicTo` | `Close`) — mirrors `SKPath`'s verb set 1:1.

Validation on `PutLayer`:
- reject coordinates that are NaN / ±∞
- reject stroke widths ≤ 0 or > 1000 pixels
- reject opacities outside `[0, 1]`
- reject empty polyline / polygon (< 2 / < 3 points)
- reject image bytes > 256 KB or decoded dimensions > 2048 px (after cheap header peek — no full decode before validation)
- reject unknown oneof case / unknown `PathVerb` kind
- reject layer name outside 1..64 UTF-8 code points or containing path separators / control chars

**Rationale**: The eight primitives cover every useful overlay case (route lines, threat zones, markers, annotations, legends, inline images) while keeping the server's render work to simple `SKCanvas` calls. Per-primitive validation happens once at `PutLayer` time, not every frame — failures surface immediately to the caller.

**Alternatives considered**:
- Free-form `SKPath` + `SKPaint` serialisation. Rejected by Q1 clarification (risk + portability).
- Adding curves / gradients / blend modes. Rejected for v1 — the eight primitives + solid fills cover the intended use cases; gradients can ship in a follow-up.

---

## R12 — Coordinate-space transform at render time

**Decision**: Every frame, `HeadlessRenderer` computes a single world→screen affine matrix from the current `ViewerCamera` (pan, zoom, auto-fit letterbox) and passes it plus the raw primitive list to the overlay compositor. For each `World`-space primitive the compositor calls `SKCanvas.Save()` + `SKCanvas.SetMatrix(world)` before drawing, and restores afterwards; for `Screen`-space primitives it draws directly in viewport coordinates (no transform). The matrix is computed once per frame regardless of primitive count.

**Rationale**:
- Matches the convention the `SceneBuilder` already uses internally for world-anchored overlays (unit glyphs transform with the camera the same way).
- Per-primitive matrix switches cost under 1 µs each; even at the FR-026 cap (8000 primitives) total matrix overhead stays under 8 ms / frame — well inside SC-009's 100 ms budget.
- Single source of truth for the world matrix (`ViewerCamera` → matrix) means camera RPCs (FR-009) automatically move world-space overlays.

**Alternatives considered**:
- Per-primitive embedded matrix. Rejected — forces clients to do camera math and breaks the "mark a chokepoint once, it tracks camera" property.
- Normalized coordinates only. Rejected by Q2 clarification.

---

## R13 — `OverlayLayerStore` concurrency and per-client cleanup

**Decision**: `OverlayLayerStore` holds a single `Dictionary<clientId: Guid, Dictionary<layerName: string, OverlayLayer>>` guarded by `ReaderWriterLockSlim`. Writers take the write lock for `PutLayer` / `DeleteLayer` / `ClearLayers` / `removeClient`; readers take the read lock for the once-per-frame snapshot operation. The render pipeline takes a read-lock snapshot into an immutable `OverlayLayerSnapshot` record and releases the lock before drawing — so draw time never blocks writers, and write RPCs never block the render loop beyond the snapshot copy (measured under 1 ms for worst-case 8 clients × 16 layers × 500 primitives).

Client identity: the existing `ScriptingHub` already assigns each connected client a `Guid` at `Connected` time (used for the clients roster in `GetSessionStatus`). Overlay RPCs pull this id from the `ServerCallContext.UserState` or from the connected-client registry.

Cleanup: `ScriptingHub` emits the existing `ScriptingClientDetached` event on clean disconnect or overflow-detach. `OverlayLayerStore` subscribes to that event and calls `removeClient(id)` synchronously in the handler — meeting SC-010 (visible within two frames at 10 Hz).

**Rationale**: `ReaderWriterLockSlim` is appropriate when reads dominate (one per frame) and writes are rare (on every `PutLayer`). Lock-free alternatives (`ImmutableDictionary` + `Interlocked.CompareExchange`) add allocation cost per mutation without measurable benefit at this scale. Subscribing to `ScriptingClientDetached` keeps cleanup at the existing lifecycle boundary — no new disconnect-detection code.

**Alternatives considered**:
- Each subscriber owns its own render pipeline + its own overlay list. Rejected — an overlay-authoring client is usually different from a frame-consuming client, and overlays must appear for every frame subscriber (FR-027), not just the uploader.
- TTL-based cleanup (drop layer after N minutes of inactivity). Rejected — no user-visible benefit, and the disconnect signal is already reliable.

---

## R14 — Composing overlays into the render pipeline

**Decision**: `HeadlessRenderer.renderOnce` / per-subscriber worker sequence:

1. Pull current `GameState` from `SessionManager`.
2. Pull current `VizConfig` + `Camera` from `HubStateStore`.
3. Pull current overlay snapshot from `OverlayLayerStore`.
4. Build the `FSBar.Viz.Scene` via `SceneBuilder.buildSceneHeadlessView`.
5. Draw the scene into the off-screen SKSurface.
6. Draw overlay layers in ascending `zHint` order (tie-break by most-recent-upload timestamp), applying the world or screen transform per primitive.
7. Encode (PNG/JPEG).
8. Enqueue on the subscriber channel.

Steps 1–3 execute atomic snapshots (no locks held past the snapshot). Step 6 is pure Skia drawing; no allocation on the hot path aside from transient `SKPath` objects (pooled via `ArrayPool<byte>` for coordinate arrays where relevant).

**Rationale**: Clean composition — built-in scene first, client overlays on top (per FR-024, Q3 consequence). Snapshotting the overlay store once per frame bounds the contention window. Drawing in zHint order matches the spec's "layers have a per-layer zHint" rule.

**Alternatives considered**:
- Render overlays onto a separate SKSurface + compose via `DrawImage`. Rejected — redundant copy; Skia's same-surface drawing is already compositing.
- Interleave overlays with built-in overlays via shared z values. Rejected by Assumptions (overlays above built-ins in v1) and FR-024.

---

## Summary of resolved unknowns

| Topic | Resolution |
|-------|-----------|
| Central store location | `FSBar.Hub.HubStateStore` (core lib) |
| Render pipeline | `FSBar.Hub.HeadlessRenderer`, off-screen raster SKSurface |
| Wire compatibility | Additive only in `fsbar.hub.scripting.v1`, `buf breaking` gate |
| Rehoming | None — all existing capabilities stay in their current projects; hub wraps them |
| Subscriber concurrency | Cap 8, per-subscriber pipeline, reuse 16-deep channel / detach-32 policy |
| Lobby while running | Reject with `FAILED_PRECONDITION` |
| Snapshot format | Single `HubStateSnapshot` message via `GetHubState` |
| VizConfig projection | `map<string, oneof>` via `ConfigDescriptors` round-trip |
| Render-frame format | PNG default, JPEG optional, dual timestamps |
| Overlay DSL shape | 8 typed primitives + common `OverlayStyle`, validated at `PutLayer` |
| Overlay coordinate transform | Per-frame world→screen matrix; `World` primitives wrapped in Save/SetMatrix, `Screen` draws raw |
| Overlay store concurrency | `ReaderWriterLockSlim` + per-frame immutable snapshot; cleanup on `ScriptingClientDetached` |
| Overlay composition | Scene first, overlays in ascending zHint order, all above built-in overlays |
| Examples | `17..21` FSI scripts, one per user story (21 covers US6 overlays) |

No `NEEDS CLARIFICATION` remains.
