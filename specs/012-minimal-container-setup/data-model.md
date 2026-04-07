# Data Model: Minimal Container Setup

**Feature**: 012-minimal-container-setup | **Date**: 2026-04-07

## Overview

This feature has no application data model. The "entities" are infrastructure artifacts:

## Entities

### Container Image

- **Base**: `archlinux:latest`
- **Build args**: `GH_TOKEN` (GitHub access token for repo cloning)
- **Layers**: System packages → User setup → AUR packages → Dev tools → .NET/Python tools → FSI MCP server → Git identity → Project repos → Entrypoint
- **State**: Immutable after build. No persistent state inside the container.

### Host Bind Mounts (runtime)

| Mount Source (host) | Mount Target (container) | Purpose |
|---------------------|--------------------------|---------|
| BAR game folder | `/home/developer/.local/state/Beyond All Reason` | Engine binaries, maps, game data |
| X11 socket | `/tmp/.X11-unix` | Display forwarding |
| GPU devices | `/dev/dri` (or NVIDIA devices) | Hardware-accelerated rendering |

### Exposed Ports

| Port | Service |
|------|---------|
| 5020 | FSI MCP Server (SSE transport) |
| 8080 | General-purpose (user-defined) |

## Relationships

- Container Image **requires** Host Bind Mounts at runtime for BAR engine and graphical features.
- FSI MCP Server **depends on** .NET 10.0 SDK and the cloned fsi-mcp-server repo (pre-built in image).
- GPU passthrough **depends on** host drivers and X11 socket mount.
