# Dungeon Shell Adapter

## Gate

`Gate VS-1.4.1C: Dungeon Modular Shell Adapter`

## Purpose

The dungeon shell adapter is a visual-only layer for the existing generated dungeon. It lets `DungeonSceneController` decorate generated floors, walls, corridors, doorway frames, stairs, pillars, and room accents through game-owned wrapper prefabs while keeping the current graph generation, validation, collision, spawning, minimap, stairs, pickups, and room-purpose behavior authoritative.

This gate does not rewrite `GraphFirstDungeonGenerator`, adopt vendor dungeon generators, import demo scenes, or change dungeon gameplay rules.

## Runtime Types

- `DungeonShellVisualKind`: enumerates floor, wall, doorway, corridor, corner, pillar, stairs, room accent, and secret accent visual slots.
- `DungeonShellVisualDefinition`: describes display name, wrapper resource path, fallback color/scale metadata, visual-only policy, collider stripping policy, and warning label.
- `DungeonShellVisualCatalog`: fixed game-owned definitions for all required shell kinds.
- `DungeonShellVisualResolver`: loads wrappers from `Resources/DungeonVisuals`, strips unsafe colliders from instances, warns once per bad/missing resource, and safely leaves graybox primitives visible when wrappers are unavailable.
- `DungeonShellVisualMode`: selects `Off`, `SafeGraybox`, or validated `AdapterVisuals`.
- `DungeonShellVisualTruthReport`: records active mode, fallback reason, spawned visuals, skipped risky visuals, clearance counts, violations, and failing visual/source ids.
- `DungeonVisualWrapperPrefabBuilder`: editor-only helper that creates primitive wrapper prefabs and game-owned fallback materials.

## Wrapper Policy

Runtime dungeon code may only load wrappers from:

- `Assets/Game/Resources/DungeonVisuals`

Raw vendor assets, if selected in a future gate, must stay isolated under:

- `Assets/Game/Art/Imported/Dungeon/VendorSource`

Runtime code must not load arbitrary vendor prefabs directly. Wrapper roots are game-owned and normalized to dungeon gameplay scale.

## Current Implementation

- The normal graybox dungeon builds first and remains visible during shell validation.
- Shell wrappers spawn under a temporary `DungeonShellVisuals` root.
- Only after validation passes may eligible graybox renderers be hidden.
- If validation fails, shell visuals are destroyed and SafeGraybox remains visible.
- Room floors, corridor floors, and source-owned room wall visuals can use wrapper visuals.
- Solid doorway wrappers, corridor wall wrappers, corners, pillars, stairs, room accents, secret accents, headers, caps, and trim are skipped until they can satisfy clearance checks.
- Existing primitive colliders and build records remain authoritative.
- Missing wrappers fall back to current graybox visuals with one controlled warning per missing resource path.
- Wrapper prefabs are primitive/game-owned in this gate; no raw DungeonModularPack files were copied.

## Visual Truth Rules

- Every wall-like visual must link to a trusted source wall primitive or `DungeonWallSpanRecord`.
- Doorway and corridor clearance bounds include player-size padding and must remain visually clear.
- Shell visuals stay non-colliding; gameplay collision is still the existing graybox collision.
- Adapter visuals must report zero violations or fallback to SafeGraybox.
- SafeGraybox must never hide graybox renderers.

## Deferred

- Full modular art import from `DungeonModularPack`.
- Decorative torch/chain/trim placement.
- Room-type-specific prop dressing.
- Dungeon generation overhaul.
- Boss path, boss rooms, and labyrinth progression.

## Validation Focus

- Dungeon builds must still pass `DungeonValidator`.
- Corridors and rooms remain traversable.
- Stairs and room-purpose interactables still spawn.
- Minimap-compatible build records remain unchanged.
- No duplicate shell root containers accumulate after rebuild.
- No scene YAML, ProjectSettings, demo scenes, vendor scripts, or bulk asset folders are committed.
