# Bundled HighBarV2 Proxy

This directory ships the HighBarV2 skirmish AI binary + Lua descriptors with
the FSBar repo so the hub (feature `035-central-gui-hub`) can install the
proxy into a user's BAR data directory without requiring a sibling HighBarV2
checkout (FR-006a).

## Layout

```
proxy/
├── bundled/
│   └── <version>/                 # e.g. 0.1.17/
│       ├── libSkirmishAI.so       # native proxy binary
│       ├── AIInfo.lua             # BAR skirmish AI descriptor
│       └── AIOptions.lua          # BAR skirmish AI options descriptor
├── BUNDLED_VERSION                # single line plain text: the active <version>
└── README.md                      # this file
```

The hub reads `BUNDLED_VERSION` at startup and expects exactly one matching
`bundled/<version>/` subdirectory containing all three files.

## Refreshing the bundled proxy (maintainer)

Maintainers refresh the bundle from a sibling HighBarV2 checkout with:

```bash
scripts/refresh-bundled-proxy.sh <new-version>
```

By default the script reads from `../HighBarV2/build/`; override with the
`HIGHBARV2_REPO` environment variable. The script refuses to overwrite an
existing `bundled/<version>/` directory unless `--force` is passed, and
rewrites `BUNDLED_VERSION` last so hub readers never see a torn state.

## Runtime resolution

The hub resolves the bundle root in this order:

1. `$FSBAR_HUB_BUNDLED_PROXY_DIR` — for dev / test runs.
2. Assembly-relative `proxy/` — for installed builds.

See `src/FSBar.Hub/BundledProxy.fsi` for the canonical resolution contract.
