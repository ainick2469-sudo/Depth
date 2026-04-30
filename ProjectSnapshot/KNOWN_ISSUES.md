# Known Issues

- HUD weapon cylinder alignment may still need manual adjustment.
- Revolver material/readability needs manual visual confirmation.
- Town building layout is still graybox and not final.
- Town labels/signage are deferred until sign assets exist.
- Damage-number readability should be manually confirmed.
- Enemy attack families are still mostly data/fallback, not fully bespoke.
- Dungeon generation is still grid/simple and needs a future foundation pass.
- Full skill trees are not implemented.
- Ascension Route is not implemented.
- Dungeon Generation Pack is a future research/import candidate, not integrated.
- Basic reserve ammo fields and pickup code remain serialized for compatibility, but current Gunslinger gameplay treats basic ammo as infinite and active rewards should not grant or mention ammo.
- Full map input is intentionally simplified to hold `M` open / release `M` close; tap-toggle and hold-to-zoom are deferred.
- Special ammo may return later as a separate, more interesting loot system.
- Slime and Spitter Slime code/data remain for compatibility but are debug-only and must not appear in normal encounter generation or active bounties.
- The new 24-enemy roster uses runtime primitive graybox silhouettes only; imported enemy art/rigging is intentionally deferred.
