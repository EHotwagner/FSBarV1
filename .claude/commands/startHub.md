Start the FSBar Hub GUI (`FSBar.Hub.App`).

## Steps

1. Run the hub in the background via Bash with `run_in_background: true`:

   ```bash
   XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
     dotnet run --project src/FSBar.Hub.App
   ```

2. Report the background shell ID to the user so they can monitor or stop it.

## Notes

- `XDG_RUNTIME_DIR=/tmp/runtime-developer` is required for GLFW windowing.
- `DISPLAY=:0` is required for the graphical window.
- The hub listens on `127.0.0.1:5021` for the gRPC scripting service.
- Do NOT pass `--no-build` unless the user explicitly asks — a fresh `dotnet run` ensures changes are picked up.
