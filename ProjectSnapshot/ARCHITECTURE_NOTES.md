# Architecture Notes

- `PlayerResourceController` owns current Health-adjacent runtime resources: Stamina, Focus, and future Mana support.
- Gunslinger HUD shows Health, Stamina, Focus, Ammo chambers, and class XP. Mana is preserved for future caster classes but hidden from Gunslinger play.
- `WeaponRuntimeState` keeps serialized reserve fields compatible while current gameplay uses infinite basic reserve ammo; active room rewards, drops, and shops should not grant basic ammo.
- `CombatFeedbackService` is the single shared screen-space damage-number route for revolver, rifle, pistol whip, Chain Spark, and bounty damage feedback.
- Full map input is currently hold-to-open/release-to-close on `ToggleFullMap`; minimap visibility is separate and should return after pause/settings/inventory.
- `TownRuntimeKioskBuilder` is the runtime authority for prototype town service kiosks; scene YAML placement should not be edited for these polish passes.
