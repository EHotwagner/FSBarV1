# Quickstart — Unit Encyclopedia Filters (044)

## GUI walkthrough

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  FSBAR_HUB_INITIAL_TAB=Units \
  dotnet run --project src/FSBar.Hub.App
```

1. The Units tab opens with every unit visible. Above the list,
   three chip rows (Faction / Tier / Mobility) and a search input
   are now visible alongside a `"N of M units shown"` count.
2. Click **Arm** — list narrows to Armada units; count updates within
   one frame.
3. Click **T2** — list narrows to Armada T2; AND across categories.
4. Click **Cor** (in addition to **Arm**) — now Arm OR Cor T2.
5. Type `bomb` into the search input — narrows to names containing
   "bomb".
6. Click **Clear filters** — every chip deactivates, search empties,
   full list returns.
7. Switch to the Viewer tab and back — filters remain applied
   (session persistence, FR-008).

## FSI / scripting check

The new filter fields ride on the existing
`HubEvent.EncyclopediaSelectionChanged` stream. A script can observe
them end-to-end:

```fsharp
#load "src/FSBar.Hub/scripts/prelude.fsx"
open FSBar.Hub

let state = HubStateStore.current store
let entries = EncyclopediaData.buildFromBarData ()
let visible = EncyclopediaFilter.apply state.Encyclopedia entries
printfn "%d of %d units shown" (List.length visible) (List.length entries)
```

## Unit-test recipe

`tests/FSBar.Hub.Tests/EncyclopediaFilterTests.fs` covers:

| Scenario | Spec ref |
|----------|----------|
| Empty selection → every entry passes | FR-002 / R3 |
| Single faction chip → only that faction passes | US1 AS-1 |
| Two faction chips → OR within category | US1 AS-3 |
| Faction + Tier → AND across categories | US1 AS-2 |
| Air mobility + search "bomb" → intersects both | US2 AS-1 |
| Cleared selection after active → equal to `defaultSelection` | US1 AS-4 |
| Empty-match set → `apply` returns `[]`; caller renders empty-state | FR-007 |

## Surface-area baselines

After the `.fsi` edits, regenerate baselines:

```bash
SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Hub.Tests/FSBar.Hub.Tests.fsproj
```

Commit the updated `.baseline` files alongside the `.fsi` change.
