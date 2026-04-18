# Feature Specification: Comprehensive gRPC Logging Stream for Hub Diagnostics

**Feature Branch**: `042-grpc-log-stream`
**Created**: 2026-04-18
**Status**: Draft
**Input**: User description: "create a comprehensive grpc logging stream that emits hub logs to the grpc client. the logging features are fine grained and can be turned on/off. the idea is to be able to run tests from the client and have enough information to debug hub features like admin/speed......"

## Clarifications

### Session 2026-04-18

- Q: Scope of instrumentation — does this feature add new log-emission sites or only route existing `HubEvent` traffic? → A: Add new fine-grained emission sites across every listed subsystem, including `Debug`-level traces in admin-channel wire protocol and scripting RPC dispatch; explicitly out-of-scope: engine-launcher `infolog.txt` capture, map-analysis, synthetic-data, and viz-rendering internals.
- Q: What does a client receive when it opens the stream with no filter (empty categories, no severity floor)? → A: All categories at `Info` floor; `Debug` requires explicit opt-in.
- Q: Who owns the correlation ID for a unary RPC — client, Hub, or both? → A: Both — Hub auto-assigns a fresh ID per RPC; a client-supplied request-metadata header overrides. Effective ID is echoed back in the unary response.
- Q: Per-entry message length policy? → A: Cap at 8 KiB UTF-8; longer messages are truncated at the Hub with a trailing ` …[truncated N bytes]` marker where `N` is the original byte length beyond the cap.
- Q: Concurrent log-stream subscriber cap? → A: New `HubSettings.MaxLogStreamSubscribers` (default 8, range 1–32); exceeding subscriptions are rejected with `ResourceExhausted`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Stream fine-grained hub logs to a remote test client (Priority: P1)

A test author writing an integration test — either an F# scripting client over gRPC or a live test harness — needs to observe what the Hub is doing while it exercises a feature (launching a session, toggling pause, changing engine speed, installing the proxy, talking to the autohost channel). Today the only remote visibility is `StreamHubStateEvents`, which emits coarse domain events (`StateChanged`, `SessionPaused`, `AdminChannelStatusChanged`, `DiagnosticsLine(Info|Warning|Error)`). When a test fails, the author has to re-run locally and stare at the Hub GUI or read `infolog.txt` to figure out what actually happened between the domain events. A comprehensive gRPC log stream closes that gap: the client subscribes to a logging RPC, receives every log line the Hub would have written to its own diagnostics sink, and attaches the captured transcript to the test failure.

**Why this priority**: This is the core motivation of the feature. Without remote log visibility, every non-trivial test failure becomes a local reproduction exercise. Shipping this as the MVP immediately unblocks authoring of reliable admin-channel, session-launch, and scripting-RPC tests.

**Independent Test**: A scripting client connects to the Hub, starts the log stream RPC with no filters, triggers any observable Hub action (e.g. `ConfigureLobby` with an invalid map), and within one second receives at least one log entry on the stream whose message matches the failure reason surfaced by the corresponding unary RPC response.

**Acceptance Scenarios**:

1. **Given** the Hub is running and a gRPC scripting client is connected, **When** the client opens the log stream with no filters and then calls `LaunchSession` on a valid lobby, **Then** the client receives a sequence of log entries covering session setup, engine spawn, proxy handshake, and the `Running` transition — each entry carrying a timestamp, severity, source component, and free-form message.
2. **Given** two scripting clients are connected to the Hub, **When** both open independent log streams and a third action emits a log line, **Then** both clients receive the same line with matching timestamps and neither client sees lines addressed to other features it filtered out.
3. **Given** a test client that started the log stream before issuing a `Pause` RPC against a running session, **When** the admin channel acknowledges the pause, **Then** the stream contains at least one entry naming the admin-channel component and the pause outcome (`Sent` / `Coalesced` / `Rejected`) within two seconds of the RPC returning.

---

### User Story 2 - Filter logs by feature/category and severity (Priority: P1)

