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
            if (offer.purchaseLimit > 0 && profileService.GetPurchaseCount(shop.shopId, offer.offerId) >= offer.purchaseLimit)
            {
                message = "That offer is sold out for now.";
                return false;
            }

            if (!profileService.TrySpendGold(offer.cost))
            {
                message = "Not enough gold.";
                return false;
            }

            message = string.Empty;
            bool changed = offer.action switch
            {
                ShopOfferAction.BuyPortalSigil => BuyPortalSigil(),
                ShopOfferAction.UnlockWeapon => profileService.UnlockWeapon(offer.rewardId),
                ShopOfferAction.AcceptBounty => profileService.AcceptBounty(offer.rewardId),
                ShopOfferAction.StoreHeirloom => StoreHeirloom(offer.rewardId),
                ShopOfferAction.GainCurioDust => GainCurioDust(),
                ShopOfferAction.TurnInBounty => TurnInBounty(offer.rewardId, out message),
                ShopOfferAction.RestockAmmo => RestockAmmo(),
                ShopOfferAction.BuyRumor => BuyRumor(),
                _ => false
            };

            if (!changed)
            {
                profileService.AddGold(offer.cost);
                if (string.IsNullOrWhiteSpace(message))
                {
                    message = "Nothing changed.";
                }
                return false;
            }

            profileService.RecordPurchase(shop.shopId, offer.offerId);
            message = $"{offer.displayName} acquired.";
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

        private bool TurnInBounty(string bountyId, out string message)
        {
            return profileService.TryTurnInBounty(bountyId, out message);
        }

        private bool RestockAmmo()
        {
            RunState run = GameBootstrap.Instance != null ? GameBootstrap.Instance.RunService?.Current : null;
            if (run == null || run.weaponAmmo == null)
            {
                return false;
            }

            run.weaponAmmo.reserveAmmo = run.weaponAmmo.maxReserveAmmo;
            GameBootstrap.Instance.RunService.Save();
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
