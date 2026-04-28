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
        private HudBarView healthBar;
        private HudBarView manaBar;
        private HudBarView staminaBar;
        private PlayerHealth playerHealth;
        private PlayerResourceController resources;
        private float nextResolveTime;
        private float nextTextRefreshTime;

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
                manaBar?.Set("MANA", resources.CurrentMana, resources.MaxMana);
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
            if (resourceText != null && healthBar != null && manaBar != null && staminaBar != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            healthBar ??= new HudBarView(transform, "HudHealthBar", font, new Color(0.85f, 0.12f, 0.08f, 0.94f), new Vector2(28f, 154f));
            manaBar ??= new HudBarView(transform, "HudManaBar", font, new Color(0.18f, 0.42f, 0.98f, 0.94f), new Vector2(28f, 128f));
            staminaBar ??= new HudBarView(transform, "HudStaminaBar", font, new Color(0.95f, 0.73f, 0.24f, 0.94f), new Vector2(28f, 102f));
            GameObject textObject = new GameObject("HudResources", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(transform, false);
            resourceText = textObject.GetComponent<Text>();
            resourceText.font = font;
            resourceText.fontSize = 18;
            resourceText.alignment = TextAnchor.LowerLeft;
            resourceText.color = UiTheme.Text;
            resourceText.raycastTarget = false;
            resourceText.horizontalOverflow = HorizontalWrapMode.Overflow;
            resourceText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = resourceText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(280f, 88f);
            rect.anchoredPosition = new Vector2(28f, 24f);

            GameObject statusObject = new GameObject("HudResourceStatus", typeof(RectTransform), typeof(Text));
            statusObject.transform.SetParent(transform, false);
            statusText = statusObject.GetComponent<Text>();
            statusText.font = font;
            statusText.fontSize = 15;
            statusText.alignment = TextAnchor.LowerLeft;
            statusText.color = new Color(1f, 0.82f, 0.42f, 0.94f);
            statusText.raycastTarget = false;
            statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
            statusText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = statusRect.anchorMax = new Vector2(0f, 0f);
            statusRect.pivot = new Vector2(0f, 0f);
            statusRect.sizeDelta = new Vector2(420f, 28f);
            statusRect.anchoredPosition = new Vector2(28f, 188f);
            statusText.enabled = false;
        }
    }
}
