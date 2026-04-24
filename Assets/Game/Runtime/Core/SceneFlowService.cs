using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrontierDepths.Core
{
    public enum TownHubLoadReason
    {
        Default,
        DungeonEntranceReturn,
        DungeonPortalReturn
    }

    public sealed class SceneFlowService
    {
        private readonly GameBootstrap host;
        private TownHubLoadReason pendingTownHubLoadReason = TownHubLoadReason.Default;

        public SceneFlowService(GameBootstrap host)
        {
            this.host = host;
        }

        public void LoadScene(GameSceneId sceneId)
        {
            Time.timeScale = 1f;
            host.StartManagedCoroutine(LoadSceneRoutine(GameSceneCatalog.GetName(sceneId)));
        }

        public void ReloadCurrentScene()
        {
            Time.timeScale = 1f;
            host.StartManagedCoroutine(LoadSceneRoutine(SceneManager.GetActiveScene().name));
        }

        public void SetPendingTownHubLoadReason(TownHubLoadReason reason)
        {
            pendingTownHubLoadReason = reason;
        }

        public TownHubLoadReason ConsumePendingTownHubLoadReason()
        {
            TownHubLoadReason reason = pendingTownHubLoadReason;
            pendingTownHubLoadReason = TownHubLoadReason.Default;
            return reason;
        }

        private static IEnumerator LoadSceneRoutine(string sceneName)
        {
            Time.timeScale = 1f;
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!operation.isDone)
            {
                yield return null;
            }
        }
    }
}
