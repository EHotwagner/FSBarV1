namespace FSBar.Hub

/// Locates and validates the HighBarV2 skirmish-AI proxy that the hub
/// installs into a user's BAR data dir (FR-006 / FR-006a).
///
/// On-disk layout (repo-relative):
///
///     proxy/
///     ├── bundled/
///     │   └── <version>/              # e.g. 0.1.17/
///     │       ├── libSkirmishAI.so
///     │       ├── AIInfo.lua
///     │       └── AIOptions.lua
///     ├── BUNDLED_VERSION             # single non-empty line = <version>
///     └── README.md
///
/// At runtime `resolve ()` picks the bundle root by checking, in order,
/// the `$FSBAR_HUB_BUNDLED_PROXY_DIR` env var (dev / test) and the
/// assembly-relative `proxy/` directory (installed build), then verifies
/// every required file exists. Reads are cheap; the caller is expected
/// to memoise the result for the life of the hub process.
module BundledProxy =

    /// Resolved bundle descriptor. Every path is absolute; every named
    /// file has been `File.Exists`-checked at resolution time.
    type BundledProxyInfo = {
        /// Bundle version, the single line in `BUNDLED_VERSION`.
        Version: string
        /// Absolute path to `proxy/bundled/<version>/`.
        BundleRoot: string
        /// `<BundleRoot>/libSkirmishAI.so`.
        LibSkirmishAiPath: string
        /// `<BundleRoot>/AIInfo.lua`.
        AiInfoLuaPath: string
        /// `<BundleRoot>/AIOptions.lua`.
        AiOptionsLuaPath: string
    }

    /// Reasons `resolve` may fail. Each carries the concrete path the
    /// check was against so the GUI can render an actionable message.
    type BundledProxyError =
        /// Could not find `BUNDLED_VERSION` — neither the env-var
        /// override nor the assembly-relative fallback exists.
        | VersionFileMissing of path: string
        /// `BUNDLED_VERSION` is empty, or contains more than a single
        /// non-blank line.
        | VersionFileMalformed of path: string
        /// `BUNDLED_VERSION` names a version whose `bundled/<v>/`
        /// sibling directory does not exist.
        | BundleDirMissing of path: string
        /// One of `libSkirmishAI.so` / `AIInfo.lua` / `AIOptions.lua`
        /// is absent from the bundle dir.
        | RequiredFileMissing of path: string

    /// Resolves the bundle. See module docs for the search order.
    val resolve: unit -> Result<BundledProxyInfo, BundledProxyError>

    /// Human-readable rendering of a `BundledProxyError`. Used by the
    /// GUI and by diagnostics logs.
    val formatError: BundledProxyError -> string
