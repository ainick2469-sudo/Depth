# Current Gates

## Latest Stable Gameplay Base

- `14a8ca1 Gate VS-1.4.1D: stabilize dungeon shell traversal and doorway visuals`

## Current Gate

- `Gate VS-1.4.1E.1: Fix Shell Floor Alignment And Descent State Regressions`

Purpose:

- Fix raised non-colliding corridor/room floor visuals by validating floor wrappers as thin flush veneers.
- Preserve the VS-1.4.1D/E visual-truth contract: no blocked doorways, no walk-through wall/floor lies, and SafeGraybox fallback on validation failure.
- Persist current player health across dungeon depth transitions.
- Refresh HUD location text so world floor and labyrinth depth are distinct.
- Remove the black chamber-bullet backing while keeping yellow revolver pips.
- Add stamina exhaustion lockout/resume thresholds without changing combat balance.

## Next Planned Gate

- `Gate VS-1.4.1F: Labyrinth Layout Quality Foundation`

Improve room size variety, layout pacing, landmark/main-path metadata, and corridor quality without undoing shell-truth validation.
