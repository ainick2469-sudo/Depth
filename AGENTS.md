# FrontierDepths Agent Instructions

## Project Identity
FrontierDepths is a Unity 6 first-person dungeon crawler prototype:
- frontier town hub
- FATE-inspired dungeon descent
- graph-first procedural floors
- arcade combat
- roguelike upgrade stacking
- co-op later, not now

Current milestone:
Document the rules, stabilize the dungeon, and prove the single-player graybox combat loop.

## Golden Rules
- Do not import Fab or art assets during the current milestone.
- Do not add multiplayer.
- Do not migrate to URP.
- Do not migrate to Unity Input System.
- Do not rewrite the dungeon generator architecture.
- Do not rename or replace core services unless explicitly required.
- Do not move unrelated files.
- Do not add third-party packages.
- Do not continue to the next milestone gate if the current gate fails.

## Folder Ownership
Use these folders for new code:

- `Assets/Game/Runtime/Core`
  Bootstrap, save/profile/run state, scene flow, shared serializable state.
- `Assets/Game/Runtime/World`
  Dungeon graph generation, room/corridor rendering, dungeon validation, dungeon debug tools, floor runtime, dungeon interactables.
- `Assets/Game/Runtime/Combat`
  Damage, health, weapons, enemies, status effects, stats, upgrades, combat tags.
- `Assets/Game/Runtime/Player`
  Player movement, camera, player-owned input behavior, player weapon/health if tightly player-specific.
- `Assets/Game/Runtime/Interaction`
  `IInteractable`, interact raycasts, interaction prompts, interactable utilities.
- `Assets/Game/Runtime/Progression`
  Shops, town services, profile upgrades, currency, unlocks, reward definitions if tied to progression.
- `Assets/Game/Runtime/UI`
  HUD, menus, panels, text prompts, crosshair, debug overlays.
- `Assets/Game/Editor`
  Editor-only builders, validators, tools, scene/data generation helpers.
- `Assets/Game/Tests/EditMode`
  Pure logic and validation tests.
- `Assets/Game/Tests/PlayMode`
  Scene flow and runtime play tests.

## Data Rules
- ScriptableObject definition files use the suffix `Definition`.
- Runtime mutable state uses the suffix `State`.
- Logic owners use `Service`, `Controller`, `Director`, or `Resolver`.
- Do not mix ScriptableObject definitions with mutable run state.
- Do not store runtime-only state in assets.

## Naming Rules
Use clear generated object names:
- `Room_<NodeId>_<RoomType>_<TemplateName>`
- `Corridor_<FromNodeId>_To_<ToNodeId>`
- `DoorOpening_<NodeId>_<Direction>`
- `Wall_<NodeId>_<Direction>`
- `Feature_<NodeId>_<FeatureType>`
- `SpawnPoint_<NodeId>_<SpawnType>`
- `Enemy_<NodeId>_<EnemyType>`
- `Interactable_<NodeId>_<Type>`

## Dungeon Rules
- Corridor and doorway correctness beats decorative shaping.
- No corridor may end in an unopened wall.
- Required rooms must be reachable.
- Secret rooms must have a valid graph path and a rendered traversal path.
- Landmark rooms stay flat and aligned during this milestone.
- Advanced room templates stay disabled for ordinary rooms during this milestone.
- If validation fails, retry up to 3 times, then load a guaranteed-safe fallback layout.
- Never silently continue with broken required geometry.

## Combat Rules
- Keep combat graybox and readable.
- Starter revolver first.
- One melee enemy first.
- Add a ranged enemy only after melee works.
- Weapon raycasts hit `Enemy` and `Environment`.
- Interaction raycasts hit `Interactable`.
- Use simple pooling or capped reusable feedback for repeated combat effects.

## UI Rules
- UI displays state; UI does not own game rules.
- Do not put combat, dungeon, save, or reward logic inside UI classes.
- Avoid object discovery in `Update`.
- Prefer stable references or explicit wiring.

## Testing Rules
After each gate:
- run compile checks
- run relevant EditMode tests
- manually verify the required scene flow
- report changed files
- report console errors or warnings
- report known limitations

## Manual Verification Path
`MainMenu -> New Game -> TownHub -> Dungeon Gate -> DungeonRuntime -> kill enemies -> choose reward -> descend stairs -> return to town`

## Git Rules
After each successful gate:
- `git status`
- `git add -A`
- `git commit -m "Gate X: short description"`
- `git push`

If the workspace is not a git repository, report that clearly instead of inventing git state.
