using UnityEngine;

namespace FrontierDepths.Combat
{
    [RequireComponent(typeof(Collider))]
    public sealed class HealthPickup : MonoBehaviour
    {
        [SerializeField] private float amount = 10f;
        private string lastBlockedReason = string.Empty;

        public float Amount => amount;
        public string LastBlockedReason => lastBlockedReason;

        public void Configure(float healAmount)
        {
            amount = Mathf.Max(1f, healAmount);
            ConfigureCollider();
            EnsureMagnet();
        }

        public bool ApplyToPlayer(GameObject playerObject)
        {
            lastBlockedReason = string.Empty;
            PlayerHealth playerHealth = playerObject != null ? playerObject.GetComponentInParent<PlayerHealth>() : null;
            if (playerHealth == null)
            {
                return false;
            }

            float healed = playerHealth.Heal(amount);
            if (healed <= 0f)
            {
                lastBlockedReason = "Health full.";
            }

            return healed > 0f;
        }

        private void Awake()
        {
            ConfigureCollider();
            EnsureMagnet();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != null && ApplyToPlayer(other.gameObject))
            {
                Destroy(gameObject);
            }
        }

        private void EnsureMagnet()
        {
            if (GetComponent<PickupMagnetController>() == null)
            {
                gameObject.AddComponent<PickupMagnetController>();
            }
        }

        private void ConfigureCollider()
        {
            Collider pickupCollider = GetComponent<Collider>();
            if (pickupCollider != null)
            {
                pickupCollider.isTrigger = true;
            }
        }
    }
}
