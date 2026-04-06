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

    let private vertexShaderSrc = """#version 330 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aTexCoord;
out vec2 TexCoord;
void main() {
    gl_Position = vec4(aPos, 0.0, 1.0);
    TexCoord = aTexCoord;
}"""

    let private fragmentShaderSrc = """#version 330 core
in vec2 TexCoord;
out vec4 FragColor;
uniform sampler2D tex;
void main() {
    FragColor = texture(tex, TexCoord);
}"""

    let private platformRegistered = lazy (Silk.NET.Windowing.Glfw.GlfwWindowing.RegisterPlatform())

    let run (config: ViewerConfig) : IDisposable =
        let mutable windowRef: IWindow option = None
        let surfaceLock = obj ()
        let mutable shutdownRequested = false
        let windowCompleted = new ManualResetEventSlim(false)

        let thread =
            Thread(
                ThreadStart(fun () ->
                  try
                    platformRegistered.Force()
                    let mutable opts = WindowOptions.Default
                    opts.Title <- config.Title
                    opts.Size <- Vector2D<int>(config.Width, config.Height)
                    opts.UpdatesPerSecond <- float config.TargetFps
                    opts.FramesPerSecond <- float config.TargetFps
                    opts.VSync <- false

                    let win = Window.Create opts
                    windowRef <- Some win

                    let mutable gl: GL = Unchecked.defaultof<_>
                    let mutable glReady = false
                    let mutable surface: SKSurface = Unchecked.defaultof<_>
                    let mutable texture: uint32 = 0u
                    let mutable vao: uint32 = 0u
                    let mutable vbo: uint32 = 0u
                    let mutable shaderProgram: uint32 = 0u
                    let mutable surfaceWidth = 0
                    let mutable surfaceHeight = 0

                    let recreateSurface () =
                        if not glReady then () // Guard pre-init resize
                        else

                        let fbSize = win.FramebufferSize

                        if fbSize.X > 0 && fbSize.Y > 0 then
                            let info = new SKImageInfo(fbSize.X, fbSize.Y, SKColorType.Rgba8888, SKAlphaType.Premul)
                            let newSurface = SKSurface.Create(info)
                            let oldSurface =
                                lock surfaceLock (fun () ->
                                    let old = surface
                                    surface <- newSurface
                                    surfaceWidth <- fbSize.X
                                    surfaceHeight <- fbSize.Y
                                    old)
                            if not (obj.ReferenceEquals(oldSurface, null)) then
                                oldSurface.Dispose()
                            eprintfn "[Viewer] Surface created: %dx%d" fbSize.X fbSize.Y
                        else
                            // Zero-size framebuffer (minimized): set surface to null
                            let oldSurface =
                                lock surfaceLock (fun () ->
                                    let old = surface
                                    surface <- Unchecked.defaultof<_>
                                    surfaceWidth <- 0
                                    surfaceHeight <- 0
                                    old)
                            if not (obj.ReferenceEquals(oldSurface, null)) then
                                oldSurface.Dispose()
                            eprintfn "[Viewer] Framebuffer zero-size, surface cleared"

                    let compileShader (gl: GL) (shaderType: ShaderType) (source: string) =
                        let shader = gl.CreateShader(shaderType)
                        gl.ShaderSource(shader, source)
                        gl.CompileShader(shader)
                        let status = gl.GetShader(shader, ShaderParameterName.CompileStatus)
                        if status = 0 then
                            let log = gl.GetShaderInfoLog(shader)
                            eprintfn "[Viewer] Shader compile error: %s" log
                        shader

                    let setupGl () =
                        let vs = compileShader gl ShaderType.VertexShader vertexShaderSrc
                        let fs = compileShader gl ShaderType.FragmentShader fragmentShaderSrc
                        shaderProgram <- gl.CreateProgram()
                        gl.AttachShader(shaderProgram, vs)
                        gl.AttachShader(shaderProgram, fs)
                        gl.LinkProgram(shaderProgram)
                        let linkStatus = gl.GetProgram(shaderProgram, ProgramPropertyARB.LinkStatus)
                        if linkStatus = 0 then
                            let log = gl.GetProgramInfoLog(shaderProgram)
                            eprintfn "[Viewer] Shader link error: %s" log
                        gl.DeleteShader(vs)
                        gl.DeleteShader(fs)

                        // Fullscreen quad vertices: pos(x,y) + texcoord(u,v)
                        let bl = [| -1.0f; -1.0f; 0.0f; 1.0f |]
                        let br = [|  1.0f; -1.0f; 1.0f; 1.0f |]
                        let tr = [|  1.0f;  1.0f; 1.0f; 0.0f |]
                        let tl = [| -1.0f;  1.0f; 0.0f; 0.0f |]
                        let vertices: float32[] = Array.concat [| bl; br; tr; bl; tr; tl |]

                        vao <- gl.GenVertexArray()
                        gl.BindVertexArray(vao)

                        vbo <- gl.GenBuffer()
                        gl.BindBuffer(GLEnum.ArrayBuffer, vbo)
                        let span = ReadOnlySpan<float32>(vertices)
                        gl.BufferData(GLEnum.ArrayBuffer, ReadOnlySpan<byte>(System.Runtime.InteropServices.MemoryMarshal.AsBytes(span).ToArray()), GLEnum.StaticDraw)

                        gl.VertexAttribPointer(0u, 2, GLEnum.Float, false, 4u * 4u, nativeint 0 |> NativeInterop.NativePtr.ofNativeInt<byte> |> NativeInterop.NativePtr.toVoidPtr)
                        gl.EnableVertexAttribArray(0u)
                        gl.VertexAttribPointer(1u, 2, GLEnum.Float, false, 4u * 4u, nativeint 8 |> NativeInterop.NativePtr.ofNativeInt<byte> |> NativeInterop.NativePtr.toVoidPtr)
                        gl.EnableVertexAttribArray(1u)

                        texture <- gl.GenTexture()
                        gl.BindTexture(GLEnum.Texture2D, texture)
                        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, int GLEnum.Linear)
                        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, int GLEnum.Linear)

                    let mutable inputCtx: Silk.NET.Input.IInputContext option = None

                    win.add_Load (fun _ ->
                        gl <- GL.GetApi(win)
                        gl.ClearColor(
                            float32 config.ClearColor.Red / 255.0f,
                            float32 config.ClearColor.Green / 255.0f,
                            float32 config.ClearColor.Blue / 255.0f,
                            1.0f)
                        setupGl ()
                        glReady <- true
                        recreateSurface ()
                        eprintfn "[Viewer] Window loaded, GL context ready"

                        let input = win.CreateInput()
                        inputCtx <- Some input

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

                    // Cross-thread shutdown: check flag from the window thread
                    win.add_Update (fun _ ->
                        if shutdownRequested then
                            eprintfn "[Viewer] Shutdown requested, closing window"
                            win.Close())

                    win.add_Render (fun _ ->
                        // Snapshot surface state under lock
                        let snapSurface, snapWidth, snapHeight =
                            lock surfaceLock (fun () -> surface, surfaceWidth, surfaceHeight)

                        if not (obj.ReferenceEquals(snapSurface, null)) then
                            try
                                let canvas = snapSurface.Canvas

                                if not (obj.ReferenceEquals(canvas, null)) then
                                    canvas.Clear config.ClearColor
                                    let fbSize = win.FramebufferSize
                                    config.OnRender canvas fbSize
                                    canvas.Flush()

                                    // Upload raster pixels to GL texture and draw fullscreen quad
                                    let pixmap = snapSurface.PeekPixels()
                                    if not (obj.ReferenceEquals(pixmap, null)) then
                                        let pixels = pixmap.GetPixels()
                                        gl.BindTexture(GLEnum.Texture2D, texture)
                                        gl.TexImage2D(GLEnum.Texture2D, 0, int GLEnum.Rgba8, uint32 snapWidth, uint32 snapHeight, 0, GLEnum.Rgba, GLEnum.UnsignedByte, pixels.ToPointer())
                                        gl.Viewport(0, 0, uint32 snapWidth, uint32 snapHeight)
                                        gl.Clear(uint32 GLEnum.ColorBufferBit)
                                        gl.UseProgram(shaderProgram)
                                        gl.BindVertexArray(vao)
                                        gl.DrawArrays(GLEnum.Triangles, 0, 6u)
                            with
                            | :? ObjectDisposedException as ex ->
                                eprintfn "[Viewer] Render warning (ObjectDisposed): %s" ex.Message
                            | :? NullReferenceException as ex ->
                                eprintfn "[Viewer] Render warning (NullReference): %s" ex.Message
                            | :? System.ArgumentNullException as ex ->
                                eprintfn "[Viewer] Render warning (ArgumentNull): %s" ex.Message
                            | ex ->
                                eprintfn "[Viewer] Render callback exception (%s): %s" (ex.GetType().Name) ex.Message)

                    win.add_Closing (fun _ ->
                        match inputCtx with
                        | Some ctx ->
                            ctx.Dispose()
                            inputCtx <- None
                        | None -> ()

                        let oldSurface =
                            lock surfaceLock (fun () ->
                                let old = surface
                                surface <- Unchecked.defaultof<_>
                                old)
                        if not (obj.ReferenceEquals(oldSurface, null)) then
                            oldSurface.Dispose()

                        if shaderProgram <> 0u then
                            gl.DeleteProgram(shaderProgram)
                        if vao <> 0u then
                            gl.DeleteVertexArray(vao)
                        if vbo <> 0u then
                            gl.DeleteBuffer(vbo)
                        if texture <> 0u then
                            gl.DeleteTexture(texture)
                        eprintfn "[Viewer] Resources released")

                    win.Run()
                    eprintfn "[Viewer] Window thread exiting"
                  with
                  | ex ->
                      eprintfn "[Viewer] Window thread error: %s" ex.Message
                  windowCompleted.Set())
            )

        thread.IsBackground <- true
        thread.Start()

        let stop () =
            shutdownRequested <- true
            // Wait for the window thread to finish with a timeout
            if not (windowCompleted.Wait(TimeSpan.FromSeconds(5.0))) then
                eprintfn "[Viewer] Warning: window thread did not complete within 5s timeout"
            windowRef <- None

        new ViewerHandle(stop) :> IDisposable
