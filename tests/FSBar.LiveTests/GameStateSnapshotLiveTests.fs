namespace FSBar.LiveTests

open Xunit
open FSBar.Client

/// Live-engine integration tests for the batched GameState snapshot
/// (spec 045 / HighBarV2 032). Exercises `Callbacks.getGameStateSnapshot`
/// against a real spring-headless + HighBarV2 proxy (>= 0.1.5).
///
/// Note: the connect-time preflight snapshot in `BarClient.Start` means
/// that if this test class's EngineFixture initialized at all, the proxy
/// already advertises callback id 15 — so the SC-005 "hard error on
/// pre-0.1.5 proxy" path is implicitly exercised by the fixture itself
/// when the proxy is too old. There is no forced pre-0.1.5 negative
/// test here because no legacy binary is archived locally.
[<Collection("Engine")>]
type GameStateSnapshotLiveTests(engine: EngineFixture) =

    [<Fact>]
    [<Trait("Category", "Snapshot")>]
    member _.``snapshot_returns_well_formed_result``() =
        let snap = Callbacks.getGameStateSnapshot engine.Client.Stream
        Assert.True(snap.Frame >= 0, $"Frame should be non-negative, got {snap.Frame}")
        // Friendlies list is never null (F# lists can't be). Early in the
        // game there is at least the commander so >= 1 is a reasonable
        // lower bound once warmup has completed.
        Assert.True(snap.Friendlies.Length >= 1,
            $"Expected at least 1 friendly (commander), got {snap.Friendlies.Length}")
        // Economy is always present on success.
        Assert.True(snap.Economy.MetalStorage > 0.0f,
            $"MetalStorage should be > 0, got {snap.Economy.MetalStorage}")
        Assert.True(snap.Economy.EnergyStorage > 0.0f,
            $"EnergyStorage should be > 0, got {snap.Economy.EnergyStorage}")

    [<Fact>]
    [<Trait("Category", "Snapshot")>]
    member _.``snapshot_friendly_matches_per_unit_ground_truth``() =
        // FR-003 observational equivalence: snapshot position/health
        // match the engine's per-unit callbacks (independent harness
        // path, never on the client's refresh path).
        let snap = Callbacks.getGameStateSnapshot engine.Client.Stream
        Assert.True(snap.Friendlies.Length >= 1)
        let f = snap.Friendlies.[0]
        let groundPos = Callbacks.getUnitPos engine.Client.Stream f.UnitId
        let groundHp = Callbacks.getUnitHealth engine.Client.Stream f.UnitId
        let (sx, sy, sz) = f.Position
        let (gx, gy, gz) = groundPos
        let eps = 1e-2f
        Assert.InRange(sx - gx, -eps, eps)
        Assert.InRange(sy - gy, -eps, eps)
        Assert.InRange(sz - gz, -eps, eps)
        // Health can move between the two calls on a live tick; allow
        // a generous tolerance.
        Assert.InRange(f.Health - groundHp, -10.0f, 10.0f)
        Assert.True(f.UnitDefId > 0, $"UnitDefId should be > 0, got {f.UnitDefId}")

    [<Fact>]
    [<Trait("Category", "Snapshot")>]
    member _.``snapshot_no_duplicate_ids_across_LOS_and_radar``() =
        let snap = Callbacks.getGameStateSnapshot engine.Client.Stream
        let losIds = snap.LosEnemies |> List.map (fun e -> e.UnitId) |> Set.ofList
        let radarIds = snap.RadarOnlyEnemies |> List.map (fun e -> e.UnitId) |> Set.ofList
        let overlap = Set.intersect losIds radarIds
        Assert.True(overlap.IsEmpty, $"LOS and radar-only enemy ids must be disjoint, overlap={overlap}")

    [<Fact>]
    [<Trait("Category", "Snapshot")>]
    member _.``applySnapshot_updates_GameState_over_multiple_ticks``() =
        // SC-002 exercised implicitly: GameEvent.Update now runs through
        // Callbacks.getGameStateSnapshot → applySnapshot. Driving 30
        // frames and observing Units+economy stay populated confirms
        // the batched path is the live refresh.
        let frames = System.Collections.Generic.List<_>()
        engine.Client.WaitFrames 30 frames.Add
        let state = engine.Client.GameState
        Assert.True(state.Units.Count >= 1,
            $"After 30 frames, expected Units to be populated, got {state.Units.Count}")
        Assert.True(state.Metal.Storage > 0.0f,
            $"After 30 frames, Metal.Storage should be > 0, got {state.Metal.Storage}")
        Assert.True(state.Energy.Storage > 0.0f,
            $"After 30 frames, Energy.Storage should be > 0, got {state.Energy.Storage}")

    [<Fact>]
    [<Trait("Category", "Snapshot")>]
    member _.``snapshot_radar_only_entries_carry_no_health_by_construction``() =
        // FR-004 structural: the RadarOnlyEnemySnapshot record has no
        // Health field — cannot be violated at runtime. We still assert
        // via reflection as a regression guard in case the mapper ever
        // changes shape.
        let fields =
            typeof<RadarOnlyEnemySnapshot>.GetProperties()
            |> Array.map (fun p -> p.Name)
            |> Set.ofArray
        Assert.DoesNotContain("Health", fields)
