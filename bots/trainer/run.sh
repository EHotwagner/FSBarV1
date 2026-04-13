#!/usr/bin/env bash
# bots/trainer/run.sh — launch one trainer iteration and materialise a conformant run directory.
#
# Usage: bash bots/trainer/run.sh <rung_name> <iter_id>
#   <rung_name>: must match a Rung.name from bots/trainer/ladder.json (e.g. "NullAI", "BARb/dev")
#   <iter_id>  : iteration counter string (e.g. "001", "smoke", "042")
#
# Produces a directory under bots/runs/ conforming to
# specs/020-bot-iterative-trainer/contracts/run-directory.md. On any exit path, a
# result.json is present — the bot writes it on clean termination, or we stub it here.
set -euo pipefail

if [[ $# -lt 2 ]]; then
  echo "Usage: $0 <rung_name> <iter_id>" >&2
  exit 64
fi

rung_name="$1"
iter_id="$2"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"

# Unwired-command parsing lives in lib/parse_unwired.sh — see that file's header
# for the upstream contract reference (../HighBarV2/specs/030-proxy-contract-docs/
# contracts/unwired-command-log.md) and the parser corrections vs. feature 021.
# shellcheck source=lib/parse_unwired.sh
source "$SCRIPT_DIR/lib/parse_unwired.sh"

LADDER="$SCRIPT_DIR/ladder.json"
# 023: BOT_SCRIPT selector — the runner can launch any .fsx in $SCRIPT_DIR.
# Default keeps the rush bot (bot.fsx) as the implicit choice for backward
# compatibility with 020/021/022 invocations. The macro bot is selected via
#   BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh <rung> <iter>
BOT_SCRIPT="${BOT_SCRIPT:-bot.fsx}"
export BOT_SCRIPT
BOT_FSX="$SCRIPT_DIR/$BOT_SCRIPT"

if ! command -v jq >/dev/null 2>&1; then
  echo "ERROR: jq is required but not installed" >&2
  exit 2
fi
if [[ ! -f "$LADDER" ]]; then
  echo "ERROR: missing $LADDER" >&2
  exit 2
fi
if [[ ! -f "$BOT_FSX" ]]; then
  echo "ERROR: missing $BOT_FSX (BOT_SCRIPT=$BOT_SCRIPT)" >&2
  exit 2
fi

# Verify we are on the feature branch (warn but do not block — caller may be on a detached state)
current_branch="$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown")"
if [[ "$current_branch" != "023-trainer-builder-economy" ]]; then
  echo "[run.sh] WARNING: not on feature branch (current: $current_branch)" >&2
fi

echo "[run.sh] iter=$iter_id rung=$rung_name bot_script=$BOT_SCRIPT"

# Parse ladder
map_name="$(jq -r '.map' "$LADDER")"
seed="$(jq -r '.seed' "$LADDER")"
rung_json="$(jq -c --arg name "$rung_name" '.rungs[] | select(.name == $name)' "$LADDER")"
if [[ -z "$rung_json" || "$rung_json" == "null" ]]; then
  echo "ERROR: rung '$rung_name' not found in $LADDER" >&2
  exit 3
fi
opponent="$(echo "$rung_json" | jq -r '.opponent')"
opponent_options="$(echo "$rung_json" | jq -c '.options')"
max_frames="$(echo "$rung_json" | jq -r '.max_frames')"

echo "[run.sh] ladder: map=$map_name opponent=$opponent options=$opponent_options max_frames=$max_frames"

# Build FSBar.Client so bot.fsx's #r directives find fresh DLLs (tests bin picks them up)
echo "[run.sh] dotnet build src/FSBar.Client.Tests ..."
dotnet build src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug --nologo --verbosity quiet >/dev/null

# Compose run directory name
iso_ts="$(date -u +'%Y-%m-%dT%H-%M-%S')"
rung_slug="$(echo "$rung_name" | tr '/' '-' | tr -c '[:alnum:]._-' '_' | sed 's/_*$//')"
run_dir="bots/runs/${iso_ts}_${rung_slug}_${iter_id}"
mkdir -p "$run_dir"
run_dir_abs="$(cd "$run_dir" && pwd)"
echo "[run.sh] run dir: $run_dir"

# Meta
engine_version="$(ls -1 "$HOME/.local/state/Beyond All Reason/engine/" 2>/dev/null | sort | tail -1)"
git_sha="$(git rev-parse --short HEAD 2>/dev/null || echo '0000000')"
host_name="$(hostname 2>/dev/null || cat /etc/hostname 2>/dev/null || echo 'unknown')"

jq -n \
  --arg iter_id "$iter_id" \
  --arg ts "$(date -u +'%Y-%m-%dT%H:%M:%SZ')" \
  --arg rung "$rung_name" \
  --arg opponent "$opponent" \
  --argjson opts "$opponent_options" \
  --arg map "$map_name" \
  --argjson seed "$seed" \
  --argjson max_frames "$max_frames" \
  --arg engine "$engine_version" \
  --arg sha "$git_sha" \
  --arg host "$host_name" \
  '{
    iter_id: $iter_id,
    start_timestamp: $ts,
    rung_name: $rung,
    opponent: $opponent,
    opponent_options: $opts,
    map: $map,
    seed: $seed,
    max_frames: $max_frames,
    engine_version: $engine,
    git_sha: $sha,
    host: $host
  }' > "$run_dir/meta.json"

