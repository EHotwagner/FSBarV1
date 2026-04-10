namespace FSBar.SyntheticData

open FSBar.Client

module Validation =

    let validate (scene: Scene) : string list =
        let errors = ResizeArray<string>()

        // Check frame count
        if scene.Frames.Length <> 300 then
            errors.Add $"Expected 300 frames, got {scene.Frames.Length}"
        if scene.GameFrames.Length <> 300 then
            errors.Add $"Expected 300 game frames, got {scene.GameFrames.Length}"

        // Check frame numbers
        for i in 0 .. scene.Frames.Length - 1 do
            let expected = uint32 (i + 1)
            if scene.Frames.[i].FrameNumber <> expected then
                errors.Add $"Frame {i}: expected FrameNumber {expected}, got {scene.Frames.[i].FrameNumber}"
            if i < scene.GameFrames.Length && scene.GameFrames.[i].FrameNumber <> expected then
                errors.Add $"GameFrame {i}: expected FrameNumber {expected}, got {scene.GameFrames.[i].FrameNumber}"

        // Check Init event in frame 1
        if scene.GameFrames.Length > 0 then
            let hasInit =
                scene.GameFrames.[0].Events
                |> List.exists (function GameEvent.Init _ -> true | _ -> false)
            if not hasInit then
                errors.Add "Frame 1 missing Init event"

        // Check Update event in every frame
        for i in 0 .. scene.GameFrames.Length - 1 do
            let hasUpdate =
                scene.GameFrames.[i].Events
                |> List.exists (function GameEvent.Update _ -> true | _ -> false)
            if not hasUpdate then
                errors.Add $"Frame {i + 1} missing Update event"

        // Check position bounds
        for i in 0 .. scene.Frames.Length - 1 do
            let state = scene.Frames.[i]
            for kv in state.Units do
                let u = kv.Value
                let (x, y, z) = u.Position
                if x < 0.0f || x > scene.MapWidth then
                    errors.Add $"Frame {i + 1}, Unit {u.UnitId}: X={x} out of bounds [0, {scene.MapWidth}]"
                if z < 0.0f || z > scene.MapHeight then
                    errors.Add $"Frame {i + 1}, Unit {u.UnitId}: Z={z} out of bounds [0, {scene.MapHeight}]"
                if y < 0.0f || y > 400.0f then
                    errors.Add $"Frame {i + 1}, Unit {u.UnitId}: Y={y} out of bounds [0, 400]"
            for kv in state.Enemies do
                let e = kv.Value
                let (x, y, z) = e.Position
                if x < 0.0f || x > scene.MapWidth then
                    errors.Add $"Frame {i + 1}, Enemy {e.EnemyId}: X={x} out of bounds [0, {scene.MapWidth}]"
                if z < 0.0f || z > scene.MapHeight then
                    errors.Add $"Frame {i + 1}, Enemy {e.EnemyId}: Z={z} out of bounds [0, {scene.MapHeight}]"
                if y < 0.0f || y > 400.0f then
                    errors.Add $"Frame {i + 1}, Enemy {e.EnemyId}: Y={y} out of bounds [0, 400]"

        // Check economy consistency
        for i in 0 .. scene.Frames.Length - 1 do
            let state = scene.Frames.[i]
            for (name, econ) in [("Metal", state.Metal); ("Energy", state.Energy)] do
                if econ.Current < 0.0f then
                    errors.Add $"Frame {i + 1}: {name}.Current={econ.Current} < 0"
                if econ.Current > econ.Storage then
                    errors.Add $"Frame {i + 1}: {name}.Current={econ.Current} > Storage={econ.Storage}"
                if econ.Income < 0.0f then
                    errors.Add $"Frame {i + 1}: {name}.Income={econ.Income} < 0"
                if econ.Usage < 0.0f then
                    errors.Add $"Frame {i + 1}: {name}.Usage={econ.Usage} < 0"

        // Check all DefIds exist in UnitDefCache
        for i in 0 .. scene.Frames.Length - 1 do
            let state = scene.Frames.[i]
            for kv in state.Units do
                let u = kv.Value
                if (UnitDefCache.tryFindById scene.UnitDefs u.DefId).IsNone then
                    errors.Add $"Frame {i + 1}, Unit {u.UnitId}: DefId {u.DefId} not in UnitDefCache"
            for kv in state.Enemies do
                let e = kv.Value
                match e.DefId with
                | Some d when (UnitDefCache.tryFindById scene.UnitDefs d).IsNone ->
                    errors.Add $"Frame {i + 1}, Enemy {e.EnemyId}: DefId {d} not in UnitDefCache"
                | _ -> ()

        errors |> Seq.toList

    let validateContinuity (scene: Scene) : string list =
        let errors = ResizeArray<string>()

        for i in 1 .. scene.Frames.Length - 1 do
            let prev = scene.Frames.[i - 1]
            let curr = scene.Frames.[i]
            let frameNum = i + 1

            // Check unit position deltas
            for kv in curr.Units do
                let u = kv.Value
                match Map.tryFind u.UnitId prev.Units with
                | Some prevU ->
                    let (px, _, pz) = prevU.Position
                    let (cx, _, cz) = u.Position
                    let dx = abs (cx - px)
                    let dz = abs (cz - pz)
                    if dx > 6.0f then
                        errors.Add $"Frame {frameNum}, Unit {u.UnitId}: X delta={dx} > 6.0"
                    if dz > 6.0f then
                        errors.Add $"Frame {frameNum}, Unit {u.UnitId}: Z delta={dz} > 6.0"
                | None ->
                    // New unit — must have UnitCreated event
                    let hasCreated =
                        scene.GameFrames.[i].Events
                        |> List.exists (function
                            | GameEvent.UnitCreated (uid, _) -> uid = u.UnitId
                            | _ -> false)
                    if not hasCreated then
                        errors.Add $"Frame {frameNum}, Unit {u.UnitId}: appeared without UnitCreated event"

            // Check units that disappeared
            for kv in prev.Units do
                let uid = kv.Key
                if not (Map.containsKey uid curr.Units) then
                    let hasDestroyed =
                        scene.GameFrames.[i].Events
                        |> List.exists (function
                            | GameEvent.UnitDestroyed (did, _) -> did = uid
                            | _ -> false)
                    if not hasDestroyed then
                        errors.Add $"Frame {frameNum}, Unit {uid}: disappeared without UnitDestroyed event"

            // Check economy continuity (relaxed: just check no wild jumps)
            for (name, prevE, currE) in [("Metal", prev.Metal, curr.Metal); ("Energy", prev.Energy, curr.Energy)] do
                let maxDelta = (prevE.Income + prevE.Usage + 1.0f) / 30.0f * 2.0f // generous tolerance
                let delta = abs (currE.Current - prevE.Current)
                if delta > maxDelta && delta > 1.0f then
                    errors.Add $"Frame {frameNum}: {name}.Current delta={delta} exceeds tolerance (income={prevE.Income}, usage={prevE.Usage})"

        errors |> Seq.toList
