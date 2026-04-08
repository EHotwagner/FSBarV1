#!/usr/bin/env bash
set -euo pipefail

# Check dependency freshness: compare DLLs in nupkg/ against build output.
# Usage: ./scripts/check-deps.sh

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
NUPKG_DIR="$REPO_ROOT/nupkg"
TMPDIR=$(mktemp -d)
trap 'rm -rf "$TMPDIR"' EXIT

stale=0
checked=0

for nupkg in "$NUPKG_DIR"/*.nupkg; do
    [[ -f "$nupkg" ]] || continue
    pkg_name=$(basename "$nupkg" | sed 's/\.[0-9].*//')

    # Extract DLLs from nupkg (it's a ZIP)
    python3 -c "
import zipfile, sys, os
with zipfile.ZipFile('$nupkg', 'r') as z:
    for name in z.namelist():
        if name.endswith('.dll') and '/net' in name:
            z.extract(name, '$TMPDIR/$pkg_name')
            print(name)
" > "$TMPDIR/${pkg_name}_files.txt" 2>/dev/null

    while IFS= read -r dll_path; do
        dll_name=$(basename "$dll_path")
        nupkg_hash=$(sha256sum "$TMPDIR/$pkg_name/$dll_path" | cut -d' ' -f1)

        # Search for this DLL in build output directories
        found=false
        for build_dll in $(find "$REPO_ROOT/src" "$REPO_ROOT/tests" -path "*/bin/Debug/net*/$(basename "$dll_path")" 2>/dev/null); do
            found=true
            build_hash=$(sha256sum "$build_dll" | cut -d' ' -f1)
            checked=$((checked + 1))

            rel_path="${build_dll#$REPO_ROOT/}"
            if [[ "$nupkg_hash" == "$build_hash" ]]; then
                echo "  ✓ $dll_name in $rel_path — fresh"
            else
                echo "  ✗ $dll_name in $rel_path — STALE"
                stale=$((stale + 1))
            fi
        done

        if [[ "$found" == "false" ]]; then
            echo "  ? $dll_name — not found in build output (run dotnet build first)"
        fi
    done < "$TMPDIR/${pkg_name}_files.txt"
done

echo ""
if [[ $checked -eq 0 ]]; then
    echo "No dependencies checked. Run 'dotnet build' first."
    exit 1
elif [[ $stale -gt 0 ]]; then
    echo "$stale of $checked dependency DLL(s) are STALE. Run 'dotnet build' to refresh."
    exit 1
else
    echo "All $checked dependency DLL(s) are fresh."
    exit 0
fi
