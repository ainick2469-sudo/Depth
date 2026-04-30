# Asset Integration Plan

## Pass 1: Town Visual Replacement

Goal: replace tilted graybox service blocks with curated, readable town landmarks while preserving current runtime town service logic.

- Blacksmith uses `3DForge` smithy/forge wrappers.
- Tavern/Inn uses `MedievalTavernPack` bar, tables, chairs, fireplace, and lamp wrappers.
- Quartermaster uses a house/shop wrapper from `3DForge` or `Mega Fantasy Props Pack`.
- Bounty Board uses a simple board/sign wrapper from generic wood/sign props.
- Buildings face the town center/player approach.
- Interaction stations remain at the front.
- Duplicate/legacy visual clutter remains hidden or removed by runtime authority.

## Pass 2: Dungeon Modular Visual Shell

Goal: replace graybox floor/wall/corridor/doorway visuals with curated modular pieces while keeping the current dungeon graph/generator.

- Use `DungeonModularPack` as the first shell kit.
- Wrap wall, floor, arch, pillar, step, torch, and handrail prefabs under `Assets/Game/Prefabs/Dungeon`.
- Do not import or adopt `ADoorToGaming` dungeon generation scripts.
- Do not change room graph generation in this pass.
- Validate collision, scale, door width, room readability, and first-person sightlines.

## Pass 3: Room Identity Dressing

Goal: make room purpose visually legible instead of only changing reward text.

- Green cache rooms get crates, barrels, supply chests, or bundled packs.
- Purple shrine rooms get candles, pillars, altar/shrine substitutes, and distinct light color.
- Red boss/hard rooms get stronger gates, pillars, banners, and arena framing.
- Orange trap/ambush rooms get warning props, floor plates, chains, spikes substitutes, or danger lights.
- Blue map/survey rooms get table/map/book/sign props.
- Gold treasure rooms get chests, coins, crates, and brighter reward anchors.
- Rainbow wildcard rooms get intentionally strange mixed props after core room types are clear.

## Later Passes

- Overworld dressing with `RockFREE`, selected `3DForge` road/fence/gate pieces, and selected `EmaceArt` landmarks.
- Enemy equipment props using `FlexUnit` weapons/shields.
- Audio ambience after a dedicated selection/LFS pass.
