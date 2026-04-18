# Data Model — Feature 041 (Hub 040 follow-ups)

**Date**: 2026-04-18 · **Branch**: `041-hub-040-followups`

This feature adds **no new persisted entities** and **no new wire
messages**. Every type below already exists in the feature-040
surface; this document captures the per-frame transient values, the
diagnostic event payloads, and the audit-deliverable shapes the
implementation needs.

---

## 1. `OverlayLayerSnapshot` (existing — feature 040)

**Module**: `FSBar.Hub.OverlayLayerStore`

```fsharp
type OverlayLayerSnapshot =
    { Entries: (Guid * OverlayLayer) array }
```

**Lifetime**: One per render frame. Allocated by
`OverlayLayerStore.snapshot` inside `HeadlessRenderer`'s per-frame
draw closure. **MUST NOT** be retained beyond the closure (FR-006).

**Sort order**: Pre-sorted by `(ownerId, zHint ascending,
uploadedAt ascending)` as documented in the existing `.fsi`. The
renderer iterates in this order and the iteration order IS the draw
order (later overlays draw on top, FR-004).

**Validation**: None at snapshot time — caps and primitive validation
fired on `putLayer` (feature 040 US6 T060).

---

## 2. `OverlayPrimitive` (existing — feature 040)

**Module**: `FSBar.Hub.OverlayLayerStore`

```fsharp
type OverlayPrimitive =
    | Line of from * to * style * space
    | Polyline of points * style * space
    | Polygon of points * style * space
    | Rectangle of x * y * w * h * cornerRadius * style * space
    | Circle of center * radius * style * space
    | Path of verbs * style * space
    | Text of anchor * text * fontSize * fontFamily * align * style * space
    | Image of anchor * width * height * bytes * space
```

**Render-pass mapping** (R3 from research.md):

| Case | Skia call | World transform |
|---|---|---|
| `Line` | `SKCanvas.DrawLine` | apply matrix to `from` and `to` |
| `Polyline` | `SKCanvas.DrawPoints` | apply matrix to each point |
| `Polygon` | `SKCanvas.DrawPath` (closed) | apply matrix to each point |
| `Rectangle` | `SKCanvas.DrawRoundRect` | apply matrix to corners |
| `Circle` | `SKCanvas.DrawCircle` | apply matrix to center + scale to radius |
| `Path` | `SKCanvas.DrawPath` | apply matrix per `PathVerb` point |
| `Text` | `SKCanvas.DrawText` | apply matrix to anchor; font size unscaled (text reads at viewport pixel size, not world units) |
| `Image` | `SKCanvas.DrawImage` | apply matrix to anchor; image pixels unscaled |

**Style application**: `OverlayStyle` maps to a transient `SKPaint`
constructed once per primitive (no caching — Skia paints are cheap
and the per-frame primitive count is bounded by FR-026 caps).

---

## 3. `RenderOverrunDiagnostic` (transient — new in this feature)

**Module**: `FSBar.Hub.HeadlessRenderer` (private — wrapped into
`HubEvents.HubEvent.DiagnosticsLine` before publish)

```fsharp
// Internal record; does not surface in any .fsi
type private RenderOverrunDiagnostic =
    { ElapsedMs: float
      PrimitiveCount: int
      ActiveSubscriberCount: int
      FrameTimestampUnixMs: int64 }
```

**Trigger**: Composite-pass elapsed time exceeds the SC-002 5 ms
budget for a single frame. Measured via
`Stopwatch.GetTimestamp()` deltas around the per-frame overlay-loop
inside `HeadlessRenderer`'s draw closure.

**Surface**: Formatted into a single string and emitted as
`HubEvent.DiagnosticsLine (Severity.Warning, message)` on the
existing `HubEventBus.Sink`. Format:

```
"HeadlessRenderer overlay composite over budget: 7.2 ms (budget 5.0 ms), 1342 primitives, 3 subscribers"
```

**State transitions**: None — fire-and-forget event per-frame.

**Validation**: None — the warning is informational only and the
frame ships unchanged with all overlays drawn (FR-006a).

---

## 4. `MutationRejection` (transient — new in this feature)

**Module**: `FSBar.Hub.HubStateStore` (private)

```fsharp
type private MutationRejection =
    { Mutator: string
      Reason: string }
```

**Trigger**: Any `HubStateStore.set*` mutator returns
`SubmitOutcome.Rejected reason`. The mutator emits exactly one
`HubEvent.DiagnosticsLine (Severity.Warning, formatted)` before
returning the `Rejected` outcome (R7 from research.md).

**Surface**: `HubEvent.DiagnosticsLine`. Format:

