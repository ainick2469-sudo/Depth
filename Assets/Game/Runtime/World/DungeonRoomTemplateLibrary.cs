using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    public static class DungeonRoomTemplateLibrary
    {
        public sealed class TemplateData
        {
            public DungeonTemplateFeature feature;
            public Vector2Int[] cells;
            public Vector2Int northSocket;
            public Vector2Int southSocket;
            public Vector2Int eastSocket;
            public Vector2Int westSocket;
        }

        private static readonly Dictionary<DungeonRoomTemplateKind, TemplateData> Templates =
            new Dictionary<DungeonRoomTemplateKind, TemplateData>
            {
                {
                    DungeonRoomTemplateKind.SquareChamber,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Rect(-4, 4, -4, 4),
                        northSocket = new Vector2Int(0, 4),
                        southSocket = new Vector2Int(0, -4),
                        eastSocket = new Vector2Int(4, 0),
                        westSocket = new Vector2Int(-4, 0)
                    }
                },
                {
                    DungeonRoomTemplateKind.BroadRectangle,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Rect(-6, 6, -4, 4),
                        northSocket = new Vector2Int(0, 4),
                        southSocket = new Vector2Int(0, -4),
                        eastSocket = new Vector2Int(6, 0),
                        westSocket = new Vector2Int(-6, 0)
                    }
                },
                {
                    DungeonRoomTemplateKind.LongGallery,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Rect(-8, 8, -4, 4),
                        northSocket = new Vector2Int(0, 4),
                        southSocket = new Vector2Int(0, -4),
                        eastSocket = new Vector2Int(8, 0),
                        westSocket = new Vector2Int(-8, 0)
                    }
                },
                {
                    DungeonRoomTemplateKind.LChamber,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Union(
                            Rect(-6, 0, -4, 5),
                            Rect(0, 6, -1, 6)),
                        northSocket = new Vector2Int(1, 6),
                        southSocket = new Vector2Int(-2, -4),
                        eastSocket = new Vector2Int(6, 1),
                        westSocket = new Vector2Int(-6, 0)
                    }
                },
                {
                    DungeonRoomTemplateKind.CruciformChamber,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Union(
                            Rect(-4, 4, -7, 7),
                            Rect(-7, 7, -4, 4)),
                        northSocket = new Vector2Int(0, 7),
                        southSocket = new Vector2Int(0, -7),
                        eastSocket = new Vector2Int(7, 0),
                        westSocket = new Vector2Int(-7, 0)
                    }
                },
                {
                    DungeonRoomTemplateKind.SplitChamber,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.SplitDivider,
                        cells = Rect(-6, 6, -4, 4),
                        northSocket = new Vector2Int(0, 4),
                        southSocket = new Vector2Int(0, -4),
                        eastSocket = new Vector2Int(6, 0),
                        westSocket = new Vector2Int(-6, 0)
                    }
                },
                {
                    DungeonRoomTemplateKind.RaisedDaisChamber,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.RaisedDais,
                        cells = Rect(-6, 6, -6, 6),
                        northSocket = new Vector2Int(0, 6),
                        southSocket = new Vector2Int(0, -6),
                        eastSocket = new Vector2Int(6, 0),
                        westSocket = new Vector2Int(-6, 0)
                    }
                },
                {
                    DungeonRoomTemplateKind.SunkenPitChamber,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.SunkenPit,
                        cells = Rect(-6, 6, -6, 6),
                        northSocket = new Vector2Int(0, 6),
                        southSocket = new Vector2Int(0, -6),
                        eastSocket = new Vector2Int(6, 0),
                        westSocket = new Vector2Int(-6, 0)
                    }
                },
                {
                    DungeonRoomTemplateKind.BalconyBridgeChamber,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.BalconyBridge,
                        cells = Rect(-7, 7, -5, 5),
                        northSocket = new Vector2Int(0, 5),
                        southSocket = new Vector2Int(0, -5),
                        eastSocket = new Vector2Int(7, 0),
                        westSocket = new Vector2Int(-7, 0)
                    }
                }
            };

        public static TemplateData GetTemplate(DungeonRoomTemplateKind kind)
        {
            return Templates[kind];
        }

        public static HashSet<Vector2Int> GetCells(DungeonNode node)
        {
            TemplateData template = GetTemplate(node.roomTemplate);
            HashSet<Vector2Int> rotated = new HashSet<Vector2Int>();
            for (int i = 0; i < template.cells.Length; i++)
            {
                rotated.Add(Rotate(template.cells[i], node.rotationQuarterTurns));
            }

            return rotated;
        }

        public static Vector2Int GetDoorSocket(DungeonNode node, Vector2Int direction)
        {
            TemplateData template = GetTemplate(node.roomTemplate);
            Vector2Int localDirection = Rotate(direction, -node.rotationQuarterTurns);
            Vector2Int baseSocket = localDirection switch
            {
                { x: 0, y: 1 } => template.northSocket,
                { x: 0, y: -1 } => template.southSocket,
                { x: 1, y: 0 } => template.eastSocket,
                _ => template.westSocket
            };

            return Rotate(baseSocket, node.rotationQuarterTurns);
        }

        public static DungeonTemplateFeature GetFeature(DungeonNode node)
        {
            return GetTemplate(node.roomTemplate).feature;
        }

        public static Vector2Int Rotate(Vector2Int cell, int quarterTurns)
        {
            quarterTurns = ((quarterTurns % 4) + 4) % 4;
            return quarterTurns switch
            {
                1 => new Vector2Int(cell.y, -cell.x),
                2 => new Vector2Int(-cell.x, -cell.y),
                3 => new Vector2Int(-cell.y, cell.x),
                _ => cell
            };
        }

        private static Vector2Int[] Rect(int minX, int maxX, int minY, int maxY)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    cells.Add(new Vector2Int(x, y));
                }
            }

            return cells.ToArray();
        }

        private static Vector2Int[] Union(params Vector2Int[][] groups)
        {
            HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
            for (int i = 0; i < groups.Length; i++)
            {
                for (int j = 0; j < groups[i].Length; j++)
                {
                    cells.Add(groups[i][j]);
                }
            }

            Vector2Int[] output = new Vector2Int[cells.Count];
            cells.CopyTo(output);
            return output;
        }
    }
}
