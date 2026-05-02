# Dungeon Shell Adapter

## Gate

Current: `Gate VS-1.4.1H: Safe Room Merge Geometry And Irregular Room Shapes`

Foundation: `Gate VS-1.4.1C: Dungeon Modular Shell Adapter`

## Purpose

The dungeon shell adapter is a visual-only layer for the existing generated dungeon. It lets `DungeonSceneController` decorate generated floors, walls, corridors, doorway frames, stairs, pillars, and room accents through game-owned wrapper prefabs while keeping the current graph generation, validation, collision, spawning, minimap, stairs, pickups, and room-purpose behavior authoritative.

This gate does not rewrite `GraphFirstDungeonGenerator`, adopt vendor dungeon generators, import demo scenes, or change dungeon gameplay rules.

## Runtime Types

- `DungeonShellVisualKind`: enumerates legacy floor/wall/corridor slots plus curated safe roles for room floor, corridor floor, room wall, doorway side trim, stair markers, and room-purpose floor markers.
- `DungeonShellVisualDefinition`: describes display name, wrapper resource path, fallback color/scale metadata, visual-only policy, collider stripping policy, and warning label.
- `DungeonShellVisualCatalog`: fixed game-owned definitions for all required shell kinds.
- `DungeonShellVisualResolver`: loads wrappers from `Resources/DungeonVisuals`, strips unsafe colliders from instances, warns once per bad/missing resource, and safely leaves graybox primitives visible when wrappers are unavailable.
- `DungeonShellVisualMode`: selects `Off`, `SafeGraybox`, or validated `AdapterVisuals`.
- `DungeonShellVisualTruthReport`: records active mode, fallback reason, spawned visuals, skipped risky visuals, clearance counts, violations, and failing visual/source ids.
- `DungeonShellVisualTruthReport`: also records floor veneer counts, raised-floor violations, skipped raised floor visuals, floor checks, corridor floor checks, and max floor vertical offset.
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
- Room floors, corridor floors, stair markers, and subtle room-purpose floor markers use thin validated floor veneers instead of full-height floor slabs.
- Source-owned room wall visuals and side-only doorway trim can use wrapper visuals.
- Solid doorway wrappers, corridor wall wrappers, corners, pillars, overhead headers/caps, freestanding props, room accents, and secret reveal visuals stay disabled until a later prop/dressing pass.
- Visual intensity tiers are enforced: Tier 1 floors/material swaps, Tier 2 validated trims/markers, Tier 3 risky props disabled.
- Secret/hidden rooms must remain neutral before discovery and must not get special tints, markers, trim, accents, or minimap spoilers.
- Existing primitive colliders and build records remain authoritative.
- Missing wrappers fall back to current graybox visuals with one controlled warning per missing resource path.
- Wrapper prefabs are primitive/game-owned in this gate; no raw DungeonModularPack files were copied.

## Visual Truth Rules

- Every wall-like visual must link to a trusted source wall primitive or `DungeonWallSpanRecord`.
- Doorway and corridor clearance bounds include player-size padding and must remain visually clear.
- Shell visuals stay non-colliding; gameplay collision is still the existing graybox collision.
- Doorway side trim must sit outside doorway/corridor clearance and may never fill the passable opening.
- Purpose markers are thin floor-level tints only; they must not block pickups, enemies, stairs, interactables, doorways, or corridors.
- Floor-like visuals must validate as flush veneers: thin Y height, X/Z footprint aligned to approved source bounds, top surface within tolerance of the source walkable floor, and no raised non-colliding slabs.
- Raised floor veneers are skipped or trigger SafeGraybox fallback before any graybox renderer is hidden.
- Adapter visuals must report zero violations or fallback to SafeGraybox.
- SafeGraybox must never hide graybox renderers.
- Layout-quality changes must not weaken shell validation: any future merged-room or landmark geometry must preserve doorway clearance, corridor clearance, floor veneer alignment, and source-owned visual truth.
- VS-1.4.1H compound connectors are source-level corridor geometry, not visual-only wall removal. They must still generate matching floor/collision/wall records before shell visuals hide graybox.
- Future boss doors or deeper merged-room geometry must pass the same visual-truth checks before hiding graybox.

## Deferred

- Full modular art import from `DungeonModularPack`.
- Decorative torch/chain/trim placement.
- Room-type-specific prop dressing.
- Arbitrary mesh-based room merging and interior wall surgery beyond the conservative VS-1.4.1H compound connector pass.
- Dungeon generation overhaul.
- Boss combat, boss doors, and enforced labyrinth progression locks.

## Validation Focus

- Dungeon builds must still pass `DungeonValidator`.
- Corridors and rooms remain traversable.
- Stairs and room-purpose interactables still spawn.
- Minimap-compatible build records remain unchanged.
- No duplicate shell root containers accumulate after rebuild.
- No scene YAML, ProjectSettings, demo scenes, vendor scripts, or bulk asset folders are committed.
