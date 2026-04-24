using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Progression
{
    public sealed class TownServiceStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private string shopId;
        [SerializeField] private string prompt = "Browse stock";

        public string DisplayName => shopId;
        public string Prompt => prompt;

        public bool CanInteract(PlayerInteractor interactor, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public void Interact(PlayerInteractor interactor)
        {
            TownHubController townHub = FindFirstObjectByType<TownHubController>();
            ShopDefinition[] shops = Resources.LoadAll<ShopDefinition>("Definitions/Shops");
            for (int i = 0; i < shops.Length; i++)
            {
                if (shops[i].shopId == shopId)
                {
                    townHub?.OpenService(shops[i]);
                    return;
                }
            }
        }
    }
}
