---
name: "proto-regen"
description: "Regenerate FSBar.Proto F# code from proto/highbar/*.proto and proto/hub/scripting.proto via buf generate. Use when a .proto file changed and the committed src/FSBar.Proto/Generated/*.gen.fs files need refreshing."
user-invocable: true
---

## Regenerate

```bash
cd proto && buf generate
```

Generated files land under `src/FSBar.Proto/Generated/` and are committed so plain `dotnet build` works without the plugin.

## Plugin install (first time only)

`protoc-gen-fsgrpc` is not on nuget.org and no prebuilt binary is distributed. Install from source via the sibling repo helper:

```bash
~/tools/fsGRPCSkills/fsgrpc-setup/scripts/install-protoc-gen-fsgrpc.sh
```

The script clones `dmgtech/fsgrpc@a52b8a7`, patches it to skip optics emission (so generated code compiles against `FsGrpc 1.0.6`), publishes for the current TFM, and drops a wrapper at `~/.local/bin/protoc-gen-fsgrpc`. See `--help` for why the patch is necessary.

## Verify

1. `dotnet build FSBarV1.slnx` succeeds.
2. `git diff src/FSBar.Proto/Generated/` shows no gratuitous rewrites — only the intended change.
3. For wire-breaking checks, run `buf breaking` (feature 040's SC-007 additive-only guard relies on this).
