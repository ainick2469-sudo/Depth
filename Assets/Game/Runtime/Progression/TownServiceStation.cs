using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Progression
{
    public sealed class TownServiceStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private string shopId;
        [SerializeField] private string displayName;
        [SerializeField] private string prompt = "Browse stock";

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? shopId : displayName;
        public string Prompt => prompt;

        public void Configure(string configuredShopId, string configuredPrompt)
        {
            Configure(configuredShopId, configuredPrompt, configuredShopId);
        }

        public void Configure(string configuredShopId, string configuredPrompt, string configuredDisplayName)
        {
            shopId = configuredShopId ?? string.Empty;
            displayName = string.IsNullOrWhiteSpace(configuredDisplayName) ? shopId : configuredDisplayName;
            prompt = string.IsNullOrWhiteSpace(configuredPrompt) ? "Browse stock" : configuredPrompt;
        }

        public bool CanInteract(PlayerInteractor interactor, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public void Interact(PlayerInteractor interactor)
        {
            TownHubController townHub = FindAnyObjectByType<TownHubController>();
            townHub?.OpenService(TownShopCatalog.GetShop(shopId));
        }
    }
}