An author debugging a specific Hub feature (say, admin-channel speed changes) does not want to wade through unrelated render-frame or preset-load chatter. They need to enable only the categories they care about at the severity floor they care about, and flip categories on or off while the stream is already running — without tearing down the session or reconnecting. This keeps the log volume tractable on CI runs (where logs are captured as test artifacts) and makes failure triage fast.

**Why this priority**: Fine-grained on/off control is explicitly requested in the feature description and is what distinguishes this stream from a dumb tail. Without filters, the stream either floods the client or the author turns verbose logging off and loses the debug value.

**Independent Test**: A test client opens the stream filtered to `AdminChannel` category at `Debug` severity floor, triggers a flurry of preset-save and camera-pan actions (which must not emit entries on the stream), then triggers a pause, then mutates the filter over the RPC to add `SessionManager` — and receives only admin-channel entries during phase one and both admin-channel + session-manager entries during phase two.

**Acceptance Scenarios**:

1. **Given** the client opens a log stream with `categories = [AdminChannel]` and `minSeverity = Debug`, **When** a session-launch sequence runs, **Then** the client receives admin-channel entries (including `Debug`-level wire-protocol traces) but does **not** receive entries tagged with other categories.
2. **Given** an active filtered log stream, **When** the client sends an update-filter request adding a new category, **Then** subsequent log entries matching the new category are delivered on the same stream without the client having to reconnect, and the old categories remain active.
3. **Given** the Hub emits 10,000 log entries per second across all categories, **When** a client subscribes with `minSeverity = Warning`, **Then** the client receives only entries at `Warning` or higher and the server does not serialise/send entries that would be filtered out.

---

### User Story 3 - Correlate log entries with RPC calls and session events (Priority: P2)

When a test calls several unary RPCs in sequence (e.g. `ConfigureLobby` → `LaunchSession` → `Pause` → `StopSession`), the author wants to know exactly which log lines belong to which call, and which belong to background hub activity (scripting-client attach/detach, headless render frames, event-bus fan-out). Correlation keys — request IDs, session IDs, client IDs — attached to each entry let a test assertion express things like "the pause I issued succeeded with `Sent` on the admin channel, and no error lines were emitted between my call and the acknowledgement."

**Why this priority**: This makes the stream useful for **structured assertions**, not just after-the-fact inspection. Without correlation, tests can only grep for substrings, which is fragile. Deferred to P2 because the unfiltered stream from US1 already enables manual debugging — correlation is the polish that makes automation robust.

**Independent Test**: A test client issues a `Pause` RPC while recording the log stream, then asserts that at least one entry between the client-side call and client-side response timestamps carries a correlation ID equal to the one the client passed in the request metadata (or that the Hub assigned and returned in the response).

**Acceptance Scenarios**:

1. **Given** a scripting client passes a correlation ID header on a unary RPC, **When** the Hub handles that RPC, **Then** every log entry emitted while handling it carries the same correlation ID field on the log stream.
2. **Given** a running session identified by session ID `S1`, **When** the admin channel emits log entries about it, **Then** each entry carries `S1` in a session-ID field that the test client can assert on.
3. **Given** two scripting clients acting in parallel, **When** each issues its own `Pause` call, **Then** entries for each call carry distinct correlation IDs and no client-assigned correlation ID from client A leaks into client B's logs.

---

### User Story 4 - Control log volume and retention to avoid drowning the transport (Priority: P2)

The Hub runs at interactive latencies — tens of events per frame at 60 fps during render-frame streaming is realistic. A misconfigured filter or a burst of proxy-install chatter can produce more log entries per second than the gRPC transport or the client can absorb. The system must gracefully drop or coalesce rather than wedge the producer, and must tell the client how many entries it missed since the last delivery so the client can decide whether a test result is still trustworthy.

**Why this priority**: Necessary for production use but the naive unbounded version from US1 is already enough for interactive debugging. Drop handling matters mainly when a CI run enables broad filters on a long test.

**Independent Test**: With a throttled client (artificially slow consumer), the test triggers a burst of log entries known to exceed the per-client buffer, then asserts that (a) the client still receives entries after the burst, (b) at least one subsequent entry carries a `dropped_since_last = N` field with `N > 0`, and (c) the Hub's own operation was not delayed by the slow client.

