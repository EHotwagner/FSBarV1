#!/usr/bin/env bash
# Validate prerequisites for running FSBar live integration tests.
# Usage: check-prerequisites.sh [--json]
#
# Checks:
#   - Engine binary exists (via HIGHBAR_TEST_ENGINE, engine-version.json, or auto-detect)
#   - SPRING_DATADIR auto-detection
#   - Game archive exists in SPRING_DATADIR/packages/
#   - Map file exists in SPRING_DATADIR/maps/
#
# Engine resolution priority:
#   1. HIGHBAR_TEST_ENGINE environment variable (full binary path)
#   2. engine-version.json config file (version + binary name)
#   3. Auto-detect from ~/.local/state/Beyond All Reason/engine/recoil_*/
#
# Exit codes:
#   0 - All prerequisites met
#   1 - One or more prerequisites missing
#   2 - Script error (e.g., cannot read config file)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_FILE="${SCRIPT_DIR}/engine-version.json"
BAR_DATA_DIR="${HOME}/.local/state/Beyond All Reason"

JSON_MODE=false
for arg in "$@"; do
    case "${arg}" in
        --json) JSON_MODE=true ;;
    esac
done

# Read config if it exists (now optional)
ENGINE_BINARY="spring-headless"
ENGINE_VERSION=""
GAME_NAME=""
MAP_NAME=""
HAS_CONFIG=false

if [ -f "${CONFIG_FILE}" ]; then
    if ! command -v jq &>/dev/null; then
        if ${JSON_MODE}; then
            echo '{"error":"jq not found","passed":false}'
        else
            echo "ERROR: jq is required but not found. Install with: pacman -S jq" >&2
        fi
        exit 2
    fi
    HAS_CONFIG=true
    ENGINE_BINARY=$(jq -r '.engine.binary' "${CONFIG_FILE}")
    ENGINE_VERSION=$(jq -r '.engine.version' "${CONFIG_FILE}")
    GAME_NAME=$(jq -r '.game.name' "${CONFIG_FILE}")
    MAP_NAME=$(jq -r '.map.name' "${CONFIG_FILE}")
fi

ALL_PASSED=true
CHECKS=()

# Check 1: Engine binary
RESOLVED_ENGINE=""
ENGINE_PATH="${HIGHBAR_TEST_ENGINE:-}"
if [ -n "${ENGINE_PATH}" ]; then
    # Priority 1: HIGHBAR_TEST_ENGINE env var
    if [ -x "${ENGINE_PATH}" ]; then
        RESOLVED_ENGINE="${ENGINE_PATH}"
        CHECKS+=("{\"name\":\"engine_binary\",\"passed\":true,\"detail\":\"${ENGINE_BINARY} found at ${ENGINE_PATH} (via HIGHBAR_TEST_ENGINE)\"}")
    else
        ALL_PASSED=false
        CHECKS+=("{\"name\":\"engine_binary\",\"passed\":false,\"detail\":\"HIGHBAR_TEST_ENGINE set but not executable: ${ENGINE_PATH}\"}")
    fi
elif ${HAS_CONFIG} && [ -n "${ENGINE_VERSION}" ]; then
    # Priority 2: engine-version.json config file
    _CONFIG_ENGINE="${BAR_DATA_DIR}/engine/recoil_${ENGINE_VERSION}/${ENGINE_BINARY}"
    if [ -x "${_CONFIG_ENGINE}" ]; then
        RESOLVED_ENGINE="${_CONFIG_ENGINE}"
        CHECKS+=("{\"name\":\"engine_binary\",\"passed\":true,\"detail\":\"${ENGINE_BINARY} v${ENGINE_VERSION} found at ${_CONFIG_ENGINE} (via engine-version.json)\"}")
    else
        # Fall back to PATH
        _PATH_ENGINE="$(command -v "${ENGINE_BINARY}" 2>/dev/null || true)"
        if [ -n "${_PATH_ENGINE}" ]; then
            RESOLVED_ENGINE="${_PATH_ENGINE}"
            CHECKS+=("{\"name\":\"engine_binary\",\"passed\":true,\"detail\":\"${ENGINE_BINARY} found on PATH at ${_PATH_ENGINE}\"}")
        else
            ALL_PASSED=false
            CHECKS+=("{\"name\":\"engine_binary\",\"passed\":false,\"detail\":\"${ENGINE_BINARY} v${ENGINE_VERSION} not found at ${_CONFIG_ENGINE} or on PATH\"}")
        fi
    fi
