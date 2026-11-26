using Lingjuan.Data;

namespace Lingjuan.UI
{
    /// <summary>Main 与 Level 之间的场景跳转，统一走黑幕转场。</summary>
    public static class GameFlow
    {
        public const string MainScene = "Main";
        public const string LevelScene = "Level";

        public static void EnterRegion(RegionDefinition region)
        {
            if (region == null)
            {
                return;
            }

            GameSession.SetRegion(region.regionId);
            SceneTransition.Load(LevelScene);
        }

        public static void ReturnToMain()
        {
            SceneTransition.Load(MainScene);
        }
    }
}
