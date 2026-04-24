# FrontierDepths Project Map

## Scenes
- `Assets/Scenes/Bootstrap.unity`
  Startup scene. Initializes `GameBootstrap` and scene flow.
- `Assets/Scenes/MainMenu.unity`
  Main menu entry.
- `Assets/Scenes/TownHub.unity`
  Playable town hub. Contains dungeon gate, shops, stash, and service stations.
- `Assets/Scenes/DungeonRuntime.unity`
  Runtime dungeon scene. `DungeonSceneController` generates the active floor.
- `Assets/Scenes/Sandbox_ArtImport.unity`
  Empty art import sandbox. Do not use during current milestone.
- `Assets/Scenes/Net_Playtest.unity`
  Reserved for future multiplayer. Do not use during current milestone.

## Core Runtime Files
- `Assets/Game/Runtime/Core/GameBootstrap.cs`
  Global startup and service owner.
- `Assets/Game/Runtime/Core/SceneFlowService.cs`
  Scene transitions.
- `Assets/Game/Runtime/Core/RunService.cs`
  Active run and floor state.
- `Assets/Game/Runtime/Core/SaveService.cs`
  Profile and run persistence.
- `Assets/Game/Runtime/Core/ProfileService.cs`
  Player profile state.

## Dungeon Runtime Files
- `Assets/Game/Runtime/World/GraphFirstDungeonGenerator.cs`
  Logical dungeon graph generation.
- `Assets/Game/Runtime/World/DungeonLayoutGraph.cs`
  Graph, node, and edge data structures.
- `Assets/Game/Runtime/World/DungeonRoomTemplateLibrary.cs`
  Room templates and template selection helpers.
- `Assets/Game/Runtime/World/DungeonSceneController.cs`
  Runtime dungeon scene generation and floor setup.
- `Assets/Game/Runtime/World/DungeonAscendInteractable.cs`
  Stair-up / town-return interaction.
- `Assets/Game/Runtime/World/DungeonStairsInteractable.cs`
  Stair-down interaction.
- `Assets/Game/Runtime/World/DungeonReturnAnchorInteractable.cs`
  Portal-return interaction.

## UI Runtime Files
- `Assets/Game/Runtime/UI/MainMenuController.cs`
  New game / load game entry flow.
- `Assets/Game/Runtime/UI/GameHudController.cs`
  HUD prompt, status, and panel display.

## Current Dangerous Files
Touch carefully:
- `Assets/Game/Runtime/World/DungeonSceneController.cs`
- `Assets/Game/Runtime/World/DungeonRoomTemplateLibrary.cs`
- `Assets/Game/Runtime/World/GraphFirstDungeonGenerator.cs`
- `Assets/Game/Runtime/Core/GameBootstrap.cs`
- `Assets/Game/Runtime/Core/SceneFlowService.cs`
- `Assets/Game/Runtime/Core/RunService.cs`
- `Assets/Game/Runtime/Core/SaveService.cs`

## Current Milestone
See `docs/CURRENT_MILESTONE.md`.
