using UnityEngine;

namespace FrontierDepths.Core
{
    public static class DungeonDirectionUtility
    {
        private static readonly string[] EightWayLabels =
        {
            "N", "NE", "E", "SE", "S", "SW", "W", "NW"
        };

        public static float NormalizeYaw(float yawDegrees)
        {
            yawDegrees %= 360f;
            return yawDegrees < 0f ? yawDegrees + 360f : yawDegrees;
        }

        public static string GetCardinalLabel(float yawDegrees)
        {
            float normalized = NormalizeYaw(yawDegrees);
            int index = Mathf.RoundToInt(normalized / 45f) % EightWayLabels.Length;
            return EightWayLabels[index];
        }

        public static float GetCompassOffset(float yawDegrees)
        {
            return -NormalizeYaw(yawDegrees);
        }
    }
}
