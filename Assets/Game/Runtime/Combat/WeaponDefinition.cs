using UnityEngine;

namespace FrontierDepths.Combat
{
    [CreateAssetMenu(menuName = "FrontierDepths/Combat/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string weaponId = "weapon.frontier_revolver";
        public string displayName = "Frontier Revolver";
        [TextArea] public string description = "Reliable sidearm for the long descent.";
        public float baseDamage = 14f;
        public float fireRate = 2.2f;
    }
}
