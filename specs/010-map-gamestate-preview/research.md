# Research: Map & GameState Preview via SkiaViewer

**Date**: 2026-04-06

## R1: Map Data Serialization Format

**Decision**: Custom binary format using BinaryWriter/BinaryReader with magic bytes and version header.

**Rationale**: MapGrid contains large 2D float32/int arrays (up to 512x512). Binary serialization is the most efficient approach — a 512x512 map's heightmap alone is ~1MB. Binary format meets the <1s save/load requirement without any additional dependencies. The project already has no JSON or MessagePack dependencies in FSBar.Viz, and adding one just for serialization would violate the "minimize dependencies" constitution constraint.

**Alternatives considered**:
- Protocol Buffers (FsGrpc): Project uses protobuf for engine communication, but the proto definitions are in FSBar.Proto/HighBar — adding map serialization protos would conflate engine protocol with persistence. Also, protobuf has overhead for large flat arrays.
- System.Text.Json: Poor performance for large numeric arrays; JSON is 3-5x larger than binary for float data.
- MessagePack: Would require a new dependency; overkill for internal-only persistence.

## R2: 2D Array Serialization Strategy

**Decision**: Flatten 2D arrays to 1D row-major order for serialization. On load, reshape using known dimensions from the header.

**Rationale**: F# `Array2D` doesn't have built-in binary serialization. Flattening to row-major is the standard approach and allows efficient block writes via `BinaryWriter`. The dimensions are stored in the header, so reshaping on load is deterministic.

**Alternatives considered**:
- Serialize with dimensions per array: Redundant since all arrays derive dimensions from WidthHeightmap/HeightHeightmap.
- Use jagged arrays: Would change the MapGrid type contract; not worth it.

## R3: Preview Session Architecture

**Decision**: PreviewSession creates its own SkiaViewer instance using ViewerConfig, with internal VizConfig and ViewState management. Reuses SceneBuilder.drawFrame for rendering.

**Rationale**: The existing GameViz module is tightly coupled to BarClient (live engine). Creating a separate PreviewSession avoids modifying GameViz and keeps the live/preview paths cleanly separated. Both use the same SceneBuilder.drawFrame, so rendering is identical.

**Alternatives considered**:
- Modify GameViz to accept mock data: Would add complexity to the already stateful GameViz module and risk breaking live mode.
- Subclass/extend GameViz: F# doesn't have class inheritance for modules; composition is the F# way.

## R4: Playback Timing Strategy

**Decision**: The viewer always renders at 60fps. For animated playback, a game-state index advances at the specified game-fps rate. The render callback reads the current snapshot from the sequence based on elapsed time.

**Rationale**: Decoupling render rate from game state rate ensures smooth visual output regardless of game speed. A Stopwatch tracks elapsed time; the current frame index = (elapsed * gameFps). This allows game-fps values from 1 (slow-motion) to 60 (real-time) without affecting render smoothness.

**Alternatives considered**:
- Advance one snapshot per render frame: Only works if game-fps = render-fps = 60. Doesn't support slow-motion or fast-forward.
- Timer-based advancement: Adds threading complexity. Stopwatch-based calculation in the render callback is simpler and lock-free.

## R5: MockSnapshot Builder Pattern

**Decision**: Use pipeline-style builder functions (`|>`) operating on GameSnapshot records. No mutable builder class.

**Rationale**: F# idiomatic pattern. Records are immutable with `{ x with ... }` syntax. Pipeline composition reads naturally: `emptySnapshot grid |> withFriendlyAt (100f, 0f, 100f) |> withEconomy 500f 10f 8f 1000f`. No new types needed — builders operate on the existing GameSnapshot type.

**Alternatives considered**:
- Mutable builder class: Non-idiomatic F#; adds unnecessary complexity.
- Computation expression: Over-engineered for simple record construction.
