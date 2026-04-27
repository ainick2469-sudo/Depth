using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string RuntimePanelName = "RuntimeMainMenuPanel";

        [SerializeField] private Text subtitleText;
        [SerializeField] private Text hintText;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;

        private RectTransform panel;
        private RectTransform settingsPanel;
        private RectTransform invitePanel;
        private Text runtimeSubtitle;
        private Text runtimeHint;
        private SharedSettingsPanelController sharedSettingsPanel;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            EnsureMenuCamera();
            using (LoadTimingLogger.Measure("Main menu UI build"))
            {
                EnsureRuntimeUi();
            }
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
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                HidePanels();
            }
        }

        public void StartNewGame()
        {
            GameBootstrap.Instance.ProfileService.ResetProgress();
            GameBootstrap.Instance.RunService.EndRun();
            GameBootstrap.Instance.SceneFlowService.SetPendingTownHubLoadReason(TownHubLoadReason.Default);
            GameBootstrap.Instance.SceneFlowService.LoadScene(GameSceneId.TownHub);
        }

        public void LoadGame()
        {
            GameBootstrap.Instance.SceneFlowService.SetPendingTownHubLoadReason(TownHubLoadReason.Default);
            GameBootstrap.Instance.SceneFlowService.LoadScene(GameSceneId.TownHub);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("Quit requested from Main Menu. Application.Quit is ignored in the editor.");
#else
            Application.Quit();
#endif
        }

        private void EnsureRuntimeUi()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("RuntimeMainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
            }

            Transform existing = canvas.transform.Find(RuntimePanelName);
            if (existing != null)
            {
                panel = existing.GetComponent<RectTransform>();
                return;
            }

            GameObject panelObject = new GameObject(RuntimePanelName, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvas.transform, false);
            panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            panelObject.GetComponent<Image>().color = new Color(0.025f, 0.022f, 0.02f, 1f);

            CreateBackdrop(panel);
            CreateLabel(panel, "Title", "FRONTIER DEPTHS", 54, new Vector2(-310f, -92f), new Vector2(560f, 70f), TextAnchor.MiddleLeft, UiTheme.Text);
            runtimeSubtitle = CreateLabel(panel, "Subtitle", string.Empty, 20, new Vector2(-310f, -158f), new Vector2(560f, 58f), TextAnchor.UpperLeft, UiTheme.MutedText);
            runtimeHint = CreateLabel(panel, "Hint", string.Empty, 14, new Vector2(-310f, -620f), new Vector2(560f, 30f), TextAnchor.MiddleLeft, UiTheme.MutedText);

            RectTransform menuCard = CreateCard(panel, "MenuCard", new Vector2(340f, 0f), new Vector2(360f, 420f));
            newGameButton = CreateButton(menuCard, "NewGame", "New Game", new Vector2(0f, -62f), StartNewGame);
            loadGameButton = CreateButton(menuCard, "LoadGame", "Load Game", new Vector2(0f, -118f), LoadGame);
            CreateButton(menuCard, "Settings", "Settings", new Vector2(0f, -174f), ToggleSettings);
            CreateButton(menuCard, "Invite", "Invite Friends", new Vector2(0f, -230f), ToggleInvite);
            CreateButton(menuCard, "Quit", "Leave Game", new Vector2(0f, -286f), QuitGame);

            settingsPanel = CreateCard(panel, "SettingsPanel", new Vector2(0f, 0f), new Vector2(590f, 660f));
            sharedSettingsPanel = settingsPanel.gameObject.AddComponent<SharedSettingsPanelController>();
            sharedSettingsPanel.Build("SETTINGS", message =>
            {
                if (runtimeHint != null)
                {
                    runtimeHint.text = message;
                }
            }, null, HidePanels);
            settingsPanel.gameObject.SetActive(false);

            invitePanel = CreateCard(panel, "InvitePanel", new Vector2(0f, 0f), new Vector2(430f, 270f));
            CreateLabel(invitePanel, "InviteTitle", "INVITE FRIENDS", 24, new Vector2(0f, -28f), new Vector2(360f, 34f), TextAnchor.MiddleCenter, UiTheme.Accent);
            CreateLabel(invitePanel, "InviteBody", "Multiplayer invites are coming later.\nFor now, Frontier Depths is a solo descent.", 17, new Vector2(0f, -86f), new Vector2(360f, 100f), TextAnchor.MiddleCenter, UiTheme.Text);
            CreateButton(invitePanel, "CloseInvite", "Close", new Vector2(0f, -196f), HidePanels, 180f);
            invitePanel.gameObject.SetActive(false);
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

            string subtitle = canLoad
                ? "Pick up where you left off, or wipe the slate clean."
                : "Start a fresh descent into the frontier underworld.";

            if (subtitleText != null) subtitleText.text = subtitle;
            if (runtimeSubtitle != null) runtimeSubtitle.text = subtitle;
            if (hintText != null) hintText.text = "N = New Game    L = Load Game";
            if (runtimeHint != null) runtimeHint.text = "N = New Game    L = Load Game    Esc = Close Panel";
            if (loadGameButton != null) loadGameButton.interactable = canLoad;
            RefreshSettingsText();
        }

        private static void EnsureMenuCamera()
        {
            if (Camera.main != null || FindAnyObjectByType<Camera>() != null)
            {
                return;
            }

            GameObject cameraObject = new GameObject("MainMenuRuntimeCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.022f, 0.02f, 1f);
            camera.transform.position = new Vector3(0f, 2f, -10f);
            camera.transform.rotation = Quaternion.identity;
        }

        private void ToggleSettings()
        {
            settingsPanel.gameObject.SetActive(!settingsPanel.gameObject.activeSelf);
            invitePanel.gameObject.SetActive(false);
            RefreshSettingsText();
        }

        private void ToggleInvite()
        {
            invitePanel.gameObject.SetActive(!invitePanel.gameObject.activeSelf);
            settingsPanel.gameObject.SetActive(false);
        }

        private void HidePanels()
        {
            if (settingsPanel != null) settingsPanel.gameObject.SetActive(false);
            if (invitePanel != null) invitePanel.gameObject.SetActive(false);
        }

        private void AdjustSensitivity(float delta)
        {
            GameSettingsState settings = GameSettingsService.Current;
            settings.mouseSensitivity += delta;
            SaveSettings(settings);
        }

        private void AdjustFov(float delta)
        {
            GameSettingsState settings = GameSettingsService.Current;
            settings.fov += delta;
            SaveSettings(settings);
        }

        private void SaveSettings(GameSettingsState settings)
        {
            settings.Clamp();
            GameSettingsService.Save(settings);
            GameSettingsService.ApplyRuntime(settings);
            RefreshSettingsText();
        }

        private void RefreshSettingsText()
        {
            sharedSettingsPanel?.Refresh();
        }

        private static void CreateBackdrop(Transform parent)
        {
            GameObject band = new GameObject("CopperBand", typeof(RectTransform), typeof(Image));
            band.transform.SetParent(parent, false);
            RectTransform rect = band.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(450f, 0f);
            rect.anchoredPosition = Vector2.zero;
            band.GetComponent<Image>().color = new Color(0.16f, 0.095f, 0.045f, 0.38f);
        }

        private static RectTransform CreateCard(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            card.GetComponent<Image>().color = UiTheme.Panel;
            return rect;
        }

        private static Text CreateLabel(Transform parent, string name, string label, int fontSize, Vector2 position, Vector2 size, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            UiTheme.StyleText(text, fontSize, alignment, color);
            text.text = label;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action, float width = 240f)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(action);
            UiTheme.StyleButton(button);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, 42f);
            rect.anchoredPosition = position;
            Text text = CreateLabel(buttonObject.transform, "Label", label, UiTheme.BodySize, Vector2.zero, rect.sizeDelta, TextAnchor.MiddleCenter, UiTheme.Text);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            return button;
        }
    }
}
