#!/usr/bin/env bash
# refresh-bundled-proxy.sh — copy a built HighBarV2 proxy into
# proxy/bundled/<version>/ and atomically rewrite BUNDLED_VERSION.
#
# Intended for maintainers who've rebuilt the proxy in a sibling
# HighBarV2 checkout (or pass --source to point elsewhere). Users
# should never need to run this — they pick up the committed bundle
# by pulling.
#
# Usage:
#   refresh-bundled-proxy.sh <version> [--source DIR] [--force]
#
# Flags:
#   <version>      Bundle version (e.g. 0.1.17). Required.
#   --source DIR   Directory to pull files from. Must contain
#                    libSkirmishAI.so (the built proxy binary)
#                    AIInfo.lua      (proxy/data/AIInfo.lua)
#                    AIOptions.lua   (proxy/data/AIOptions.lua)
#                  Default: ${HIGHBARV2_REPO:-../HighBarV2}/build/
#                  (with AIInfo.lua + AIOptions.lua pulled from
#                  ${HIGHBARV2_REPO:-../HighBarV2}/proxy/data/ when
#                  they're not in build/ — matches the historical
#                  HighBarV2 layout).
#   --force        Overwrite an existing proxy/bundled/<version>/
#                  directory. Without this, the script refuses to
#                  touch a populated destination.
#
# Safety:
#   * Writes BUNDLED_VERSION LAST, so readers never see a new
#     version string pointing at a missing or half-populated
#     bundled/<version>/ directory.
#   * Refuses to run from outside an FSBarV1 repo root.

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

info()  { echo -e "${BLUE}[info]${NC} $1"; }
ok()    { echo -e "${GREEN}[ok]${NC} $1"; }
warn()  { echo -e "${YELLOW}[warn]${NC} $1" >&2; }
die()   { echo -e "${RED}[error]${NC} $1" >&2; exit 1; }

VERSION=""
SOURCE_DIR=""
FORCE=0

while [[ $# -gt 0 ]]; do
    case "$1" in
        --source) SOURCE_DIR="$2"; shift 2 ;;
        --force)  FORCE=1; shift ;;
        --help|-h)
            sed -n '3,30p' "$0" | sed 's/^# \{0,1\}//'
            exit 0
            ;;
        -*) die "Unknown flag: $1 (use --help)" ;;
        *)  if [[ -z "$VERSION" ]]; then VERSION="$1"; shift
            else die "Unexpected positional arg: $1"; fi ;;
    esac
done

[[ -n "$VERSION" ]] || die "version argument is required (e.g. 0.1.17)"

# Validate version looks like a plain semver-ish token. Refuse
# anything with shell metachars so path substitution is safe.
if [[ ! "$VERSION" =~ ^[0-9A-Za-z._-]+$ ]]; then
    die "version '$VERSION' must match [0-9A-Za-z._-]+"
fi

# Locate repo root — the directory containing proxy/BUNDLED_VERSION.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
[[ -f "$REPO_ROOT/proxy/BUNDLED_VERSION" ]] \
    || die "$REPO_ROOT/proxy/BUNDLED_VERSION not found — run from FSBarV1/scripts/"

# Default source tree.
if [[ -z "$SOURCE_DIR" ]]; then
    HIGHBARV2_REPO="${HIGHBARV2_REPO:-$REPO_ROOT/../HighBarV2}"
    SOURCE_DIR="$HIGHBARV2_REPO/build"
fi

# Resolve each required source file — fall back to proxy/data/ for
# the .lua descriptors when they're not in build/.
resolve_source () {
    local name="$1"
    # build/ first
    if [[ -f "$SOURCE_DIR/$name" ]]; then echo "$SOURCE_DIR/$name"; return 0; fi
    # proxy/data/ fallback (historical HighBarV2 layout)
    local data_dir="$SOURCE_DIR/../proxy/data"
    if [[ -f "$data_dir/$name" ]]; then echo "$data_dir/$name"; return 0; fi
    return 1
}

LIB_SRC=""
AIINFO_SRC=""
AIOPT_SRC=""
LIB_SRC="$(resolve_source libSkirmishAI.so)" \
    || die "libSkirmishAI.so not found under $SOURCE_DIR or its proxy/data sibling"
AIINFO_SRC="$(resolve_source AIInfo.lua)" \
    || die "AIInfo.lua not found under $SOURCE_DIR or its proxy/data sibling"
AIOPT_SRC="$(resolve_source AIOptions.lua)" \
    || die "AIOptions.lua not found under $SOURCE_DIR or its proxy/data sibling"

BUNDLE_DIR="$REPO_ROOT/proxy/bundled/$VERSION"
VERSION_FILE="$REPO_ROOT/proxy/BUNDLED_VERSION"

if [[ -d "$BUNDLE_DIR" ]] && compgen -G "$BUNDLE_DIR/*" >/dev/null 2>&1; then
    if [[ $FORCE -ne 1 ]]; then
        die "$BUNDLE_DIR already has files — re-run with --force to overwrite"
    fi
    warn "overwriting populated $BUNDLE_DIR (--force)"
fi

mkdir -p "$BUNDLE_DIR"
info "copying libSkirmishAI.so  ← $LIB_SRC"
cp "$LIB_SRC"  "$BUNDLE_DIR/libSkirmishAI.so"
info "copying AIInfo.lua        ← $AIINFO_SRC"
cp "$AIINFO_SRC" "$BUNDLE_DIR/AIInfo.lua"
info "copying AIOptions.lua     ← $AIOPT_SRC"
cp "$AIOPT_SRC" "$BUNDLE_DIR/AIOptions.lua"

# Atomically rewrite BUNDLED_VERSION last so readers never see a
# mid-refresh state.
TMP_VERSION_FILE="$VERSION_FILE.tmp"
printf '%s\n' "$VERSION" > "$TMP_VERSION_FILE"
mv "$TMP_VERSION_FILE" "$VERSION_FILE"

PROXY_DIR_REL="proxy/bundled/$VERSION"
ok "refreshed bundled proxy:"
echo "    $PROXY_DIR_REL/libSkirmishAI.so  ($(stat -c%s "$BUNDLE_DIR/libSkirmishAI.so") bytes)"
echo "    $PROXY_DIR_REL/AIInfo.lua        ($(stat -c%s "$BUNDLE_DIR/AIInfo.lua") bytes)"
echo "    $PROXY_DIR_REL/AIOptions.lua     ($(stat -c%s "$BUNDLE_DIR/AIOptions.lua") bytes)"
echo "    proxy/BUNDLED_VERSION = $VERSION"
echo
echo "Next step: git add proxy/BUNDLED_VERSION proxy/bundled/$VERSION/ && git commit"
