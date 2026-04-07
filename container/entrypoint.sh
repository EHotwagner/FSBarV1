#!/bin/bash
# Minimal entrypoint for FSBar development container
# Creates XDG_RUNTIME_DIR and execs the user's command

export XDG_RUNTIME_DIR="${XDG_RUNTIME_DIR:-/tmp/runtime-developer}"
mkdir -p "$XDG_RUNTIME_DIR"

exec "$@"
