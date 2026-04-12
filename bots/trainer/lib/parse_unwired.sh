#!/usr/bin/env bash
# bots/trainer/lib/parse_unwired.sh — sourceable parser for unwired_commands.json.
#
# Contracts:
#   FSBarV1 side : specs/022-incorporate-highbar-030/contracts/unwired-commands-report.md
#   Upstream     : ../HighBarV2/specs/030-proxy-contract-docs/contracts/unwired-command-log.md
#
# Parser corrections vs. feature 021's run.sh implementation (satisfies FR-006):
#   (1) The `case=` token carries an *integer* protobuf oneof field number, not an
#       alphabetic command name. Feature 021 grepped for `[A-Za-z_][A-Za-z0-9_]*`
#       and silently fell back to "unknown" on every line, so by_case was never
#       populated and rc_minus_2_count was always zero.
#   (2) In verbose_commands mode, `case=` and `rc=` appear on separate infolog
#       lines correlated by a `Cmd <N>:` prefix — not on the same line. Feature
#       021 grepped for `rc=-2` and tried to pull `case=` off the same line,
#       which never matches in either mode.
#
# This parser scans engine.stderr only for the always-on line
#   [HB] unsupported command oneof case=<INT> (proxy switch table miss)
# which is emitted unconditionally by the proxy regardless of verbose_commands.
# Keying by integer keeps the parser dependency-free (see research.md D2);
# consumers needing command names look them up against messages.proto themselves.

parse_unwired_stderr() {
    local stderr_path="$1"
    local output_json_path="$2"

    local total=0
    local by_case_json='{}'

    if [[ -f "$stderr_path" ]]; then
        declare -A counts
        while IFS= read -r case_int; do
            [[ -z "$case_int" ]] && continue
            counts[$case_int]=$(( ${counts[$case_int]:-0} + 1 ))
            total=$((total + 1))
        done < <(sed -n 's/^\[HB\] unsupported command oneof case=\([0-9]\+\) .*/\1/p' "$stderr_path")

        for k in "${!counts[@]}"; do
            by_case_json="$(jq -n --arg k "$k" --argjson v "${counts[$k]}" --argjson base "$by_case_json" '$base + {($k): $v}')"
        done
    fi

    jq -n --argjson total "$total" --argjson by_case "$by_case_json" \
        '{rc_minus_2_count: $total, by_case: $by_case}' > "$output_json_path"
}
