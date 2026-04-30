# Project Map

- `Assets/Game/Runtime/Combat`: weapon runtime state, player weapon controller, pickups, damage feedback, upgrades, enemy taxonomy and roster catalog.
- `Assets/Game/Runtime/Core`: profile, run state, bootstrap, input binding, scene catalog.
- `Assets/Game/Runtime/UI`: HUD, weapon panel, compass/location label, minimap/full map, inventory, settings, run info.
- `Assets/Game/Runtime/World`: dungeon runtime scene assembly, encounters, enemy pack selection, minimap data, pickups.
- `Assets/Game/Runtime/Progression`: town services, shops, bounties, XP, runtime kiosks.
- `Assets/Game/Tests/EditMode`: Unity EditMode coverage for combat, HUD, resources, map, dungeon metadata, town systems.
- `Tools/GenerateChatReviewExport.ps1`: allowlisted lightweight code/docs export for ChatGPT review without Unity caches or heavy asset payloads.
- `ProjectSnapshot`: compact markdown project context, current gates, known issues, roster docs, and export contents notes.
