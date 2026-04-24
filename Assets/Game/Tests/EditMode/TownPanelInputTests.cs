using FrontierDepths.Progression;
using NUnit.Framework;

namespace FrontierDepths.Tests.EditMode
{
    public class TownPanelInputTests
    {
        [Test]
        public void ShouldClosePanelFromInput_IgnoresSameFrameOpen()
        {
            bool shouldClose = TownHubController.ShouldClosePanelFromInput(
                isPanelOpen: true,
                eDown: true,
                escapeDown: false,
                openedThisFrame: true);

            Assert.IsFalse(shouldClose);
        }

        [Test]
        public void ShouldClosePanelFromInput_ClosesOnLaterEPress()
        {
            bool shouldClose = TownHubController.ShouldClosePanelFromInput(
                isPanelOpen: true,
                eDown: true,
                escapeDown: false,
                openedThisFrame: false);

            Assert.IsTrue(shouldClose);
        }

        [Test]
        public void ShouldClosePanelFromInput_ClosesOnLaterEscapePress()
        {
            bool shouldClose = TownHubController.ShouldClosePanelFromInput(
                isPanelOpen: true,
                eDown: false,
                escapeDown: true,
                openedThisFrame: false);

            Assert.IsTrue(shouldClose);
        }
    }
}
