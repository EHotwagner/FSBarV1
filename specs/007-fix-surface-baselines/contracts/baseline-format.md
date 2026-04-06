# Contract: Baseline File Format

## Overview

Each `.baseline` file is a verbatim copy of the corresponding `.fsi` signature file content. No transformation, normalization, or formatting is applied.

## File Naming

```
src/FSBar.Client.Tests/Baselines/{ModuleName}.baseline
```

Where `{ModuleName}` matches the `.fsi` filename without extension (e.g., `Commands.fsi` → `Commands.baseline`).

## Content

Exact text content of the `.fsi` file, including:
- Namespace declarations
- Module declarations
- Type definitions (records, DUs, classes, interfaces)
- `val` declarations with full signatures
- XML doc comments (`///`)
- Attributes (`[<RequireQualifiedAccess>]`, etc.)
- Whitespace and blank lines (preserved as-is)

## Comparison Semantics

Baseline passes when: `File.ReadAllText(baselinePath) = File.ReadAllText(fsiPath)`

String equality comparison. No normalization. Any difference — including whitespace, comments, or reordering — is treated as a surface change.

## Covered Modules (12)

BarClient, Callbacks, Commands, Connection, EngineConfig, EngineLauncher, Events, MapCache, MapGrid, MapQuery, Protocol, ScriptGenerator
