# ChatGPT Handoff

Read this file first when reviewing Frontier Depths.

## Latest Gate

Current gate: Gate VS-1.4.0: World-Floor Architecture Foundation.

Latest stable base before this gate: `4a4b99f Gate VS-1.3.1D: add project vision bible and AI handoff export`.

## Current Project State

Frontier Depths is a Unity western-fantasy dungeon ascension RPG prototype. The current playable loop is Main Menu -> TownHub -> DungeonRuntime -> combat/rewards -> return/deeper descent. The architecture is pivoting toward persistent world-floors where each floor eventually has a settlement/camp, outer field, labyrinth entrance, labyrinth, boss room, and gate to the next world floor.

## Most Important Folders

- `Assets/Game/Runtime/Combat`: weapons, damage, pickups, enemy definitions, enemy health, combat feedback.
- `Assets/Game/Runtime/Core`: run/profile state, input, scene flow, settings, events.
- `Assets/Game/Runtime/UI`: HUD, minimap/full map, compass, inventory, pause/settings, weapon HUD.
- `Assets/Game/Runtime/World`: dungeon generation/runtime assembly, encounters, room purposes, drops, stairs.
- `Assets/Game/Runtime/Progression`: town services, shops, reputation, bounty/progression-facing systems.
- `Assets/Game/Tests/EditMode`: regression coverage for current gates.
- `ProjectSnapshot`: AI handoff docs and export tooling notes.
- `Tools`: scripts for lightweight review exports.

## Current Gameplay Loop

The player starts from a frontier settlement, enters the current prototype labyrinth, fights graybox enemies, earns rewards/XP/reputation, interacts with room-purpose events, tracks bounties, and can return through the current scene flow. The long-term loop wraps this with generated world floors, floor bosses, teleport gates, and an Ascension Route.

## Current Known Issues

See `KNOWN_ISSUES.md`. The highest-level truths: dungeon generation is still a baseline, enemy attack families need real gameplay behavior, skill trees are not implemented, the town is graybox, and revolver material readability still needs manual confirmation.

## Recently Changed

Recent gates added HUD/minimap polish, infinite basic ammo policy, screen-space damage feedback, town context labels, weapon HUD alignment, slime retirement, 24 enemy definitions, enemy packs, and export tooling.

## Planned Next

Next planned gate: Gate VS-1.4.1: Graybox World Floor Runtime.

The enemy attack-family behavior pass remains important, but the current roadmap first locks the world-floor wrapper so future dungeon/overworld work has a stable state model.

## What Not To Touch

- Do not overhaul dungeon generation without a dedicated gate.
- Do not build overworld generation before VS-1.4.1.
- Do not add skill trees before the Gunslinger tree gate.
- Do not add new enemy families before current enemies play differently.
- Do not import asset packs directly into main.
- Do not commit scene YAML or ProjectSettings churn casually.
- Do not reintroduce basic ammo scarcity into current Gunslinger gameplay.

## Where To Read More

- Vision: `GAME_VISION.md`
- Implemented systems: `IMPLEMENTED_SYSTEMS.md`
- Not real yet: `NOT_YET_IMPLEMENTED.md`
- Backlog: `DESIGN_BACKLOG.md`
- Roadmap: `ROADMAP.md`
- World-floor architecture: `WORLD_FLOOR_ARCHITECTURE.md`
- Scrapped/deferred boundaries: `SCRAPPED_AND_DEFERRED.md`
- Enemy roster: `ENEMY_ROSTER.md`
- Enemy packs: `ENEMY_PACKS.md`
- Export contents: `EXPORT_CONTENTS.md`

## Export

When the full project is too large to upload, send `ProjectSnapshot/AI_CONTEXT_EXPORT.zip` instead.

Generate it with:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/GenerateChatReviewExport.ps1
```
