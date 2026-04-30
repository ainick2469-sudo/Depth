# Known Issues

- Manual visual validation is still required after HUD/viewmodel/export changes; tests prove layout structure and readable colors, but final pixel fit depends on the ornate HUD art in-game.
- Weapon HUD chamber pips are centralized under `CylinderChamberRoot`; future alignment tweaks should adjust the single revolver chamber layout constants only.
- Basic reserve ammo fields and pickup code remain serialized for compatibility, but current Gunslinger gameplay treats basic ammo as infinite and active rewards should not grant or mention ammo.
- Full map input is intentionally simplified to hold `M` open / release `M` close; tap-toggle and hold-to-zoom are deferred.
- Special ammo may return later as a separate, more interesting loot system.
- Slime and Spitter Slime code/data remain for compatibility but are debug-only and must not appear in normal encounter generation or active bounties.
- Many new attack families are metadata/tuning placeholders in VS-1.3.1; full ranged projectiles, traps, summons, shield blocking, and support actions are deferred.
- The new 24-enemy roster uses runtime primitive graybox silhouettes only; imported enemy art/rigging is intentionally deferred.
- Dungeon generation overhaul is intentionally deferred.
