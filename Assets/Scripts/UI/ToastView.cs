using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>复用文字提示飘屏，显示后自动淡出。</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ToastView : MonoBehaviour
    {
        public CanvasGroup group;
        public Text label;
        public float duration = 3f;

        private Coroutine _routine;

        private void Awake()
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<Text>(true);
            }

            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        public void Show(string message)
        {
            if (label == null || group == null)
            {
                return;
            }

            label.text = message;
            gameObject.SetActive(true);

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            const float fadeIn = 0.15f;
            const float fadeOut = 0.3f;

            group.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Clamp01(elapsed / fadeIn);
                yield return null;
            }

            group.alpha = 1f;
            yield return new WaitForSeconds(duration);

            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                group.alpha = 1f - Mathf.Clamp01(elapsed / fadeOut);
                yield return null;
            }

            group.alpha = 0f;
            _routine = null;
        }
    }
}
