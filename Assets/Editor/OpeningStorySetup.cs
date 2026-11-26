using Lingjuan.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lingjuan.EditorTools
{
    /// <summary>在 Main 场景搭建开场剧情界面并绑定到主菜单。</summary>
    public static class OpeningStorySetup
    {
        private const string MainPath = "Assets/Scenes/Main.unity";

        [MenuItem("灵卷绘形/搭建开场剧情并绑定")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[灵卷绘形] 请先退出 Play 模式再执行。");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
            Canvas canvas = FindCanvas(scene);
            MainMenuController controller = UnityEngine.Object.FindObjectOfType<MainMenuController>(true);
            if (canvas == null || controller == null)
            {
                Debug.LogError("[灵卷绘形] Main 场景缺少 Canvas 或 MainMenuController。");
                return;
            }

            GameObject root = canvas.transform.Find("开场剧情")?.gameObject;
            if (root == null)
            {
                root = new GameObject("开场剧情", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                root.transform.SetParent(canvas.transform, false);
            }

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();

            Image background = root.GetComponent<Image>();
            if (background == null)
            {
                background = root.AddComponent<Image>();
            }

            background.color = Color.black;
            background.raycastTarget = true;

            Button button = root.GetComponent<Button>();
            if (button == null)
            {
                button = root.AddComponent<Button>();
            }

            button.transition = Selectable.Transition.None;
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            GameObject textGo = root.transform.Find("剧情文字")?.gameObject;
            if (textGo == null)
            {
                textGo = new GameObject("剧情文字", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textGo.transform.SetParent(root.transform, false);
            }

            Text label = textGo.GetComponent<Text>();
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, 40f);
            labelRect.sizeDelta = new Vector2(1500f, 620f);
            labelRect.localScale = Vector3.one;

            Text sample = FindSampleText(canvas.transform);
            label.font = sample != null ? sample.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 40;
            label.lineSpacing = 1.6f;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;

            OpeningStoryView view = root.GetComponent<OpeningStoryView>();
            if (view == null)
            {
                view = root.AddComponent<OpeningStoryView>();
            }

            view.background = background;
            view.label = label;
            view.advanceButton = button;

            controller.openingStory = view;
            EditorUtility.SetDirty(controller);
            root.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[灵卷绘形] 开场剧情已搭建并绑定到 MainMenuController。");
        }

        private static Canvas FindCanvas(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Canvas canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                {
                    return canvas;
                }
            }

            return null;
        }

        private static Text FindSampleText(Transform canvas)
        {
            foreach (Text text in canvas.GetComponentsInChildren<Text>(true))
            {
                if (text.font != null)
                {
                    return text;
                }
            }

            return null;
        }
    }
}
