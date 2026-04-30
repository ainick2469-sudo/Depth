using System.IO;
using UnityEditor;
using UnityEngine;

namespace FrontierDepths.Editor
{
    public static class DungeonVisualWrapperPrefabBuilder
    {
        private const string DungeonVisualRoot = "Assets/Game/Resources/DungeonVisuals";
        private const string MaterialRoot = "Assets/Game/Art/Imported/Dungeon/Materials";

        [MenuItem("Frontier Depths/Dungeon/Rebuild Dungeon Visual Wrappers")]
        public static void RebuildDungeonVisualWrappers()
        {
            EnsureFolder(DungeonVisualRoot);
            EnsureFolder(MaterialRoot);

            Material floor = GetOrCreateMaterial("DungeonWrapper_Floor.mat", new Color(0.36f, 0.34f, 0.3f));
            Material corridorFloor = GetOrCreateMaterial("DungeonWrapper_CorridorFloor.mat", new Color(0.26f, 0.25f, 0.23f));
            Material wall = GetOrCreateMaterial("DungeonWrapper_Wall.mat", new Color(0.15f, 0.16f, 0.18f));
            Material trim = GetOrCreateMaterial("DungeonWrapper_Trim.mat", new Color(0.36f, 0.32f, 0.26f));
            Material accent = GetOrCreateMaterial("DungeonWrapper_Accent.mat", new Color(0.52f, 0.4f, 0.22f));
            Material secret = GetOrCreateMaterial("DungeonWrapper_Secret.mat", new Color(0.43f, 0.31f, 0.58f));

            CreateFloorVisual(floor, trim);
            CreateWallVisual(wall, trim);
            CreateDoorwayVisual(wall, trim, accent);
            CreateCorridorVisual(corridorFloor, trim);
            CreateCornerVisual(wall, trim);
            CreatePillarVisual(wall, trim);
            CreateStairsVisual("StairsUpVisual", new Color(0.32f, 0.52f, 0.62f), trim);
            CreateStairsVisual("StairsDownVisual", new Color(0.8f, 0.66f, 0.22f), trim);
            CreateRoomAccentVisual(accent, trim);
            CreateSecretAccentVisual(secret, trim);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dungeon visual wrappers rebuilt.");
        }

