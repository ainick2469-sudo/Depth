using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    public static class DungeonShellVisualResolver
    {
        private static readonly HashSet<string> WarnedMissingPaths = new HashSet<string>();
        private static readonly HashSet<string> WarnedBadPrefabs = new HashSet<string>();

        public static GameObject InstantiateVisual(
            DungeonShellVisualDefinition definition,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            out bool usedWrapper)
        {
            usedWrapper = false;
            if (parent == null || string.IsNullOrWhiteSpace(definition.preferredResourcePath))
            {
                return null;
            }

            GameObject prefab = Resources.Load<GameObject>(definition.preferredResourcePath);
            if (prefab == null)
            {
                WarnMissing(definition);
                return null;
            }

            if (prefab.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                WarnBad(definition, "no renderers");
                return null;
            }

            GameObject visual = Object.Instantiate(prefab, parent, false);
            visual.name = $"ShellVisual_{definition.kind}";
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = localRotation;
            visual.transform.localScale = localScale;
            if (definition.stripPrefabColliders)
            {
                StripColliders(visual.transform);
            }

            usedWrapper = true;
            return visual;
        }

        public static bool TryInstantiateVisual(
            DungeonShellVisualKind kind,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            out GameObject visual,
            bool applyTint = false,
            Color tint = default)
        {
            visual = null;
            if (!DungeonShellVisualCatalog.TryGet(kind, out DungeonShellVisualDefinition definition))
            {
                return false;
            }

            visual = InstantiateVisual(definition, parent, localPosition, localScale, localRotation, out bool usedWrapper);
            if (visual != null && applyTint)
            {
                ApplyRuntimeTint(visual, tint);
            }

            return usedWrapper && visual != null;
        }

        internal static int MissingWarningCountForTests => WarnedMissingPaths.Count;
        internal static int BadPrefabWarningCountForTests => WarnedBadPrefabs.Count;

        internal static void ResetWarningsForTests()
        {
            WarnedMissingPaths.Clear();
            WarnedBadPrefabs.Clear();
        }

        internal static GameObject LoadVisualForTests(string resourcePath)
        {
            DungeonShellVisualDefinition definition = new DungeonShellVisualDefinition(
                DungeonShellVisualKind.RoomAccent,
                "Test Visual",
                resourcePath,
                Color.white,
                Vector3.one,
                visualOnly: true,
                stripPrefabColliders: true,
                warningLabel: "Test Visual");

            GameObject parent = new GameObject("DungeonShellVisualResolverTestParent");
            try
            {
                return InstantiateVisual(definition, parent.transform, Vector3.zero, Vector3.one, Quaternion.identity, out _);
            }
            finally
            {
                if (parent != null && parent.transform.childCount == 0)
                {
                    Object.DestroyImmediate(parent);
                }
            }
        }

        private static void WarnMissing(DungeonShellVisualDefinition definition)
        {
            if (WarnedMissingPaths.Add(definition.preferredResourcePath))
            {
                Debug.LogWarning($"Dungeon shell visual resource missing: {definition.preferredResourcePath}. Using graybox fallback for {definition.warningLabel}.");
            }
        }

        private static void WarnBad(DungeonShellVisualDefinition definition, string reason)
        {
            if (WarnedBadPrefabs.Add(definition.preferredResourcePath))
            {
                Debug.LogWarning($"Dungeon shell visual resource invalid: {definition.preferredResourcePath} ({reason}). Using graybox fallback for {definition.warningLabel}.");
            }
        }

        private static void StripColliders(Transform root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(colliders[i]);
                }
                else
                {
                    Object.DestroyImmediate(colliders[i]);
                }
            }
        }

        private static void ApplyRuntimeTint(GameObject visual, Color tint)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material source = renderers[i].sharedMaterial;
                Shader shader = source != null ? source.shader : Shader.Find("Standard");
                Material instance = source != null ? new Material(source) : new Material(shader);
                instance.color = tint;
                renderers[i].sharedMaterial = instance;
            }
        }
    }
}
