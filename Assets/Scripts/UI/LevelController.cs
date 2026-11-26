using System;
using System.Collections;
using Lingjuan.Audio;
using Lingjuan.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    [Serializable]
    public class RegionBackground
    {
        public string regionId;
        public GameObject root;
    }

    /// <summary>Level 的区域状态、关卡图显隐与工具栏入口。</summary>
    public class LevelController : MonoBehaviour
    {
        public GameDatabase database;
        public RegionBackground[] backgrounds;
        public Button scrollButton;
        public Button codexButton;
        public Button settingsButton;
        public Button fragmentButton;
        public GameObject scrollPanel;
        public GameObject codexPanel;
        public GameObject settingsPanel;
        public ScrollPanelView scrollView;
        public CodexPanelView codexView;
        public SettingsPanelView settingsView;
        public PanelGroup panelGroup;
        public ToastView toastView;
        public TraceFieldView traceField;
        public SketchPanelView sketchView;
        public FragmentPanelView fragmentView;
        public NameQuizView quizView;

        public string CurrentRegionId { get; private set; }

        private void Start()
        {
            AudioManager.PlayBgm(BgmId.Explore);

            if (database != null)
            {
                database.BuildIndex();
            }

            string regionId = GameSession.GetRegionOrFallback();
            ApplyRegion(regionId, GetTraceRevealDelay(true));

            if (panelGroup != null)
            {
                panelGroup.CloseAll();
            }

            if (scrollButton != null)
            {
                scrollButton.onClick.AddListener(ToggleScroll);
            }

            if (codexButton != null)
            {
                codexButton.onClick.AddListener(OpenCodex);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OpenSettings);
            }

            if (toastView != null && database != null && database.config != null)
            {
                toastView.duration = database.config.toastDuration;
            }

            if (fragmentButton != null)
            {
                fragmentButton.onClick.AddListener(OpenLastFragment);
            }

            ButtonScaleEffect.Attach(scrollButton != null ? scrollButton.gameObject : null);
            ButtonScaleEffect.Attach(codexButton != null ? codexButton.gameObject : null);
            ButtonScaleEffect.Attach(settingsButton != null ? settingsButton.gameObject : null);
            ButtonScaleEffect.Attach(fragmentButton != null ? fragmentButton.gameObject : null);

            StartCoroutine(ShowEntryToast(regionId));
        }

        private IEnumerator ShowEntryToast(string regionId)
        {
            // 等黑幕淡出结束再提示，避免提示被遮住
            yield return new WaitForSeconds(0.7f);
            ShowToast("已进入" + GetRegionName(regionId));
        }

        private void OnDestroy()
        {
            if (scrollButton != null)
            {
                scrollButton.onClick.RemoveListener(ToggleScroll);
            }

            if (codexButton != null)
            {
                codexButton.onClick.RemoveListener(OpenCodex);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OpenSettings);
            }

            if (fragmentButton != null)
            {
                fragmentButton.onClick.RemoveListener(OpenLastFragment);
            }
        }

        public void SwitchRegion(string regionId)
        {
            if (string.IsNullOrEmpty(regionId))
            {
                return;
            }

            if (regionId == CurrentRegionId)
            {
                panelGroup?.CloseAll();
                return;
            }

            GameSession.SetRegion(regionId);
            ApplyRegion(regionId, GetTraceRevealDelay(false));
            panelGroup?.CloseAll();
            ShowToast("已进入" + GetRegionName(regionId));
        }

        public void ReturnToMain()
        {
            GameFlow.ReturnToMain();
        }

        private void ApplyRegion(string regionId, float traceRevealDelay)
        {
            CurrentRegionId = regionId;

            if (backgrounds != null)
            {
                foreach (RegionBackground background in backgrounds)
                {
                    if (background != null && background.root != null)
                    {
                        background.root.SetActive(background.regionId == regionId);
                    }
                }
            }

            scrollView?.SetSelectedRegion(regionId);
            codexView?.SetDefaultRegion(regionId);
            traceField?.SetRegion(regionId, traceRevealDelay);
        }

        /// <summary>等“已进入”飘屏完全消失后再让踪迹淡入。</summary>
        private float GetTraceRevealDelay(bool entry)
        {
            float toast = database != null && database.config != null ? database.config.toastDuration : 2f;
            return toast + 0.45f + (entry ? 0.7f : 0f);
        }

        private string GetRegionName(string regionId)
        {
            RegionDefinition region = database != null ? database.GetRegion(regionId) : null;
            return region != null ? region.displayName : regionId;
        }

        private void ToggleScroll()
        {
            if (scrollPanel == null)
            {
                return;
            }

            bool opening = !scrollPanel.activeSelf;
            AudioManager.PlaySfx(opening ? SfxId.PageOpen : SfxId.PageClose);

            if (opening)
            {
                scrollView?.SetSelectedRegion(CurrentRegionId);
            }

            panelGroup?.Toggle(scrollPanel);
        }

        private void OpenCodex()
        {
            if (codexPanel == null)
            {
                return;
            }

            AudioManager.PlaySfx(SfxId.PageOpen);
            codexView?.Open(CurrentRegionId);
            panelGroup?.Open(codexPanel);
        }

        private void OpenSettings()
        {
            if (settingsPanel == null)
            {
                return;
            }

            AudioManager.PlaySfx(SfxId.PageOpen);
            settingsView?.Open(true);
            panelGroup?.Open(settingsPanel);
        }

        /// <summary>残页按钮：回看最近一次写生成功后的残页。</summary>
        private void OpenLastFragment()
        {
            if (fragmentView == null)
            {
                return;
            }

            if (database == null || !PlayerProgress.TryGetLastFragment(out string beastId, out int fragmentIndex))
            {
                AudioManager.PlaySfx(SfxId.Error);
                ShowToast("尚无残页");
                return;
            }

            BeastDefinition beast = database.GetBeast(beastId);
            if (beast == null)
            {
                AudioManager.PlaySfx(SfxId.Error);
                ShowToast("尚无残页");
                return;
            }

            AudioManager.PlaySfx(SfxId.PageOpen);
            panelGroup?.CloseAll();
            fragmentView.Open(beast, fragmentIndex, null);
        }

        private void ShowToast(string message)
        {
            toastView?.Show(message);
        }
    }
}
