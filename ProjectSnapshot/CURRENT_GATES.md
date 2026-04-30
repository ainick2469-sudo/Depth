# Current Gates

## Latest Stable Gameplay Base

- `c163b65 Gate VS-1.4.1B: integrate curated town visuals and layout`

## Current Gate

- `Gate VS-1.4.1C: Dungeon Modular Shell Adapter`

Purpose:

- Add a visual-only dungeon shell adapter over the existing `DungeonSceneController` build path.
- Load only game-owned wrapper prefabs from `Assets/Game/Resources/DungeonVisuals`.
- Keep current dungeon graph generation, collision, spawning, minimap, stairs, pickups, and room-purpose behavior authoritative.
- Do not import the Dungeon Generation Pack, demo scenes, vendor scripts, scene YAML, or ProjectSettings churn.
- Use primitive/game-owned wrappers for this gate; raw dungeon vendor assets remain deferred.

## Next Planned Gate

- `Gate VS-1.4.2: World Floor 1 Graybox Overworld Runtime`

Build the first simple runtime overworld wrapper around Frontier Outpost, the safe settlement, outer field, road, and existing Training Labyrinth gate.
