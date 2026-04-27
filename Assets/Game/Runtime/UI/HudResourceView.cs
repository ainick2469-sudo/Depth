using FrontierDepths.Core;
using FrontierDepths.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class HudResourceView : MonoBehaviour
    {
        private Text resourceText;

        private void Awake()
        {
            EnsureUi();
        }

        private void Update()
        {
            EnsureUi();
            ProfileState profile = GameBootstrap.Instance?.ProfileService?.Current;
            if (resourceText == null || profile == null)
            {
                return;
            }

            resourceText.text =
                $"Gold {profile.gold}\n" +
                $"Rep {ReputationService.GetTitle(profile.townReputation)} ({profile.townReputation})\n" +
                $"Skill Pts {profile.skillPoints}";
        }

        private void EnsureUi()
        {
            if (resourceText != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            rect.anchoredPosition = new Vector2(28f, 72f);
        }
    }
}
