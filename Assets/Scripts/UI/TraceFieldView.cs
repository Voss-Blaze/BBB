using System.Collections;
using System.Collections.Generic;
using Lingjuan.Audio;
using Lingjuan.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>地图上的踪迹探索：随机散布踪迹，点击后进入写生、残页与辨名流程。</summary>
    public class TraceFieldView : MonoBehaviour
    {
        public GameDatabase database;
        public LevelController levelController;
        public ToastView toastView;
        public SketchPanelView sketchView;
        public FragmentPanelView fragmentView;
        public NameQuizView quizView;
        public RectTransform spotRoot;
        public Button blankButton;

        private static readonly Vector2[] DefaultSlots =
        {
            new Vector2(0.18f, 0.70f),
            new Vector2(0.36f, 0.40f),
            new Vector2(0.58f, 0.72f),
            new Vector2(0.68f, 0.34f),
            new Vector2(0.30f, 0.20f),
            new Vector2(0.74f, 0.58f)
        };

        private readonly List<GameObject> _spots = new List<GameObject>();
        private static Sprite _fallbackSpot;

        private string _regionId;
        private int _blankClicks;
        private bool _busy;
        private bool _revealed = true;
        private CanvasGroup _group;
        private Coroutine _revealRoutine;

        [Header("出现节奏")]
        [Tooltip("踪迹淡入时长（秒）")]
        public float revealDuration = 0.45f;

        private GameConfig Config => database != null ? database.config : null;

        private void Awake()
        {
            if (blankButton != null)
            {
                blankButton.onClick.AddListener(OnBlankClicked);
            }
        }

        private void OnDestroy()
        {
            if (blankButton != null)
            {
                blankButton.onClick.RemoveListener(OnBlankClicked);
            }
        }

        /// <summary>切换或进入区域时重建踪迹，延迟后淡入。</summary>
        public void SetRegion(string regionId, float revealDelay)
        {
            _regionId = regionId;
            _blankClicks = 0;
            _busy = false;
            Build();

            EnsureGroup();
            _revealed = false;

            if (_group != null)
            {
                _group.alpha = 0f;
                _group.blocksRaycasts = false;
                _group.interactable = false;
            }

            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
            }

            _revealRoutine = StartCoroutine(RevealRoutine(revealDelay));
        }

        public void SetRegion(string regionId)
        {
            SetRegion(regionId, 0f);
        }

        private void EnsureGroup()
        {
            if (_group != null)
            {
                return;
            }

            Transform root = spotRoot != null ? spotRoot : transform;
            _group = root.GetComponent<CanvasGroup>();
            if (_group == null)
            {
                _group = root.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private IEnumerator RevealRoutine(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            float elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_group != null)
                {
                    _group.alpha = Mathf.Clamp01(elapsed / revealDuration);
                }

                yield return null;
            }

            if (_group != null)
            {
                _group.alpha = 1f;
                _group.blocksRaycasts = true;
                _group.interactable = true;
            }

            _revealed = true;
            _revealRoutine = null;
        }

        private void Build()
        {
            ClearSpots();

            RegionDefinition region = database != null ? database.GetRegion(_regionId) : null;
            if (region == null || region.beasts == null)
            {
                return;
            }

            var candidates = new List<TraceEntry>();
            foreach (BeastDefinition beast in region.beasts)
            {
                if (beast == null || beast.traces == null)
                {
                    continue;
                }

                foreach (TraceData trace in beast.traces)
                {
                    if (trace == null)
                    {
                        continue;
                    }

                    int fragmentIndex = trace.fragmentIndex;
                    if (PlayerProgress.HasFragment(beast.beastId, fragmentIndex) ||
                        PlayerProgress.IsTraceFound(beast.beastId, fragmentIndex))
                    {
                        continue;
                    }

                    candidates.Add(new TraceEntry { beast = beast, trace = trace });
                }
            }

            Shuffle(candidates);

            int count = Config != null ? Mathf.Max(1, Config.tracesPerEntry) : 3;
            count = Mathf.Min(count, candidates.Count);
            if (count <= 0)
            {
                return;
            }

            Vector2[] slots = region.traceSlots != null && region.traceSlots.Length > 0
                ? (Vector2[])region.traceSlots.Clone()
                : (Vector2[])DefaultSlots.Clone();
            Shuffle(slots);

            for (int i = 0; i < count; i++)
            {
                CreateSpot(candidates[i], slots[i % slots.Length]);
            }
        }

        private void CreateSpot(TraceEntry entry, Vector2 slot)
        {
            var go = new GameObject("踪迹_" + entry.beast.beastId + "_" + entry.trace.fragmentIndex,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));

            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(spotRoot != null ? spotRoot : transform, false);
            rect.anchorMin = slot;
            rect.anchorMax = slot;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(220f, 220f);

            Sprite sprite = entry.trace.spotSprite != null ? entry.trace.spotSprite : GetFallbackSpot();
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : new Color(0.95f, 0.86f, 0.62f, 0.85f);
            image.preserveAspect = true;
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => OnSpotClicked(entry, go));

            _spots.Add(go);
        }

        private void OnSpotClicked(TraceEntry entry, GameObject spot)
        {
            if (_busy || !_revealed)
            {
                return;
            }

            _busy = true;
            _blankClicks = 0;
            AudioManager.PlaySfx(SfxId.Discover);
            StartCoroutine(PulseSpot(spot));

            if (toastView != null && entry.trace != null)
            {
                toastView.Show(entry.trace.prompt);
            }

            StartCoroutine(DiscoverRoutine(entry, spot));
        }

        /// <summary>点击后踪迹微微放大，作为发现反馈。</summary>
        private IEnumerator PulseSpot(GameObject spot)
        {
            if (spot == null)
            {
                yield break;
            }

            Transform tr = spot.transform;
            Vector3 from = tr.localScale;
            Vector3 to = from * 1.25f;
            const float duration = 0.22f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - k, 2f);

                if (spot == null)
                {
                    yield break;
                }

                tr.localScale = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            if (spot != null)
            {
                tr.localScale = to;
            }
        }

        private IEnumerator DiscoverRoutine(TraceEntry entry, GameObject spot)
        {
            // 提示只停留一小会儿，随后立刻淡入写生界面
            float wait = Config != null ? Mathf.Clamp(Config.toastDuration * 0.4f, 0.6f, 1f) : 0.8f;
            yield return new WaitForSeconds(wait);

            if (!_busy)
            {
                yield break;
            }

            if (sketchView == null)
            {
                _busy = false;
                yield break;
            }

            sketchView.Open(entry.beast, success => OnSketchFinished(entry, spot, success));
        }

        private void OnSketchFinished(TraceEntry entry, GameObject spot, bool success)
        {
            if (!success)
            {
                _busy = false;

                if (spot != null)
                {
                    spot.transform.localScale = Vector3.one;
                }

                toastView?.Show("写生未成，踪迹仍在");
                return;
            }

            PlayerProgress.MarkSketchDone(entry.beast.beastId);
            PlayerProgress.SetLastFragment(entry.beast.beastId, entry.trace.fragmentIndex);
            RemoveSpot(spot);

            if (fragmentView == null)
            {
                OpenQuiz(entry);
                return;
            }

            fragmentView.Open(entry.beast, entry.trace.fragmentIndex, () => OpenQuiz(entry));
        }

        private void OpenQuiz(TraceEntry entry)
        {
            if (quizView == null)
            {
                PlayerProgress.AddFragment(entry.beast.beastId, entry.trace.fragmentIndex);
                PlayerProgress.MarkNamed(entry.beast.beastId);
                PlayerProgress.MarkTraceFound(entry.beast.beastId, entry.trace.fragmentIndex);
                _busy = false;
                return;
            }

            AudioManager.PlaySfx(SfxId.PageOpen);
            quizView.Open(entry.beast, correct =>
            {
                PlayerProgress.MarkTraceFound(entry.beast.beastId, entry.trace.fragmentIndex);

                if (correct)
                {
                    PlayerProgress.AddFragment(entry.beast.beastId, entry.trace.fragmentIndex);
                    PlayerProgress.MarkNamed(entry.beast.beastId);
                    toastView?.Show("《" + entry.beast.displayName + "》已录入图鉴");
                }
                else
                {
                    toastView?.Show("辨名有误，残页随风散去");
                }

                _busy = false;
            });
        }

        private void OnBlankClicked()
        {
            if (_busy || !_revealed)
            {
                return;
            }

            _blankClicks++;
            int threshold = Config != null ? Mathf.Max(1, Config.blankClicksForHint) : 3;
            if (_blankClicks < threshold)
            {
                return;
            }

            _blankClicks = 0;
            AudioManager.PlaySfx(SfxId.Error);
            toastView?.Show("这里无异兽踪迹");
        }

        private void RemoveSpot(GameObject spot)
        {
            if (spot == null)
            {
                return;
            }

            _spots.Remove(spot);
            Destroy(spot);
        }

        private void ClearSpots()
        {
            foreach (GameObject spot in _spots)
            {
                if (spot != null)
                {
                    Destroy(spot);
                }
            }

            _spots.Clear();
        }

        /// <summary>没有踪迹图时用一枚淡色圆点占位。</summary>
        private static Sprite GetFallbackSpot()
        {
            if (_fallbackSpot != null)
            {
                return _fallbackSpot;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    byte alpha = distance <= radius ? (byte)255 : (byte)0;
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            _fallbackSpot = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _fallbackSpot;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        private class TraceEntry
        {
            public BeastDefinition beast;
            public TraceData trace;
        }
    }
}
