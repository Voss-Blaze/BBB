using UnityEngine;

namespace Lingjuan.Audio
{
    /// <summary>集中引用全部音频资源，运行时从 Resources 加载。</summary>
    [CreateAssetMenu(menuName = "灵卷绘形/音频库", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [Header("背景音乐")]
        public AudioClip bgmMain;
        public AudioClip bgmExplore;

        [Header("通用界面")]
        public AudioClip uiClick;
        public AudioClip error;

        [Header("探索与写生")]
        public AudioClip discover;
        public AudioClip countdown;
        public AudioClip judgeSuccess;
        public AudioClip judgeFail;
        public AudioClip reward;
    }
}
