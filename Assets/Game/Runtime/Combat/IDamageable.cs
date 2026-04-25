using UnityEngine;

namespace FrontierDepths.Combat
{
    public interface IDamageable
    {
        DamageResult ApplyDamage(DamageInfo damageInfo);
    }
}
