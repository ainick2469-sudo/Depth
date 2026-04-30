using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    public static class DungeonShellVisualCatalog
    {
        public const string FloorPath = "DungeonVisuals/FloorVisual";
        public const string WallPath = "DungeonVisuals/WallVisual";
        public const string DoorwayPath = "DungeonVisuals/DoorwayVisual";
        public const string CorridorPath = "DungeonVisuals/CorridorVisual";
        public const string CornerPath = "DungeonVisuals/CornerVisual";
        public const string PillarPath = "DungeonVisuals/PillarVisual";
        public const string StairsUpPath = "DungeonVisuals/StairsUpVisual";
        public const string StairsDownPath = "DungeonVisuals/StairsDownVisual";
        public const string RoomAccentPath = "DungeonVisuals/RoomAccentVisual";
        public const string SecretAccentPath = "DungeonVisuals/SecretAccentVisual";
        public const string DoorwaySideTrimPath = "DungeonVisuals/WallVisual";
        public const string StairMarkerPath = "DungeonVisuals/FloorVisual";
        public const string RoomPurposeFloorTintPath = "DungeonVisuals/FloorVisual";
        public const string RoomPurposeMarkerPath = "DungeonVisuals/FloorVisual";

        private static readonly DungeonShellVisualDefinition[] Definitions =
        {
            Create(DungeonShellVisualKind.Floor, "Floor", FloorPath, new Color(0.28f, 0.27f, 0.25f), new Vector3(6f, 0.12f, 6f)),
            Create(DungeonShellVisualKind.Wall, "Wall", WallPath, new Color(0.2f, 0.21f, 0.24f), new Vector3(6f, 6f, 0.35f)),
            Create(DungeonShellVisualKind.Doorway, "Doorway", DoorwayPath, new Color(0.25f, 0.25f, 0.28f), new Vector3(8f, 6f, 0.5f)),
            Create(DungeonShellVisualKind.Corridor, "Corridor", CorridorPath, new Color(0.19f, 0.18f, 0.17f), new Vector3(6f, 0.12f, 6f)),
            Create(DungeonShellVisualKind.Corner, "Corner", CornerPath, new Color(0.22f, 0.23f, 0.26f), new Vector3(1.2f, 6f, 1.2f)),
            Create(DungeonShellVisualKind.Pillar, "Pillar", PillarPath, new Color(0.26f, 0.25f, 0.24f), new Vector3(1.2f, 4f, 1.2f)),
            Create(DungeonShellVisualKind.StairsUp, "Stairs Up", StairsUpPath, new Color(0.32f, 0.52f, 0.62f), new Vector3(5.5f, 1f, 5.5f)),
            Create(DungeonShellVisualKind.StairsDown, "Stairs Down", StairsDownPath, new Color(0.8f, 0.66f, 0.22f), new Vector3(6f, 1f, 6f)),
            Create(DungeonShellVisualKind.RoomAccent, "Room Accent", RoomAccentPath, new Color(0.38f, 0.42f, 0.36f), new Vector3(2f, 2f, 2f)),
            Create(DungeonShellVisualKind.SecretAccent, "Secret Accent", SecretAccentPath, new Color(0.56f, 0.42f, 0.72f), new Vector3(2f, 2f, 2f)),
            Create(DungeonShellVisualKind.RoomFloor, "Room Floor", FloorPath, new Color(0.34f, 0.33f, 0.3f), new Vector3(6f, 0.1f, 6f)),
            Create(DungeonShellVisualKind.CorridorFloor, "Corridor Floor", CorridorPath, new Color(0.24f, 0.23f, 0.21f), new Vector3(6f, 0.1f, 6f)),
            Create(DungeonShellVisualKind.RoomWall, "Room Wall", WallPath, new Color(0.16f, 0.17f, 0.2f), new Vector3(6f, 6f, 0.35f)),
            Create(DungeonShellVisualKind.CorridorWall, "Corridor Wall", WallPath, new Color(0.15f, 0.16f, 0.18f), new Vector3(6f, 6f, 0.35f)),
            Create(DungeonShellVisualKind.DoorwaySideTrim, "Doorway Side Trim", DoorwaySideTrimPath, new Color(0.34f, 0.31f, 0.26f), new Vector3(0.3f, 5.5f, 0.4f)),
            Create(DungeonShellVisualKind.StairMarker, "Stair Marker", StairMarkerPath, new Color(0.7f, 0.58f, 0.24f), new Vector3(3f, 0.08f, 3f)),
            Create(DungeonShellVisualKind.RoomPurposeFloorTint, "Room Purpose Floor Tint", RoomPurposeFloorTintPath, new Color(0.5f, 0.42f, 0.28f, 0.72f), new Vector3(4f, 0.05f, 4f)),
            Create(DungeonShellVisualKind.RoomPurposeMarker, "Room Purpose Marker", RoomPurposeMarkerPath, new Color(0.5f, 0.42f, 0.28f, 0.85f), new Vector3(1.8f, 0.08f, 1.8f))
        };

        public static IReadOnlyList<DungeonShellVisualDefinition> All => Definitions;

        public static bool TryGet(DungeonShellVisualKind kind, out DungeonShellVisualDefinition definition)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].kind == kind)
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = default;
            return false;
        }

        private static DungeonShellVisualDefinition Create(DungeonShellVisualKind kind, string displayName, string resourcePath, Color fallbackColor, Vector3 fallbackScale)
        {
            return new DungeonShellVisualDefinition(
                kind,
                displayName,
                resourcePath,
                fallbackColor,
                fallbackScale,
                visualOnly: true,
                stripPrefabColliders: true,
                warningLabel: displayName);
        }

        public static DungeonShellVisualProfile GetActiveProfile()
        {
            return DungeonShellVisualProfile.TrainingLabyrinth;
        }
    }

    public enum DungeonShellVisualProfile
    {
        TrainingLabyrinth,
        PrisonStone,
        CryptStone,
        MineStone
    }
}
