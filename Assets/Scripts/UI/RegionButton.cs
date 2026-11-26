using System;
using Lingjuan.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lingjuan.UI
{
    /// <summary>区域卷轴按钮，Main 与 Level 画卷共用。</summary>
    [RequireComponent(typeof(Button))]
    public class RegionButton : MonoBehaviour
    {
        public RegionDefinition region;
        public Button button;
        public Image labelImage;

        public event Action<RegionDefinition> Clicked;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (labelImage == null)
            {
                labelImage = GetComponent<Image>();
            }

            button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        public void SetSelected(bool selected)
        {
            if (labelImage == null)
            {
                return;
            }

            labelImage.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        }

        private void HandleClick()
        {
            Clicked?.Invoke(region);
        }
    }
}
