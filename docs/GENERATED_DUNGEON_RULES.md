# Generated Dungeon Rules

## Corridor and Doorway Rules
- Corridor and doorway correctness beats decorative shaping.
- No corridor may end in an unopened wall.
- Room opening and corridor mouth must share one opening definition.
- Required rooms must be reachable.
- Secret rooms must have a valid graph path and a rendered traversal path.
- Landmark rooms stay flat and aligned during this milestone.

## Validation Rules
After generation and rendering, validate the actual `DungeonBuildResult`:
- every graph edge has a rendered corridor
- every rendered corridor connects to valid room openings
- no corridor mouth is blocked by a wall span
- every connected room has a floor, walls, and at least one valid doorway
- entry, stairs, return route, landmark, and secret are reachable
- player spawn is inside the entry room and clear of geometry
- no interactable spawns inside walls or outside the playable floor
- floor-1 return route exists and does not require Town Sigil
- no duplicate required return-to-town interactables exist

## Prototype Floor Bands
During the combat prototype, floors 4-5 are intentionally softened as a transition band:
- floor 1 targets 10-14 rooms
- floors 2-3 target 12-16 rooms
- floors 4-5 target 14-18 rooms with 3 required sectors and 4 preferred
- floors 4-5 prefer loops, entry degree 3, and minor-axis extent 4, but accept compact valid layouts
- floors 6-8 are the first stricter band with 16-22 rooms, 4 required sectors, required loops, and wider extents

This is temporary reliability tuning for combat testing. After the Encounter Director and reward loop work, floors 4+ can become stricter again.

## Prototype Proportions
During Gate 3B.1, first-person combat feel is the scale target:
- room centers are tuned around compact 78-unit spacing
- safe ordinary combat templates stay at or below a 66-unit major footprint
- ordinary combat rooms should allow the player to strafe while tracking one melee enemy without immediately hitting a wall
- hallways should read as connectors, not long combat runways
- corridor metrics track average length, max length, and percent over 36 units; corridors over 40 units should warn

If top-down compactness passes but first-person still feels stretched, prioritize first-person feel.

## Layout Repetition Rules
Normal generation may prefer rerolling exact repeated layout shapes from the previous 3 floors.
Generation reliability always wins:
- anti-repetition must never cause fallback by itself
- if every valid normal candidate repeats shape, accept the best valid normal layout
- fallback remains an emergency path, not a valid normal-generation success

## Spawn Safety Rules
Enemy, chest, shrine, and interactable spawning must use validated spawn points.
Spawn points must:
- be inside playable room bounds
- be away from doorway openings
- be away from corridor mouths
- not overlap stairs, return anchors, player spawn, room features, or other interactables
- have enough clearance
- be reachable through the room/corridor graph

Room template resizing must preserve doorway alignment, reserved doorway zones, spawn-point safety, and reachable floor pockets. Compact spacing must never force room overlap, corridor-room clipping, or corridor/doorway misalignment; valid placement wins and logs a spacing warning.

## Combat Test Population
`enableCombatTestEnemies` is temporary sandbox population for Gate 3B.1, not the final Encounter Director API.
- floor 1 spawns up to 3 test melee enemies
- floors 2-5 spawn up to 4 test melee enemies
- floor 6+ waits for the real Encounter Director
- optional enemies spawn best-effort and never fail dungeon validation
- target dummies are not enemies and must not affect future floor-clear counts

## Enemy Aggro Rules
Prototype melee enemies can wake from:
- direct sight/detection
- being damaged by the player
- hearing a `WeaponFired` event within the current-floor hearing radius
- nearby ally damage group alerts

Damaged enemies should chase immediately. Heard gunfire should alert nearby enemies within 0.2 seconds, without line of sight, but must not wake the whole dungeon unless enemies are actually within range. Dead enemies ignore hearing and group alerts, and enemies unsubscribe from gameplay events when dead, disabled, or destroyed.

## Failure Behavior
If validation fails:
- log seed, floor index, node id, room type, template name, and failure reason
- retry generation up to 3 times
- if retries fail, load a guaranteed-safe fallback layout:
  - entry room
  - one ordinary room
  - one landmark room
  - one simple secret spur
  - stairs down
  - floor-1 return route when needed
- validate the fallback layout too
- label fallback as `REQUESTED FALLBACK` or `EMERGENCY FALLBACK` in logs/status
- include top normal-generation failure reasons and the best failed graph signature
- never silently continue with broken required geometry