```
"HubStateStore.<mutator> rejected: <reason>"
```

Examples:

- `"HubStateStore.setVizAttribute rejected: unknown key: foo.bar"`
- `"HubStateStore.setCamera rejected: scale 200.0 outside [0.05, 100.0]"`
- `"HubStateStore.setLobby rejected: write contention"` (rare —
  three retries on `Interlocked.CompareExchange` exhausted)

**State transitions**: Caller observes `Rejected` and silently
re-renders with `HubStateStore.current()`'s authoritative value
(FR-023a "silent rollback" semantics).

---

## 5. `UiParityFixtureGuard` (test infrastructure — new in this feature)

**Module**: `tests/FSBar.Hub.LiveTests/UiParityFixtureGuard.fs` (new
helper, internal to the test project)

```fsharp
type FixtureRequirement =
    | Engine
    | AiBinary of name: string
    | MapArchive of fileName: string

type FixtureCheck =
    | Available
    | Missing of FixtureRequirement list

val check: requirements: FixtureRequirement list -> FixtureCheck
val skipIfMissing: requirements: FixtureRequirement list -> unit
```

**Behaviour**: `skipIfMissing` calls into the existing
`FSBar.Hub.LiveTests.LiveSession` engine-detection helper for each
requirement, and on any missing requirement calls
`Skip.If(true, "missing fixtures: <names>")` from
`Xunit.SkippableFact` (R4 from research.md). Tests use this at the
top of their body so a single guarded call replaces hand-written
guard ladders.

**Validation**: A test that calls `skipIfMissing []` is a no-op
(returns immediately).

---

## 6. `CoverageAuditRow` (audit deliverable — new in this feature)

**Format**: Markdown table row in
`specs/041-hub-040-followups/coverage-audit.md`. Not an in-process F#
type.

| Field | Description |
|---|---|
| Tab | One of: Setup, Viewer, Units, Style, Settings, Grpc |
| Action label | Human-readable action (e.g. "Click 'Launch session'") |
| Code site | `Tabs/SetupTab.fs:L<line>` reference |
| RPC | `fsbar.hub.scripting.v1.ScriptingService/<MethodName>` |
| FR ref | `FR-NNN` (feature 040 functional requirement) |
| Status | `mapped` / `no RPC — intentional` / `no RPC — gap` |

**Validation**: Reviewer confirms 100% of `mapped` + `intentional` +
`gap` rows account for every action in every tab's input handler.
Any `gap` row is an action-item input to a future feature.

---

## 7. `Sc006ProbeMeasurement` (audit deliverable — new in this feature)

**Format**: Markdown sections in
`specs/041-hub-040-followups/sc-006-probe.md`. Not an in-process F#
type.

Records the line-counts touched to add one new
`ConfigDescriptors.all` entry and surface it via `SetVizAttribute`
end-to-end:

| Section | Content |
|---|---|
| Attribute chosen | name + descriptor kind (e.g. `overlays.fogOfWar : Bool`) |
| Files touched | absolute paths and line counts |
| Total lines | added / modified / deleted |
| Time | wall-clock minutes |
| SC-007 result | pass (≤ 10 lines, ≤ 2 files) / fail with breakdown |

**Validation**: SC-007 numeric thresholds checked against the
`Total lines` row.

---

## 8. `QuickstartWalkthroughLog` (audit deliverable — new in this feature)

**Format**: Markdown timestamped list in
`specs/041-hub-040-followups/quickstart-walkthrough.md`. Not an
in-process F# type.

Each entry records: step number from feature 040's `quickstart.md`,
wall-clock time started/completed, observed result vs. expected,
friction notes. Validation is reviewer inspection.

---

## Cross-Entity Relationships

```
HeadlessRenderer per-frame closure
   ├── reads HubStateStore.current().Camera          (R3)
   ├── calls OverlayLayerStore.snapshot              (R2)
   ├── iterates OverlayLayerSnapshot.Entries
   │     └── per primitive: apply OverlayPrimitive  (Section 2)
   └── if elapsed > 5 ms → emit RenderOverrunDiagnostic via DiagnosticsLine (Section 3)

HubStateStore.set* mutators
   ├── on success → emit typed HubEvent (existing)
   └── on Rejected → emit MutationRejection via DiagnosticsLine (Section 4)

UiParity live tests
   └── call UiParityFixtureGuard.skipIfMissing       (Section 5)

US5 audits
   ├── coverage-audit.md  rows = Section 6
   ├── sc-006-probe.md    measurement = Section 7
   └── quickstart-walkthrough.md log = Section 8
```

No new persisted state, no new wire types. Storage section in
plan.md remains "Filesystem only — unchanged from feature 040".
