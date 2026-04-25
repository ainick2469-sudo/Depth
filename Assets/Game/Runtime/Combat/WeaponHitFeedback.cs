using System;
using UnityEngine;

namespace FrontierDepths.Combat
{
    public enum WeaponHitFeedbackKind
    {
        Normal,
        Reduced,
        Kill,
        Environment
    }

    [Serializable]
    public struct WeaponHitFeedback
    {
        public WeaponHitFeedbackKind kind;
        public DamageResult damageResult;
        public GameObject targetObject;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public float requestedDamage;
        public float finalDamage;

        public bool IsDamageHit => damageResult.applied;
    }
}
