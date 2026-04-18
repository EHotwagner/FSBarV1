# Feature 040 baselines

This directory holds the SC-007 baseline(s) captured before feature-040 F# changes
begin, used by the Phase 9 polish task T094 to diff post-feature behaviour and
prove the existing scripting contract (feature 039) still works byte-identically.

## 16-hub-admin.baseline.txt

**Not yet captured.** The admin-channel script requires a running
`FSBar.Hub.App` instance on `127.0.0.1:5021` plus an active session launched via
the Setup tab. Run before starting Phase 3 (US1) F# changes:

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet run --project src/FSBar.Hub.App  # launch via Setup tab, then in a second shell:
dotnet fsi scripts/examples/16-hub-admin.fsx \
  > specs/040-grpc-full-hub-ui/baselines/16-hub-admin.baseline.txt
```

T094 diffs the post-feature script output against this baseline (byte-equivalent
required). Phase-1 setup only extends the proto + stubs the new RPC overrides,
so the pre-flight behaviour of every feature-039 admin RPC is unchanged.
