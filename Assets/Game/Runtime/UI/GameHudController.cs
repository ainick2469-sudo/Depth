using FrontierDepths.Core;
using FrontierDepths.Progression;
using FrontierDepths.World;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class GameHudController : MonoBehaviour
    {
        [SerializeField] private Text promptText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text panelText;
        [SerializeField] private Image panelBackground;

        private PlayerInteractor interactor;
        private TownHubController townHub;
        private DungeonSceneController dungeonScene;

        private void Update()
        {
            interactor ??= FindFirstObjectByType<PlayerInteractor>();
            townHub ??= FindFirstObjectByType<TownHubController>();
            dungeonScene ??= FindFirstObjectByType<DungeonSceneController>();

            promptText.text = interactor != null ? interactor.PromptText : string.Empty;

            if (townHub != null)
            {
                statusText.text = townHub.GetStatusLine();
                bool panelOpen = townHub.IsPanelOpen;
                panelBackground.enabled = panelOpen;
                panelText.enabled = panelOpen;
                panelText.text = panelOpen ? townHub.BuildPanelText() : string.Empty;
                if (panelOpen && Input.GetKeyDown(KeyCode.Escape))
                {
                    townHub.CloseService();
                }

                return;
            }

            if (dungeonScene != null)
            {
                statusText.text = string.Empty;
            }
            else
            {
                statusText.text = string.Empty;
            }

            panelBackground.enabled = false;
            panelText.enabled = false;
            panelText.text = string.Empty;
        }
    }
}
