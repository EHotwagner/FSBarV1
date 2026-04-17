# Data Model — Hub Viewer Fixes (038)

The feature touches persisted settings, the session manager's state,
and the shared unit-display construction path. No new persistence
formats and no new proto wire contracts.

---

## 1. `HubSettings` (extended)

**File**: `src/FSBar.Hub/HubSettings.fs(i)`

Add one field to the existing record. Everything else stays identical.

```fsharp
type HubSettings = {
    BarDataDirOverride: string option
    EngineVersionOverride: string option
    GrpcPort: int
    LaunchGraphicalViewerDefault: bool
    /// NEW — default ON per FR-004a. When true, every launched match
    /// begins paused and stays paused until the user toggles via the
    /// Viewer-tab control or any other pause mechanism.
    StartPausedDefault: bool
    SchemaVersion: int
}
```

### Defaults

```fsharp
let defaults: HubSettings = {
    BarDataDirOverride = None
    EngineVersionOverride = None
    GrpcPort = 5021
    LaunchGraphicalViewerDefault = false   // unchanged
    StartPausedDefault = true              // NEW
    SchemaVersion = 1
}
```

### Validation

- `StartPausedDefault` is a plain bool — no validation.
- `SchemaVersion` stays `1`. Missing `StartPausedDefault` on read
  (older files) silently defaults to `true` via
  `parseBool root "startPausedDefault" defaults.StartPausedDefault`
  — additive change, no migration step.

### State transitions

None — immutable record reloaded from disk on Hub startup and rewritten
atomically on every Setup-tab toggle.

---

## 2. `SessionManager` internal state

**File**: `src/FSBar.Hub/SessionManager.fs(i)`

Two new internal fields + two new public members.

### New internal state (private to impl)

```fsharp
// Set by Launch(), consumed on first Running transition.
let mutable private startPausedForNextLaunch : bool = false

// Reflects the hub-known engine pause state. Flipped by every /pause
// we emit. Not a mirror of the engine — see research.md §R2 (A).
let mutable private isPaused : int = 0  // 0 or 1, Interlocked-updated
```

### New public members

```fsharp
type SessionManager =
    ...existing members...
    /// True when the hub has most recently issued a pause to the engine.
    /// See research.md §R6 for drift semantics.
    member IsPaused : bool

    /// Convenience — flips IsPaused and sends a single /pause chat
    /// command. Safe to call in any state; no-op when not Running.
    member TogglePause : unit -> unit
```

`Launch(config)` picks up `startPausedForNextLaunch <- settings.StartPausedDefault`
via a new `Launch: config * startPaused:bool` overload; see contracts
for the exact signature.

### State-machine side effects

```
Starting config → (lifecycle pump) → Running rs
                                     ├─ if startPausedForNextLaunch
                                     │     then rs.BarClient.SendCommands [Commands.sendText "/pause" 0]
                                     │          isPaused <- 1
                                     │          events.Publish(SessionPaused true)
                                     └─ otherwise isPaused <- 0

Viewer-tab click → TogglePause()    ├─ flip isPaused
                                     ├─ rs.BarClient.SendCommands [Commands.sendText "/pause" 0]
                                     └─ events.Publish(SessionPaused isPaused)

End() / teardown                    ├─ isPaused <- 0
                                     └─ startPausedForNextLaunch unchanged
```

Existing `SetPaused(bool)` becomes the assignment form: "ensure paused
state matches the argument"; it is a thin wrapper over `TogglePause`
when the current state differs.

---

## 3. `UnitDisplay` construction — shared adapter module

**New file**: `src/FSBar.Viz/UnitDisplayAdapter.fs(i)`

Single source of truth for translating every upstream unit shape into
a `UnitDisplay` before handing it to `UnitGlyph.buildUnit`. FR-002
("single shared code path") becomes compiler-enforced because the
module exposes exactly three public constructors.

