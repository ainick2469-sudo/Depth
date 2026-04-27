using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrontierDepths.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        public SaveService SaveService { get; private set; }
        public ProfileService ProfileService { get; private set; }
        public InventoryService InventoryService { get; private set; }
        public RunService RunService { get; private set; }
        public SceneFlowService SceneFlowService { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureExists()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject bootstrap = new GameObject(nameof(GameBootstrap));
            bootstrap.AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SaveService ??= new SaveService();
            ProfileService ??= new ProfileService(SaveService);
            InventoryService ??= new InventoryService(ProfileService);
            RunService ??= new RunService(SaveService, ProfileService);
            SceneFlowService ??= new SceneFlowService(this);
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == GameSceneCatalog.Bootstrap)
            {
                SceneFlowService.LoadScene(GameSceneId.MainMenu);
            }
        }

        public Coroutine StartManagedCoroutine(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }
    }
}
