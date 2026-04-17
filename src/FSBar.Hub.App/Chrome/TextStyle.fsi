namespace FSBar.Hub.App.Chrome

open SkiaSharp
open SkiaViewer

/// Shared text palette + font sizes used across every tab and chrome
/// element. Previously each tab defined its own `headingText` /
/// `bodyText` / `dimText` paints with a slate palette; this module
/// centralises those to a whiter, slightly-larger readability-first
/// style.
module TextStyle =

    // --- Colors (near-white so text is legible against the dark
    // content panels; dim still lighter than the slate it was
    // previously so low-priority metadata doesn't disappear).
    val headingColor: SKColor
    val bodyColor: SKColor
    val dimColor: SKColor
    val accentColor: SKColor

    val headingPaint: Paint
    val bodyPaint: Paint
    val dimPaint: Paint
    val accentPaint: Paint

    // --- Font sizes (px). Bumped by ~2px across the board for
    // comfortable reading at 1280x800.
    val headerSize: float32     // section titles
    val titleSize: float32      // tab-top heading
    val bodySize: float32       // primary data rows
    val dimSize: float32        // subtitle / metadata
