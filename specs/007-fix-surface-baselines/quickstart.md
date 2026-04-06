# Quickstart: Surface-Area Baseline Tests

## Run baseline tests

```bash
dotnet test src/FSBar.Client.Tests/ --filter "SurfaceArea"
```

## Regenerate baselines after intentional API changes

```bash
UPDATE_BASELINES=true dotnet test src/FSBar.Client.Tests/ --filter "SurfaceArea"
```

Then review the changes:

```bash
git diff src/FSBar.Client.Tests/Baselines/
```

## Generate initial baselines (first time)

Same as regeneration — run with `UPDATE_BASELINES=true` to create all baseline files from current `.fsi` content.

## Add a new module

1. Create `NewModule.fsi` and `NewModule.fs` in `src/FSBar.Client/`
2. Run tests — the surface-area test will fail, reporting the missing baseline
3. Run with `UPDATE_BASELINES=true` to generate the baseline
4. Commit the new `.baseline` file alongside the module
