using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lingjuan.Audio
{
    public enum BgmId
    {
        Main = 0,
        Explore = 1
    }

    public enum SfxId
    {
        UiClick = 0,
        PageOpen = 1,
        PageClose = 2,
        Error = 3,
        Discover = 4,
        Countdown = 5,
        JudgeSuccess = 6,
        JudgeFail = 7,
        Reward = 8,
        Escape = 9
    }

    /// <summary>常驻音频管理：BGM 循环、一次性音效、写生时压低音乐，音量跟随设置面板。</summary>
    public class AudioManager : MonoBehaviour
    {
        private const string LibraryPath = "AudioLibrary";
        private const string MusicKey = "Lingjuan.Volume.Music";
        private const string SfxKey = "Lingjuan.Volume.Sfx";
        private const float BgmBaseVolume = 0.6f;
        private const float SfxCooldown = 0.06f;
        private const float DefaultMusicVolume = 1f;
        private const float DefaultSfxVolume = 1f;

        private static AudioManager _instance;

        private AudioSource _bgm;
        private AudioSource _uiSource;
        private AudioSource _eventSource;
        private AudioLibrary _library;
        private float _musicVolume = DefaultMusicVolume;
        private float _sfxVolume = DefaultSfxVolume;
        private float _duck = 1f;
        private Coroutine _bgmFade;
        private readonly Dictionary<SfxId, float> _lastPlayed = new Dictionary<SfxId, float>();

        private static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AudioManager");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<AudioManager>();
                }

                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance == null)
            {
                var go = new GameObject("AudioManager");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<AudioManager>();
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _bgm = gameObject.AddComponent<AudioSource>();
            _bgm.loop = true;
            _bgm.playOnAwake = false;
            _bgm.spatialBlend = 0f;

            _uiSource = gameObject.AddComponent<AudioSource>();
            _uiSource.playOnAwake = false;
            _uiSource.spatialBlend = 0f;

            _eventSource = gameObject.AddComponent<AudioSource>();
            _eventSource.playOnAwake = false;
            _eventSource.spatialBlend = 0f;

            _library = Resources.Load<AudioLibrary>(LibraryPath);
            if (_library == null)
            {
                Debug.LogWarning("[灵卷绘形] 找不到 Resources/AudioLibrary.asset，音频将不会播放。");
            }

            _musicVolume = PlayerPrefs.GetFloat(MusicKey, DefaultMusicVolume);
            _sfxVolume = PlayerPrefs.GetFloat(SfxKey, DefaultSfxVolume);
            ApplyBgmVolume();
        }

        public static void PlayBgm(BgmId id)
        {
            AudioManager manager = Instance;
            AudioClip clip = manager.GetBgmClip(id);
            if (clip == null)
            {
                return;
            }

            if (manager._bgm.clip == clip && manager._bgm.isPlaying)
            {
                return;
            }

            manager.StopBgmFade();
            manager._bgmFade = manager.StartCoroutine(manager.SwitchBgm(clip));
        }

        public static void StopBgm()
        {
            AudioManager manager = Instance;
            manager.StopBgmFade();
            manager._bgmFade = manager.StartCoroutine(manager.FadeOutAndStop());
        }

        /// <summary>写生等场合压低音乐；factor 为 1 时恢复。</summary>
        public static void DuckBgm(float factor)
        {
            AudioManager manager = Instance;
            manager._duck = Mathf.Clamp01(factor);
            manager.StopBgmFade();
            manager._bgmFade = manager.StartCoroutine(manager.FadeToTarget(0.35f));
        }

        public static void SetMusicVolume(float value)
        {
            AudioManager manager = Instance;
            manager._musicVolume = Mathf.Clamp01(value);
            manager.StopBgmFade();
            manager.ApplyBgmVolume();
        }

        public static void SetSfxVolume(float value)
        {
            Instance._sfxVolume = Mathf.Clamp01(value);
        }

        public static void PlaySfx(SfxId id, float volumeScale = 1f)
        {
            AudioManager manager = Instance;
            AudioClip clip = manager.GetSfxClip(id);
            if (clip == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (manager._lastPlayed.TryGetValue(id, out float last) && now - last < SfxCooldown)
            {
                return;
            }

            manager._lastPlayed[id] = now;

            float volume = 0.85f;
            float pitch = 1f;

            switch (id)
            {
                case SfxId.UiClick:
                    volume = 0.6f;
                    break;
                case SfxId.PageOpen:
                    volume = 0.55f;
                    pitch = 0.95f;
                    break;
                case SfxId.PageClose:
                    volume = 0.5f;
                    pitch = 1.05f;
                    break;
                case SfxId.Error:
                    volume = 0.7f;
                    break;
                case SfxId.Discover:
                    volume = 0.9f;
                    break;
                case SfxId.Countdown:
                    volume = 0.7f;
                    break;
                case SfxId.JudgeSuccess:
                    volume = 0.9f;
                    break;
                case SfxId.JudgeFail:
                    volume = 0.85f;
                    break;
                case SfxId.Reward:
                    volume = 1f;
                    break;
                case SfxId.Escape:
                    volume = 1f;
                    pitch = 0.86f;
                    break;
            }

            AudioSource source = id == SfxId.JudgeSuccess || id == SfxId.JudgeFail || id == SfxId.Reward || id == SfxId.Escape
                ? manager._eventSource
                : manager._uiSource;

            source.pitch = pitch;
            source.PlayOneShot(clip, Mathf.Clamp01(manager._sfxVolume * volume * volumeScale));
        }

        private AudioClip GetBgmClip(BgmId id)
        {
            if (_library == null)
            {
                return null;
            }

            return id == BgmId.Main ? _library.bgmMain : _library.bgmExplore;
        }

        private AudioClip GetSfxClip(SfxId id)
        {
            if (_library == null)
            {
                return null;
            }

            switch (id)
            {
                case SfxId.UiClick:
                    return _library.uiClick;
                case SfxId.PageOpen:
                case SfxId.PageClose:
                    return _library.uiClick;
                case SfxId.Error:
                    return _library.error;
                case SfxId.Discover:
                    return _library.discover;
                case SfxId.Countdown:
                    return _library.countdown;
                case SfxId.JudgeSuccess:
                    return _library.judgeSuccess;
                case SfxId.JudgeFail:
                case SfxId.Escape:
                    return _library.judgeFail;
                case SfxId.Reward:
                    return _library.reward;
                default:
                    return null;
            }
        }

        private IEnumerator SwitchBgm(AudioClip clip)
        {
            if (_bgm.isPlaying)
            {
                yield return FadeBgm(0f, 0.3f);
                _bgm.Stop();
            }

            _bgm.clip = clip;
            _bgm.volume = 0f;
            _bgm.Play();
            yield return FadeBgm(TargetBgmVolume, 0.6f);
            _bgmFade = null;
        }

        private IEnumerator FadeOutAndStop()
        {
            yield return FadeBgm(0f, 0.3f);
            _bgm.Stop();
            _bgmFade = null;
        }

        private IEnumerator FadeToTarget(float duration)
        {
            yield return FadeBgm(TargetBgmVolume, duration);
            _bgmFade = null;
        }

        private IEnumerator FadeBgm(float target, float duration)
        {
            float start = _bgm.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _bgm.volume = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _bgm.volume = target;
        }

        private float TargetBgmVolume => BgmBaseVolume * _musicVolume * _duck;

        private void ApplyBgmVolume()
        {
            if (_bgm != null)
            {
                _bgm.volume = TargetBgmVolume;
            }
        }

        private void StopBgmFade()
        {
            if (_bgmFade != null)
            {
                StopCoroutine(_bgmFade);
                _bgmFade = null;
            }
        }
    }
}