**Acceptance Scenarios**:

1. **Given** a client consuming log entries more slowly than the Hub produces them, **When** the per-client log buffer overflows, **Then** older queued entries are dropped (not newer) and the next delivered entry reports how many were dropped since the previous delivery.
2. **Given** a client explicitly cancels its log stream, **When** the Hub detects the cancellation, **Then** the per-client buffer and any background fan-out state are released within one second and no further entries are serialised for that client.

---

### User Story 5 - Default sensible filters for common debug sessions (Priority: P3)

A test author who has not thought about categories should get useful output by calling the stream RPC with empty fields. The Hub ships named filter presets ("admin-channel debug", "session lifecycle", "scripting wire trace") that the client can request by name instead of enumerating category IDs. This lowers the barrier to writing a first passing test.

**Why this priority**: Quality-of-life. Not blocking — authors can always pass a raw category list. Valuable for onboarding and for making example scripts readable.

**Independent Test**: A scripting client calls the log stream RPC with `preset = "session-lifecycle"` and observes that entries for session-manager, admin-channel, and engine-launcher categories at `Info` severity and above are delivered, while render-frame and preset-persistence categories are suppressed.

**Acceptance Scenarios**:

1. **Given** the Hub ships a `session-lifecycle` preset, **When** a client subscribes with that preset name, **Then** the delivered entries match the preset's documented category + severity set.
2. **Given** a client passes both a preset name and an explicit category list, **When** the Hub assembles the effective filter, **Then** the explicit list overrides the preset (not the other way around) so tests retain deterministic filter control.

---

### Edge Cases