else
    # Priority 3: Auto-detect from standard BAR directory
    _FOUND=""
    _LATEST_VERSION=""
    if [ -d "${BAR_DATA_DIR}/engine" ]; then
        # Find all recoil_* directories, sort descending, take first
        while IFS= read -r candidate_dir; do
            _DIR_NAME="$(basename "${candidate_dir}")"
            _VER="${_DIR_NAME#recoil_}"
            _BIN="${candidate_dir}/${ENGINE_BINARY}"
            if [ -x "${_BIN}" ]; then
                _FOUND="${_BIN}"
                _LATEST_VERSION="${_VER}"
                break
            fi
        done < <(find "${BAR_DATA_DIR}/engine" -maxdepth 1 -type d -name 'recoil_*' 2>/dev/null | sort -r)
    fi

    if [ -n "${_FOUND}" ]; then
        RESOLVED_ENGINE="${_FOUND}"
        ENGINE_VERSION="${_LATEST_VERSION}"
        CHECKS+=("{\"name\":\"engine_binary\",\"passed\":true,\"detail\":\"${ENGINE_BINARY} v${_LATEST_VERSION} auto-detected at ${_FOUND}\"}")
    else
        _PATH_ENGINE="$(command -v "${ENGINE_BINARY}" 2>/dev/null || true)"
        if [ -n "${_PATH_ENGINE}" ]; then
            RESOLVED_ENGINE="${_PATH_ENGINE}"
            CHECKS+=("{\"name\":\"engine_binary\",\"passed\":true,\"detail\":\"${ENGINE_BINARY} found on PATH at ${_PATH_ENGINE}\"}")
        else
            ALL_PASSED=false
            CHECKS+=("{\"name\":\"engine_binary\",\"passed\":false,\"detail\":\"${ENGINE_BINARY} not found. Searched: HIGHBAR_TEST_ENGINE (not set), ${BAR_DATA_DIR}/engine/recoil_*/, PATH\"}")
        fi
    fi
fi

# Auto-detect game version if not in config
if [ -z "${GAME_NAME}" ] && [ -d "${BAR_DATA_DIR}/rapid" ]; then
    _VERSIONS_GZ=""
    for _VPATH in \
        "${BAR_DATA_DIR}/rapid/repos-cdn.beyondallreason.dev/byar/versions.gz" \
        "${BAR_DATA_DIR}/rapid/repos.springrts.com/byar/versions.gz"; do
        if [ -f "${_VPATH}" ]; then
            _VERSIONS_GZ="${_VPATH}"
            break
        fi
    done
    if [ -n "${_VERSIONS_GZ}" ]; then
        GAME_NAME="$(zcat "${_VERSIONS_GZ}" | grep '^byar:test,' | tail -1 | cut -d',' -f4)"
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
    # Fallback: check standard BAR data directory
    if [ -z "${DATA_DIR}" ]; then
        if [ -d "${BAR_DATA_DIR}/maps" ] && [ -d "${BAR_DATA_DIR}/packages" ]; then
            DATA_DIR="${BAR_DATA_DIR}"
        fi
    fi
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
    _GAME_NAME_ESCAPED="${GAME_NAME//\"/\\\"}"
    echo "{\"passed\":${ALL_PASSED},\"engine\":\"${RESOLVED_ENGINE}\",\"datadir\":\"${DATA_DIR}\",\"engine_version\":\"${ENGINE_VERSION}\",\"game_name\":\"${_GAME_NAME_ESCAPED}\",\"checks\":${CHECKS_JSON}}"
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
    if [ -n "${ENGINE_VERSION}" ]; then
        echo "Engine version: ${ENGINE_VERSION}"
    fi
    if [ -n "${GAME_NAME}" ]; then
        echo "Game: ${GAME_NAME}"
    fi
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
