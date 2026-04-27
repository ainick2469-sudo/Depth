using UnityEngine;

namespace FrontierDepths.Combat
{
    [RequireComponent(typeof(Collider))]
    public sealed class AmmoPickup : MonoBehaviour
    {
        [SerializeField] private int amount = 10;

        public int Amount => amount;

        public void Configure(int ammoAmount)
        {
            amount = Mathf.Max(1, ammoAmount);
            ConfigureCollider();
        }

        public bool ApplyToPlayer(GameObject playerObject)
        {
            PlayerWeaponController weapon = playerObject != null ? playerObject.GetComponentInParent<PlayerWeaponController>() : null;
            if (weapon == null)
            {
                return false;
            }

            int modifiedAmount = Mathf.Max(1, Mathf.CeilToInt(amount * RunStatAggregator.Current.AmmoPickupMultiplier));
            int added = weapon.TryAddAmmoToReserve(modifiedAmount, true);
            if (added <= 0 && weapon.ReserveAmmo >= weapon.MaxReserveAmmo)
            {
                Debug.Log("Ammo Full");
            }

            return added > 0;
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
