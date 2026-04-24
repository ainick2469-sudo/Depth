using System.Collections.Generic;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;

namespace FrontierDepths.Tests.EditMode
{
    public class DungeonSceneControllerGeometryTests
    {
        [Test]
        public void CorridorOuterWidth_MatchesCurrentSideWallPlacement()
        {
            Assert.That(DungeonSceneController.GetCorridorOuterWidth(16f), Is.EqualTo(17f).Within(0.001f));
        }

        [Test]
        public void VisualDoorwayWidth_MatchesCorridorOuterWidth()
        {
            float corridorOuterWidth = DungeonSceneController.GetCorridorOuterWidth(16f);

            Assert.That(DungeonSceneController.GetVisualDoorwayWidth(16f), Is.EqualTo(corridorOuterWidth).Within(0.001f));
        }

        [Test]
        public void ValidationDoorwayWidth_AddsDoorwayClearance()
        {
            float visualOpeningWidth = DungeonSceneController.GetVisualDoorwayWidth(16f);

            Assert.That(DungeonSceneController.GetValidationDoorwayWidth(visualOpeningWidth), Is.EqualTo(17.5f).Within(0.001f));
        }

        [Test]
        public void BuildCorridorRoute_UsesStraightSegmentWhenHorizontallyAligned()
        {
            List<Vector3> route = DungeonSceneController.BuildCorridorRoute(
                new Vector3(6f, 0f, 0f),
                new Vector3(14f, 0f, 0f),
                new Vector2Int(1, 0));

            Assert.That(route.Count, Is.EqualTo(2));
            AssertVector3(route[0], new Vector3(6f, 0f, 0f));
            AssertVector3(route[1], new Vector3(14f, 0f, 0f));
        }

        [Test]
        public void BuildCorridorRoute_UsesStraightSegmentWhenVerticallyAligned()
        {
            List<Vector3> route = DungeonSceneController.BuildCorridorRoute(
                new Vector3(0f, 0f, 6f),
                new Vector3(0f, 0f, 14f),
                new Vector2Int(0, 1));

            Assert.That(route.Count, Is.EqualTo(2));
            AssertVector3(route[0], new Vector3(0f, 0f, 6f));
            AssertVector3(route[1], new Vector3(0f, 0f, 14f));
        }

        [Test]
        public void BuildCorridorRoute_UsesDogLegWhenSocketsAreOffset()
        {
            List<Vector3> route = DungeonSceneController.BuildCorridorRoute(
                new Vector3(6f, 0f, 0f),
                new Vector3(14f, 0f, 8f),
                new Vector2Int(1, 0));

            Assert.That(route.Count, Is.EqualTo(4));
            AssertVector3(route[1], new Vector3(10f, 0f, 0f));
            AssertVector3(route[2], new Vector3(10f, 0f, 8f));
        }

        [Test]
        public void ExpandRouteEndpointsIntoRooms_OnlyMovesFirstAndLastPoints()
        {
            List<Vector3> route = new List<Vector3>
            {
                new Vector3(6f, 0f, 0f),
                new Vector3(10f, 0f, 0f),
                new Vector3(10f, 0f, 8f),
                new Vector3(14f, 0f, 8f)
            };

            List<Vector3> expanded = DungeonSceneController.ExpandRouteEndpointsIntoRooms(route, new Vector2Int(1, 0), 0.75f);

            Assert.That(expanded.Count, Is.EqualTo(route.Count));
            AssertVector3(expanded[0], new Vector3(5.25f, 0f, 0f));
            AssertVector3(expanded[1], route[1]);
            AssertVector3(expanded[2], route[2]);
            AssertVector3(expanded[3], new Vector3(14.75f, 0f, 8f));
        }

        private static void AssertVector3(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.001f));
        }
    }
}
