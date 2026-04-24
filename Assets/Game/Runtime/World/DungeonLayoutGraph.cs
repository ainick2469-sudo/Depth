using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    [Serializable]
    public sealed class DungeonLayoutGraph
    {
        public List<DungeonNode> nodes = new List<DungeonNode>();
        public List<DungeonEdge> edges = new List<DungeonEdge>();
        public string entryHubNodeId;
        public string transitUpNodeId;
        public string transitDownNodeId;

        // Legacy aliases kept for existing references and older saves.
        public string entryNodeId;
        public string stairsNodeId;
        public string returnAnchorNodeId;

        public DungeonNode GetNode(string nodeId)
        {
            return nodes.Find(node => node.nodeId == nodeId);
        }

        public bool HasPath(string fromNodeId, string toNodeId)
        {
            return GetGraphDistance(fromNodeId, toNodeId) >= 0;
        }

        public int GetGraphDistance(string fromNodeId, string toNodeId)
        {
            if (string.IsNullOrWhiteSpace(fromNodeId) || string.IsNullOrWhiteSpace(toNodeId))
            {
                return -1;
            }

            Dictionary<string, int> distances = BuildDistanceMap(fromNodeId);
            return distances.TryGetValue(toNodeId, out int distance) ? distance : -1;
        }

        public Dictionary<string, int> BuildDistanceMap(string startNodeId)
        {
            Dictionary<string, int> distances = new Dictionary<string, int>();
            if (string.IsNullOrWhiteSpace(startNodeId))
            {
                return distances;
            }

            Queue<string> frontier = new Queue<string>();
            frontier.Enqueue(startNodeId);
            distances[startNodeId] = 0;

            while (frontier.Count > 0)
            {
                string current = frontier.Dequeue();
                int currentDistance = distances[current];

                for (int i = 0; i < edges.Count; i++)
                {
                    string next = edges[i].GetOther(current);
                    if (string.IsNullOrWhiteSpace(next) || distances.ContainsKey(next))
                    {
                        continue;
                    }

                    distances[next] = currentDistance + 1;
                    frontier.Enqueue(next);
                }
            }

            return distances;
        }

        public int GetDegree(string nodeId)
        {
            int degree = 0;
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].a == nodeId || edges[i].b == nodeId)
                {
                    degree++;
                }
            }

            return degree;
        }

        public List<DungeonNode> GetNeighbors(string nodeId)
        {
            List<DungeonNode> neighbors = new List<DungeonNode>();
            for (int i = 0; i < edges.Count; i++)
            {
                string otherId = edges[i].GetOther(nodeId);
                if (string.IsNullOrWhiteSpace(otherId))
                {
                    continue;
                }

                DungeonNode other = GetNode(otherId);
                if (other != null)
                {
                    neighbors.Add(other);
                }
            }

            return neighbors;
        }
    }

    [Serializable]
    public sealed class DungeonNode
    {
        public string nodeId;
        public string label;
        public DungeonNodeKind nodeKind = DungeonNodeKind.Ordinary;
        public Vector2Int gridPosition;
        public DungeonRoomTemplateKind roomTemplate = DungeonRoomTemplateKind.SquareChamber;
        public int rotationQuarterTurns;
    }

    [Serializable]
    public struct DungeonEdge
    {
        public string a;
        public string b;

        public string GetOther(string value)
        {
            if (a == value)
            {
                return b;
            }

            if (b == value)
            {
                return a;
            }

            return null;
        }
    }

    public enum DungeonNodeKind
    {
        EntryHub,
        Ordinary,
        Landmark,
        Secret,
        TransitUp,
        TransitDown
    }

    public enum DungeonRoomTemplateKind
    {
        SquareChamber,
        BroadRectangle,
        LongGallery,
        LChamber,
        CruciformChamber,
        SplitChamber,
        RaisedDaisChamber,
        SunkenPitChamber,
        BalconyBridgeChamber
    }

    public enum DungeonTemplateFeature
    {
        Flat,
        RaisedDais,
        SunkenPit,
        SplitDivider,
        BalconyBridge
    }
}
