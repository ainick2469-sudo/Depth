using UnityEngine;

namespace FrontierDepths.Combat
{
    public sealed class WeaponModelView : MonoBehaviour
    {
        private const string RevolverResourcePath = "Weapons/FrontierRevolver_Model";
        private const string ImportedModelName = "ImportedWeaponModel";

        [SerializeField] private Transform weaponBlockoutRoot;

        private GameObject importedInstance;
        private string activeWeaponId = string.Empty;
        private bool attemptedLoad;
        private bool modelLoaded;
        private bool fallbackMaterialsApplied;
        private bool poseApplied;

        internal bool ModelLoadedForTests => modelLoaded;
        internal bool IsGrayboxFallbackActiveForTests => !modelLoaded;
        internal int InstanceCountForTests => weaponBlockoutRoot != null ? CountNamedChildren(weaponBlockoutRoot, ImportedModelName) : 0;
        internal bool FallbackMaterialsAppliedForTests => fallbackMaterialsApplied;
        internal bool PoseAppliedForTests => poseApplied;
        internal Vector3 ImportedLocalEulerForTests => importedInstance != null ? importedInstance.transform.localEulerAngles : Vector3.zero;
        internal Color[] FallbackMaterialColorsForTests => GetRendererColors();
        internal string[] MaterialDebugLinesForTests => GetMaterialDebugLines();

        public void Configure(Transform blockoutRoot, string weaponId)
        {
            weaponBlockoutRoot = blockoutRoot;
            activeWeaponId = weaponId ?? string.Empty;
            Refresh();
        }

        public void Refresh()
        {
            if (weaponBlockoutRoot == null)
            {
                modelLoaded = false;
                return;
            }

            bool shouldUseImportedModel = activeWeaponId == WeaponCatalog.FrontierRevolverId;
            if (!shouldUseImportedModel)
            {
                DestroyImportedInstance();
                SetGrayboxRenderersVisible(true);
                modelLoaded = false;
                return;
            }

            if (importedInstance == null && !attemptedLoad)
            {
                attemptedLoad = true;
                GameObject prefab = Resources.Load<GameObject>(RevolverResourcePath);
                if (prefab != null)
                {
                    importedInstance = Instantiate(prefab, weaponBlockoutRoot, false);
                    importedInstance.name = ImportedModelName;
                    ApplyPose(importedInstance.transform, GetPose(activeWeaponId));
                    ApplyReadableFallbackMaterials(importedInstance);
                }
            }

            modelLoaded = importedInstance != null;
            if (modelLoaded && !poseApplied)
            {
                ApplyPose(importedInstance.transform, GetPose(activeWeaponId));
            }

            if (modelLoaded && !fallbackMaterialsApplied)
            {
                ApplyReadableFallbackMaterials(importedInstance);
            }

            SetGrayboxRenderersVisible(!modelLoaded);
        }

        private void DestroyImportedInstance()
        {
            if (importedInstance == null)
            {
                return;
            }

            DestroyRuntimeMaterials(importedInstance);

            if (Application.isPlaying)
            {
                Destroy(importedInstance);
            }
            else
            {
                DestroyImmediate(importedInstance);
            }

            importedInstance = null;
            attemptedLoad = false;
            fallbackMaterialsApplied = false;
            poseApplied = false;
        }

        private void ApplyPose(Transform target, WeaponViewPose pose)
        {
            if (target == null)
            {
                poseApplied = false;
                return;
            }

            target.localPosition = pose.localPosition;
            target.localRotation = Quaternion.Euler(pose.localEulerAngles);
            target.localScale = pose.localScale;
            poseApplied = true;
        }

        private static WeaponViewPose GetPose(string weaponId)
        {
            if (weaponId == WeaponCatalog.FrontierRevolverId)
            {
                return new WeaponViewPose(
                    new Vector3(0.02f, -0.04f, 0.08f),
                    new Vector3(0f, 180f, 0f),
                    Vector3.one * 0.78f);
            }

            return WeaponViewPose.Identity;
        }

