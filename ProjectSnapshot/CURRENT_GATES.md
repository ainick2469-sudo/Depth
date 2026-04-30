# Current Gates

## Latest Stable Gameplay Base

- `32546d3 Gate VS-1.4.1A: scan staging assets and plan integration`

## Current Gate

- `Gate VS-1.4.1B: Curated Town Asset Integration and Town Layout Replacement`

Purpose:

- Replace confusing runtime graybox town service placement with deterministic, front-facing service stations.
- Use a tiny curated identity-prop import from staging, not whole vendor packs.
- Load only game-owned wrapper prefabs from `Assets/Game/Resources/TownVisuals`.
- Preserve Blacksmith, Quartermaster, Saloon / Inn, Bounty Board, and existing scene Dungeon Gate behavior.
- Keep dungeon generation, enemies, skill trees, scene YAML, and ProjectSettings untouched.

## Next Planned Gate

- `Gate VS-1.4.1C: Dungeon Modular Shell Adapter`

Use `DungeonModularPack` as curated wrapper-prefab shell art without changing dungeon graph generation. Do not import or adopt `ADoorToGaming` generator scripts.
