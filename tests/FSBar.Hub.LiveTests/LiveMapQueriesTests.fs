namespace FSBar.Hub.LiveTests

// Feature 046 — US3 live tests. Exercises the map-query RPCs and
// GetUnitDefExtended against a real BAR engine session. All map data
// is read from the SessionManager's warmup cache (RunningSession
// MapGrid + MetalSpots) — no new engine round-trips.

open System
open System.IO
open Xunit
open FSBar.Client
open FSBar.Hub
open Fsbar.Hub.Scripting.V1

[<Collection("HubSession")>]
type LiveMapQueriesTests() =

    /// US3 AS1+AS2 — on a live Avalanche session, GetMapInfo returns
    /// non-zero width×height and a data_dir; ListMetalSpots returns ≥1
    /// spot with metal_value > 0.
    [<SkippableFact>]
    [<Trait("Category", "Feature046")>]
    member _.``US3 — GetMapInfo + ListMetalSpots populated on Avalanche``() =
        task {
            let svc, sm, bus =
                StateStreamFixtures.launchAndWait "Avalanche 3.4" "avalanche_3.4"
            try
                let mi = (svc.GetMapInfo GetMapInfoRequest.empty HeadlessOrchestrationFixtures.nullContext).Result
                Assert.True(mi.Width > 0, sprintf "map width not populated: %A" mi)
                Assert.True(mi.Height > 0, sprintf "map height not populated: %A" mi)
                Assert.False(String.IsNullOrEmpty(mi.DataDir), "data_dir empty")
                Assert.Equal("Avalanche 3.4", mi.MapName)

                let ms = (svc.ListMetalSpots ListMetalSpotsRequest.empty HeadlessOrchestrationFixtures.nullContext).Result
                Assert.NotEmpty(ms.Spots)
                for s in ms.Spots do
                    Assert.True(s.MetalValue > 0.0f,
                        sprintf "metal spot %A has non-positive value" s)
            finally
                StateStreamFixtures.stopSession svc sm
                try (svc :> IDisposable).Dispose() with _ -> ()
                try (sm :> IDisposable).Dispose() with _ -> ()
                try (bus :> IDisposable).Dispose() with _ -> ()
        }

    /// US3 — all five grid RPCs return non-empty grids sized
    /// (width × height) matching their declared resolution. Spot-checks
    /// that the flatten is not empty when a grid is present.
    [<SkippableFact>]
    [<Trait("Category", "Feature046")>]
    member _.``US3 — heightmap + slope + los + radar + resource grids populated on Avalanche``() =
        task {
            let svc, sm, bus =
                StateStreamFixtures.launchAndWait "Avalanche 3.4" "avalanche_3.4"
            try
                let hm = (svc.GetHeightmap GetHeightmapRequest.empty HeadlessOrchestrationFixtures.nullContext).Result
                Assert.Equal(hm.Width * hm.Height, List.length hm.Heights)
                Assert.True(hm.Width > 0 && hm.Height > 0)

                let corners = (svc.GetCornersHeightmap GetCornersHeightmapRequest.empty HeadlessOrchestrationFixtures.nullContext).Result
                Assert.Equal(corners.Width * corners.Height, List.length corners.Heights)
                // Corners are (w+1)×(h+1) of the heightmap.
                Assert.Equal(hm.Width + 1, corners.Width)
                Assert.Equal(hm.Height + 1, corners.Height)

                let slope = (svc.GetSlopeMap GetSlopeMapRequest.empty HeadlessOrchestrationFixtures.nullContext).Result
                Assert.Equal(slope.Width * slope.Height, List.length slope.Slopes)
                Assert.True(slope.Width > 0 && slope.Height > 0)

                let los = (svc.GetLosMap GetLosMapRequest.empty HeadlessOrchestrationFixtures.nullContext).Result
                Assert.Equal(los.Width * los.Height, List.length los.Values)

                let radar = (svc.GetRadarMap GetRadarMapRequest.empty HeadlessOrchestrationFixtures.nullContext).Result
                Assert.Equal(radar.Width * radar.Height, List.length radar.Values)

                let res = (svc.GetResourceMap GetResourceMapRequest.empty HeadlessOrchestrationFixtures.nullContext).Result
                Assert.Equal(res.Width * res.Height, List.length res.Values)
            finally
                StateStreamFixtures.stopSession svc sm
                try (svc :> IDisposable).Dispose() with _ -> ()
                try (sm :> IDisposable).Dispose() with _ -> ()
                try (bus :> IDisposable).Dispose() with _ -> ()
        }

    /// US3 AS3 — GetUnitDefExtended for a commander (armcom) returns
    /// the full planning surface: non-empty build_options, cost,
    /// build_time, sight_range_elmo, footprint.
    [<SkippableFact>]
    [<Trait("Category", "Feature046")>]
    member _.``US3 — GetUnitDefExtended for armcom returns full planning surface``() =
        task {
            let svc, sm, bus =
                StateStreamFixtures.launchAndWait "Avalanche 3.4" "avalanche_3.4"
            try
                // Wait briefly so the live UnitDefCache has a chance to
                // load (it populates on the BarClient's pump side).
                do! Async.Sleep(2000)

                let req : GetUnitDefRequest = { Selector = GetUnitDefRequest.SelectorCase.InternalName "armcom" }
                let resp = (svc.GetUnitDefExtended req HeadlessOrchestrationFixtures.nullContext).Result
                Assert.True(resp.UnitDef.IsSome, "armcom not found in encyclopedia")
                let info = resp.UnitDef.Value
                Assert.Equal("armcom", info.InternalName)
                Assert.True(info.Cost.IsSome, "cost not set")
                let cost = info.Cost.Value
                Assert.True(cost.Metal > 0.0f, "metal cost not positive")
                Assert.True(cost.Energy > 0.0f, "energy cost not positive")
                Assert.True(info.BuildTime > 0.0f, "build_time not positive")
                Assert.True(info.SightRangeElmo > 0.0f, "sight_range_elmo not positive")
                Assert.True(info.FootprintX > 0, "footprint_x not positive")
                Assert.True(info.FootprintZ > 0, "footprint_z not positive")
                Assert.True(info.MaxHealth > 0, "max_health not positive")
                Assert.NotEmpty(info.BuildOptions)
            finally
                StateStreamFixtures.stopSession svc sm
                try (svc :> IDisposable).Dispose() with _ -> ()
                try (sm :> IDisposable).Dispose() with _ -> ()
                try (bus :> IDisposable).Dispose() with _ -> ()
        }
