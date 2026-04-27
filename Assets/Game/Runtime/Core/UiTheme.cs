using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.Core
{
    public static class UiTheme
    {
        public static readonly Color Panel = new Color(0.035f, 0.03f, 0.025f, 0.95f);
        public static readonly Color PanelAlt = new Color(0.06f, 0.048f, 0.036f, 0.96f);
        public static readonly Color Button = new Color(0.18f, 0.13f, 0.09f, 0.96f);
        public static readonly Color ButtonDisabled = new Color(0.12f, 0.11f, 0.1f, 0.72f);
        public static readonly Color Text = new Color(0.96f, 0.94f, 0.86f, 1f);
        public static readonly Color MutedText = new Color(0.72f, 0.69f, 0.62f, 1f);
        public static readonly Color Accent = new Color(0.94f, 0.64f, 0.28f, 1f);
        public static readonly Color Danger = new Color(0.95f, 0.28f, 0.22f, 1f);

        public const int TitleSize = 32;
        public const int HeaderSize = 22;
        public const int BodySize = 16;
        public const int SmallSize = 13;

        public static Font RuntimeFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static void StyleText(Text text, int size, TextAnchor alignment, Color? color = null)
        {
            if (text == null)
            {
                return;
            }

            text.font = RuntimeFont;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color ?? Text;
            text.raycastTarget = false;
        }

        public static void StyleButton(Button button, bool interactable = true)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = interactable;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = interactable ? Button : ButtonDisabled;
            }
        }
    }
}
