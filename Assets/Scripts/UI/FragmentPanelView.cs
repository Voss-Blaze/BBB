using System;
using Lingjuan.Audio;
using Lingjuan.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>残页界面：展示写生所得的残页内容，返回后进入辨名。</summary>
    public class FragmentPanelView : MonoBehaviour
    {
        public Text titleText;
        public Text contentText;
        public Button backButton;

        private Action _onClosed;

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBack);
            }
        }

        private void OnDestroy()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBack);
            }
        }

        public void Open(BeastDefinition beast, int fragmentIndex, Action onClosed)
        {
            _onClosed = onClosed;

            FragmentData fragment = null;
            if (beast != null && beast.fragments != null && fragmentIndex >= 0 && fragmentIndex < beast.fragments.Count)
            {
                fragment = beast.fragments[fragmentIndex];
            }

            string title = fragment != null ? fragment.title : "残页";
            string body = fragment != null ? fragment.body : "此页空白。";

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (contentText != null)
            {
                contentText.text = titleText != null ? body : title + "\n\n" + body;
            }

            UiPanelAnimator.Show(gameObject);
        }

        public void Close()
        {
            UiPanelAnimator.Hide(gameObject);
        }

        private void HandleBack()
        {
            AudioManager.PlaySfx(SfxId.PageClose);
            Close();
            Action callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }
    }
}
