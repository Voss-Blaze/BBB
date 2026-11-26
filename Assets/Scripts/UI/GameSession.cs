using UnityEngine;

namespace Lingjuan.UI
{
    /// <summary>跨场景保存当前选择的区域。</summary>
    public static class GameSession
    {
        private const string LastRegionKey = "Lingjuan.LastRegion";

        public static string SelectedRegionId { get; private set; }

        public static void SetRegion(string regionId)
        {
            if (string.IsNullOrEmpty(regionId))
            {
                return;
            }

            SelectedRegionId = regionId;
            PlayerPrefs.SetString(LastRegionKey, regionId);
            PlayerPrefs.Save();
        }

        /// <summary>清除记录的区域选择（重新绘卷）。</summary>
        public static void Reset()
        {
            SelectedRegionId = null;
            PlayerPrefs.DeleteKey(LastRegionKey);
            PlayerPrefs.Save();
        }

        public static string GetRegionOrFallback(string fallback = "shanjuan")        {
            if (!string.IsNullOrEmpty(SelectedRegionId))
            {
                return SelectedRegionId;
            }

            string saved = PlayerPrefs.GetString(LastRegionKey, string.Empty);
            if (!string.IsNullOrEmpty(saved))
            {
                SelectedRegionId = saved;
                return saved;
            }

            SelectedRegionId = fallback;
            return fallback;
        }
    }
}
