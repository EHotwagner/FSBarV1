# Quickstart: Minimal Container Setup for BAR Development

**Feature**: 012-minimal-container-setup | **Date**: 2026-04-07

## Prerequisites

- Linux host with **Podman** or **Docker** installed
- X11 display server running
- BAR (Beyond All Reason) game installation on the host
- GPU drivers installed (NVIDIA or Mesa/AMD) — optional, needed for graphical workflows
- A GitHub personal access token (for cloning private repos during build)

## 1. Build the Image

```bash
# With Podman
podman build --build-arg GH_TOKEN=<your-github-token> -t fsbar-dev -f container/Containerfile .

# With Docker
docker build --build-arg GH_TOKEN=<your-github-token> -t fsbar-dev -f container/Containerfile .
```

## 2. Run the Container

### Headless mode (FSI/CLI only, no GPU needed)

```bash
podman run -it --rm \
  -v /path/to/Beyond-All-Reason:/home/developer/.local/state/Beyond\ All\ Reason \
  -p 5020:5020 \
  fsbar-dev
```

### Graphical mode (GPU + X11 display forwarding)

**For Mesa/AMD/Intel GPU:**
```bash
podman run -it --rm \
  -v /path/to/Beyond-All-Reason:/home/developer/.local/state/Beyond\ All\ Reason \
  -v /tmp/.X11-unix:/tmp/.X11-unix \
  -e DISPLAY=$DISPLAY \
  --device /dev/dri \
  -p 5020:5020 \
  fsbar-dev
```

**For NVIDIA GPU (requires NVIDIA Container Toolkit):**
```bash
docker run -it --rm \
  -v /path/to/Beyond-All-Reason:/home/developer/.local/state/Beyond\ All\ Reason \
  -v /tmp/.X11-unix:/tmp/.X11-unix \
  -e DISPLAY=$DISPLAY \
  --gpus all \
  -p 5020:5020 \
  fsbar-dev
```

## 3. Verify the Environment

Inside the container:

```bash
# Check core tools
dotnet --version          # Should show 10.0.x
git --version             # Should be available
fsautocomplete --version  # F# language server

# Check BAR mount
ls ~/.local/state/Beyond\ All\ Reason/engine/
# Should list engine directories

# Start FSI MCP server
cd ~/tools/fsi-mcp-server/server
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 dotnet run --no-build
# Should bind to http://127.0.0.1:5020/sse

# Verify GPU (graphical mode only)
glxinfo | grep "OpenGL renderer"
# Should show your GPU, not "llvmpipe" or "software"
```

## 4. Optional: Claude Code Agent Setup

If you want to use Claude Code as your development agent:

1. **Install Claude Code CLI** (already in the image if retained):
   ```bash
   claude --version
   ```

2. **Clone FSAgents commands**:
   ```bash
   git clone --depth 1 https://github.com/EHotwagner/FSAgents.git ~/tools/FSAgents
   mkdir -p ~/.claude/commands
   cp ~/tools/FSAgents/*.md ~/.claude/commands/
   ```

3. **Set up agent skills** (clone repos listed in your `skills.list`):
   ```bash
   # For each skill repo:
   git clone --depth 1 <skill-repo-url> ~/tools/<skill-name>
   ```

4. **Configure settings.json**:
   ```bash
   # Discover SKILL.md files and generate settings
   SKILLS=$(find ~/tools -name "SKILL.md" -type f -exec dirname {} \; | jq -R . | jq -s .)
   echo "{\"permissions\":{},\"agentSkills\":$SKILLS}" > ~/.claude/settings.json
   ```

5. **Configure FSI MCP server** in your Claude Code MCP settings to point to `http://127.0.0.1:5020/sse`.

## Port Reference

| Port | Service | Required |
|------|---------|----------|
| 5020 | FSI MCP Server | Yes (for F# Interactive) |
| 8080 | General-purpose | No (map if needed) |

## Troubleshooting

- **No GPU detected**: Ensure host GPU drivers are installed and `/dev/dri` exists. For NVIDIA, install the NVIDIA Container Toolkit.
- **Display not forwarding**: Run `xhost +local:` on the host to allow container X11 connections.
- **Port 5020 in use**: Map to a different host port: `-p 5021:5020`
- **BAR folder not found**: Verify the mount source path matches your BAR installation location.
