namespace FSBar.Viz

open System
open System.Threading
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open Silk.NET.Input
open SkiaSharp

type ViewerConfig =
    { Title: string
      Width: int
      Height: int
      TargetFps: int
      ClearColor: SKColor
      OnRender: SKCanvas -> Vector2D<int> -> unit
      OnResize: int -> int -> unit
      OnKeyDown: Key -> unit
      OnMouseScroll: float32 -> float32 -> float32 -> unit
      OnMouseDrag: float32 -> float32 -> unit }

module Viewer =

    type private ViewerHandle(stop: unit -> unit) =
        interface IDisposable with
            member _.Dispose() = stop ()

    let run (config: ViewerConfig) : IDisposable =
        let mutable windowRef: IWindow option = None

        let thread =
            Thread(
                ThreadStart(fun () ->
                  try
                    let mutable opts = WindowOptions.Default
                    opts.Title <- config.Title
                    opts.Size <- Vector2D<int>(config.Width, config.Height)
                    opts.UpdatesPerSecond <- float config.TargetFps
                    opts.FramesPerSecond <- float config.TargetFps
                    opts.VSync <- false

                    let win = Window.Create opts
                    windowRef <- Some win

                    let mutable gl: GL = Unchecked.defaultof<_>
                    let mutable grCtx: GRContext = Unchecked.defaultof<_>
                    let mutable surface: SKSurface = Unchecked.defaultof<_>

                    let recreateSurface () =
                        if not (obj.ReferenceEquals(surface, null)) then
                            surface.Dispose()
                            surface <- Unchecked.defaultof<_>

                        if obj.ReferenceEquals(gl, null) || obj.ReferenceEquals(grCtx, null) then
                            ()
                        else
                            let fbSize = win.FramebufferSize
                            let fbo = gl.GetInteger(GLEnum.FramebufferBinding)
                            let fbInfo = GRGlFramebufferInfo(uint32 fbo, uint32 0x8058u)

                            let backend =
                                new GRBackendRenderTarget(fbSize.X, fbSize.Y, 0, 8, fbInfo)

                            surface <-
                                SKSurface.Create(
                                    grCtx,
                                    backend,
                                    GRSurfaceOrigin.BottomLeft,
                                    SKColorType.Rgba8888
                                )

                    win.add_Load (fun _ ->
                        gl <- GL.GetApi(win)
                        let glInterface = GRGlInterface.Create()
                        grCtx <- GRContext.CreateGl(glInterface)
                        recreateSurface ()

                        let input = win.CreateInput()

                        for kb in input.Keyboards do
                            kb.add_KeyDown (fun _ key _ -> config.OnKeyDown key)

                        let mutable dragging = false
                        let mutable lastMouseX = 0.0f
                        let mutable lastMouseY = 0.0f

                        for mouse in input.Mice do
                            mouse.add_Scroll (fun _ wheel ->
                                let pos = mouse.Position
                                config.OnMouseScroll wheel.Y pos.X pos.Y)

                            mouse.add_MouseDown (fun _ btn ->
                                if btn = MouseButton.Left then
                                    dragging <- true
                                    lastMouseX <- mouse.Position.X
                                    lastMouseY <- mouse.Position.Y)

                            mouse.add_MouseUp (fun _ btn ->
                                if btn = MouseButton.Left then
                                    dragging <- false)

                            mouse.add_MouseMove (fun _ pos ->
                                if dragging then
                                    let dx = pos.X - lastMouseX
                                    let dy = pos.Y - lastMouseY
                                    lastMouseX <- pos.X
                                    lastMouseY <- pos.Y
                                    config.OnMouseDrag dx dy))

                    win.add_FramebufferResize (fun size ->
                        recreateSurface ()
                        config.OnResize size.X size.Y)

                    win.add_Render (fun _ ->
                        if not (obj.ReferenceEquals(surface, null)) then
                            try
                                let canvas = surface.Canvas

                                if not (obj.ReferenceEquals(canvas, null)) then
                                    canvas.Clear config.ClearColor
                                    let fbSize = win.FramebufferSize
                                    config.OnRender canvas fbSize
                                    canvas.Flush()
                                    gl.Flush()
                            with
                            | :? ObjectDisposedException -> ()
                            | :? NullReferenceException -> ()
                            | :? System.ArgumentNullException -> ())

                    win.add_Closing (fun _ ->
                        if not (obj.ReferenceEquals(surface, null)) then
                            surface.Dispose()
                            surface <- Unchecked.defaultof<_>

                        if not (obj.ReferenceEquals(grCtx, null)) then
                            grCtx.Dispose()
                            grCtx <- Unchecked.defaultof<_>)

                    win.Run()
                  with
                  | ex ->
                      eprintfn "[GameViz.Viewer] Window thread error: %s" ex.Message)
            )

        thread.IsBackground <- true
        thread.Start()

        let stop () =
            match windowRef with
            | Some w ->
                try
                    w.Close()
                with
                | _ -> ()

                windowRef <- None
            | _ -> ()

        new ViewerHandle(stop) :> IDisposable
