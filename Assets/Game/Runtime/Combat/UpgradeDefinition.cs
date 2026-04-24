using UnityEngine;

namespace FrontierDepths.Combat
{
    [CreateAssetMenu(menuName = "FrontierDepths/Combat/Upgrade Definition")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        public string upgradeId = "upgrade.fire_ember";
        public string displayName = "Ember Round";
        [TextArea] public string description = "Shots ignite weak targets.";
        public GameplayTag primaryTag = GameplayTag.Fire;
        public StatModifier modifier;
    }
}
