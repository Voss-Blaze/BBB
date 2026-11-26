using Lingjuan.Audio;
using Lingjuan.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>Level 画卷界面：区域切换、当前区域高亮与关闭。</summary>
    public class ScrollPanelView : MonoBehaviour
    {
        public RegionButton[] regionButtons;
        public Button backButton;
        public Button blankCloseButton;
        public LevelController levelController;
        [Tooltip("画卷底图，按当前区域切换 Scroll Sprite")]
        public Image scrollImage;

        public void SetSelectedRegion(string regionId)
        {
            ApplyScrollImage(regionId);

            if (regionButtons == null)
            {
                return;
            }

            foreach (RegionButton regionButton in regionButtons)
            {
                if (regionButton != null)
                {
                    bool selected = regionButton.region != null && regionButton.region.regionId == regionId;
                    regionButton.SetSelected(selected);
                }
            }
        }

        private void ApplyScrollImage(string regionId)
        {
            if (scrollImage == null || regionButtons == null)
            {
                return;
            }

            foreach (RegionButton regionButton in regionButtons)
            {
                if (regionButton == null || regionButton.region == null)
                {
                    continue;
                }

                if (regionButton.region.regionId == regionId && regionButton.region.scrollSprite != null)
                {
                    scrollImage.sprite = regionButton.region.scrollSprite;
                    return;
                }
            }
        }

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(Close);
            }

            if (blankCloseButton != null)
            {
                blankCloseButton.onClick.AddListener(Close);
            }

            if (regionButtons != null)
            {
                foreach (RegionButton regionButton in regionButtons)
                {
                    if (regionButton != null)
                    {
                        regionButton.Clicked += OnRegionClicked;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(Close);
            }

            if (blankCloseButton != null)
            {
                blankCloseButton.onClick.RemoveListener(Close);
            }

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
        }

        public void Close()
        {
            AudioManager.PlaySfx(SfxId.PageClose);
            UiPanelAnimator.Hide(gameObject);
        }

        private void OnRegionClicked(RegionDefinition region)
        {
            if (levelController != null && region != null)
            {
                AudioManager.PlaySfx(SfxId.UiClick);
                levelController.SwitchRegion(region.regionId);
            }
        }
    }
}