        private static void CreateFloorVisual(Material floor, Material trim)
        {
            GameObject root = CreateRoot("FloorVisual");
            try
            {
                CreateBox(root.transform, "StoneSlab", Vector3.zero, new Vector3(1f, 0.08f, 1f), floor);
                CreateBox(root.transform, "NorthGroove", new Vector3(0f, 0.055f, 0.48f), new Vector3(1f, 0.025f, 0.035f), trim);
                CreateBox(root.transform, "WestGroove", new Vector3(-0.48f, 0.055f, 0f), new Vector3(0.035f, 0.025f, 1f), trim);
                SavePrefab(root, $"{DungeonVisualRoot}/FloorVisual.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateWallVisual(Material wall, Material trim)
        {
            GameObject root = CreateRoot("WallVisual");
            try
            {
                CreateBox(root.transform, "StoneWall", Vector3.zero, Vector3.one, wall);
                CreateBox(root.transform, "TopTrim", new Vector3(0f, 0.46f, -0.02f), new Vector3(1.05f, 0.08f, 1.08f), trim);
                CreateBox(root.transform, "BaseTrim", new Vector3(0f, -0.46f, -0.02f), new Vector3(1.05f, 0.08f, 1.08f), trim);
                SavePrefab(root, $"{DungeonVisualRoot}/WallVisual.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateDoorwayVisual(Material wall, Material trim, Material accent)
        {
            GameObject root = CreateRoot("DoorwayVisual");
            try
            {
                CreateBox(root.transform, "LeftJamb", new Vector3(-0.42f, 0f, 0f), new Vector3(0.16f, 1f, 1f), wall);
                CreateBox(root.transform, "RightJamb", new Vector3(0.42f, 0f, 0f), new Vector3(0.16f, 1f, 1f), wall);
                CreateBox(root.transform, "Lintel", new Vector3(0f, 0.43f, 0f), new Vector3(1f, 0.14f, 1f), trim);
                CreateBox(root.transform, "Marker", new Vector3(0f, 0.08f, -0.52f), new Vector3(0.24f, 0.24f, 0.04f), accent);
                SavePrefab(root, $"{DungeonVisualRoot}/DoorwayVisual.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateCorridorVisual(Material floor, Material trim)
        {
            GameObject root = CreateRoot("CorridorVisual");
            try
            {
                CreateBox(root.transform, "CorridorSlab", Vector3.zero, new Vector3(1f, 0.08f, 1f), floor);
                CreateBox(root.transform, "CenterDustLine", new Vector3(0f, 0.055f, 0f), new Vector3(0.1f, 0.025f, 1f), trim);
                SavePrefab(root, $"{DungeonVisualRoot}/CorridorVisual.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateCornerVisual(Material wall, Material trim)
        {
            GameObject root = CreateRoot("CornerVisual");
            try
            {
                CreateBox(root.transform, "CornerPost", Vector3.zero, Vector3.one, wall);
                CreateBox(root.transform, "CornerCap", new Vector3(0f, 0.48f, 0f), new Vector3(1.12f, 0.08f, 1.12f), trim);
                SavePrefab(root, $"{DungeonVisualRoot}/CornerVisual.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreatePillarVisual(Material wall, Material trim)
        {
            GameObject root = CreateRoot("PillarVisual");
            try
            {
                CreateBox(root.transform, "PillarCore", Vector3.zero, new Vector3(0.62f, 1f, 0.62f), wall);
                CreateBox(root.transform, "PillarBase", new Vector3(0f, -0.44f, 0f), new Vector3(1f, 0.12f, 1f), trim);
                CreateBox(root.transform, "PillarCap", new Vector3(0f, 0.44f, 0f), new Vector3(1f, 0.12f, 1f), trim);
                SavePrefab(root, $"{DungeonVisualRoot}/PillarVisual.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateStairsVisual(string prefabName, Color stairColor, Material trim)
        {
            GameObject root = CreateRoot(prefabName);
            try
            {
                Material material = GetOrCreateMaterial($"DungeonWrapper_{prefabName}.mat", stairColor);
                CreateBox(root.transform, "StepLow", new Vector3(0f, -0.28f, 0.22f), new Vector3(1f, 0.18f, 0.25f), material);
                CreateBox(root.transform, "StepMid", new Vector3(0f, -0.08f, 0f), new Vector3(1f, 0.18f, 0.25f), material);
                CreateBox(root.transform, "StepHigh", new Vector3(0f, 0.12f, -0.22f), new Vector3(1f, 0.18f, 0.25f), material);
                CreateBox(root.transform, "StepTrim", new Vector3(0f, 0.27f, -0.4f), new Vector3(1f, 0.08f, 0.1f), trim);
                SavePrefab(root, $"{DungeonVisualRoot}/{prefabName}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateRoomAccentVisual(Material accent, Material trim)
        {
            GameObject root = CreateRoot("RoomAccentVisual");
            try
            {
                CreateBox(root.transform, "AccentBase", new Vector3(0f, -0.35f, 0f), new Vector3(1f, 0.18f, 1f), trim);
                CreateBox(root.transform, "AccentMarker", new Vector3(0f, 0.05f, 0f), new Vector3(0.48f, 0.7f, 0.48f), accent);
                SavePrefab(root, $"{DungeonVisualRoot}/RoomAccentVisual.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateSecretAccentVisual(Material secret, Material trim)
        {
            GameObject root = CreateRoot("SecretAccentVisual");
            try
            {
                CreateBox(root.transform, "SecretBase", new Vector3(0f, -0.35f, 0f), new Vector3(1f, 0.18f, 1f), trim);
                CreateBox(root.transform, "SecretMarker", new Vector3(0f, 0.05f, 0f), new Vector3(0.42f, 0.75f, 0.42f), secret);
                SavePrefab(root, $"{DungeonVisualRoot}/SecretAccentVisual.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateRoot(string name)
        {
            GameObject root = new GameObject(name);
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root;
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

        private static Material GetOrCreateMaterial(string fileName, Color color)
        {
            EnsureFolder(MaterialRoot);
            string path = $"{MaterialRoot}/{fileName}";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader)
                {
                    color = color
                };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.color = color;
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                EnsureFolder(parent);
                if (!AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.CreateFolder(parent, folder);
                }
            }
        }
    }
}
