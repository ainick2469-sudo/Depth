using UnityEngine;

namespace FrontierDepths.Combat
{
    [CreateAssetMenu(menuName = "FrontierDepths/Combat/Status Effect Definition")]
    public sealed class StatusEffectDefinition : ScriptableObject
    {
        public string effectId = "status.burn";
        public string displayName = "Burn";
        public GameplayTag tag = GameplayTag.Fire;
        public float duration = 4f;
    }
}
