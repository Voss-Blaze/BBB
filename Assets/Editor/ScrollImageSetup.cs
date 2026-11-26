using System.Text;
using Lingjuan.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lingjuan.EditorTools
{
    /// <summary>把 Level 画卷界面的底图 Image 绑定到 ScrollPanelView，用于按区域切换 Scroll Sprite。</summary>
    public static class ScrollImageSetup
    {
        private const string LevelPath = "Assets/Scenes/Level.unity";

        [MenuItem("灵卷绘形/绑定画卷底图")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[灵卷绘形] 请先退出 Play 模式再执行。");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
            Transform panel = FindDeep(scene, "画卷界面");
            if (panel == null)
            {
                Debug.LogError("[灵卷绘形] Level 场景找不到 画卷界面。");
                return;
            }

            ScrollPanelView view = panel.GetComponent<ScrollPanelView>();
            if (view == null)
            {
                Debug.LogError("[灵卷绘形] 画卷界面 上找不到 ScrollPanelView。");
                return;
            }

            var report = new StringBuilder();
            Image best = null;
            float bestArea = 0f;

            foreach (Image image in panel.GetComponentsInChildren<Image>(true))
            {
                RectTransform rect = image.rectTransform;
                float area = Mathf.Abs(rect.rect.width * rect.rect.height);
                report.AppendLine(image.gameObject.name + " | " + rect.rect.width + "x" + rect.rect.height
                    + " | sprite=" + (image.sprite != null ? image.sprite.name : "null"));

                if (image.transform == panel)
                {
                    continue;
                }

                if (image.sprite == null)
                {
                    continue;
                }

                if (image.gameObject.name.Contains("底图") || image.gameObject.name.Contains("背景"))
                {
                    best = image;
                    break;
                }

                if (area > bestArea)
                {
                    bestArea = area;
                    best = image;
                }
            }

            if (best == null)
            {
                Debug.LogError("[灵卷绘形] 没有找到可用的画卷底图 Image。");
                return;
            }

            view.scrollImage = best;
            EditorUtility.SetDirty(view);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[灵卷绘形] 画卷底图已绑定：" + best.gameObject.name + "\n" + report);
        }

        private static Transform FindDeep(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindDeep(root.transform, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindDeep(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
