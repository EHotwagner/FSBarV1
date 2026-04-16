module FSBar.Viz.Tests.ConfigPanelTests

open Xunit
open SkiaSharp
open SkiaViewer
open Silk.NET.Input
open FSBar.Viz

let private cfg = VizDefaults.defaultConfig
let private windowW = 1280.0f
let private windowH = 720.0f

[<Fact>]
let ``initialState is closed`` () =
    let s = ConfigPanel.initialState
    Assert.False(s.IsOpen)
    Assert.Equal(0.0f, s.ScrollOffset)
    Assert.Equal<string option>(None, s.ActiveControl)

[<Fact>]
let ``toggle flips IsOpen`` () =
    let s0 = ConfigPanel.initialState
    let s1 = ConfigPanel.toggle s0
    let s2 = ConfigPanel.toggle s1
    Assert.True(s1.IsOpen)
    Assert.False(s2.IsOpen)

[<Fact>]
let ``buildPanel returns empty list when closed`` () =
    let s = ConfigPanel.initialState
    let elems = ConfigPanel.buildPanel cfg s windowW windowH [] None
    Assert.Empty(elems)

[<Fact>]
let ``buildPanel returns elements when open`` () =
    let s = ConfigPanel.toggle ConfigPanel.initialState
    let elems = ConfigPanel.buildPanel cfg s windowW windowH [] None
    Assert.NotEmpty(elems)

[<Fact>]
let ``hitTest returns true for point inside panel when open`` () =
    let s = ConfigPanel.toggle ConfigPanel.initialState
    // Panel occupies x in [windowW - 280, windowW]
    Assert.True(ConfigPanel.hitTest (windowW - 10.0f) 100.0f s windowW)

[<Fact>]
let ``hitTest returns false for point outside panel`` () =
    let s = ConfigPanel.toggle ConfigPanel.initialState
    Assert.False(ConfigPanel.hitTest 50.0f 100.0f s windowW)

[<Fact>]
let ``hitTest returns false when panel closed`` () =
    let s = ConfigPanel.initialState
    Assert.False(ConfigPanel.hitTest (windowW - 10.0f) 100.0f s windowW)

[<Fact>]
let ``mouse down on empty area produces no config change`` () =
    let s = ConfigPanel.toggle ConfigPanel.initialState
    // Click outside panel bounds: x < windowW - 280
    let res = ConfigPanel.handleInput (InputEvent.MouseDown(MouseButton.Left, 10.0f, 10.0f)) cfg s windowW windowH
    Assert.True(res.UpdatedConfig.IsNone)
    Assert.True(res.Action.IsNone)

// Row layout pitches mirrored from ConfigPanel.fs — kept in sync so tests
// can compute click-Y from the current preset count without relying on
// hard-coded offsets.
let private rowHeight = 22.0f
let private headerHeight = 24.0f
let private spacerHeight = rowHeight / 2.0f

/// Computed Y of the save button given the current preset file count.
let private saveButtonY () =
    let nPresets = FSBar.Viz.StylePreset.listNames().Length
    // Title + PresetHeader + N×preset-item
    headerHeight + headerHeight + float32 nPresets * rowHeight

let private resetButtonY () = saveButtonY () + rowHeight

/// Y of the Colors section header when all sections are collapsed.
let private colorsHeaderY () = resetButtonY () + rowHeight + spacerHeight

[<Fact>]
let ``section toggle changes expanded set`` () =
    let mutable s = ConfigPanel.toggle ConfigPanel.initialState
    s <- { s with ExpandedSections = Set.empty }
    let y = colorsHeaderY () + 2.0f
    let x = windowW - 100.0f
    let res = ConfigPanel.handleInput (InputEvent.MouseDown(MouseButton.Left, x, y)) cfg s windowW windowH
    Assert.True(Set.contains "Colors" res.PanelState.ExpandedSections)

[<Fact>]
let ``scroll wheel updates ScrollOffset`` () =
    let s = ConfigPanel.toggle ConfigPanel.initialState
    let res = ConfigPanel.handleInput (InputEvent.MouseScroll(-1.0f, windowW - 10.0f, 100.0f)) cfg s windowW windowH
    Assert.True(res.PanelState.ScrollOffset > 0.0f)

[<Fact>]
let ``scroll offset clamps to 0 when scrolling up at top`` () =
    let s = { ConfigPanel.toggle ConfigPanel.initialState with ScrollOffset = 0.0f }
    let res = ConfigPanel.handleInput (InputEvent.MouseScroll(1.0f, windowW - 10.0f, 100.0f)) cfg s windowW windowH
    Assert.Equal(0.0f, res.PanelState.ScrollOffset)

[<Fact>]
let ``save button click emits SavePreset action`` () =
    let s = { ConfigPanel.toggle ConfigPanel.initialState with ExpandedSections = Set.empty }
    let y = saveButtonY () + 4.0f
    let x = windowW - 100.0f
    let res = ConfigPanel.handleInput (InputEvent.MouseDown(MouseButton.Left, x, y)) cfg s windowW windowH
    match res.Action with
    | Some (ConfigPanelAction.SavePreset _) -> ()
    | other -> Assert.Fail(sprintf "Expected SavePreset action, got %A" other)

[<Fact>]
let ``reset button click emits ResetDefaults action`` () =
    let s = { ConfigPanel.toggle ConfigPanel.initialState with ExpandedSections = Set.empty }
    let y = resetButtonY () + 4.0f
    let x = windowW - 100.0f
    let res = ConfigPanel.handleInput (InputEvent.MouseDown(MouseButton.Left, x, y)) cfg s windowW windowH
    match res.Action with
    | Some ConfigPanelAction.ResetDefaults -> ()
    | other -> Assert.Fail(sprintf "Expected ResetDefaults action, got %A" other)

[<Fact>]
let ``mouse up clears ActiveControl`` () =
    let s = { ConfigPanel.toggle ConfigPanel.initialState with ActiveControl = Some "sizes.unitMarker" }
    let res = ConfigPanel.handleInput (InputEvent.MouseUp(MouseButton.Left, windowW - 10.0f, 10.0f)) cfg s windowW windowH
    Assert.Equal<string option>(None, res.PanelState.ActiveControl)

[<Fact>]
let ``handleInput preserves config when nothing was clicked`` () =
    let s = ConfigPanel.toggle ConfigPanel.initialState
    // Click outside panel entirely
    let res = ConfigPanel.handleInput (InputEvent.MouseDown(MouseButton.Left, 10.0f, 10.0f)) cfg s windowW windowH
    Assert.True(res.UpdatedConfig.IsNone)
