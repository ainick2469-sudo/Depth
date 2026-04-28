using UnityEngine;

namespace FrontierDepths.UI
{
    public static class HudRuntimeSpriteFactory
    {
        private static Sprite filledCircleSprite;

        public static Sprite GetFilledCircleSprite()
        {
            if (filledCircleSprite != null)
            {
                return filledCircleSprite;
            }

            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeHudFilledCircle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color clear = new Color(1f, 1f, 1f, 0f);
            Color solid = Color.white;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = (size - 2) * 0.5f;
            float feather = 1.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01((radius - distance) / feather);
                    texture.SetPixel(x, y, alpha >= 1f ? solid : new Color(1f, 1f, 1f, alpha));
                    if (alpha <= 0f)
                    {
                        texture.SetPixel(x, y, clear);
                    }
                }
            }

            texture.Apply(false, true);
            filledCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            filledCircleSprite.name = "RuntimeHudFilledCircleSprite";
            return filledCircleSprite;
        }

        internal static void ClearCacheForTests()
        {
            filledCircleSprite = null;
        }
    }
}
