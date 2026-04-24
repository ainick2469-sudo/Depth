using FrontierDepths.World;
using NUnit.Framework;

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

            Assert.AreEqual(92f, normalized, 0.01f);
            Assert.LessOrEqual(DungeonSceneController.GetMinimumSafeRoomSpacing(), normalized);
        }
    }
}
