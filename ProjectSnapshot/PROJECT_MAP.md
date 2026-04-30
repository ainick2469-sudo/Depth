# Project Map

## Combat

- Path: `Assets/Game/Runtime/Combat`
- Contains: weapon runtime state, player weapon controller, pickups, damage feedback, upgrades, enemy definitions, enemy health, and enemy catalog data.
- Key files: `WeaponModelView.cs`, `PlayerWeaponController.cs`, `CombatFeedbackService.cs`, `EnemyCatalog.cs`, `EnemyDefinition.cs`, `SimpleMeleeEnemyController.cs`.
- Do not edit casually: weapon save fields, ammo compatibility fields, and enemy taxonomy defaults.

## UI

- Path: `Assets/Game/Runtime/UI`
- Contains: HUD, weapon panel, compass/location label, minimap/full map, inventory, settings, pause menu, and run info panels.
- Key files: `GameHudController.cs`, `WeaponHudView.cs`, `DungeonMinimapController.cs`, `PauseMenuController.cs`, `HudResourceView.cs`.
- Do not edit casually: pause/map input state, minimap hierarchy, and weapon HUD chamber layout constants.

## World

- Path: `Assets/Game/Runtime/World`
- Contains: world-floor catalog/progression helpers, dungeon runtime assembly, graph generation, room metadata, encounters, enemy pack selection, room purpose rewards, stairs, and pickups.
- Key files: `WorldFloorCatalog.cs`, `WorldFloorProgressionService.cs`, `WorldFloorSceneContext.cs`, `GraphFirstDungeonGenerator.cs`, `DungeonSceneController.cs`, `DungeonEncounterDirector.cs`, `RoomPurposeCatalog.cs`.
- Do not edit casually: world-floor persistence semantics, scene YAML, safe/transit room rules, encounter spacing, and room purpose reward policy.

## Progression

- Path: `Assets/Game/Runtime/Progression`
- Contains: town services, shops, town kiosk builder, town layout, reputation-facing services, and service panels.
- Key files: `TownHubController.cs`, `TownRuntimeKioskBuilder.cs`, `TownServiceLayoutManager.cs`, `TownShopCatalog.cs`, `TownShopService.cs`.
- Do not edit casually: runtime town layout authority or shop ammo policy.

## Core

- Path: `Assets/Game/Runtime/Core`
- Contains: bootstrap, run/profile state, world-floor persistence DTOs, scene flow, settings, input binding, events, reputation, bounty system, and shared labels.
- Key files: `GameBootstrap.cs`, `RunState.cs`, `ProfileState.cs`, `WorldFloorProgressionProfileState.cs`, `ProfileService.cs`, `SceneFlowService.cs`, `BountySystem.cs`, `InputBindingService.cs`.
- Do not edit casually: serialized state fields, backward-compatible profile defaults, and scene flow rules.

## Player

- Path: `Assets/Game/Runtime/Player`
- Contains: first-person movement controller and player-facing movement logic.
- Do not edit casually: pause/input capture behavior without regression tests.

## Tests

- Path: `Assets/Game/Tests/EditMode`
- Contains: Unity EditMode coverage for combat, HUD, resources, map, dungeon metadata, town systems, enemy roster, and export tooling.
- Do not edit casually: gate regression tests unless the gate intentionally changes behavior.

## ProjectSnapshot

- Path: `ProjectSnapshot`
- Contains: AI handoff docs, vision, backlog, roadmap, known issues, current gates, enemy docs, test index, and export contents.
- Do not edit casually: `CHATGPT_HANDOFF.md`, `GAME_VISION.md`, and roadmap boundaries without reflecting current direction.

## Tools

- Path: `Tools`
- Contains: lightweight export scripts.
- Key file: `GenerateChatReviewExport.ps1`.
- Do not edit casually: export allowlist/exclusion rules that prevent cache/art payloads from entering AI uploads.

## Art / Resources

- Paths: `Assets/Game/Art`, `Assets/Game/Resources`, `Assets/Game/Prefabs`, `Assets/Game/Data`
- Contains: project-side art, resource-prefabs, gameplay prefabs, and lightweight data.
- Do not edit casually: imported assets, FBX/material import state, or prefab references without Unity validation.