- **Client subscribes before the feature it wants to observe fires**: the stream must be live for entries emitted after subscription returns to the client; entries emitted between RPC invocation and stream open are either delivered or documented as lost (not silently fabricated).
- **Category referenced by the client does not exist**: the RPC returns a clear validation error naming the bad category, rather than silently accepting an empty filter and delivering nothing.
- **Filter update arrives while an entry is mid-serialisation**: in-flight entries are delivered under the old filter; the new filter applies from the next entry. The client receives an acknowledgement event carrying the new effective filter so it can synchronise assertions.
- **Hub shuts down while a client is subscribed**: the stream completes cleanly with a documented terminal status; the client does not hang indefinitely waiting for more entries.
- **Very long log messages (e.g. a stack trace)**: messages longer than 8 KiB (UTF-8) are truncated at the Hub and emitted with a trailing ` …[truncated N bytes]` marker, where `N` is the number of bytes dropped. Silent truncation is not permitted; tests that care about tail content can assert the marker is absent.
- **Sensitive values in log payloads**: values such as absolute paths to the user's BAR install, lobby-generated tokens, or autohost UDP port numbers should be emitted as-is for local debug but must not be exposed to clients outside the Hub host. The Hub's existing gRPC surface is already loopback-only — the log stream inherits that boundary and does not need per-entry redaction.
- **Test that tails the stream during a crash**: if the Hub process dies, the stream gets a connection-level termination. Out of scope to replay logs from a previous Hub run — this stream is live-only.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Hub MUST expose a server-streaming log-stream RPC on its existing scripting gRPC service that any connected scripting client can subscribe to without launching a session first.
- **FR-002**: Each streamed log entry MUST carry a monotonically non-decreasing timestamp (Hub-local), a severity level, a source-category identifier, and a human-readable message.
- **FR-003**: Severity levels MUST include at minimum `Debug`, `Info`, `Warning`, and `Error`, and MUST be orderable so a client can specify a floor (e.g. "warn and above").
- **FR-004**: The set of log categories MUST cover every user-facing Hub subsystem observable today via `HubEvent`s or GUI affordances — at minimum: session lifecycle (`SessionManager`), admin channel (`AdminChannel`), scripting transport (`ScriptingHub` attach/detach/RPC dispatch), proxy install, viz/render (`HeadlessRenderer`, `HubStateStore` viz mutations), preset persistence, lobby configuration, settings changes, and the engine-speed/pause/force-end admin operations that the user's description singled out as the motivating test target.
- **FR-004a**: Each subsystem listed in FR-004 MUST receive new, purpose-built log-emission sites; routing the existing `HubEvent` bus is not sufficient. At minimum, `AdminChannel` MUST emit `Debug`-level entries for every outbound and inbound wire message (identifying the command/event kind and any payload fields), and `ScriptingHub` MUST emit `Debug`-level entries at RPC dispatch and completion (identifying the RPC name, client, and correlation ID).
- **FR-004b**: Out-of-scope for this feature (not required to emit log entries on the stream): engine-launcher `infolog.txt` capture, map-analysis, synthetic-data generation, and viz-rendering internal passes (scene builder, layer renderer, glyph renderer). These subsystems may emit log entries later in a follow-up feature.
- **FR-005**: A subscribing client MUST be able to specify, at subscription time, a whitelist of categories and a severity floor; the Hub MUST NOT deliver entries that fail either filter.
- **FR-005a**: When a client subscribes with an empty category whitelist and no explicit severity floor, the Hub MUST apply the default filter (all categories, `Info` severity floor). `Debug`-level entries MUST NOT reach a client that has not explicitly opted in via either a category-specific request or a preset that lowers the floor.
- **FR-006**: A subscribing client MUST be able to update its category whitelist and severity floor over the open stream without reconnecting; the Hub MUST acknowledge the filter change in-stream so the client can synchronise subsequent assertions.
- **FR-007**: A subscription request that names an unknown category MUST be rejected with a validation error identifying the bad category; the stream MUST NOT open silently with an empty effective filter.
- **FR-008**: Every Hub subsystem listed in FR-004 MUST emit at least one entry per user-observable action (launch, stop, pause, resume, speed change, force end, admin message, proxy install step, preset load/save/delete, scripting client attach/detach) so tests can assert coverage.
- **FR-009**: Log entries emitted while the Hub handles a specific unary gRPC RPC MUST carry a correlation identifier derived from the incoming request so the client can attribute entries to its call (per US3).
- **FR-009a**: The Hub MUST auto-assign a fresh correlation identifier to every unary RPC it accepts, so entries for two concurrent calls are always distinguishable even when neither client sets a correlation header. When a client does supply a correlation identifier via request metadata, the Hub MUST use that value instead, and MUST echo the effective (chosen) correlation identifier back in the unary RPC response so the client can assert on it.
- **FR-010**: Log entries about a specific session MUST carry the session identifier; log entries about a specific connected scripting client MUST carry that client's identifier.
- **FR-011**: The Hub MUST NOT block, slow, or drop user-facing work (RPC handling, event-bus publish, render loop) when a log-stream client is slow or unresponsive. Per-client buffering and drop handling happen on the serving side of the stream.
- **FR-012**: When the per-client buffer overflows, the Hub MUST drop the oldest entries (not the newest) and MUST report the number of entries dropped since the last successful delivery to that client, so the client can detect loss.
- **FR-012a**: Any log entry whose message text exceeds 8 KiB (UTF-8) MUST be truncated at the Hub before delivery, with a trailing ` …[truncated N bytes]` marker appended where `N` is the number of bytes removed. The truncation marker itself counts within the delivered message. A client MAY detect truncation by testing for the marker suffix.
- **FR-013**: The Hub MUST release per-client log-stream resources within one second of the client cancelling the stream or the underlying gRPC channel closing.
- **FR-014**: Existing `HubEvent.DiagnosticsLine` emissions MUST continue to reach local GUI consumers unchanged; the new stream supplements, not replaces, the event bus.
- **FR-015**: The new stream MUST co-exist with all feature-040 RPCs (including `StreamHubStateEvents` and `StreamRenderFrames`) without causing cross-stream starvation on a single client.
- **FR-015a**: The Hub MUST cap concurrent log-stream subscribers via a new `HubSettings.MaxLogStreamSubscribers` setting (default 8, configurable range 1–32), mirroring the feature-040 `MaxRenderFrameSubscribers` pattern. A subscription attempt that would exceed the configured cap MUST be rejected with a `ResourceExhausted`-equivalent status and a message identifying the cap; the caller MAY retry later.
- **FR-016**: The Hub MUST continue to work without any log-stream subscribers — production cost (CPU, allocation) of the feature when no one is listening MUST be negligible (subscriber count check only, no message formatting).
- **FR-017**: The wire contract of this feature MUST be additive — no existing scripting RPC, message, or field may be renumbered, removed, or repurposed. Existing clients compiled against the prior contract MUST continue to work unmodified.
- **FR-018**: The logging surface MUST ship with at least one working example script and at least one live test that assert the admin-channel debug scenario end-to-end (pause, resume, engine-speed change, force-end), so the motivating use case from the feature description is verifiably covered.

