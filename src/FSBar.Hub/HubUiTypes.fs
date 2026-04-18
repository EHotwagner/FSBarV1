namespace FSBar.Hub

type HubTab =
    | Setup
    | Viewer
    | Units
    | Style
    | Cfg
    | Grpc

type ViewerCamera =
    { Scale: float32
      OriginX: float32
      OriginY: float32
      AutoFit: bool }

type FactionFilterKey =
    | Armada
    | Cortex
    | Legion
    | Raptors
    | Scavengers
    | Neutral

type TierFilterKey =
    | T1
    | T2
    | T3
    | Commander

type MobilityFilterKey =
    | Building
    | Ground
    | Hover
    | Ship
    | Air
    | Amphib

type EncyclopediaSelection =
    { FactionFilter: Set<FactionFilterKey>
      TierFilter: Set<TierFilterKey>
      MobilityFilter: Set<MobilityFilterKey>
      SearchText: string
      SelectedDefId: int option }

module EncyclopediaSelection =
    let defaults: EncyclopediaSelection =
        { FactionFilter = Set.empty
          TierFilter = Set.empty
          MobilityFilter = Set.empty
          SearchText = ""
          SelectedDefId = None }

type SubmitOutcome =
    | Sent
    | Rejected of reason: string

type ToggleTarget =
    | Toggle
    | On
    | Off

module ViewerCamera =
    let defaults: ViewerCamera =
        { Scale = 1.0f
          OriginX = 0.0f
          OriginY = 0.0f
          AutoFit = true }

    let private isFiniteF32 (v: float32) =
        not (System.Single.IsNaN(v)) && not (System.Single.IsInfinity(v))

    let validate (camera: ViewerCamera) : Result<ViewerCamera, string> =
        if not (isFiniteF32 camera.Scale) then
            Error "Scale must be finite"
        elif camera.Scale < 0.05f || camera.Scale > 100.0f then
            Error (sprintf "Scale=%f outside [0.05, 100.0]" (float camera.Scale))
        elif not (isFiniteF32 camera.OriginX) then
            Error "OriginX must be finite"
        elif not (isFiniteF32 camera.OriginY) then
            Error "OriginY must be finite"
        else
            Ok camera
