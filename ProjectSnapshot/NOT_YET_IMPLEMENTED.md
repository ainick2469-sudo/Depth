# Not Yet Implemented

These systems have been discussed or implied, but they are not real production gameplay yet.

| System | Why It Matters | Likely Gate | Dependencies | Risks |
| --- | --- | --- | --- | --- |
| Full skill trees | Classes need long-term identity and build choices. | VS-1.3.5 | Stable resources, class XP, UI patterns. | Too much UI before combat feel is stable. |
| Class selection | Needed for multiple class fantasies. | Later | At least one complete class loop. | Premature class work can dilute Gunslinger. |
| Class-specific resources beyond Gunslinger | Future classes need different loops. | Later | Skill tree and class selection foundation. | Generic resources make classes feel samey. |
| Real boss system | Milestones need memorable checks. | VS-1.5.0 | Enemy behavior, arenas, rewards. | Bosses before base combat feel will expose weak fundamentals. |
| Milestone bosses | Gives descent structure and long-term goals. | VS-1.5.x | Boss system and floor bands. | Requires reward and persistence decisions. |
| Ascension Route | Core long-term hook: going up is escalation. | VS-2.0 | Descent, bosses, floor scaling, persistence. | Huge design load if attempted too early. |
| Full dungeon generation overhaul | Needed for better room variety and pacing. | VS-1.4.0 | Metadata/planning foundation. | Imported generator packs may not fit architecture. |
| Graybox world floor runtime | Needed to make Floor 1 more than TownHub -> DungeonRuntime. | VS-1.4.1 | World-floor definitions/progression state. | Can balloon into terrain work if not kept graybox. |
| Labyrinth entrance bridge | Needed to connect overworld exploration to the current dungeon runtime. | VS-1.4.2 | Graybox world floor runtime. | Scene flow/map markers can regress if overbuilt. |
| Teleport gate UI | Needed for revisiting unlocked floor settlements. | VS-1.4.4 | Floor unlock state and settlement tracking. | Premature travel UI before world floors exist. |
| Merged rooms | Supports arenas, landmarks, and larger fights. | VS-1.3.3 or VS-1.4.0 | Room metadata and placement rules. | Can break minimap, pacing, and spawn logic. |
| Secret room chains | Adds discovery depth. | Later | Reliable map concealment and room rules. | Easy to reveal accidentally. |
| Room biomes/zones | Gives depth bands identity. | Later | Floor bands, tile/theme rules. | Art/content cost can balloon. |
| Real enemy special attacks | Makes roster play differently. | VS-1.3.4 | Existing 24-enemy metadata. | Overbuilding controllers per enemy would hurt maintainability. |
| Real animations/rigged enemies | Needed for final readability. | Later | Stable body plans and asset pipeline. | Imported rigs can create churn and scope creep. |
| Town interiors | Makes town feel alive. | Later | Town progression and service identity. | Scene work and asset needs. |
| Proper shop UI | Makes economy readable. | Later | Real itemization and town services. | Premature UI before economy design. |
| Real loot/itemization | Core ARPG depth. | Later | Combat, rewards, inventory. | Too many items before gameplay loops are stable. |
| Death recovery/body recovery | Adds run consequence and return goals. | Soon | Save/run state and town return flow. | Needs careful UX to avoid frustration. |
| Stash/storage | Supports long-term loot. | Later | Real items and town services. | Useless until loot exists. |
| Armor/equipment | Adds buildcraft. | Later | Itemization and stat model. | Can bloat balance early. |
| Quest chains | Gives town and dungeon narrative direction. | Later | Bounties and town progression. | Requires content pipeline. |
| Real bounty target presentation | Makes bounties feel like hunts. | VS-1.3.6 | Enemy nameplates, unique variants. | Current markers may be too prototype. |
| Full map pan/zoom polish | Improves navigation. | Later | Stable minimap/full-map state. | Input complexity can regress pause/map behavior. |
| Multiplayer/invite friends | Long-term social goal. | Long-term | Networking architecture, save rules. | Very large scope. |
| Dungeon Generation Pack review/import | External research candidate. | Spike branch only | Asset import workflow and architecture review. | Must not replace current architecture blindly. |
