#!/usr/bin/env bash
# refresh-all.sh — Regenerate every committed map-cache file under
# bots/trainer/map-cache/. Single entry point per feature 026 FR-004.
#
# The canonical list of supported maps lives in
# src/FSBar.Client/MapCacheFile.fs (MapCacheFile.supportedMaps). This
# wrapper reads it via scripts/examples/15-list-supported-maps.fsx so
# there is no hand-maintained map list here (FR-008).
#
# Per-map exit codes from 14-cache-map-analysis.fsx:
#   0 — refreshed successfully
#   3 — map's .sd7 is not installed on this machine (skip with warning)
#   other — hard failure (abort)
#
# Overall exit code: 0 iff at least one map was refreshed successfully;
# non-zero otherwise so the contributor notices when nothing happened.

set -u

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

cd "$REPO_ROOT"

refreshed=0
skipped=0

while IFS= read -r map_name; do
    [ -z "$map_name" ] && continue
    echo
    echo "=== Refreshing: $map_name ==="
    set +e
    dotnet fsi scripts/examples/14-cache-map-analysis.fsx "$map_name"
    rc=$?
    set -e 2>/dev/null || true
    case "$rc" in
        0)
            refreshed=$((refreshed + 1))
            ;;
        3)
            echo "[warn] $map_name — .sd7 not installed locally, skipping"
            skipped=$((skipped + 1))
            ;;
        *)
            echo "[error] refresh failed for $map_name (exit $rc)"
            exit $rc
            ;;
    esac
done < <(dotnet fsi scripts/examples/15-list-supported-maps.fsx | sed -n 's/^MAP://p')

echo
echo "=== Summary: refreshed=$refreshed skipped=$skipped ==="

if [ "$refreshed" -eq 0 ]; then
    echo "[error] no maps were refreshed — is your local BAR maps directory populated?"
    exit 1
fi

exit 0
