namespace FSBar.SyntheticData

open FSBar.Client

module EconomySim =

    let step (snapshot: EconomySnapshot) : EconomySnapshot =
        let delta = (snapshot.Income - snapshot.Usage) / 30.0f
        let newCurrent = snapshot.Current + delta
        let clamped = max 0.0f (min newCurrent snapshot.Storage)
        { snapshot with Current = clamped }
