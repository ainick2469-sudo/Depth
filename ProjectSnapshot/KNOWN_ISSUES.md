# Known Issues

- Manual visual validation is still required after HUD/viewmodel/map-input/material changes.
- Basic reserve ammo fields and pickup code remain serialized for compatibility, but current Gunslinger gameplay treats basic ammo as infinite and active rewards should not grant or mention ammo.
- Full map input is intentionally simplified to hold `M` open / release `M` close; tap-toggle and hold-to-zoom are deferred.
- Special ammo may return later as a separate, more interesting loot system.
- Slime and Spitter Slime code/data remain for compatibility but are debug-only and must not appear in normal encounter generation or active bounties.
- Many new attack families are metadata/tuning placeholders in VS-1.3.1; full ranged projectiles, traps, summons, shield blocking, and support actions are deferred.
- The new 24-enemy roster uses runtime primitive graybox silhouettes only; imported enemy art/rigging is intentionally deferred.
- Dungeon generation overhaul is intentionally deferred.
