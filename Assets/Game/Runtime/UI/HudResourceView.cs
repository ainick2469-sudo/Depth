using FrontierDepths.Core;
using FrontierDepths.Combat;
using FrontierDepths.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class HudResourceView : MonoBehaviour
    {
        private Text resourceText;
        private Text statusText;
        private RectTransform resourcePanelRoot;
        private Image resourcePanelBackground;
        private HudBarView healthBar;
        private HudBarView focusBar;
        private HudBarView staminaBar;
        private PlayerHealth playerHealth;
        private PlayerResourceController resources;
        private float nextResolveTime;
        private float nextTextRefreshTime;

        internal int ResourceBarCountForTests => (healthBar != null ? 1 : 0) + (focusBar != null ? 1 : 0) + (staminaBar != null ? 1 : 0);
        internal string ResourceTextForTests => resourceText != null ? resourceText.text : string.Empty;
        internal bool HasResourcePanelForTests => resourcePanelRoot != null && resourcePanelBackground != null;

        private void Awake()
        {
            EnsureUi();
        }

        private void Update()
        {
            EnsureUi();
            ResolveRuntimeObjects();
            ProfileState profile = GameBootstrap.Instance?.ProfileService?.Current;
            if (healthBar != null && playerHealth != null)
            {
                healthBar.Set("HP", playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (resources != null)
            {
                focusBar?.Set("FOCUS", resources.CurrentFocus, resources.MaxFocus);
                staminaBar?.Set("STAM", resources.CurrentStamina, resources.MaxStamina);
                if (statusText != null)
                {
                    statusText.text = resources.StatusMessage;
                    statusText.enabled = !string.IsNullOrWhiteSpace(statusText.text);
                }
            }
            else if (statusText != null)
            {
                statusText.enabled = false;
            }

            if (resourceText == null || profile == null || Time.unscaledTime < nextTextRefreshTime)
            {
                return;
            }

            nextTextRefreshTime = Time.unscaledTime + 0.15f;
            resourceText.text =
                $"Gold {profile.gold}\n" +
                $"Rep {ReputationService.GetTitle(profile.townReputation)} ({profile.townReputation})\n" +
                $"Skill Pts {profile.skillPoints}";
        }

        private void ResolveRuntimeObjects()
        {
            if (Time.unscaledTime < nextResolveTime && playerHealth != null && resources != null)
            {
                return;
            }

            nextResolveTime = Time.unscaledTime + 0.5f;
            playerHealth ??= FindAnyObjectByType<PlayerHealth>();
            resources ??= FindAnyObjectByType<PlayerResourceController>();
        }

        private void EnsureUi()
        {
            if (resourcePanelRoot != null && resourceText != null && healthBar != null && focusBar != null && staminaBar != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Transform panelParent = HudLayoutConstants.GetZoneOrRoot(transform, HudLayoutConstants.BottomLeftZoneName);
            if (resourcePanelRoot == null)
            {
                Transform existing = FindNamedTransform(transform, "HudResourcePanel");
                if (existing != null)
                {
                    resourcePanelRoot = existing.GetComponent<RectTransform>();
                    resourcePanelBackground = existing.GetComponent<Image>();
                }
            }

            if (resourcePanelRoot == null)
            {
                GameObject panelObject = new GameObject("HudResourcePanel", typeof(RectTransform), typeof(Image));
                panelObject.transform.SetParent(panelParent != null ? panelParent : transform, false);
                resourcePanelRoot = panelObject.GetComponent<RectTransform>();
                resourcePanelBackground = panelObject.GetComponent<Image>();
            }

            resourcePanelRoot.anchorMin = resourcePanelRoot.anchorMax = new Vector2(0f, 0f);
            resourcePanelRoot.pivot = new Vector2(0f, 0f);
            resourcePanelRoot.sizeDelta = new Vector2(HudLayoutConstants.ResourcePanelWidth + 24f, 184f);
            resourcePanelRoot.anchoredPosition = new Vector2(HudLayoutConstants.HudMargin, HudLayoutConstants.HudMargin);
            if (resourcePanelBackground != null)
            {
                resourcePanelBackground.color = new Color(0.018f, 0.018f, 0.02f, 0.68f);
                resourcePanelBackground.raycastTarget = false;
            }

            healthBar ??= new HudBarView(resourcePanelRoot, "HudHealthBar", font, new Color(0.85f, 0.12f, 0.08f, 0.94f), new Vector2(12f, 132f), HudLayoutConstants.ResourcePanelWidth);
            focusBar ??= new HudBarView(resourcePanelRoot, "HudFocusBar", font, new Color(0.18f, 0.42f, 0.98f, 0.94f), new Vector2(12f, 106f), HudLayoutConstants.ResourcePanelWidth);
            staminaBar ??= new HudBarView(resourcePanelRoot, "HudStaminaBar", font, new Color(0.95f, 0.73f, 0.24f, 0.94f), new Vector2(12f, 80f), HudLayoutConstants.ResourcePanelWidth);
            GameObject textObject = new GameObject("HudResources", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(resourcePanelRoot, false);
            resourceText = textObject.GetComponent<Text>();
            resourceText.font = font;
            resourceText.fontSize = 16;
            resourceText.alignment = TextAnchor.LowerLeft;
            resourceText.color = UiTheme.Text;
            resourceText.raycastTarget = false;
            resourceText.horizontalOverflow = HorizontalWrapMode.Overflow;
            resourceText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = resourceText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(HudLayoutConstants.ResourcePanelWidth, 70f);
            rect.anchoredPosition = new Vector2(12f, 10f);

            GameObject statusObject = new GameObject("HudResourceStatus", typeof(RectTransform), typeof(Text));
            statusObject.transform.SetParent(resourcePanelRoot, false);
            statusText = statusObject.GetComponent<Text>();
            statusText.font = font;
            statusText.fontSize = 14;
            statusText.alignment = TextAnchor.UpperLeft;
            statusText.color = new Color(1f, 0.82f, 0.42f, 0.94f);
            statusText.raycastTarget = false;
            statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
            statusText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = statusRect.anchorMax = new Vector2(0f, 1f);
            statusRect.pivot = new Vector2(0f, 1f);
            statusRect.sizeDelta = new Vector2(HudLayoutConstants.ResourcePanelWidth, 24f);
            statusRect.anchoredPosition = new Vector2(12f, -8f);
            statusText.enabled = false;
        }

        private static Transform FindNamedTransform(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindNamedTransform(root.GetChild(i), objectName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
