# Architecture Notes

- `PlayerResourceController` owns current Health-adjacent runtime resources: Stamina, Focus, and future Mana support.
- Gunslinger HUD shows Health, Stamina, Focus, Ammo chambers, and class XP. Mana is preserved for future caster classes but hidden from Gunslinger play.
- `WeaponRuntimeState` keeps serialized reserve fields compatible while current gameplay uses infinite basic reserve ammo.
- `CombatFeedbackService` is the shared damage-number route for revolver, rifle, pistol whip, Chain Spark, and bounty damage feedback.
- `TownRuntimeKioskBuilder` is the runtime authority for prototype town service kiosks; scene YAML placement should not be edited for these polish passes.

