using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class WorldFloorDefinition
    {
        public int floorNumber;
        public string floorName = string.Empty;
        public string biomeTheme = string.Empty;
        public int dangerTier;
        public bool hasMajorTown;
        public bool hasMinorCamp;
        public bool hasLabyrinth;
        public string bossId = string.Empty;
        public string bossName = string.Empty;
        public string labyrinthId = string.Empty;
        public string labyrinthName = string.Empty;
        public string majorSettlementId = string.Empty;
        public string majorSettlementName = string.Empty;
        public string minorCampId = string.Empty;
        public string minorCampName = string.Empty;
        public string[] fieldEnemyPool = System.Array.Empty<string>();
        public string[] labyrinthEnemyPool = System.Array.Empty<string>();
        public string[] specialRoomPool = System.Array.Empty<string>();
        public Vector2Int worldSize;
        public int townCount;
        public int landmarkCount;
        public string roadStyle = string.Empty;
        public string weatherProfile = string.Empty;
        public string musicProfile = string.Empty;
        public string visualPalette = string.Empty;

        public string PrimarySettlementId =>
            !string.IsNullOrWhiteSpace(majorSettlementId) ? majorSettlementId : minorCampId;

        public string PrimarySettlementName =>
            !string.IsNullOrWhiteSpace(majorSettlementName) ? majorSettlementName : minorCampName;
    }
}
