#!/usr/bin/env bash
# bots/trainer/run.sh — launch one trainer iteration and materialise a conformant run directory.
#
# Usage: bash bots/trainer/run.sh <rung_name> <iter_id> [OPTIONS]
#   <rung_name>: must match a Rung.name from bots/trainer/ladder.json (e.g. "NullAI", "BARb/dev")
#   <iter_id>  : iteration counter string (e.g. "001", "smoke", "042")
#
# Options:
#   --viewer           Open FSBar.Viz viewer window (implies --speed 3 unless overridden)
#   --full-viz         Viewer with all overlays, terrain, speed 2, no frame limit
#   --speed <1-5|max>  Set game speed (1=realtime, 2=5x, 3=10x, 4=20x, 5=50x, max=100x)
#   --map <name>       Override map (default: from ladder.json)
#   --bot <script>     Override bot script (default: bot.fsx)
#   --opponent <name>  Override opponent AI (default: from ladder.json)
#   --profile <name>   Override opponent profile (default: from ladder.json)
#
# Produces a directory under bots/runs/ conforming to
# specs/020-bot-iterative-trainer/contracts/run-directory.md. On any exit path, a
# result.json is present — the bot writes it on clean termination, or we stub it here.
set -euo pipefail

if [[ $# -lt 2 ]]; then
  echo "Usage: $0 <rung_name> <iter_id> [OPTIONS]" >&2
  echo "" >&2
  echo "Options:" >&2
  echo "  --viewer           Open FSBar.Viz viewer window (implies --speed 3 unless overridden)" >&2
  echo "  --full-viz         Viewer with all overlays, terrain, speed 2, no frame limit" >&2
  echo "  --speed <1-5|max>  Set game speed (1=realtime, 2=5x, 3=10x, 4=20x, 5=50x, max=100x)" >&2
  echo "  --map <name>       Override map (default: from ladder.json)" >&2
  echo "  --bot <script>     Override bot script (default: bot.fsx)" >&2
  echo "  --opponent <name>  Override opponent AI (default: from ladder.json)" >&2
  echo "  --profile <name>   Override opponent profile (default: from ladder.json)" >&2
  exit 64
fi

rung_name="$1"
iter_id="$2"
shift 2

# Parse optional CLI arguments
opt_viewer=""
opt_full_viz=""
opt_speed=""
opt_map=""
opt_bot=""
opt_opponent=""
opt_profile=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --viewer)
      opt_viewer=1
      shift
      ;;
    --full-viz)
      opt_full_viz=1
      opt_viewer=1
      shift
      ;;
    --speed)
      if [[ -z "${2:-}" ]]; then
        echo "ERROR: --speed requires a value (1-5 or max)" >&2
        exit 64
      fi
      opt_speed="$2"
      shift 2
      ;;
    --map)
      if [[ -z "${2:-}" ]]; then
        echo "ERROR: --map requires a non-empty value" >&2
        exit 64
      fi
      opt_map="$2"
      shift 2
      ;;
    --bot)
      if [[ -z "${2:-}" ]]; then
        echo "ERROR: --bot requires a non-empty value" >&2
        exit 64
      fi
      opt_bot="$2"
      shift 2
      ;;
    --opponent)
      if [[ -z "${2:-}" ]]; then
        echo "ERROR: --opponent requires a non-empty value" >&2
        exit 64
      fi
      opt_opponent="$2"
      shift 2
      ;;
    --profile)
      if [[ -z "${2:-}" ]]; then
        echo "ERROR: --profile requires a non-empty value" >&2
        exit 64
      fi
      opt_profile="$2"
      shift 2
      ;;
    *)
      echo "ERROR: unknown option: $1" >&2
      exit 64
      ;;
  esac
done

# Validate --speed
if [[ -n "$opt_speed" ]]; then
  case "$opt_speed" in
    1|2|3|4|5|max) ;;
    *)
      echo "ERROR: --speed must be 1-5 or 'max' (got: $opt_speed)" >&2
      exit 64
      ;;
  esac
fi

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
# compatibility with 020/021/022 invocations. CLI --bot overrides env var.
if [[ -n "$opt_bot" ]]; then
  BOT_SCRIPT="$opt_bot"
elif [[ -z "${BOT_SCRIPT:-}" ]]; then
  BOT_SCRIPT="bot.fsx"
fi
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
if [[ "$current_branch" != "029-trainer-viewer-options" ]]; then
  echo "[run.sh] WARNING: not on feature branch (current: $current_branch)" >&2
fi

# Map speed level to engine game speed value (FR-009)
map_speed_level() {
  case "$1" in
    1)   echo 1   ;;
    2)   echo 5   ;;
    3)   echo 10  ;;
    4)   echo 20  ;;
    5)   echo 50  ;;
    max) echo 100 ;;
  esac
}

