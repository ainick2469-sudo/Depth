using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrontierDepths.Combat
{
    public sealed class PlayerDeathReturnController : MonoBehaviour
    {
        [SerializeField] private KeyCode returnToTownKey = KeyCode.R;

        private PlayerHealth health;
        private FirstPersonController playerController;
        private bool deathFlowActive;
        private bool returnStarted;

        public bool IsDeathFlowActive => deathFlowActive;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (health != null)
            {
                health.Died += HandlePlayerDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandlePlayerDied;
            }
        }

        private void Update()
        {
            if (!deathFlowActive || returnStarted || !Input.GetKeyDown(returnToTownKey))
            {
                return;
            }

            ReturnToTownAfterDeath();
        }

        internal bool ReturnToTownAfterDeath()
        {
            if (returnStarted)
            {
                return false;
            }

            returnStarted = true;
            Time.timeScale = 1f;

            try
            {
                GameBootstrap bootstrap = GameBootstrap.Instance;
                if (bootstrap != null && bootstrap.RunService != null && bootstrap.SceneFlowService != null)
                {
                    bootstrap.RunService.SaveActiveFloorState();
                    bootstrap.RunService.EndRun();
                    bootstrap.SceneFlowService.SetPendingTownHubLoadReason(TownHubLoadReason.DungeonEntranceReturn);
                    bootstrap.SceneFlowService.LoadScene(GameSceneId.TownHub);
                    return true;
                }
            }
            catch
            {
                // The fallback below keeps death from trapping the player in DungeonRuntime.
            }

            SceneManager.LoadScene(GameSceneCatalog.TownHub, LoadSceneMode.Single);
            return true;
        }

        private void HandlePlayerDied(PlayerHealth playerHealth)
        {
            deathFlowActive = true;
            if (playerController != null)
            {
                playerController.SetUiCaptured(true);
            }
        }

        private void ResolveReferences()
        {
            health ??= GetComponent<PlayerHealth>();
            playerController ??= GetComponent<FirstPersonController>();
        }
    }
}
