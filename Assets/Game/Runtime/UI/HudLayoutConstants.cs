using UnityEngine;

namespace FrontierDepths.UI
{
    public static class HudLayoutConstants
    {
        public const float HudMargin = 24f;
        public const float ResourcePanelWidth = 240f;
        public const float WeaponPanelWidth = 340f;
        public const float WeaponPanelHeight = 170f;
        public const float MinimapOuterSize = 190f;
        public const float MinimapContentPadding = 22f;
        public const float AmmoPipSize = 12f;
        public const float AmmoPipSpacing = 4f;

        public const string TopCenterZoneName = "TopCenterZone";
        public const string TopRightZoneName = "TopRightZone";
        public const string BottomLeftZoneName = "BottomLeftZone";
        public const string BottomCenterZoneName = "BottomCenterZone";
        public const string BottomRightZoneName = "BottomRightZone";
        public const string CenterZoneName = "CenterZone";
        public const string OverlayZoneName = "OverlayZone";

        public static RectTransform EnsureZone(Transform root, string zoneName)
        {
            if (root == null || string.IsNullOrWhiteSpace(zoneName))
            {
                return null;
            }

            Transform existing = root.Find(zoneName);
            if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
            {
                StretchToParent(existingRect);
                return existingRect;
            }

            GameObject zoneObject = new GameObject(zoneName, typeof(RectTransform));
            zoneObject.transform.SetParent(root, false);
            RectTransform rect = zoneObject.GetComponent<RectTransform>();
            StretchToParent(rect);
            return rect;
        }

        public static RectTransform GetZoneOrRoot(Transform root, string zoneName)
        {
            if (root == null)
            {
                return null;
            }

            Transform zone = root.Find(zoneName);
            return zone != null && zone.TryGetComponent(out RectTransform rect)
                ? rect
                : root as RectTransform;
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
