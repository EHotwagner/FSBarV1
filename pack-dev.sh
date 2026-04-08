#!/usr/bin/env bash
set -euo pipefail

# Pack FSBar.Proto, FSBar.Client, and FSBar.Viz with a timestamp-based
# prerelease version into the local nupkg/ feed.
# Usage: ./pack-dev.sh [target-dir]
#   target-dir: Directory to place the .nupkg files (default: ./nupkg)

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TARGET_DIR="${1:-$SCRIPT_DIR/nupkg}"
SUFFIX="dev.$(date +%Y%m%dT%H%M%S)"

PROJECTS=(
    "$SCRIPT_DIR/src/FSBar.Proto/FSBar.Proto.fsproj"
    "$SCRIPT_DIR/src/FSBar.Client/FSBar.Client.fsproj"
    "$SCRIPT_DIR/src/FSBar.Viz/FSBar.Viz.fsproj"
)

mkdir -p "$TARGET_DIR"

# Remove old dev versions of FSBar packages from target
rm -f "$TARGET_DIR"/FSBar.Proto.*.nupkg
rm -f "$TARGET_DIR"/FSBar.Client.*.nupkg
rm -f "$TARGET_DIR"/FSBar.Viz.*.nupkg

for proj in "${PROJECTS[@]}"; do
    name=$(basename "$proj" .fsproj)
    echo "Packing $name..."
    dotnet pack "$proj" --version-suffix "$SUFFIX" -o "$TARGET_DIR" -c Debug
done

echo "Packed FSBar.{Proto,Client,Viz} with suffix ${SUFFIX} to ${TARGET_DIR}"
