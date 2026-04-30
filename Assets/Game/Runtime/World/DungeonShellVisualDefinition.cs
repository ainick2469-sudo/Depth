using UnityEngine;

namespace FrontierDepths.World
{
    public readonly struct DungeonShellVisualDefinition
    {
        public readonly DungeonShellVisualKind kind;
        public readonly string displayName;
        public readonly string preferredResourcePath;
        public readonly Color fallbackColor;
        public readonly Vector3 fallbackScale;
        public readonly bool visualOnly;
        public readonly bool stripPrefabColliders;
        public readonly string warningLabel;

        public DungeonShellVisualDefinition(
            DungeonShellVisualKind kind,
            string displayName,
            string preferredResourcePath,
            Color fallbackColor,
            Vector3 fallbackScale,
            bool visualOnly,
            bool stripPrefabColliders,
            string warningLabel)
        {
            this.kind = kind;
            this.displayName = displayName;
            this.preferredResourcePath = preferredResourcePath;
            this.fallbackColor = fallbackColor;
            this.fallbackScale = fallbackScale;
            this.visualOnly = visualOnly;
            this.stripPrefabColliders = stripPrefabColliders;
            this.warningLabel = warningLabel;
        }
    }
}
