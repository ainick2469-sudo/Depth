using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    [Flags]
    internal enum DungeonExitMask
    {
        None = 0,
        North = 1 << 0,
        South = 1 << 1,
        East = 1 << 2,
        West = 1 << 3
    }

    public static class DungeonRoomTemplateLibrary
    {
        private static readonly DungeonRoomTemplateKind[] GateOneSafeOrdinaryTemplates =
        {
            DungeonRoomTemplateKind.SquareChamber,
            DungeonRoomTemplateKind.BroadRectangle,
            DungeonRoomTemplateKind.LongGallery,
            DungeonRoomTemplateKind.LChamberSafe,
            DungeonRoomTemplateKind.TChamberSafe,
            DungeonRoomTemplateKind.CrossChamberSafe,
            DungeonRoomTemplateKind.OctagonChamberSafe,
            DungeonRoomTemplateKind.PillarHallSafe,
            DungeonRoomTemplateKind.AlcoveRoomSafe,
            DungeonRoomTemplateKind.WideBendSafe,
            DungeonRoomTemplateKind.ForkRoomSafe
        };

        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0)
        };

        public sealed class TemplateData
        {
            public DungeonTemplateFeature feature;
            public Vector2Int[] cells;
            public Vector2Int centerCell;
            public Vector2Int northSocket;
            public Vector2Int southSocket;
            public Vector2Int eastSocket;
            public Vector2Int westSocket;
            internal DungeonExitMask supportedExits;
        }

        private static readonly Dictionary<DungeonRoomTemplateKind, TemplateData> Templates =
            new Dictionary<DungeonRoomTemplateKind, TemplateData>
            {
                {
                    DungeonRoomTemplateKind.SquareChamber,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Rect(-2, 2, -2, 2),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 2),
                        southSocket = new Vector2Int(0, -2),
                        eastSocket = new Vector2Int(2, 0),
                        westSocket = new Vector2Int(-2, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.South | DungeonExitMask.East | DungeonExitMask.West
                    }
                },
                {
                    DungeonRoomTemplateKind.BroadRectangle,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Rect(-3, 3, -2, 2),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 2),
                        southSocket = new Vector2Int(0, -2),
                        eastSocket = new Vector2Int(3, 0),
                        westSocket = new Vector2Int(-3, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.South | DungeonExitMask.East | DungeonExitMask.West
                    }
                },
                {
                    DungeonRoomTemplateKind.LongGallery,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Rect(-3, 3, -1, 1),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 1),
                        southSocket = new Vector2Int(0, -1),
                        eastSocket = new Vector2Int(3, 0),
                        westSocket = new Vector2Int(-3, 0),
                        supportedExits = DungeonExitMask.East | DungeonExitMask.West
                    }
                },
                {
                    DungeonRoomTemplateKind.LChamberSafe,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Union(
                            Rect(-1, 3, -1, 1),
                            Rect(-1, 1, -1, 3)),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 3),
                        southSocket = new Vector2Int(0, -1),
                        eastSocket = new Vector2Int(3, 0),
                        westSocket = new Vector2Int(-1, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.East
                    }
                },
                {
                    DungeonRoomTemplateKind.TChamberSafe,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Union(
                            Rect(-3, 3, -1, 1),
                            Rect(-1, 1, -1, 3)),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 3),
                        southSocket = new Vector2Int(0, -1),
                        eastSocket = new Vector2Int(3, 0),
                        westSocket = new Vector2Int(-3, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.East | DungeonExitMask.West
                    }
                },
                {
                    DungeonRoomTemplateKind.CrossChamberSafe,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Union(
                            Rect(-1, 1, -3, 3),
                            Rect(-3, 3, -1, 1)),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 3),
                        southSocket = new Vector2Int(0, -3),
                        eastSocket = new Vector2Int(3, 0),
                        westSocket = new Vector2Int(-3, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.South | DungeonExitMask.East | DungeonExitMask.West
                    }
                },
                {
                    DungeonRoomTemplateKind.OctagonChamberSafe,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Filter(Rect(-3, 3, -3, 3), cell => Mathf.Abs(cell.x) + Mathf.Abs(cell.y) <= 5),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 3),
                        southSocket = new Vector2Int(0, -3),
                        eastSocket = new Vector2Int(3, 0),
                        westSocket = new Vector2Int(-3, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.South | DungeonExitMask.East | DungeonExitMask.West
                    }
                },
                {
                    DungeonRoomTemplateKind.PillarHallSafe,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Union(
                            Rect(-3, 3, -1, 1),
                            Rect(-2, 2, -2, 2)),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 2),
                        southSocket = new Vector2Int(0, -2),
                        eastSocket = new Vector2Int(3, 0),
                        westSocket = new Vector2Int(-3, 0),
                        supportedExits = DungeonExitMask.East | DungeonExitMask.West
                    }
                },
                {
                    DungeonRoomTemplateKind.AlcoveRoomSafe,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Union(
                            Rect(-2, 2, -2, 1),
                            Rect(-1, 1, 2, 3)),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 3),
                        southSocket = new Vector2Int(0, -2),
                        eastSocket = new Vector2Int(2, 0),
                        westSocket = new Vector2Int(-2, 0),
                        supportedExits = DungeonExitMask.North
                    }
                },
                {
                    DungeonRoomTemplateKind.WideBendSafe,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Union(
                            Rect(-2, 3, -1, 1),
                            Rect(-1, 1, -2, 3)),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 3),
                        southSocket = new Vector2Int(0, -2),
                        eastSocket = new Vector2Int(3, 0),
                        westSocket = new Vector2Int(-2, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.East
                    }
                },
                {
                    DungeonRoomTemplateKind.ForkRoomSafe,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.Flat,
                        cells = Union(
                            Rect(-3, 3, -1, 1),
                            Rect(-2, 2, 1, 3)),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 3),
                        southSocket = new Vector2Int(0, -1),
                        eastSocket = new Vector2Int(3, 0),
                        westSocket = new Vector2Int(-3, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.East | DungeonExitMask.West
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
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(1, 6),
                        southSocket = new Vector2Int(-2, -4),
                        eastSocket = new Vector2Int(6, 1),
                        westSocket = new Vector2Int(-6, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.South | DungeonExitMask.East | DungeonExitMask.West
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
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 7),
                        southSocket = new Vector2Int(0, -7),
                        eastSocket = new Vector2Int(7, 0),
                        westSocket = new Vector2Int(-7, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.South | DungeonExitMask.East | DungeonExitMask.West
                    }
                },
                {
                    DungeonRoomTemplateKind.SplitChamber,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.SplitDivider,
                        cells = Rect(-6, 6, -4, 4),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 4),
                        southSocket = new Vector2Int(0, -4),
                        eastSocket = new Vector2Int(6, 0),
                        westSocket = new Vector2Int(-6, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.South | DungeonExitMask.East | DungeonExitMask.West
                    }
                },
                {
                    DungeonRoomTemplateKind.RaisedDaisChamber,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.RaisedDais,
                        cells = Rect(-6, 6, -6, 6),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 6),
                        southSocket = new Vector2Int(0, -6),
                        eastSocket = new Vector2Int(6, 0),
                        westSocket = new Vector2Int(-6, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.South | DungeonExitMask.East | DungeonExitMask.West
                    }
                },
                {
                    DungeonRoomTemplateKind.SunkenPitChamber,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.SunkenPit,
                        cells = Rect(-6, 6, -6, 6),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 6),
                        southSocket = new Vector2Int(0, -6),
                        eastSocket = new Vector2Int(6, 0),
                        westSocket = new Vector2Int(-6, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.South | DungeonExitMask.East | DungeonExitMask.West
                    }
                },
                {
                    DungeonRoomTemplateKind.BalconyBridgeChamber,
                    new TemplateData
                    {
                        feature = DungeonTemplateFeature.BalconyBridge,
                        cells = Rect(-7, 7, -5, 5),
                        centerCell = Vector2Int.zero,
                        northSocket = new Vector2Int(0, 5),
                        southSocket = new Vector2Int(0, -5),
                        eastSocket = new Vector2Int(7, 0),
                        westSocket = new Vector2Int(-7, 0),
                        supportedExits = DungeonExitMask.North | DungeonExitMask.South | DungeonExitMask.East | DungeonExitMask.West
                    }
                }
            };

        public static TemplateData GetTemplate(DungeonRoomTemplateKind kind)
        {
            return Templates[kind];
        }

        public static HashSet<Vector2Int> GetCells(DungeonNode node)
        {
            return GetRotatedCells(node.roomTemplate, node.rotationQuarterTurns);
        }

        internal static HashSet<Vector2Int> GetRotatedCells(DungeonRoomTemplateKind kind, int quarterTurns)
        {
            TemplateData template = GetTemplate(kind);
            HashSet<Vector2Int> rotated = new HashSet<Vector2Int>();
            for (int i = 0; i < template.cells.Length; i++)
            {
                rotated.Add(Rotate(template.cells[i], quarterTurns));
            }

            return rotated;
        }

        public static Vector2Int GetDoorSocket(DungeonNode node, Vector2Int direction)
        {
            return GetDoorSocket(node.roomTemplate, node.rotationQuarterTurns, direction);
        }

        internal static Vector2Int GetDoorSocket(DungeonRoomTemplateKind kind, int quarterTurns, Vector2Int direction)
        {
            TemplateData template = GetTemplate(kind);
            Vector2Int localDirection = Rotate(direction, -quarterTurns);
            Vector2Int baseSocket = localDirection switch
            {
                { x: 0, y: 1 } => template.northSocket,
                { x: 0, y: -1 } => template.southSocket,
                { x: 1, y: 0 } => template.eastSocket,
                _ => template.westSocket
            };

            return Rotate(baseSocket, quarterTurns);
        }

        internal static Vector2Int GetCenterCell(DungeonRoomTemplateKind kind, int quarterTurns)
        {
            return Rotate(GetTemplate(kind).centerCell, quarterTurns);
        }

        public static DungeonTemplateFeature GetFeature(DungeonNode node)
        {
            return GetTemplate(node.roomTemplate).feature;
        }

        public static bool IsGateOneSafeOrdinaryTemplate(DungeonRoomTemplateKind kind)
        {
            for (int i = 0; i < GateOneSafeOrdinaryTemplates.Length; i++)
            {
                if (GateOneSafeOrdinaryTemplates[i] == kind)
                {
                    return true;
                }
            }

            return false;
        }

        public static DungeonRoomTemplateKind[] GetGateOneSafeOrdinaryTemplates()
        {
            DungeonRoomTemplateKind[] copy = new DungeonRoomTemplateKind[GateOneSafeOrdinaryTemplates.Length];
            GateOneSafeOrdinaryTemplates.CopyTo(copy, 0);
            return copy;
        }

        internal static DungeonExitMask DirectionToMask(Vector2Int direction)
        {
            return direction switch
            {
                { x: 0, y: 1 } => DungeonExitMask.North,
                { x: 0, y: -1 } => DungeonExitMask.South,
                { x: 1, y: 0 } => DungeonExitMask.East,
                { x: -1, y: 0 } => DungeonExitMask.West,
                _ => DungeonExitMask.None
            };
        }

        internal static DungeonExitMask RotateExitMask(DungeonExitMask mask, int quarterTurns)
        {
            DungeonExitMask rotated = DungeonExitMask.None;
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int direction = CardinalDirections[i];
                DungeonExitMask directionMask = DirectionToMask(direction);
                if ((mask & directionMask) == 0)
                {
                    continue;
                }

                rotated |= DirectionToMask(Rotate(direction, quarterTurns));
            }

            return rotated;
        }

        internal static DungeonExitMask GetSupportedExitMask(DungeonRoomTemplateKind kind, int quarterTurns)
        {
            return RotateExitMask(GetTemplate(kind).supportedExits, quarterTurns);
        }

        internal static bool SupportsRequiredExits(DungeonRoomTemplateKind kind, int quarterTurns, DungeonExitMask requiredMask)
        {
            if (requiredMask == DungeonExitMask.None)
            {
                return true;
            }

            DungeonExitMask supported = GetSupportedExitMask(kind, quarterTurns);
            return (supported & requiredMask) == requiredMask;
        }

        internal static List<int> GetValidRotations(DungeonRoomTemplateKind kind, DungeonExitMask requiredMask)
        {
            List<int> rotations = new List<int>();
            for (int rotation = 0; rotation < 4; rotation++)
            {
                if (!SupportsRequiredExits(kind, rotation, requiredMask))
                {
                    continue;
                }

                if (!AreRequiredSocketsBoundaryValid(kind, rotation, requiredMask))
                {
                    continue;
                }

                if (!AreRequiredSocketsAdjacentToWalkableFloor(kind, rotation, requiredMask))
                {
                    continue;
                }

                if (!AreRequiredSocketsConnectedToCenter(kind, rotation, requiredMask))
                {
                    continue;
                }

                rotations.Add(rotation);
            }

            return rotations;
        }

        internal static bool IsStructurallyValid(DungeonRoomTemplateKind kind)
        {
            TemplateData template = GetTemplate(kind);
            if (template.feature != DungeonTemplateFeature.Flat)
            {
                return false;
            }

            if (!GetRotatedCells(kind, 0).Contains(template.centerCell))
            {
                return false;
            }

            for (int rotation = 0; rotation < 4; rotation++)
            {
                DungeonExitMask supported = GetSupportedExitMask(kind, rotation);
                if (!AreRequiredSocketsBoundaryValid(kind, rotation, supported))
                {
                    return false;
                }

                if (!AreRequiredSocketsAdjacentToWalkableFloor(kind, rotation, supported))
                {
                    return false;
                }

                if (!AreRequiredSocketsConnectedToCenter(kind, rotation, supported))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool AreRequiredSocketsBoundaryValid(DungeonRoomTemplateKind kind, int quarterTurns, DungeonExitMask requiredMask)
        {
            HashSet<Vector2Int> cells = GetRotatedCells(kind, quarterTurns);
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int direction = CardinalDirections[i];
                DungeonExitMask directionMask = DirectionToMask(direction);
                if ((requiredMask & directionMask) == 0)
                {
                    continue;
                }

                Vector2Int socket = GetDoorSocket(kind, quarterTurns, direction);
                if (!cells.Contains(socket))
                {
                    return false;
                }

                if (cells.Contains(socket + direction))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool AreRequiredSocketsAdjacentToWalkableFloor(DungeonRoomTemplateKind kind, int quarterTurns, DungeonExitMask requiredMask)
        {
            HashSet<Vector2Int> cells = GetRotatedCells(kind, quarterTurns);
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int direction = CardinalDirections[i];
                DungeonExitMask directionMask = DirectionToMask(direction);
                if ((requiredMask & directionMask) == 0)
                {
                    continue;
                }

                Vector2Int socket = GetDoorSocket(kind, quarterTurns, direction);
                if (!cells.Contains(socket - direction))
                {
                    bool hasAdjacentFloor = false;
                    for (int neighborIndex = 0; neighborIndex < CardinalDirections.Length; neighborIndex++)
                    {
                        Vector2Int neighbor = socket + CardinalDirections[neighborIndex];
                        if (neighbor == socket + direction)
                        {
                            continue;
                        }

                        if (cells.Contains(neighbor))
                        {
                            hasAdjacentFloor = true;
                            break;
                        }
                    }

                    if (!hasAdjacentFloor)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal static bool AreRequiredSocketsConnectedToCenter(DungeonRoomTemplateKind kind, int quarterTurns, DungeonExitMask requiredMask)
        {
            HashSet<Vector2Int> cells = GetRotatedCells(kind, quarterTurns);
            Vector2Int centerCell = GetCenterCell(kind, quarterTurns);
            if (!cells.Contains(centerCell))
            {
                return false;
            }

            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int direction = CardinalDirections[i];
                DungeonExitMask directionMask = DirectionToMask(direction);
                if ((requiredMask & directionMask) == 0)
                {
                    continue;
                }

                Vector2Int socket = GetDoorSocket(kind, quarterTurns, direction);
                if (!HasPath(cells, socket, centerCell))
                {
                    return false;
                }
            }

            return true;
        }

        internal static Vector2 GetFootprintSize(DungeonRoomTemplateKind kind, int quarterTurns, float cellSize)
        {
            HashSet<Vector2Int> cells = GetRotatedCells(kind, quarterTurns);
            GetLocalBounds(cells, cellSize, out float minX, out float maxX, out float minZ, out float maxZ);
            return new Vector2(maxX - minX, maxZ - minZ);
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

        private static void GetLocalBounds(HashSet<Vector2Int> cells, float cellSize, out float minX, out float maxX, out float minZ, out float maxZ)
        {
            minX = float.MaxValue;
            maxX = float.MinValue;
            minZ = float.MaxValue;
            maxZ = float.MinValue;

            foreach (Vector2Int cell in cells)
            {
                float x = cell.x * cellSize;
                float z = cell.y * cellSize;
                minX = Mathf.Min(minX, x - cellSize * 0.5f);
                maxX = Mathf.Max(maxX, x + cellSize * 0.5f);
                minZ = Mathf.Min(minZ, z - cellSize * 0.5f);
                maxZ = Mathf.Max(maxZ, z + cellSize * 0.5f);
            }
        }

        private static bool HasPath(HashSet<Vector2Int> cells, Vector2Int start, Vector2Int goal)
        {
            if (!cells.Contains(start) || !cells.Contains(goal))
            {
                return false;
            }

            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            HashSet<Vector2Int> seen = new HashSet<Vector2Int> { start };
            frontier.Enqueue(start);

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                if (current == goal)
                {
                    return true;
                }

                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int next = current + CardinalDirections[i];
                    if (!cells.Contains(next) || !seen.Add(next))
                    {
                        continue;
                    }

                    frontier.Enqueue(next);
                }
            }

            return false;
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

        private static Vector2Int[] Filter(Vector2Int[] source, Predicate<Vector2Int> predicate)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            for (int i = 0; i < source.Length; i++)
            {
                if (predicate(source[i]))
                {
                    cells.Add(source[i]);
                }
            }

            return cells.ToArray();
        }
    }
}
