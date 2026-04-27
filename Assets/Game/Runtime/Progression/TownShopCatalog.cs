using System.Collections.Generic;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Progression
{
    public static class TownShopCatalog
    {
        public static ShopDefinition GetShop(string shopId)
        {
            if (string.IsNullOrWhiteSpace(shopId))
            {
                return null;
            }

            ShopDefinition runtime = CreateRuntimeShop(shopId);
            if (runtime != null)
            {
                return runtime;
            }

            ShopDefinition[] shops = Resources.LoadAll<ShopDefinition>("Definitions/Shops");
            for (int i = 0; i < shops.Length; i++)
            {
                if (shops[i] != null && shops[i].shopId == shopId)
                {
                    return shops[i];
                }
            }

            return null;
        }

        public static ShopDefinition[] GetVisibleTownServices()
        {
            List<ShopDefinition> shops = new List<ShopDefinition>
            {
                GetShop("shop.blacksmith"),
                GetShop("shop.quartermaster"),
                GetShop("shop.saloon"),
                GetShop("shop.bounty_board")
            };
            shops.RemoveAll(shop => shop == null);
            return shops.ToArray();
        }

        private static ShopDefinition CreateRuntimeShop(string shopId)
        {
            return shopId switch
            {
                "shop.quartermaster" => CreateQuartermaster(),
                "shop.saloon" => CreateSaloon(),
                "shop.bounty_board" => CreateBountyBoard(),
                _ => null
            };
        }

        private static ShopDefinition CreateQuartermaster()
        {
            ShopDefinition shop = ScriptableObject.CreateInstance<ShopDefinition>();
            shop.shopId = "shop.quartermaster";
            shop.serviceType = TownServiceType.Quartermaster;
            shop.displayName = "Quartermaster";
            shop.greeting = "Ammo, sigils, and the boring things that keep brave people alive.";
            shop.offers = new[]
            {
                new ShopOffer
                {
                    offerId = "offer.town_sigil",
                    displayName = "Town Sigil",
                    description = "One emergency return to town. Carry cap: 1.",
                    cost = 125,
                    purchaseLimit = 1,
                    action = ShopOfferAction.BuyPortalSigil,
                    rewardId = "item.town_sigil"
                },
                new ShopOffer
                {
                    offerId = "offer.reserve_restock",
                    displayName = "Reserve Ammo Restock",
                    description = "Refill your current revolver reserve to max.",
                    cost = 35,
                    purchaseLimit = 0,
                    action = ShopOfferAction.RestockAmmo,
                    rewardId = "ammo.reserve"
                }
            };
            return shop;
        }

        private static ShopDefinition CreateSaloon()
        {
            ShopDefinition shop = ScriptableObject.CreateInstance<ShopDefinition>();
            shop.shopId = "shop.saloon";
            shop.serviceType = TownServiceType.Saloon;
            shop.displayName = "Saloon / Inn";
            shop.greeting = "A warm meal, a bad song, and rumors from people who survived long enough to exaggerate.";
            shop.offers = new[]
            {
                new ShopOffer
                {
                    offerId = "offer.saloon_rumor",
                    displayName = "Buy A Rumor",
                    description = "Gain a small amount of class XP. Real rumor systems arrive later.",
                    cost = 20,
                    purchaseLimit = 0,
                    action = ShopOfferAction.BuyRumor,
                    rewardId = "rumor.prototype"
                }
            };
            return shop;
        }

        private static ShopDefinition CreateBountyBoard()
        {
            ShopDefinition shop = ScriptableObject.CreateInstance<ShopDefinition>();
            shop.shopId = "shop.bounty_board";
            shop.serviceType = TownServiceType.BountyBoard;
            shop.displayName = "Bounty Board";
            shop.greeting = "Wanted notices. Bad handwriting. Worse creatures.";
            List<ShopOffer> offers = new List<ShopOffer>();
            IReadOnlyList<BountyDefinition> bounties = BountyCatalog.All;
            ProfileState profile = GameBootstrap.Instance != null ? GameBootstrap.Instance.ProfileService?.Current : null;
            for (int i = 0; i < bounties.Count; i++)
            {
                BountyDefinition bounty = bounties[i];
                if (!BountyCatalog.IsVisible(profile, bounty))
                {
                    continue;
                }

                BountyRuntimeState state = profile != null ? BountyObjectiveTracker.GetOrCreate(profile, bounty.bountyId) : null;
                bool killed = state != null && state.state == BountyState.Killed;
                offers.Add(new ShopOffer
                {
                    offerId = killed ? $"turnin.{bounty.bountyId}" : $"accept.{bounty.bountyId}",
                    displayName = killed ? $"Turn In: {bounty.targetName}" : bounty.title,
                    description = $"Floor {bounty.minFloor}-{bounty.maxFloor}. {bounty.reason} Reward: {bounty.goldReward}g, {bounty.xpReward} XP.",
                    cost = 0,
                    purchaseLimit = 0,
                    action = killed ? ShopOfferAction.TurnInBounty : ShopOfferAction.AcceptBounty,
                    rewardId = bounty.bountyId
                });
            }

            shop.offers = offers.ToArray();
            return shop;
        }
    }
}
