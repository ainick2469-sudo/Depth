using UnityEngine;

namespace FrontierDepths.Progression
{
    public readonly struct TownServiceVisualDefinition
    {
        public readonly string serviceId;
        public readonly string displayName;
        public readonly string prompt;
        public readonly string preferredResourcePath;
        public readonly Vector3 layoutPosition;
        public readonly Color fallbackColor;
        public readonly Vector2 footprintSize;
        public readonly Vector3 interactionOffset;
        public readonly float yawOffset;
        public readonly float scaleMultiplier;
        public readonly Vector3 entranceLocalOffset;
        public readonly Vector3 labelLocalOffset;
        public readonly bool usesAssetVisual;

        public TownServiceVisualDefinition(
            string serviceId,
            string displayName,
            string prompt,
            string preferredResourcePath,
            Vector3 layoutPosition,
            Color fallbackColor,
            Vector2 footprintSize,
            Vector3 interactionOffset,
            float yawOffset,
            float scaleMultiplier,
            Vector3 entranceLocalOffset,
            Vector3 labelLocalOffset,
            bool usesAssetVisual)
        {
            this.serviceId = serviceId;
            this.displayName = displayName;
            this.prompt = prompt;
            this.preferredResourcePath = preferredResourcePath;
            this.layoutPosition = layoutPosition;
            this.fallbackColor = fallbackColor;
            this.footprintSize = footprintSize;
            this.interactionOffset = interactionOffset;
            this.yawOffset = yawOffset;
            this.scaleMultiplier = scaleMultiplier;
            this.entranceLocalOffset = entranceLocalOffset;
            this.labelLocalOffset = labelLocalOffset;
            this.usesAssetVisual = usesAssetVisual;
        }
    }
}
