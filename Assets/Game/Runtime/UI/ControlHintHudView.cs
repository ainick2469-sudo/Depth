using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class ControlHintHudView : MonoBehaviour
    {
        private Text hintText;

        private void Awake()
        {
            EnsureUi();
        }

        private void Update()
        {
            EnsureUi();
            if (hintText == null)
            {
                return;
            }

            bool dungeon = SceneManager.GetActiveScene().name == GameSceneCatalog.DungeonRuntime;
            hintText.text = BuildHintText(dungeon);
        }

        internal static string BuildHintText(bool dungeon)
        {
            return dungeon
                ? $"{InputBindingService.GetDisplay(GameplayInputAction.Fire)} Fire  |  {InputBindingService.GetDisplay(GameplayInputAction.Reload)} Reload  |  {InputBindingService.GetDisplay(GameplayInputAction.Dash)} Dash  |  {InputBindingService.GetDisplay(GameplayInputAction.ToggleFullMap)} Map  |  {InputBindingService.GetDisplay(GameplayInputAction.ManaSense)} Depth Sense  |  {InputBindingService.GetDisplay(GameplayInputAction.Inventory)} Inventory  |  {InputBindingService.GetDisplay(GameplayInputAction.RunInfo)} Stats"
                : $"{InputBindingService.GetDisplay(GameplayInputAction.Interact)} Interact  |  {InputBindingService.GetDisplay(GameplayInputAction.Inventory)} Inventory  |  {InputBindingService.GetDisplay(GameplayInputAction.RunInfo)} Stats  |  {InputBindingService.GetDisplay(GameplayInputAction.Pause)} Pause";
        }

        private void EnsureUi()
        {
            if (hintText != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject textObject = new GameObject("ControlHints", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(transform, false);
            hintText = textObject.GetComponent<Text>();
            hintText.font = font;
            hintText.fontSize = 15;
            hintText.alignment = TextAnchor.LowerCenter;
            hintText.color = new Color(UiTheme.Text.r, UiTheme.Text.g, UiTheme.Text.b, 0.78f);
            hintText.raycastTarget = false;
            hintText.horizontalOverflow = HorizontalWrapMode.Overflow;
            hintText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = hintText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(980f, 28f);
            rect.anchoredPosition = new Vector2(0f, 22f);
        }
    }
}
