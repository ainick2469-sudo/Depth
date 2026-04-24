# FrontierDepths

Initial Unity 6 dungeon crawler prototype scaffold for a FATE-inspired first-person co-op roguelike.

## What Exists

- `Assets/Scenes/Bootstrap.unity`
  - Boot scene that initializes `GameBootstrap` and routes into town.
- `Assets/Scenes/TownHub.unity`
  - Graybox frontier hub with:
    - dungeon gate
    - quartermaster
    - blacksmith
    - curio dealer
    - bounty board
    - stash
- `Assets/Scenes/DungeonRuntime.unity`
  - Placeholder dungeon scene driven by a graph-first generator.
- `Assets/Scenes/Sandbox_ArtImport.unity`
  - Empty intake scene for Fab art passes.
- `Assets/Scenes/Net_Playtest.unity`
  - Reserved scene for later networking checks.

## Code Layout

- `Assets/Game/Runtime/Core`
  - bootstrap, save/profile/run state, scene flow
- `Assets/Game/Runtime/Player`
  - first-person movement and camera
- `Assets/Game/Runtime/Interaction`
  - interactable contracts and focus raycast
- `Assets/Game/Runtime/Progression`
  - town services, shop framework, profile-side progression
- `Assets/Game/Runtime/World`
  - floor state, chapter/band/theme data, dungeon graph generation, scene runtime
- `Assets/Game/Runtime/Combat`
  - early combat data contracts and upgrade tags
- `Assets/Game/Runtime/UI`
  - HUD and town service panel
- `Assets/Game/Editor`
  - reproducible scene/data asset builder

## Content Assets

- Runtime definitions live under `Assets/Resources/Definitions/...`
- Vendor imports belong under `Assets/ThirdParty/Fab/...`

## Current Milestone

This project currently covers the first implementation slice:

1. fresh project scaffold
2. bootstrap services
3. graybox town hub
4. keyboard-driven town service UI
5. dungeon run start and placeholder floor descent
6. expensive return-to-town hook via Town Sigils

## Important Note

The Unity 6000.4.1f1 editor in this workspace had package compatibility issues with the current `Input System` and `URP` package versions, so the starter slice uses:

- built-in render path
- classic input for the first-person controller

The architecture is still ready for a later package reintroduction once a compatible stack is chosen.
