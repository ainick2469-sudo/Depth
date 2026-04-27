using UnityEngine;

namespace FrontierDepths.Combat
{
    [RequireComponent(typeof(Collider))]
    public sealed class AmmoPickup : MonoBehaviour
    {
        [SerializeField] private int amount = 10;
        private string lastBlockedReason = string.Empty;

        public int Amount => amount;
        public string LastBlockedReason => lastBlockedReason;

        public void Configure(int ammoAmount)
        {
            amount = Mathf.Max(1, ammoAmount);
            ConfigureCollider();
            EnsureMagnet();
        }

        public bool ApplyToPlayer(GameObject playerObject)
        {
            lastBlockedReason = string.Empty;
            PlayerWeaponController weapon = playerObject != null ? playerObject.GetComponentInParent<PlayerWeaponController>() : null;
            if (weapon == null)
            {
                return false;
            }

            int modifiedAmount = Mathf.Max(1, Mathf.CeilToInt(amount * RunStatAggregator.Current.AmmoPickupMultiplier));
            int added = weapon.TryAddAmmoToReserve(modifiedAmount, true);
            if (added <= 0 && weapon.ReserveAmmo >= weapon.MaxReserveAmmo)
            {
                lastBlockedReason = "Ammo reserve full.";
                Debug.Log(lastBlockedReason);
            }

            return added > 0;
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
