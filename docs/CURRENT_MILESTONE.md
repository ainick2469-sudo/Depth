# Current Milestone

## Name
Document the rules, stabilize the dungeon, and prove the loop.

## Order
1. Documentation / navigation layer
2. Gate 1: Geometry Stabilization
3. Gate 2: Minimal Responsiveness
4. Gate 3: Combat Vertical Slice
5. Gate 4: Reward Choice
6. Gate 5: Floor 1 Exploration Hooks

## Summary
- This milestone is a gated graybox vertical slice, not a broad feature pass.
- Stay on Built-In render pipeline and classic input.
- Do not spend this milestone on Fab imports, multiplayer, URP/Input System migration, bosses, crafting, pets, or broad content expansion.

## Gate 1: Geometry Stabilization
- Enforce a safe ordinary-room template subset:
  - square chamber
  - broad rectangle
  - long gallery
- Disable unstable ordinary-room templates.
- Add anti-repetition rules so clusters do not collapse into identical rooms.
- Corridor/doorway joins must be correct.
- Secret room paths must render and be traversable.
- Landmark rooms stay flat/aligned for this milestone.
- Descend spawns the player in the entry/start room.
- Only one valid return-to-town route on floor 1.
- Remove duplicate return interactables.
- Remove debug-ish dungeon overlay text from normal play.

Validation:
- validate the rendered build result, not only the logical graph
- validate corridors, openings, reachability, spawn safety, and interactable placement
- retry up to 3 times on failure
- fall back to a guaranteed-safe debug layout if needed

## Gate 2: Minimal Responsiveness
- Remove avoidable object discovery in `Update`
- Remove per-instance material cloning during dungeon build
- Reduce unnecessary rebuilds/allocations
- Add stable mouse sensitivity
- Add pause/escape cursor unlock
- Add cleaner crosshair and prompt readability
- Add minimal landing and/or footstep feedback

## Gate 3: Combat Vertical Slice
- Add player revolver, player health, enemy health, simple enemy controller
- Add safe dungeon encounter spawning
- Add floor-clear message

## Gate 4: Reward Choice
Starter reward pool:
- `+Damage`
- `+Fire Rate`
- `+Max Health`
- `Crit Chance`
- `Kill Heals`
- `Bullets Burn`
- `Bullets Poison`
- `Bullets Shock / Chain Lightning Once`

Synergies to prove:
- Burn + Poison = small explosion cloud
- Shock + Crit = chain burst

## Gate 5: Floor 1 Exploration Hooks
- chest room
- shrine room
- locked door
- secret wall
- optional elite room
- fog-of-war / minimap placeholder

## Acceptance Criteria
- `MainMenu -> New Game -> TownHub -> DungeonRuntime` works without editor-only steps
- a new player can enter a generated floor, fight enemies, clear the floor, choose a reward, descend, and return to town
- no compile errors
- no console errors during normal play
- no duplicate interactables
- no invalid spawns inside geometry
- no corridor visually dead-ends into a wall
- no required interaction is hidden, unreachable, or blocked
- manual verification loop completes in under 3 minutes

## Final Rules
- Implement this milestone in gates, not as one giant pass
- Do not continue to combat if geometry validation is failing
- Do not continue to rewards if combat is not playable
- Do not continue to exploration if rewards are not stable