# Determine speed level and game speed
if [[ -n "$opt_speed" ]]; then
  BOT_SPEED_LEVEL="$opt_speed"
  BOT_GAME_SPEED="$(map_speed_level "$opt_speed")"
elif [[ -n "$opt_full_viz" && -z "${BOT_GAME_SPEED:-}" ]]; then
  # 031: full-viz default is speed 2 (5x) when no explicit --speed
  BOT_SPEED_LEVEL="2"
  BOT_GAME_SPEED="5"
elif [[ -n "$opt_viewer" && -z "${BOT_GAME_SPEED:-}" ]]; then
  # FR-010: viewer default is speed 3 (10x) when no explicit --speed
  BOT_SPEED_LEVEL="3"
  BOT_GAME_SPEED="10"
else
  BOT_SPEED_LEVEL="${BOT_SPEED_LEVEL:-max}"
  BOT_GAME_SPEED="${BOT_GAME_SPEED:-100}"
fi

# Export viewer flag
if [[ -n "$opt_viewer" ]]; then
  export BOT_VIEWER=1
fi

# Export full-viz flag
if [[ -n "$opt_full_viz" ]]; then
  export BOT_FULL_VIZ=1
fi

export BOT_SPEED_LEVEL
export BOT_GAME_SPEED

echo "[run.sh] iter=$iter_id rung=$rung_name bot_script=$BOT_SCRIPT viewer=${BOT_VIEWER:-0} full_viz=${BOT_FULL_VIZ:-0} speed=$BOT_SPEED_LEVEL ($BOT_GAME_SPEED)"

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

# Apply CLI overrides for map, opponent, profile (T004)
if [[ -n "$opt_map" ]]; then
  map_name="$opt_map"
fi
if [[ -n "$opt_opponent" ]]; then
  opponent="$opt_opponent"
fi
if [[ -n "$opt_profile" ]]; then
  opponent_options="{\"profile\":\"$opt_profile\"}"
fi

echo "[run.sh] ladder: map=$map_name opponent=$opponent options=$opponent_options max_frames=$max_frames"

# Build FSBar.Client so bot.fsx's #r directives find fresh DLLs (tests bin picks them up)
echo "[run.sh] dotnet build tests/FSBar.Client.Tests ..."
dotnet build tests/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug --nologo --verbosity quiet >/dev/null

# When viewer is active, also build FSBar.Viz.Tests so viewer.fsx can #r the viz DLLs
if [[ "${BOT_VIEWER:-}" == "1" ]]; then
  echo "[run.sh] dotnet build tests/FSBar.Viz.Tests (for viewer) ..."
  dotnet build tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj -c Debug --nologo --verbosity quiet >/dev/null
fi

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
  --argjson viewer "${BOT_VIEWER:-0}" \
  --argjson full_viz "${BOT_FULL_VIZ:-0}" \
  --arg speed_level "$BOT_SPEED_LEVEL" \
  --arg map_override "${opt_map:-}" \
  --arg bot_script "$BOT_SCRIPT" \
  --arg opponent_override "${opt_opponent:-}" \
  --arg opponent_profile "${opt_profile:-}" \
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
    host: $host,
    viewer: ($viewer == 1),
    full_viz: ($full_viz == 1),
    speed_level: $speed_level,
    map_override: (if $map_override == "" then null else $map_override end),
    bot_script: $bot_script,
    opponent_override: (if $opponent_override == "" then null else $opponent_override end),
    opponent_profile: (if $opponent_profile == "" then null else $opponent_profile end)
  }
  | if .full_viz then . + {initial_overlays: ["Units", "Events", "MetalSpots", "EconomyHud", "WeaponRanges", "SightRanges", "CommandQueue", "FullNames"]} else . end' > "$run_dir/meta.json"

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
# BOT_GAME_SPEED and BOT_VIEWER already exported above

echo "[run.sh] launching dotnet fsi $BOT_FSX ..."
set +e
if [[ "${BOT_VIEWER:-}" == "1" ]]; then
  # With viewer: tee stdout so operator sees output and the log file is still written.
  # Ensure DISPLAY is set for the SkiaViewer window (GLFW needs it).
  export DISPLAY="${DISPLAY:-:0}"
  export XDG_RUNTIME_DIR="${XDG_RUNTIME_DIR:-/tmp/runtime-$(id -u)}"
  dotnet fsi "$BOT_FSX" 2>&1 | tee "$run_dir/stdout.log" &
  bot_pid=$!
else
  dotnet fsi "$BOT_FSX" > "$run_dir/stdout.log" 2>&1 &
  bot_pid=$!
fi
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
