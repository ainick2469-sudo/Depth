using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class DashHudView : MonoBehaviour
    {
        private Text dashText;
        private FirstPersonController player;

        private void Awake()
        {
            EnsureUi();
        }

        private void Update()
        {
            EnsureUi();
            player ??= FindAnyObjectByType<FirstPersonController>();
            if (dashText == null || player == null)
            {
                if (dashText != null)
                {
                    dashText.enabled = false;
                }
                return;
            }

            dashText.enabled = true;
            float remaining = player.DashCooldownRemaining;
            dashText.text = remaining <= 0.01f
                ? $"{InputBindingService.GetDisplay(GameplayInputAction.Dash)} Dash Ready"
                : $"Dash {remaining:0.0}s";
            dashText.color = remaining <= 0.01f ? UiTheme.Accent : new Color(1f, 0.78f, 0.42f, 0.95f);
        }

        private void EnsureUi()
        {
            if (dashText != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject textObject = new GameObject("DashCooldown", typeof(RectTransform), typeof(Text));
            Transform parent = HudLayoutConstants.GetZoneOrRoot(transform, HudLayoutConstants.BottomCenterZoneName);
            textObject.transform.SetParent(parent != null ? parent : transform, false);
            dashText = textObject.GetComponent<Text>();
            dashText.font = font;
            dashText.fontSize = 18;
            dashText.alignment = TextAnchor.LowerCenter;
            dashText.raycastTarget = false;
            RectTransform rect = dashText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(320f, 28f);
            rect.anchoredPosition = new Vector2(0f, 54f);
        }
    }
}
