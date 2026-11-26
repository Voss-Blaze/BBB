using Lingjuan.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>设置面板，Main 与 Level 共用；Level 打开时显示返回主页。</summary>
    public class SettingsPanelView : MonoBehaviour
    {
        private const string MasterKey = "Lingjuan.Volume.Master";
        private const string MusicKey = "Lingjuan.Volume.Music";
        private const string SfxKey = "Lingjuan.Volume.Sfx";

        public Slider masterSlider;
        public Slider musicSlider;
        public Slider sfxSlider;
        public Button homeButton;
        public Button blankCloseButton;

        private void Awake()
        {
            if (blankCloseButton != null)
            {
                blankCloseButton.onClick.AddListener(Close);
            }

            if (homeButton != null)
            {
                homeButton.onClick.AddListener(ReturnHome);
            }

            InitSlider(masterSlider, MasterKey);
            InitSlider(musicSlider, MusicKey);
            InitSlider(sfxSlider, SfxKey);

            ApplyMaster(masterSlider != null ? masterSlider.value : 1f);
            AudioManager.SetMusicVolume(musicSlider != null ? musicSlider.value : 1f);
            AudioManager.SetSfxVolume(sfxSlider != null ? sfxSlider.value : 1f);
        }

        public void Open(bool showHome)
        {
            if (homeButton != null)
            {
                homeButton.gameObject.SetActive(showHome);
            }

            gameObject.SetActive(true);
        }

        public void Close()
        {
            AudioManager.PlaySfx(SfxId.PageClose);
            UiPanelAnimator.Hide(gameObject);
        }

        private void InitSlider(Slider slider, string key)
        {
            if (slider == null)
            {
                return;
            }

            slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(key, 1f));
            slider.onValueChanged.AddListener(value => OnSliderChanged(slider, key, value));
        }

        private void OnSliderChanged(Slider slider, string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            if (slider == masterSlider)
            {
                ApplyMaster(value);
            }
            else if (slider == musicSlider)
            {
                AudioManager.SetMusicVolume(value);
            }
            else if (slider == sfxSlider)
            {
                AudioManager.SetSfxVolume(value);
            }
        }

        private static void ApplyMaster(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
        }

        private void ReturnHome()
        {
            AudioManager.PlaySfx(SfxId.UiClick);
            PlayerPrefs.Save();
            GameFlow.ReturnToMain();
        }

        private void OnDisable()
        {
            PlayerPrefs.Save();
        }
    }
}