### Key Entities *(include if feature involves data)*

- **Log Entry**: one observable line of Hub activity. Key attributes: hub-local timestamp; severity (`Debug`/`Info`/`Warning`/`Error`); source category (enumerated); optional correlation ID (RPC); optional session ID; optional scripting-client ID; message text; dropped-since-last-delivery counter.
- **Log Category**: a named subsystem that emits log entries. Examples: `SessionManager`, `AdminChannel`, `ScriptingHub`, `ProxyInstall`, `HeadlessRenderer`, `HubStateStore`, `PresetPersistence`, `Lobby`, `Settings`. The set is finite, enumerated, and documented.
- **Log Filter**: the effective subscription-level policy for one client. Key attributes: category whitelist (empty = all); severity floor; optional preset name. Mutable over the lifetime of the subscription.
- **Log Preset**: a named, hub-shipped combination of categories + severity floor intended for a common debugging scenario. Examples: `session-lifecycle`, `admin-channel`, `scripting-wire`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A test author can attribute every Hub-emitted log line during a launch → pause → resume → stop cycle to either a specific client RPC or a specific background component — no entry is uncategorised.
- **SC-002**: When a scripting client subscribes with a narrow filter (one category, `Info` floor), the cumulative per-entry overhead on the Hub's action-handling path is indistinguishable from the no-subscriber baseline in ordinary interactive use (measured against the current Hub render/event workload).
- **SC-003**: A slow client consuming one log entry per second while the Hub emits one hundred per second does not delay, stall, or reorder any non-logging Hub operation over a ten-minute soak; the slow client still receives a drop counter covering the missed entries.
- **SC-004**: A live test that pauses, changes engine speed, and force-ends a session can — using only the log stream, without reading `infolog.txt` or the local GUI — assert (a) that each operation emitted at least one entry on the expected category, (b) the entries carry the operation's correlation ID, and (c) the admin-channel status transitions are reflected in the stream.
- **SC-005**: The feature ships with a runnable example script that a new contributor can invoke to see annotated log output for a full session-launch cycle within five minutes of checkout, without reading source code.
- **SC-006**: Existing scripting clients (feature 040 and feature 041 examples) continue to compile and run unmodified against the updated proto.

## Assumptions

- The scripting gRPC service is the correct delivery surface — no new transport or port is introduced. Feature 040 already wires a loopback-only gRPC server that all current scripting clients use.
- Per the existing architecture, the log stream is a live tail only; the Hub does not retain log history across process restarts, and clients that subscribe late do not see earlier entries. Tests that care about startup logs must subscribe early.
- Log volume under normal interactive use is dominated by a handful of categories (session lifecycle, admin channel, render frames). Verbose `Debug`-level output for a single category is acceptable at full fidelity; enabling `Debug` across all categories is a deliberate user choice and may produce more entries per second than a slow consumer can handle — drop handling (FR-012) covers this.
- The Hub's local GUI continues to render diagnostic output via the existing `HubEvent.DiagnosticsLine` path; this feature does not replace or reshape GUI diagnostics. Local operators still see everything they see today.
- Because the Hub's scripting endpoint is loopback-only, the stream does not need encryption or authentication beyond what the transport already provides, and log content does not need per-field redaction.
- The existing categories listed in FR-004 already expose every Hub user-observable action; this feature does not require adding new user actions, only new instrumentation inside the existing code paths.
- Hub auto-assignment of correlation IDs (FR-009a) makes correlation a zero-configuration property for tests; the client-override path exists only for cross-system tracing scenarios and is not required by any in-repo live test.
