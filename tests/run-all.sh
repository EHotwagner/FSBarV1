#!/usr/bin/env bash
# Unified test runner for FSBarV1.
# Usage: run-all.sh [--category CATEGORY] [--graphical] [--help]
#
# Categories: unit, integration, all (default)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
REPORT_DIR="${REPO_ROOT}/reports/testreports"

# Defaults
CATEGORY="all"
GRAPHICAL=false
ENGINE_AVAILABLE=false
RUN_START=""
INTERRUPTED=false

# Per-tier results
declare -A TIER_PASSED TIER_FAILED TIER_SKIPPED TIER_OUTPUT TIER_STATUS

# Engine env vars (populated by auto-detection)
RESOLVED_ENGINE=""
RESOLVED_DATADIR=""

# ─── Argument parsing ───────────────────────────────────────────────

usage() {
    cat <<'EOF'
Usage: run-all.sh [OPTIONS]

Options:
  --category CAT   Run only tests in category CAT
                   Categories: unit, integration, all
                   Default: all
  --graphical      Launch full graphical BAR game with AI for visual validation
  --help           Show this help

Examples:
  ./tests/run-all.sh                       # Run all tests
  ./tests/run-all.sh --category unit       # Unit tests only
  ./tests/run-all.sh --category integration # Integration tests (needs engine)
  ./tests/run-all.sh --graphical           # Launch graphical game
EOF
    exit 0
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --category) CATEGORY="${2:?--category requires a value}"; shift 2 ;;
        --graphical) GRAPHICAL=true; shift ;;
        --help|-h) usage ;;
        *) echo "Unknown option: $1" >&2; usage ;;
    esac
done

# ─── Signal trapping ────────────────────────────────────────────────

cleanup() {
    INTERRUPTED=true
    echo ""
    echo "=== Interrupted — cleaning up ==="
    generate_report
    print_summary
    exit 1
}

trap cleanup SIGINT SIGTERM

# ─── Engine detection ──────────────────────────────────────────────

check_engine_prereqs() {
    local result
    result=$("${SCRIPT_DIR}/check-prerequisites.sh" --json 2>/dev/null || echo '{"passed":false}')
    if echo "$result" | jq -e '.passed' >/dev/null 2>&1; then
        ENGINE_AVAILABLE=true
        RESOLVED_ENGINE=$(echo "$result" | jq -r '.engine')
        RESOLVED_DATADIR=$(echo "$result" | jq -r '.datadir')
    fi
}

# ─── Tier runners ───────────────────────────────────────────────────

should_run() {
    local tier="$1"
    if [ "$CATEGORY" = "all" ]; then
        return 0
    else
        [ "$CATEGORY" = "$tier" ]
    fi
}

run_tier() {
    local tier="$1"
    local label="$2"
    shift 2
    local cmd=("$@")

    echo ""
    echo "━━━ ${label} ━━━"

    local output
    local exit_code=0
    output=$("${cmd[@]}" 2>&1) || exit_code=$?

    TIER_OUTPUT[$tier]="$output"

    # Parse pass/fail counts from dotnet test output
    local passed=0 failed=0
    local dp df
    dp=$(echo "$output" | grep -oP 'Passed:\s+\K\d+' | tail -1 || true)
    df=$(echo "$output" | grep -oP 'Failed:\s+\K\d+' | tail -1 || true)
    passed=${dp:-0}
    failed=${df:-0}

    TIER_PASSED[$tier]=$passed
    TIER_FAILED[$tier]=$failed
    TIER_SKIPPED[$tier]=0

    if [ $exit_code -eq 0 ]; then
        TIER_STATUS[$tier]="pass"
        echo "  ✓ PASSED (${passed} passed)"
    else
        TIER_STATUS[$tier]="fail"
        echo "  ✗ FAILED (${passed} passed, ${failed} failed)"
        echo "$output" | tail -10 | sed 's/^/    /'
    fi
}

skip_tier() {
    local tier="$1"
    local label="$2"
    local reason="$3"

    echo ""
    echo "━━━ ${label} ━━━"
    echo "  ⊘ SKIPPED: ${reason}"

    TIER_STATUS[$tier]="skip"
    TIER_PASSED[$tier]=0
    TIER_FAILED[$tier]=0
    TIER_SKIPPED[$tier]=1
    TIER_OUTPUT[$tier]=""
}

# ─── Report generation ──────────────────────────────────────────────

