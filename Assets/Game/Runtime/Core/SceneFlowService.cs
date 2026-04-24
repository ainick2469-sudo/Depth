using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrontierDepths.Core
{
    public sealed class SceneFlowService
    {
        private readonly GameBootstrap host;

        public SceneFlowService(GameBootstrap host)
        {
            this.host = host;
        }

        public void LoadScene(GameSceneId sceneId)
        {
            host.StartManagedCoroutine(LoadSceneRoutine(GameSceneCatalog.GetName(sceneId)));
        }

        public void ReloadCurrentScene()
        {
            host.StartManagedCoroutine(LoadSceneRoutine(SceneManager.GetActiveScene().name));
        }

        private static IEnumerator LoadSceneRoutine(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!operation.isDone)
            {
                yield return null;
            }
        }
    }
}
