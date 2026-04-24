using System.Collections.Generic;
using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;

namespace FrontierDepths.Tests.EditMode
{
    public class DungeonSpawnRoutingTests
    {
        [Test]
        public void ResolveSpawnRouting_StartedRun_UsesTransitUp()
        {
            DungeonLayoutGraph graph = CreateGraph();

            DungeonSceneController.DungeonSpawnRoutingResult routing = DungeonSceneController.ResolveSpawnRouting(
                graph,
                FloorTransitionKind.StartedRun,
                PortalAnchorState.Invalid,
                1);

            Assert.IsFalse(routing.useExplicitWorldPosition);
            Assert.AreEqual(graph.transitUpNodeId, routing.selectedNodeId);
            Assert.AreEqual(DungeonNodeKind.TransitUp.ToString(), routing.selectedNodeKind);
            Assert.IsFalse(routing.usedEntryFallback);
        }

        [Test]
        public void ResolveSpawnRouting_Descended_UsesTransitUp()
        {
            DungeonLayoutGraph graph = CreateGraph();

            DungeonSceneController.DungeonSpawnRoutingResult routing = DungeonSceneController.ResolveSpawnRouting(
                graph,
                FloorTransitionKind.Descended,
                PortalAnchorState.Invalid,
                2);

            Assert.AreEqual(graph.transitUpNodeId, routing.selectedNodeId);
            Assert.AreEqual(DungeonNodeKind.TransitUp.ToString(), routing.selectedNodeKind);
        }

        [Test]
        public void ResolveSpawnRouting_Ascended_UsesTransitDown()
        {
            DungeonLayoutGraph graph = CreateGraph();

            DungeonSceneController.DungeonSpawnRoutingResult routing = DungeonSceneController.ResolveSpawnRouting(
                graph,
                FloorTransitionKind.Ascended,
                PortalAnchorState.Invalid,
                2);

            Assert.AreEqual(graph.transitDownNodeId, routing.selectedNodeId);
            Assert.AreEqual(DungeonNodeKind.TransitDown.ToString(), routing.selectedNodeKind);
        }

        [Test]
        public void ResolveSpawnRouting_ReturnedByPortal_UsesPortalAnchorWhenValid()
        {
            DungeonLayoutGraph graph = CreateGraph();
            PortalAnchorState anchor = new PortalAnchorState
            {
                isValid = true,
                floorIndex = 3,
                roomId = "ordinary",
                worldPosition = new SerializableVector3(new Vector3(24f, 2f, 48f))
            };

            DungeonSceneController.DungeonSpawnRoutingResult routing = DungeonSceneController.ResolveSpawnRouting(
                graph,
                FloorTransitionKind.ReturnedByPortal,
                anchor,
                3);

            Assert.IsTrue(routing.useExplicitWorldPosition);
            Assert.AreEqual("ordinary", routing.selectedNodeId);
            Assert.AreEqual(DungeonNodeKind.Ordinary.ToString(), routing.selectedNodeKind);
            Assert.That(routing.explicitWorldPosition, Is.EqualTo(new Vector3(24f, 2f, 48f)));
        }

        [Test]
        public void ResolveSpawnRouting_MissingRequestedNode_FallsBackToEntryHub()
        {
            DungeonLayoutGraph graph = CreateGraph();
            graph.transitUpNodeId = "missing_transit_up";

            DungeonSceneController.DungeonSpawnRoutingResult routing = DungeonSceneController.ResolveSpawnRouting(
                graph,
                FloorTransitionKind.Descended,
                PortalAnchorState.Invalid,
                2);

            Assert.IsFalse(routing.useExplicitWorldPosition);
            Assert.AreEqual(graph.entryHubNodeId, routing.selectedNodeId);
            Assert.AreEqual(DungeonNodeKind.EntryHub.ToString(), routing.selectedNodeKind);
            Assert.IsTrue(routing.usedEntryFallback);
            Assert.IsNotEmpty(routing.warningMessage);
        }

        private static DungeonLayoutGraph CreateGraph()
        {
            return new DungeonLayoutGraph
            {
                entryHubNodeId = "entry",
                transitUpNodeId = "transit_up",
                transitDownNodeId = "transit_down",
                nodes = new List<DungeonNode>
                {
                    new DungeonNode { nodeId = "entry", nodeKind = DungeonNodeKind.EntryHub, gridPosition = Vector2Int.zero },
                    new DungeonNode { nodeId = "transit_up", nodeKind = DungeonNodeKind.TransitUp, gridPosition = new Vector2Int(1, 0) },
                    new DungeonNode { nodeId = "transit_down", nodeKind = DungeonNodeKind.TransitDown, gridPosition = new Vector2Int(-1, 0) },
                    new DungeonNode { nodeId = "ordinary", nodeKind = DungeonNodeKind.Ordinary, gridPosition = new Vector2Int(0, 1) }
                }
            };
        }
    }
}
