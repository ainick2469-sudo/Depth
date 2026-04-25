using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;

namespace FrontierDepths.Tests.EditMode
{
    public class DungeonRoomTemplateLibraryTests
    {
        [Test]
        public void AllSafeOrdinaryTemplatesAreStructurallyValid()
        {
            DungeonRoomTemplateKind[] templates = DungeonRoomTemplateLibrary.GetGateOneSafeOrdinaryTemplates();
            for (int i = 0; i < templates.Length; i++)
            {
                Assert.IsTrue(
                    DungeonRoomTemplateLibrary.IsStructurallyValid(templates[i]),
                    $"Template {templates[i]} failed structural validation.");
            }
        }

        [Test]
        public void LongGallery_AcceptsStraightExits_AndRejectsCornerExits()
        {
            Assert.IsNotEmpty(
                DungeonRoomTemplateLibrary.GetValidRotations(
                    DungeonRoomTemplateKind.LongGallery,
                    DungeonExitMask.East | DungeonExitMask.West));

            Assert.IsEmpty(
                DungeonRoomTemplateLibrary.GetValidRotations(
                    DungeonRoomTemplateKind.LongGallery,
                    DungeonExitMask.North | DungeonExitMask.East));
        }

        [Test]
        public void LChamberSafe_AcceptsCornerExits_AndRejectsStraightExits()
        {
            Assert.IsNotEmpty(
                DungeonRoomTemplateLibrary.GetValidRotations(
                    DungeonRoomTemplateKind.LChamberSafe,
                    DungeonExitMask.North | DungeonExitMask.East));

            Assert.IsEmpty(
                DungeonRoomTemplateLibrary.GetValidRotations(
                    DungeonRoomTemplateKind.LChamberSafe,
                    DungeonExitMask.East | DungeonExitMask.West));
        }

        [Test]
        public void NormalizeRoomSpacing_UsesTunedDefaultForLegacyValue()
        {
            float normalized = DungeonSceneController.NormalizeRoomSpacing(56f);

            Assert.That(normalized, Is.InRange(74f, 82f));
            Assert.AreEqual(DungeonSceneController.GetMinimumSafeRoomSpacing(), normalized, 0.01f);
        }

        [Test]
        public void KeyCombatTemplates_HaveLargerCombatReadyFootprints()
        {
            AssertFootprint(DungeonRoomTemplateKind.SquareChamber, 54f, 54f);
            AssertFootprint(DungeonRoomTemplateKind.BroadRectangle, 66f, 54f);
            AssertFootprint(DungeonRoomTemplateKind.LongGallery, 66f, 42f);
        }

        [Test]
        public void SafeTemplateFootprints_StayCompactEnoughForShorterSpacing()
        {
            DungeonRoomTemplateKind[] templates = DungeonRoomTemplateLibrary.GetGateOneSafeOrdinaryTemplates();
            for (int i = 0; i < templates.Length; i++)
            {
                for (int rotation = 0; rotation < 4; rotation++)
                {
                    Vector2 footprint = DungeonRoomTemplateLibrary.GetFootprintSize(templates[i], rotation, 6f);
                    Assert.LessOrEqual(
                        Mathf.Max(footprint.x, footprint.y),
                        66.01f,
                        $"{templates[i]} rotation {rotation} is too large for compact 78-unit room spacing.");
                }
            }
        }

        private static void AssertFootprint(DungeonRoomTemplateKind kind, float expectedWidth, float expectedLength)
        {
            Vector2 footprint = DungeonRoomTemplateLibrary.GetFootprintSize(kind, 0, 6f);

            Assert.That(footprint.x, Is.EqualTo(expectedWidth).Within(0.01f));
            Assert.That(footprint.y, Is.EqualTo(expectedLength).Within(0.01f));
        }
    }
}
