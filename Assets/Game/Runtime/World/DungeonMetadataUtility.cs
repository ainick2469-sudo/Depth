using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    public static class DungeonMetadataUtility
    {
        public static void ApplyGraphDefaults(DungeonLayoutGraph graph, int floorIndex, int seed)
        {
            if (graph == null)
            {
                return;
            }

            Dictionary<string, int> distances = graph.BuildDistanceMap(graph.entryHubNodeId);
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                AssignStructuralDefaults(node, distances.TryGetValue(node.nodeId, out int distance) ? distance : -1, floorIndex);
            }
        }

        public static void CopyNodeMetadata(DungeonNode node, DungeonRoomBuildRecord roomRecord)
        {
            if (node == null || roomRecord == null)
            {
                return;
            }

            roomRecord.zoneId = node.zoneId ?? string.Empty;
            roomRecord.zoneType = node.zoneType;
            roomRecord.roomRole = node.roomRole;
            roomRecord.criticalPathIndex = node.criticalPathIndex;
            roomRecord.isOptional = node.isOptional;
            roomRecord.dangerTier = node.dangerTier;
            roomRecord.lockId = node.lockId ?? string.Empty;
            roomRecord.requiredKeyId = node.requiredKeyId ?? string.Empty;
            roomRecord.grantsKeyId = node.grantsKeyId ?? string.Empty;
            roomRecord.bountyId = node.bountyId ?? string.Empty;
            roomRecord.questId = node.questId ?? string.Empty;
        }

        public static void ApplyPurposeMetadata(DungeonRoomBuildRecord roomRecord, RoomPurposeDefinition purpose, int floorIndex)
        {
            if (roomRecord == null || purpose == null)
            {
                return;
            }

            if (IsStructuralProtected(roomRecord.roomType))
            {
                return;
            }

            switch (purpose.effect)
            {
                case RoomPurposeEffect.Cache:
                    SetPurposeRole(roomRecord, DungeonZoneType.ForgottenHalls, DungeonRoomRole.Treasure, floorIndex, 0);
                    break;
                case RoomPurposeEffect.Shrine:
                case RoomPurposeEffect.Fountain:
                case RoomPurposeEffect.Sanctuary:
                    SetPurposeRole(roomRecord, DungeonZoneType.Shrine, DungeonRoomRole.Shrine, floorIndex, 0);
                    break;
                case RoomPurposeEffect.Elite:
                    SetPurposeRole(roomRecord, DungeonZoneType.ForgottenHalls, DungeonRoomRole.Elite, floorIndex, 2);
                    break;
                case RoomPurposeEffect.Ambush:
                    SetPurposeRole(roomRecord, DungeonZoneType.ForgottenHalls, DungeonRoomRole.Combat, floorIndex, 1);
                    break;
                case RoomPurposeEffect.Wild:
                    SetPurposeRole(roomRecord, DungeonZoneType.ForgottenHalls, DungeonRoomRole.Hub, floorIndex, 1);
                    break;
                case RoomPurposeEffect.Treasury:
                    SetPurposeRole(roomRecord, DungeonZoneType.Treasury, DungeonRoomRole.Treasure, floorIndex, 0);
                    break;
                case RoomPurposeEffect.Armory:
                    SetPurposeRole(roomRecord, DungeonZoneType.Armory, DungeonRoomRole.Armory, floorIndex, 0);
                    break;
                case RoomPurposeEffect.CursedVault:
                    SetPurposeRole(roomRecord, DungeonZoneType.CursedDepths, DungeonRoomRole.Secret, floorIndex, 2);
                    break;
                case RoomPurposeEffect.Scout:
                    SetPurposeRole(roomRecord, DungeonZoneType.ForgottenHalls, DungeonRoomRole.Scout, floorIndex, 0);
                    break;
            }
        }

        public static string BuildZoneId(DungeonZoneType zoneType, int index = 0)
        {
            string raw = zoneType.ToString();
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (i > 0 && char.IsUpper(c))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
            }

            return $"{builder}_{Mathf.Max(0, index)}";
        }

        public static int CalculateDangerTier(int floorIndex, DungeonRoomRole role, RoomPurposeEffect purposeEffect = RoomPurposeEffect.None)
        {
            int tier = Mathf.Max(0, floorIndex / 3);
            tier += role switch
            {
                DungeonRoomRole.Elite => 2,
                DungeonRoomRole.MiniBoss => 4,
                DungeonRoomRole.Boss => 6,
                DungeonRoomRole.Secret => 1,
                _ => 0
            };
            tier += purposeEffect switch
            {
                RoomPurposeEffect.Ambush => 1,
                RoomPurposeEffect.CursedVault => 2,
                _ => 0
            };

            return Mathf.Clamp(tier, 0, 10);
        }

        private static void AssignStructuralDefaults(DungeonNode node, int distance, int floorIndex)
        {
            if (node == null)
            {
                return;
            }

            switch (node.nodeKind)
            {
                case DungeonNodeKind.EntryHub:
                    SetNodeMetadata(node, DungeonZoneType.Entrance, DungeonRoomRole.Start, distance, false, floorIndex, 0);
                    break;
                case DungeonNodeKind.TransitUp:
                    SetNodeMetadata(node, DungeonZoneType.Entrance, DungeonRoomRole.Return, distance, false, floorIndex, 0);
                    break;
                case DungeonNodeKind.TransitDown:
                    SetNodeMetadata(node, DungeonZoneType.BossWing, DungeonRoomRole.Exit, distance, false, floorIndex, 0);
                    break;
                case DungeonNodeKind.Landmark:
                    SetNodeMetadata(node, DungeonZoneType.Shrine, DungeonRoomRole.Shrine, distance, true, floorIndex, 0);
                    break;
                case DungeonNodeKind.Secret:
                    SetNodeMetadata(node, DungeonZoneType.SecretNetwork, DungeonRoomRole.Secret, distance, true, floorIndex, 1);
                    break;
                default:
                    SetNodeMetadata(node, DungeonZoneType.ForgottenHalls, DungeonRoomRole.Combat, distance, true, floorIndex, 0);
                    break;
            }
        }

        private static void SetNodeMetadata(DungeonNode node, DungeonZoneType zone, DungeonRoomRole role, int distance, bool optional, int floorIndex, int bonus)
        {
            node.zoneType = zone;
            node.roomRole = role;
            node.zoneId = BuildZoneId(zone);
            node.criticalPathIndex = distance;
            node.isOptional = optional;
            node.dangerTier = Mathf.Clamp(CalculateDangerTier(floorIndex, role) + bonus, 0, 10);
            node.lockId ??= string.Empty;
            node.requiredKeyId ??= string.Empty;
            node.grantsKeyId ??= string.Empty;
            node.bountyId ??= string.Empty;
            node.questId ??= string.Empty;
        }

        private static void SetPurposeRole(DungeonRoomBuildRecord roomRecord, DungeonZoneType zone, DungeonRoomRole role, int floorIndex, int bonus)
        {
            roomRecord.zoneType = zone;
            roomRecord.roomRole = role;
            roomRecord.zoneId = BuildZoneId(zone);
            roomRecord.dangerTier = Mathf.Clamp(CalculateDangerTier(floorIndex, role) + bonus, 0, 10);
        }

        private static bool IsStructuralProtected(DungeonNodeKind nodeKind)
        {
            return nodeKind == DungeonNodeKind.EntryHub ||
                   nodeKind == DungeonNodeKind.TransitUp ||
                   nodeKind == DungeonNodeKind.TransitDown ||
                   nodeKind == DungeonNodeKind.Secret;
        }
    }
}
