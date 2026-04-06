namespace FSBar.Viz

open System
open System.IO
open FSBar.Client

module MapData =

    let private magic = [| byte 'F'; byte 'S'; byte 'M'; byte 'G' |]
    let private version = 1

    let private writeFloat32Array2D (bw: BinaryWriter) (arr: float32[,]) =
        let rows = Array2D.length1 arr
        let cols = Array2D.length2 arr
        for r in 0 .. rows - 1 do
            for c in 0 .. cols - 1 do
                bw.Write(arr.[r, c])

    let private writeInt32Array2D (bw: BinaryWriter) (arr: int[,]) =
        let rows = Array2D.length1 arr
        let cols = Array2D.length2 arr
        for r in 0 .. rows - 1 do
            for c in 0 .. cols - 1 do
                bw.Write(arr.[r, c])

    let private readFloat32Array2D (br: BinaryReader) (rows: int) (cols: int) =
        let arr = Array2D.zeroCreate<float32> rows cols
        for r in 0 .. rows - 1 do
            for c in 0 .. cols - 1 do
                arr.[r, c] <- br.ReadSingle()
        arr

    let private readInt32Array2D (br: BinaryReader) (rows: int) (cols: int) =
        let arr = Array2D.zeroCreate<int> rows cols
        for r in 0 .. rows - 1 do
            for c in 0 .. cols - 1 do
                arr.[r, c] <- br.ReadInt32()
        arr

    let save (path: string) (grid: MapGrid) (metalSpots: (float32 * float32 * float32 * float32) array) =
        use fs = File.Create(path)
        use bw = new BinaryWriter(fs)

        bw.Write(magic, 0, 4)
        bw.Write(version)
        bw.Write(grid.WidthHeightmap)
        bw.Write(grid.HeightHeightmap)

        writeFloat32Array2D bw grid.HeightMap
        writeFloat32Array2D bw grid.SlopeMap
        writeInt32Array2D bw grid.ResourceMap
        writeInt32Array2D bw grid.LosMap
        writeInt32Array2D bw grid.RadarMap

        bw.Write(metalSpots.Length)
        for (x, y, z, v) in metalSpots do
            bw.Write(x)
            bw.Write(y)
            bw.Write(z)
            bw.Write(v)

    let load (path: string) : MapGrid * (float32 * float32 * float32 * float32) array =
        use fs = File.OpenRead(path)
        use br = new BinaryReader(fs)

        let fileMagic = br.ReadBytes(4)
        if fileMagic <> magic then
            failwithf "Invalid FSMG file: expected magic bytes 'FSMG' but got '%s'" (Text.Encoding.ASCII.GetString(fileMagic))

        let fileVersion = br.ReadInt32()
        if fileVersion <> version then
            failwithf "Unsupported FSMG version: expected %d but got %d" version fileVersion

        let w = br.ReadInt32()
        let h = br.ReadInt32()

        if w <= 0 || h <= 0 then
            failwithf "Invalid map dimensions: %dx%d" w h

        let heightMap =
            try readFloat32Array2D br (h + 1) (w + 1)
            with :? EndOfStreamException -> failwithf "Truncated file: could not read HeightMap (%dx%d)" (h + 1) (w + 1)

        let slopeMap =
            try readFloat32Array2D br (h / 2) (w / 2)
            with :? EndOfStreamException -> failwithf "Truncated file: could not read SlopeMap (%dx%d)" (h / 2) (w / 2)

        let resourceMap =
            try readInt32Array2D br h w
            with :? EndOfStreamException -> failwithf "Truncated file: could not read ResourceMap (%dx%d)" h w

        let losMap =
            try readInt32Array2D br h w
            with :? EndOfStreamException -> failwithf "Truncated file: could not read LosMap (%dx%d)" h w

        let radarMap =
            try readInt32Array2D br h w
            with :? EndOfStreamException -> failwithf "Truncated file: could not read RadarMap (%dx%d)" h w

        let spotCount =
            try br.ReadInt32()
            with :? EndOfStreamException -> failwith "Truncated file: could not read metal spot count"

        let metalSpots =
            Array.init spotCount (fun _ ->
                try
                    let x = br.ReadSingle()
                    let y = br.ReadSingle()
                    let z = br.ReadSingle()
                    let v = br.ReadSingle()
                    (x, y, z, v)
                with :? EndOfStreamException ->
                    failwith "Truncated file: could not read metal spots")

        let grid: MapGrid =
            { WidthElmos = w * 8
              HeightElmos = h * 8
              WidthHeightmap = w
              HeightHeightmap = h
              HeightMap = heightMap
              SlopeMap = slopeMap
              ResourceMap = resourceMap
              LosMap = losMap
              RadarMap = radarMap }

        (grid, metalSpots)
