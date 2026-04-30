# Asset Candidate List

Source: `C:\Users\nickb\FrontierDepths_AssetStaging`

No assets are copied in this gate. This list identifies likely candidates for future curated wrapper prefabs.

## Town / Settlement

- Blacksmith: `3DForge/FantasyExteriors/Village & Towns/Prefabs/Buildings/ForestVillage/Smithy.prefab`.
- Blacksmith props: `3DForge/Fantasy_Interiors/Villages_&_Towns/Prefabs/Forge`.
- Tavern / Inn: `MedievalTavernPack/Prefabs/Furniture`, `Architecture`, `Ornaments`, and `Tableware`.
- Quartermaster / merchant: `3DForge/.../BasicHouse.prefab`, `Mega Fantasy Props Pack/Prefabs/Houses`.
- Bounty board / signs: use generic sign/wood/board candidates from `Mega Fantasy Props Pack` or build a wrapper from planks if no exact board is found.
- Roads/fences/gates: `3DForge/FantasyExteriors/Village & Towns/Prefabs/Fences`, road/earth/cobblestone textures, decks, stairs, and gates.
- Lamps/torches: `3DForge/Fantasy_Interiors/.../Prefabs/Props/Lighting`, `MedievalTavernPack/Prefabs/Furniture/Lamp_01.prefab`.
- Barrels/crates/carts: `3DForge` containers and `Mega Fantasy Props Pack/Prefabs/Complex prefabs`.

## Dungeon / Labyrinth

- Modular floors/walls: `DungeonModularPack/Prefabs/Tile_A.prefab`, `Tile_B.prefab`, `Wall_A.prefab`, `Wall_B.prefab`, `Wall_C.prefab`.
- Doorways/arches: `DungeonModularPack/Prefabs/Arch_A.prefab`; `Mega Fantasy Props Pack/Prefabs/Castle walls`.
- Pillars/stairs: `DungeonModularPack/Prefabs/Pillar_A.prefab`, `Pillar_B.prefab`, `Step_A.prefab`.
- Gates: `Mega Fantasy Props Pack/Prefabs/Castle walls/stone_half_gate.prefab`, `3DForge` fence gate prefabs.
- Chests/shrine/trap room dressing: `Mega Fantasy Props Pack` props plus 3DForge containers, candles, webs, pillars, and forge chains.
- Boss room candidates: castle walls/towers from `Mega Fantasy Props Pack` and arch/pillar combinations from `DungeonModularPack`.

## Props

- Barrels/crates: `3DForge/Fantasy_Interiors/.../Containers`, `Mega Fantasy Props Pack/Prefabs/Complex prefabs`.
- Chests/books/tableware: `Mega Fantasy Props Pack` and `MedievalTavernPack`.
- Candles/torches/lanterns: `3DForge` lighting props and `DungeonModularPack/Torch_A/B`.
- Tools/carts/forge items: `3DForge/Fantasy_Interiors/.../Forge`.
- Bones/rubble: not confirmed as strong candidates; rescan specific pack folders before importing.

## Weapons

- Enemy/display weapons: `FlexUnit/MedievalWeaponPack/Prefabs`.
- Blacksmith weapon racks/blades: `3DForge/Fantasy_Interiors/.../Forge_Blades`.
- No obvious revolver or rifle replacement found in staging.

## Enemies

- No clear rigged enemy candidates found in this scan.
- Keep the current 24-enemy runtime graybox roster until a future enemy art/rigging pass.

## UI / HUD

- No strong UI/HUD replacement pack found.
- Keep current HUD assets and treat UI art as a separate future source.

## Audio / VFX

- `Caves and Dungeons` has many dungeon ambience WAV files but is very large.
- `3DForge` has flame particle/material assets; use only after material/render validation.
- Do not import audio/VFX payloads until a dedicated audio/VFX gate.
