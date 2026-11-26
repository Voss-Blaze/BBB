using UnityEngine;

namespace Lingjuan.UI
{
    /// <summary>保证画卷、图鉴、设置同时只显示一个。</summary>
    public class PanelGroup : MonoBehaviour
    {
        public GameObject[] panels;

        public void CloseAll()
        {
            if (panels == null)
            {
                return;
            }

            foreach (GameObject panel in panels)
            {
                if (panel != null)
                {
                    UiPanelAnimator.Hide(panel);
                }
            }
        }

        public void Open(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            CloseAll();
            UiPanelAnimator.Show(target);
        }

        public void Toggle(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            bool wasActive = target.activeSelf;
            CloseAll();

            if (!wasActive)
            {
                UiPanelAnimator.Show(target);
            }
        }

        public bool IsOpen(GameObject target)
        {
            return target != null && target.activeSelf;
        }
    }
}
