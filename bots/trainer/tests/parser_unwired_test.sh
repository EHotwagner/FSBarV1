#!/usr/bin/env bash
# bots/trainer/tests/parser_unwired_test.sh — fixture-based test for parse_unwired.sh.
#
# Verifies SC-001 (step-change from silent zero to accurate non-zero count) and
# SC-002 (zero-regression on matches with no rejections) against synthetic
# stderr fixtures, plus the FR-003 always-emit invariant under missing-input.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TRAINER_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
# shellcheck source=../lib/parse_unwired.sh
source "$TRAINER_DIR/lib/parse_unwired.sh"

FIXTURES="$SCRIPT_DIR/fixtures"
OUT_NONEMPTY="$(mktemp /tmp/unwired_test_out.XXXXXX.json)"
OUT_EMPTY="$(mktemp /tmp/unwired_test_out_empty.XXXXXX.json)"
OUT_MISSING="$(mktemp /tmp/unwired_test_out_missing.XXXXXX.json)"
trap 'rm -f "$OUT_NONEMPTY" "$OUT_EMPTY" "$OUT_MISSING"' EXIT

fail() {
    echo "FAIL: $1" >&2
    [[ -n "${2:-}" ]] && echo "$2" >&2
    exit 1
}

assert_json_eq() {
    local label="$1" actual_file="$2" expected_json="$3"
    local actual
    actual="$(jq -cS '.' "$actual_file")"
    local expected
    expected="$(echo "$expected_json" | jq -cS '.')"
    if [[ "$actual" != "$expected" ]]; then
        fail "$label" "expected: $expected
actual:   $actual"
    fi
}

# --- Case 1: non-empty fixture (4 matches, 3 distinct integers, includes 999) ---
parse_unwired_stderr "$FIXTURES/unwired_stderr.txt" "$OUT_NONEMPTY"
assert_json_eq "non-empty fixture" "$OUT_NONEMPTY" \
    '{"rc_minus_2_count": 4, "by_case": {"99": 2, "45": 1, "999": 1}}'

# Explicit "no throw on unknown integer" assertion
if ! jq -e '.by_case | has("999")' "$OUT_NONEMPTY" >/dev/null; then
    fail "by_case missing the high-integer '999' key — unknown-integer invariant broken"
fi

# --- Case 2: empty fixture (noise lines only, FR-003 / SC-002) ---
parse_unwired_stderr "$FIXTURES/unwired_stderr_empty.txt" "$OUT_EMPTY"
assert_json_eq "empty fixture" "$OUT_EMPTY" \
    '{"rc_minus_2_count": 0, "by_case": {}}'

# --- Case 3: missing input path (FR-003 always-emit under missing file) ---
parse_unwired_stderr "/nonexistent/path-that-does-not-exist.txt" "$OUT_MISSING"
assert_json_eq "missing input" "$OUT_MISSING" \
    '{"rc_minus_2_count": 0, "by_case": {}}'

echo "PASS: parser_unwired_test"
