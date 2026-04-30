using System.IO;
using FrontierDepths.Progression;
using UnityEditor;
using UnityEngine;

namespace FrontierDepths.Editor
{
    public static class TownVisualWrapperPrefabBuilder
    {
        private const string TownVisualRoot = "Assets/Game/Resources/TownVisuals";
        private const string MaterialRoot = "Assets/Game/Art/Imported/Town/Materials";
        private const string VendorRoot = "Assets/Game/Art/Imported/Town/VendorSource";

        [MenuItem("Frontier Depths/Town/Rebuild Town Visual Wrappers")]
        public static void RebuildTownVisualWrappers()
        {
            EnsureFolder(TownVisualRoot);
            EnsureFolder(MaterialRoot);

            Material wood = GetOrCreateMaterial("TownWrapper_Wood.mat", new Color(0.47f, 0.27f, 0.13f));
            Material metal = GetOrCreateMaterial("TownWrapper_Metal.mat", new Color(0.45f, 0.45f, 0.42f));
            Material stone = GetOrCreateMaterial("TownWrapper_Stone.mat", new Color(0.38f, 0.36f, 0.32f));
            Material sign = GetOrCreateMaterial("TownWrapper_Sign.mat", new Color(0.74f, 0.58f, 0.34f));
            Material cloth = GetOrCreateMaterial("TownWrapper_Cloth.mat", new Color(0.54f, 0.22f, 0.16f));

            CreateBlacksmithVisual(wood, metal, stone, sign);
            CreateSaloonInnVisual(wood, metal, stone, sign);
            CreateQuartermasterVisual(wood, metal, stone, cloth, sign);
            CreateBountyBoardVisual(wood, sign, metal);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Town visual wrappers rebuilt.");
        }

