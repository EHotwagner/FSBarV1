namespace FSBar.SyntheticData

open FSBar.Client

module UnitDefs =

    let mkDef defId name cost buildSpeed maxRange buildOptions =
        { DefId = defId
          Name = name
          Cost = cost
          BuildSpeed = buildSpeed
          MaxWeaponRange = maxRange
          BuildOptions = buildOptions }

    // --- DefId constants ---
    // Arm faction
    let [<Literal>] ArmCommander = 1
    let [<Literal>] ArmMex = 2
    let [<Literal>] ArmSolar = 3
    let [<Literal>] ArmWind = 4
    let [<Literal>] ArmLab = 5
    let [<Literal>] ArmPeewee = 6
    let [<Literal>] ArmFlash = 7
    let [<Literal>] ArmRockko = 8
    let [<Literal>] ArmSamson = 9
    let [<Literal>] ArmFark = 10
    // Cor faction (enemies)
    let [<Literal>] CorCommander = 11
    let [<Literal>] CorGator = 12
    let [<Literal>] CorThud = 13
    let [<Literal>] CorStorm = 14
    let [<Literal>] CorLab = 15
    // Heavy units (Scene C)
    let [<Literal>] ArmAdvLab = 16
    let [<Literal>] ArmZeus = 17
    let [<Literal>] ArmAnni = 18
    let [<Literal>] CorAdvLab = 19
    let [<Literal>] CorSumo = 20
    let [<Literal>] CorGoliath = 21
    let [<Literal>] ArmStorage = 22
    let [<Literal>] CorStorage = 23

    // --- Max health table ---
    let healthTable =
        Map.ofList [
            ArmCommander, 3000.0f
            ArmMex, 200.0f
            ArmSolar, 250.0f
            ArmWind, 150.0f
            ArmLab, 1500.0f
            ArmPeewee, 280.0f
            ArmFlash, 350.0f
            ArmRockko, 600.0f
            ArmSamson, 300.0f
            ArmFark, 400.0f
            CorCommander, 3000.0f
            CorGator, 400.0f
            CorThud, 650.0f
            CorStorm, 320.0f
            CorLab, 1500.0f
            ArmAdvLab, 3000.0f
            ArmZeus, 900.0f
            ArmAnni, 5000.0f
            CorAdvLab, 3000.0f
            CorSumo, 4500.0f
            CorGoliath, 6000.0f
            ArmStorage, 800.0f
            CorStorage, 800.0f
        ]

    // --- Speed table (elmos/frame) ---
    let speedTable =
        Map.ofList [
            ArmCommander, 1.5f
            ArmMex, 0.0f       // building
            ArmSolar, 0.0f     // building
            ArmWind, 0.0f      // building
            ArmLab, 0.0f       // building
            ArmPeewee, 3.0f
            ArmFlash, 4.5f
            ArmRockko, 2.0f
            ArmSamson, 3.0f
            ArmFark, 3.5f
            CorCommander, 1.5f
            CorGator, 4.0f
            CorThud, 2.0f
            CorStorm, 3.0f
            CorLab, 0.0f       // building
            ArmAdvLab, 0.0f    // building
            ArmZeus, 2.5f
            ArmAnni, 1.0f
            CorAdvLab, 0.0f    // building
            CorSumo, 1.5f
            CorGoliath, 1.2f
            ArmStorage, 0.0f   // building
            CorStorage, 0.0f   // building
        ]

    let maxHealthFor (defId: int) (cache: UnitDefCache) : float32 =
        Map.tryFind defId healthTable |> Option.defaultValue 100.0f

    let speedFor (defId: int) (cache: UnitDefCache) : float32 =
        Map.tryFind defId speedTable |> Option.defaultValue 2.0f

    // --- Scene A defs: early game ---
    let sceneA =
        [
            mkDef ArmCommander "arm_commander" 2500.0f 200.0f 300.0f [| ArmMex; ArmSolar; ArmWind; ArmLab; ArmStorage |]
            mkDef ArmMex "arm_mex" 60.0f 0.0f 0.0f [||]
            mkDef ArmSolar "arm_solar" 150.0f 0.0f 0.0f [||]
            mkDef ArmWind "arm_wind" 50.0f 0.0f 0.0f [||]
            mkDef ArmLab "arm_lab" 650.0f 100.0f 0.0f [| ArmPeewee; ArmFlash; ArmRockko; ArmSamson; ArmFark |]
            mkDef ArmPeewee "arm_peewee" 100.0f 0.0f 240.0f [||]
            mkDef ArmFlash "arm_flash" 150.0f 0.0f 200.0f [||]
            mkDef ArmRockko "arm_rockko" 200.0f 0.0f 350.0f [||]
            mkDef ArmSamson "arm_samson" 180.0f 0.0f 550.0f [||]
            mkDef ArmFark "arm_fark" 120.0f 100.0f 0.0f [||]
            mkDef CorCommander "cor_commander" 2500.0f 200.0f 300.0f [||]
            mkDef CorGator "cor_gator" 150.0f 0.0f 210.0f [||]
            mkDef CorThud "cor_thud" 200.0f 0.0f 340.0f [||]
        ]
        |> UnitDefCache.ofSeq

    // --- Scene B defs: mid game ---
    let sceneB =
        [
            mkDef ArmCommander "arm_commander" 2500.0f 200.0f 300.0f [| ArmMex; ArmSolar; ArmWind; ArmLab; ArmStorage |]
            mkDef ArmMex "arm_mex" 60.0f 0.0f 0.0f [||]
            mkDef ArmSolar "arm_solar" 150.0f 0.0f 0.0f [||]
            mkDef ArmLab "arm_lab" 650.0f 100.0f 0.0f [| ArmPeewee; ArmFlash; ArmRockko; ArmSamson; ArmFark |]
            mkDef ArmPeewee "arm_peewee" 100.0f 0.0f 240.0f [||]
            mkDef ArmFlash "arm_flash" 150.0f 0.0f 200.0f [||]
            mkDef ArmRockko "arm_rockko" 200.0f 0.0f 350.0f [||]
            mkDef ArmSamson "arm_samson" 180.0f 0.0f 550.0f [||]
            mkDef ArmFark "arm_fark" 120.0f 100.0f 0.0f [||]
            mkDef CorCommander "cor_commander" 2500.0f 200.0f 300.0f [||]
            mkDef CorGator "cor_gator" 150.0f 0.0f 210.0f [||]
            mkDef CorThud "cor_thud" 200.0f 0.0f 340.0f [||]
            mkDef CorStorm "cor_storm" 160.0f 0.0f 400.0f [||]
            mkDef CorLab "cor_lab" 650.0f 100.0f 0.0f [||]
        ]
        |> UnitDefCache.ofSeq

    // --- Scene C defs: late game ---
    let sceneC =
        [
            mkDef ArmCommander "arm_commander" 2500.0f 200.0f 300.0f [| ArmMex; ArmSolar; ArmLab; ArmAdvLab; ArmStorage |]
            mkDef ArmMex "arm_mex" 60.0f 0.0f 0.0f [||]
            mkDef ArmSolar "arm_solar" 150.0f 0.0f 0.0f [||]
            mkDef ArmLab "arm_lab" 650.0f 100.0f 0.0f [| ArmPeewee; ArmFlash; ArmRockko; ArmSamson |]
            mkDef ArmPeewee "arm_peewee" 100.0f 0.0f 240.0f [||]
            mkDef ArmFlash "arm_flash" 150.0f 0.0f 200.0f [||]
            mkDef ArmRockko "arm_rockko" 200.0f 0.0f 350.0f [||]
            mkDef ArmSamson "arm_samson" 180.0f 0.0f 550.0f [||]
            mkDef ArmFark "arm_fark" 120.0f 100.0f 0.0f [||]
            mkDef ArmAdvLab "arm_advlab" 2500.0f 200.0f 0.0f [| ArmZeus; ArmAnni |]
            mkDef ArmZeus "arm_zeus" 400.0f 0.0f 280.0f [||]
            mkDef ArmAnni "arm_anni" 3000.0f 0.0f 1100.0f [||]
            mkDef ArmStorage "arm_storage" 300.0f 0.0f 0.0f [||]
            mkDef CorCommander "cor_commander" 2500.0f 200.0f 300.0f [||]
            mkDef CorGator "cor_gator" 150.0f 0.0f 210.0f [||]
            mkDef CorThud "cor_thud" 200.0f 0.0f 340.0f [||]
            mkDef CorStorm "cor_storm" 160.0f 0.0f 400.0f [||]
            mkDef CorLab "cor_lab" 650.0f 100.0f 0.0f [||]
            mkDef CorAdvLab "cor_advlab" 2500.0f 200.0f 0.0f [||]
            mkDef CorSumo "cor_sumo" 2000.0f 0.0f 250.0f [||]
            mkDef CorGoliath "cor_goliath" 3500.0f 0.0f 350.0f [||]
            mkDef CorStorage "cor_storage" 300.0f 0.0f 0.0f [||]
        ]
        |> UnitDefCache.ofSeq
