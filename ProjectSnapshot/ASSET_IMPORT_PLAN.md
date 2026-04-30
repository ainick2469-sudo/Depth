# Asset Import Plan

This plan prepares future curated imports from `C:\Users\nickb\FrontierDepths_AssetStaging`. It does not copy assets in this gate.

## Destination Structure

Use vendor isolation plus game-owned wrappers:

- `Assets/ThirdParty/AssetStore/<PackName>/...` for untouched vendor originals if assets are copied later.
- `Assets/Game/Art/Imported/Town/...` for curated town meshes/material variants that become game-owned.
- `Assets/Game/Art/Imported/Dungeon/...` for curated labyrinth module meshes/material variants.
- `Assets/Game/Art/Imported/Overworld/...` for rocks, roads, cliffs, and field/camp visual pieces.
- `Assets/Game/Art/Imported/Props/...` for selected barrels, crates, chests, signs, tools, and shrine dressing.
- `Assets/Game/Art/Imported/Weapons/...` for selected weapon display/enemy-equipment meshes.
- `Assets/Game/Art/Imported/Materials/...` for game-owned material variants or overrides.
- `Assets/Game/Prefabs/Town/...` for service/building wrappers.
- `Assets/Game/Prefabs/Dungeon/...` for labyrinth shell wrappers.
- `Assets/Game/Prefabs/Props/...` for curated props.
- `Assets/Game/Prefabs/Weapons/...` for weapon display/enemy equipment wrappers.

## First Recommended Imports

| Priority | Source | Destination Wrapper | Why | System | Cleanup | Risk | Timing |
| ---: | --- | --- | --- | --- | --- | --- | --- |
| 1 | `3DForge/.../Prefabs/Buildings/ForestVillage/Smithy.prefab` | `Assets/Game/Prefabs/Town/Blacksmith/BlacksmithBuilding.prefab` | Replace graybox blacksmith block with readable landmark. | Runtime town services | Scale, rotation, collider, material brightness. | Medium | Next town art gate |
| 2 | `3DForge/.../Prefabs/Forge/*` selected props | `Assets/Game/Prefabs/Town/Blacksmith/Props/...` | Add anvil, forge, tools, chains, blades. | Blacksmith kiosk dressing | Trim to selected props only. | Low | Next town art gate |
| 3 | `MedievalTavernPack/Prefabs/Furniture/Bar_01_mod.prefab` and tables/chairs | `Assets/Game/Prefabs/Town/Saloon/...` | Make Saloon/Inn visibly different. | Town service dressing | Wrapper prefabs and colliders. | Low | Next town art gate |
| 4 | `Mega Fantasy Props Pack/Prefabs/Houses/House.001.prefab` | `Assets/Game/Prefabs/Town/Quartermaster/QuartermasterBuilding.prefab` | Better shop/quartermaster shell. | Quartermaster service | Inspect scale and dependencies. | Medium | Next town art gate |
| 5 | `DungeonModularPack/Prefabs/Wall_A.prefab`, `Tile_A.prefab`, `Arch_A.prefab`, `Pillar_A.prefab` | `Assets/Game/Prefabs/Dungeon/ModularShell/...` | Replace graybox dungeon visuals while preserving generator. | Dungeon runtime renderer | Validate shader graph; use wrappers. | Medium | Dungeon visual shell gate |
| 6 | `Mega Fantasy Props Pack/Prefabs/Complex prefabs/Barrels.prefab`, `Boxes.prefab` | `Assets/Game/Prefabs/Props/Storage/...` | Room dressing and cache identity. | Room purpose visuals | Remove excess child clutter if needed. | Medium | Room identity gate |
| 7 | `RockFREE` selected rocks | `Assets/Game/Prefabs/Overworld/Rocks/...` | World-floor road/cave/field dressing. | Future overworld runtime | Inspect scale/colliders. | Low | VS-1.4.1+ overworld gate |
| 8 | `FlexUnit/MedievalWeaponPack/Prefabs/Shield.prefab` | `Assets/Game/Prefabs/Weapons/Enemy/Shield.prefab` | Improve shield enemy readability later. | Enemy visuals | Use as prop only; no controller changes. | Low | Enemy visual pass |

## Do Not Import Yet

- `ADoorToGaming/Dungeon Generation` scripts or sample scene.
- Whole `Caves and Dungeons` audio folder.
- Whole `Mega Fantasy Props Pack` folder.
- Any FPS controllers, cameras, demo managers, lighting managers, post-processing setups, or input systems.
- Any demo scene into Build Settings.

## Import Procedure For A Future Gate

1. Create a branch dedicated to the asset slice.
2. Copy only selected source assets and their required dependencies into `Assets/ThirdParty/AssetStore/<PackName>/...`.
3. Create wrapper or prefab variant under `Assets/Game/Prefabs/...`.
4. Normalize scale, rotation, colliders, material shader, and LOD choices in the wrapper.
5. Validate from the first-person camera in town/dungeon, not only the scene view.
6. Commit art imports separately from gameplay logic changes.
