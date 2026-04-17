#!/usr/bin/env bash
# hub-spawn-engine.sh — thin wrapper that installs prctl(PR_SET_PDEATHSIG)
# on the calling process before execing spring-headless. Ensures the
# BAR engine is killed by the kernel if the hub process dies by any
# means — including SIGKILL or segfault — that bypasses the hub's
# SIGTERM handler.
#
# Usage:
#   hub-spawn-engine.sh <engine-binary> [engine args...]
#
# Intended to replace direct `Process.Start(spring-headless, ...)`
# calls in FSBar.Client.EngineLauncher so the hub's
# ProcessLifetime.sweepChildEngines path has a belt-and-braces
# counterpart for the hub-crash case. EngineLauncher integration is
# tracked as a follow-up (the trainer would also benefit).
#
# Requires: Linux (prctl is a Linux-specific syscall). The script is
# a no-op wrapper on other platforms — it still exec's the binary
# but the kernel-level parent-death tracking only takes effect on
# Linux.

set -euo pipefail

if [[ $# -lt 1 ]]; then
    echo "usage: $(basename "$0") <engine-binary> [engine args...]" >&2
    exit 2
fi

BINARY="$1"; shift

# The prctl call must run in the same process that exec's the binary
# so the signal setting is inherited across exec. We shell out to
# `setpriv --pdeathsig SIGTERM` when available (util-linux 2.33+);
# fall back to a Python one-liner when setpriv lacks the flag.

if command -v setpriv >/dev/null 2>&1 && setpriv --help 2>&1 | grep -q pdeathsig; then
    exec setpriv --pdeathsig SIGTERM -- "$BINARY" "$@"
fi

if command -v python3 >/dev/null 2>&1; then
    exec python3 - "$BINARY" "$@" <<'PYEOF'
import ctypes, ctypes.util, os, signal, sys
libc = ctypes.CDLL(ctypes.util.find_library("c"))
PR_SET_PDEATHSIG = 1
libc.prctl(PR_SET_PDEATHSIG, signal.SIGTERM, 0, 0, 0)
os.execv(sys.argv[1], sys.argv[1:])
PYEOF
fi

echo "warning: neither setpriv --pdeathsig nor python3 available; parent-death tracking disabled" >&2
exec "$BINARY" "$@"
