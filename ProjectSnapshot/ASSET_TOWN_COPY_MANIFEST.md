# Asset Town Copy Manifest

Gate: `VS-1.4.1B`

Policy: copy only a tiny town identity-prop subset from `C:\Users\nickb\FrontierDepths_AssetStaging`. Runtime systems must load only game-owned wrapper prefabs from `Assets/Game/Resources/TownVisuals/...`, never these vendor files directly.

## Size Budget

- Hard gate cap: 75 MB.
- Preferred cap: 25 MB.
- Planned copied raw payload, excluding `.meta`: approximately 0.32 MB.
- Planned copied raw payload including `.meta`: well below 1 MB.

## Copied Assets

| Source Path | Destination Path | Size | Meta Copied | Detected Dependencies | Reason | Runtime Usage |
| --- | --- | ---: | --- | --- | --- | --- |
| `Assets/3DForge/Fantasy_Interiors/Villages_&_Towns/Prefabs/Forge/fi_vil_forge_anvil.prefab` | `Assets/Game/Art/Imported/Town/VendorSource/3DForge/Fantasy_Interiors/Villages_And_Towns/Prefabs/Forge/fi_vil_forge_anvil.prefab` | 2.1 KB | Yes | `fi_vil_forge_anvil.fbx`, `fi_village_forge.mat` | Blacksmith identity prop. | Wrapper-only: `TownVisuals/BlacksmithVisual`. |
| `Assets/3DForge/Fantasy_Interiors/Villages_&_Towns/Prefabs/Forge/fi_vil_forge_forgebase.prefab` | `Assets/Game/Art/Imported/Town/VendorSource/3DForge/Fantasy_Interiors/Villages_And_Towns/Prefabs/Forge/fi_vil_forge_forgebase.prefab` | 2.3 KB | Yes | `fi_vil_forge_forgebase.fbx` | Blacksmith forge silhouette. | Wrapper-only: `TownVisuals/BlacksmithVisual`. |
| `Assets/3DForge/Fantasy_Interiors/Villages_&_Towns/Prefabs/Forge/fi_vil_forge_workbensh_large1.prefab` | `Assets/Game/Art/Imported/Town/VendorSource/3DForge/Fantasy_Interiors/Villages_And_Towns/Prefabs/Forge/fi_vil_forge_workbensh_large1.prefab` | 2.2 KB | Yes | `fi_vil_forge_workbensh_large1.fbx`, `fi_village_forge.mat` | Blacksmith work area. | Wrapper-only: `TownVisuals/BlacksmithVisual`. |
| `Assets/3DForge/Fantasy_Interiors/Villages_&_Towns/Prefabs/Forge/fi_vil_forge_toolsrack1b.prefab` | `Assets/Game/Art/Imported/Town/VendorSource/3DForge/Fantasy_Interiors/Villages_And_Towns/Prefabs/Forge/fi_vil_forge_toolsrack1b.prefab` | 2.2 KB | Yes | `fi_vil_forge_toolsrack1b.fbx`, `fi_village_forge.mat` | Blacksmith tool silhouette. | Wrapper-only: `TownVisuals/BlacksmithVisual`. |
| `Assets/3DForge/Fantasy_Interiors/Villages_&_Towns/Prefabs/Forge/Forge_Props/fi_vil_forge_hammer2.prefab` | `Assets/Game/Art/Imported/Town/VendorSource/3DForge/Fantasy_Interiors/Villages_And_Towns/Prefabs/Forge/Forge_Props/fi_vil_forge_hammer2.prefab` | 2.1 KB | Yes | `fi_vil_forge_hammer2.fbx`, `fi_village_forge.mat` | Small blacksmith detail prop. | Wrapper-only: `TownVisuals/BlacksmithVisual`. |
| `Assets/3DForge/Fantasy_Interiors/Villages_&_Towns/Meshes/Forge/fi_vil_forge_anvil.fbx` | `Assets/Game/Art/Imported/Town/VendorSource/3DForge/Fantasy_Interiors/Villages_And_Towns/Meshes/Forge/fi_vil_forge_anvil.fbx` | 22.7 KB | Yes | None. | Mesh dependency. | Vendor prefab dependency. |
| `Assets/3DForge/Fantasy_Interiors/Villages_&_Towns/Meshes/Forge/fi_vil_forge_forgebase.fbx` | `Assets/Game/Art/Imported/Town/VendorSource/3DForge/Fantasy_Interiors/Villages_And_Towns/Meshes/Forge/fi_vil_forge_forgebase.fbx` | 17.3 KB | Yes | None. | Mesh dependency. | Vendor prefab dependency. |
| `Assets/3DForge/Fantasy_Interiors/Villages_&_Towns/Meshes/Forge/fi_vil_forge_workbensh_large1.fbx` | `Assets/Game/Art/Imported/Town/VendorSource/3DForge/Fantasy_Interiors/Villages_And_Towns/Meshes/Forge/fi_vil_forge_workbensh_large1.fbx` | 23.8 KB | Yes | None. | Mesh dependency. | Vendor prefab dependency. |
| `Assets/3DForge/Fantasy_Interiors/Villages_&_Towns/Meshes/Forge/fi_vil_forge_toolsrack1b.fbx` | `Assets/Game/Art/Imported/Town/VendorSource/3DForge/Fantasy_Interiors/Villages_And_Towns/Meshes/Forge/fi_vil_forge_toolsrack1b.fbx` | 61.9 KB | Yes | None. | Mesh dependency. | Vendor prefab dependency. |
| `Assets/3DForge/Fantasy_Interiors/Villages_&_Towns/Meshes/Forge/Forge_Props/fi_vil_forge_hammer2.fbx` | `Assets/Game/Art/Imported/Town/VendorSource/3DForge/Fantasy_Interiors/Villages_And_Towns/Meshes/Forge/Forge_Props/fi_vil_forge_hammer2.fbx` | 20.0 KB | Yes | None. | Mesh dependency. | Vendor prefab dependency. |
| `Assets/3DForge/Fantasy_Interiors/Villages_&_Towns/Materials/fi_village_forge.mat` | `Assets/Game/Art/Imported/Town/VendorSource/3DForge/Fantasy_Interiors/Villages_And_Towns/Materials/fi_village_forge.mat` | 2.4 KB | Yes | Heavy textures skipped. | Preserve prefab references only. | Overridden by game-owned wrapper materials. |
| `Assets/MedievalTavernPack/Prefabs/Furniture/Bar_01_mod.prefab` | `Assets/Game/Art/Imported/Town/VendorSource/MedievalTavernPack/Prefabs/Furniture/Bar_01_mod.prefab` | 3.3 KB | Yes | `bar_01_mod.FBX`, `Bar_01_mod.mat` | Saloon identity prop. | Wrapper-only: `TownVisuals/SaloonInnVisual`. |
| `Assets/MedievalTavernPack/Prefabs/Furniture/Table_01.prefab` | `Assets/Game/Art/Imported/Town/VendorSource/MedievalTavernPack/Prefabs/Furniture/Table_01.prefab` | 3.3 KB | Yes | `table_01.fbx`, `Table_01.mat` | Saloon table prop. | Wrapper-only: `TownVisuals/SaloonInnVisual`. |
| `Assets/MedievalTavernPack/Prefabs/Furniture/Chair_01.prefab` | `Assets/Game/Art/Imported/Town/VendorSource/MedievalTavernPack/Prefabs/Furniture/Chair_01.prefab` | 3.3 KB | Yes | `chair_01.FBX`, `Chair_01.mat` | Saloon chair prop. | Wrapper-only: `TownVisuals/SaloonInnVisual`. |
| `Assets/MedievalTavernPack/Prefabs/Ornaments/Barrel_01.prefab` | `Assets/Game/Art/Imported/Town/VendorSource/MedievalTavernPack/Prefabs/Ornaments/Barrel_01.prefab` | 3.2 KB | Yes | `barrel_01.FBX`, `Barrel_01.mat` | Saloon/town barrel prop. | Wrapper-only: `TownVisuals/SaloonInnVisual`. |
| `Assets/MedievalTavernPack/Meshes/Furniture/bar_01_mod.FBX` | `Assets/Game/Art/Imported/Town/VendorSource/MedievalTavernPack/Meshes/Furniture/bar_01_mod.FBX` | 31.8 KB | Yes | None. | Mesh dependency. | Vendor prefab dependency. |
| `Assets/MedievalTavernPack/Meshes/Furniture/table_01.fbx` | `Assets/Game/Art/Imported/Town/VendorSource/MedievalTavernPack/Meshes/Furniture/table_01.fbx` | 30.9 KB | Yes | None. | Mesh dependency. | Vendor prefab dependency. |
| `Assets/MedievalTavernPack/Meshes/Furniture/chair_01.FBX` | `Assets/Game/Art/Imported/Town/VendorSource/MedievalTavernPack/Meshes/Furniture/chair_01.FBX` | 46.3 KB | Yes | None. | Mesh dependency. | Vendor prefab dependency. |
| `Assets/MedievalTavernPack/Meshes/Ornaments/barrel_01.FBX` | `Assets/Game/Art/Imported/Town/VendorSource/MedievalTavernPack/Meshes/Ornaments/barrel_01.FBX` | 36.9 KB | Yes | None. | Mesh dependency. | Vendor prefab dependency. |
| `Assets/MedievalTavernPack/Material/Furniture/Bar_01_mod.mat` | `Assets/Game/Art/Imported/Town/VendorSource/MedievalTavernPack/Material/Furniture/Bar_01_mod.mat` | 2.2 KB | Yes | Heavy textures skipped. | Preserve prefab references only. | Overridden by game-owned wrapper materials. |
| `Assets/MedievalTavernPack/Material/Furniture/Table_01.mat` | `Assets/Game/Art/Imported/Town/VendorSource/MedievalTavernPack/Material/Furniture/Table_01.mat` | 2.2 KB | Yes | Heavy textures skipped. | Preserve prefab references only. | Overridden by game-owned wrapper materials. |
| `Assets/MedievalTavernPack/Material/Furniture/Chair_01.mat` | `Assets/Game/Art/Imported/Town/VendorSource/MedievalTavernPack/Material/Furniture/Chair_01.mat` | 2.2 KB | Yes | Heavy textures skipped. | Preserve prefab references only. | Overridden by game-owned wrapper materials. |
| `Assets/MedievalTavernPack/Material/Ornaments/Barrel_01.mat` | `Assets/Game/Art/Imported/Town/VendorSource/MedievalTavernPack/Material/Ornaments/Barrel_01.mat` | 2.2 KB | Yes | Heavy textures skipped. | Preserve prefab references only. | Overridden by game-owned wrapper materials. |

## Skipped Heavy Dependencies

- `3DForge/.../Textures/fi_village_forge_hd.png`, `fi_village_forge_hd_n.png`, and `fi_village_forge_hd_ao.png` total about 42 MB. Skipped because wrapper materials replace vendor material appearance.
- `MedievalTavernPack` furniture/ornament albedo and normal textures total about 29 MB for the selected bar/table/chair/barrel set. Skipped because the saloon wrapper uses game-owned readable wood/metal materials.
- Full 3DForge Smithy exterior and village exterior texture set skipped because the dependency chain is heavy and outside this identity-prop pass.

## Wrapper Outputs

- `Assets/Game/Resources/TownVisuals/BlacksmithVisual.prefab`
- `Assets/Game/Resources/TownVisuals/SaloonInnVisual.prefab`
- `Assets/Game/Resources/TownVisuals/QuartermasterVisual.prefab`
- `Assets/Game/Resources/TownVisuals/BountyBoardVisual.prefab`

Quartermaster and Bounty Board wrappers are game-owned primitive compositions in this gate.
