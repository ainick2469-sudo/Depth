# ChatGPT Handoff

Read this file first when reviewing Frontier Depths.

## Latest Gate

Current gate: Gate VS-1.3.1D: Project Vision Bible, AI Handoff Export, and Roadmap Lock.

Latest gameplay base before this documentation gate: `ad5bcaa Gate VS-1.3.1C: hotfix pause freeze HUD nudge and revolver materials`.

## Current Project State

Frontier Depths is a Unity western-fantasy dungeon ascension RPG prototype. The current playable loop is Main Menu -> TownHub -> DungeonRuntime -> combat/rewards -> return/deeper descent.

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

The player starts from a frontier town, enters the dungeon, fights graybox enemies, earns rewards/XP/reputation, interacts with room-purpose events, tracks bounties, and can return through the current scene flow. The long-term loop will add milestone bosses and an Ascension Route.

## Current Known Issues

See `KNOWN_ISSUES.md`. The highest-level truths: dungeon generation is still a baseline, enemy attack families need real gameplay behavior, skill trees are not implemented, the town is graybox, and revolver material readability still needs manual confirmation.

## Recently Changed

Recent gates added HUD/minimap polish, infinite basic ammo policy, screen-space damage feedback, town context labels, weapon HUD alignment, slime retirement, 24 enemy definitions, enemy packs, and export tooling.

## Planned Next

Next planned gate: Gate VS-1.3.2: Dungeon Generation Metadata and Room Planning Foundation.

The enemy attack-family behavior pass remains important, but the current roadmap puts dungeon metadata/planning next so room generation work has a clear foundation.

## What Not To Touch

- Do not overhaul dungeon generation without a dedicated gate.
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
- Enemy roster: `ENEMY_ROSTER.md`
- Enemy packs: `ENEMY_PACKS.md`
- Export contents: `EXPORT_CONTENTS.md`

## Export

When the full project is too large to upload, send `ProjectSnapshot/AI_CONTEXT_EXPORT.zip` instead.

Generate it with:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/GenerateChatReviewExport.ps1
```
