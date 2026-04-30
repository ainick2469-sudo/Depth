using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Progression
{
    public sealed class TownRuntimeKioskBuilder : MonoBehaviour
    {
        public const string RootName = "RuntimeTownKiosks";
        public const string PathRootName = "RuntimeTownPaths";
        private static readonly Vector3 TownCenter = Vector3.zero;

        private static Material woodMaterial;
        private static Material metalMaterial;
        private static Material stoneMaterial;
        private static Material signMaterial;
        private static Material pathMaterial;

        private void Start()
        {
            EnsureRuntimeKiosks(transform);
        }

        public static Transform EnsureRuntimeKiosks(Transform parent)
        {
            using (LoadTimingLogger.Measure("Town kiosk build"))
            {
                Transform safeParent = parent != null ? parent : FindAnyObjectByType<TownHubController>()?.transform;
                if (safeParent == null)
                {
                    return null;
                }

                Transform root = safeParent.Find(RootName);
                if (root == null)
                {
                    GameObject rootObject = new GameObject(RootName);
                    rootObject.transform.SetParent(safeParent, false);
                    root = rootObject.transform;
                }

                RebuildRuntimeRoot(root);
                foreach (TownServiceVisualDefinition definition in TownServiceVisualCatalog.All)
                {
                    CreateKiosk(root, definition);
                }

                CreatePathDressing(root);
                EnsureRuntimeKioskLabels(root);
                return root;
            }
        }

        public static int EnsureRuntimeKioskLabels(Transform root)
        {
            if (root == null)
            {
                return 0;
            }

            int labelCount = 0;
            foreach (TownServiceVisualDefinition definition in TownServiceVisualCatalog.All)
            {
                Transform kiosk = FindDirectChild(root, $"Kiosk_{definition.displayName}");
                if (kiosk == null)
                {
                    continue;
                }

                EnsureLabel(kiosk, "SignLabel", definition.displayName, definition.labelLocalOffset, 42f);
                labelCount++;
            }

            return labelCount;
        }

        private static void RebuildRuntimeRoot(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (Application.isPlaying)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void CreateKiosk(Transform root, TownServiceVisualDefinition definition)
        {
            GameObject kiosk = new GameObject($"Kiosk_{definition.displayName}");
            kiosk.transform.SetParent(root, false);
            kiosk.transform.localPosition = definition.layoutPosition;
            kiosk.transform.localRotation = Quaternion.Euler(0f, GetYawWithFrontFacingCenterForTests(definition.layoutPosition) + definition.yawOffset, 0f);

            if (!CreateAssetVisual(kiosk.transform, definition))
            {
                CreateFallbackShell(kiosk.transform, definition);
            }

            EnsureLabel(kiosk.transform, "SignLabel", definition.displayName, definition.labelLocalOffset, 42f);

            if (!string.IsNullOrWhiteSpace(definition.serviceId))
            {
                GameObject station = new GameObject("ServiceStation", typeof(BoxCollider), typeof(TownServiceStation));
                station.transform.SetParent(kiosk.transform, false);
                station.transform.localPosition = definition.interactionOffset;
                station.transform.localScale = new Vector3(Mathf.Max(3f, definition.footprintSize.x * 0.62f), 2.4f, 1.55f);
                BoxCollider collider = station.GetComponent<BoxCollider>();
                collider.isTrigger = true;
                station.GetComponent<TownServiceStation>().Configure(definition.serviceId, definition.prompt, definition.displayName);
            }
        }

        private static void CreateFallbackShell(Transform parent, TownServiceVisualDefinition definition)
        {
            float width = Mathf.Max(3f, definition.footprintSize.x);
            float depth = Mathf.Max(3f, definition.footprintSize.y);
            Color baseColor = definition.fallbackColor;

            CreateBox(parent, "Fallback_BackWall", new Vector3(0f, 2f, -depth * 0.32f), new Vector3(width, 3.3f, 0.35f), baseColor, GetStoneMaterial());
            CreateBox(parent, "Fallback_LeftPost", new Vector3(-width * 0.48f, 1.6f, 0.05f), new Vector3(0.35f, 3.2f, depth * 0.78f), baseColor, GetWoodMaterial());
            CreateBox(parent, "Fallback_RightPost", new Vector3(width * 0.48f, 1.6f, 0.05f), new Vector3(0.35f, 3.2f, depth * 0.78f), baseColor, GetWoodMaterial());
            CreateBox(parent, "Fallback_Counter", new Vector3(0f, 0.75f, depth * 0.34f), new Vector3(width * 0.72f, 1f, 0.7f), Color.Lerp(baseColor, Color.white, 0.18f), GetWoodMaterial());

            if (definition.serviceId == "shop.bounty_board")
            {
                CreateBox(parent, "Fallback_BountyBoard", new Vector3(0f, 2f, depth * 0.08f), new Vector3(width * 0.75f, 2.2f, 0.18f), new Color(0.44f, 0.25f, 0.11f), GetSignMaterial());
                CreateBox(parent, "Fallback_NoticePaper", new Vector3(0f, 2.05f, depth * 0.2f), new Vector3(width * 0.52f, 1.35f, 0.04f), new Color(0.84f, 0.72f, 0.48f), GetSignMaterial());
            }
        }

        private static bool CreateAssetVisual(Transform parent, TownServiceVisualDefinition definition)
        {
            GameObject prefab = TownServiceVisualResolver.LoadPreferredVisual(definition, out bool usedAssetVisual);
            if (!usedAssetVisual || prefab == null)
            {
                return false;
            }

            GameObject visual = Instantiate(prefab, parent, false);
            visual.name = "AssetVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * Mathf.Max(0.01f, definition.scaleMultiplier);
            StripVisualColliders(visual.transform);
            ApplyWrapperMaterials(visual.transform, definition);
            return true;
        }

        private static void CreatePathDressing(Transform root)
        {
            GameObject pathRoot = new GameObject(PathRootName);
            pathRoot.transform.SetParent(root, false);
            CreateBox(pathRoot.transform, "MainRoad_ToDungeonGate", new Vector3(0f, 0.03f, 18f), new Vector3(5.2f, 0.06f, 42f), Color.white, GetPathMaterial());
            CreateBox(pathRoot.transform, "LeftServicePath", new Vector3(-8.5f, 0.035f, 4f), new Vector3(15f, 0.05f, 3.2f), Color.white, GetPathMaterial());
            CreateBox(pathRoot.transform, "RightServicePath", new Vector3(8.5f, 0.035f, 4f), new Vector3(15f, 0.05f, 3.2f), Color.white, GetPathMaterial());
            CreateBox(pathRoot.transform, "RearServicePath", new Vector3(-9f, 0.035f, -6.5f), new Vector3(18f, 0.05f, 3f), Color.white, GetPathMaterial());
        }

        private static void CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;

            Collider collider = box.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material != null ? material : CreateRuntimeMaterial("TownRuntimeFallback", color);
                if (material == null)
                {
                    renderer.sharedMaterial.color = color;
                }
            }
        }

        private static void EnsureLabel(Transform parent, string name, string text, Vector3 localPosition, float distance)
        {
            if (parent == null || parent.Find(name) != null)
            {
                return;
            }

            WorldLabelBillboard label = WorldLabelBillboard.Create(parent, name, text, localPosition, UiTheme.Text, distance, true);
            TextMesh textMesh = label.GetComponent<TextMesh>();
            textMesh.characterSize = 0.3f;
            textMesh.fontSize = 46;
        }

        private static void StripVisualColliders(Transform root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (Application.isPlaying)
                {
                    Destroy(colliders[i]);
                }
                else
                {
                    DestroyImmediate(colliders[i]);
                }
            }
        }

        private static void ApplyWrapperMaterials(Transform root, TownServiceVisualDefinition definition)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                string lowerName = renderer.name.ToLowerInvariant();
                Material material =
                    lowerName.Contains("anvil") || lowerName.Contains("hammer") || lowerName.Contains("tool") ? GetMetalMaterial() :
                    lowerName.Contains("stone") || lowerName.Contains("forge") ? GetStoneMaterial() :
                    lowerName.Contains("board") || lowerName.Contains("sign") || lowerName.Contains("paper") ? GetSignMaterial() :
                    GetWoodMaterial();

                renderer.sharedMaterial = material;
            }
        }

        private static Material GetWoodMaterial()
        {
            return woodMaterial ??= CreateRuntimeMaterial("TownRuntime_Wood", new Color(0.48f, 0.28f, 0.13f));
        }

        private static Material GetMetalMaterial()
        {
            return metalMaterial ??= CreateRuntimeMaterial("TownRuntime_Metal", new Color(0.42f, 0.43f, 0.41f));
        }

        private static Material GetStoneMaterial()
        {
            return stoneMaterial ??= CreateRuntimeMaterial("TownRuntime_Stone", new Color(0.39f, 0.36f, 0.31f));
        }

        private static Material GetSignMaterial()
        {
            return signMaterial ??= CreateRuntimeMaterial("TownRuntime_Sign", new Color(0.72f, 0.55f, 0.31f));
        }

        private static Material GetPathMaterial()
        {
            return pathMaterial ??= CreateRuntimeMaterial("TownRuntime_Path", new Color(0.34f, 0.28f, 0.2f));
        }

        private static Material CreateRuntimeMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            Material material = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            material.name = name;
            material.color = color;
            return material;
        }

        internal static float GetYawWithFrontFacingCenterForTests(Vector3 kioskPosition)
        {
            Vector3 toCenter = TownCenter - kioskPosition;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude <= 0.0001f)
            {
                toCenter = Vector3.forward;
            }

            return Quaternion.LookRotation(toCenter.normalized, Vector3.up).eulerAngles.y;
        }

        internal static bool DoesKioskFrontFaceTownCenterForTests(Transform kiosk)
        {
            if (kiosk == null)
            {
                return false;
            }

            Vector3 toCenter = TownCenter - kiosk.localPosition;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            Vector3 front = kiosk.forward;
            front.y = 0f;
            return Vector3.Dot(front.normalized, toCenter.normalized) > 0.85f;
        }

        internal static bool IsInteractionPointInFrontForTests(Transform kiosk)
        {
            TownServiceStation station = kiosk != null ? kiosk.GetComponentInChildren<TownServiceStation>(true) : null;
            if (station == null)
            {
                return false;
            }

            Vector3 local = kiosk.InverseTransformPoint(station.transform.position);
            return local.z > 0f;
        }

        internal static bool DoServiceFootprintsOverlapForTests(TownServiceVisualDefinition a, TownServiceVisualDefinition b)
        {
            float minX = (a.footprintSize.x + b.footprintSize.x) * 0.5f;
            float minZ = (a.footprintSize.y + b.footprintSize.y) * 0.5f;
            Vector3 delta = a.layoutPosition - b.layoutPosition;
            return Mathf.Abs(delta.x) < minX && Mathf.Abs(delta.z) < minZ;
        }

        private static Transform FindDirectChild(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
