namespace FSBar.Viz

open System.IO
open FSBar.Client

module MapData =

    let private magic = "FSMG"B
    let private version = 1

    let save (path: string) (grid: MapGrid) (metalSpots: (float32 * float32 * float32 * float32) array) : unit =
        use stream = new FileStream(path, FileMode.Create, FileAccess.Write)
        use writer = new BinaryWriter(stream)

        // Magic + version
        writer.Write(magic)
        writer.Write(version)

        // Dimensions
        writer.Write(grid.WidthElmos)
        writer.Write(grid.HeightElmos)
        writer.Write(grid.WidthHeightmap)
        writer.Write(grid.HeightHeightmap)

        let w = grid.WidthHeightmap
        let h = grid.HeightHeightmap

        // HeightMap: (w+1) * (h+1)
        for z = 0 to h do
            for x = 0 to w do
                writer.Write(grid.HeightMap.[x, z])

        // SlopeMap: (w/2) * (h/2)
        let sw = w / 2
        let sh = h / 2
        for z = 0 to sh - 1 do
            for x = 0 to sw - 1 do
                writer.Write(grid.SlopeMap.[x, z])

        // ResourceMap: w * h
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                writer.Write(grid.ResourceMap.[x, z])

        // LosMap: w * h
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                writer.Write(grid.LosMap.[x, z])

        // RadarMap: w * h
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                writer.Write(grid.RadarMap.[x, z])

        // MetalSpots
        writer.Write(metalSpots.Length)
        for (x, y, z, richness) in metalSpots do
            writer.Write(x)
            writer.Write(y)
            writer.Write(z)
            writer.Write(richness)

    let load (path: string) : MapGrid * (float32 * float32 * float32 * float32) array =
        use stream = new FileStream(path, FileMode.Open, FileAccess.Read)
        use reader = new BinaryReader(stream)

        // Validate magic
        let magicBytes = reader.ReadBytes(4)
        if magicBytes <> magic then
            failwith $"MapData.load: invalid magic bytes — expected FSMG, got %A{magicBytes}"

        // Validate version
        let ver = reader.ReadInt32()
        if ver <> 1 then
            failwith $"MapData.load: unsupported version %d{ver}, expected 1"

        // Dimensions
        let widthElmos = reader.ReadInt32()
        let heightElmos = reader.ReadInt32()
        let w = reader.ReadInt32()
        let h = reader.ReadInt32()

        // HeightMap: (w+1) * (h+1)
        let heightMap =
            Array2D.init (w + 1) (h + 1) (fun _ _ -> reader.ReadSingle())

        // SlopeMap: (w/2) * (h/2)
        let sw = w / 2
        let sh = h / 2
        let slopeMap =
            Array2D.init sw sh (fun _ _ -> reader.ReadSingle())

        // ResourceMap: w * h
        let resourceMap =
            Array2D.init w h (fun _ _ -> reader.ReadInt32())

        // LosMap: w * h
        let losMap =
            Array2D.init w h (fun _ _ -> reader.ReadInt32())

        // RadarMap: w * h
        let radarMap =
            Array2D.init w h (fun _ _ -> reader.ReadInt32())

        // MetalSpots
        let spotCount = reader.ReadInt32()
        let metalSpots =
            Array.init spotCount (fun _ ->
                let x = reader.ReadSingle()
                let y = reader.ReadSingle()
                let z = reader.ReadSingle()
                let richness = reader.ReadSingle()
                (x, y, z, richness))

        let grid: MapGrid =
            { WidthElmos = widthElmos
              HeightElmos = heightElmos
              WidthHeightmap = w
              HeightHeightmap = h
              HeightMap = heightMap
              SlopeMap = slopeMap
              ResourceMap = resourceMap
              LosMap = losMap
              RadarMap = radarMap }

        (grid, metalSpots)
