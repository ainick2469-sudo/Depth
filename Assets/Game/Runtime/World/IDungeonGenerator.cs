using FrontierDepths.Core;

namespace FrontierDepths.World
{
    public interface IDungeonGenerator
    {
        bool TryGenerateNormal(FloorState floorState, out DungeonLayoutGraph graph, out GraphValidationReport report);

        DungeonLayoutGraph GenerateFallback(FloorState floorState);
    }
}
