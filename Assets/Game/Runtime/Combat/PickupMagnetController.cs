using UnityEngine;

namespace FrontierDepths.Combat
{
    public sealed class PickupMagnetController : MonoBehaviour
    {
        [SerializeField] private float pickupRadius = 1.6f;
        [SerializeField] private float magnetRadius = 3f;
        [SerializeField] private float magnetDelay = 0.35f;
        [SerializeField] private float magnetSpeed = 9f;

        private float createdAt;
        private float nextPlayerResolveTime;
        private float nextBlockedMessageTime;
        private Transform player;

        public float PickupRadius => pickupRadius;
        public float MagnetRadius => magnetRadius;

        private void Awake()
        {
            createdAt = Time.time;
        }

        private void Update()
        {
            ResolvePlayer();
            if (player == null || Time.time - createdAt < magnetDelay)
            {
                return;
            }

            Vector3 toPlayer = player.position - transform.position;
            float distance = toPlayer.magnitude;
            if (distance <= pickupRadius)
            {
                TryCollect(player.gameObject);
                return;
            }

            if (distance <= magnetRadius && distance > 0.001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, player.position + Vector3.up * 0.8f, magnetSpeed * Time.deltaTime);
            }
        }

        public bool TryCollect(GameObject playerObject)
        {
            if (playerObject == null)
            {
                return false;
            }

            bool applied = false;
            string blockedReason = string.Empty;
            GoldPickup gold = GetComponent<GoldPickup>();
            AmmoPickup ammo = GetComponent<AmmoPickup>();
            HealthPickup health = GetComponent<HealthPickup>();
            if (gold != null)
            {
                applied = gold.ApplyToPlayer(playerObject);
            }
            else if (ammo != null)
            {
                applied = ammo.ApplyToPlayer(playerObject);
                blockedReason = ammo.LastBlockedReason;
            }
            else if (health != null)
            {
                applied = health.ApplyToPlayer(playerObject);
                blockedReason = health.LastBlockedReason;
            }

            if (applied)
            {
                Destroy(gameObject);
            }
            else if (!string.IsNullOrWhiteSpace(blockedReason) && Time.time >= nextBlockedMessageTime)
            {
                nextBlockedMessageTime = Time.time + 1f;
                Debug.Log(blockedReason);
            }

            return applied;
        }

        private void ResolvePlayer()
        {
            if (player != null || Time.time < nextPlayerResolveTime)
            {
                return;
            }

            nextPlayerResolveTime = Time.time + 0.25f;
            PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
            player = health != null ? health.transform : null;
        }
    }
}
