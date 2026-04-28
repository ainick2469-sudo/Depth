using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class CompassHudView : MonoBehaviour
    {
        private Text compassText;
        private Text floorText;
        private Transform player;
        private float nextResolveTime;

        private void Awake()
        {
            EnsureUi();
        }

        private void Update()
        {
            EnsureUi();
            ResolvePlayer();
            if (compassText == null)
            {
                return;
            }

            float yaw = player != null ? player.eulerAngles.y : 0f;
            string heading = DungeonDirectionUtility.GetCardinalLabel(yaw);
            compassText.text = $"W   NW   N   NE   E\n<color=#ffd970>{heading}</color>";

            RunState run = GameBootstrap.Instance?.RunService?.Current;
            if (floorText != null)
            {
                floorText.text = run != null ? $"Floor {Mathf.Max(1, run.floorIndex)} - Frontier Depths" : "Frontier Depths";
            }
        }

        private void ResolvePlayer()
        {
            if (player != null || Time.unscaledTime < nextResolveTime)
            {
                return;
            }

            nextResolveTime = Time.unscaledTime + 0.5f;
            FirstPersonController controller = FindAnyObjectByType<FirstPersonController>();
            player = controller != null ? controller.transform : null;
        }

        private void EnsureUi()
        {
            if (compassText != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Transform parent = HudLayoutConstants.GetZoneOrRoot(transform, HudLayoutConstants.TopCenterZoneName);
            GameObject compassObject = new GameObject("Compass", typeof(RectTransform), typeof(Text));
            compassObject.transform.SetParent(parent != null ? parent : transform, false);
            compassText = compassObject.GetComponent<Text>();
            compassText.font = font;
            compassText.fontSize = 18;
            compassText.alignment = TextAnchor.UpperCenter;
            compassText.color = UiTheme.Text;
            compassText.raycastTarget = false;
            compassText.horizontalOverflow = HorizontalWrapMode.Overflow;
            compassText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform compassRect = compassText.rectTransform;
            compassRect.anchorMin = compassRect.anchorMax = new Vector2(0.5f, 1f);
            compassRect.pivot = new Vector2(0.5f, 1f);
            compassRect.sizeDelta = new Vector2(420f, 56f);
            compassRect.anchoredPosition = new Vector2(0f, -18f);

            GameObject floorObject = new GameObject("FloorLabel", typeof(RectTransform), typeof(Text));
            floorObject.transform.SetParent(parent != null ? parent : transform, false);
            floorText = floorObject.GetComponent<Text>();
            floorText.font = font;
            floorText.fontSize = 15;
            floorText.alignment = TextAnchor.UpperCenter;
            floorText.color = new Color(UiTheme.Text.r, UiTheme.Text.g, UiTheme.Text.b, 0.78f);
            floorText.raycastTarget = false;
            RectTransform floorRect = floorText.rectTransform;
            floorRect.anchorMin = floorRect.anchorMax = new Vector2(0.5f, 1f);
            floorRect.pivot = new Vector2(0.5f, 1f);
            floorRect.sizeDelta = new Vector2(420f, 24f);
            floorRect.anchoredPosition = new Vector2(0f, -74f);
        }
    }
}
