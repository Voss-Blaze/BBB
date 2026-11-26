using System.Collections.Generic;
using Lingjuan.Data;
using UnityEngine;

namespace Lingjuan.UI
{
    /// <summary>玩家探索进度：发现踪迹、完成写生、辨名成功、残页入库，持久化到 PlayerPrefs。</summary>
    public static class PlayerProgress
    {
        private const string FoundKey = "Lingjuan.Progress.Found";
        private const string SketchKey = "Lingjuan.Progress.Sketched";
        private const string NamedKey = "Lingjuan.Progress.Named";
        private const string FragmentKey = "Lingjuan.Progress.Fragments";
        private const string LastBeastKey = "Lingjuan.Progress.LastBeast";
        private const string LastFragmentKey = "Lingjuan.Progress.LastFragment";
        private const string IntroKey = "Lingjuan.Progress.IntroPlayed";

        private static readonly HashSet<string> Found = new HashSet<string>();
        private static readonly HashSet<string> Sketched = new HashSet<string>();
        private static readonly HashSet<string> Named = new HashSet<string>();
        private static readonly HashSet<string> Fragments = new HashSet<string>();
        private static bool _loaded;

        private static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            Load(FoundKey, Found);
            Load(SketchKey, Sketched);
            Load(NamedKey, Named);
            Load(FragmentKey, Fragments);
        }

        public static bool IsSketchDone(string beastId)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(beastId) && Sketched.Contains(beastId);
        }

        public static bool IsNamed(string beastId)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(beastId) && Named.Contains(beastId);
        }

        public static bool IsTraceFound(string beastId, int fragmentIndex)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(beastId) && Found.Contains(TraceKey(beastId, fragmentIndex));
        }

        public static bool HasFragment(string beastId, int fragmentIndex)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(beastId) && Fragments.Contains(TraceKey(beastId, fragmentIndex));
        }

        public static int GetFragmentCount(BeastDefinition beast)
        {
            if (beast == null || beast.fragments == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < beast.fragments.Count; i++)
            {
                if (HasFragment(beast.beastId, i))
                {
                    count++;
                }
            }

            return count;
        }

        public static void MarkSketchDone(string beastId)
        {
            Store(SketchKey, Sketched, beastId);
        }

        public static void MarkNamed(string beastId)
        {
            Store(NamedKey, Named, beastId);
        }

        public static void MarkTraceFound(string beastId, int fragmentIndex)
        {
            if (string.IsNullOrEmpty(beastId))
            {
                return;
            }

            EnsureLoaded();
            if (Found.Add(TraceKey(beastId, fragmentIndex)))
            {
                Save(FoundKey, Found);
            }
        }

        public static void AddFragment(string beastId, int fragmentIndex)
        {
            if (string.IsNullOrEmpty(beastId))
            {
                return;
            }

            EnsureLoaded();
            if (Fragments.Add(TraceKey(beastId, fragmentIndex)))
            {
                Save(FragmentKey, Fragments);
            }
        }

        /// <summary>记录最近一次写生成功后展示的残页，供残页按钮回看。</summary>
        public static void SetLastFragment(string beastId, int fragmentIndex)
        {
            if (string.IsNullOrEmpty(beastId))
            {
                return;
            }

            EnsureLoaded();
            PlayerPrefs.SetString(LastBeastKey, beastId);
            PlayerPrefs.SetInt(LastFragmentKey, fragmentIndex);
            PlayerPrefs.Save();
        }

        public static bool TryGetLastFragment(out string beastId, out int fragmentIndex)
        {
            EnsureLoaded();
            beastId = PlayerPrefs.GetString(LastBeastKey, string.Empty);
            fragmentIndex = PlayerPrefs.GetInt(LastFragmentKey, 0);
            return !string.IsNullOrEmpty(beastId);
        }

        /// <summary>开场剧情是否已播放（只播一次，重新绘卷后重置）。</summary>
        public static bool IsIntroPlayed()
        {
            return PlayerPrefs.GetInt(IntroKey, 0) == 1;
        }

        public static void MarkIntroPlayed()
        {
            PlayerPrefs.SetInt(IntroKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>清空全部进度（重新绘卷）。</summary>
        public static void ClearAll()
        {
            EnsureLoaded();
            Found.Clear();
            Sketched.Clear();
            Named.Clear();
            Fragments.Clear();

            PlayerPrefs.DeleteKey(FoundKey);
            PlayerPrefs.DeleteKey(SketchKey);
            PlayerPrefs.DeleteKey(NamedKey);
            PlayerPrefs.DeleteKey(FragmentKey);
            PlayerPrefs.DeleteKey(LastBeastKey);
            PlayerPrefs.DeleteKey(LastFragmentKey);
            PlayerPrefs.DeleteKey(IntroKey);
            PlayerPrefs.Save();
        }

        private static void Store(string key, HashSet<string> set, string beastId)
        {
            if (string.IsNullOrEmpty(beastId))
            {
                return;
            }

            EnsureLoaded();
            if (set.Add(beastId))
            {
                Save(key, set);
            }
        }

        private static string TraceKey(string beastId, int fragmentIndex)
        {
            return beastId + "#" + fragmentIndex;
        }

        private static void Load(string key, HashSet<string> set)
        {
            string raw = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            foreach (string item in raw.Split(';'))
            {
                if (!string.IsNullOrEmpty(item))
                {
                    set.Add(item);
                }
            }
        }

        private static void Save(string key, HashSet<string> set)
        {
            PlayerPrefs.SetString(key, string.Join(";", set));
            PlayerPrefs.Save();
        }
    }
}
