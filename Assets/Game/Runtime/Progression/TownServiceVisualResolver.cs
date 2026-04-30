using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.Progression
{
    public static class TownServiceVisualResolver
    {
        private static readonly HashSet<string> WarnedMissingPaths = new HashSet<string>();

        public static GameObject LoadPreferredVisual(TownServiceVisualDefinition definition, out bool usedAssetVisual)
        {
            usedAssetVisual = false;
            if (string.IsNullOrWhiteSpace(definition.preferredResourcePath))
            {
                return null;
            }

            GameObject prefab = Resources.Load<GameObject>(definition.preferredResourcePath);
            if (prefab != null)
            {
                usedAssetVisual = true;
                return prefab;
            }

            if (WarnedMissingPaths.Add(definition.preferredResourcePath))
            {
                Debug.LogWarning($"Town visual resource missing: {definition.preferredResourcePath}. Using fallback visual for {definition.displayName}.");
            }

            return null;
        }

        internal static int MissingWarningCountForTests => WarnedMissingPaths.Count;

        internal static void ResetWarningsForTests()
        {
            WarnedMissingPaths.Clear();
        }

        internal static GameObject LoadVisualForTests(string resourcePath)
        {
            TownServiceVisualDefinition definition = new TownServiceVisualDefinition(
                "test.service",
                "Test Service",
                "Press E to test",
                resourcePath,
                Vector3.zero,
                Color.white,
                Vector2.one,
                Vector3.forward,
                0f,
                1f,
                Vector3.forward,
                Vector3.up,
                true);

            return LoadPreferredVisual(definition, out _);
        }
    }
}
