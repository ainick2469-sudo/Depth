# Frontier Depths Chat Review Snapshot

Latest base before this pass: `0167726 Gate VS-1.3.1B: fix HUD chamber alignment weapon readability and town label`

Frontier Depths is a Unity dungeon-crawler prototype centered on a Gunslinger loop: town hub -> dungeon floor -> combat/rewards -> return/deeper dive.

Current design decisions:
- Gunslinger uses Health, Stamina, Focus, and loaded weapon chambers.
- Mana remains future support for caster classes.
- Basic reserve ammo is inactive; reload/chamber timing remains.
- Slime and Spitter Slime are debug-only compatibility content.
- Dungeon generation overhaul, death recovery, and enemy attack-family behavior are deferred gates.

Lightweight export:
- Run `Tools/GenerateChatReviewExport.ps1` from the repo root.
- It writes `ProjectSnapshot/CHAT_REVIEW_EXPORT.zip`.
- The export is allowlisted for code/docs/lightweight project metadata and intentionally excludes Unity caches, generated zips, imported heavy art/audio/models, and build outputs.
