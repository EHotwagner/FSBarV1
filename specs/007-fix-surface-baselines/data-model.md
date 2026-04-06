# Data Model: Fix Missing Baseline Surface FSI Coverage

## Entities

### Baseline File

A text file storing the canonical content of one `.fsi` signature file at a known-good point in time.

| Attribute | Description |
|-----------|-------------|
| Module Name | Derived from filename (e.g., `Commands` from `Commands.baseline`) |
| Content | Exact text content of the corresponding `.fsi` file |
| File Path | `src/FSBar.Client.Tests/Baselines/{ModuleName}.baseline` |

**Validation rules**:
- Content must be non-empty (every `.fsi` file declares at least a namespace)
- One-to-one relationship with `.fsi` files in `src/FSBar.Client/`

### FSI Signature File (existing)

The source-of-truth `.fsi` files in `src/FSBar.Client/`. Not modified by this feature.

| Attribute | Description |
|-----------|-------------|
| Module Name | Derived from filename (e.g., `Commands` from `Commands.fsi`) |
| Content | Public API surface declarations |
| File Path | `src/FSBar.Client/{ModuleName}.fsi` |

## Relationships

```
FSI Signature File 1:1 Baseline File
  - Each .fsi MUST have exactly one .baseline
  - Baseline content MUST match .fsi content (when tests pass)
  - Missing baseline = test failure
  - Content mismatch = test failure with diff
```

## State Transitions

```
Baseline File States:
  [Missing] --generate--> [Current]
  [Current] --fsi change--> [Stale] (detected by test failure)
  [Stale]   --regenerate--> [Current]
```
