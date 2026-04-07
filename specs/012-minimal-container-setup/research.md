# Research: Minimal Container Setup

**Feature**: 012-minimal-container-setup | **Date**: 2026-04-07

## R1: GPU Passthrough Flags (Docker/Podman)

**Decision**: Use `--device /dev/dri` for Mesa/AMD GPUs and `--gpus all` (Docker) or `--device nvidia.com/gpu=all` (Podman with CDI) for NVIDIA GPUs. Both require host drivers installed.

**Rationale**: `/dev/dri` is the standard Linux DRM device path and works universally for Mesa-based drivers (Intel, AMD). NVIDIA requires the NVIDIA Container Toolkit for Docker or CDI device configuration for Podman. Documenting both paths covers the primary GPU vendors.

**Alternatives considered**:
- `--privileged` mode: Grants full device access but is a security risk. Rejected.
- Manual device node mapping (`--device /dev/nvidia0`): Fragile, varies by GPU count. Rejected in favor of toolkit-based approaches.

## R2: X11 Display Forwarding

**Decision**: Mount the X11 socket (`-v /tmp/.X11-unix:/tmp/.X11-unix`) and pass `DISPLAY` environment variable (`-e DISPLAY=$DISPLAY`). Set `XDG_RUNTIME_DIR=/tmp/runtime-developer` inside the container.

**Rationale**: This is the standard approach for X11 forwarding into containers. The CLAUDE.md already documents that `XDG_RUNTIME_DIR` and `DISPLAY=:0` are required for GLFW windowing (Silk.NET). The entrypoint should create the XDG_RUNTIME_DIR if it doesn't exist.

**Alternatives considered**:
- Wayland passthrough: Out of scope per assumptions.
- VNC/remote display: Adds complexity and latency. Rejected for local development use case.

## R3: BAR Game Folder Mount Path

**Decision**: Mount the host BAR installation at `/home/developer/.local/state/Beyond All Reason` inside the container — matching the existing path convention used by the BAR engine and documented in CLAUDE.md.

**Rationale**: The engine headless binary path and spring data dir in CLAUDE.md reference this path. Using the same path inside the container avoids reconfiguring engine paths.

**Alternatives considered**:
- Mount at a custom path (e.g., `/bar`): Simpler mount command but requires path remapping in all engine configurations. Rejected.

## R4: Entrypoint Simplification

**Decision**: Simplify the entrypoint to create `XDG_RUNTIME_DIR`, optionally start the FSI MCP server if requested, and exec the user's command. Remove the update-agent-skills service loop.

**Rationale**: The agent skills update service is removed per clarification. The entrypoint should be minimal and transparent — consumers should understand what runs at startup.

**Alternatives considered**:
- No entrypoint (just CMD): Loses the XDG_RUNTIME_DIR creation. Rejected.
- Full service manager (supervisord): Overkill for this use case. Rejected.

## R5: Node.js Necessity

**Decision**: Retain Node.js and npm. They are needed by Claude Code CLI (which consumers may optionally install) and potentially by downstream tooling.

**Rationale**: Claude Code CLI requires Node.js. Even though Claude Code is optional for consumers, removing Node.js would make the optional setup path fail. The size overhead is acceptable.

**Alternatives considered**:
- Remove Node.js entirely: Breaks Claude Code CLI installation path. Rejected.

## R6: Port Exposure

**Decision**: Expose only ports 5020 (FSI MCP server) and 8080 (general-purpose). Remove the 10 exposed ports from the original Containerfile.

**Rationale**: The original exposes 10 ports (4173, 5000, 5001, 5020, 5137, 5173, 8080, 8081, 18888, 50051) — most are for development workflows not relevant to consumers. Port 5020 is required for FSI MCP. Port 8080 is kept as a conventional general-purpose port. Consumers can map additional ports as needed.

**Alternatives considered**:
- Keep all original ports: Clutters the image and confuses consumers about what's actually used. Rejected.
- Expose only 5020: Too restrictive if consumers need a web port. Rejected.
