using System;
using System.Collections;
using System.Collections.Generic;
using Lingjuan.Audio;
using Lingjuan.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>写生玩法：画笔像钟表指针一样顺时针旋转，笔尖朝外，命中本轮颜色弧线即成功。</summary>
    public class SketchPanelView : MonoBehaviour
    {
        public GameDatabase database;
        public Image panelBackground;
        public Image baseImage;
        public Image doneImage;
        public Image portraitImage;
        public Image portraitLight;
        public RectTransform pointer;
        public RectTransform arcRoot;
        public Button judgeButton;
        public Text roundText;
        public Text toleranceText;
        public Text hintText;
        public Image timerFill;

        [Header("盘心对齐")]
        [Tooltip("弧线、灰环与指针相对面板中心的偏移，按底图圆盘实际圆心测得")]
        public Vector2 diskOffset = new Vector2(-34f, -15f);

        /// <summary>指针图默认笔尖朝左下，这里换算成笔尖方向角。</summary>
        private const float PointerBaseAngle = 240.7f;
        private const float PointerPivotX = 0.8074f;
        private const float PointerPivotY = 0.9253f;
        private static readonly Vector2 PointerSize = new Vector2(441f, 494f);

        private const int ArcsPerRound = 3;
        private const float ArcDisplay = 1046f;
        private const float ArcMargin = 6f;
        private const float DegreesPerSecond = 80f;
        private const float MaxArcWidth = 20f;
        private const float MinArcWidth = 12f;

        private static readonly Dictionary<int, Sprite> ArcSprites = new Dictionary<int, Sprite>();
        private static Sprite _ringSprite;

        private static readonly Color[] RoundColors =
        {
            new Color(0.86f, 0.32f, 0.26f, 0.95f),
            new Color(0.28f, 0.52f, 0.68f, 0.95f),
            new Color(0.90f, 0.72f, 0.30f, 0.95f),
            new Color(0.52f, 0.40f, 0.66f, 0.95f)
        };

        private readonly List<Image> _arcs = new List<Image>();
        private readonly List<Vector2> _allArcs = new List<Vector2>();
        private readonly List<Vector2> _targets = new List<Vector2>();

        private BeastDefinition _beast;
        private Action<bool> _onFinished;
        private int _rounds;
        private int _roundIndex;
        private int _tolerance;
        private int _maxTolerance;
        private float _timeLeft;
        private float _speed;
        private float _angle;
        private float _arcWidth;
        private bool _playing;
        private Vector2 _appliedOffset;
        private int _lastCountdownSecond = -1;

        private GameConfig Config => database != null ? database.config : null;
        private float RoundTime => Config != null && Config.roundTime > 0f ? Config.roundTime : 15f;
        private float Buffer => Config != null ? Config.roundBuffer : 1f;

        private void Awake()
        {
            if (judgeButton != null)
            {
                judgeButton.transition = Selectable.Transition.None;
                judgeButton.onClick.AddListener(OnJudge);
            }
        }

        private void OnDestroy()
        {
            if (judgeButton != null)
            {
                judgeButton.onClick.RemoveListener(OnJudge);
            }
        }

        private void OnDisable()
        {
            _playing = false;
            StopAllCoroutines();
            AudioManager.DuckBgm(1f);
        }

        private void LateUpdate()
        {
            if (diskOffset != _appliedOffset)
            {
                _appliedOffset = diskOffset;
                ApplyDiskOffset();
            }
        }

        private void ApplyDiskOffset()
        {
            if (pointer != null)
            {
                pointer.anchoredPosition = diskOffset;
            }

            foreach (Image arc in _arcs)
            {
                if (arc != null)
                {
                    arc.rectTransform.anchoredPosition = diskOffset;
                }
            }
        }

        /// <summary>开始一次写生，结束后回调 true 表示完成。</summary>
        public void Open(BeastDefinition beast, Action<bool> onFinished)
        {
            if (beast == null)
            {
                return;
            }

            _beast = beast;
            _onFinished = onFinished;
            _rounds = beast.writeRounds > 0
                ? beast.writeRounds
                : (beast.tier == BeastTier.Advanced ? 4 : 3);

            if (Config != null)
            {
                _rounds = Mathf.Clamp(_rounds, 1, Mathf.Max(1, Config.maxRounds));
                _tolerance = Mathf.Max(1, Config.tolerance);
            }
            else
            {
                _tolerance = 2;
            }

            _roundIndex = 0;
            _maxTolerance = _tolerance;

            UiPanelAnimator animator = GetComponent<UiPanelAnimator>();
            if (animator == null)
            {
                animator = gameObject.AddComponent<UiPanelAnimator>();
            }

            animator.useScale = false;
            animator.openDuration = 0.55f;
            animator.closeDuration = 0.2f;

            UiPanelAnimator.Show(gameObject);
            AudioManager.PlaySfx(SfxId.PageOpen);
            AudioManager.DuckBgm(0.6f);

            ApplyBeastArt(beast);
            ClearArcs();
            WarmupArcSprites();

            if (baseImage != null)
            {
                baseImage.gameObject.SetActive(true);
            }

            if (doneImage != null)
            {
                doneImage.gameObject.SetActive(false);
            }

            if (pointer != null)
            {
                pointer.gameObject.SetActive(true);
            }

            if (arcRoot != null)
            {
                arcRoot.gameObject.SetActive(true);
            }

            StartCoroutine(BeginAfterReveal());
        }

        /// <summary>等淡入差不多完成再开始第一轮，避免玩家在看不见时损失时间。</summary>
        private IEnumerator BeginAfterReveal()
        {
            yield return new WaitForSecondsRealtime(0.45f);

            if (gameObject.activeSelf)
            {
                StartRound();
            }
        }

        private void ApplyBeastArt(BeastDefinition beast)
        {
            if (panelBackground != null && beast.writeBackdropSprite != null)
            {
                panelBackground.sprite = beast.writeBackdropSprite;
            }

            if (baseImage != null && beast.writeBaseSprite != null)
            {
                baseImage.sprite = beast.writeBaseSprite;
            }

            Sprite finished = beast.writeDoneSprite != null ? beast.writeDoneSprite : beast.writeFrameSprite;
            if (doneImage != null && finished != null)
            {
                doneImage.sprite = finished;
            }

            Sprite portrait = beast.portraitSprite != null ? beast.portraitSprite : beast.codexSprite;
            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.color = new Color(0.35f, 0.35f, 0.38f, 1f);
            }

            if (portraitLight != null)
            {
                portraitLight.sprite = portrait;
                portraitLight.color = Color.white;
                portraitLight.fillAmount = 0f;
            }

            if (pointer != null)
            {
                pointer.anchorMin = new Vector2(0.5f, 0.5f);
                pointer.anchorMax = new Vector2(0.5f, 0.5f);
                pointer.pivot = new Vector2(PointerPivotX, PointerPivotY);
                pointer.anchoredPosition = diskOffset;
                pointer.sizeDelta = PointerSize;
                pointer.localRotation = Quaternion.identity;
            }
        }

        private void StartRound()
        {
            _playing = false;
            _angle = UnityEngine.Random.Range(0f, 360f);
            _arcWidth = Mathf.Round(Mathf.Lerp(MaxArcWidth, MinArcWidth, Mathf.Clamp01(_roundIndex / (float)Mathf.Max(1, _rounds - 1))));
            _timeLeft = RoundTime;
            _lastCountdownSecond = -1;
            float baseFactor = Config != null ? Config.baseSpeed + Config.speedStep * _roundIndex : 1f + 0.15f * _roundIndex;
            _speed = Mathf.Max(24f, baseFactor * DegreesPerSecond);
            _targets.Clear();

            Color color = RoundColors[_roundIndex % RoundColors.Length];
            for (int i = 0; i < ArcsPerRound; i++)
            {
                float start = FindFreeStart(_arcWidth);
                CreateArc(start, _arcWidth, color);
                _targets.Add(new Vector2(start, _arcWidth));
                _allArcs.Add(new Vector2(start, _arcWidth));
            }

            _playing = true;
            UpdateHud();
            SetHint("笔尖转到同色弧线上时点击画面");
        }

        private void Update()
        {
            if (!_playing)
            {
                return;
            }

            _angle += _speed * Time.deltaTime;
            if (pointer != null)
            {
                pointer.localRotation = Quaternion.Euler(0f, 0f, -_angle);
            }

            _timeLeft -= Time.deltaTime;
            if (timerFill != null)
            {
                timerFill.fillAmount = Mathf.Clamp01(_timeLeft / RoundTime);
            }

            int seconds = Mathf.CeilToInt(Mathf.Max(0f, _timeLeft));
            if (seconds != _lastCountdownSecond)
            {
                _lastCountdownSecond = seconds;
                if (seconds >= 1 && seconds <= 3)
                {
                    AudioManager.PlaySfx(SfxId.Countdown);
                }
            }

            if (_timeLeft <= 0f)
            {
                _playing = false;
                StartCoroutine(TimeoutRoutine());
            }
        }

        /// <summary>点击画面判定笔尖是否落在本轮任意一条弧线上。</summary>
        public void OnJudge()
        {
            if (!_playing)
            {
                return;
            }

            if (IsPointerOnArc())
            {
                _playing = false;
                AudioManager.PlaySfx(SfxId.JudgeSuccess);
                StartCoroutine(RoundSuccessRoutine());
                return;
            }

            _tolerance--;
            UpdateHud();
            SetHint("笔意偏了，再稳一稳");
            AudioManager.PlaySfx(SfxId.JudgeFail);

            if (_tolerance <= 0)
            {
                _playing = false;
                StartCoroutine(FailRoutine());
            }
        }

        private bool IsPointerOnArc()
        {
            float pointerAngle = Mathf.Repeat(PointerBaseAngle - _angle, 360f);

            foreach (Vector2 target in _targets)
            {
                if (Mathf.Repeat(pointerAngle - target.x, 360f) <= target.y)
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator RoundSuccessRoutine()
        {
            SetHint("一笔已成");
            yield return StartCoroutine(ConvergeRings());

            _roundIndex++;
            if (portraitLight != null)
            {
                portraitLight.fillAmount = Mathf.Clamp01(_roundIndex / (float)_rounds);
            }

            if (_roundIndex >= _rounds)
            {
                yield return CompleteRoutine();
                yield break;
            }

            yield return new WaitForSeconds(Buffer);
            StartRound();
        }

        private IEnumerator TimeoutRoutine()
        {
            AudioManager.PlaySfx(SfxId.JudgeFail);
            _tolerance--;
            UpdateHud();
            SetHint("时辰已过，此笔重画");

            if (_tolerance <= 0)
            {
                yield return FailRoutine();
                yield break;
            }

            yield return new WaitForSeconds(0.6f);
            StartRound();
        }

        private IEnumerator CompleteRoutine()
        {
            AudioManager.PlaySfx(SfxId.Reward);
            SetHint("笔下生形，写生完成");

            if (baseImage != null)
            {
                baseImage.gameObject.SetActive(false);
            }

            if (doneImage != null)
            {
                doneImage.gameObject.SetActive(true);
            }

            if (arcRoot != null)
            {
                arcRoot.gameObject.SetActive(false);
            }

            if (pointer != null)
            {
                pointer.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(1.2f);

            Close();
            Action<bool> finished = _onFinished;
            _onFinished = null;
            finished?.Invoke(true);
        }

        private IEnumerator FailRoutine()
        {
            AudioManager.PlaySfx(SfxId.Escape);
            _playing = false;
            SetHint("笔意散乱，写生失败");
            yield return new WaitForSeconds(1f);

            Close();
            Action<bool> finished = _onFinished;
            _onFinished = null;
            finished?.Invoke(false);
        }

        private IEnumerator ConvergeRings()
        {
            Image outer = CreateRingVisual("灰环外", 1046f);
            Image inner = CreateRingVisual("灰环内", 900f);

            const float duration = 0.45f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / duration);

                if (outer != null)
                {
                    outer.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, k);
                    SetAlpha(outer, Mathf.Lerp(0.55f, 0f, k));
                }

                if (inner != null)
                {
                    inner.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.06f, 0.95f, k);
                    SetAlpha(inner, Mathf.Lerp(0.45f, 0f, k));
                }

                yield return null;
            }

            if (outer != null)
            {
                Destroy(outer.gameObject);
            }

            if (inner != null)
            {
                Destroy(inner.gameObject);
            }
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private Image CreateRingVisual(string name, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(arcRoot != null ? arcRoot : transform, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = diskOffset;
            rect.sizeDelta = new Vector2(size, size);
            rect.localScale = Vector3.one;

            var image = go.GetComponent<Image>();
            image.sprite = GetRingSprite();
            image.color = new Color(0.7f, 0.7f, 0.7f, 0.55f);
            image.raycastTarget = false;
            image.preserveAspect = false;
            return image;
        }

        private void CreateArc(float start, float width, Color color)
        {
            var go = new GameObject("弧线" + (_arcs.Count + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(arcRoot != null ? arcRoot : transform, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = diskOffset;
            rect.sizeDelta = new Vector2(ArcDisplay, ArcDisplay);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.Euler(0f, 0f, start);

            var image = go.GetComponent<Image>();
            image.sprite = GetArcSprite(Mathf.RoundToInt(width));
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = false;

            _arcs.Add(image);
        }

        /// <summary>提前把各轮弧线与灰环贴图生成好，避免游戏中点击时卡顿。</summary>
        private void WarmupArcSprites()
        {
            for (int i = 0; i < _rounds; i++)
            {
                GetArcSprite(Mathf.RoundToInt(Mathf.Lerp(MaxArcWidth, MinArcWidth, Mathf.Clamp01(i / (float)Mathf.Max(1, _rounds - 1)))));
            }

            GetRingSprite();
        }

        private static Sprite GetArcSprite(int width)
        {
            if (ArcSprites.TryGetValue(width, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }

            sprite = BuildArcSprite(width);
            ArcSprites[width] = sprite;
            return sprite;
        }

        private static Sprite GetRingSprite()
        {
            if (_ringSprite == null)
            {
                _ringSprite = BuildArcSprite(360);
            }

            return _ringSprite;
        }

        /// <summary>生成一张白色圆环弧贴图，弧从 0° 逆时针展开 width 度。</summary>
        private static Sprite BuildArcSprite(int width)
        {
            const int size = 384;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.4745f;
            float halfThickness = 6f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float radial = Mathf.Clamp01(halfThickness + 0.75f - Mathf.Abs(distance - radius));
                    byte alpha = 0;

                    if (radial > 0f)
                    {
                        float angle = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg, 360f);
                        float span = width >= 360 ? 360f : width;
                        float angular = Mathf.Clamp01(span - Mathf.Repeat(angle, 360f));

                        if (width >= 360 || angular > 0f)
                        {
                            alpha = (byte)(Mathf.Min(radial, angular) * 255f);
                        }
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>在所有已存在的弧之外找一个空闲起点，避免历史弧与本轮弧重叠。</summary>
        private float FindFreeStart(float width)
        {
            for (int attempt = 0; attempt < 240; attempt++)
            {
                float candidate = UnityEngine.Random.Range(0f, 360f);
                bool free = true;

                foreach (Vector2 arc in _allArcs)
                {
                    if (Mathf.Repeat(candidate - arc.x, 360f) < arc.y + ArcMargin ||
                        Mathf.Repeat(arc.x - candidate, 360f) < width + ArcMargin)
                    {
                        free = false;
                        break;
                    }
                }

                if (free)
                {
                    return candidate;
                }
            }

            return UnityEngine.Random.Range(0f, 360f);
        }

        private void UpdateHud()
        {
            if (roundText != null)
            {
                roundText.text = "第 " + (_roundIndex + 1) + " / " + _rounds + " 笔";
            }

            if (toleranceText != null)
            {
                var builder = new System.Text.StringBuilder("容错 ");
                for (int i = 0; i < _maxTolerance; i++)
                {
                    builder.Append(i < _tolerance ? '●' : '○');
                }

                toleranceText.text = builder.ToString();
            }
        }

        private void SetHint(string message)
        {
            if (hintText != null)
            {
                hintText.text = message;
            }
        }

        private void ClearArcs()
        {
            foreach (Image arc in _arcs)
            {
                if (arc != null)
                {
                    Destroy(arc.gameObject);
                }
            }

            _arcs.Clear();
            _allArcs.Clear();
            _targets.Clear();
        }

        private void Close()
        {
            _playing = false;
            ClearArcs();
            AudioManager.DuckBgm(1f);
            UiPanelAnimator.Hide(gameObject);
        }
    }
}
