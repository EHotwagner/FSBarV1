# Implementation Plan: Minimal Container Setup for BAR Development

**Branch**: `012-minimal-container-setup` | **Date**: 2026-04-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/012-minimal-container-setup/spec.md`

## Summary

Strip the existing `Containerfile.emacs` down to a minimal consumer-facing image that includes only the tools needed for FSBar development (.NET 10.0, fsautocomplete, fantomas, upcons, specify-cli, FSI MCP server) with three project repos (FSBarV1, HighBarV2, SkiaViewer). Provide documentation covering build commands, BAR game folder mounting, GPU passthrough, X11 display forwarding, and optional Claude Code agent setup.

## Technical Context

**Language/Version**: Containerfile (OCI/Docker format), Bash (entrypoint), Markdown (documentation)
**Primary Dependencies**: Arch Linux base image, .NET 10.0 SDK, Node.js, GitHub CLI, FSI MCP server
**Storage**: N/A (container image layers + host bind mounts at runtime)
**Testing**: Manual build-and-run verification (no automated test suite for container setup)
**Target Platform**: Linux host with Podman or Docker, X11 display server
**Project Type**: Container image + documentation (infrastructure/devops)
**Performance Goals**: Image build time < 10 minutes; image size ≥ 30% smaller than original
**Constraints**: Must use Arch Linux base for BAR engine compatibility; X11 only (no Wayland)
**Scale/Scope**: Single Containerfile + setup documentation; consumer-facing

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The constitution governs F# code projects. This feature produces a Containerfile and documentation — no F# source code, no public API surface, no .fsi files, no behavioral code changes. The following gates are evaluated:

| Gate | Status | Notes |
|------|--------|-------|
| I. Spec-First Delivery | **PASS** | Spec and plan exist prior to implementation |
| II. Compiler-Enforced Structural Contracts | **N/A** | No F# code or public API surface introduced |
| III. Test Evidence Is Mandatory | **N/A** | No behavior-changing code; manual verification via build/run |
| IV. Observability and Safe Failure Handling | **N/A** | No runtime code; entrypoint is passthrough |
| V. Scripting Accessibility | **N/A** | No F# library or API |
| F# exclusive stack | **N/A** | Containerfile is infrastructure, not an F# project |
| .fsi signature files | **N/A** | No F# modules |
| Surface-area baselines | **N/A** | No public API |
| Dependencies minimized | **PASS** | Feature explicitly removes unnecessary dependencies |

**Result**: All applicable gates pass. N/A gates are correctly scoped out — this is infrastructure/documentation, not F# library code.

## Project Structure

### Documentation (this feature)

```text
specs/012-minimal-container-setup/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (minimal — no data model)
├── quickstart.md        # Phase 1 output (primary documentation deliverable)
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
container/
├── Containerfile            # Minimal consumer-facing Containerfile
├── entrypoint.sh            # Container entrypoint script (simplified)
└── README.md                # Setup documentation: build, run, GPU, mounts, optional Claude Code
```

**Structure Decision**: All container-related files live in a `container/` directory at the repo root. This keeps infrastructure artifacts separate from F# source code. The README.md in this directory is the primary consumer documentation (FR-007, FR-012).

## Complexity Tracking

No constitution violations to justify — all applicable gates pass.

## Packages Removed vs Retained

### Removed from original Containerfile

**Pacman packages**: emacs, wmctrl, python-pyqt6, python-pyqt6-webengine, python-pyqt6-sip, qt6-multimedia, qt6-svg, firefox, nushell, fuse2, gtk3, xorg-xeyes

**.NET global tools**: fable, fake-cli, paket, fsdocs-tool, aspire.cli

**Node tools**: vite (npm entirely removable if no Node tools needed)

**Agent tooling**: FSAgents commands, skills.list, agent skills cloning, settings.json discovery, update-agent-skills.sh service

**Project repos**: BPEWrapper, PScriptingV2, PhysicsSandbox

### Retained

**Pacman packages**: base-devel, curl, wget, git, jq, sudo, openssh, nodejs, npm, dotnet-sdk, aspnet-runtime, github-cli, python, python-pip, ttf-liberation, fontconfig, openal, sdl2, alsa-lib, nss

**AUR packages**: freeimage

**.NET global tools**: fsautocomplete, fantomas

**Python tools**: specify-cli (spec-kit), uv

**Custom tools**: upcons (Constitutions repo), FSI MCP server

**Project repos**: FSBarV1, HighBarV2, SkiaViewer

**Infrastructure**: Claude Code CLI install (kept as optional — consumer may use it), git identity setup (parameterized via build arg)