        private void ApplyReadableFallbackMaterials(GameObject modelRoot)
        {
            fallbackMaterialsApplied = false;
            if (modelRoot == null)
            {
                return;
            }

            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            int totalMaterialSlots = CountMaterialSlots(renderers);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material[] sourceMaterials = renderer.sharedMaterials;
                if (sourceMaterials == null || sourceMaterials.Length == 0)
                {
                    renderer.sharedMaterial = CreateFallbackMaterial(renderer.name, i, 0, totalMaterialSlots);
                    fallbackMaterialsApplied = true;
                    continue;
                }

                Material[] runtimeMaterials = new Material[sourceMaterials.Length];
                for (int slot = 0; slot < sourceMaterials.Length; slot++)
                {
                    Material sourceMaterial = sourceMaterials[slot];
                    runtimeMaterials[slot] = CreateFallbackMaterial($"{renderer.name}_{sourceMaterial?.name}", i, slot, totalMaterialSlots);
                }

                renderer.sharedMaterials = runtimeMaterials;
                fallbackMaterialsApplied = true;
            }
        }

        private static void DestroyRuntimeMaterials(GameObject modelRoot)
        {
            if (modelRoot == null)
            {
                return;
            }

            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i] != null ? renderers[i].sharedMaterials : null;
                if (materials == null)
                {
                    continue;
                }

