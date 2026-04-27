using System;
using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.Progression
{
    public sealed class TownServicePanelController : MonoBehaviour
    {
        public const string RootName = "RuntimeTownServiceUI";

        private readonly System.Collections.Generic.List<Button> offerButtons = new System.Collections.Generic.List<Button>();
        private RectTransform panel;
        private Text titleText;
        private Text goldText;
        private Text resultText;
        private ShopDefinition shop;
        private Action<int> selectOffer;

        public static TownServicePanelController Instance { get; private set; }
        public static bool IsAnyVisible => Instance != null && Instance.IsVisible;
        public bool IsVisible => panel != null && panel.gameObject.activeSelf;

        private void Awake()
        {
            Instance = this;
            EnsureUi();
            Hide();
        }

        public static TownServicePanelController GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("RuntimeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
            }

            Transform existing = canvas.transform.Find(RootName);
            if (existing != null && existing.TryGetComponent(out TownServicePanelController existingController))
            {
                Instance = existingController;
                return Instance;
            }

            GameObject root = new GameObject(RootName, typeof(RectTransform), typeof(TownServicePanelController));
            root.transform.SetParent(canvas.transform, false);
            Instance = root.GetComponent<TownServicePanelController>();
            return Instance;
        }

        public void Show(ShopDefinition definition, Action<int> onSelect)
        {
            EnsureUi();
            shop = definition;
            selectOffer = onSelect;
            panel.gameObject.SetActive(definition != null);
            Refresh();
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }

            shop = null;
            selectOffer = null;
        }

        public void Refresh(string message = null)
        {
            EnsureUi();
            if (shop == null)
            {
                return;
            }

            ProfileService profileService = GameBootstrap.Instance != null ? GameBootstrap.Instance.ProfileService : null;
            ProfileState profile = profileService != null ? profileService.Current : new ProfileState();
            titleText.text = shop.displayName;
            goldText.text = $"Gold: {profile.gold} | Sigils: {profile.townSigils} | Skill Pts: {profile.skillPoints}";
            resultText.text = string.IsNullOrWhiteSpace(message) ? shop.greeting : message;

            for (int i = 0; i < offerButtons.Count; i++)
            {
                Destroy(offerButtons[i].gameObject);
            }

            offerButtons.Clear();
            for (int i = 0; i < shop.offers.Length; i++)
            {
                int offerIndex = i;
                ShopOffer offer = shop.offers[i];
                string label = $"{i + 1}. {offer.displayName} - {offer.cost}g\n{BuildOfferDescription(profile, offer)}";
                Button button = CreateButton(panel, $"Offer_{i}", label);
                bool soldOut = offer.purchaseLimit > 0 && profileService != null && profileService.GetPurchaseCount(shop.shopId, offer.offerId) >= offer.purchaseLimit;
                button.interactable = profile.gold >= offer.cost && !soldOut && CanUseOffer(profile, offer);
                button.onClick.AddListener(() => selectOffer?.Invoke(offerIndex));
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(520f, 58f);
                rect.anchoredPosition = new Vector2(0f, -136f - i * 66f);
                offerButtons.Add(button);
            }
        }

        private void EnsureUi()
        {
            if (panel != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panel = GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(620f, 520f);
            panel.anchoredPosition = Vector2.zero;
            Image image = gameObject.AddComponent<Image>();
            image.color = UiTheme.Panel;

            titleText = CreateText(panel, "Title", font, 30, TextAnchor.MiddleCenter, new Vector2(0f, -28f), new Vector2(560f, 42f));
            titleText.color = UiTheme.Accent;
            goldText = CreateText(panel, "Gold", font, 18, TextAnchor.MiddleCenter, new Vector2(0f, -70f), new Vector2(560f, 30f));
            resultText = CreateText(panel, "Result", font, 16, TextAnchor.UpperCenter, new Vector2(0f, -96f), new Vector2(560f, 42f));

            Button closeButton = CreateButton(panel, "Close", "Close");
            closeButton.onClick.AddListener(() => FindAnyObjectByType<TownHubController>()?.CloseServiceFromInput());
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(180f, 42f);
            closeRect.anchoredPosition = new Vector2(0f, 24f);
        }

        private static Text CreateText(Transform parent, string name, Font font, int size, TextAnchor alignment, Vector2 position, Vector2 sizeDelta)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = UiTheme.Text;
            text.raycastTarget = false;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = position;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = UiTheme.Button;
            Button button = buttonObject.GetComponent<Button>();

            Text text = CreateText(buttonObject.transform, "Label", font, 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(500f, 54f));
            text.text = label;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            return button;
        }

        private static string BuildOfferDescription(ProfileState profile, ShopOffer offer)
        {
            if (offer.action != ShopOfferAction.AcceptBounty && offer.action != ShopOfferAction.TurnInBounty)
            {
                return offer.description;
            }

            BountyDefinition bounty = BountyCatalog.Get(offer.rewardId);
            BountyRuntimeState state = profile != null ? BountyObjectiveTracker.GetOrCreate(profile, offer.rewardId) : null;
            string stateLabel = state != null ? state.state.ToString() : "Available";
            return bounty == null
                ? $"{offer.description}\nState: {stateLabel}"
                : $"Target: {bounty.targetName} | Floor {bounty.minFloor}-{bounty.maxFloor} | State: {stateLabel}\n{bounty.reason}\nReward: {bounty.goldReward}g, {bounty.xpReward} XP";
        }

        private static bool CanUseOffer(ProfileState profile, ShopOffer offer)
        {
            if (offer.action == ShopOfferAction.AcceptBounty)
            {
                return BountyObjectiveTracker.CanAccept(profile, offer.rewardId, out _);
            }

            if (offer.action == ShopOfferAction.TurnInBounty)
            {
                BountyRuntimeState state = profile != null ? BountyObjectiveTracker.GetOrCreate(profile, offer.rewardId) : null;
                return state != null && state.state == BountyState.Killed;
            }

            return true;
        }
    }
}
