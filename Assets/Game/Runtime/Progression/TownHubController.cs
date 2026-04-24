using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Progression
{
    public sealed class TownHubController : MonoBehaviour
    {
        private TownShopService shopService;
        private ShopDefinition activeShop;
        private string lastMessage = "Welcome back to the frontier.";
        private FirstPersonController playerController;

        public bool IsPanelOpen => activeShop != null;

        public string BuildPanelText()
        {
            if (activeShop == null)
            {
                return string.Empty;
            }

            ProfileState profile = GameBootstrap.Instance.ProfileService.Current;
            string text = $"{activeShop.displayName}\n{activeShop.greeting}\n\nGold: {profile.gold} | Sigils: {profile.townSigils}\n\n";
            for (int i = 0; i < activeShop.offers.Length; i++)
            {
                ShopOffer offer = activeShop.offers[i];
                text += $"{i + 1}. {offer.displayName} [{offer.cost}g]\n{offer.description}\n\n";
            }

            text += "Press 1-9 to take an offer. Press Escape to close.";
            if (!string.IsNullOrWhiteSpace(lastMessage))
            {
                text += $"\n\n{lastMessage}";
            }

            return text;
        }

        public string GetStatusLine()
        {
            ProfileState profile = GameBootstrap.Instance.ProfileService.Current;
            return $"Town Hub\nGold: {profile.gold}\nSigils: {profile.townSigils}\nWeapon: {profile.equippedWeaponId}";
        }

        private void Start()
        {
            shopService = new TownShopService(GameBootstrap.Instance.ProfileService);
            playerController = FindFirstObjectByType<FirstPersonController>();
        }

        private void Update()
        {
            if (!IsPanelOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) TrySelect(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TrySelect(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TrySelect(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TrySelect(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) TrySelect(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) TrySelect(5);
            if (Input.GetKeyDown(KeyCode.Alpha7)) TrySelect(6);
            if (Input.GetKeyDown(KeyCode.Alpha8)) TrySelect(7);
            if (Input.GetKeyDown(KeyCode.Alpha9)) TrySelect(8);
        }

        public void OpenService(ShopDefinition definition)
        {
            activeShop = definition;
            lastMessage = definition != null ? definition.greeting : string.Empty;
            playerController ??= FindFirstObjectByType<FirstPersonController>();
            playerController?.SetUiCaptured(definition != null);
        }

        public void CloseService()
        {
            activeShop = null;
            playerController ??= FindFirstObjectByType<FirstPersonController>();
            playerController?.SetUiCaptured(false);
        }

        private void TrySelect(int index)
        {
            if (activeShop == null)
            {
                return;
            }

            shopService.TryExecuteOffer(activeShop, index, out lastMessage);
        }
    }
}
