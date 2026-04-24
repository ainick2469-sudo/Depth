# Unity Workflow

## During This Milestone
- Work in graybox.
- Stay on Built-In render pipeline.
- Stay on classic input.
- Do not import Fab or art assets.

## Scene Flow
- `Bootstrap` initializes `GameBootstrap` and loads `MainMenu`
- `MainMenu` starts a new or loaded run
- `TownHub` is the playable hub
- `DungeonRuntime` builds the active floor

## Regeneration / Validation
- Use the dungeon debug controls to regenerate with same seed or new seed
- Prefer validation-driven debugging over manual scene poking
- Record known-bad seeds when a geometry bug is found

## Editing Rules
- Use scene/runtime changes only when needed for the current gate
- Avoid unrelated refactors while fixing a gate
- Keep changes scoped to the milestone

## After Each Gate
- compile check
- relevant EditMode tests
- manual verification of the relevant flow
- report changed files, console warnings/errors, and known limitations
