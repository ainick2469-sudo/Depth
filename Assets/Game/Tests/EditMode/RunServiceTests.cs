using FrontierDepths.Core;
using NUnit.Framework;

namespace FrontierDepths.Tests.EditMode
{
    public class RunServiceTests
    {
        [Test]
        public void FloorState_Normalize_AssignsReasonableDefaults()
        {
            FloorState state = new FloorState();
            state.Normalize(3, 1000);

            Assert.AreEqual(3, state.floorIndex);
            Assert.AreEqual(1093, state.floorSeed);
            Assert.IsFalse(string.IsNullOrWhiteSpace(state.floorBandId));
        }

        [Test]
        public void RunState_Normalize_KeepsVisitedFloorRecordForCurrentFloor()
        {
            RunState state = new RunState
            {
                isActive = true,
                seed = 2000,
                floorIndex = 4,
                currentFloor = new FloorState { floorIndex = 4, floorSeed = 5908 }
            };

            state.Normalize();

            Assert.AreEqual(1, state.visitedFloors.Count);
            Assert.AreEqual(4, state.visitedFloors[0].floorIndex);
            Assert.AreEqual(5908, state.visitedFloors[0].floorSeed);
        }
    }
}
