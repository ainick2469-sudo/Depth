using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text hintText;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Refresh();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                StartNewGame();
            }
            else if (Input.GetKeyDown(KeyCode.L))
            {
                LoadGame();
            }
        }

        public void StartNewGame()
        {
            GameBootstrap.Instance.ProfileService.ResetProgress();
            GameBootstrap.Instance.RunService.EndRun();
            GameBootstrap.Instance.SceneFlowService.LoadScene(GameSceneId.TownHub);
        }

        public void LoadGame()
        {
            GameBootstrap.Instance.SceneFlowService.LoadScene(GameSceneId.TownHub);
        }

        private void Refresh()
        {
            ProfileState profile = GameBootstrap.Instance.ProfileService.Current;
            bool canLoad =
                GameBootstrap.Instance.RunService.HasActiveRun ||
                profile.gold != 350 ||
                profile.townSigils > 0 ||
                profile.curioDust > 0 ||
                !string.IsNullOrWhiteSpace(profile.storedHeirloomId) ||
                profile.unlockedWeaponIds.Count > 1 ||
                profile.activeBountyIds.Count > 0 ||
                profile.purchaseRecords.Count > 0;

            if (subtitleText != null)
            {
                subtitleText.text = canLoad
                    ? "Pick up where you left off or wipe the slate clean."
                    : "Start a fresh descent into the frontier underworld.";
            }

            if (hintText != null)
            {
                hintText.text = "N = New Game    L = Load Game";
            }

            if (loadGameButton != null)
            {
                loadGameButton.interactable = canLoad;
            }
        }
    }
}
