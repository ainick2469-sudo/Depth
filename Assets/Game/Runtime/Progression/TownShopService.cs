using FrontierDepths.Core;

namespace FrontierDepths.Progression
{
    public sealed class TownShopService : IShopService
    {
        private readonly ProfileService profileService;

        public TownShopService(ProfileService profileService)
        {
            this.profileService = profileService;
        }

        public bool TryExecuteOffer(ShopDefinition shop, int index, out string message)
        {
            if (shop == null || shop.offers == null || index < 0 || index >= shop.offers.Length)
            {
                message = "That offer is not available.";
                return false;
            }

            ShopOffer offer = shop.offers[index];
            int effectiveCost = ReputationService.GetDiscountedCost(offer.cost, profileService.Current.townReputation);
            if (offer.purchaseLimit > 0 && profileService.GetPurchaseCount(shop.shopId, offer.offerId) >= offer.purchaseLimit)
            {
                message = "That offer is sold out for now.";
                return false;
            }

            if (!profileService.TrySpendGold(effectiveCost))
            {
                message = "Not enough gold.";
                return false;
            }

            message = string.Empty;
            bool changed = offer.action switch
            {
                ShopOfferAction.BuyPortalSigil => BuyPortalSigil(),
                ShopOfferAction.UnlockWeapon => UnlockWeapon(offer.rewardId, out message),
                ShopOfferAction.AcceptBounty => profileService.AcceptBounty(offer.rewardId),
                ShopOfferAction.StoreHeirloom => StoreHeirloom(offer.rewardId),
                ShopOfferAction.GainCurioDust => GainCurioDust(),
                ShopOfferAction.TurnInBounty => TurnInBounty(offer.rewardId, out message),
                ShopOfferAction.RestockAmmo => RestockAmmo(out message),
                ShopOfferAction.BuyRumor => BuyRumor(),
                _ => false
            };

            if (!changed)
            {
                profileService.AddGold(effectiveCost);
                if (string.IsNullOrWhiteSpace(message))
                {
                    message = "Nothing changed.";
                }
                return false;
            }

            profileService.RecordPurchase(shop.shopId, offer.offerId);
            if (string.IsNullOrWhiteSpace(message))
            {
                message = $"{offer.displayName} acquired.";
            }
            return true;
        }

        private bool BuyPortalSigil()
        {
            if (profileService.Current.townSigils >= 1)
            {
                return false;
            }

            profileService.AddTownSigil(1);
            return true;
        }

        private bool StoreHeirloom(string heirloomId)
        {
            profileService.SetHeirloom(heirloomId);
            return true;
        }

        private bool GainCurioDust()
        {
            profileService.Current.curioDust += 1;
            profileService.Save();
            return true;
        }

        private bool UnlockWeapon(string weaponId, out string message)
        {
            if (!profileService.UnlockWeapon(weaponId))
            {
                message = "Already owned.";
                return false;
            }

            message = weaponId == "weapon.frontier_rifle"
                ? "Frontier Rifle purchased. Press I to equip."
                : "Weapon purchased. Press I to equip.";
            return true;
        }

        private bool TurnInBounty(string bountyId, out string message)
        {
            return profileService.TryTurnInBounty(bountyId, out message);
        }

        private bool RestockAmmo(out string message)
        {
            message = string.Empty;
            if (GameBootstrap.Instance == null || GameBootstrap.Instance.RunService == null)
            {
                message = "No ammo state available.";
                return false;
            }

            const int purchaseAmount = 12;
            RunState run = GameBootstrap.Instance.RunService.EnsureRun();
            int added = run.TryAddReserveAmmoToActiveWeapon(purchaseAmount);
            if (added <= 0)
            {
                message = "Ammo reserve full.";
                return false;
            }

            GameBootstrap.Instance.RunService.Save();
            GameplayEventBus.Publish(new GameplayEvent
            {
                eventType = GameplayEventType.AmmoRestocked,
                weaponId = run.equippedWeaponId,
                amount = added,
                timestamp = UnityEngine.Time.unscaledTime
            });
            message = $"+{added} reserve ammo.";
            return true;
        }

        private bool BuyRumor()
        {
            profileService.Current.classXp += 15;
            profileService.Save();
            return true;
        }
    }
}
