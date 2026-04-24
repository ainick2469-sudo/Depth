namespace FrontierDepths.Core
{
    public static class GameSceneCatalog
    {
        public const string Bootstrap = "Bootstrap";
        public const string MainMenu = "MainMenu";
        public const string TownHub = "TownHub";
        public const string DungeonRuntime = "DungeonRuntime";
        public const string SandboxArtImport = "Sandbox_ArtImport";
        public const string NetPlaytest = "Net_Playtest";

        public static string GetName(GameSceneId sceneId)
        {
            return sceneId switch
            {
                GameSceneId.Bootstrap => Bootstrap,
                GameSceneId.MainMenu => MainMenu,
                GameSceneId.TownHub => TownHub,
                GameSceneId.DungeonRuntime => DungeonRuntime,
                GameSceneId.SandboxArtImport => SandboxArtImport,
                GameSceneId.NetPlaytest => NetPlaytest,
                _ => TownHub
            };
        }

        public static string GetPath(GameSceneId sceneId)
        {
            return $"Assets/Scenes/{GetName(sceneId)}.unity";
        }
    }
}
