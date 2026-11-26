using System.Collections;
using UnityEngine;

namespace Lingjuan.UI
{
    /// <summary>面板开合动画：内容缩放 + 整体淡入淡出；全屏底板只做淡入淡出。</summary>
    public class UiPanelAnimator : MonoBehaviour
    {
        public float openDuration = 0.18f;
        public float closeDuration = 0.12f;
        [Tooltip("关闭缩放，只做透明度渐变")]
        public bool useScale = true;

        private CanvasGroup _group;
        private Transform _content;
        private Vector3 _baseScale = Vector3.one;
        private Coroutine _routine;

        public static void Show(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            if (!panel.activeSelf)
            {
                panel.SetActive(true);
            }

            UiPanelAnimator animator = panel.GetComponent<UiPanelAnimator>();
            if (animator == null)
            {
                animator = panel.AddComponent<UiPanelAnimator>();
            }

            animator.PlayShow();
        }

        public static void Hide(GameObject panel)
        {
            if (panel == null || !panel.activeSelf)
            {
                return;
            }

            UiPanelAnimator animator = panel.GetComponent<UiPanelAnimator>();
            if (animator == null)
            {
                panel.SetActive(false);
                return;
            }

            animator.PlayHide();
        }

        private void PlayShow()
        {
            EnsureReferences();

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(ShowRoutine());
        }

        private void PlayHide()
        {
            EnsureReferences();

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(HideRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            if (_group != null)
            {
                _group.alpha = 0f;
                _group.blocksRaycasts = true;
                _group.interactable = true;
            }

            Vector3 from = _baseScale * 0.94f;
            if (useScale && _content != null)
            {
                _content.localScale = from;
            }

            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / openDuration);
                float eased = 1f - Mathf.Pow(1f - k, 3f);

                if (_group != null)
                {
                    _group.alpha = eased;
                }

                if (useScale && _content != null)
                {
                    _content.localScale = Vector3.LerpUnclamped(from, _baseScale, eased);
                }

                yield return null;
            }

            if (_group != null)
            {
                _group.alpha = 1f;
            }

            if (useScale && _content != null)
            {
                _content.localScale = _baseScale;
            }

            _routine = null;
        }

        private IEnumerator HideRoutine()
        {
            if (_group != null)
            {
                _group.blocksRaycasts = false;
            }

            Vector3 start = useScale && _content != null ? _content.localScale : _baseScale;
            Vector3 target = _baseScale * 0.97f;

            float elapsed = 0f;
            while (elapsed < closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / closeDuration);

                if (_group != null)
                {
                    _group.alpha = 1f - k;
                }

                if (useScale && _content != null)
                {
                    _content.localScale = Vector3.LerpUnclamped(start, target, k);
                }

                yield return null;
            }

            if (_group != null)
            {
                _group.alpha = 1f;
            }

            if (useScale && _content != null)
            {
                _content.localScale = _baseScale;
            }

            _routine = null;
            gameObject.SetActive(false);
        }

        private void EnsureReferences()
        {
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
                if (_group == null)
                {
                    _group = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (_content == null)
            {
                _content = FindContent();
                _baseScale = _content != null ? _content.localScale : Vector3.one;
            }
        }

        /// <summary>全屏底板不缩放，只让最大的非全屏子物体做缩放。</summary>
        private Transform FindContent()
        {
            var rect = transform as RectTransform;
            if (rect == null || !IsStretched(rect))
            {
                return transform;
            }

            Transform best = null;
            float bestArea = 0f;

            foreach (RectTransform child in rect)
            {
                if (IsStretched(child))
                {
                    continue;
                }

                float area = Mathf.Abs(child.rect.width * child.rect.height);
                if (area > bestArea)
                {
                    bestArea = area;
                    best = child;
                }
            }

            return best;
        }

        private static bool IsStretched(RectTransform rect)
        {
            return Mathf.Approximately(rect.anchorMin.x, 0f)
                && Mathf.Approximately(rect.anchorMin.y, 0f)
                && Mathf.Approximately(rect.anchorMax.x, 1f)
                && Mathf.Approximately(rect.anchorMax.y, 1f);
        }
    }
}
