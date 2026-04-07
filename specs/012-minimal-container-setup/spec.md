# Feature Specification: Minimal Container Setup for BAR Development

**Feature Branch**: `012-minimal-container-setup`  
**Created**: 2026-04-07  
**Status**: Draft  
**Input**: User description: "create a minimal docker/podman installation. this is your current containerfile: https://github.com/EHotwagner/SystemAdmin/blob/main/Containers/Containerfile.emacs rip out anything unnecessary and add installation instructions for the mounted bar folder/installation, gpu passthrough and that fsi-mcp should be considered."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Build and Run Minimal Container (Priority: P1)

A developer wants to build a container image from the streamlined Containerfile and launch it with the BAR game installation mounted from the host, so they can begin F# development against the live game engine without manually installing dependencies.

**Why this priority**: This is the core deliverable — without a working minimal container, nothing else matters.

**Independent Test**: Can be fully tested by building the image with `podman build` and running `podman run` with the BAR mount, then verifying that `dotnet --version`, `fsautocomplete --version`, and the FSI MCP server start successfully inside the container.

**Acceptance Scenarios**:

1. **Given** the Containerfile and a host with Podman or Docker installed, **When** the developer runs the build command, **Then** the image builds successfully with no errors and is significantly smaller than the original emacs-based image.
2. **Given** a built container image, **When** the developer runs the container with the documented mount flags, **Then** the BAR game folder is accessible inside the container at the expected path.
3. **Given** a running container with the BAR folder mounted, **When** the developer starts the FSI MCP server, **Then** it binds to port 5020 and responds to connections.

---

### User Story 2 - GPU Passthrough for Graphical Engine (Priority: P2)

A developer wants to run the BAR game engine (graphical mode) inside the container with GPU acceleration, so they can use the SkiaViewer visualization and windowed game rendering.

**Why this priority**: GPU passthrough is essential for graphical workflows (viz, game engine) but the container can still be useful for headless/FSI work without it.

**Independent Test**: Can be fully tested by running `glxinfo | grep renderer` inside the container and confirming the host GPU is visible, then launching a simple OpenGL window (e.g., `xeyes` or SkiaViewer).

**Acceptance Scenarios**:

1. **Given** a host with a GPU and appropriate drivers, **When** the developer runs the container with the documented GPU passthrough flags, **Then** OpenGL rendering works inside the container using the host GPU.
2. **Given** GPU passthrough is configured, **When** the developer launches the graphical engine in windowed mode, **Then** the game window appears on the host display without fallback to software rendering.

---

### User Story 3 - FSI MCP Server Integration (Priority: P2)

A developer wants the FSI MCP server to be pre-built and ready to launch inside the container, so they can use F# Interactive with MCP tooling immediately after container startup.

**Why this priority**: The FSI MCP server is a key development tool for this project's workflow and must work out of the box.

**Independent Test**: Can be fully tested by starting the container and running the FSI MCP server start command, then sending a test F# expression via the MCP protocol.

**Acceptance Scenarios**:

1. **Given** a freshly started container, **When** the developer runs the FSI MCP server start command, **Then** it starts without needing additional builds or dependency installation.
2. **Given** the FSI MCP server is running, **When** the developer sends F# code that references FSBar assemblies from the mounted BAR build output, **Then** the code executes successfully.

---

### User Story 4 - Clear Setup Documentation (Priority: P3)

A developer new to the project wants step-by-step instructions covering: building the image, running the container with all required flags (mounts, GPU, ports, display), and verifying the environment works.

**Why this priority**: Documentation ensures reproducibility and onboarding, but is secondary to the container itself working.

**Independent Test**: Can be fully tested by giving the documentation to a developer unfamiliar with the setup and verifying they can follow it to a working environment without additional guidance.

**Acceptance Scenarios**:

1. **Given** the documentation, **When** a developer follows the build instructions, **Then** they produce a working container image on the first attempt.
2. **Given** the documentation, **When** a developer follows the run instructions with all flags, **Then** they have a container with BAR mounted, GPU passthrough active, display forwarding working, and the FSI MCP server launchable.

---

### Edge Cases

