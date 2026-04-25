using System;
using UnityEngine;

namespace FrontierDepths.Combat
{
    public enum DamageType
    {
        Physical,
        Fire,
        Frost,
        Shock,
        Poison,
        Blood,
        Holy,
        Void
    }

    public enum DamageDeliveryType
    {
        Raycast,
        Projectile,
        Melee,
        Area,
        Beam,
        Trap,
        StatusTick
    }

    [Serializable]
    public struct DamageInfo
    {
        public float amount;
        public GameObject source;
        public string weaponId;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public DamageType damageType;
        public DamageDeliveryType deliveryType;
        public GameplayTag[] tags;
        public bool canCrit;
        public bool isCritical;
        public float knockbackForce;
        public float statusChance;

        public bool HasTag(GameplayTag tag)
        {
            if (tags == null)
            {
                return false;
            }

            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == tag)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