# Snapshot bot + ladder
cp "$BOT_FSX" "$run_dir/bot.fsx.snapshot"
cp "$LADDER" "$run_dir/ladder.snapshot.json"

# Cleanup + stub traps
cleanup_called=0
write_stub_if_missing() {
  if [[ ! -f "$run_dir/result.json" ]]; then
    echo "[run.sh] result.json missing — writing stub"
    jq -n \
      --arg cause "$1" \
      --arg msg "$2" \
      '{
        outcome: "error",
        frames: 0,
        cause: $cause,
        victory_signal: null,
        error_message: $msg,
        telemetry: {
          commands_total: 0,
          units_built: 0,
          units_lost: 0,
          enemy_units_killed: 0,
          peak_metal: 0,
          peak_energy: 0,
          frames_survived: 0
        }
      }' > "$run_dir/result.json"
  fi
}

write_interrupted_stub() {
  jq -n \
    '{
      outcome: "interrupted",
      frames: 0,
      cause: "operator-interrupt (SIGINT)",
      victory_signal: null,
      error_message: null,
      telemetry: {
        commands_total: 0,
        units_built: 0,
        units_lost: 0,
        enemy_units_killed: 0,
        peak_metal: 0,
        peak_energy: 0,
        frames_survived: 0
      }
    }' > "$run_dir/result.json"
}

on_interrupt() {
  echo "[run.sh] SIGINT caught — killing engine processes and cleaning up"
  [[ -n "${bot_pid:-}" ]] && kill -TERM "$bot_pid" 2>/dev/null || true
  pkill -f "spring-headless" 2>/dev/null || true
  sleep 0.2
  write_interrupted_stub
  exit 130
}
trap on_interrupt INT

# Launch
export HIGHBAR_BOT_RUN_DIR="$run_dir_abs"
export BOT_OPPONENT="$opponent"
export BOT_OPPONENT_OPTIONS="$opponent_options"
export BOT_MAP="$map_name"
export BOT_SEED="$seed"
export BOT_MAX_FRAMES="$max_frames"
export BOT_GAME_SPEED="${BOT_GAME_SPEED:-100}"

echo "[run.sh] launching dotnet fsi $BOT_FSX ..."
set +e
dotnet fsi "$BOT_FSX" > "$run_dir/stdout.log" 2>&1 &
bot_pid=$!
wait $bot_pid
bot_exit=$?
set -e
echo "[run.sh] bot exit code: $bot_exit"

# Copy engine session logs. The session dir guid matches the socket filename — but because
# bot.fsx uses EngineConfig.defaultConfig() which generates a new guid every run, we can't
# recover it from our side deterministically. Grab the newest /tmp/fsbar-* dir modified
# during this run and copy its logs in.
newest_session=""
if compgen -G "/tmp/fsbar-*" >/dev/null; then
  newest_session="$(ls -1dt /tmp/fsbar-* 2>/dev/null | head -1)"
fi

if [[ -n "$newest_session" && -d "$newest_session" ]]; then
  echo "[run.sh] copying engine logs from $newest_session"
  [[ -f "$newest_session/stdout.log" ]] && cp "$newest_session/stdout.log" "$run_dir/engine.stdout" || touch "$run_dir/engine.stdout"
  [[ -f "$newest_session/stderr.log" ]] && cp "$newest_session/stderr.log" "$run_dir/engine.stderr" || touch "$run_dir/engine.stderr"
  [[ -f "$newest_session/infolog.txt" ]] && cp "$newest_session/infolog.txt" "$run_dir/engine.infolog" || touch "$run_dir/engine.infolog"
else
  echo "[run.sh] WARNING: no /tmp/fsbar-* session dir found"
  touch "$run_dir/engine.stdout" "$run_dir/engine.stderr" "$run_dir/engine.infolog"
fi

# Post-match unwired-command parsing per 022 FR-001..FR-004.
# Always writes unwired_commands.json (FR-003 always-emit invariant).
parse_unwired_stderr "$run_dir/engine.stderr" "$run_dir/unwired_commands.json"
echo "[run.sh] unwired_commands.json: rc_minus_2_count=$(jq -r '.rc_minus_2_count' "$run_dir/unwired_commands.json")"

write_stub_if_missing "bot-exit-without-result" "dotnet fsi exited with code $bot_exit"

# Summary
if [[ -f "$run_dir/result.json" ]]; then
  outcome="$(jq -r '.outcome' "$run_dir/result.json")"
  frames="$(jq -r '.frames' "$run_dir/result.json")"
  cause="$(jq -r '.cause' "$run_dir/result.json")"
  echo "[run.sh] result: outcome=$outcome frames=$frames cause=$cause"
fi

exit "$bot_exit"
