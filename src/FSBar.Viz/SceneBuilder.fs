namespace FSBar.Viz

open SkiaSharp

module SceneBuilder =
    let private getScheme (config: VizConfig) (layer: LayerKind) =
        match Map.tryFind layer config.ColorSchemes with
        | Some s -> s
        | None -> ColorMaps.colorSchemeFor layer

    let drawFrame (canvas: SKCanvas) (snapshot: GameSnapshot) (config: VizConfig) (viewState: ViewState) =
        // Draw base layer
        if snapshot.MapGrid.WidthHeightmap <= 0 then () else
        let scheme = getScheme config config.BaseLayer
        let bmp = LayerRenderer.renderLayer snapshot.MapGrid config.BaseLayer scheme
        if obj.ReferenceEquals(bmp, null) || bmp.Width <= 0 then () else
        let destRect =
            SKRect(
                viewState.OriginX,
                viewState.OriginY,
                viewState.OriginX + float32 bmp.Width * viewState.Scale,
                viewState.OriginY + float32 bmp.Height * viewState.Scale
            )
        canvas.DrawBitmap(bmp, destRect)

        // Draw grid lines overlay
        if config.ShowGridLines && config.ActiveOverlays.Contains OverlayKind.Grid then
            use gridPaint = new SKPaint(IsAntialias = true, Color = SKColor(255uy, 255uy, 255uy, 40uy), StrokeWidth = 1.0f)
            let spacing = float32 config.GridLineSpacing * viewState.Scale
            let mapW = float32 bmp.Width * viewState.Scale
            let mapH = float32 bmp.Height * viewState.Scale
            let mutable x = viewState.OriginX
            while x <= viewState.OriginX + mapW do
                canvas.DrawLine(x, viewState.OriginY, x, viewState.OriginY + mapH, gridPaint)
                x <- x + spacing
            let mutable y = viewState.OriginY
            while y <= viewState.OriginY + mapH do
                canvas.DrawLine(viewState.OriginX, y, viewState.OriginX + mapW, y, gridPaint)
                y <- y + spacing

        // Draw unit overlay
        if config.ActiveOverlays.Contains OverlayKind.Units then
            use friendlyPaint = new SKPaint(IsAntialias = true, Color = SKColor(255uy, 0uy, 255uy, byte (config.OverlayOpacity * 255.0f)), Style = SKPaintStyle.Fill)
            use enemyPaint = new SKPaint(IsAntialias = true, Color = SKColor(255uy, 0uy, 0uy, byte (config.OverlayOpacity * 255.0f)), Style = SKPaintStyle.Fill)
            for kvp in snapshot.Units do
                let u = kvp.Value
                let gx = u.PositionX / 8.0f
                let gz = u.PositionZ / 8.0f
                let sx = viewState.OriginX + gx * viewState.Scale
                let sy = viewState.OriginY + gz * viewState.Scale
                let paint = if u.IsEnemy then enemyPaint else friendlyPaint
                canvas.DrawCircle(sx, sy, config.UnitMarkerSize, paint)

        // Draw event indicators
        if config.ActiveOverlays.Contains OverlayKind.Events then
            use eventPaint = new SKPaint(IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.0f)
            for ev in snapshot.EventIndicators do
                let age = snapshot.FrameNumber - ev.FrameCreated
                if age < ev.DurationFrames then
                    let progress = float32 age / float32 ev.DurationFrames
                    let radius = config.UnitMarkerSize * (1.0f + progress * 3.0f)
                    let alpha = byte ((1.0f - progress) * 255.0f * config.OverlayOpacity)
                    eventPaint.Color <-
                        match ev.Kind with
                        | EventKind.UnitCreated -> SKColor(0uy, 255uy, 0uy, alpha)
                        | EventKind.UnitDestroyed -> SKColor(255uy, 0uy, 0uy, alpha)
                        | EventKind.EnemySpotted -> SKColor(255uy, 255uy, 0uy, alpha)
                        | EventKind.Combat -> SKColor(255uy, 128uy, 0uy, alpha)
                    let gx = ev.PositionX / 8.0f
                    let gz = ev.PositionZ / 8.0f
                    let sx = viewState.OriginX + gx * viewState.Scale
                    let sy = viewState.OriginY + gz * viewState.Scale
                    canvas.DrawCircle(sx, sy, radius, eventPaint)

        // Draw metal spots overlay
        if config.ActiveOverlays.Contains OverlayKind.MetalSpots then
            use spotPaint = new SKPaint(IsAntialias = true, Color = SKColor(200uy, 200uy, 200uy, byte (config.OverlayOpacity * 255.0f)), Style = SKPaintStyle.Fill)
            for (mx, _, mz, richness) in snapshot.MetalSpots do
                let gx = mx / 8.0f
                let gz = mz / 8.0f
                let sx = viewState.OriginX + gx * viewState.Scale
                let sy = viewState.OriginY + gz * viewState.Scale
                let r = 2.0f + richness * 4.0f
                canvas.DrawCircle(sx, sy, r, spotPaint)

        // Draw economy HUD
        if config.ActiveOverlays.Contains OverlayKind.EconomyHud then
            use bgPaint = new SKPaint(Color = SKColor(0uy, 0uy, 0uy, 180uy), Style = SKPaintStyle.Fill)
            use textPaint = new SKPaint(IsAntialias = true, Color = config.LabelColor, TextSize = 14.0f)
            let panelX = float32 viewState.WindowWidth - 200.0f
            let panelY = 10.0f
            canvas.DrawRect(panelX, panelY, 190.0f, 80.0f, bgPaint)
            let m = snapshot.EconomyMetal
            let e = snapshot.EconomyEnergy
            canvas.DrawText(sprintf "Metal:  %.0f / %.0f" m.Current m.Storage, panelX + 8.0f, panelY + 18.0f, textPaint)
            canvas.DrawText(sprintf "  +%.1f  -%.1f" m.Income m.Usage, panelX + 8.0f, panelY + 34.0f, textPaint)
            canvas.DrawText(sprintf "Energy: %.0f / %.0f" e.Current e.Storage, panelX + 8.0f, panelY + 54.0f, textPaint)
            canvas.DrawText(sprintf "  +%.1f  -%.1f" e.Income e.Usage, panelX + 8.0f, panelY + 70.0f, textPaint)

        // Draw disconnected overlay
        if not snapshot.Connected then
            use bgPaint = new SKPaint(Color = SKColor(0uy, 0uy, 0uy, 150uy), Style = SKPaintStyle.Fill)
            use textPaint = new SKPaint(IsAntialias = true, Color = SKColors.Red, TextSize = 32.0f, TextAlign = SKTextAlign.Center)
            let cx = float32 viewState.WindowWidth / 2.0f
            let cy = float32 viewState.WindowHeight / 2.0f
            canvas.DrawRect(cx - 150.0f, cy - 25.0f, 300.0f, 50.0f, bgPaint)
            canvas.DrawText("DISCONNECTED", cx, cy + 10.0f, textPaint)
