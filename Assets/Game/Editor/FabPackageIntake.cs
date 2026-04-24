using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FrontierDepths.Editor
{
    public static class FabPackageIntake
    {
        private sealed class PackageDescriptor
        {
            public string packageFileName;
            public string vendorFolderName;
            public string[] primaryKeywords;
        }

        private static readonly PackageDescriptor[] TownPackages =
        {
            new PackageDescriptor
            {
                packageFileName = "pb_frontier_settlement.unitypackage",
                vendorFolderName = "PB_Frontier_Settlement",
                primaryKeywords = new[] { "settlement", "frontier", "building", "house", "hut", "fence", "gate" }
            },
            new PackageDescriptor
            {
                packageFileName = "pb_thunder_hammer_forge.unitypackage",
                vendorFolderName = "PB_Thunder_Hammer_Forge",
                primaryKeywords = new[] { "forge", "blacksmith", "hammer" }
            },
            new PackageDescriptor
            {
                packageFileName = "campfires.unitypackage",
                vendorFolderName = "Campfires_And_Torches",
                primaryKeywords = new[] { "campfire", "torch", "fire" }
            },
            new PackageDescriptor
            {
                packageFileName = "basictreasurecoins.unitypackage",
                vendorFolderName = "Basic_Treasure_Coins",
                primaryKeywords = new[] { "coin", "treasure", "gold" }
            },
            new PackageDescriptor
            {
                packageFileName = "cowboyelderly.unitypackage",
                vendorFolderName = "Cowboy_Elderly",
                primaryKeywords = new[] { "cowboy", "elder", "old", "npc" }
            },
            new PackageDescriptor
            {
                packageFileName = "oldbarn.unitypackage",
                vendorFolderName = "Old_Barn",
                primaryKeywords = new[] { "barn", "shed", "farm" }
            },
            new PackageDescriptor
            {
                packageFileName = "salooninterior.unitypackage",
                vendorFolderName = "Saloon_Interior",
                primaryKeywords = new[] { "saloon", "bar", "interior", "tavern" }
            }
        };

        [MenuItem("FrontierDepths/Fab/Import Downloaded Packages")]
        public static void ImportDownloadedPackages()
        {
            EnsureFolder("Assets/ThirdParty/Fab");
            foreach (PackageDescriptor descriptor in TownPackages)
            {
                ImportPackage(descriptor);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Fab intake: downloaded Unity packages imported.");
        }

        [MenuItem("FrontierDepths/Fab/Build Sandbox Gallery")]
        public static void BuildSandboxGallery()
        {
            string scenePath = "Assets/Scenes/Sandbox_ArtImport.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject root = GetOrCreateRoot("FabPreviewRoot");
            ClearChildren(root.transform);

            float x = -36f;
            float z = 0f;
            for (int i = 0; i < TownPackages.Length; i++)
            {
                string vendorRoot = $"Assets/ThirdParty/Fab/{TownPackages[i].vendorFolderName}";
                UnityEngine.Object asset = FindBestAsset(vendorRoot, TownPackages[i].primaryKeywords);
                if (asset == null)
                {
                    continue;
                }

                GameObject instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                instance.name = $"{TownPackages[i].vendorFolderName}_Preview";
                instance.transform.SetParent(root.transform, false);
                instance.transform.position = new Vector3(x, 0f, z);

                GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pedestal.name = "Pedestal";
                pedestal.transform.SetParent(root.transform, false);
                pedestal.transform.position = new Vector3(x, -0.5f, z);
                pedestal.transform.localScale = new Vector3(12f, 1f, 12f);

                x += 18f;
                if (x > 36f)
                {
                    x = -36f;
                    z += 18f;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Fab intake: sandbox gallery rebuilt.");
        }

        [MenuItem("FrontierDepths/Fab/Build Town Art Slice")]
        public static void BuildTownArtSlice()
        {
            string scenePath = "Assets/Scenes/TownHub.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject root = GetOrCreateRoot("TownArtSlice");
            ClearChildren(root.transform);

            GameObject forge = CreateTownVariant("BlacksmithForge", "Assets/ThirdParty/Fab/PB_Thunder_Hammer_Forge", new[] { "forge", "blacksmith", "hammer" });
            GameObject barn = CreateTownVariant("OldBarn", "Assets/ThirdParty/Fab/Old_Barn", new[] { "barn", "shed" });
            GameObject saloon = CreateTownVariant("SaloonInterior", "Assets/ThirdParty/Fab/Saloon_Interior", new[] { "saloon", "bar", "interior" });
            GameObject settlement = CreateTownVariant("FrontierSettlement", "Assets/ThirdParty/Fab/PB_Frontier_Settlement", new[] { "settlement", "frontier", "building", "house", "hut" });
            GameObject campfire = CreateTownVariant("TownCampfire", "Assets/ThirdParty/Fab/Campfires_And_Torches", new[] { "campfire", "torch" });
            GameObject cowboy = CreateTownVariant("CowboyElder", "Assets/ThirdParty/Fab/Cowboy_Elderly", new[] { "cowboy", "elder", "old" });
            GameObject coins = CreateTownVariant("TreasureCoins", "Assets/ThirdParty/Fab/Basic_Treasure_Coins", new[] { "coin", "treasure", "gold" });

            PlaceVariant(root.transform, settlement, new Vector3(26f, 0f, -10f), Vector3.one * 1f);
            PlaceVariant(root.transform, saloon, new Vector3(-28f, 0f, -14f), Vector3.one * 1f);
            PlaceVariant(root.transform, forge, new Vector3(34f, 0f, 12f), Vector3.one * 1f);
            PlaceVariant(root.transform, barn, new Vector3(10f, 0f, -10f), Vector3.one * 1f);
            PlaceVariant(root.transform, campfire, new Vector3(0f, 0f, 4f), Vector3.one * 1f);
            PlaceVariant(root.transform, campfire, new Vector3(-10f, 0f, 12f), Vector3.one * 1f);
            PlaceVariant(root.transform, cowboy, new Vector3(-8f, 0f, -6f), Vector3.one * 1f);
            PlaceVariant(root.transform, coins, new Vector3(-30f, 0f, 18f), Vector3.one * 1f);

            if (saloon != null)
            {
                HidePlaceholder("Saloon");
            }

            if (forge != null)
            {
                HidePlaceholder("ForgePad");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Fab intake: first town art slice rebuilt.");
        }

        private static void ImportPackage(PackageDescriptor descriptor)
        {
            string packagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", descriptor.packageFileName);
            if (!File.Exists(packagePath))
            {
                Debug.LogWarning($"Fab intake: package not found in Downloads: {descriptor.packageFileName}");
                return;
            }

            HashSet<string> before = new HashSet<string>(AssetDatabase.GetAllAssetPaths());
            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh();
            HashSet<string> after = new HashSet<string>(AssetDatabase.GetAllAssetPaths());

            List<string> importedRoots = GetImportedRoots(before, after);
            if (importedRoots.Count == 0)
            {
                return;
            }

            string vendorRoot = $"Assets/ThirdParty/Fab/{descriptor.vendorFolderName}";
            EnsureFolder(vendorRoot);

            for (int i = 0; i < importedRoots.Count; i++)
            {
                string source = importedRoots[i];
                if (source.StartsWith(vendorRoot, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string destination = $"{vendorRoot}/{Path.GetFileName(source)}";
                if (AssetDatabase.IsValidFolder(source))
                {
                    destination = MakeUniqueAssetPath(destination);
                }
                else
                {
                    destination = MakeUniqueAssetPath(destination);
                }

                string result = AssetDatabase.MoveAsset(source, destination);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    Debug.LogWarning($"Fab intake: could not move '{source}' to '{destination}': {result}");
                }
            }
        }

        private static List<string> GetImportedRoots(HashSet<string> before, HashSet<string> after)
        {
            HashSet<string> roots = new HashSet<string>();
            foreach (string path in after)
            {
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || before.Contains(path))
                {
                    continue;
                }

                string root = GetTopLevelAssetPath(path);
                if (!string.IsNullOrWhiteSpace(root) &&
                    !root.StartsWith("Assets/Game", StringComparison.OrdinalIgnoreCase) &&
                    !root.StartsWith("Assets/Scenes", StringComparison.OrdinalIgnoreCase) &&
                    !root.StartsWith("Assets/Resources", StringComparison.OrdinalIgnoreCase) &&
                    !root.StartsWith("Assets/Settings", StringComparison.OrdinalIgnoreCase) &&
                    !root.StartsWith("Assets/ThirdParty/Fab", StringComparison.OrdinalIgnoreCase))
                {
                    roots.Add(root);
                }
            }

            return new List<string>(roots);
        }

        private static string GetTopLevelAssetPath(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            if (parts.Length <= 2)
            {
                return assetPath;
            }

            return $"{parts[0]}/{parts[1]}";
        }

        private static GameObject CreateTownVariant(string prefabName, string vendorRoot, string[] keywords)
        {
            EnsureFolder("Assets/Game/Prefabs");
            EnsureFolder("Assets/Game/Prefabs/Town");

            UnityEngine.Object asset = FindBestAsset(vendorRoot, keywords);
            if (asset == null)
            {
                return null;
            }

            string prefabPath = $"Assets/Game/Prefabs/Town/{prefabName}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            if (instance == null)
            {
                return null;
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
            return saved;
        }

        private static UnityEngine.Object FindBestAsset(string rootPath, string[] keywords)
        {
            if (!AssetDatabase.IsValidFolder(rootPath))
            {
                return null;
            }

            List<string> guids = new List<string>();
            guids.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { rootPath }));
            guids.AddRange(AssetDatabase.FindAssets("t:Model", new[] { rootPath }));
            string bestPath = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < guids.Count; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string lower = path.ToLowerInvariant();
                int score = 0;
                for (int keywordIndex = 0; keywordIndex < keywords.Length; keywordIndex++)
                {
                    if (lower.Contains(keywords[keywordIndex].ToLowerInvariant()))
                    {
                        score += 10 - keywordIndex;
                    }
                }

                if (lower.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    score += 2;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPath = path;
                }
            }

            return string.IsNullOrWhiteSpace(bestPath) ? null : AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(bestPath);
        }

        private static void PlaceVariant(Transform root, GameObject prefab, Vector3 position, Vector3 scale)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                return;
            }

            instance.transform.SetParent(root, false);
            instance.transform.position = position;
            instance.transform.localScale = scale;
        }

        private static GameObject GetOrCreateRoot(string name)
        {
            GameObject root = GameObject.Find(name);
            if (root == null)
            {
                root = new GameObject(name);
            }

            return root;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
        }

        private static void HidePlaceholder(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
            {
                target.SetActive(false);
            }
        }

        private static void EnsureFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string MakeUniqueAssetPath(string assetPath)
        {
            return AssetDatabase.GenerateUniqueAssetPath(assetPath);
        }
    }
}
