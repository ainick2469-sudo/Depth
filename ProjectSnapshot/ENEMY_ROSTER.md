# Enemy Roster

Gate VS-1.3.1 retires slime/blob enemies from normal gameplay and moves the active direction toward rig-friendly humanoids, quadrupeds, large quadrupeds, and flyers.

## Slime Retirement

- `Slime` and `SpitterSlime` remain creatable for debug/compatibility.
- Normal floor catalogs and encounter templates must exclude retired slime archetypes.
- The legacy `bounty.lantern_eater_slime` id is preserved, but it now targets a non-slime early prisoner bounty.

## Taxonomy

- Body plans: Humanoid, Quadruped, LargeQuadruped, Flying, EliteHumanoid, BossHumanoid, BossBeast.
- Factions: Prisoner, Goblin, Ratfolk, Undead, Beast, Cultist, Vampire, Werebeast, Orc, CursedNobility, DungeonConstruct, DeepHorror.
- Roles: Swarmer, Grunt, Shield, Archer, Charger, Ambusher, Trapper, Support, Summoner, Brute, Hunter, EliteDuelist, Boss.
- Floor bands: RecruitDungeon, OrganizedDungeon, TacticalDungeon, CursedOrders, GothicHunters, DeepHorrors.

## Active VS-1.3.1 Roster

- Floors 1-3: Torchless Prisoner, Candle Goblin, Mold-Covered Skeleton, Rusty Dagger Ratfolk, Dungeon Janitor Ghoul, Starved Dungeon Wolf, Coal-Eyed Alley Cat, Rust Bell Bat.
- Floors 4-7: Chain-Bound Thief, Goblin Shield Rat, Bone Archer Initiate, Lantern Cultist, Pickaxe Skeleton Miner, Cursed Kennel Wolf, Crypt Lynx, Ash-Eaten Prison Guard.
- Floors 8-12: Goblin Tripwire Trapper, Barrel-Head Bandit, Sewer Knife Twin, Rotten Bell Ringer, Crossbow Goblin, Bone-Mane Wolf, Dungeon Ram, Mossback Bear Cub.

## Behavior Status

- Implemented: existing simple melee, movement-speed tuning, windup tuning, hearing/group alert tuning, level scaling, bounty markers, primitive silhouette markers.
- Placeholder metadata: ranged projectiles, traps, summons, buffs, debuffs, shield blocking, fear/howl, teleport-style strikes.
- Unsupported attack families fall back to safe simple enemy behavior rather than spawning broken controllers.
