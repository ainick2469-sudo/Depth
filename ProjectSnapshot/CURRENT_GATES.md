# Current Gates

## Latest Stable Gameplay Base

- `b575dd7 Gate VS-1.4.1F: improve labyrinth layout quality foundation`

## Current Gate

- `Gate VS-1.4.1G: Labyrinth Objective Path And Boss Approach Foundation`

Purpose:

- Add deterministic labyrinth objective-path metadata.
- Select an objective/key room, boss approach room, boss room placeholder, and exit/stairs room for every generated build.
- Keep exit/stairs locking metadata prepared but disabled by default so current playtesting remains unblocked.
- Prevent objective/boss placeholder rooms from becoming random cache/shrine/reward rooms.
- Expose objective roles to minimap/full-map metadata without revealing secret rooms or adding boss combat.

## Next Planned Gate

- `Gate VS-1.4.1H: Safe Room Merge Geometry And Irregular Room Shapes`

Use VS-1.4.1F/G metadata to safely reshape rooms after the objective spine is stable.
