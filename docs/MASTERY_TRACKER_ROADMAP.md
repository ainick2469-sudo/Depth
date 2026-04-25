# Mastery Tracker Roadmap

## Purpose
FrontierDepths should eventually support a broad mastery web without turning every tracker into a one-off script. The current implementation starts with 10 lightweight trackers fed by internal gameplay events. It records progress and debug output only; major gameplay bonuses wait until combat, rewards, and exploration have enough real actions to justify them.

## Expansion Plan
- Start with 10 trackers now: weapon fire/hit, physical damage, reloads, dry-fire, stairs, movement, and a small meta tracker.
- Expand to 25 after enemies and rewards exist, adding kill, damage taken, survival, floor-clear, and reward-pick trackers.
- Expand to 50 after exploration hooks exist, adding room discovery, secrets, chests, shrines, portals, risk rooms, and economy trackers.
- Expand to 100 only once the game has enough distinct actions, weapons, statuses, classes, and dungeon events to make the count meaningful instead of decorative.

## Tracker Families
- Weapon Masteries: revolver, rifle, shotgun, bow, staff, blade, heavy weapon, thrown weapon, beam weapon, trap weapon.
- Damage Type Masteries: physical, fire, frost, shock, poison, blood, holy, void, mixed elemental, nonlethal/control.
- Combat Behavior Masteries: marksman hits, close-range kills, long-range hits, weakpoint hits, crit chains, overkill, armor break, interrupt, no-miss streaks, multi-kill.
- Movement Masteries: distance moved, jumps, future dash, air time, dodge timing, sprint routes, fall recovery, no-hit traversal, speed floor clear, reposition after reload.
- Survival Masteries: damage taken, low-health survival, healing, flawless rooms, death recovery, armor use, hazard avoidance, potion timing, shield timing, revive prevention.
- Exploration Masteries: rooms discovered, secrets found, optional branches, landmark rooms, map completion, shortcut use, rare room discovery, backtracking avoidance, floor-depth milestones.
- Loot And Economy Masteries: chests opened, shop purchases, sell/upgrade actions, gold collected, treasure-room clears, shrine trades, bargain buys, high-value finds.
- Portal/Rift/Dungeon Interaction Masteries: stairs used, portals used, town returns, sigils, rift exits, shortcut anchors, floor transitions, dungeon events, return-route mastery.
- Ability/Class Masteries: ability casts, cooldown efficiency, class passives, combo usage, summoned aid, crowd control, status application, finisher abilities, class-specific rituals.
- Hidden/Weird Masteries: dry-fire discipline, pacifist rooms, cursed object use, strange shrine outcomes, secret chains, risky returns, environmental kills, odd build synergies.

## Guardrails
- Do not create 100 tracker scripts.
- Keep tracker definitions data-driven through event rules.
- Aggregate high-frequency events like distance moved.
- Save compact state only: tracker id, XP, level, counts, and claimed milestones.
- Keep external analytics separate; this roadmap is for internal progression memory.
