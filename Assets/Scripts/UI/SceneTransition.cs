using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>常驻黑幕转场：淡入黑幕 → 异步加载场景 → 等新场景渲染一帧 → 淡出黑幕。</summary>
    public class SceneTransition : MonoBehaviour
    {
        private const float FadeOutDuration = 0.22f;
        private const float FadeInDuration = 0.45f;
        private const float MinCoverDuration = 0.1f;

        private static SceneTransition _instance;

        private CanvasGroup _group;
        private bool _busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Ensure();
        }

        private static SceneTransition Ensure()
        {
            if (_instance != null)
            {
                return _instance;
            }

            var root = new GameObject("SceneTransition");
            DontDestroyOnLoad(root);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            root.AddComponent<GraphicRaycaster>();

            var cover = new GameObject("Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cover.transform.SetParent(root.transform, false);

            var rect = (RectTransform)cover.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = cover.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            _instance = root.AddComponent<SceneTransition>();
            _instance._group = group;
            return _instance;
        }

        /// <summary>立即用黑幕盖住当前画面（用于剧情结束时无缝衔接）。</summary>
        public static void Cover()
        {
            SceneTransition transition = Ensure();
            transition._group.alpha = 1f;
            transition._group.blocksRaycasts = true;
        }

        public static void Load(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            SceneTransition transition = Ensure();
            if (transition._busy)
            {
                return;
            }

            transition.StartCoroutine(transition.LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            _busy = true;
            _group.blocksRaycasts = true;
            _group.interactable = false;

            yield return FadeTo(1f, FadeOutDuration);

            float coveredAt = Time.unscaledTime;
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }

            float remain = MinCoverDuration - (Time.unscaledTime - coveredAt);
            if (remain > 0f)
            {
                yield return new WaitForSecondsRealtime(remain);
            }

            // 等新场景完成第一帧渲染，再揭开黑幕
            yield return null;

            yield return FadeTo(0f, FadeInDuration);

            _group.blocksRaycasts = false;
            _group.interactable = true;
            _busy = false;
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            float start = _group.alpha;
            if (duration <= 0f)
            {
                _group.alpha = target;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _group.alpha = target;
        }
    }
}
