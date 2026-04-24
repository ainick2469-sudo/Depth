# Testing

## EditMode Expectations
- geometry validation catches blocked corridors, bad openings, and bad spawns
- known-bad seeds do not reproduce blocked openings or unreachable required rooms
- safe room template subset is enforced
- floor-1 return route never requires Town Sigil
- duplicate required return interactables are rejected
- fallback layout builds and passes validation

## PlayMode Expectations
- `MainMenu -> New Game -> TownHub`
- `TownHub -> DungeonRuntime`
- spawn in entry hub
- secret path is traversable if generated
- floor-1 return goes back to town without Town Sigil
- return to town safely from floor 1

## Manual Verification Path
`MainMenu -> New Game -> TownHub -> Dungeon Gate -> DungeonRuntime -> inspect geometry -> return to town`

## Gate Reporting
At the end of each gate, report:
- files changed
- tests run
- manual play path tested
- console errors/warnings remaining
- known limitations
