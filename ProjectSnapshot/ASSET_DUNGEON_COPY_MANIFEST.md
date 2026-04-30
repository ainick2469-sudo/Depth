# Asset Dungeon Copy Manifest

## Gate

`Gate VS-1.4.1C: Dungeon Modular Shell Adapter`

## Copy Decision

No raw staged dungeon vendor assets were copied into `FrontierDepths` for this gate.

Reason:

- The adapter architecture can be validated with game-owned primitive wrapper prefabs.
- Avoiding raw vendor copies keeps the pass focused on shell plumbing, collision safety, tests, and fallback behavior.
- `DungeonModularPack` assets should be selected in a future curated visual pass after scale, shader, material, pivot, and dependency checks.

## Payload Summary

- Raw copied vendor payload: `0 MB`
- Hard cap for this gate: `75 MB`
- Preferred cap: under `25 MB`
- Source staging project inspected previously: `C:\Users\nickb\FrontierDepths_AssetStaging`
- Runtime wrapper path created: `Assets/Game/Resources/DungeonVisuals`
- Game-owned material path created: `Assets/Game/Art/Imported/Dungeon/Materials`

## Current Game-Owned Wrappers

| Wrapper | Runtime Path | Source | Notes |
| --- | --- | --- | --- |
| FloorVisual | `DungeonVisuals/FloorVisual` | Game-owned primitive | Visual-only; no gameplay collider. |
| WallVisual | `DungeonVisuals/WallVisual` | Game-owned primitive | Visual-only; existing wall primitive collider stays authoritative. |
| DoorwayVisual | `DungeonVisuals/DoorwayVisual` | Game-owned primitive | Visual-only doorway frame marker. |
| CorridorVisual | `DungeonVisuals/CorridorVisual` | Game-owned primitive | Visual-only; existing corridor collision floor remains authoritative. |
| CornerVisual | `DungeonVisuals/CornerVisual` | Game-owned primitive | Prepared slot for future modular corners. |
| PillarVisual | `DungeonVisuals/PillarVisual` | Game-owned primitive | Prepared slot for future modular pillars. |
| StairsUpVisual | `DungeonVisuals/StairsUpVisual` | Game-owned primitive | Visual-only decoration on existing return lift. |
| StairsDownVisual | `DungeonVisuals/StairsDownVisual` | Game-owned primitive | Visual-only decoration on existing stairs down. |
| RoomAccentVisual | `DungeonVisuals/RoomAccentVisual` | Game-owned primitive | Prepared for non-secret room feature accents. |
| SecretAccentVisual | `DungeonVisuals/SecretAccentVisual` | Game-owned primitive | Prepared for secret room feature accents. |

## Future Candidate Copy Set

Only after manual staging review:

- `DungeonModularPack` modular floor tile, wall tile, arch/frame, pillar, and step piece.
- Copy selected dependencies only, including `.meta` files.
- Keep copied raw files under `Assets/Game/Art/Imported/Dungeon/VendorSource/...`.
- Wrap selected visuals under `Assets/Game/Resources/DungeonVisuals/...`.
- Replace broken/pink/vendor-shader materials in wrappers only with game-owned materials.

## Explicitly Not Copied

- `ADoorToGaming` dungeon generator scripts.
- Demo scenes.
- Vendor runtime/editor scripts.
- Shader Graph or render-pipeline-specific materials.
- Full `DungeonModularPack` folder.
- `Caves and Dungeons` large payload.
- Textures/audio/video/model folders not needed for the visual-only adapter.
