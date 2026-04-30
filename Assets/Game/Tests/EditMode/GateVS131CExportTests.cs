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
            string legacyScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "GenerateProjectSnapshot.ps1");
            string docsPath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSnapshot", "EXPORT_CONTENTS.md");
            string ignorePath = Path.Combine(Directory.GetCurrentDirectory(), ".gitignore");

            Assert.IsTrue(File.Exists(scriptPath), "Chat review export script should exist.");
            Assert.IsTrue(File.Exists(legacyScriptPath), "Legacy project snapshot script should exist.");
            Assert.IsTrue(File.Exists(docsPath), "Export contents documentation should exist.");
            Assert.IsTrue(File.Exists(ignorePath), ".gitignore should exist.");

            string script = File.ReadAllText(scriptPath);
            StringAssert.Contains("ProjectSnapshot/AI_CONTEXT_EXPORT.zip", script);
            StringAssert.Contains("Assets/Game", script);
            StringAssert.Contains("ProjectSnapshot", script);
            StringAssert.Contains("Packages/manifest.json", script);
            StringAssert.Contains("README.md", script);
            StringAssert.Contains(".gitignore", script);
            StringAssert.Contains(".fbx", script);
            StringAssert.Contains(".png", script);
            StringAssert.Contains("Library", script);
            StringAssert.Contains("UserSettings", script);

            string legacyScript = File.ReadAllText(legacyScriptPath);
            StringAssert.Contains("GenerateChatReviewExport.ps1", legacyScript);
            StringAssert.Contains("RefreshGeneratedIndexes", legacyScript);
            Assert.IsFalse(legacyScript.Contains("README_FOR_CHATGPT.md\""), "Legacy export wrapper should not overwrite curated README docs by default.");

            string docs = File.ReadAllText(docsPath);
            StringAssert.Contains("AI_CONTEXT_EXPORT.zip", docs);
            StringAssert.Contains("Assets/Game/Runtime/**/*.cs", docs);
            StringAssert.Contains("Excluded", docs);

            string ignore = File.ReadAllText(ignorePath);
            StringAssert.Contains("ProjectSnapshot/CHAT_REVIEW_EXPORT.zip", ignore);
            StringAssert.Contains("ProjectSnapshot/AI_CONTEXT_EXPORT.zip", ignore);
            StringAssert.Contains("ProjectSnapshot/*_EXPORT.zip", ignore);
            StringAssert.Contains("ProjectSnapshot/*.zip", ignore);
            StringAssert.Contains("ProjectSnapshot/_chat_review_export_tmp/", ignore);
            StringAssert.Contains("ProjectSnapshot/_ai_context_export_tmp/", ignore);
        }

        [Test]
        public void ProjectSnapshotDocs_IncludeVisionBacklogRoadmapAndHandoff()
        {
            string snapshotPath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSnapshot");
            string[] requiredDocs =
            {
                "CHATGPT_HANDOFF.md",
                "GAME_VISION.md",
                "DESIGN_BACKLOG.md",
                "IMPLEMENTED_SYSTEMS.md",
                "NOT_YET_IMPLEMENTED.md",
                "ROADMAP.md",
                "ASSET_IMPORT_GUIDE.md",
                "README_FOR_CHATGPT.md"
            };

            foreach (string requiredDoc in requiredDocs)
            {
                string fullPath = Path.Combine(snapshotPath, requiredDoc);
                Assert.IsTrue(File.Exists(fullPath), $"Expected ProjectSnapshot doc to exist: {requiredDoc}");
            }

            string handoff = File.ReadAllText(Path.Combine(snapshotPath, "CHATGPT_HANDOFF.md"));
            StringAssert.Contains("AI_CONTEXT_EXPORT.zip", handoff);
            StringAssert.Contains("What Not To Touch", handoff);

            string vision = File.ReadAllText(Path.Combine(snapshotPath, "GAME_VISION.md"));
            StringAssert.Contains("Going down is progression", vision);
            StringAssert.Contains("Going up is escalation", vision);
        }
    }
}
