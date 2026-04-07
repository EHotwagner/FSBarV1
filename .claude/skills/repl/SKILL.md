---
name: "repl"
description: "Start a headless BAR engine REPL in FSI. Restarts FSI, loads Repl.fsx, and opens the module."
user-invocable: true
disable-model-invocation: true
---

## Instructions

1. Use the `mcp__fsi-server__restart_fsi` MCP tool to restart FSI (picks up fresh DLLs).
2. Use the `mcp__fsi-server__send_fsharp_code` MCP tool to send this code (agentName: "repl"):
   ```
   #load "/home/developer/projects/FSBarV1/scripts/examples/Repl.fsx";; open Repl;;
   ```
3. Wait a moment, then check `mcp__fsi-server__get_recent_fsi_events` to confirm the script loaded (look for "FSBar REPL loaded" or `val help`).
4. Tell the user the REPL is ready. Remind them of key commands: `start()`, `step N`, `units()`, `economy()`, `help()`.
