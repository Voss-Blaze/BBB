using Lingjuan.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lingjuan.EditorTools
{
    /// <summary>在 Main 场景搭建白模确认弹窗（重新绘卷 / 退出绘卷共用）。</summary>
    public static class ConfirmDialogSetup
    {
        private const string MainPath = "Assets/Scenes/Main.unity";

        [MenuItem("灵卷绘形/搭建确认弹窗并绑定")]
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

            Font font = FindFont(canvas.transform);

            GameObject root = canvas.transform.Find("确认弹窗")?.gameObject;
            if (root == null)
            {
                root = new GameObject("确认弹窗", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                root.transform.SetParent(canvas.transform, false);
            }

            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);
            rootRect.SetAsLastSibling();

            Image dim = root.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            dim.raycastTarget = true;

            GameObject board = root.transform.Find("弹窗底板")?.gameObject;
            if (board == null)
            {
                board = new GameObject("弹窗底板", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                board.transform.SetParent(root.transform, false);
            }

            RectTransform boardRect = board.GetComponent<RectTransform>();
            boardRect.anchorMin = new Vector2(0.5f, 0.5f);
            boardRect.anchorMax = new Vector2(0.5f, 0.5f);
            boardRect.pivot = new Vector2(0.5f, 0.5f);
            boardRect.anchoredPosition = Vector2.zero;
            boardRect.sizeDelta = new Vector2(620f, 340f);
            boardRect.localScale = Vector3.one;

            Image boardImage = board.GetComponent<Image>();
            boardImage.color = new Color(0.96f, 0.94f, 0.9f, 1f);
            boardImage.raycastTarget = true;

            Text message = EnsureText(board.transform, "提示文字", font);
            RectTransform messageRect = message.rectTransform;
            messageRect.anchorMin = new Vector2(0.5f, 0.5f);
            messageRect.anchorMax = new Vector2(0.5f, 0.5f);
            messageRect.pivot = new Vector2(0.5f, 0.5f);
            messageRect.anchoredPosition = new Vector2(0f, 52f);
            messageRect.sizeDelta = new Vector2(540f, 150f);
            messageRect.localScale = Vector3.one;
            message.fontSize = 30;
            message.alignment = TextAnchor.MiddleCenter;
            message.color = new Color(0.16f, 0.14f, 0.12f);
            message.text = "确定继续吗？";

            Color buttonColor = new Color(0.85f, 0.8f, 0.7f);
            Button confirm = EnsureButton(board.transform, "确认", font, new Vector2(-140f, -92f), buttonColor);
            Button cancel = EnsureButton(board.transform, "取消", font, new Vector2(140f, -92f), buttonColor);

            ConfirmDialogView view = root.GetComponent<ConfirmDialogView>();
            if (view == null)
            {
                view = root.AddComponent<ConfirmDialogView>();
            }

            view.messageText = message;
            view.confirmButton = confirm;
            view.cancelButton = cancel;

            controller.confirmDialog = view;
            EditorUtility.SetDirty(controller);
            root.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[灵卷绘形] 确认弹窗已搭建并绑定。");
        }

        private static Text EnsureText(Transform parent, string objectName, Font font)
        {
            GameObject go = parent.Find(objectName)?.gameObject;
            if (go == null)
            {
                go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                go.transform.SetParent(parent, false);
            }

            Text text = go.GetComponent<Text>();
            text.font = font;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button EnsureButton(Transform parent, string objectName, Font font, Vector2 position, Color color)
        {
            GameObject go = parent.Find(objectName)?.gameObject;
            if (go == null)
            {
                go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
            }

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(180f, 64f);
            rect.localScale = Vector3.one;

            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            Button button = go.GetComponent<Button>();
            if (button == null)
            {
                button = go.AddComponent<Button>();
            }

            button.targetGraphic = image;

            Text label = EnsureText(go.transform, "文字", font);
            Stretch(label.rectTransform);
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.18f, 0.16f, 0.13f);
            label.text = objectName;

            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
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

        private static Font FindFont(Transform canvas)
        {
            foreach (Text text in canvas.GetComponentsInChildren<Text>(true))
            {
                if (text.font != null)
                {
                    return text.font;
                }
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
