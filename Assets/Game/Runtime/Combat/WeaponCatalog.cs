using UnityEngine;

namespace FrontierDepths.Combat
{
    public static class WeaponCatalog
    {
        public const string FrontierRevolverId = "weapon.frontier_revolver";
        public const string FrontierRifleId = "weapon.frontier_rifle";

        private static WeaponDefinition fallbackRevolver;
        private static WeaponDefinition fallbackRifle;

        public static bool TryGet(string weaponId, out WeaponDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return false;
            }

            WeaponDefinition[] definitions = Resources.LoadAll<WeaponDefinition>("Definitions/Combat");
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null && definitions[i].weaponId == weaponId)
                {
                    definition = definitions[i];
                    return true;
                }
            }

            definition = weaponId switch
            {
                FrontierRevolverId => fallbackRevolver ??= CreateRevolver(),
                FrontierRifleId => fallbackRifle ??= CreateRifle(),
                _ => null
            };
            return definition != null;
        }

        public static string GetDisplayName(string weaponId)
        {
            return TryGet(weaponId, out WeaponDefinition definition)
                ? definition.displayName
                : (string.IsNullOrWhiteSpace(weaponId) ? "Unknown Weapon" : weaponId);
        }

        private static WeaponDefinition CreateRevolver()
        {
            WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.weaponId = FrontierRevolverId;
            weapon.displayName = "Frontier Revolver";
            weapon.description = "Reliable starter sidearm.";
            weapon.weaponArchetype = WeaponArchetype.Revolver;
            weapon.baseDamage = 15f;
            weapon.fireRate = 2.857f;
            weapon.magazineSize = 6;
            weapon.startingReserveAmmo = 36;
            weapon.maxReserveAmmo = 72;
            weapon.reloadDuration = 1.4f;
            weapon.maxRange = 45f;
            weapon.fullDamageRange = 17f;
            weapon.damageMultiplierAtMaxRange = 0.5f;
            weapon.damageType = DamageType.Physical;
            weapon.deliveryType = DamageDeliveryType.Raycast;
            return weapon;
        }

        private static WeaponDefinition CreateRifle()
        {
            WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.weaponId = FrontierRifleId;
            weapon.displayName = "Frontier Rifle";
            weapon.description = "Slow, steady long gun with better reach.";
            weapon.weaponArchetype = WeaponArchetype.Rifle;
            weapon.baseDamage = 24f;
            weapon.fireRate = 1.55f;
            weapon.magazineSize = 5;
            weapon.startingReserveAmmo = 24;
            weapon.maxReserveAmmo = 60;
            weapon.reloadDuration = 1.85f;
            weapon.maxRange = 70f;
            weapon.fullDamageRange = 38f;
            weapon.damageMultiplierAtMaxRange = 0.72f;
            weapon.damageType = DamageType.Physical;
            weapon.deliveryType = DamageDeliveryType.Raycast;
            weapon.critChance = 0.03f;
            return weapon;
        }
    }
}
