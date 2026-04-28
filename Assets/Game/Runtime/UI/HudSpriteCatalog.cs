using System.Collections.Generic;
using FrontierDepths.Combat;
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
            return TryGetHealthBarSprite();
        }

        public static Sprite GetStaminaBarSprite()
        {
            return TryGetStaminaBarSprite();
        }

        public static Sprite GetFocusBarSprite()
        {
            return TryGetFocusBarSprite();
        }

        public static Sprite GetWeaponPanelSprite()
        {
            return TryGetWeaponPanelFrame();
        }

        public static Sprite GetInventoryPanelSprite()
        {
            return LoadSprite("Panels/inventory_panel_frame_large");
        }

        public static Sprite GetMinimapFrameSprite()
        {
            return TryGetMinimapFrame();
        }

        public static Sprite TryGetHealthBarSprite()
        {
            return LoadSprite("Bars/health_bar_frame_fill");
        }

        public static Sprite TryGetStaminaBarSprite()
        {
            return LoadSprite("Bars/stamina_grit_bar_frame_fill");
        }

        public static Sprite TryGetFocusBarSprite()
        {
            return LoadSprite("Bars/mana_bar_frame_fill");
        }

        public static Sprite TryGetWeaponPanelFrame()
        {
            return LoadSprite("Panels/weapon_ammo_panel_frame");
        }

        public static Sprite TryGetMinimapFrame()
        {
            return LoadSprite("Icons/Map/circular_minimap_frame");
        }

        public static Sprite TryGetAmmoPip()
        {
            return LoadSprite("Icons/Ammo/ammo_pip");
        }

        public static Sprite TryGetFallbackIcon()
        {
            return LoadSprite("Icons/Weapons/fallback_weapon");
        }

        public static Sprite TryGetWeaponIcon(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return TryGetFallbackIcon();
            }

            string relativePath = weaponId switch
            {
                WeaponCatalog.FrontierRevolverId => "Icons/Weapons/frontier_revolver",
                WeaponCatalog.FrontierRifleId => "Icons/Weapons/frontier_rifle",
                _ => $"Icons/Weapons/{NormalizeResourceName(weaponId)}"
            };

            return LoadSprite(relativePath);
        }

        internal static int CachedSpriteCountForTests => Cache.Count;
        internal static int MissingSpriteWarningCountForTests => WarnedMissingPaths.Count;

        internal static void ClearCacheForTests()
        {
            Cache.Clear();
            WarnedMissingPaths.Clear();
        }

        internal static Sprite TryGetSpriteForTests(string relativePath)
        {
            return LoadSprite(relativePath);
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

        private static string NormalizeResourceName(string value)
        {
            char[] chars = value.Trim().ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]))
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }
    }
}
