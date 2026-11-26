using System;
using System.Collections;
using Lingjuan.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>开场剧情：黑底白字，文字淡入淡出，点击屏幕逐句推进。</summary>
    public class OpeningStoryView : MonoBehaviour
    {
        public Image background;
        public Text label;
        public Button advanceButton;

        private const float FadeInTime = 0.6f;
        private const float FadeOutTime = 0.35f;

        private static readonly string[] Lines =
        {
            "古经有云，巫臷（zhì）之国，鸾歌凤舞，百兽相伴。",
            "不耕而食，不织而衣。",
            "你是十巫之巫朌，朌祚（bānzuò）。",
            "守此方乐土，三日一祭，赐族人谷帛。",
            "昨日祭祀，而今日神馈不至。",
            "你登坛祈问鸾凤，天雷轰然。",
            "鸾凤双双坠落在祭坛之前，躯体冰冷。",
            "你生火暖它，额头相贴，窥见灾劫：",
            "五藏经山，异兽被屠戮烹食。",
            "昆仑天罚降临，祸及巫臷。",
            "鸾凤携神谕、画卷而至。",
            "此卷能寻遍大荒受伤异兽。"
        };

        private Action _onComplete;
        private Coroutine _routine;
        private bool _advanceRequested;

        private void Awake()
        {
            if (advanceButton != null)
            {
                advanceButton.transition = Selectable.Transition.None;
                advanceButton.onClick.AddListener(RequestAdvance);
            }
        }

        private void OnDestroy()
        {
            if (advanceButton != null)
            {
                advanceButton.onClick.RemoveListener(RequestAdvance);
            }
        }

        public void Open(Action onComplete)
        {
            _onComplete = onComplete;

            if (background != null)
            {
                background.color = new Color(0f, 0f, 0f, 0f);
            }

            if (label != null)
            {
                label.text = Lines[0];
                SetTextAlpha(0f);
            }

            gameObject.SetActive(true);

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(PlayRoutine());
        }

        private void RequestAdvance()
        {
            if (!_advanceRequested)
            {
                _advanceRequested = true;
                AudioManager.PlaySfx(SfxId.UiClick);
            }
        }

        private IEnumerator PlayRoutine()
        {
            yield return FadeBackground(1f, 0.25f);

            for (int i = 0; i < Lines.Length; i++)
            {
                if (label != null)
                {
                    label.text = Lines[i];
                }

                yield return FadeText(0f, 1f, FadeInTime);

                _advanceRequested = false;
                while (!_advanceRequested)
                {
                    yield return null;
                }

                yield return FadeText(1f, 0f, FadeOutTime);
            }

            _routine = null;
            SceneTransition.Cover();
            gameObject.SetActive(false);

            Action callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        private IEnumerator FadeBackground(float to, float duration)
        {
            if (background == null)
            {
                yield break;
            }

            Color color = background.color;
            float from = color.a;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                background.color = color;
                yield return null;
            }

            color.a = to;
            background.color = color;
        }

        private IEnumerator FadeText(float from, float to, float duration)
        {
            if (label == null)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetTextAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetTextAlpha(to);
        }

        private void SetTextAlpha(float alpha)
        {
            if (label == null)
            {
                return;
            }

            Color color = label.color;
            color.a = Mathf.Clamp01(alpha);
            label.color = color;
        }
    }
}