                for (int slot = 0; slot < materials.Length; slot++)
                {
                    Material material = materials[slot];
                    if (material == null || !material.name.StartsWith("RuntimeRevolverFallbackMaterial"))
                    {
                        continue;
                    }

                    if (Application.isPlaying)
                    {
                        Destroy(material);
                    }
                    else
                    {
                        DestroyImmediate(material);
                    }
                }
            }
        }

        private static Material CreateFallbackMaterial(string rendererName, int rendererIndex, int materialIndex, int totalMaterialSlots)
        {
            RevolverMaterialCategory category = GetFallbackMaterialCategory(rendererName, rendererIndex, materialIndex, totalMaterialSlots);
            Color color = GetFallbackMaterialColor(category);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Sprites/Default");
            Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            material.name = $"RuntimeRevolverFallbackMaterial_{category}";
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            bool grip = category == RevolverMaterialCategory.Grip;
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", grip ? 0.05f : 0.5f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", grip ? 0.24f : 0.42f);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", grip ? 0.24f : 0.42f);
            }

            return material;
        }

        private static RevolverMaterialCategory GetFallbackMaterialCategory(string rendererName, int rendererIndex, int materialIndex, int totalMaterialSlots)
        {
            string lowerName = (rendererName ?? string.Empty).ToLowerInvariant();
            bool grip = lowerName.Contains("grip") || lowerName.Contains("handle") || lowerName.Contains("wood");
            bool brightMetal = lowerName.Contains("barrel") ||
                               lowerName.Contains("cylinder") ||
                               lowerName.Contains("chamber") ||
                               lowerName.Contains("sight");
            bool accentMetal = lowerName.Contains("trigger") || lowerName.Contains("hammer");
            bool bodyMetal = lowerName.Contains("frame") || lowerName.Contains("body") || lowerName.Contains("receiver");

            if (grip)
            {
                return RevolverMaterialCategory.Grip;
            }

            if (brightMetal)
            {
                return RevolverMaterialCategory.Steel;
            }

            if (accentMetal)
            {
                return RevolverMaterialCategory.AccentMetal;
            }

            if (bodyMetal || totalMaterialSlots <= 1)
            {
                return RevolverMaterialCategory.BodyGunmetal;
            }

            int bucket = Mathf.Abs((rendererIndex * 3) + materialIndex) % 5;
            return bucket switch
            {
                1 or 2 => RevolverMaterialCategory.Steel,
                3 => RevolverMaterialCategory.Grip,
                4 => RevolverMaterialCategory.AccentMetal,
                _ => RevolverMaterialCategory.BodyGunmetal
            };
        }

        private static Color GetFallbackMaterialColor(RevolverMaterialCategory category)
        {
            return category switch
            {
                RevolverMaterialCategory.Steel => new Color(0.72f, 0.74f, 0.70f, 1f),
                RevolverMaterialCategory.Grip => new Color(0.43f, 0.27f, 0.14f, 1f),
                RevolverMaterialCategory.AccentMetal => new Color(0.38f, 0.39f, 0.38f, 1f),
                _ => new Color(0.48f, 0.50f, 0.49f, 1f)
            };
        }

        private Color[] GetRendererColors()
        {
            if (importedInstance == null)
            {
                return System.Array.Empty<Color>();
            }

            Renderer[] renderers = importedInstance.GetComponentsInChildren<Renderer>(true);
            int totalMaterials = CountMaterialSlots(renderers);
            Color[] colors = new Color[totalMaterials];
            int colorIndex = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i] != null ? renderers[i].sharedMaterials : null;
                if (materials == null)
                {
                    continue;
                }

                for (int slot = 0; slot < materials.Length; slot++)
                {
                    Material material = materials[slot];
                    colors[colorIndex++] = material != null ? material.color : Color.clear;
                }
            }

            return colors;
        }

        private string[] GetMaterialDebugLines()
        {
            if (importedInstance == null)
            {
                return System.Array.Empty<string>();
            }

            Renderer[] renderers = importedInstance.GetComponentsInChildren<Renderer>(true);
            int totalMaterials = CountMaterialSlots(renderers);
            string[] lines = new string[totalMaterials];
            int lineIndex = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Material[] materials = renderer != null ? renderer.sharedMaterials : null;
                if (materials == null)
                {
                    continue;
                }

                for (int slot = 0; slot < materials.Length; slot++)
                {
                    Material material = materials[slot];
                    Color color = material != null ? material.color : Color.clear;
                    string category = material != null && material.name.StartsWith("RuntimeRevolverFallbackMaterial_")
                        ? material.name.Substring("RuntimeRevolverFallbackMaterial_".Length)
                        : "Imported";
                    lines[lineIndex++] = $"{renderer?.name ?? "missing"}[{slot}] | {category} | {material?.name ?? "missing"} | {color.r:0.00},{color.g:0.00},{color.b:0.00}";
                }
            }

            return lines;
        }

        private void SetGrayboxRenderersVisible(bool visible)
        {
            if (weaponBlockoutRoot == null)
            {
                return;
            }

            Renderer[] renderers = weaponBlockoutRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null ||
                    (importedInstance != null && renderer.transform.IsChildOf(importedInstance.transform)) ||
                    renderer.transform.name == "WeaponMuzzleFlash")
                {
                    continue;
                }

                renderer.enabled = visible;
            }
        }

        private static int CountNamedChildren(Transform root, string objectName)
        {
            if (root == null)
            {
                return 0;
            }

            int count = root.name == objectName ? 1 : 0;
            for (int i = 0; i < root.childCount; i++)
            {
                count += CountNamedChildren(root.GetChild(i), objectName);
            }

            return count;
        }

        private static int CountMaterialSlots(Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i] != null ? renderers[i].sharedMaterials : null;
                count += materials != null && materials.Length > 0 ? materials.Length : 1;
            }

            return count;
        }

        private enum RevolverMaterialCategory
        {
            BodyGunmetal,
            Steel,
            Grip,
            AccentMetal
        }

        internal readonly struct WeaponViewPose
        {
            public static readonly WeaponViewPose Identity = new WeaponViewPose(Vector3.zero, Vector3.zero, Vector3.one);

            public readonly Vector3 localPosition;
            public readonly Vector3 localEulerAngles;
            public readonly Vector3 localScale;

            public WeaponViewPose(Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
            {
                this.localPosition = localPosition;
                this.localEulerAngles = localEulerAngles;
                this.localScale = localScale;
            }
        }
    }
}
