namespace FSBar.Hub.App.Tabs

open SkiaSharp
open SkiaViewer
open FSBar.Hub

module GrpcTab =

    let private headingText = Scene.fill (SKColor(0xeauy, 0xeeuy, 0xf6uy, 0xffuy))
    let private bodyText = Scene.fill (SKColor(0xc0uy, 0xcauy, 0xdcuy, 0xffuy))
    let private dimText = Scene.fill (SKColor(0x7auy, 0x86uy, 0x9cuy, 0xffuy))
    let private rowBg = Scene.fill (SKColor(0x16uy, 0x1buy, 0x26uy, 0xffuy))
    let private panelBg = Scene.fill (SKColor(0x10uy, 0x14uy, 0x1cuy, 0xffuy))
    let private bannerText = Scene.fill (SKColor(0xffuy, 0xa5uy, 0x50uy, 0xffuy))

    let private rowHeight : float32 = 22.0f

    let render
            (service: ScriptingHub.ScriptingService option)
            (endpointUrl: string)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : Element list =
        let baseY = contentY + 22.0f
        let heading = "gRPC — scripting service endpoint"
        let headerBlock =
            [ Scene.text heading (contentX + 8.0f) baseY 18.0f headingText
              Scene.text (sprintf "Endpoint: %s" endpointUrl) (contentX + 8.0f) (baseY + 24.0f) 13.0f bodyText ]
        match service with
        | None ->
            headerBlock @
            [ Scene.text
                "⚠ scripting service is not running — check BAR install detection."
                (contentX + 8.0f) (baseY + 50.0f) 12.0f bannerText ]
        | Some svc ->
            let clients = svc.Clients
            let overflow = svc.OverflowDetachCount
            let summary =
                sprintf "%d connected · %d OverflowDropLimit detaches since start"
                    clients.Length overflow
            let rosterTop = baseY + 80.0f
            let panelW = contentW - 16.0f
            let rosterElems =
                [ yield Scene.rect (contentX + 8.0f) rosterTop panelW 28.0f panelBg
                  yield Scene.text "id" (contentX + 16.0f) (rosterTop + 18.0f) 11.0f dimText
                  yield Scene.text "label" (contentX + 100.0f) (rosterTop + 18.0f) 11.0f dimText
                  yield Scene.text "remote" (contentX + 260.0f) (rosterTop + 18.0f) 11.0f dimText
                  yield Scene.text "attached" (contentX + 500.0f) (rosterTop + 18.0f) 11.0f dimText
                  yield Scene.text "drops" (contentX + 620.0f) (rosterTop + 18.0f) 11.0f dimText
                  for i in 0 .. clients.Length - 1 do
                      let c = clients.[i]
                      let rowY = rosterTop + 28.0f + float32 i * rowHeight
                      if i % 2 = 0 then
                          yield Scene.rect (contentX + 8.0f) rowY panelW rowHeight rowBg
                      let attached =
                          System.DateTimeOffset.FromUnixTimeMilliseconds(c.AttachedAtUnixMs)
                              .ToString("HH:mm:ss")
                      yield Scene.text (c.ClientId.ToString("N").Substring(0, 8))
                              (contentX + 16.0f) (rowY + 15.0f) 11.0f bodyText
                      yield Scene.text c.ClientLabel
                              (contentX + 100.0f) (rowY + 15.0f) 11.0f bodyText
                      yield Scene.text c.RemoteEndpoint
                              (contentX + 260.0f) (rowY + 15.0f) 11.0f bodyText
                      yield Scene.text attached
                              (contentX + 500.0f) (rowY + 15.0f) 11.0f bodyText
                      yield Scene.text (string c.CumulativeDroppedFrames)
                              (contentX + 620.0f) (rowY + 15.0f) 11.0f bodyText
                  if clients.IsEmpty then
                      yield Scene.text "(no clients connected — start one with `dotnet fsi src/FSBar.Hub/scripts/examples/03-launch-and-stream.fsx`)"
                              (contentX + 16.0f) (rosterTop + 52.0f) 11.0f dimText ]
            headerBlock @
            [ Scene.text summary (contentX + 8.0f) (baseY + 52.0f) 12.0f bodyText ]
            @ rosterElems
