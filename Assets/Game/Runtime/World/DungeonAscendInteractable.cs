using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class DungeonAscendInteractable : MonoBehaviour, IInteractable
    {
        public string DisplayName => GameBootstrap.Instance.RunService.Current.floorIndex > 1 ? "Upper Stairs" : "Surface Lift";

        public string Prompt => GameBootstrap.Instance.RunService.Current.floorIndex > 1
            ? $"Climb to floor {GameBootstrap.Instance.RunService.Current.floorIndex - 1}"
            : "Climb back to town";

        public bool CanInteract(PlayerInteractor interactor, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (GameBootstrap.Instance.RunService.Current.floorIndex > 1)
            {
                GameBootstrap.Instance.RunService.AscendToPreviousFloor();
                GameBootstrap.Instance.SceneFlowService.ReloadCurrentScene();
                return;
            }

            GameBootstrap.Instance.SceneFlowService.LoadScene(GameSceneId.TownHub);
        }
    }
}
