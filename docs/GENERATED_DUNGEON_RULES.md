# Generated Dungeon Rules

## Corridor and Doorway Rules
- Corridor and doorway correctness beats decorative shaping.
- No corridor may end in an unopened wall.
- Room opening and corridor mouth must share one opening definition.
- Required rooms must be reachable.
- Secret rooms must have a valid graph path and a rendered traversal path.
- Landmark rooms stay flat and aligned during this milestone.

## Validation Rules
After generation and rendering, validate:
- every graph edge has a rendered corridor
- every rendered corridor connects to valid room openings
- every connected room has a floor, walls, and at least one valid doorway
- entry, stairs, return route, landmark, and secret are reachable
- player spawn is inside the entry room and clear of geometry
- room features do not overlap doorways, corridor mouths, stairs, spawn, or required interactables
- no interactable spawns inside walls or outside the playable floor

## Spawn Safety Rules
Enemy, chest, shrine, and interactable spawning must use validated spawn points.
Spawn points must:
- be inside playable room bounds
- be away from doorway openings
- be away from corridor mouths
- not overlap stairs, return anchors, player spawn, room features, or other interactables
- have enough clearance
- be reachable through the room/corridor graph

## Failure Behavior
If validation fails:
- log seed, floor index, node id, room type, template name, and failure reason
- retry generation up to 3 times
- if retries fail, load a guaranteed-safe fallback layout:
  - entry room
  - one ordinary room
  - one landmark room
  - stairs down
  - floor-1 return route when needed
- never silently continue with broken required geometry
