namespace FSBar.SyntheticData

/// Scene validation for internal consistency.
module Validation =
    /// Validate structural invariants of a scene. Returns a list of error messages (empty = valid).
    val validate: scene: Scene -> string list

    /// Validate frame-to-frame continuity. Returns a list of error messages (empty = valid).
    val validateContinuity: scene: Scene -> string list