- What happens when the host has no GPU or incompatible drivers? The container should still start and work for headless/FSI workflows; documentation should note GPU passthrough is optional.
- What happens when the BAR game folder is not present at the expected host path? The container should start but clearly indicate (via docs or startup message) that the mount is missing.
- What happens when port 5020 is already in use on the host? Documentation should note port mapping options.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Containerfile MUST produce a working container with .NET 10.0, Node.js, and the FSI MCP server pre-built. Claude Code CLI is optional and documented separately.
- **FR-002**: The Containerfile MUST remove packages not needed for BAR/FSBar development (emacs, wmctrl, PyQt6, firefox, nushell, fuse2, gtk3, and related GUI/editor packages from the original).
- **FR-010**: The Containerfile MUST clone exactly three project repos into the image: FSBarV1, HighBarV2, and SkiaViewer. All other repos from the original Containerfile (BPEWrapper, PScriptingV2, PhysicsSandbox) MUST be removed.
- **FR-011**: The Containerfile MUST NOT include Claude Code agent tooling (FSAgents commands, agent skills, skills.list, settings.json discovery, or the update-agent-skills service).
- **FR-012**: The documentation MUST include a section describing the optional Claude Code agent infrastructure (FSAgents, skills, MCP server configuration) and how consumers can set it up if they choose to use Claude Code.
- **FR-013**: The Containerfile MUST install only fsautocomplete and fantomas as .NET global tools. All other global tools (fable, fake-cli, paket, fsdocs-tool, aspire.cli) and Node tools (vite) MUST be removed.
- **FR-014**: The Containerfile MUST retain the upcons tool (Constitutions repo) and the specify-cli (spec-kit).
- **FR-003**: The container MUST support mounting the host BAR game installation folder (including engine binaries, maps, and data) at a documented path inside the container.
- **FR-004**: The container MUST support GPU passthrough via documented run flags so the graphical engine and SkiaViewer can use hardware-accelerated OpenGL rendering.
- **FR-005**: The container MUST expose the required ports (minimally 5020 for FSI MCP server, and any ports needed for game engine communication).
- **FR-006**: The container MUST include display forwarding support (X11) so graphical windows (game engine in windowed mode, SkiaViewer) can render on the host display.
- **FR-007**: The documentation MUST provide complete build and run commands for both Docker and Podman, including all required flags for mounts, GPU, display, and ports.
- **FR-008**: The Containerfile MUST retain the FSI MCP server clone-and-build step, including the .NET 10.0 target framework patching.
- **FR-009**: The Containerfile MUST retain essential native libraries required by the BAR engine and SkiaSharp (OpenAL, SDL2, libfreeimage).

### Key Entities

- **Container Image**: The built artifact from the minimal Containerfile, containing all development tools and pre-built FSI MCP server.
- **BAR Installation**: The host-side Beyond All Reason game folder (engine binaries, maps, game data) mounted into the container at runtime.
- **FSI MCP Server**: The F# Interactive MCP server pre-built in the container, providing REPL capabilities via MCP protocol on port 5020.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The container image builds successfully and is at least 30% smaller than the original emacs-based image (measured by compressed layer size).
- **SC-002**: A developer can go from cloning the repo to a working development environment in under 10 minutes (build + first run).
- **SC-003**: The FSI MCP server starts and accepts connections within 10 seconds of being launched inside the container.
- **SC-004**: Graphical applications (SkiaViewer, game engine in windowed mode) render on the host display when GPU passthrough is configured.
- **SC-005**: All documented setup steps complete without errors on a standard Linux host with Podman or Docker installed.

## Clarifications

### Session 2026-04-07

- Q: How should project source code repos be handled — cloned into image or mounted at runtime? → A: Clone only the 3 required repos (FSBarV1, HighBarV2, SkiaViewer) into the image. This installation targets consumers, not the original developer.
- Q: Should Claude Code agent infrastructure (FSAgents, skills, update service) be retained? → A: Remove all agent tooling from the Containerfile. Add documentation about agent infrastructure for consumers who choose to use Claude Code.
- Q: Which .NET/Node global tools should remain? → A: Keep only fsautocomplete and fantomas. Remove fable, fake-cli, paket, fsdocs-tool, aspire.cli, and vite.
- Q: Should upcons and specify-cli be included for consumers? → A: Keep both upcons and specify-cli in the image.

## Assumptions

- The host machine runs Linux with Podman or Docker installed (Windows/macOS GPU passthrough is out of scope).
- The host has an X11 display server (Wayland is out of scope for initial version).
- The BAR game installation already exists on the host at a known path (the container does not install the game).
- The host GPU drivers (NVIDIA or Mesa/AMD) are already installed and working on the host.
- The Containerfile will continue to use Arch Linux as the base image for package compatibility with the BAR engine.
- Claude Code CLI and GitHub CLI authentication tokens are provided at build time or runtime as in the original Containerfile.
- This container is intended for consumers (end users), not the original developer. Setup should be self-contained and require minimal external knowledge.
