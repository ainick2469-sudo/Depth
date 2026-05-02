# World-Floor Architecture

Gate VS-1.4.0 pivots Frontier Depths from a simple `TownHub -> DungeonRuntime` prototype toward persistent generated floor-worlds.

## Target Shape

```text
MainMenu
  -> WorldFloorRuntime
      -> Settlement / Camp / Safe Zone
      -> Outer Field / Overworld
      -> Labyrinth Entrance
          -> LabyrinthRuntime
              -> Labyrinth rooms
              -> Safe rooms
              -> Treasure / shrine / trap / secret rooms
              -> Boss approach
              -> Boss room
              -> Gate to next World Floor
```

## Current Reality

- `TownHub` is still the playable safe settlement scene.
- `DungeonRuntime` is still the playable dungeon scene.
- In the new architecture, current `DungeonRuntime` represents the future Floor 1 labyrinth runtime.
- The current dungeon generator is preserved; VS-1.4.0 adds data/state wrappers above it.
- VS-1.4.1F adds labyrinth layout-quality metadata that future world-floor gates can consume for boss approach rooms, landmark rooms, side objectives, safe routes, and full-map readability.
- VS-1.4.1G adds the first deterministic labyrinth objective skeleton: objective/key room, boss approach, boss room placeholder, and exit/stairs metadata. Progression locks are prepared but not enforced by default.
- VS-1.4.1H starts making that skeleton visible through safer room template selection and conservative compound connector records, without adding boss combat or overworld runtime.

## World Floor Definition

Each world floor has metadata for:

- floor number and name
- biome theme and danger tier
- major town / minor camp flags
- labyrinth id/name
- boss id/name
- field and labyrinth enemy pools
- special room pool
- world size, town count, landmark count
- road, weather, music, and palette profiles

Only floors 1-5 are seeded now. Do not build all floors manually.

## Progression State

Profile persistence tracks:

- current world floor
- highest unlocked world floor
- unlocked/cleared floor records
- defeated bosses
- known labyrinth entrances
- visited settlements
- unlocked teleport gates
- stable per-floor seeds

Older profiles default to Floor 1 unlocked with Floor 2 locked.

## Unlock Rule

When a floor boss is defeated with the matching boss id, that floor becomes cleared and the next catalog floor unlocks. Boss combat itself is not implemented in this gate.

## Deferred

- No overworld generator yet.
- No terrain, forests, lakes, or field enemy zones yet.
- No boss combat yet.
- No boss-door/key-lock enforcement yet; VS-1.4.1G prepares metadata only.
- No arbitrary merged-room mesh generation yet; VS-1.4.1H uses conservative source-level compound connectors and existing safe templates only.
- No teleport gate UI yet.
- No imported dungeon generation pack.
