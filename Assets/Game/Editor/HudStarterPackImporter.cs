using UnityEditor;

namespace FrontierDepths.Editor
{
    /// <summary>
    /// Applies safe UI sprite settings to the optional DungeonHUD starter-pack assets.
    /// These files live under Resources so the runtime HUD can load them without scene wiring.
    /// </summary>
    public static class HudStarterPackImporter
    {
        public const string RootPath = "Assets/Resources/ThirdParty/DungeonHUD";

        public static void ApplyImportSettings()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { RootPath });
            for (int i = 0; i < assetGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (!importer.sRGBTexture)
                {
                    importer.sRGBTexture = true;
                    changed = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }

                if (importer.spritePixelsPerUnit != 100f)
                {
                    importer.spritePixelsPerUnit = 100f;
                    changed = true;
                }

                if (importer.textureCompression != TextureImporterCompression.Compressed)
                {
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
