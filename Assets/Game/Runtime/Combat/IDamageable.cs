using UnityEngine;

namespace FrontierDepths.Combat
{
    public interface IDamageable
    {
        void ApplyDamage(float amount, GameObject source);
    }
}
