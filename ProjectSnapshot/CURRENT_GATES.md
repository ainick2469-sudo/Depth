# Current Gates

## Latest Stable Gameplay Base

- `f9b7313 Gate VS-1.4.1C: add dungeon modular shell adapter`

## Current Gate

- `Gate VS-1.4.1D: Stabilize Dungeon Shell Traversal And Doorway Visuals`

Purpose:

- Make dungeon shell visuals validate against actual playable collision and clearance before hiding graybox renderers.
- Skip solid doorway wrappers, risky trim/caps/accent wrappers, and corridor wall wrappers until they can respect doorway/corridor clearances.
- Add `DungeonShellVisualMode` with AdapterVisuals, SafeGraybox fallback, and Off.
- Keep current dungeon generation, collisions, minimap, room purposes, spawns, stairs, pickups, town, enemies, and assets unchanged.

## Next Planned Gate

- `Gate VS-1.4.2: World Floor 1 Graybox Overworld Runtime`

Build the first simple runtime overworld wrapper around Frontier Outpost, the safe settlement, outer field, road, and existing Training Labyrinth gate.
