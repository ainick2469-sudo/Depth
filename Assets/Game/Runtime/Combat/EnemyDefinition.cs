using UnityEngine;

namespace FrontierDepths.Combat
{
    [CreateAssetMenu(menuName = "FrontierDepths/Combat/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string enemyId = "enemy.mire_hound";
        public string displayName = "Mire Hound";
        public float maxHealth = 55f;
        public float moveSpeed = 4.5f;
    }
}
