using System.Collections;
using Lingjuan.Audio;
using Lingjuan.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>管理场景中的踪迹，点击后进入写生、残页与辨名流程。</summary>
    public class TraceFieldView : MonoBehaviour
    {
        public GameDatabase database;
        public LevelController levelController;
        public ToastView toastView;
        public SketchPanelView sketchView;
        public FragmentPanelView fragmentView;
        public NameQuizView quizView;
        public Button blankButton;
        [SerializeField] private Camera mapCamera;

        private TraceSpot[] _spots = System.Array.Empty<TraceSpot>();
        private EventTrigger _mapInput;
        private TraceSpot _pulsingSpot;
        private int _blankClicks;
        private bool _busy;

        [Header("点击反馈")]
        [Tooltip("点击踪迹后的放大倍数")]
        public float pulseScale = 1.25f;
        [Tooltip("点击踪迹后的放大时长（秒）")]
        public float pulseDuration = 0.22f;

        private GameConfig Config => database != null ? database.config : null;

        private void Awake()
        {
            if (blankButton == null)
            {
                return;
            }

            if (mapCamera == null)
            {
                mapCamera = Camera.main;
            }

            // 复用原地图点击区，交给 EventSystem 处理菜单遮挡和触屏输入。
            _mapInput = blankButton.gameObject.AddComponent<EventTrigger>();
            var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            click.callback.AddListener(data => OnMapClicked((PointerEventData)data));
            _mapInput.triggers.Add(click);
            var submit = new EventTrigger.Entry { eventID = EventTriggerType.Submit };
            submit.callback.AddListener(_ =>
            {
                if (blankButton.IsInteractable())
                {
                    OnBlankClicked();
                }
            });
            _mapInput.triggers.Add(submit);
        }

        private void OnDestroy()
        {
            if (_mapInput != null)
            {
                Destroy(_mapInput);
            }
        }

        public void SetRegion(Transform map)
        {
            StopAllCoroutines();
            if (_pulsingSpot != null)
            {
                _pulsingSpot.RestoreScale();
                _pulsingSpot = null;
            }

            _blankClicks = 0;
            _busy = false;
            _spots = map != null ? map.GetComponentsInChildren<TraceSpot>(true) : System.Array.Empty<TraceSpot>();
            foreach (TraceSpot spot in _spots)
            {
                BeastDefinition beast = spot.Beast;
                spot.Renderer.enabled = spot.Data != null &&
                    !PlayerProgress.HasFragment(beast.beastId, spot.FragmentIndex) &&
                    !PlayerProgress.IsTraceFound(beast.beastId, spot.FragmentIndex);
            }
        }

        private void OnMapClicked(PointerEventData pointer)
        {
            if (_busy || pointer.button != PointerEventData.InputButton.Left || !blankButton.IsInteractable())
            {
                return;
            }

            if (mapCamera != null)
            {
                Ray ray = mapCamera.ScreenPointToRay(pointer.position);
                TraceSpot hit = null;
                foreach (TraceSpot spot in _spots)
                {
                    if (spot.Hit(ray) && (hit == null || spot.Renderer.sortingOrder >= hit.Renderer.sortingOrder))
                    {
                        hit = spot;
                    }
                }

                if (hit != null)
                {
                    OnSpotClicked(new TraceEntry { beast = hit.Beast, trace = hit.Data }, hit);
                    return;
                }
            }

            OnBlankClicked();
        }

        private void OnSpotClicked(TraceEntry entry, TraceSpot spot)
        {
            if (_busy || entry.trace == null)
            {
                return;
            }

            _busy = true;
            _pulsingSpot = spot;
            _blankClicks = 0;
            AudioManager.PlaySfx(SfxId.Discover);
            StartCoroutine(PulseSpot(spot.gameObject));

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
            Vector3 to = from * Mathf.Max(0f, pulseScale);
            float duration = Mathf.Max(0.01f, pulseDuration);

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

        private IEnumerator DiscoverRoutine(TraceEntry entry, TraceSpot spot)
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
                spot.RestoreScale();
                _busy = false;
                yield break;
            }

            sketchView.Open(entry.beast, success => OnSketchFinished(entry, spot, success));
        }

        private void OnSketchFinished(TraceEntry entry, TraceSpot spot, bool success)
        {
            if (!success)
            {
                _busy = false;

                if (spot != null)
                {
                    spot.RestoreScale();
                }

                toastView?.Show("写生未成，踪迹仍在");
                return;
            }

            PlayerProgress.MarkSketchDone(entry.beast.beastId);
            PlayerProgress.SetLastFragment(entry.beast.beastId, entry.trace.fragmentIndex);
            spot.Renderer.enabled = false;

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
            if (_busy)
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

        private class TraceEntry
        {
            public BeastDefinition beast;
            public TraceData trace;
        }
    }
}
