using UnityEngine;

namespace FrontierDepths.World
{
    [CreateAssetMenu(menuName = "FrontierDepths/World/Theme Kit")]
    public sealed class ThemeKitDefinition : ScriptableObject
    {
        public string themeId = "theme.frontier_town";
        public string displayName = "Frontier Town";
        public Color keyColor = new Color(0.63f, 0.44f, 0.25f);
        public Color accentColor = new Color(0.95f, 0.75f, 0.34f);
    }
}
