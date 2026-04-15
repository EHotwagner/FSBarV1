# Quickstart — Feature 027 Map Terrain Visualization

This quickstart shows a developer how to open the new brown/blue
elevation-shaded map viz over a committed cached map, browse between
maps without restarting the viewer, and fall back to the raw
`HeightMap` debug layer for comparison.

Prerequisites:
- Linux dev environment with `dotnet fsi`, a running X display
  (`DISPLAY=:0`) and `XDG_RUNTIME_DIR` set for GLFW (the project
  standard; see `CLAUDE.md` § FSI MCP Server).
- `FSBar.Viz` and `FSBar.Client` built at current HEAD on branch
  `027-map-terrain-viz`.
- At least one committed cache file under `bots/trainer/map-cache/`
  (today: `avalanche_3.4.json`).

---

## 1. Launch the cached preview with the default map

```bash
cd ~/projects/FSBarV1
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
    dotnet fsi src/FSBar.Viz/scripts/examples/04-base-terrain-cache.fsx
```

Expected:
- A SkiaViewer window opens titled "FSBar Preview".
- The default committed map loads (the first entry in
  `MapCacheFile.supportedMaps`).
- Terrain is rendered with land in dark→light brown (higher = lighter)
  and water in dark→light blue (deeper = darker). The shoreline is
  visibly sharp.
- Metal spots are visible as circular markers that pulse smoothly at
  roughly one-and-a-half-second cadence. The terrain color underneath
  each marker remains at least partially visible at all times.
- The map fills the window (auto-fit).

## 2. Launch the cached preview with a specific map by name

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
    dotnet fsi src/FSBar.Viz/scripts/examples/04-base-terrain-cache.fsx "Avalanche 3.4"
```

Expected:
- Same as step 1, but the named map is loaded immediately.
- If the name does not match any entry in `MapCacheFile.supportedMaps`,
  the viewer falls back to the first entry and prints a warning line
  to stderr naming the rejected name and the valid set.

## 3. Browse maps without restarting

Inside the running viewer window (focused), press:

- `]` or `.` → advance to the next supported map (wraps at the end).
- `[` or `,` → retreat to the previous supported map (wraps at zero).

Expected:
- The terrain image is replaced with the new map's terrain within a
  few seconds of the key press.
- The new map re-auto-fits the window.
- No stale markers or terrain fragments remain from the previous map.
- The stderr log shows a line of the form
  `[PreviewSession] Switched to map <name>`.

## 4. Pan, zoom, reset

Still inside the running window:

- Mouse wheel scroll → zoom toward the cursor (auto-fit disengages).
- Left-click and drag → pan (auto-fit disengages).
- `Home` key → `ResetView`: re-enables auto-fit and refits the current
  map.

Expected:
- Pan/zoom motions are smooth and snap to the cursor.
- `Home` instantly re-fits the current map and re-enables auto-fit.
- Resizing the window while auto-fit is on re-fits; resizing after a
  pan/zoom preserves the user's view scale.

## 5. Switch to the raw HeightMap debug view

Inside the running window, press `Key.Number1` — this is the existing
binding for `LayerKind.HeightMap`. The viewer switches to the old raw
heightmap rendering.

Press `Key.BaseTerrainKey` (to be assigned during implementation; likely
a free key like `Key.B`) to return to the new `BaseTerrain` layer.

Expected:
- Both layers render over the same cache with no reload.
- The old `HeightMap`, `SlopeMap`, `ResourceMap`, etc. layers are still
  selectable (FR-016).

## 6. Fault injection — intentionally broken cache file

Rename `bots/trainer/map-cache/avalanche_3.4.json` aside and re-run
step 1:

```bash
mv bots/trainer/map-cache/avalanche_3.4.json /tmp/avalanche-backup.json
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
    dotnet fsi src/FSBar.Viz/scripts/examples/04-base-terrain-cache.fsx
```

Expected:
- The viewer opens but displays a formatted error banner naming the
  map and the exact `MapCacheFile.LoadError` (e.g., `FileMissing
  /.../avalanche_3.4.json`) — no crash, no blank window.

Restore the file:
```bash
mv /tmp/avalanche-backup.json bots/trainer/map-cache/avalanche_3.4.json
```

---

## Acceptance mapping

| Spec scenario                  | Step |
|--------------------------------|------|
| US1 AC 1 (brown ramp)          | 1    |
| US1 AC 2 (blue ramp)           | 1    |
| US1 AC 3 (multi-map fit)       | 1, 3 |
| US1 AC 4 (bad cache error)     | 6    |
| US2 AC 1 (N markers)           | 1    |
| US2 AC 2 (pulse visible)       | 1    |
| US2 AC 3 (terrain visible)     | 1    |
| US2 AC 4 (zero-metal map)      | (future) |
| US3 AC 1 (no live game)        | 1    |
| US3 AC 2 (fast switch)         | 3    |
| US3 AC 3 (list matches cache)  | 3    |
| FR-009a (resize + autofit)     | 4    |
| FR-016 (HeightMap still works) | 5    |

US2 AC 4 (zero-metal map) is covered by the unit test
`MapQueryMetalSpotsTests.empty grid → empty array`, not by the
quickstart — we only have one cached map today and it has metal.
