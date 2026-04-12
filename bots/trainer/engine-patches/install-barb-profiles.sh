#!/usr/bin/env bash
# install-barb-profiles.sh — idempotently patch BARb AIOptions.lua in every installed engine.
#
# Background: BARb ships with the easy/medium/hard difficulty profile items commented out,
# so the engine rejects `profile=easy` (etc.) as an unknown option. The trainer ladder depends
# on those profiles being selectable, so we copy an in-repo patched AIOptions.lua over the
# engine's copy. A .bak backup of the original is kept on first patch.
#
# Usage: bash bots/trainer/engine-patches/install-barb-profiles.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PATCH_SRC="$SCRIPT_DIR/BARb_AIOptions.lua"

if [[ ! -f "$PATCH_SRC" ]]; then
  echo "ERROR: patch source not found: $PATCH_SRC" >&2
  exit 1
fi

ENGINE_ROOT="$HOME/.local/state/Beyond All Reason/engine"
if [[ ! -d "$ENGINE_ROOT" ]]; then
  echo "ERROR: BAR engine directory not found: $ENGINE_ROOT" >&2
  exit 1
fi

echo "→ Detecting BAR engine versions under $ENGINE_ROOT ..."

patched=0
skipped=0
found=0

while IFS= read -r -d '' target; do
  found=$((found + 1))
  engine_ver="$(basename "$(dirname "$(dirname "$(dirname "$(dirname "$(dirname "$target")")")")")")"
  if cmp -s "$PATCH_SRC" "$target"; then
    echo "→ $engine_ver ... already patched (no-op)"
    skipped=$((skipped + 1))
  else
    if [[ ! -f "${target}.bak" ]]; then
      cp -- "$target" "${target}.bak"
      echo "→ $engine_ver ... backup written to $(basename "$target").bak"
    fi
    cp -- "$PATCH_SRC" "$target"
    echo "→ $engine_ver ... patched OK"
    patched=$((patched + 1))
  fi
done < <(find "$ENGINE_ROOT" -path "*/AI/Skirmish/BARb/stable/AIOptions.lua" -print0)

if [[ $found -eq 0 ]]; then
  echo "WARNING: no BARb AIOptions.lua files found — is BARb installed?" >&2
  exit 2
fi

echo "→ Done. BARb profiles available: dev, easy, medium, hard."
echo "→ Summary: found=$found patched=$patched skipped=$skipped"
