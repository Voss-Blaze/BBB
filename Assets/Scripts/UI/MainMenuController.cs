using Lingjuan.Audio;
using Lingjuan.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>Main 界面的区域入口、图鉴与设置。</summary>
    public class MainMenuController : MonoBehaviour
    {
        public GameDatabase database;
        public RegionButton[] regionButtons;
        public Button codexButton;
        public Button settingsButton;
        public GameObject codexPanel;
        public GameObject settingsPanel;
        public CodexPanelView codexView;
        public SettingsPanelView settingsView;
        public PanelGroup panelGroup;
        public Button[] resetButtons;
        public Button[] quitButtons;
        public OpeningStoryView openingStory;
        public ConfirmDialogView confirmDialog;

        private bool _storyPlaying;
        private RegionDefinition _pendingRegion;

        private void Start()
        {
            AudioManager.PlayBgm(BgmId.Main);

            if (panelGroup != null)
            {
                panelGroup.CloseAll();
            }

            if (regionButtons != null)
            {
                foreach (RegionButton regionButton in regionButtons)
                {
                    if (regionButton != null)
                    {
                        regionButton.Clicked += OnRegionClicked;
                        ButtonScaleEffect.Attach(regionButton.gameObject, 1.06f, 1.1f);
                    }
                }
            }

            if (codexButton != null)
            {
                codexButton.onClick.AddListener(OpenCodex);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OpenSettings);
            }

            ButtonScaleEffect.Attach(codexButton != null ? codexButton.gameObject : null);
            ButtonScaleEffect.Attach(settingsButton != null ? settingsButton.gameObject : null);

            BindButtons(resetButtons, RequestReset);
            BindButtons(quitButtons, RequestQuit);
            AttachScaleEffect(resetButtons);
            AttachScaleEffect(quitButtons);
        }

        private static void AttachScaleEffect(Button[] buttons)
        {
            if (buttons == null)
            {
                return;
            }

            foreach (Button button in buttons)
            {
                if (button != null)
                {
                    ButtonScaleEffect.Attach(button.gameObject, 1.05f, 1.09f);
                }
            }
        }

        private void OnDestroy()
        {
            if (regionButtons != null)
            {
                foreach (RegionButton regionButton in regionButtons)
                {
                    if (regionButton != null)
                    {
                        regionButton.Clicked -= OnRegionClicked;
                    }
                }
            }

            if (codexButton != null)
            {
                codexButton.onClick.RemoveListener(OpenCodex);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OpenSettings);
            }

            UnbindButtons(resetButtons, RequestReset);
            UnbindButtons(quitButtons, RequestQuit);
        }

        private static void BindButtons(Button[] buttons, UnityEngine.Events.UnityAction action)
        {
            if (buttons == null)
            {
                return;
            }

            foreach (Button button in buttons)
            {
                if (button != null)
                {
                    button.onClick.AddListener(action);
                }
            }
        }

        private static void UnbindButtons(Button[] buttons, UnityEngine.Events.UnityAction action)
        {
            if (buttons == null)
            {
                return;
            }

            foreach (Button button in buttons)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(action);
                }
            }
        }

        private void RequestReset()
        {
            AudioManager.PlaySfx(SfxId.UiClick);

            if (confirmDialog != null)
            {
                confirmDialog.Open("重新绘卷将清除全部探索进度，确定继续吗？", ResetProgress);
                return;
            }

            ResetProgress();
        }

        private void RequestQuit()
        {
            AudioManager.PlaySfx(SfxId.UiClick);

            if (confirmDialog != null)
            {
                confirmDialog.Open("确定要退出绘卷吗？", QuitGame);
                return;
            }

            QuitGame();
        }

        private void ResetProgress()
        {
            PlayerProgress.ClearAll();
            GameSession.Reset();
            SceneTransition.Load(GameFlow.MainScene);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnRegionClicked(RegionDefinition region)
        {
            if (region == null || _storyPlaying)
            {
                return;
            }

            AudioManager.PlaySfx(SfxId.UiClick);

            if (!PlayerProgress.IsIntroPlayed())
            {
                _storyPlaying = true;
                _pendingRegion = region;
                panelGroup?.CloseAll();

                if (openingStory != null)
                {
                    openingStory.Open(OnStoryFinished);
                    return;
                }

                OnStoryFinished();
                return;
            }

            GameFlow.EnterRegion(region);
        }

        private void OnStoryFinished()
        {
            _storyPlaying = false;
            PlayerProgress.MarkIntroPlayed();

            RegionDefinition region = _pendingRegion;
            _pendingRegion = null;

            if (region != null)
            {
                GameFlow.EnterRegion(region);
            }
        }

        private void OpenCodex()
        {
            if (codexView == null || codexPanel == null)
            {
                return;
            }

            AudioManager.PlaySfx(SfxId.PageOpen);
            codexView.Open(GameSession.GetRegionOrFallback());
            panelGroup?.Open(codexPanel);
        }

        private void OpenSettings()
        {
            if (settingsView == null || settingsPanel == null)
            {
                return;
            }

            AudioManager.PlaySfx(SfxId.PageOpen);
            settingsView.Open(false);
            panelGroup?.Open(settingsPanel);
        }
    }
}
