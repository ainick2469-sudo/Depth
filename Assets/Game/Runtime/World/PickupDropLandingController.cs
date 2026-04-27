using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class PickupDropLandingController : MonoBehaviour
    {
        private const float GroundRaycastHeight = 4f;
        private const float GroundRaycastDistance = 16f;
        private const float LandingDuration = 0.36f;

        private Collider[] colliders;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float startedAt;
        private bool landed;

        public bool IsLanded => landed;
        public Vector3 TargetPosition => targetPosition;

        private void Awake()
        {
            BeginLanding(transform.position);
        }

        public void BeginLanding(Vector3 spawnPosition)
        {
            colliders = GetComponentsInChildren<Collider>(true);
            SetCollidersEnabled(false);
            startPosition = spawnPosition + Vector3.up * 0.4f;
            targetPosition = TryFindGroundedPosition(spawnPosition, out Vector3 grounded)
                ? grounded + Vector3.up * 0.35f
                : spawnPosition;

            if ((targetPosition - spawnPosition).sqrMagnitude <= 0.01f && !TryFindGroundedPosition(spawnPosition, out _))
            {
                Debug.LogWarning($"Pickup grounding could not find floor below {name}; leaving it at spawn position instead of deleting it.");
            }

            transform.position = startPosition;
            startedAt = Time.time;
            landed = false;
        }

        public static bool TryFindGroundedPosition(Vector3 origin, out Vector3 grounded)
        {
            Vector3 start = origin + Vector3.up * GroundRaycastHeight;
            if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, GroundRaycastDistance, -1, QueryTriggerInteraction.Ignore))
            {
                grounded = hit.point;
                return true;
            }

            grounded = origin;
            return false;
        }

        private void Update()
        {
            if (landed)
            {
                transform.Rotate(Vector3.up, 70f * Time.deltaTime, Space.World);
                transform.position = targetPosition + Vector3.up * (Mathf.Sin(Time.time * 3.1f) * 0.06f);
                return;
            }

            float t = Mathf.Clamp01((Time.time - startedAt) / LandingDuration);
            Vector3 position = Vector3.Lerp(startPosition, targetPosition, EaseOut(t));
            position.y += Mathf.Sin(t * Mathf.PI) * 0.75f;
            transform.position = position;
            if (t >= 1f)
            {
                landed = true;
                transform.position = targetPosition;
                SetCollidersEnabled(true);
            }
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = enabled;
                }
            }
        }

        private static float EaseOut(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }
}
