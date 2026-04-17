namespace FSBar.Hub

open FSBar.Client

/// Lobby builder types + validation + projection onto the existing
/// FSBar.Client.EngineConfig / ScriptGenerator path.
///
/// The hub lets users configure arbitrary team counts, mixed AI / human
/// / spectator seats, and game modes (Skirmish / FFA / Team). The data
/// model here is richer than what ScriptGenerator currently emits — the
/// existing generator produces a fixed two-team AI-vs-AI skirmish
/// script. `toEngineConfig` collapses a validated lobby down to that
/// shape, and enforces any excess structure via a Phase-3 restriction
/// documented on the `toEngineConfig` signature below. US4 / US-Team /
/// US-FFA expansions widen the adapter as the generator learns to emit
/// more elaborate scripts.
module LobbyConfig =

    /// Skirmish = a single match, two or more allied groupings.
    /// FFA = every team is exactly one seat; requires >= 3 teams.
    /// Team = explicit team-vs-team scoring.
    type GameMode =
        | Skirmish
        | FFA
        | Team

    /// A seat is either an AI entry (with a configurable options map) or
    /// a human entry (just a player name — v1 does not drive real human
    /// connections, but the data model leaves room).
    type SeatKind =
        | AiSeat of aiName: string * options: Map<string, string>
        | HumanSeat of playerName: string

    /// One participant in a `Team`. Handicap ∈ [-100, 100].
    type Seat = {
        Kind: SeatKind
        /// BAR faction label (e.g. "Armada" | "Cortex" | "Legion" ...).
        Side: string
        /// Per-seat handicap; applied by BAR engine. 0 = no handicap.
        Handicap: int
    }

    /// One allied grouping. `AllyTeamId` groups teams that share victory
    /// conditions — two `Team`s with the same `AllyTeamId` are allies.
    type Team = {
        Seats: Seat list
        AllyTeamId: int
    }

    /// A spectator seat — watches but does not play.
    type Spectator = {
        PlayerName: string
    }

    /// The full lobby shape captured in `HubSettings.LastLobby` (Phase 3
    /// will extend `HubSettings` with this field) and handed to
    /// `SessionManager.Launch`.
    type LobbyConfig = {
        /// BAR map name as it appears under `<dataDir>/maps/<name>.sd7`.
        MapName: string
        Mode: GameMode
        /// Engine time multiplier. UI clamps to [0.1f, 100.0f]; the
        /// validator rejects anything outside.
        EngineSpeed: float32
        /// US4 toggle — also start the graphical `spring` binary
        /// alongside the headless engine.
        LaunchGraphicalViewer: bool
        /// All teams in launch order. `Teams.[i].AllyTeamId` controls
        /// victory grouping.
        Teams: Team list
        Spectators: Spectator list
    }

    /// Validation failures. `validate` returns every failure it finds
    /// so the UI can render a single coherent error list per AS-1.3.
    type LobbyError =
        | NotEnoughTeams
        | TeamHasNoActiveSeats of teamIndex: int
        | HandicapOutOfRange of teamIndex: int * seatIndex: int * value: int
        | MapNotInstalled of mapName: string
        | UnknownAi of teamIndex: int * seatIndex: int * aiName: string
        | FfaTeamHasMultipleSeats of teamIndex: int
        | FfaTooFewTeams
        | EngineSpeedOutOfRange of value: float32
        | GraphicalBinaryMissing of engineVersion: string
        /// Phase-3 adapter restriction — `toEngineConfig` currently
        /// demands a 2-team, one-AI-seat-per-team layout because
        /// `FSBar.Client.ScriptGenerator` does not yet emit arbitrary
        /// lobbies. Wider layouts will land in future phases.
        | AdapterUnsupportedShape of reason: string

    /// Factory value: map empty (user must pick one), Skirmish mode, 1x
    /// speed, no graphical viewer, two teams each with one AI seat
    /// (team 0 = HighBarV2 / Armada, team 1 = BARb / Cortex), no
    /// spectators. The validator still rejects this value because
    /// `MapName = ""` fails the map-installed check.
    val defaults: LobbyConfig

    /// Returns the full list of validation failures. Errors are
    /// grouped roughly by increasing specificity — structural failures
    /// first, then per-seat, then mode-specific rules.
    val validate:
        install: BarInstall.BarInstall ->
        config: LobbyConfig ->
            Result<LobbyConfig, LobbyError list>

    /// Collapses a validated lobby to an `FSBar.Client.EngineConfig`
    /// suitable for the existing script generator.
    ///
    /// **Phase-3 restriction**: the caller must have passed `validate`
    /// first *and* the lobby must be a simple 2-team, one-AI-seat-per-
    /// team skirmish. Human seats, spectators, >2 teams, or mode != Skirmish
    /// yield an `AdapterUnsupportedShape` error. When the generator learns
    /// richer scripts this function widens.
    val toEngineConfig:
        install: BarInstall.BarInstall ->
        config: LobbyConfig ->
            Result<EngineConfig, LobbyError>

    /// Human-readable rendering of a `LobbyError`. Used by the Setup
    /// tab's error list and by diagnostics logs.
    val formatError: LobbyError -> string
