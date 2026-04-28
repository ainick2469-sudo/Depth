using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.UI
{
    public static class HudSpriteCatalog
    {
        private const string BasePath = "ThirdParty/DungeonHUD/";

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> WarnedMissingPaths = new HashSet<string>();

        public static Sprite GetHealthBarSprite()
        {
            return LoadSprite("Bars/health_bar_frame_fill");
        }

        public static Sprite GetStaminaBarSprite()
        {
            return LoadSprite("Bars/stamina_grit_bar_frame_fill");
        }

        public static Sprite GetFocusBarSprite()
        {
            return LoadSprite("Bars/mana_bar_frame_fill");
        }

        public static Sprite GetWeaponPanelSprite()
        {
            return LoadSprite("Panels/weapon_ammo_panel_frame");
        }

        public static Sprite GetInventoryPanelSprite()
        {
            return LoadSprite("Panels/inventory_panel_frame_large");
        }

        public static Sprite GetMinimapFrameSprite()
        {
            return LoadSprite("Icons/Map/circular_minimap_frame");
        }

        private static Sprite LoadSprite(string relativePath)
        {
            if (Cache.TryGetValue(relativePath, out Sprite sprite))
            {
                return sprite;
            }

            sprite = Resources.Load<Sprite>($"{BasePath}{relativePath}");
            Cache[relativePath] = sprite;
            if (sprite == null && WarnedMissingPaths.Add(relativePath))
            {
                Debug.LogWarning($"HUD sprite missing at Resources path '{BasePath}{relativePath}'. Falling back to runtime UI.");
            }

            return sprite;
        }
    }
}
