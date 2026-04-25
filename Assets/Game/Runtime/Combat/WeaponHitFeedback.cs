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

    internal struct WeaponShotResolution
    {
        public Ray aimRay;
        public Vector3 cameraTargetPoint;
        public Vector3 muzzleStart;
        public Vector3 finalTracerEnd;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public WeaponShotHitKind kind;
        public Collider hitCollider;
        public IDamageable damageable;
        public bool muzzleObstructed;
        public Collider muzzleObstructionCollider;
        public int raycastHitCount;
        public int ignoredHitCount;
        public float maxRange;

        public bool HasHit => kind != WeaponShotHitKind.None && hitCollider != null;
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
