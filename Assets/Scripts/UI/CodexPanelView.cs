using System.Text;
using Lingjuan.Audio;
using Lingjuan.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>图鉴面板，Main 与 Level 共用；区域签仅浏览。</summary>
    public class CodexPanelView : MonoBehaviour
    {
        public GameDatabase database;
        public Button[] regionButtons;
        public Button previousButton;
        public Button nextButton;
        public Image nameImage;
        public Text nameText;
        public Image portraitImage;
        [SerializeField] private BeastPortrait[] beastPortraits;
        public Text fragmentTitleText;
        public Text contentText;
        public Button blankCloseButton;

        [Header("区域签选中效果")]
        [Tooltip("当前选中卷的区域签放大倍数")]
        public float selectedScale = 1.08f;

        private int _regionIndex;
        private int _beastIndex;
        private string _defaultRegionId;
        private Vector3[] _baseScales;
        private Vector3[] _scaleTargets;

        private void Awake()
        {
            if (regionButtons != null)
            {
                _baseScales = new Vector3[regionButtons.Length];
                _scaleTargets = new Vector3[regionButtons.Length];

                for (int i = 0; i < regionButtons.Length; i++)
                {
                    int index = i;
                    if (regionButtons[i] != null)
                    {
                        regionButtons[i].onClick.AddListener(() => SelectRegion(index));
                        _baseScales[i] = regionButtons[i].transform.localScale;
                    }
                    else
                    {
                        _baseScales[i] = Vector3.one;
                    }

                    _scaleTargets[i] = _baseScales[i];
                }
            }

            if (previousButton != null)
            {
                previousButton.onClick.AddListener(() => StepBeast(-1));
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(() => StepBeast(1));
            }

            if (blankCloseButton != null)
            {
                blankCloseButton.onClick.AddListener(Close);
            }
        }

        public void SetDefaultRegion(string regionId)
        {
            _defaultRegionId = regionId;
        }

        public void Open(string regionId)
        {
            if (database != null)
            {
                database.BuildIndex();
            }

            string target = string.IsNullOrEmpty(regionId) ? _defaultRegionId : regionId;
            _regionIndex = Mathf.Max(0, FindRegionIndex(target));
            _beastIndex = 0;
            Refresh();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            AudioManager.PlaySfx(SfxId.PageClose);
            UiPanelAnimator.Hide(gameObject);
        }

        private int FindRegionIndex(string regionId)
        {
            if (database == null || database.Regions == null)
            {
                return 0;
            }

            for (int i = 0; i < database.Regions.Count; i++)
            {
                RegionDefinition region = database.Regions[i];
                if (region != null && region.regionId == regionId)
                {
                    return i;
                }
            }

            return 0;
        }

        private void SelectRegion(int index)
        {
            AudioManager.PlaySfx(SfxId.UiClick);
            _regionIndex = index;
            _beastIndex = 0;
            Refresh();
        }

        private void StepBeast(int delta)
        {
            RegionDefinition region = GetCurrentRegion();
            if (region == null || region.beasts == null || region.beasts.Count == 0)
            {
                return;
            }

            AudioManager.PlaySfx(SfxId.UiClick);
            _beastIndex = (_beastIndex + delta + region.beasts.Count) % region.beasts.Count;
            Refresh();
        }

        private RegionDefinition GetCurrentRegion()
        {
            if (database == null || database.Regions == null || database.Regions.Count == 0)
            {
                return null;
            }

            _regionIndex = Mathf.Clamp(_regionIndex, 0, database.Regions.Count - 1);
            return database.Regions[_regionIndex];
        }

        private void Refresh()
        {
            RegionDefinition region = GetCurrentRegion();
            UpdateScaleTargets();

            if (region == null || region.beasts == null || region.beasts.Count == 0)
            {
                return;
            }

            _beastIndex = Mathf.Clamp(_beastIndex, 0, region.beasts.Count - 1);
            BeastDefinition beast = region.beasts[_beastIndex];
            if (beast == null)
            {
                return;
            }

            bool sketched = PlayerProgress.IsSketchDone(beast.beastId);
            bool named = PlayerProgress.IsNamed(beast.beastId);

            if (nameImage != null)
            {
                nameImage.sprite = beast.nameSprite;
                nameImage.enabled = named && beast.nameSprite != null;
            }

            if (nameText != null)
            {
                nameText.text = named ? beast.displayName : "？？？";
                nameText.enabled = !named || beast.nameSprite == null;
            }

            ShowPortrait(beast, sketched);

            int fragmentCount = PlayerProgress.GetFragmentCount(beast);

            if (fragmentTitleText != null)
            {
                fragmentTitleText.text = "残页内容 " + fragmentCount + "/" +
                                          (beast.fragments != null ? beast.fragments.Count : 0);
            }

            if (contentText != null)
            {
                contentText.text = BuildContent(beast);
            }
        }

        private void ShowPortrait(BeastDefinition beast, bool sketched)
        {
            Image selected = null;
            if (beastPortraits != null)
            {
                foreach (BeastPortrait entry in beastPortraits)
                {
                    if (entry == null || entry.image == null)
                    {
                        continue;
                    }

                    bool visible = selected == null && entry.beast == beast;
                    entry.image.gameObject.SetActive(visible);
                    if (visible)
                    {
                        selected = entry.image;
                    }
                }
            }

            if (portraitImage != null)
            {
                portraitImage.enabled = selected == null;
                if (selected == null)
                {
                    selected = portraitImage;
                    selected.sprite = beast.portraitSprite != null ? beast.portraitSprite : beast.codexSprite;
                }
            }

            if (selected != null)
            {
                selected.color = sketched ? Color.white : new Color(0.14f, 0.14f, 0.17f, 1f);
            }
        }

        [System.Serializable]
        private sealed class BeastPortrait
        {
            public BeastDefinition beast = null;
            public Image image = null;
        }

        private void UpdateScaleTargets()
        {
            if (regionButtons == null || _baseScales == null)
            {
                return;
            }

            for (int i = 0; i < regionButtons.Length && i < _baseScales.Length; i++)
            {
                _scaleTargets[i] = _baseScales[i] * (i == _regionIndex ? selectedScale : 1f);
            }
        }

        private void Update()
        {
            if (regionButtons == null || _scaleTargets == null)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime);

            for (int i = 0; i < regionButtons.Length && i < _scaleTargets.Length; i++)
            {
                if (regionButtons[i] != null)
                {
                    Transform tr = regionButtons[i].transform;
                    tr.localScale = Vector3.Lerp(tr.localScale, _scaleTargets[i], t);
                }
            }
        }

        private static string BuildContent(BeastDefinition beast)
        {
            if (beast.fragments == null || beast.fragments.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < beast.fragments.Count; i++)
            {
                if (!PlayerProgress.HasFragment(beast.beastId, i))
                {
                    continue;
                }

                FragmentData fragment = beast.fragments[i];
                if (fragment == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine().AppendLine();
                }

                if (!string.IsNullOrEmpty(fragment.title))
                {
                    builder.AppendLine(fragment.title);
                }

                builder.Append(fragment.body);
            }

            return builder.ToString();
        }
    }
}
