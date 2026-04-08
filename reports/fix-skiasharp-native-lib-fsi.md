# Fix: SkiaSharp Native Library Loading in FSI

**Date:** 2026-04-08
**Component:** FSBar.Viz / SkiaViewer (FSI REPL)

## Problem

When running `viz()` from the FSI REPL, the SkiaViewer window failed to initialize with:

```
[Viewer] Window thread error: The type initializer for 'SkiaSharp.SKImageInfo' threw an exception.
```

The underlying cause was that SkiaSharp's P/Invoke could not resolve `libSkiaSharp` at runtime. The native library exists at:

```
tests/FSBar.Viz.Tests/bin/Debug/net10.0/runtimes/linux-x64/native/libSkiaSharp.so
```

but .NET's DllImport resolver only searches the assembly directory (`bin/Debug/net10.0/`) and standard system paths — not the `runtimes/` subdirectory.

## What didn't work

- **`dlopen` with `RTLD_GLOBAL`** — The Repl.fsx script already calls `dlopen` with `RTLD_NOW | RTLD_GLOBAL` (flags `0x102`) to preload the native library. This loads the library into the process, but SkiaSharp's P/Invoke name resolution still fails because .NET's DllImport does not consult the global symbol table.

- **`NativeLibrary.Load` (full path)** — Same result. The library is loaded into the process but the P/Invoke short-name lookup (`libSkiaSharp`) still fails.

- **Preloading before `#r` references** — Restarting FSI and loading native libs before any SkiaSharp assembly references did not help, since the issue is name resolution, not load order.

## Fix

Created a symlink in the assembly directory so that .NET's DllImport resolver finds the library by its short name:

```bash
ln -sf runtimes/linux-x64/native/libSkiaSharp.so \
  tests/FSBar.Viz.Tests/bin/Debug/net10.0/libSkiaSharp.so
```

After this, `viz()` initializes successfully with the Vulkan backend:

```
[Viewer] Vulkan MSAA: max=8, using=4x
[Viewer] Backend selected: Vulkan (AMD Radeon Graphics (RADV RENOIR))
[Viewer] Surface created: 1024x640
```

## Notes

- The symlink is in a `bin/Debug/` output directory that is not committed to git. It will need to be recreated after a clean rebuild (`dotnet clean` / `dotnet build`).
- A more permanent fix would be to add a build target that copies or symlinks the native asset into the output directory, or to use `NativeLibrary.SetDllImportResolver` in the Viewer initialization code.
