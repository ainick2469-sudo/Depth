using UnityEngine;

namespace FrontierDepths.World
{
    [CreateAssetMenu(menuName = "FrontierDepths/World/Floor Band")]
    public sealed class FloorBandDefinition : ScriptableObject
    {
        public string floorBandId = "floorband.frontier_mine";
        public string displayName = "Frontier Mine";
        public int startFloor = 1;
        public int endFloor = 10;
        public string themeKitId = "theme.frontier_town";
        [TextArea] public string notes = "Dusty mine shafts with wooden supports and lantern pools.";
    }
}
