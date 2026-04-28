using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class HudBarView
    {
        private readonly RectTransform rootRect;
        private readonly Image fill;
        private readonly Text label;
        private readonly float width;

        public RectTransform RootRect => rootRect;
        public string CurrentLabel => label != null ? label.text : string.Empty;

        public HudBarView(Transform parent, string name, Font font, Color fillColor, Vector2 anchoredPosition, float width = 230f)
        {
            this.width = width;
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0f, 0f);
            rootRect.sizeDelta = new Vector2(width, 20f);
            rootRect.anchoredPosition = anchoredPosition;
            Image background = root.GetComponent<Image>();
            background.color = new Color(0.02f, 0.025f, 0.03f, 0.72f);
            background.raycastTarget = false;

            GameObject fillObject = new GameObject($"{name}Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(root.transform, false);
            fill = fillObject.GetComponent<Image>();
            fill.color = fillColor;
            fill.raycastTarget = false;
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = new Vector2(width, 16f);
            fillRect.anchoredPosition = new Vector2(0f, 0f);

            GameObject labelObject = new GameObject($"{name}Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(root.transform, false);
            label = labelObject.GetComponent<Text>();
            label.font = font;
            label.fontSize = 13;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        public void Set(string title, float current, float max)
        {
            max = Mathf.Max(1f, max);
            current = Mathf.Clamp(current, 0f, max);
            fill.rectTransform.sizeDelta = new Vector2(width * Mathf.Clamp01(current / max), 16f);
            label.text = $"{title} {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
    }
}
