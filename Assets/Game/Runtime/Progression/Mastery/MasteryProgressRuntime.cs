using UnityEngine;

namespace FrontierDepths.Progression.Mastery
{
    public sealed class MasteryProgressRuntime : MonoBehaviour
    {
        private static MasteryProgressRuntime instance;

        [SerializeField] private bool showDebugOverlay;
        [SerializeField] private bool debugLogProgress = true;

        private MasteryProgressService service;

        public static MasteryProgressService Service => instance != null ? instance.service : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureExists()
        {
            if (instance != null)
            {
                return;
            }

            GameObject runtime = new GameObject(nameof(MasteryProgressRuntime));
            runtime.AddComponent<MasteryProgressRuntime>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            service = new MasteryProgressService(MasteryTrackerCatalog.CreateStarterTrackers(), debugLogProgress: debugLogProgress);
            service.StartListening();
        }

        private void OnDestroy()
        {
            service?.StopListening();
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11))
            {
                showDebugOverlay = !showDebugOverlay;
            }
        }

        private void OnGUI()
        {
            if (!showDebugOverlay || service == null)
            {
                return;
            }

            GUI.Box(new Rect(12f, 92f, 680f, 28f), service.GetDebugSummary());
        }
    }
}
