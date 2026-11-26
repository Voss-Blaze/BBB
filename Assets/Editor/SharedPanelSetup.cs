using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lingjuan.Data;
using Lingjuan.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lingjuan.EditorTools
{
    /// <summary>把 Main 的图鉴/设置提取为共享预制体，并完成 Main/Level 的场景绑定。</summary>
    public static class SharedPanelSetup
    {
        private const string MainPath = "Assets/Scenes/Main.unity";
        private const string LevelPath = "Assets/Scenes/Level.unity";
        private const string CodexPath = "Assets/Prefabs/UI/CodexPanel.prefab";
        private const string SettingsPath = "Assets/Prefabs/UI/SettingPanel.prefab";

        [MenuItem("灵卷绘形/初始化共享面板与场景绑定")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[灵卷绘形] 请先退出 Play 模式再执行。");
                return;
            }

            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset");
            if (database == null)
            {
                Debug.LogError("[灵卷绘形] 找不到 Assets/Data/GameDatabase.asset。");
                return;
            }

            database.BuildIndex();
            EnsureFolder("Assets/Prefabs/UI");
            EnsureRegionPrefabs(database);

            Scene mainScene = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
            Transform mainCanvas = FindCanvas();
            if (mainCanvas == null)
            {
                Debug.LogError("[灵卷绘形] Main 场景找不到 Canvas。");
                return;
            }

            GameObject codex = EnsureSharedPanel(mainCanvas, "Codex", CodexPath, database, PrepareCodex);
            GameObject settings = EnsureSharedPanel(mainCanvas, "SettingPanel", SettingsPath, database, PrepareSettings);
            if (codex == null || settings == null)
            {
                return;
            }

            GameObject mainControllerGo = EnsureChild(mainCanvas, "MainMenuController");
            MainMenuController mainController = EnsureComponent<MainMenuController>(mainControllerGo);
            PanelGroup mainGroup = EnsureComponent<PanelGroup>(mainControllerGo);

            mainController.database = database;
            mainController.regionButtons = FindRegionButtons(mainScene);
            mainController.codexButton = FindButton(mainCanvas, "Main/图鉴按钮");
            mainController.settingsButton = FindButton(mainCanvas, "Main/设置按钮");
            mainController.codexPanel = codex;
            mainController.settingsPanel = settings;
            mainController.codexView = codex.GetComponent<CodexPanelView>();
            mainController.settingsView = settings.GetComponent<SettingsPanelView>();
            mainController.panelGroup = mainGroup;
            mainGroup.panels = new[] { codex, settings };

            codex.SetActive(false);
            settings.SetActive(false);
            EditorSceneManager.MarkSceneDirty(mainScene);
            EditorSceneManager.SaveScene(mainScene);

            Scene levelScene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
            Transform levelCanvas = FindCanvas();
            if (levelCanvas == null)
            {
                Debug.LogError("[灵卷绘形] Level 场景找不到 Canvas。");
                return;
            }

            GameObject levelCodex = EnsurePanelInstance(levelCanvas, "CodexPanel", CodexPath);
            GameObject levelSettings = EnsurePanelInstance(levelCanvas, "SettingPanel", SettingsPath);
            if (levelCodex == null || levelSettings == null)
            {
                return;
            }

            GameObject scrollPanel = levelCanvas.Find("画卷界面")?.gameObject;
            if (scrollPanel == null)
            {
                Debug.LogError("[灵卷绘形] Level 场景找不到 Canvas/画卷界面。");
                return;
            }

            ScrollPanelView scrollView = EnsureComponent<ScrollPanelView>(scrollPanel);
            scrollView.regionButtons = FindRegionButtons(levelScene);
            scrollView.backButton = EnsureButtonAt(scrollPanel.transform, "返回");
            scrollView.blankCloseButton = EnsureBlankClose(scrollPanel.transform, 0);

            SetActive(scrollPanel, false);
            SetActive(levelCodex, false);
            SetActive(levelSettings, false);

            GameObject levelControllerGo = EnsureChild(levelCanvas, "LevelController");
            LevelController levelController = EnsureComponent<LevelController>(levelControllerGo);
            PanelGroup levelGroup = EnsureComponent<PanelGroup>(levelControllerGo);

            levelController.database = database;
            levelController.backgrounds = FindBackgrounds(levelScene);
            levelController.scrollButton = FindButton(levelCanvas, "GameUI/画卷按钮");
            levelController.codexButton = FindButton(levelCanvas, "GameUI/图鉴按钮");
            levelController.settingsButton = FindButton(levelCanvas, "GameUI/设置按钮");
            levelController.fragmentButton = FindButton(levelCanvas, "GameUI/残页按钮");
            levelController.scrollPanel = scrollPanel;
            levelController.codexPanel = levelCodex;
            levelController.settingsPanel = levelSettings;
            levelController.scrollView = scrollView;
            levelController.codexView = levelCodex.GetComponent<CodexPanelView>();
            levelController.settingsView = levelSettings.GetComponent<SettingsPanelView>();
            levelController.panelGroup = levelGroup;
            levelController.toastView = EnsureToast(levelCanvas, database);
            levelGroup.panels = new[] { scrollPanel, levelCodex, levelSettings };
            scrollView.levelController = levelController;

            EditorSceneManager.MarkSceneDirty(levelScene);
            EditorSceneManager.SaveScene(levelScene);

            AddLevelToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[灵卷绘形] 共享面板与场景绑定完成。");
        }

        /// <summary>把 Main 的「重新绘卷 / 退出绘卷」按钮绑定到主菜单控制器。</summary>
        [MenuItem("灵卷绘形/绑定主菜单重绘与退出按钮")]
        public static void BindMainFlowButtons()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[灵卷绘形] 请先退出 Play 模式再执行。");
                return;
            }

            Scene mainScene = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
            MainMenuController controller = UnityEngine.Object.FindObjectOfType<MainMenuController>(true);
            if (controller == null)
            {
                Debug.LogError("[灵卷绘形] Main 场景找不到 MainMenuController。");
                return;
            }

            controller.resetButtons = FindNamedButtons(mainScene, "重新绘卷");
            controller.quitButtons = FindNamedButtons(mainScene, "退出绘卷");
            EditorUtility.SetDirty(controller);

            EditorSceneManager.MarkSceneDirty(mainScene);
            EditorSceneManager.SaveScene(mainScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[灵卷绘形] 主菜单按钮绑定完成：重绘 " + controller.resetButtons.Length + " 个，退出 " + controller.quitButtons.Length + " 个。");
        }

        private static Button[] FindNamedButtons(Scene scene, string objectName)
        {
            var result = new List<Button>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name != objectName)
                    {
                        continue;
                    }

                    Button button = child.GetComponent<Button>();
                    if (button != null)
                    {
                        result.Add(button);
                    }
                }
            }

            return result.ToArray();
        }

        private static GameObject EnsureSharedPanel(
            Transform canvas,
            string objectName,
            string prefabPath,
            GameDatabase database,
            Action<GameObject, GameDatabase> prepare)
        {
            GameObject existing = canvas.Find(objectName)?.gameObject;
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;

            if (!prefabExists)
            {
                if (existing == null)
                {
                    Debug.LogError("[灵卷绘形] 找不到 " + objectName + "，且没有现存预制体。");
                    return null;
                }

                prepare(existing, database);
                PrefabUtility.SaveAsPrefabAsset(existing, prefabPath);
            }
            else
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                prepare(contents, database);
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                PrefabUtility.UnloadPrefabContents(contents);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(existing) == prefabPath)
            {
                existing.SetActive(false);
                return existing;
            }

            if (prefab == null)
            {
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            instance.name = objectName;

            if (existing != null)
            {
                CopyRect(existing.GetComponent<RectTransform>(), instance.GetComponent<RectTransform>());
                instance.transform.SetSiblingIndex(existing.transform.GetSiblingIndex());
                UnityEngine.Object.DestroyImmediate(existing);
            }

            instance.SetActive(false);
            return instance;
        }

        private static GameObject EnsurePanelInstance(Transform canvas, string objectName, string prefabPath)
        {
            GameObject existing = canvas.Find(objectName)?.gameObject;
            if (existing != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(existing) == prefabPath)
            {
                existing.SetActive(false);
                return existing;
            }

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[灵卷绘形] 找不到预制体 " + prefabPath);
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            instance.name = objectName;
            instance.SetActive(false);
            return instance;
        }

        private static void PrepareCodex(GameObject root, GameDatabase database)
        {
            CodexPanelView view = EnsureComponent<CodexPanelView>(root);
            view.database = database;
            view.regionButtons = new[]
            {
                FindButton(root.transform, "山卷按钮"),
                FindButton(root.transform, "海内卷按钮"),
                FindButton(root.transform, "海外卷按钮"),
                FindButton(root.transform, "大荒卷按钮")
            };
            view.previousButton = FindButton(root.transform, "上一个Button (Legacy)");
            view.nextButton = FindButton(root.transform, "下一个Button (Legacy)");
            Transform nameRoot = root.transform.Find("异兽名底图/异兽名");
            view.nameText = nameRoot != null ? nameRoot.GetComponent<Text>() : null;
            view.nameImage = EnsureNameImage(nameRoot);
            view.portraitImage = FindImage(root.transform, "异兽立绘");
            view.fragmentTitleText = FindText(root.transform, "残页内容Title/Text (Legacy)");
            view.contentText = FindText(root.transform, "内容");
            view.blankCloseButton = EnsureBlankClose(root.transform, 1);
        }

        private static void PrepareSettings(GameObject root, GameDatabase database)
        {
            SettingsPanelView view = EnsureComponent<SettingsPanelView>(root);
            view.masterSlider = FindSlider(root.transform, "总音量Slider");
            view.musicSlider = FindSlider(root.transform, "音乐Slider");
            view.sfxSlider = FindSlider(root.transform, "音效Slider");
            view.homeButton = EnsureHomeButton(root.transform);
            view.blankCloseButton = EnsureBlankClose(root.transform, 1);
        }

        private static void EnsureRegionPrefabs(GameDatabase database)
        {
            var items = new[]
            {
                new RegionPrefab("Assets/Art/山卷.prefab", "shanjuan"),
                new RegionPrefab("Assets/Art/海内卷.prefab", "hainei"),
                new RegionPrefab("Assets/Art/海外卷.prefab", "haiwai"),
                new RegionPrefab("Assets/Art/大荒卷.prefab", "dahuang")
            };

            foreach (RegionPrefab item in items)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(item.Path);
                RegionButton button = EnsureComponent<RegionButton>(contents);
                button.region = database.GetRegion(item.RegionId);
                button.button = contents.GetComponent<Button>();
                button.labelImage = contents.GetComponent<Image>();
                PrefabUtility.SaveAsPrefabAsset(contents, item.Path);
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static RegionButton[] FindRegionButtons(Scene scene)
        {
            return UnityEngine.Object.FindObjectsOfType<RegionButton>(true)
                .Where(button => button.gameObject.scene == scene && button.region != null)
                .OrderBy(button => button.region.order)
                .ToArray();
        }

        private static RegionBackground[] FindBackgrounds(Scene scene)
        {
            var names = new[]
            {
                new RegionPrefab("山关卡", "shanjuan"),
                new RegionPrefab("海内关卡", "hainei"),
                new RegionPrefab("海外关卡", "haiwai"),
                new RegionPrefab("大荒关卡", "dahuang")
            };

            GameObject[] roots = scene.GetRootGameObjects();
            var result = new List<RegionBackground>();

            foreach (RegionPrefab item in names)
            {
                GameObject root = roots.FirstOrDefault(go => go.name == item.Path);
                if (root == null)
                {
                    Debug.LogWarning("[灵卷绘形] 找不到关卡图 " + item.Path);
                }

                result.Add(new RegionBackground { regionId = item.RegionId, root = root });
            }

            return result.ToArray();
        }

        private static ToastView EnsureToast(Transform canvas, GameDatabase database)
        {
            Transform toast = canvas.Find("GameUI/文字提示飘屏");
            if (toast == null)
            {
                Debug.LogWarning("[灵卷绘形] 找不到 GameUI/文字提示飘屏。");
                return null;
            }

            EnsureComponent<CanvasGroup>(toast.gameObject);
            ToastView view = EnsureComponent<ToastView>(toast.gameObject);
            view.group = toast.GetComponent<CanvasGroup>();

            if (view.label == null)
            {
                view.label = toast.GetComponent<Text>();
                if (view.label == null)
                {
                    view.label = toast.GetComponentInChildren<Text>(true);
                }
            }

            if (database.config != null)
            {
                view.duration = database.config.toastDuration;
            }

            return view;
        }

        private static Button EnsureBlankClose(Transform panelRoot, int siblingIndex)
        {
            Transform existing = panelRoot.Find("BlankClose");
            GameObject go;

            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("BlankClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.transform.SetParent(panelRoot, false);

                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var image = go.GetComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0f);
                image.raycastTarget = true;

                var button = go.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
            }

            go.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, panelRoot.childCount - 1));
            return go.GetComponent<Button>();
        }

        private static Button EnsureHomeButton(Transform panelRoot)
        {
            Transform existing = panelRoot.Find("返回主页");
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                Button existingButton = existing.GetComponent<Button>();
                if (existingButton == null)
                {
                    existingButton = existing.gameObject.AddComponent<Button>();
                }

                return existingButton;
            }

            var go = new GameObject("返回主页", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(panelRoot, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 90f);
            rect.sizeDelta = new Vector2(260f, 70f);

            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.92f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var textGo = new GameObject("Text (Legacy)", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(go.transform, false);

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.GetComponent<Text>();
            text.text = "返回主页";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.16f, 0.11f, 0.07f, 1f);
            text.fontSize = 32;
            text.font = FindFont(panelRoot);
            text.raycastTarget = false;

            go.SetActive(false);
            return button;
        }

        private static Font FindFont(Transform root)
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                if (text.font != null)
                {
                    return text.font;
                }
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Button EnsureButtonAt(Transform root, string name)
        {
            Transform target = root.Find(name);
            if (target == null)
            {
                Debug.LogWarning("[灵卷绘形] 找不到按钮 " + name);
                return null;
            }

            Button button = target.GetComponent<Button>();
            if (button == null)
            {
                button = target.gameObject.AddComponent<Button>();
            }

            Graphic graphic = target.GetComponent<Graphic>();
            if (graphic == null)
            {
                var image = target.gameObject.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0f);
                graphic = image;
            }

            if (button.targetGraphic == null)
            {
                button.targetGraphic = graphic;
            }

            return button;
        }

        private static Button FindButton(Transform root, string path)
        {
            return root.Find(path)?.GetComponent<Button>();
        }

        private static Image FindImage(Transform root, string path)
        {
            return root.Find(path)?.GetComponent<Image>();
        }

        /// <summary>异兽名节点是文字，这里补一个图片子节点用于显示 nameSprite，默认隐藏。</summary>
        private static Image EnsureNameImage(Transform nameRoot)
        {
            if (nameRoot == null)
            {
                return null;
            }

            Transform existing = nameRoot.Find("异兽名图片");
            if (existing != null)
            {
                return existing.GetComponent<Image>();
            }

            var go = new GameObject("异兽名图片", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(nameRoot, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.enabled = false;
            return image;
        }

        private static Text FindText(Transform root, string path)
        {
            return root.Find(path)?.GetComponent<Text>();
        }

        private static Slider FindSlider(Transform root, string path)
        {
            return root.Find(path)?.GetComponent<Slider>();
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Transform FindCanvas()
        {
            Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            return canvas != null ? canvas.transform : null;
        }

        private static void CopyRect(RectTransform from, RectTransform to)
        {
            if (from == null || to == null)
            {
                return;
            }

            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition;
            to.sizeDelta = from.sizeDelta;
            to.localScale = from.localScale;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }

        private static void AddLevelToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(scene => scene.path == LevelPath && scene.enabled))
            {
                return;
            }

            scenes.RemoveAll(scene => scene.path == LevelPath);
            scenes.Add(new EditorBuildSettingsScene(LevelPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent))
            {
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private class RegionPrefab
        {
            public readonly string Path;
            public readonly string RegionId;

            public RegionPrefab(string path, string regionId)
            {
                Path = path;
                RegionId = regionId;
            }
        }
    }
}
