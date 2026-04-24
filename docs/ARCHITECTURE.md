# FrontierDepths Architecture

## Current Structure
- `Assets/Game/Runtime/Core`
- `Assets/Game/Runtime/World`
- `Assets/Game/Runtime/Combat`
- `Assets/Game/Runtime/Player`
- `Assets/Game/Runtime/Interaction`
- `Assets/Game/Runtime/Progression`
- `Assets/Game/Runtime/UI`
- `Assets/Game/Runtime/Networking`
- `Assets/Game/Editor`
- `Assets/Game/Tests`
- `Assets/Scenes`
- `Assets/Resources/Definitions`
- `Assets/ThirdParty/Fab`

## Current Assembly Direction
The project is organized around gameplay subsystem boundaries:
- `Core` owns bootstrap, scene flow, saves, profile, and run state.
- `World` owns dungeon generation, room templates, interactables, and floor runtime.
- `Combat` owns combat contracts, damage, stats, weapons, enemies, and upgrades.
- `Player` owns movement and player-specific runtime behavior.
- `Interaction` owns interaction contracts, prompts, and raycasts.
- `Progression` owns town services, profile unlocks, and economy.
- `UI` reads runtime state and renders it. UI should not own gameplay rules.
- `Editor` owns scene/data generation and editor-only tooling.
- `Tests` owns EditMode/PlayMode verification.

Keep dependency direction clean:
- UI may depend on Core/World/Progression/Combat state, but gameplay logic should not depend on UI.
- World may depend on Core state and definitions, but Core should not depend on World.
- Editor code must stay editor-only.

## Current Milestone Rule
Do not perform broad folder reorganization during this milestone.
Do not move unrelated runtime files.
Do not change asmdefs unless absolutely necessary to make the current milestone compile.

## Future Target Split For `World/` (Documentation Only)
The `World` folder will eventually be easier to maintain if it is split like this:

- `World/Generation`
- `World/Templates`
- `World/Rendering`
- `World/Validation`
- `World/Debug`
- `World/Interactables`
- `World/Encounters`

Suggested examples:
- `Generation/GraphFirstDungeonGenerator.cs`
- `Templates/DungeonRoomTemplateLibrary.cs`
- `Rendering/DungeonSceneController.cs`
- `Validation/DungeonValidator.cs`
- `Debug/DungeonDebugController.cs`
- `Interactables/DungeonAscendInteractable.cs`
- `Encounters/DungeonEncounterDirector.cs`

## Future Target Split For `Combat/` (Documentation Only)
- `Combat/Damage`
- `Combat/Health`
- `Combat/Weapons`
- `Combat/Enemies`
- `Combat/Stats`
- `Combat/StatusEffects`
- `Combat/Upgrades`

This is the future target. Do not perform the restructure in this milestone.