generate_report() {
    mkdir -p "$REPORT_DIR"

    local timestamp
    timestamp=$(date '+%Y-%m-%d_%H-%M-%S')
    local report_file="${REPORT_DIR}/${timestamp}_${CATEGORY}.md"

    local total_passed=0 total_failed=0 total_skipped=0
    for tier in "${!TIER_PASSED[@]}"; do
        total_passed=$((total_passed + ${TIER_PASSED[$tier]:-0}))
        total_failed=$((total_failed + ${TIER_FAILED[$tier]:-0}))
        total_skipped=$((total_skipped + ${TIER_SKIPPED[$tier]:-0}))
    done

    local status_emoji="✓"
    [ $total_failed -gt 0 ] && status_emoji="✗"
    $INTERRUPTED && status_emoji="⚠"

    cat > "$report_file" <<EOF
# Test Report: ${CATEGORY}

**Date**: ${timestamp}
**Status**: ${status_emoji} $(if $INTERRUPTED; then echo "INTERRUPTED"; elif [ $total_failed -gt 0 ]; then echo "FAILED"; else echo "PASSED"; fi)

## Environment

| Property | Value |
|----------|-------|
| Engine | ${RESOLVED_ENGINE:-not detected} |
| Data Dir | ${RESOLVED_DATADIR:-N/A} |
| Platform | $(uname -srm) |

## Results

| Tier | Status | Passed | Failed | Skipped |
|------|--------|--------|--------|---------|
EOF

    for tier in unit integration; do
        if [ -n "${TIER_STATUS[$tier]:-}" ]; then
            local status_mark="✓"
            [ "${TIER_STATUS[$tier]}" = "fail" ] && status_mark="✗"
            [ "${TIER_STATUS[$tier]}" = "skip" ] && status_mark="⊘"
            echo "| ${tier} | ${status_mark} ${TIER_STATUS[$tier]} | ${TIER_PASSED[$tier]:-0} | ${TIER_FAILED[$tier]:-0} | ${TIER_SKIPPED[$tier]:-0} |" >> "$report_file"
        fi
    done

    cat >> "$report_file" <<EOF

## Summary

- **Total Passed**: ${total_passed}
- **Total Failed**: ${total_failed}
- **Total Skipped**: ${total_skipped} tier(s)

---
*Generated by tests/run-all.sh*
EOF

    echo ""
    echo "Report saved: ${report_file}"
}

# ─── Summary ────────────────────────────────────────────────────────

print_summary() {
    local total_passed=0 total_failed=0 total_skipped=0
    for tier in "${!TIER_PASSED[@]}"; do
        total_passed=$((total_passed + ${TIER_PASSED[$tier]:-0}))
        total_failed=$((total_failed + ${TIER_FAILED[$tier]:-0}))
        total_skipped=$((total_skipped + ${TIER_SKIPPED[$tier]:-0}))
    done

    echo ""
    echo "═══════════════════════════════════════"
    if [ $total_failed -gt 0 ]; then
        echo "  RESULT: FAILED"
    else
        echo "  RESULT: PASSED"
    fi
    echo "  Passed: ${total_passed}  Failed: ${total_failed}  Skipped tiers: ${total_skipped}"
    echo "═══════════════════════════════════════"
}

# ─── Main ───────────────────────────────────────────────────────────

main() {
    RUN_START=$(date '+%Y-%m-%d %H:%M:%S')
    echo "=== FSBarV1 Test Runner ==="
    echo "Category: ${CATEGORY}"
    echo "Started: ${RUN_START}"

    # Handle graphical mode separately
    if $GRAPHICAL; then
        if [ -z "${DISPLAY:-}" ]; then
            echo "ERROR: --graphical requires a display (DISPLAY env var not set)."
            exit 1
        fi
        echo "Launching graphical test mode..."
        dotnet fsi "${SCRIPT_DIR}/FSBar.LiveTests/GraphicalLaunch.fsx"
        echo "Graphical session ended."
        exit 0
    fi

    # ── Unit tests ──

    if should_run "unit"; then
        if [ -f "${REPO_ROOT}/tests/FSBar.Client.Tests/FSBar.Client.Tests.fsproj" ]; then
            run_tier "unit" "F# Unit Tests" dotnet test "${REPO_ROOT}/tests/FSBar.Client.Tests/" --verbosity quiet
        else
            skip_tier "unit" "F# Unit Tests" "tests/FSBar.Client.Tests/ not found"
        fi
    fi

    # ── Integration tests (engine-dependent) ──

    local engine_tiers_requested=false
    should_run "integration" && engine_tiers_requested=true

    if $engine_tiers_requested; then
        echo ""
        echo "━━━ Engine Prerequisites ━━━"
        check_engine_prereqs
        if $ENGINE_AVAILABLE; then
            echo "  ✓ Engine available: ${RESOLVED_ENGINE}"
        else
            echo "  ⚠ Engine prerequisites not met — integration tests will be skipped"
        fi
    fi

    if should_run "integration"; then
        if $ENGINE_AVAILABLE; then
            run_tier "integration" "F# Integration Tests (Live Engine)" dotnet test "${REPO_ROOT}/tests/FSBar.LiveTests/" --verbosity quiet
        else
            skip_tier "integration" "F# Integration Tests (Live Engine)" "Engine prerequisites not met"
        fi
    fi

    # ── Summary & Report ──

    generate_report
    print_summary

    # Exit with failure if any tier failed
    for tier in "${!TIER_STATUS[@]}"; do
        if [ "${TIER_STATUS[$tier]}" = "fail" ]; then
            exit 1
        fi
    done
    exit 0
}

main
