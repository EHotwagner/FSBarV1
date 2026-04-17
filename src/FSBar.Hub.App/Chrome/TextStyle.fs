namespace FSBar.Hub.App.Chrome

open SkiaSharp
open SkiaViewer

module TextStyle =

    let headingColor = SKColor(0xffuy, 0xffuy, 0xffuy, 0xffuy)
    let bodyColor = SKColor(0xf3uy, 0xf5uy, 0xfauy, 0xffuy)
    let dimColor = SKColor(0xb4uy, 0xbduy, 0xccuy, 0xffuy)
    let accentColor = SKColor(0x9cuy, 0xc0uy, 0xf5uy, 0xffuy)

    let headingPaint = Scene.fill headingColor
    let bodyPaint = Scene.fill bodyColor
    let dimPaint = Scene.fill dimColor
    let accentPaint = Scene.fill accentColor

    let headerSize : float32 = 17.0f
    let titleSize : float32 = 20.0f
    let bodySize : float32 = 16.0f
    let dimSize : float32 = 15.0f
