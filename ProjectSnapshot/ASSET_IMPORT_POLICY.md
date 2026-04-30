# Asset Import Policy

## Rules

- Never dump whole Asset Store packs directly into `Assets/Game`.
- Keep vendor assets isolated under `Assets/ThirdParty/AssetStore/<PackName>` if copied into FrontierDepths later.
- Prefer curated wrapper prefabs under:
  - `Assets/Game/Prefabs/Town`
  - `Assets/Game/Prefabs/Dungeon`
  - `Assets/Game/Prefabs/Props`
  - `Assets/Game/Prefabs/Weapons`
  - `Assets/Game/Prefabs/Enemies`
  - `Assets/Game/Prefabs/UI`
- Do not modify vendor prefabs directly.
- Do not modify vendor materials directly.
- Use prefab variants or wrapper prefabs for scale, rotation, collider, material, layer, and gameplay adjustments.
- Avoid demo scenes unless inspecting them in staging.
- Avoid importing vendor scripts into runtime unless explicitly approved.
- Use Git LFS for large models/textures/audio if they must enter FrontierDepths.
- Commit asset integration separately from gameplay changes.

## Spike Branch Flow

```powershell
git status
git switch -c spike/asset-pack-name
# Copy selected assets from staging, never the whole staging project.
git status --short
git diff --stat
git diff --name-status
```

## Review Questions Before Any Import

- What exact gameplay or readability problem does this asset solve?
- Which wrapper prefab will own scale, rotation, colliders, and material overrides?
- Which dependencies are required, and are any of them huge?
- Does the asset bring scripts, editor tools, custom shaders, or demo-scene assumptions?
- Can the same result be achieved with a smaller prop subset?
