using System;

namespace FrontierDepths.Combat
{
    [Serializable]
    public struct DamageResult
    {
        public bool applied;
        public float damageApplied;
        public bool killedTarget;
        public float remainingHealth;

        public static DamageResult Ignored => new DamageResult
        {
            applied = false,
            damageApplied = 0f,
            killedTarget = false,
            remainingHealth = 0f
        };
    }
}
