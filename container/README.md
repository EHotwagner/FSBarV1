# FSBar Development Container

Minimal container setup for Beyond All Reason (BAR) F# development.

## Prerequisites

- **Linux host** with [Podman](https://podman.io/) or [Docker](https://docs.docker.com/engine/install/) installed
- **BAR game installation** on the host (engine binaries, maps, game data)
- **GitHub personal access token** for cloning repos during build
- **GPU drivers** (optional) — NVIDIA or Mesa/AMD, required only for graphical workflows
- **X11 display server** (optional) — required for graphical windows (SkiaViewer, game engine)

## Build

```bash
# With Podman
podman build --build-arg GH_TOKEN=<your-github-token> \
  -t fsbar-dev -f Containerfile .

# With Docker
docker build --build-arg GH_TOKEN=<your-github-token> \
  -t fsbar-dev -f Containerfile .
```

## Run

### Headless mode (FSI/CLI only, no GPU needed)

```bash
podman run -it --rm \
  -v "<path-to-BAR>:/home/developer/.local/state/Beyond All Reason" \
  -p 5020:5020 \
  fsbar-dev
```

Replace `<path-to-BAR>` with your BAR game installation path (e.g., `~/.local/state/Beyond All Reason`).

### Graphical mode — Mesa/AMD/Intel GPU

```bash
podman run -it --rm \
  -v "<path-to-BAR>:/home/developer/.local/state/Beyond All Reason" \
  -v /tmp/.X11-unix:/tmp/.X11-unix \
  -e DISPLAY=$DISPLAY \
  --device /dev/dri \
  -p 5020:5020 \
  fsbar-dev
```

### Graphical mode — NVIDIA GPU

Requires the [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/latest/install-guide.html).

```bash
# Docker (recommended for NVIDIA)
docker run -it --rm \
  -v "<path-to-BAR>:/home/developer/.local/state/Beyond All Reason" \
  -v /tmp/.X11-unix:/tmp/.X11-unix \
  -e DISPLAY=$DISPLAY \
  --gpus all \
  -p 5020:5020 \
  fsbar-dev

# Podman (with CDI support)
podman run -it --rm \
  -v "<path-to-BAR>:/home/developer/.local/state/Beyond All Reason" \
  -v /tmp/.X11-unix:/tmp/.X11-unix \
  -e DISPLAY=$DISPLAY \
  --device nvidia.com/gpu=all \
  -p 5020:5020 \
  fsbar-dev
```

## Verification

Run these inside the container to verify the environment:

```bash
# Core tools
dotnet --version          # Should show 10.0.x
git --version
fsautocomplete --version  # F# language server
specify --help            # Spec-kit CLI
upcons --help             # Constitutions tool

# BAR mount
ls ~/.local/state/Beyond\ All\ Reason/engine/
# Should list engine directories

# GPU (graphical mode only)
glxinfo | grep "OpenGL renderer"
# Should show your GPU name, NOT "llvmpipe" or "software rasterizer"
```

## FSI MCP Server

The FSI MCP server is pre-built inside the container. To start it:

```bash
cd ~/tools/fsi-mcp-server/server
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 dotnet run --no-build
# Binds to http://127.0.0.1:5020/sse
```

### Loading FSBar assemblies in FSI

Before loading `#r` references, preload native libraries:

```fsharp
open System.Runtime.InteropServices
[<DllImport("libdl.so.2")>]
extern nativeint dlopen(string filename, int flags)

// Adjust path to your built test output directory
let np = "/home/developer/projects/FSBarV1/tests/FSBar.Viz.Tests/bin/Debug/net10.0/runtimes/linux-x64/native"
let _ = dlopen(np + "/libglfw.so.3", 0x2 ||| 0x100)
let _ = dlopen(np + "/libSkiaSharp.so", 0x2 ||| 0x100)
```

Then load DLLs from the test output directory (has all transitive dependencies):

```fsharp
#r "/home/developer/projects/FSBarV1/tests/FSBar.Viz.Tests/bin/Debug/net10.0/<DllName>.dll"
```

> **Note**: FSI locks DLLs loaded via `#r`. After rebuilding a project, restart FSI to pick up new DLLs.

## Optional: Claude Code Agent Setup

If you want to use [Claude Code](https://claude.com/claude-code) as your development agent:

1. **Install Claude Code CLI**:
   ```bash
   curl -fsSL https://claude.ai/install.sh | bash
   ```

2. **Clone FSAgents commands**:
   ```bash
   git clone --depth 1 https://github.com/EHotwagner/FSAgents.git ~/tools/FSAgents
   mkdir -p ~/.claude/commands
   cp ~/tools/FSAgents/*.md ~/.claude/commands/
   ```

3. **Set up agent skills** — clone skill repos and generate settings:
   ```bash
   # Clone each skill repo into ~/tools/
   # Example: git clone --depth 1 <skill-repo-url> ~/tools/<skill-name>

   # Generate settings.json with discovered skills
   SKILLS=$(find ~/tools -name "SKILL.md" -type f \
       ! -path "*/Constitutions/*" ! -path "*/FSAgents/*" \
       -exec dirname {} \; | jq -R . | jq -s .)
   echo "{\"permissions\":{},\"agentSkills\":$SKILLS}" > ~/.claude/settings.json
   ```

4. **Configure FSI MCP server** in your Claude Code MCP settings to point to `http://127.0.0.1:5020/sse`.

## Troubleshooting

- **No GPU detected / software rendering**: Ensure host GPU drivers are installed and `/dev/dri` exists. For NVIDIA, install the [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/latest/install-guide.html).
- **Display not forwarding**: Run `xhost +local:` on the host to allow container X11 connections.
- **Port 5020 already in use**: Map to a different host port: `-p 5021:5020`.
- **BAR folder not found**: Verify the mount source path matches your BAR installation location. The path inside the container must be `/home/developer/.local/state/Beyond All Reason`.

## Port Reference

| Port | Service | Required |
|------|---------|----------|
| 5020 | FSI MCP Server (SSE) | Yes |
| 8080 | General-purpose | No |
