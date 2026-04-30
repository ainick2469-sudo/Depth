# Frontier Depths AI Review Snapshot

Use this folder as the project memory layer when the full Unity project is too large to upload.

## Reading Order

1. `CHATGPT_HANDOFF.md`
2. `GAME_VISION.md`
3. `RECENT_CHANGES.md`
4. `CURRENT_GATES.md`
5. `KNOWN_ISSUES.md`
6. `PROJECT_MAP.md`
7. `IMPLEMENTED_SYSTEMS.md`
8. `NOT_YET_IMPLEMENTED.md`
9. `DESIGN_BACKLOG.md`
10. `ROADMAP.md`
11. `WORLD_FLOOR_ARCHITECTURE.md`
12. `SCRAPPED_AND_DEFERRED.md`
13. `ENEMY_ROSTER.md`
14. `ENEMY_PACKS.md`
15. `TEST_INDEX.md`

## Lightweight Export

Run from the repo root:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/GenerateChatReviewExport.ps1
```

Output:

- `ProjectSnapshot/AI_CONTEXT_EXPORT.zip`

The export is allowlisted for code, tests, ProjectSnapshot docs, and lightweight project metadata. It intentionally excludes Unity caches, generated zips, imported heavy art/audio/models, build outputs, and third-party payloads.
