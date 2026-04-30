# Asset Risk Register

## High Risks

- Imported scripts could conflict with existing systems, especially `ADoorToGaming/Dungeon Generation`.
- Demo scenes should not be added to Build Settings or used as runtime scenes.
- Full vendor folders can drag in controllers, cameras, sample managers, and architecture assumptions that fight FrontierDepths.
- Huge WAV/texture/model payloads can bloat the repo quickly; use Git LFS or explicit selection if large assets enter the main repo.
- Shader Graph/custom shader dependencies may break if the pack assumes a render pipeline the main project is not using.

## Medium Risks

- Vendor prefabs may have scale, rotation, collider, layer, lighting, and material assumptions that do not match the current first-person game.
- Some packs are visually coherent alone but may clash if mixed without a shared palette and material pass.
- Asset Store editor scripts can slow imports or compile if copied wholesale.
- Imported materials may use texture paths, shader keywords, or render queues that need project-owned material variants.
- Demo-scene lighting/post-processing can make assets look better in staging than they will in FrontierDepths runtime lighting.

## Low Risks

- Focused small props such as forge tools, barrels, crates, tables, shields, and simple rocks are good wrapper candidates.
- `DungeonModularPack` has a small coherent module set, but shader/material validation is still required.
- `FlexUnit` weapons are small and useful as props/enemy equipment if kept out of player weapon logic.

## Mitigations

- Never dump whole Asset Store packs directly into `Assets/Game`.
- Keep vendor originals under `Assets/ThirdParty/AssetStore`.
- Use game-owned wrappers or prefab variants under `Assets/Game/Prefabs`.
- Copy only selected assets and required dependencies.
- Do not modify vendor prefabs/materials directly.
- Validate scale/collider/materials in a staging scene before committing.
- Commit asset payloads separately from gameplay code.
