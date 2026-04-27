using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class DungeonReturnAnchorInteractable : MonoBehaviour, IInteractable
    {
        public string RoomId { get; set; } = "return";
        public string DisplayName => "Return Anchor";
        public string Prompt => "Spend a Town Sigil to return home";

        public bool CanInteract(PlayerInteractor interactor, out string reason)
        {
            if (GameBootstrap.Instance.ProfileService.Current.townSigils <= 0)
            {
                reason = "Need a Town Sigil to return.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!GameBootstrap.Instance.ProfileService.ConsumeTownSigil())
            {
                return;
            }

            Vector3 anchorPosition = transform.position + Vector3.up * 0.5f;
            GameBootstrap.Instance.RunService.SaveActiveFloorState();
            GameBootstrap.Instance.RunService.SetPortalAnchor(anchorPosition, RoomId);
            GameBootstrap.Instance.SceneFlowService.SetPendingTownHubLoadReason(TownHubLoadReason.DungeonPortalReturn);
            GameBootstrap.Instance.SceneFlowService.LoadScene(GameSceneId.TownHub);
        }
    }
}
