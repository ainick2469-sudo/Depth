using FrontierDepths.Combat;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class RoomPurposeInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string purposeId = string.Empty;
        [SerializeField] private string displayName = "Cache";
        [SerializeField] private string prompt = "Open Cache";
        [SerializeField] private int goldAmount;
        [SerializeField] private int ammoAmount;
        [SerializeField] private float healAmount;
        [SerializeField] private float healthRiskAmount;
        [SerializeField] private string resultPrefix = string.Empty;
        [SerializeField] private RoomPurposeEffect effect = RoomPurposeEffect.Cache;

        private bool claimed;
        private string lastResultMessage = string.Empty;

        public string PurposeId => purposeId;
        public string DisplayName => displayName;
        public string Prompt => claimed ? "Already claimed" : prompt;
        public string LastResultMessage => lastResultMessage;

        public void Configure(
            string claimId,
            string title,
            string promptText,
            int gold,
            int ammo,
            float heal,
            float healthRisk)
        {
            Configure(claimId, title, promptText, gold, ammo, heal, healthRisk, string.Empty, RoomPurposeEffect.Cache);
        }

        public void Configure(
            string claimId,
            string title,
            string promptText,
            int gold,
            int ammo,
            float heal,
            float healthRisk,
            string resultText,
            RoomPurposeEffect purposeEffect)
        {
            purposeId = claimId ?? string.Empty;
            displayName = string.IsNullOrWhiteSpace(title) ? "Cache" : title;
            prompt = string.IsNullOrWhiteSpace(promptText) ? displayName : promptText;
            goldAmount = Mathf.Max(0, gold);
            ammoAmount = Mathf.Max(0, ammo);
            healAmount = Mathf.Max(0f, heal);
            healthRiskAmount = Mathf.Max(0f, healthRisk);
            resultPrefix = resultText ?? string.Empty;
            effect = purposeEffect;
            claimed = IsClaimed();
        }

        public bool CanInteract(PlayerInteractor interactor, out string reason)
        {
            claimed = IsClaimed();
            if (claimed)
            {
                reason = "Already claimed.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor, out _))
            {
                return;
            }

            GameObject playerObject = interactor != null ? interactor.gameObject : null;
            PlayerHealth playerHealth = playerObject != null ? playerObject.GetComponentInParent<PlayerHealth>() : null;
            PlayerWeaponController weapon = playerObject != null ? playerObject.GetComponentInParent<PlayerWeaponController>() : null;

            float hpLost = ApplyNonLethalRisk(playerHealth);
            float healed = playerHealth != null ? playerHealth.Heal(healAmount) : 0f;
            int ammoAdded = weapon != null ? weapon.TryAddAmmoToReserve(ammoAmount, true) : 0;
            if (goldAmount > 0 && GameBootstrap.Instance != null && GameBootstrap.Instance.ProfileService != null)
            {
                GameBootstrap.Instance.ProfileService.AddGold(goldAmount);
            }

            MarkClaimed();
            claimed = true;
            lastResultMessage = BuildResultMessage(goldAmount, ammoAdded, healed, hpLost);
            if (!string.IsNullOrWhiteSpace(resultPrefix))
            {
                lastResultMessage = $"{resultPrefix} {lastResultMessage}";
            }

            ApplyPurposeSideEffect();
            Debug.Log($"{displayName}: {lastResultMessage}");
        }

        private void ApplyPurposeSideEffect()
        {
            if (effect == RoomPurposeEffect.Scout)
            {
                FloorState floor = GetCurrentFloor();
                if (floor != null)
                {
                    floor.stairDiscovered = true;
                    GameBootstrap.Instance?.RunService?.SaveActiveFloorState();
                }
            }
        }

        private bool IsClaimed()
        {
            FloorState floor = GetCurrentFloor();
            return floor != null && floor.HasClaimedRoomPurpose(purposeId);
        }

        private void MarkClaimed()
        {
            FloorState floor = GetCurrentFloor();
            if (floor == null)
            {
                return;
            }

            floor.MarkRoomPurposeClaimed(purposeId);
            GameBootstrap.Instance?.RunService?.SaveActiveFloorState();
        }

        private float ApplyNonLethalRisk(PlayerHealth playerHealth)
        {
            if (playerHealth == null || healthRiskAmount <= 0f || playerHealth.CurrentHealth <= 1f)
            {
                return 0f;
            }

            float amount = Mathf.Min(healthRiskAmount, Mathf.Max(0f, playerHealth.CurrentHealth - 1f));
            DamageResult result = playerHealth.ApplyDamage(new DamageInfo
            {
                amount = amount,
                source = gameObject,
                hitPoint = playerHealth.transform.position,
                hitNormal = Vector3.up,
                damageType = DamageType.Void,
                deliveryType = DamageDeliveryType.StatusTick
            });
            return result.applied ? result.damageApplied : 0f;
        }

        private static FloorState GetCurrentFloor()
        {
            return GameBootstrap.Instance != null && GameBootstrap.Instance.RunService != null
                ? GameBootstrap.Instance.RunService.Current?.currentFloor
                : null;
        }

        private static string BuildResultMessage(int gold, int ammo, float healed, float hpLost)
        {
            string message = string.Empty;
            if (gold > 0)
            {
                message += $"+{gold} gold";
            }

            if (ammo > 0)
            {
                message += (message.Length > 0 ? ", " : string.Empty) + $"+{ammo} ammo";
            }

            if (healed > 0f)
            {
                message += (message.Length > 0 ? ", " : string.Empty) + $"+{healed:0} HP";
            }

            if (hpLost > 0f)
            {
                message += (message.Length > 0 ? ", " : string.Empty) + $"-{hpLost:0} HP";
            }

            return string.IsNullOrWhiteSpace(message) ? "Nothing happened." : message;
        }
    }
}
