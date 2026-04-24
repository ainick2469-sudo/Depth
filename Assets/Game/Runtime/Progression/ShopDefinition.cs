using System;
using UnityEngine;

namespace FrontierDepths.Progression
{
    [CreateAssetMenu(menuName = "FrontierDepths/Progression/Shop Definition")]
    public sealed class ShopDefinition : ScriptableObject
    {
        public string shopId = "shop.quartermaster";
        public TownServiceType serviceType;
        public string displayName = "Quartermaster";
        [TextArea] public string greeting = "A little preparation keeps the underworld from swallowing you whole.";
        public ShopOffer[] offers = Array.Empty<ShopOffer>();
    }

    [Serializable]
    public struct ShopOffer
    {
        public string offerId;
        public string displayName;
        [TextArea] public string description;
        public int cost;
        public int purchaseLimit;
        public ShopOfferAction action;
        public string rewardId;
    }

    public enum ShopOfferAction
    {
        BuyPortalSigil,
        UnlockWeapon,
        AcceptBounty,
        StoreHeirloom,
        GainCurioDust
    }
}
