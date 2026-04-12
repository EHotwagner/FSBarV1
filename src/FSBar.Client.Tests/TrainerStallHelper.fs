module FSBar.Client.Tests.TrainerStallHelper

/// Per-iteration telemetry snapshot consumed by the stall comparison.
/// Int fields come from feature 020 telemetry and are never NaN.
/// Peak fields are float option so a match that read Single.NaN for
/// the resource every frame serializes as None (per 021 FR-003). The
/// stall check must skip None when judging improvement or stagnation
/// — None neither improves nor stagnates.
type StallTelemetry = {
    FramesSurvived: int
    EnemyKilled: int
    UnitsBuilt: int
    PeakMetal: float option
    PeakEnergy: float option
}

/// Pure comparison: does `current` show any improvement over `prior`?
///
/// Returns `true` as soon as *any* comparable field improved. Returns
/// `false` only when every comparable field stagnated — int fields are
/// always comparable; peak fields are comparable only when the relevant
/// member is not `None` in the relevant side.
///
/// Transition rules for the peak option fields:
/// - Both `None`: skip (neither improvement nor stagnation from this field).
/// - Prior `None`, current `Some _`: improvement (we just learned a value).
/// - Prior `Some _`, current `None`: skip (NaN in current is "unknown",
///   not a regression — stagnation verdict comes from the other fields).
/// - Both `Some a`, `Some b`: strict improvement iff b > a.
let improvedOverPrior (prior: StallTelemetry) (current: StallTelemetry) : bool =
    let intImproved =
        current.FramesSurvived > prior.FramesSurvived
        || current.EnemyKilled > prior.EnemyKilled
        || current.UnitsBuilt > prior.UnitsBuilt

    let peakOptImproved (p: float option) (c: float option) =
        match p, c with
        | None, Some _ -> true
        | Some a, Some b when b > a -> true
        | _ -> false

    intImproved
    || peakOptImproved prior.PeakMetal current.PeakMetal
    || peakOptImproved prior.PeakEnergy current.PeakEnergy
