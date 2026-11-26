using System.Collections.Generic;
using System.IO;
using Lingjuan.Audio;
using UnityEditor;
using UnityEngine;

namespace Lingjuan.EditorTools
{
    /// <summary>统一音频导入设置，并生成 Resources/AudioLibrary 资源。</summary>
    public static class AudioSetup
    {
        private const string MusicDir = "Assets/Music";
        private const string ResourcesDir = "Assets/Resources";
        private const string LibraryPath = "Assets/Resources/AudioLibrary.asset";

        [MenuItem("灵卷绘形/生成音频库与导入设置")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[灵卷绘形] 请先退出 Play 模式再执行。");
                return;
            }

            if (!Directory.Exists(ResourcesDir))
            {
                Directory.CreateDirectory(ResourcesDir);
                AssetDatabase.Refresh();
            }

            var clips = new Dictionary<string, AudioClip>();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { MusicDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ConfigureImport(path);

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    clips[Path.GetFileName(path)] = clip;
                }
            }

            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            library.bgmMain = Get(clips, "山海回响.mp3");
            library.bgmExplore = Get(clips, "万兽鸣山.mp3");
            library.uiClick = Get(clips, "按钮类3－mcx20070509.wav");
            library.error = Get(clips, "打开逃跑窗口－xh20070419.wav");
            library.discover = Get(clips, "电子声象冒泡-WQ20070510.wav");
            library.countdown = Get(clips, "放大袖珍地图－xh20070419.wav");
            library.judgeSuccess = Get(clips, "U.I. Sound 0.wav");
            library.judgeFail = Get(clips, "关闭菜单1－xh20070511.wav");
            library.reward = Get(clips, "U.I. Sound 19.wav");

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[灵卷绘形] 音频库生成完成，共 " + clips.Count + " 个音频。");
        }

        private static AudioClip Get(Dictionary<string, AudioClip> clips, string fileName)
        {
            if (clips.TryGetValue(fileName, out AudioClip clip))
            {
                return clip;
            }

            Debug.LogWarning("[灵卷绘形] 找不到音频：" + fileName);
            return null;
        }

        private static void ConfigureImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                return;
            }

            bool music = path.EndsWith(".mp3", System.StringComparison.OrdinalIgnoreCase);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;

            if (music)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                importer.forceToMono = false;
                importer.loadInBackground = true;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.preloadAudioData = true;
                importer.forceToMono = true;
                importer.loadInBackground = false;
            }

            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }
}
