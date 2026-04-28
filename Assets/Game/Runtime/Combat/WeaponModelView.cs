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

        internal bool ModelLoadedForTests => modelLoaded;
        internal bool IsGrayboxFallbackActiveForTests => !modelLoaded;
        internal int InstanceCountForTests => weaponBlockoutRoot != null ? CountNamedChildren(weaponBlockoutRoot, ImportedModelName) : 0;

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
                    importedInstance.transform.localPosition = Vector3.zero;
                    importedInstance.transform.localRotation = Quaternion.identity;
                    importedInstance.transform.localScale = Vector3.one;
                }
            }

            modelLoaded = importedInstance != null;
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
    }
}
