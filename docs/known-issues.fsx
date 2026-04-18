(**
---
title: Known Issues
category: Reference
categoryindex: 5
index: 10
description: Current limitations, workarounds, and platform caveats.
---
*)

(**
# Known Issues

## Platform

- **Linux-first.** Unit tests run anywhere, but live engine integration, the FSI MCP flow, and every container recipe target Linux. The Hub GUI has not been tested on Windows or macOS.
- **SkiaSharp GPU backend segfaults** in the dev-container environment. `FSBar.Viz` uses a raster `SKSurface` + GL texture upload instead — see `src/FSBar.Viz/Viewer.fs`.
- **Graphical engine runs windowed only.** `EngineLauncher` forces `Fullscreen=0` in per-session `springsettings.cfg`.

## Hub

- **Pause state can drift** if the user types `/pause` in BAR's native UI — the Hub's `IsPaused` flag won't reconcile. Click the Viewer-tab pause button twice to recover. (Tracked in feature 038 notes.)
- **Admin channel requires a fresh launch.** The UDP autohost socket binds at `SessionManager.Launch`, not at Hub startup. Controls render dimmed and show an inline reason whenever `SessionManager.AdminStatus <> Some Attached`.
- **Headless renderer is raster-only** (same GPU caveat as above). Suitable for preview and streaming, but not high-FPS.

## Trainer / tests

- **`GrpcLogStream` tests are order-sensitive.** `TruncatedContent` can corrupt server-side buffer state; run `LogStreamTests` individually rather than as a suite.
- **Live tests need a real BAR install.** `./tests/check-prerequisites.sh` diagnoses missing pieces. Missing engines cause the whole live suite to skip, not fail.
- **Map-cache `codeVersion` mismatch is a hard abort** in the trainer warmup — by design. Regenerate via `bots/trainer/map-cache/refresh-all.sh` after bumping the version.

## FSI REPL

- **DLL references are locked once loaded.** After a rebuild, restart FSI (`restart_fsi` MCP tool) or the MCP server.
- **Native libraries must be `dlopen`'d** before first SkiaSharp/GLFW use — see the preamble in `scripts/examples/Repl.fsx`.

## Proto regeneration

- **No prebuilt `protoc-gen-fsgrpc`.** Generated files are committed, so `dotnet build` works out of the box. Regeneration requires installing the patched plugin via the helper in the sibling `fsGRPCSkills` repo.

## Scope gaps

- No Windows or macOS CI matrix.
- Hub settings schema bumps are one-way (additive fields only); downgrading to an older Hub after a schema bump is not supported.
- `buf breaking` is run manually rather than on every push — wire contract parity relies on reviewer discipline.
*)
