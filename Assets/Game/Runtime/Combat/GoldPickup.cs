using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Combat
{
    [RequireComponent(typeof(Collider))]
    public sealed class GoldPickup : MonoBehaviour
    {
        [SerializeField] private int amount = 8;

        public int Amount => amount;

        public void Configure(int goldAmount)
        {
            amount = Mathf.Max(1, goldAmount);
            ConfigureCollider();
            EnsureMagnet();
        }

        public bool ApplyToPlayer(GameObject playerObject)
        {
            if (!IsPlayer(playerObject) || GameBootstrap.Instance == null || GameBootstrap.Instance.ProfileService == null)
            {
                return false;
            }

            GameBootstrap.Instance.ProfileService.AddGold(amount);
            return true;
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

        private static bool IsPlayer(GameObject candidate)
        {
            return candidate != null && candidate.GetComponentInParent<PlayerHealth>() != null;
        }
    }
}
