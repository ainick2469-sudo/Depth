using UnityEngine;

namespace FrontierDepths.World
{
    [CreateAssetMenu(menuName = "FrontierDepths/World/Chapter")]
    public sealed class ChapterDefinition : ScriptableObject
    {
        public string chapterId = "chapter.frontier_descent";
        public string displayName = "Frontier Descent";
        public int startFloor = 1;
        public int endFloor = 20;
        [TextArea] public string macroModifier = "The deeper you linger, the more the underworld wakes.";
    }
}
