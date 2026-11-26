using System;
using Lingjuan.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>二次确认弹窗：显示提问文案，确认/取消后回调。</summary>
    public class ConfirmDialogView : MonoBehaviour
    {
        public Text messageText;
        public Button confirmButton;
        public Button cancelButton;

        private Action _onConfirm;

        private void Awake()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(HandleCancel);
            }
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancel);
            }
        }

        public void Open(string message, Action onConfirm)
        {
            _onConfirm = onConfirm;

            if (messageText != null)
            {
                messageText.text = message;
            }

            UiPanelAnimator.Show(gameObject);
        }

        public void Close()
        {
            _onConfirm = null;
            UiPanelAnimator.Hide(gameObject);
        }

        private void HandleConfirm()
        {
            AudioManager.PlaySfx(SfxId.UiClick);
            Action callback = _onConfirm;
            _onConfirm = null;
            UiPanelAnimator.Hide(gameObject);
            callback?.Invoke();
        }

        private void HandleCancel()
        {
            AudioManager.PlaySfx(SfxId.PageClose);
            _onConfirm = null;
            UiPanelAnimator.Hide(gameObject);
        }
    }
}
