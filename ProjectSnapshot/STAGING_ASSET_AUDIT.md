# Staging Asset Audit

Gate: `VS-1.4.1A`

Staging project: `C:\Users\nickb\FrontierDepths_AssetStaging`

Main project: `C:\UnitySkill\FrontierDepths`

This audit is read-only. No assets have been copied into FrontierDepths, no packs have been imported into the main project, and no git commands are required or expected in the staging project.

## Staging Overview

| Pack / Folder | Prefabs | Models | Materials | Textures | Scenes | Scripts | Shaders | Risk | Recommended Usage |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- |
| `3DForge` | 96 | 112 | 16 | 42 | 2 | 0 | 0 | Medium | Strong town/forge candidate; use selected village exteriors, smithy, forge props, fences, doors, barrels, crates, lights. |
| `ADoorToGaming` | 8 | 1 | 3 | 0 | 1 | 4 | 0 | High | Dungeon generator reference only; do not import scripts into runtime yet. |
| `Caves and Dungeons` | 0 | 0 | 0 | 0 | 0 | 0 | 0 | High | Audio reference only for now; very large WAV payloads should not enter main repo without explicit audio/LFS policy. |
| `DungeonModularPack` | 13 | 13 | 7 | 22 | 2 | 0 | 1 | Medium | Good first modular dungeon visual-shell kit; inspect shader graph/material pipeline before import. |
| `EmaceArt` | 238 | 213 | 16 | 13 | 3 | 0 | 1 | Medium | Overworld Slavic/ruin/landmark candidates; avoid demo scenes and custom grass shader until tested. |
| `FlexUnit` | 14 | 14 | 1 | 4 | 2 | 0 | 0 | Low | Lightweight medieval weapon candidates for props/enemy equipment, not player guns. |
| `MedievalTavernPack` | 44 | 35 | 42 | 84 | 2 | 0 | 0 | Low | Strong tavern/inn interior and furniture candidate. |
| `Mega Fantasy Props Pack` | 284 | 280 | 59 | 148 | 5 | 0 | 0 | Medium | Broad prop/chest/house/castle/fence source; avoid bundled FPS controller prefab and demo scenes. |
| `RockFREE` | 6 | 18 | 8 | 32 | 1 | 0 | 0 | Low | Rocks/cliffs/cave dressing for overworld and dungeon entrances. |
| `Scenes`, `Settings`, `TutorialInfo`, `_TerrainAutoUpgrade` | 0 | 0 | 0 | 1 | 1 | 2 | 0 | High | Unity sample/settings/tutorial leftovers; do not integrate. |

## Town Assets

- Best first source: `3DForge/FantasyExteriors/Village & Towns` for town shell pieces, `Smithy.prefab`, `BasicHouse.prefab`, fences, gates, stairs, doors, and road/earth/cobblestone textures.
- Best service dressing source: `3DForge/Fantasy_Interiors/Villages_&_Towns/Prefabs/Forge` for blacksmith workbench, anvil, forge, bellows, chains, blades, tool racks, and scrap.
- Best tavern/inn source: `MedievalTavernPack/Prefabs` for bar pieces, tables, chairs, lamps, fireplaces, architecture modules, barrels, chandelier, and tableware.
- Best generic town prop source: `Mega Fantasy Props Pack/Prefabs` for houses, fences, benches, barrels, boxes, gates, castle-wall accents, books, and misc props.
- Needed but not obvious from scan: a dedicated bounty board/sign asset. Use generic sign/board/wood prop wrappers first if a perfect board is not found.

## Dungeon / Labyrinth Assets

- Best first source: `DungeonModularPack/Prefabs` with `Wall_A/B/C`, `Tile_A/B`, `Arch_A`, `Pillar_A/B`, `Step_A`, `Torch_A/B`, and handrails.
- Secondary source: `Mega Fantasy Props Pack` castle walls, towers, stone gates, columns, stairs, chests, boxes, barrels, and dungeon dressing.
- Accent source: `3DForge/Fantasy_Interiors` stone floors, doors, pillars, webs, wall lights, containers, crates, and forge chains.
- Defer: `ADoorToGaming/Dungeon Generation` scripts. Use only as architecture reference on a spike branch if desired later.

## Overworld Assets

- Best first source: `RockFREE` for rocks, cliff/cave-edge dressing, and simple landmark silhouettes.
- Useful source: `EmaceArt/Slavic World Free` for overworld village/ruin flavor after shader/scene risk is inspected.
- Useful source: `3DForge/FantasyExteriors/Village & Towns` for roads, fences, gates, stairs, village pieces, decks, plinths, and earth/cobblestone surfaces.
- Defer: terrain auto-upgrade assets and any pipeline-specific scene lighting.

## Weapons / Combat Assets

- `FlexUnit/MedievalWeaponPack` has bow, arrow, sword, axe, dagger, shield, spear, mace, hammer, club, gladius, saber, and scythe props.
- `3DForge/Fantasy_Interiors/.../Forge_Blades` has decorative blades, swords, and weapon racks suitable for blacksmith dressing.
- No clear revolver/rifle replacement candidate was found in the staging scan. Keep current Frontier Revolver path separate from this gate.

## Deferred / Unsafe Assets

- `ADoorToGaming/Dungeon Generation/Scripts` includes runtime generator code that could fight the existing dungeon architecture.
- All demo/sample scenes are staging-only references and should not be copied into Build Settings.
- Shader Graph/custom shader files need render-pipeline validation before materials are committed.
- `Caves and Dungeons` WAVs are very large and should not be committed without deliberate audio selection.
- Vendor controllers/managers, including `Mega Fantasy Props Pack/Example Scenes/Prefabs/FPSController.prefab`, must not enter runtime gameplay.
