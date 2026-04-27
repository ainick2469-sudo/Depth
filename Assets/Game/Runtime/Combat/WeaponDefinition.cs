using UnityEngine;

namespace FrontierDepths.Combat
{
    public enum WeaponArchetype
    {
        Revolver,
        Rifle,
        Shotgun,
        Melee,
        Staff,
        Bow,
        Debug
    }

    [CreateAssetMenu(menuName = "FrontierDepths/Combat/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string weaponId = "weapon.frontier_revolver";
        public string displayName = "Frontier Revolver";
        [TextArea] public string description = "Reliable sidearm for the long descent.";
        public WeaponArchetype weaponArchetype = WeaponArchetype.Revolver;
        public float baseDamage = 15f;
        public float fireRate = 2.857f;
        public int magazineSize = 6;
        public int startingReserveAmmo = 30;
        public int maxReserveAmmo = 60;
        public float reloadDuration = 1.4f;
        public float maxRange = 45f;
        public float fullDamageRange = 17f;
        [Range(0f, 1f)] public float damageMultiplierAtMaxRange = 0.50f;
        public DamageType damageType = DamageType.Physical;
        public DamageDeliveryType deliveryType = DamageDeliveryType.Raycast;
        public float critChance;
        public float critMultiplier = 2f;
        public float knockbackForce;
        public string statusEffectId = string.Empty;
        public float statusChance;
        public float projectileSpeed;
        public float areaRadius;
        public float meleeRange;
    }
}
