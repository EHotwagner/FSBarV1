// 21-hub-overlay-layers.fsx — Feature 040 US6 walkthrough.
//
// Uploads declarative overlay layers to the Hub via PutLayer. The Hub
// composites them on top of the base Viewer scene, so the primitives
// show up in both the local window and in every StreamRenderFrames
// subscriber.
//
// Run after the Hub is running (and ideally with a session active for
// world-space primitives to make sense). The script demonstrates:
//   1. A layer with mixed World + Screen primitives.
//   2. Atomic replace via a second PutLayer with the same name.
//   3. DeleteLayer.
//   4. ClearLayers to drop all remaining layers.

#r "nuget: Grpc.Net.Client, 2.*"
#r "nuget: FSBar.Hub, *-*"

open System
open System.Threading
open Grpc.Net.Client
open Fsbar.Hub.Scripting.V1

let endpoint = "http://127.0.0.1:5021"
use channel = GrpcChannel.ForAddress(endpoint)
let client = ScriptingService.ScriptingServiceClient(channel)

let report (label: string) (res: MutationResult option) =
    match res with
    | Some r -> printfn "%-30s → %A %s" label r.Outcome r.Reason
    | None -> printfn "%-30s → no result" label

let style (color: uint32) (width: float32) : OverlayStyle =
    { StrokeColorRgba = color
      StrokeWidth = width
      HasFill = false
      FillColorRgba = 0u
      Opacity = 1.0f
      Dash = [] }

let worldCircle x y r color =
    ({ Space = CoordinateSpace.World
       Style = Some (style color 2.0f)
       Primitive =
           OverlayPrimitive.PrimitiveCase.Circle
               ({ Center = Some { X = x; Y = y }; Radius = r } : CirclePrimitive) }
     : OverlayPrimitive)

let screenLabel x y text =
    ({ Space = CoordinateSpace.Screen
       Style = Some (style 0xFFFFFFFFu 1.0f)
       Primitive =
           OverlayPrimitive.PrimitiveCase.Text
               ({ Anchor = Some { X = x; Y = y }
                  Text = text
                  FontSize = 16.0f
                  FontFamily = ""
                  Align = TextAlign.Left } : TextPrimitive) }
     : OverlayPrimitive)

// ---- Step 1: PutLayer with mixed primitives ----
printfn "— PutLayer: demo with mixed World + Screen primitives ——"
let layer1 : OverlayLayerWire = {
    Name = "demo"
    ZHint = 0
    Primitives =
        [ worldCircle 200.0f 200.0f 15.0f 0xFF0000FFu     // red circle
          worldCircle 400.0f 300.0f 25.0f 0xFFFF00FFu     // yellow circle
          screenLabel 20.0f 20.0f "demo overlay" ]
}
let put1 = client.PutLayer ({ Layer = Some layer1 } : PutLayerRequest)
report "PutLayer demo" put1.Result
if put1.ExceededCap <> "" then
    printfn "  exceededCap=%s" put1.ExceededCap

Thread.Sleep 500

// ---- Step 2: Atomic replace ----
printfn ""
printfn "— PutLayer: atomic replace with a single polygon ——"
let polygon : OverlayPrimitive = {
    Space = CoordinateSpace.World
    Style = Some (style 0xFFFF00FFu 3.0f)
    Primitive =
        OverlayPrimitive.PrimitiveCase.Polygon
            ({ Points =
                [ { X = 100.0f; Y = 100.0f }
                  { X = 300.0f; Y = 100.0f }
                  { X = 200.0f; Y = 280.0f } ] } : PolygonPrimitive)
}
let layer2 : OverlayLayerWire = {
    Name = "demo"
    ZHint = 0
    Primitives = [ polygon ]
}
let put2 = client.PutLayer ({ Layer = Some layer2 } : PutLayerRequest)
report "PutLayer demo (replace)" put2.Result

// ---- Step 3: ListLayers ----
printfn ""
printfn "— ListLayers ——"
let list = client.ListLayers ListLayersRequest.Unused
for d in list.Layers do
    printfn "  · %s (z=%d, primitives=%d)" d.Name d.ZHint d.PrimitiveCount

// ---- Step 4: DeleteLayer ----
printfn ""
printfn "— DeleteLayer demo ——"
let del = client.DeleteLayer ({ Name = "demo" } : DeleteLayerRequest)
report "DeleteLayer demo" del.Result

// ---- Step 5: Upload another layer + ClearLayers ----
printfn ""
printfn "— ClearLayers sweep ——"
let layer3 : OverlayLayerWire = {
    Name = "sweep-me"
    ZHint = 0
    Primitives = [ worldCircle 500.0f 500.0f 10.0f 0xFF00FF00u ]
}
client.PutLayer ({ Layer = Some layer3 } : PutLayerRequest) |> ignore
let cleared = client.ClearLayers ClearLayersRequest.Unused
report (sprintf "ClearLayers (cleared=%d)" cleared.ClearedCount) cleared.Result
