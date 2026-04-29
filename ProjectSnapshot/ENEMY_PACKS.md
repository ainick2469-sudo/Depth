# Enemy Packs

Enemy packs are data-driven room composition templates used by `DungeonEncounterDirector`. They must respect floor range, room capacity, safe/transit room exclusion, and spawn spacing.

## Starter Packs

- BeginnerRoom: Torchless Prisoner + Mold-Covered Skeleton.
- FirstFastPressure: Rusty Dagger Ratfolk + Candle Goblin.
- FirstBeastRoom: Starved Dungeon Wolf + Mold-Covered Skeleton.
- FirstRangedPressure: Goblin Shield Rat + Bone Archer Initiate.
- FirstTrapRoom: Goblin Tripwire Trapper + Crossbow Goblin.
- FirstSupportRoom: Lantern Cultist + Cursed Kennel Wolf + Candle Goblin.
- FirstAmbushRoom: Crypt Lynx + Chain-Bound Thief.
- FirstBruteRoom: Dungeon Janitor Ghoul + Pickaxe Skeleton Miner.
- FirstBeastPack: Cursed Kennel Wolf + Bone-Mane Wolf + Rust Bell Bat.

## Rules

- No pack may include `Slime` or `SpitterSlime`.
- Solo fallback remains available when room capacity, spacing, or budget cannot support a pack.
- Large quadruped packs start at floor 8+ and should not overfill small rooms.
- Entry, transit-up, and transit-down rooms stay safe unless a future explicit exception is added.
