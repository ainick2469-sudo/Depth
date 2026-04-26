using UnityEngine;

namespace FrontierDepths.Combat
{
    [RequireComponent(typeof(Collider))]
    public sealed class HealthPickup : MonoBehaviour
    {
        [SerializeField] private float amount = 10f;

        public float Amount => amount;

        public void Configure(float healAmount)
        {
            amount = Mathf.Max(1f, healAmount);
            ConfigureCollider();
        }

        public bool ApplyToPlayer(GameObject playerObject)
        {
            PlayerHealth playerHealth = playerObject != null ? playerObject.GetComponentInParent<PlayerHealth>() : null;
            if (playerHealth == null)
            {
                return false;
            }

            return playerHealth.Heal(amount) > 0f;
        }

        private void Awake()
        {
            ConfigureCollider();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != null && ApplyToPlayer(other.gameObject))
            {
                Destroy(gameObject);
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
