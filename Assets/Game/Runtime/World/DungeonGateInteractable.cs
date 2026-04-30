using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class DungeonGateInteractable : MonoBehaviour, IInteractable
    {
        public string DisplayName => "Dungeon Gate";
        public string Prompt => "Press E to enter Training Labyrinth";

        public bool CanInteract(PlayerInteractor interactor, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!GameBootstrap.Instance.RunService.HasActiveRun)
            {
                GameBootstrap.Instance.RunService.StartNewRun();
            }

            GameBootstrap.Instance.SceneFlowService.LoadScene(GameSceneId.DungeonRuntime);
        }
    }
}
