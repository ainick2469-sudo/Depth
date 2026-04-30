using System.IO;
using NUnit.Framework;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS131CExportTests
    {
        [Test]
        public void ChatReviewExportTool_IsAllowlistedAndDocumentsExcludedPayloads()
        {
            string scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "GenerateChatReviewExport.ps1");
            string docsPath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSnapshot", "EXPORT_CONTENTS.md");
            string ignorePath = Path.Combine(Directory.GetCurrentDirectory(), ".gitignore");

            Assert.IsTrue(File.Exists(scriptPath), "Chat review export script should exist.");
            Assert.IsTrue(File.Exists(docsPath), "Export contents documentation should exist.");
            Assert.IsTrue(File.Exists(ignorePath), ".gitignore should exist.");

            string script = File.ReadAllText(scriptPath);
            StringAssert.Contains("ProjectSnapshot/CHAT_REVIEW_EXPORT.zip", script);
            StringAssert.Contains("Assets/Game", script);
            StringAssert.Contains("ProjectSnapshot", script);
            StringAssert.Contains("Packages/manifest.json", script);
            StringAssert.Contains(".fbx", script);
            StringAssert.Contains(".png", script);
            StringAssert.Contains("Library", script);
            StringAssert.Contains("UserSettings", script);

            string docs = File.ReadAllText(docsPath);
            StringAssert.Contains("CHAT_REVIEW_EXPORT.zip", docs);
            StringAssert.Contains("Assets/Game/**/*.cs", docs);
            StringAssert.Contains("Excluded", docs);

            string ignore = File.ReadAllText(ignorePath);
            StringAssert.Contains("ProjectSnapshot/CHAT_REVIEW_EXPORT.zip", ignore);
            StringAssert.Contains("ProjectSnapshot/_chat_review_export_tmp/", ignore);
        }
    }
}
