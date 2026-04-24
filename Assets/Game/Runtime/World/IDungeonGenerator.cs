using FrontierDepths.Core;

namespace FrontierDepths.World
{
    public interface IDungeonGenerator
    {
        DungeonLayoutGraph Generate(FloorState floorState);
    }
}
