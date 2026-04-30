# Asset Town Selection

Gate: `VS-1.4.1B`

## Selection Summary

The town visual pass uses a hybrid-minimal import. The goal is service identity and layout readability, not full building replacement.

## Selected Assets

### Blacksmith

- Source pack: `3DForge`
- Selected identity props: anvil, forgebase, large workbench, tool rack, hammer.
- Reason: these props immediately communicate blacksmith identity without importing the full Smithy exterior and its large texture set.
- Runtime wrapper: `TownVisuals/BlacksmithVisual`.

### Saloon / Inn

- Source pack: `MedievalTavernPack`
- Selected identity props: bar counter, table, chair, barrel.
- Reason: a bar counter and tavern props make the Saloon/Inn readable without a full building import.
- Runtime wrapper: `TownVisuals/SaloonInnVisual`.

### Quartermaster

- Source: game-owned fallback composition.
- Reason: the staging scan found usable generic houses, but their dependencies are not needed for this pass. A crate/counter/awning wrapper is safer.
- Runtime wrapper: `TownVisuals/QuartermasterVisual`.

### Bounty Board

- Source: game-owned fallback composition.
- Reason: no perfect dedicated bounty board prefab was found. A clear board/sign wrapper is safer and more readable now.
- Runtime wrapper: `TownVisuals/BountyBoardVisual`.

## Explicitly Not Selected

- `ADoorToGaming/Dungeon Generation`: deferred reference-only pack.
- Full `3DForge` Smithy exterior: deferred because its exterior texture dependencies are large.
- Demo scenes: staging-only references.
- `Caves and Dungeons`: audio-only future reference, not town visuals.
- Weapon packs: not needed for town layout except future decorative equipment.

## Next Recommended Asset Pass

After the town layout is manually validated, the next art-safe pass should be `VS-1.4.1C: Dungeon Modular Shell Adapter`, using `DungeonModularPack` wrappers without changing dungeon graph generation.
