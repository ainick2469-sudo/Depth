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
13. `STAGING_ASSET_AUDIT.md`
14. `ASSET_STAGING_REPORT.md`
15. `STAGING_ASSET_INDEX.json`
16. `ASSET_IMPORT_PLAN.md`
17. `ASSET_RISK_REGISTER.md`
18. `ASSET_CANDIDATE_LIST.md`
19. `ASSET_INTEGRATION_PLAN.md`
20. `ASSET_IMPORT_POLICY.md`
21. `ASSET_TOWN_SELECTION.md`
22. `ASSET_TOWN_COPY_MANIFEST.md`
23. `ENEMY_ROSTER.md`
24. `ENEMY_PACKS.md`
25. `TEST_INDEX.md`

## Lightweight Export

Run from the repo root:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/GenerateChatReviewExport.ps1
```

Output:

- `ProjectSnapshot/AI_CONTEXT_EXPORT.zip`

The export is allowlisted for code, tests, ProjectSnapshot docs, and lightweight project metadata. It intentionally excludes Unity caches, generated zips, imported heavy art/audio/models, build outputs, and third-party payloads.

## Asset Staging Workflow

Asset Store packs are staged outside the main repo at:

- `C:\Users\nickb\FrontierDepths_AssetStaging`

Do not upload or copy the whole staging project. When the full project is too large, send `ProjectSnapshot/AI_CONTEXT_EXPORT.zip` instead. The export includes code/docs/json context and excludes staging folders, third-party payloads, models, textures, audio, demo scenes, generated zips, and Unity caches.

To refresh the staging audit report:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/GenerateAssetStagingReport.ps1 -StagingProjectPath "C:\Users\nickb\FrontierDepths_AssetStaging"
```

## Current Town Asset Rule

Runtime town code should load only game-owned wrappers from:

- `Assets/Game/Resources/TownVisuals`

Raw copied vendor files, when intentionally selected, must stay isolated under:

- `Assets/Game/Art/Imported/Town/VendorSource`

Do not point runtime systems directly at staging paths or vendor prefabs.
