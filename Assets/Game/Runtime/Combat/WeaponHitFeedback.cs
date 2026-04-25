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

    internal enum WeaponShotHitKind
    {
        None,
        Damageable,
        Environment
    }

    internal struct WeaponShotHit
    {
        public WeaponShotHitKind kind;
        public RaycastHit hit;
        public IDamageable damageable;

        public bool HasHit => kind != WeaponShotHitKind.None && hit.collider != null;
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