```fsharp
module FSBar.Viz.UnitDisplayAdapter

/// Resolve a tracked friendly unit (ours) to a display record.
val ofTrackedUnit:
    defCache: FSBar.Client.UnitDefCache ->
    teamId: int ->
    unitId: int ->
    unit: FSBar.Client.TrackedUnit ->
        UnitDisplay

/// Resolve a tracked enemy to a display record.
val ofTrackedEnemy:
    defCache: FSBar.Client.UnitDefCache ->
    enemyId: int ->
    enemy: FSBar.Client.TrackedEnemy ->
        UnitDisplay

/// Resolve a BarData encyclopedia entry to a display record
/// (static preview variant — no heading, pinned footprint).
val ofEncyclopediaEntry:
    entry: FSBar.Viz.EncyclopediaEntry ->
    pinnedFootprint: float32 ->
        UnitDisplay
```

(`EncyclopediaEntry` today lives inside `EncyclopediaTab` — it moves
to `FSBar.Viz` so the adapter can reference it without introducing a
`FSBar.Hub.App` → `FSBar.Viz` reverse dependency. See contracts.)

### Downstream rewiring

| Caller | Today | After 038 |
|--------|-------|-----------|
| `SceneBuilder.gameStateToSnapshotWith` | `DisplayUnits = Map.empty` | Populates via `UnitDisplayAdapter.ofTracked*` when a `UnitDefCache` is passed |
| `GameViz.buildDisplayUnits` | Hand-rolled `toUnitDisplay` + local def cache | Delegates to `UnitDisplayAdapter.ofTrackedUnit` |
| `EncyclopediaTab.renderDetail` | Builds `UnitDisplay` inline (lines 290-317) | Calls `UnitDisplayAdapter.ofEncyclopediaEntry` |
| `ConfigPanel` unit preview | Builds `UnitDisplay` inline | Same adapter |
| `SetupTab` faction/side preview | Not rendered today | N/A (no preview on Setup tab) |

This is the refactor that makes FR-001 / FR-002 possible without
drift risk.

---

## 4. `UnitDisplay` — no schema change

**File**: `src/FSBar.Viz/VizTypes.fs(i)` (existing)

The record itself is unchanged. The direction-triangle work is
rendering-only; `HeadingRadians: float32` already carries the facing.

---

## 5. `EncyclopediaEntry` relocation

**File move**:
- **From**: anonymous type inside `src/FSBar.Hub.App/Tabs/EncyclopediaTab.fs`
- **To**: public record in `src/FSBar.Viz/EncyclopediaData.fs(i)` (new)

```fsharp
module FSBar.Viz.EncyclopediaData

type EncyclopediaEntry = {
    DefId: int
    InternalName: string
    Subfolder: string
    Faction: FactionId
    Tier: Tier
    Shape: MovementShape
    MetalCost: int
    EnergyCost: int
    BuildTime: int
    Health: int
    FootprintX: int
    FootprintZ: int
    SightRangeElmo: float32
    WeaponRangesElmo: float32 list
}

val buildFromBarData: unit -> EncyclopediaEntry list
```

`EncyclopediaTab` consumes this list; `UnitDisplayAdapter` consumes
individual entries. No behaviour change for the encyclopedia itself.

---

## 6. Scene plumbing — no new shapes

The Viewer-tab pause button is a simple rect + text pair rendered
inline in `ViewerTab.render`. No new `Scene` primitives, no new
widget module, no new state beyond `sessionManager.IsPaused` for the
icon. Hit testing reuses the Setup-tab button pattern (rect
`Contains` check against mouse position).

---

## 7. Entity summary

| Entity | Kind | Lifecycle | Storage |
|--------|------|-----------|---------|
| `HubSettings.StartPausedDefault` | bool field | loaded on Hub start, written on Setup-tab toggle | `settings.json` |
| `HubSettings.LaunchGraphicalViewerDefault` | bool field (existing) | same | same |
| `SessionManager.IsPaused` | bool member | per-session, cleared on End | in-memory |
| `SessionManager.startPausedForNextLaunch` | internal bool | per-launch, set at Launch, consumed at Running | in-memory |
| `UnitDisplayAdapter` constructors | pure functions | called per-frame | n/a |
| `EncyclopediaEntry` | record | built once on first render | in-memory |
