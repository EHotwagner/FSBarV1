# BAR (Beyond All Reason) Environment Setup

> Upstream source: [HighBarV2/docs/bar-info.md](https://github.com/EHotwagner/HighBarV2/blob/master/docs/bar-info.md)

## Installation

- **Install dir**: `/home/developer/.local/state/Beyond All Reason/` (engine binaries + game data; bind-mounted from host in containers)
- **Headless binary**: `~/.local/state/Beyond All Reason/engine/recoil_<version>/spring-headless`
- **Graphical binary**: `~/.local/state/Beyond All Reason/engine/recoil_<version>/spring`
  - **Must be started in windowed mode** (not fullscreen) — see `EngineLauncher` which writes `Fullscreen=0` to `springsettings.cfg`

## Directory Layout

| Path | Contents |
|------|----------|
| `~/.local/state/Beyond All Reason/` | BAR data directory (writeable) |
| `~/.local/state/Beyond All Reason/engine/recoil_2025.06.19/` | Recoil engine (v2025.06.19) |
| `~/.local/state/Beyond All Reason/engine/recoil_2025.06.19/spring-headless` | Headless engine binary (for testing) |
| `~/.local/state/Beyond All Reason/engine/recoil_2025.06.19/AI/Skirmish/` | Skirmish AI install directory |
| `~/.local/state/Beyond All Reason/packages/` | Game packages (rapid `.sdp` files) |
| `~/.local/state/Beyond All Reason/pool/` | Rapid pool (gzipped content-addressed files) |
| `~/.local/state/Beyond All Reason/maps/` | Downloaded maps (`.sd7` files) |
| `~/.local/state/Beyond All Reason/LuaMenu/Config/IGL_data.lua` | Chobby lobby persistent settings |
| `~/.local/state/Beyond All Reason/springsettings.cfg` | Engine settings |
| `~/.local/state/Beyond All Reason/infolog.txt` | Engine/lobby log (check for errors) |

## Registering a Custom Skirmish AI

Three things are required for a C Skirmish AI to appear in the Chobby lobby:

### 1. Install AI files into the engine directory

```
~/.local/state/Beyond All Reason/engine/recoil_2025.06.19/AI/Skirmish/<AIName>/<version>/
  AIInfo.lua
  AIOptions.lua
  libSkirmishAI.so
```

For HighBarV2:

```bash
ENGINE_AI_DIR="$HOME/.local/state/Beyond All Reason/engine/recoil_2025.06.19/AI/Skirmish/HighBarV2/0.1"
mkdir -p "$ENGINE_AI_DIR"
cp build/libSkirmishAI.so "$ENGINE_AI_DIR/"
cp proxy/data/AIInfo.lua "$ENGINE_AI_DIR/"
cp proxy/data/AIOptions.lua "$ENGINE_AI_DIR/"
```

### 2. Enable developer mode

Create an empty `devmode.txt` in the BAR data directory:

```bash
touch "$HOME/.local/state/Beyond All Reason/devmode.txt"
```

This enables the **Developer** tab in Chobby's Settings menu.

### 3. Disable "Simplified AI List"

The lobby filters out unknown AIs by default. Disable this by setting
`simpleAiList = false` in the Chobby config:

**Via UI**: Settings > Developer > uncheck "Simplified AI List"

**Via config file** (`~/.local/state/Beyond All Reason/LuaMenu/Config/IGL_data.lua`):

```lua
simpleAiList = false,
```

After changing this setting, restart the lobby. The AI will then appear in the
bot dropdown when setting up a custom/skirmish game.

## How Chobby AI Discovery Works

1. **`VFS.GetAvailableAIs(gameName)`** queries the engine for C Skirmish AIs
   installed in `AI/Skirmish/`. This returns AIs like BARb, NullAI, HighBarV2.

2. **`unversionedGameAis`** (defined in `mainconfig.lua`) adds Lua-based AIs
   (SimpleAI, ScavengersAI, etc.) that live inside the game archive.

3. **`aiBlacklist`** (defined in `aiBlacklist.lua`) removes specific AIs from
   the list (e.g., CppTestAI, old deprecated AIs).

4. **`simpleAiList` filtering**: When enabled (default), only AIs listed in
   `aisimplename.lua` are shown. AIs not in this whitelist are hidden.
   Disabling this shows all detected AIs.

5. **`aisimplename.lua`** provides display names, sort order, and tooltips for
   the AI dropdown.

## Important: Do NOT Patch Pool Files

The rapid pool stores game content as gzipped, content-addressed files
(`pool/<2-char-prefix>/<rest-of-md5>.gz`). Patching these files directly
**will crash Chobby** because the content no longer matches the expected
checksums in the `.sdp` package index.

The correct approach for overriding lobby behavior is either:
- Change settings in `IGL_data.lua` (for toggle-style options)
- Use VFS filesystem overrides in `LuaMenu/` (raw filesystem takes priority
  over pool in the VFS load order `rMmeb`)

## Build & Install Cheat Sheet

```bash
# 1. Generate protobuf C sources
cd proto && buf generate && cd ..

# 2. Build proxy
cmake -B build -DCMAKE_BUILD_TYPE=Release proxy/
cmake --build build

# 3. Install into engine
ENGINE_AI_DIR="$HOME/.local/state/Beyond All Reason/engine/recoil_2025.06.19/AI/Skirmish/HighBarV2/0.1"
mkdir -p "$ENGINE_AI_DIR"
cp build/libSkirmishAI.so "$ENGINE_AI_DIR/"
cp proxy/data/AIInfo.lua "$ENGINE_AI_DIR/"
cp proxy/data/AIOptions.lua "$ENGINE_AI_DIR/"

# 4. Enable devmode + disable simplified AI list (one-time)
touch "$HOME/.local/state/Beyond All Reason/devmode.txt"
sed -i 's/simpleAiList = true/simpleAiList = false/' \
  "$HOME/.local/state/Beyond All Reason/LuaMenu/Config/IGL_data.lua"

# 5. Launch (graphical engine, windowed)
"$HOME/.local/state/Beyond All Reason/engine/recoil_2025.06.19/spring" --window
```
