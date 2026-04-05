#!/usr/bin/env bash
# Validate prerequisites for running FSBar live integration tests.
# Usage: check-prerequisites.sh [--json]
#
# Checks:
#   - Engine binary exists (via HIGHBAR_TEST_ENGINE or PATH)
#   - SPRING_DATADIR auto-detection
#   - Game archive exists in SPRING_DATADIR/packages/
#   - Map file exists in SPRING_DATADIR/maps/
#
# Exit codes:
#   0 - All prerequisites met
#   1 - One or more prerequisites missing
#   2 - Script error (e.g., cannot read config file)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_FILE="${SCRIPT_DIR}/engine-version.json"

JSON_MODE=false
for arg in "$@"; do
    case "${arg}" in
        --json) JSON_MODE=true ;;
    esac
done

# Verify config file exists
if [ ! -f "${CONFIG_FILE}" ]; then
    if ${JSON_MODE}; then
        echo '{"error":"engine-version.json not found","passed":false}'
    else
        echo "ERROR: ${CONFIG_FILE} not found" >&2
    fi
    exit 2
fi

# Verify jq is available
if ! command -v jq &>/dev/null; then
    if ${JSON_MODE}; then
        echo '{"error":"jq not found","passed":false}'
    else
        echo "ERROR: jq is required but not found. Install with: pacman -S jq" >&2
    fi
    exit 2
fi

# Read config
ENGINE_BINARY=$(jq -r '.engine.binary' "${CONFIG_FILE}")
ENGINE_VERSION=$(jq -r '.engine.version' "${CONFIG_FILE}")
GAME_NAME=$(jq -r '.game.name' "${CONFIG_FILE}")
MAP_NAME=$(jq -r '.map.name' "${CONFIG_FILE}")

ALL_PASSED=true
CHECKS=()

# Check 1: Engine binary
RESOLVED_ENGINE=""
ENGINE_PATH="${HIGHBAR_TEST_ENGINE:-}"
if [ -n "${ENGINE_PATH}" ]; then
    if [ -x "${ENGINE_PATH}" ]; then
        RESOLVED_ENGINE="${ENGINE_PATH}"
        CHECKS+=("{\"name\":\"engine_binary\",\"passed\":true,\"detail\":\"${ENGINE_BINARY} found at ${ENGINE_PATH} (via HIGHBAR_TEST_ENGINE)\"}")
    else
        ALL_PASSED=false
        CHECKS+=("{\"name\":\"engine_binary\",\"passed\":false,\"detail\":\"HIGHBAR_TEST_ENGINE set but not executable: ${ENGINE_PATH}\"}")
    fi
else
    _PATH_ENGINE="$(command -v "${ENGINE_BINARY}" 2>/dev/null || true)"
    if [ -n "${_PATH_ENGINE}" ]; then
        RESOLVED_ENGINE="${_PATH_ENGINE}"
        CHECKS+=("{\"name\":\"engine_binary\",\"passed\":true,\"detail\":\"${ENGINE_BINARY} found on PATH at ${_PATH_ENGINE}\"}")
    else
        # Search standard BAR AppImage locations
        _FOUND=""
        for candidate in "${HOME}/.local/state/Beyond All Reason/engine"/*/"${ENGINE_BINARY}"; do
            if [ -x "$candidate" ]; then
                _FOUND="$candidate"
                break
            fi
        done
        if [ -n "${_FOUND}" ]; then
            RESOLVED_ENGINE="${_FOUND}"
            CHECKS+=("{\"name\":\"engine_binary\",\"passed\":true,\"detail\":\"${ENGINE_BINARY} found at ${_FOUND}\"}")
        else
            ALL_PASSED=false
            CHECKS+=("{\"name\":\"engine_binary\",\"passed\":false,\"detail\":\"${ENGINE_BINARY} not found on PATH or in standard locations\"}")
        fi
    fi
fi

# Determine data directory — auto-detect from resolved engine binary location
DATA_DIR="${SPRING_DATADIR:-}"
if [ -z "${DATA_DIR}" ] && [ -n "${RESOLVED_ENGINE}" ]; then
    _ENGINE_DIR="$(dirname "$(readlink -f "${RESOLVED_ENGINE}")")"
    # Walk up directory tree (2 or 3 levels) looking for maps/ + packages/
    for _DEPTH in 2 3; do
        _CANDIDATE="${_ENGINE_DIR}"
        for _I in $(seq 1 "${_DEPTH}"); do
            _CANDIDATE="$(dirname "${_CANDIDATE}")"
        done
        if [ -d "${_CANDIDATE}/maps" ] && [ -d "${_CANDIDATE}/packages" ]; then
            DATA_DIR="${_CANDIDATE}"
            break
        fi
    done
fi
DATA_DIR="${DATA_DIR:-${HOME}/.spring}"

# Check 2: SPRING_DATADIR
if [ -d "${DATA_DIR}" ]; then
    CHECKS+=("{\"name\":\"spring_datadir\",\"passed\":true,\"detail\":\"SPRING_DATADIR: ${DATA_DIR}\"}")
else
    ALL_PASSED=false
    CHECKS+=("{\"name\":\"spring_datadir\",\"passed\":false,\"detail\":\"SPRING_DATADIR not found: ${DATA_DIR}\"}")
fi

# Check 3: Game archive
GAME_FOUND=false
if [ -d "${DATA_DIR}/packages" ]; then
    if ls "${DATA_DIR}/packages/"*.sdp 1>/dev/null 2>&1; then
        GAME_FOUND=true
        CHECKS+=("{\"name\":\"game_archive\",\"passed\":true,\"detail\":\"Game packages found in ${DATA_DIR}/packages/\"}")
    fi
fi
if ! ${GAME_FOUND}; then
    ALL_PASSED=false
    CHECKS+=("{\"name\":\"game_archive\",\"passed\":false,\"detail\":\"No .sdp game packages in ${DATA_DIR}/packages/\"}")
fi

# Check 4: Map
MAP_FOUND=false
if [ -d "${DATA_DIR}/maps" ]; then
    if ls "${DATA_DIR}/maps/"* 1>/dev/null 2>&1; then
        MAP_FOUND=true
        CHECKS+=("{\"name\":\"map_file\",\"passed\":true,\"detail\":\"Maps found in ${DATA_DIR}/maps/\"}")
    fi
fi
if ! ${MAP_FOUND}; then
    ALL_PASSED=false
    CHECKS+=("{\"name\":\"map_file\",\"passed\":false,\"detail\":\"No maps in ${DATA_DIR}/maps/\"}")
fi

# Output results
if ${JSON_MODE}; then
    CHECKS_JSON=$(printf '%s,' "${CHECKS[@]}")
    CHECKS_JSON="[${CHECKS_JSON%,}]"
    echo "{\"passed\":${ALL_PASSED},\"engine\":\"${RESOLVED_ENGINE}\",\"datadir\":\"${DATA_DIR}\",\"checks\":${CHECKS_JSON}}"
else
    echo "=== FSBar Live Test Prerequisites ==="
    for check in "${CHECKS[@]}"; do
        name=$(echo "$check" | jq -r '.name')
        passed=$(echo "$check" | jq -r '.passed')
        detail=$(echo "$check" | jq -r '.detail')
        if [ "$passed" = "true" ]; then
            echo "  ✓ ${name}: ${detail}"
        else
            echo "  ✗ ${name}: ${detail}"
        fi
    done
    echo ""
    if ${ALL_PASSED}; then
        echo "All prerequisites met."
    else
        echo "Some prerequisites are missing."
    fi
fi

if ${ALL_PASSED}; then
    exit 0
else
    exit 1
fi
