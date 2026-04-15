namespace FSBar.Viz

open SkiaSharp

module VizDefaults =

    let defaultViewState =
        { Scale = 1.0f
          OriginX = 0.0f
          OriginY = 0.0f
          WindowWidth = 1024
          WindowHeight = 640
          AutoFit = true }

    let defaultEconomy =
        { Current = 0.0f
          Income = 0.0f
          Usage = 0.0f
          Storage = 0.0f }

    let defaultConfig =
        { BaseLayer = LayerKind.BaseTerrain
          ActiveOverlays = Set.ofList [ OverlayKind.MetalSpots ]
          ColorSchemes = Map.empty
          UnitMarkerSize = 6.0f
          OverlayOpacity = 0.8f
          ShowGridLines = false
          GridLineSpacing = 16
          BackgroundColor = SKColors.Black
          LabelColor = SKColors.White
          UseGlyphRenderer = true
          GlyphStyle = UnitGlyphPalettes.defaults }