        private static void CreateBlacksmithVisual(Material wood, Material metal, Material stone, Material sign)
        {
            GameObject root = CreateRoot("BlacksmithVisual");
            try
            {
                CreateBox(root.transform, "WrapperForgeShell", new Vector3(0f, 1.8f, -1.2f), new Vector3(5.4f, 3.1f, 0.35f), stone);
                CreateBox(root.transform, "WrapperForgeRoof", new Vector3(0f, 3.55f, -0.35f), new Vector3(6.1f, 0.3f, 3.6f), wood);
                CreateBox(root.transform, "WrapperBlacksmithSign", new Vector3(0f, 3.05f, 1.45f), new Vector3(3.4f, 0.55f, 0.16f), sign);
                AddVendorPrefab(root.transform, "Anvil", $"{VendorRoot}/3DForge/Fantasy_Interiors/Villages_And_Towns/Prefabs/Forge/fi_vil_forge_anvil.prefab", new Vector3(-1.7f, 0.05f, 1.65f), Vector3.zero, new Vector3(0.9f, 0.9f, 0.9f), metal);
                AddVendorPrefab(root.transform, "ForgeBase", $"{VendorRoot}/3DForge/Fantasy_Interiors/Villages_And_Towns/Prefabs/Forge/fi_vil_forge_forgebase.prefab", new Vector3(1.5f, 0.05f, -0.45f), Vector3.zero, new Vector3(1.15f, 1.15f, 1.15f), stone);
                AddVendorPrefab(root.transform, "Workbench", $"{VendorRoot}/3DForge/Fantasy_Interiors/Villages_And_Towns/Prefabs/Forge/fi_vil_forge_workbensh_large1.prefab", new Vector3(-0.3f, 0.05f, 0.95f), new Vector3(0f, 12f, 0f), new Vector3(0.8f, 0.8f, 0.8f), wood);
                AddVendorPrefab(root.transform, "ToolRack", $"{VendorRoot}/3DForge/Fantasy_Interiors/Villages_And_Towns/Prefabs/Forge/fi_vil_forge_toolsrack1b.prefab", new Vector3(-2.25f, 0.05f, -0.35f), new Vector3(0f, 90f, 0f), new Vector3(0.7f, 0.7f, 0.7f), metal);
                AddVendorPrefab(root.transform, "Hammer", $"{VendorRoot}/3DForge/Fantasy_Interiors/Villages_And_Towns/Prefabs/Forge/Forge_Props/fi_vil_forge_hammer2.prefab", new Vector3(-1.1f, 1.15f, 1.85f), new Vector3(0f, 35f, 0f), new Vector3(0.85f, 0.85f, 0.85f), metal);
                SavePrefab(root, $"{TownVisualRoot}/BlacksmithVisual.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateSaloonInnVisual(Material wood, Material metal, Material stone, Material sign)
        {
            GameObject root = CreateRoot("SaloonInnVisual");
            try
            {
                CreateBox(root.transform, "WrapperSaloonWall", new Vector3(0f, 1.85f, -1.25f), new Vector3(5.8f, 3.2f, 0.35f), wood);
                CreateBox(root.transform, "WrapperSaloonPorch", new Vector3(0f, 0.2f, 1.65f), new Vector3(6.2f, 0.22f, 2.4f), wood);
                CreateBox(root.transform, "WrapperSaloonSign", new Vector3(0f, 3.15f, 1.35f), new Vector3(3.8f, 0.55f, 0.16f), sign);
                AddVendorPrefab(root.transform, "BarCounter", $"{VendorRoot}/MedievalTavernPack/Prefabs/Furniture/Bar_01_mod.prefab", new Vector3(0f, 0.05f, 1.25f), Vector3.zero, new Vector3(1.15f, 1.15f, 1.15f), wood);
                AddVendorPrefab(root.transform, "Table", $"{VendorRoot}/MedievalTavernPack/Prefabs/Furniture/Table_01.prefab", new Vector3(-1.95f, 0.05f, 1.7f), Vector3.zero, Vector3.one, wood);
                AddVendorPrefab(root.transform, "Chair", $"{VendorRoot}/MedievalTavernPack/Prefabs/Furniture/Chair_01.prefab", new Vector3(-2.2f, 0.05f, 2.35f), new Vector3(0f, 180f, 0f), Vector3.one, wood);
                AddVendorPrefab(root.transform, "Barrel", $"{VendorRoot}/MedievalTavernPack/Prefabs/Ornaments/Barrel_01.prefab", new Vector3(2.25f, 0.05f, 1.55f), Vector3.zero, Vector3.one, wood);
                SavePrefab(root, $"{TownVisualRoot}/SaloonInnVisual.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateQuartermasterVisual(Material wood, Material metal, Material stone, Material cloth, Material sign)
        {
            GameObject root = CreateRoot("QuartermasterVisual");
            try
            {
                CreateBox(root.transform, "WrapperShopWall", new Vector3(0f, 1.8f, -1.1f), new Vector3(5.2f, 3f, 0.35f), wood);
                CreateBox(root.transform, "WrapperCanvasAwning", new Vector3(0f, 3.1f, 0.75f), new Vector3(5.7f, 0.25f, 3.2f), cloth);
                CreateBox(root.transform, "WrapperCounter", new Vector3(0f, 0.8f, 1.45f), new Vector3(4.4f, 1f, 0.75f), wood);
                CreateBox(root.transform, "WrapperSupplyCrateL", new Vector3(-1.8f, 0.45f, 2.15f), new Vector3(1.2f, 0.9f, 1.2f), wood);
                CreateBox(root.transform, "WrapperSupplyCrateR", new Vector3(1.75f, 0.35f, 2.05f), new Vector3(1f, 0.7f, 1f), wood);
                CreateBox(root.transform, "WrapperQuartermasterSign", new Vector3(0f, 3.35f, 1.55f), new Vector3(3.6f, 0.55f, 0.16f), sign);
                SavePrefab(root, $"{TownVisualRoot}/QuartermasterVisual.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateBountyBoardVisual(Material wood, Material sign, Material metal)
        {
            GameObject root = CreateRoot("BountyBoardVisual");
            try
            {
                CreateBox(root.transform, "WrapperBoardLeftPost", new Vector3(-1.65f, 1.35f, 0f), new Vector3(0.22f, 2.7f, 0.22f), wood);
                CreateBox(root.transform, "WrapperBoardRightPost", new Vector3(1.65f, 1.35f, 0f), new Vector3(0.22f, 2.7f, 0.22f), wood);
                CreateBox(root.transform, "WrapperWantedBoard", new Vector3(0f, 1.95f, 0.05f), new Vector3(3.55f, 2.1f, 0.2f), wood);
                CreateBox(root.transform, "WrapperNoticeA", new Vector3(-0.75f, 2.08f, 0.21f), new Vector3(0.85f, 1.12f, 0.04f), sign);
                CreateBox(root.transform, "WrapperNoticeB", new Vector3(0.45f, 1.92f, 0.22f), new Vector3(1.05f, 1.35f, 0.04f), sign);
                CreateBox(root.transform, "WrapperLanternHook", new Vector3(1.95f, 2.75f, 0.1f), new Vector3(0.12f, 0.8f, 0.12f), metal);
                SavePrefab(root, $"{TownVisualRoot}/BountyBoardVisual.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateRoot(string name)
        {
            GameObject root = new GameObject(name);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static void AddVendorPrefab(Transform root, string name, string assetPath, Vector3 localPosition, Vector3 eulerAngles, Vector3 localScale, Material overrideMaterial)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Town visual wrapper source missing: {assetPath}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                return;
            }

            instance.name = name;
            instance.transform.SetParent(root, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(eulerAngles);
            instance.transform.localScale = localScale;
            StripColliders(instance);
            ApplyMaterial(instance, overrideMaterial);
        }

        private static void CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;
            Collider collider = box.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void StripColliders(GameObject instance)
        {
            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Object.DestroyImmediate(colliders[i]);
            }
        }

        private static void ApplyMaterial(GameObject instance, Material material)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = material;
            }
        }

        private static void SavePrefab(GameObject root, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path)?.Replace("\\", "/"));
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }

        private static Material GetOrCreateMaterial(string fileName, Color color)
        {
            string path = $"{MaterialRoot}/{fileName}";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folder = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
