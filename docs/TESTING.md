# Testing

## EditMode Expectations
- geometry validation catches blocked corridors, bad openings, and bad spawns
- known-bad seeds do not reproduce blocked openings or unreachable required rooms
- damage reduces health correctly
- death events fire once
- revolver respects fire rate and reload
- safe room template subset is enforced
- reward stacking follows flat-then-percent order and clamps correctly

## PlayMode Expectations
- `MainMenu -> New Game -> TownHub`
- `TownHub -> DungeonRuntime`
- descend, spawn in entry hub, clear enemies, choose reward, descend stairs
- return to town safely from floor 1

## Manual Verification Path
`MainMenu -> New Game -> TownHub -> Dungeon Gate -> DungeonRuntime -> kill enemies -> choose reward -> descend stairs -> return to town`

## Gate Reporting
At the end of each gate, report:
- files changed
- tests run
- manual play path tested
- console errors/warnings remaining
- known limitations
