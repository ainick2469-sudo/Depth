using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Progression
{
    public sealed class TownServiceLayoutManager : MonoBehaviour
    {
        public const string RootName = "RuntimeTownServiceAuthority";
        private static readonly string[] ExplicitLegacyObjectNames =
        {
            "QuartermasterStall",
            "ForgePad",
            "CurioStall",
            "StashChest",
            "BountyBoard"
        };

        private int hiddenLegacyCount;
        private int skippedDuplicateCount;
        private readonly System.Collections.Generic.List<string> hiddenLegacyNames = new System.Collections.Generic.List<string>();

        private void Start()
        {
            EnsureTownLayout(transform);
        }

        public void EnsureTownLayout(Transform parent)
        {
            using (LoadTimingLogger.Measure("Town layout build"))
            {
                hiddenLegacyCount = 0;
                skippedDuplicateCount = 0;
                hiddenLegacyNames.Clear();
                GetOrCreateRoot(parent != null ? parent : transform);
                HideExplicitLegacyObjects();
                HideLegacyServiceStations();
                HideLegacyServiceGeometry();
                HideCurioPlaceholders();
                TownRuntimeKioskBuilder.EnsureRuntimeKiosks(parent != null ? parent : transform);
                LabelExistingDungeonGate();
                LogSummary();
            }
        }

        public static TownServiceLayoutManager GetOrCreate(Transform parent)
        {
            TownServiceLayoutManager existing = FindAnyObjectByType<TownServiceLayoutManager>();
            if (existing != null)
            {
                existing.EnsureTownLayout(parent);
                return existing;
            }

            GameObject managerObject = new GameObject("TownServiceLayoutManager", typeof(TownServiceLayoutManager));
            if (parent != null)
            {
                managerObject.transform.SetParent(parent, false);
            }

            TownServiceLayoutManager manager = managerObject.GetComponent<TownServiceLayoutManager>();
            manager.EnsureTownLayout(parent);
            return manager;
        }

        private static Transform GetOrCreateRoot(Transform parent)
        {
            Transform existing = parent != null ? parent.Find(RootName) : null;
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject(RootName);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            return root.transform;
        }

        private void HideLegacyServiceStations()
        {
            TownServiceStation[] stations = FindObjectsByType<TownServiceStation>(FindObjectsSortMode.None);
            for (int i = 0; i < stations.Length; i++)
            {
                TownServiceStation station = stations[i];
                if (station == null ||
                    IsUnderRuntimeKioskRoot(station.transform))
                {
                    continue;
                }

                station.gameObject.SetActive(false);
                MarkHidden(station.gameObject);
            }
        }

        private void HideExplicitLegacyObjects()
        {
            for (int i = 0; i < ExplicitLegacyObjectNames.Length; i++)
            {
                GameObject legacy = GameObject.Find(ExplicitLegacyObjectNames[i]);
                if (legacy == null ||
                    legacy.name == "DungeonGate" ||
                    IsUnderRuntimeKioskRoot(legacy.transform))
                {
                    continue;
                }

                legacy.SetActive(false);
                MarkHidden(legacy);
            }
        }

        private void HideCurioPlaceholders()
        {
            GameObject[] roots = gameObject.scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                HideCurioRecursive(roots[i].transform);
            }
        }

        private void HideLegacyServiceGeometry()
        {
            GameObject[] roots = gameObject.scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                HideLegacyServiceGeometryRecursive(roots[i].transform);
            }
        }

        private void HideLegacyServiceGeometryRecursive(Transform node)
        {
            if (node == null ||
                IsUnderRuntimeKioskRoot(node) ||
                node.GetComponent<TownServiceLayoutManager>() != null ||
                node.GetComponent<TownRuntimeKioskBuilder>() != null)
            {
                return;
            }

            string lowerName = node.name.ToLowerInvariant();
            bool looksLikeLegacyService =
                lowerName.Contains("blacksmith") ||
                lowerName.Contains("quartermaster") ||
                lowerName.Contains("general store") ||
                lowerName.Contains("generalstore") ||
                lowerName.Contains("saloon") ||
                lowerName.Contains("bounty board") ||
                lowerName.Contains("bountyboard");

            if (looksLikeLegacyService && !lowerName.Contains("dungeon"))
            {
                node.gameObject.SetActive(false);
                MarkHidden(node.gameObject);
                return;
            }

            for (int i = 0; i < node.childCount; i++)
            {
                HideLegacyServiceGeometryRecursive(node.GetChild(i));
            }
        }

        private void HideCurioRecursive(Transform node)
        {
            if (node == null)
            {
                return;
            }

            string lowerName = node.name.ToLowerInvariant();
            if ((lowerName.Contains("curio") || lowerName.Contains("dusty")) && !IsUnderRuntimeKioskRoot(node))
            {
                node.gameObject.SetActive(false);
                MarkHidden(node.gameObject);
                return;
            }

            for (int i = 0; i < node.childCount; i++)
            {
                HideCurioRecursive(node.GetChild(i));
            }
        }

        private static bool IsUnderRuntimeKioskRoot(Transform transform)
        {
            while (transform != null)
            {
                if (transform.name == TownRuntimeKioskBuilder.RootName)
                {
                    return true;
                }

                transform = transform.parent;
            }

            return false;
        }

        private void LabelExistingDungeonGate()
        {
            GameObject gateObject = GameObject.Find("DungeonGate");
            if (gateObject == null)
            {
                Debug.LogWarning("TownServiceLayoutManager: existing DungeonGate object not found; runtime gate kiosk will not be created.");
                return;
            }

            Transform labelRoot = gateObject.transform.Find("DungeonGateLabel");
            if (labelRoot != null)
            {
                skippedDuplicateCount++;
                return;
            }

            WorldLabelBillboard.Create(
                gateObject.transform,
                "DungeonGateLabel",
                "Dungeon Gate",
                new Vector3(0f, 3.2f, 0f),
                UiTheme.Accent,
                42f,
                true);
        }

        private void LogSummary()
        {
            if (!Debug.isDebugBuild && !Application.isEditor)
            {
                return;
            }

            int activeRuntimeServices = 0;
            Transform kioskRoot = transform.Find(TownRuntimeKioskBuilder.RootName);
            if (kioskRoot != null)
            {
                activeRuntimeServices = kioskRoot.childCount;
            }

            string hidden = hiddenLegacyNames.Count > 0 ? string.Join(", ", hiddenLegacyNames) : "none";
            Debug.Log($"Town services unified | hiddenLegacy={hiddenLegacyCount} activeRuntimeServices={activeRuntimeServices} skippedDuplicates={skippedDuplicateCount} dungeonGate=scene hidden=[{hidden}]");
        }

        private void MarkHidden(GameObject target)
        {
            hiddenLegacyCount++;
            if (target != null && !hiddenLegacyNames.Contains(target.name))
            {
                hiddenLegacyNames.Add(target.name);
            }
        }
    }
}
