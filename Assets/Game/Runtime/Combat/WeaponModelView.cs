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
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material material = renderer.sharedMaterial;
                bool needsFallback = material == null || LooksWhite(material.color);
                if (!needsFallback)
                {
                    continue;
                }

                renderer.sharedMaterial = CreateFallbackMaterial(renderer.name);
                fallbackMaterialsApplied = true;
            }
        }

        private static bool LooksWhite(Color color)
        {
            return color.r > 0.86f && color.g > 0.86f && color.b > 0.86f;
        }

        private static Material CreateFallbackMaterial(string rendererName)
        {
            string lowerName = (rendererName ?? string.Empty).ToLowerInvariant();
            Color color = lowerName.Contains("grip") || lowerName.Contains("handle")
                ? new Color(0.16f, 0.09f, 0.045f, 1f)
                : lowerName.Contains("barrel") || lowerName.Contains("cylinder")
                    ? new Color(0.42f, 0.43f, 0.42f, 1f)
                    : new Color(0.11f, 0.12f, 0.125f, 1f);

            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Universal Render Pipeline/Lit");
            Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            material.name = "RuntimeRevolverFallbackMaterial";
            material.color = color;
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", lowerName.Contains("grip") || lowerName.Contains("handle") ? 0.05f : 0.55f);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0.35f);
            }

            return material;
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
