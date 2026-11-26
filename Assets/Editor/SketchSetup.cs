using System.Collections.Generic;
using Lingjuan.Data;
using Lingjuan.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lingjuan.EditorTools
{
    /// <summary>初始化写生、踪迹、残页与辨名的数据与 Level 场景 UI 绑定。</summary>
    public static class SketchSetup
    {
        private const string LevelPath = "Assets/Scenes/Level.unity";
        private const string DatabasePath = "Assets/Data/GameDatabase.asset";
        private const string PointerPath = "Assets/Art/UI/指针.png";
        private const string TraceFolder = "Assets/Art/UI/踪迹";

        /// <summary>每只异兽三条踪迹的图片，按残页序号 0/1/2 排列。</summary>
        private static readonly Dictionary<string, string[]> TraceFiles = new Dictionary<string, string[]>
        {
            { "bashe", new[] { "巴蛇-蛇形", "巴蛇-蛇尾", "巴蛇-蛇皮" } },
            { "biyiniao", new[] { "比翼鸟-羽毛", "比翼鸟-爪", "比翼鸟-头上的须" } },
            { "chenghuang", new[] { "乘黄-脚印", "乘黄-脚", "乘黄-尾巴" } },
            { "ershu", new[] { "耳鼠-爪子", "耳鼠-耳朵", "耳鼠-尾巴" } },
            { "jiuweihu", new[] { "九尾狐-耳朵", "九尾狐-毛", "九尾狐-尾巴" } },
            { "lushu", new[] { "蜀鹿-脚印", "蜀鹿-腿", "蜀鹿-尾巴" } },
            { "qingluan", new[] { "青鸾-爪子", "青鸾-羽毛-1", "青鸾-羽毛-2" } },
            { "wenyaoyu", new[] { "文瑶鱼-鱼鳞", "文瑶鱼-鱼翅", "文瑶鱼-鱼尾" } }
        };

        private static readonly Vector2[] Slots =
        {
            new Vector2(0.17f, 0.71f),
            new Vector2(0.36f, 0.43f),
            new Vector2(0.57f, 0.73f),
            new Vector2(0.69f, 0.36f),
            new Vector2(0.29f, 0.19f),
            new Vector2(0.75f, 0.58f)
        };

        /// <summary>清理重复/丢失脚本的组件，并把 Level 的各面板引用重新指向有效组件。</summary>
        [MenuItem("灵卷绘形/修复重复组件与引用")]
        public static void RepairBindings()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[灵卷绘形] 请先退出 Play 模式再执行。");
                return;
            }

            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            Scene scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
            Transform canvas = FindCanvas();
            LevelController controller = Object.FindObjectOfType<LevelController>(true);
            if (database == null || canvas == null || controller == null)
            {
                Debug.LogError("[灵卷绘形] Level 场景缺少数据、Canvas 或 LevelController。");
                return;
            }

            TraceFieldView trace = Dedupe<TraceFieldView>(canvas.Find("踪迹层")?.gameObject);
            SketchPanelView sketch = Dedupe<SketchPanelView>(canvas.Find("写生Panel")?.gameObject);
            FragmentPanelView fragment = Dedupe<FragmentPanelView>(canvas.Find("残页界面")?.gameObject);
            NameQuizView quiz = Dedupe<NameQuizView>(canvas.Find("异兽辨名")?.gameObject);

            if (trace != null)
            {
                trace.database = database;
                trace.levelController = controller;
                trace.toastView = controller.toastView;
                trace.sketchView = sketch;
                trace.fragmentView = fragment;
                trace.quizView = quiz;
                EditorUtility.SetDirty(trace);
            }

            if (sketch != null)
            {
                sketch.database = database;
                EditorUtility.SetDirty(sketch);
            }

            if (quiz != null)
            {
                quiz.database = database;
                EditorUtility.SetDirty(quiz);
            }

            controller.traceField = trace;
            controller.sketchView = sketch;
            controller.fragmentView = fragment;
            controller.quizView = quiz;
            EditorUtility.SetDirty(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[灵卷绘形] 重复组件清理与引用修复完成。");
        }

        private static T Dedupe<T>(GameObject go) where T : MonoBehaviour
        {
            if (go == null)
            {
                return null;
            }

            var all = new List<MonoBehaviour>(go.GetComponents<MonoBehaviour>());

            foreach (MonoBehaviour behaviour in all)
            {
                if (behaviour != null && behaviour.GetType() == typeof(MonoBehaviour))
                {
                    Object.DestroyImmediate(behaviour);
                }
            }

            var candidates = new List<T>();
            foreach (MonoBehaviour behaviour in all)
            {
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour is T typed)
                {
                    candidates.Add(typed);
                }
            }

            T keep = null;
            foreach (T candidate in candidates)
            {
                if (!string.IsNullOrEmpty(ScriptPath(candidate)))
                {
                    keep = candidate;
                    break;
                }
            }

            if (keep == null && candidates.Count > 0)
            {
                keep = candidates[0];
            }

            foreach (T candidate in candidates)
            {
                if (candidate != keep)
                {
                    Object.DestroyImmediate(candidate);
                }
            }

            if (keep == null)
            {
                keep = go.GetComponent<T>();
            }

            return keep;
        }

        private static string ScriptPath(MonoBehaviour behaviour)
        {
            var serialized = new SerializedObject(behaviour);
            SerializedProperty property = serialized.FindProperty("m_Script");
            var script = property != null ? property.objectReferenceValue as MonoScript : null;
            return script != null ? AssetDatabase.GetAssetPath(script) : string.Empty;
        }

        [MenuItem("灵卷绘形/初始化写生与踪迹玩法")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[灵卷绘形] 请先退出 Play 模式再执行。");
                return;
            }

            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError("[灵卷绘形] 找不到 " + DatabasePath);
                return;
            }

            database.BuildIndex();
            FillBeastData(database);
            FillRegionSlots(database);
            FixConfig(database);
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
            Transform canvas = FindCanvas();
            if (canvas == null)
            {
                Debug.LogError("[灵卷绘形] Level 场景找不到 Canvas。");
                return;
            }

            LevelController controller = Object.FindObjectOfType<LevelController>(true);
            SketchPanelView sketch = BuildSketchPanel(canvas, database);
            FragmentPanelView fragment = BuildFragmentPanel(canvas);
            NameQuizView quiz = BuildQuizPanel(canvas, database);
            GameObject traceLayer = BuildTraceLayer(canvas, database);

            TraceFieldView traceField = traceLayer.GetComponent<TraceFieldView>();
            traceField.sketchView = sketch;
            traceField.fragmentView = fragment;
            traceField.quizView = quiz;
            traceField.levelController = controller;
            traceField.toastView = controller != null && controller.toastView != null
                ? controller.toastView
                : Object.FindObjectOfType<ToastView>(true);

            if (controller != null)
            {
                controller.database = database;
                controller.traceField = traceField;
                controller.sketchView = sketch;
                controller.fragmentView = fragment;
                controller.quizView = quiz;
                if (controller.toastView == null)
                {
                    controller.toastView = traceField.toastView;
                }

                EditorUtility.SetDirty(controller);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[灵卷绘形] 写生与踪迹玩法初始化完成。");
        }

        private static void FillBeastData(GameDatabase database)
        {
            foreach (BeastDefinition beast in database.AllBeasts)
            {
                if (beast == null)
                {
                    continue;
                }

                string folder = "Assets/Art/" + beast.regionId + "/" + beast.beastId;
                string id = beast.beastId;

                if (beast.writeBackdropSprite == null)
                {
                    beast.writeBackdropSprite = LoadSprite(folder + "/" + id + "_bg_dark.png");
                }

                if (beast.writeBaseSprite == null)
                {
                    beast.writeBaseSprite = LoadSprite(folder + "/" + id + "_disk_base.png");
                }

                if (beast.writeDoneSprite == null)
                {
                    beast.writeDoneSprite = LoadSprite(folder + "/" + id + "_disk.png");
                }

                if (beast.writeFrameSprite == null)
                {
                    beast.writeFrameSprite = LoadSprite(folder + "/" + id + "_frame.png");
                }

                if (beast.writeRingLarge == null)
                {
                    beast.writeRingLarge = LoadSprite(folder + "/" + id + "_ring_l.png");
                }

                if (beast.writeRingMedium == null)
                {
                    beast.writeRingMedium = LoadSprite(folder + "/" + id + "_ring_m.png");
                }

                if (beast.writeRingSmall == null)
                {
                    beast.writeRingSmall = LoadSprite(folder + "/" + id + "_ring_s.png");
                }

                if (beast.portraitSprite == null)
                {
                    beast.portraitSprite = LoadSprite(folder + "/" + id + "_body.png");
                }

                if (beast.writeRounds <= 0)
                {
                    beast.writeRounds = beast.tier == BeastTier.Advanced ? 4 : 3;
                }

                Sprite[] spots = FindTraceSprites(id);
                if (spots.Length > 0 && beast.traces != null)
                {
                    for (int i = 0; i < beast.traces.Count && i < spots.Length; i++)
                    {
                        if (beast.traces[i] != null && spots[i] != null)
                        {
                            beast.traces[i].spotSprite = spots[i];
                        }
                    }
                }

                EditorUtility.SetDirty(beast);
            }
        }

        private static Sprite[] FindTraceSprites(string beastId)
        {
            if (!TraceFiles.TryGetValue(beastId, out string[] names))
            {
                return new Sprite[0];
            }

            var result = new List<Sprite>();
            foreach (string name in names)
            {
                Sprite sprite = LoadSprite(TraceFolder + "/" + name + ".png");
                if (sprite == null)
                {
                    Debug.LogWarning("[灵卷绘形] 找不到踪迹图 " + name);
                }

                result.Add(sprite);
            }

            return result.ToArray();
        }

        private static void FillRegionSlots(GameDatabase database)
        {
            foreach (RegionDefinition region in database.Regions)
            {
                if (region == null)
                {
                    continue;
                }

                if (region.traceSlots == null || region.traceSlots.Length == 0)
                {
                    region.traceSlots = (Vector2[])Slots.Clone();
                    EditorUtility.SetDirty(region);
                }
            }
        }

        private static void FixConfig(GameDatabase database)
        {
            GameConfig config = database.config;
            if (config == null)
            {
                return;
            }

            config.tracesPerEntry = 3;
            config.blankClicksForHint = 3;
            config.quizOptionCount = 3;
            if (config.roundTime <= 0f)
            {
                config.roundTime = 15f;
            }

            EditorUtility.SetDirty(config);
        }

        private static SketchPanelView BuildSketchPanel(Transform canvas, GameDatabase database)
        {
            Transform panel = canvas.Find("写生Panel");
            if (panel == null)
            {
                Debug.LogError("[灵卷绘形] 找不到 Canvas/写生Panel。");
                return null;
            }

            Image baseImage = FindImage(panel, "底图");
            Image doneImage = FindImage(panel, "完成图");
            Image portrait = FindImage(panel, "异兽立绘");

            if (portrait != null)
            {
                portrait.raycastTarget = false;
            }

            Image portraitLight = BuildPortraitLight(panel, portrait);

            GameObject arcRoot = EnsureChild(panel, "弧层");
            Stretch(arcRoot.GetComponent<RectTransform>());

            GameObject pointer = EnsureChild(panel, "指针");
            var pointerRect = pointer.GetComponent<RectTransform>();
            pointerRect.anchorMin = new Vector2(0.5f, 0.5f);
            pointerRect.anchorMax = new Vector2(0.5f, 0.5f);
            pointerRect.pivot = new Vector2(0.8074f, 0.9253f);
            pointerRect.anchoredPosition = Vector2.zero;
            pointerRect.sizeDelta = new Vector2(441f, 494f);
            pointerRect.localRotation = Quaternion.identity;

            Image pointerImage = EnsureComponent<Image>(pointer);
            pointerImage.sprite = LoadSprite(PointerPath);
            pointerImage.preserveAspect = true;
            pointerImage.raycastTarget = false;

            Font font = FindFont(panel);

            Text roundText = EnsureText(panel, "轮次文字", "第 1 / 3 笔",
                new Vector2(-620f, 468f), new Vector2(460f, 60f), TextAnchor.MiddleLeft, 46,
                new Color(0.95f, 0.86f, 0.62f, 1f), font);
            Text toleranceText = EnsureText(panel, "容错文字", "容错 ●●",
                new Vector2(620f, 468f), new Vector2(460f, 60f), TextAnchor.MiddleRight, 46,
                new Color(0.95f, 0.86f, 0.62f, 1f), font);
            Text hintText = EnsureText(panel, "提示文字", "笔尖与同色弧线重合时点击画面",
                new Vector2(0f, -452f), new Vector2(1200f, 60f), TextAnchor.MiddleCenter, 38,
                new Color(0.16f, 0.11f, 0.07f, 1f), font);

            EnsureBar(panel, "计时条底",
                new Vector2(0f, -505f), new Vector2(760f, 16f), new Color(0.16f, 0.11f, 0.07f, 0.45f));
            Image timerFill = EnsureBar(panel, "计时条",
                new Vector2(0f, -505f), new Vector2(760f, 16f), new Color(0.88f, 0.70f, 0.28f, 1f));
            if (timerFill != null)
            {
                timerFill.type = Image.Type.Filled;
                timerFill.fillMethod = Image.FillMethod.Horizontal;
                timerFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                timerFill.fillAmount = 1f;
            }

            GameObject judge = EnsureChild(panel, "判定按钮");
            Stretch(judge.GetComponent<RectTransform>());
            var judgeImage = EnsureComponent<Image>(judge);
            judgeImage.color = new Color(0f, 0f, 0f, 0f);
            judgeImage.raycastTarget = true;
            var judgeButton = EnsureComponent<Button>(judge);
            judgeButton.targetGraphic = judgeImage;
            judgeButton.transition = Selectable.Transition.None;
            judge.transform.SetAsLastSibling();

            SketchPanelView view = EnsureComponent<SketchPanelView>(panel.gameObject);
            view.database = database;
            view.panelBackground = panel.GetComponent<Image>();
            view.baseImage = baseImage;
            view.doneImage = doneImage;
            view.portraitImage = portrait;
            view.portraitLight = portraitLight;
            view.pointer = pointerRect;
            view.arcRoot = arcRoot.GetComponent<RectTransform>();
            view.judgeButton = judgeButton;
            view.roundText = roundText;
            view.toleranceText = toleranceText;
            view.hintText = hintText;
            view.timerFill = timerFill;
            EditorUtility.SetDirty(view);

            if (baseImage != null)
            {
                baseImage.gameObject.SetActive(true);
            }

            if (doneImage != null)
            {
                doneImage.gameObject.SetActive(false);
            }

            panel.gameObject.SetActive(false);
            return view;
        }

        private static Image BuildPortraitLight(Transform panel, Image portrait)
        {
            if (portrait == null)
            {
                return null;
            }

            Transform existing = panel.Find("立绘亮层");
            GameObject go = existing != null
                ? existing.gameObject
                : Object.Instantiate(portrait.gameObject, panel);

            go.name = "立绘亮层";
            var rect = go.GetComponent<RectTransform>();
            RectTransform source = portrait.rectTransform;
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.anchoredPosition = source.anchoredPosition;
            rect.sizeDelta = source.sizeDelta;
            rect.localScale = source.localScale;
            rect.localRotation = Quaternion.identity;
            rect.SetSiblingIndex(portrait.transform.GetSiblingIndex() + 1);

            var image = EnsureComponent<Image>(go);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 0f;
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = Color.white;
            return image;
        }

        private static FragmentPanelView BuildFragmentPanel(Transform canvas)
        {
            Transform root = canvas.Find("残页界面");
            if (root == null)
            {
                Debug.LogError("[灵卷绘形] 找不到 Canvas/残页界面。");
                return null;
            }

            FragmentPanelView view = EnsureComponent<FragmentPanelView>(root.gameObject);
            view.titleText = null;
            view.contentText = FindText(root, "残页内容");
            view.backButton = EnsureButtonAt(root, "返回");
            EditorUtility.SetDirty(view);

            root.gameObject.SetActive(false);
            return view;
        }

        private static NameQuizView BuildQuizPanel(Transform canvas, GameDatabase database)
        {
            Transform root = canvas.Find("异兽辨名");
            if (root == null)
            {
                Debug.LogError("[灵卷绘形] 找不到 Canvas/异兽辨名。");
                return null;
            }

            NameQuizView view = EnsureComponent<NameQuizView>(root.gameObject);
            view.database = database;
            view.nameText = FindText(root, "异兽名底图/异兽名");
            view.portraitImage = FindImage(root, "异兽立绘");
            view.startButton = EnsureButtonAt(root, "开始辨名");
            view.backButton = EnsureButtonAt(root, "返回");
            view.optionButtons = new[]
            {
                EnsureButtonAt(root, "辨名 (1)"),
                EnsureButtonAt(root, "辨名 (2)"),
                EnsureButtonAt(root, "辨名 (3)")
            };
            view.optionTexts = new[]
            {
                FindText(root, "辨名 (1)/Text (Legacy)"),
                FindText(root, "辨名 (2)/Text (Legacy)"),
                FindText(root, "辨名 (3)/Text (Legacy)")
            };
            EditorUtility.SetDirty(view);

            root.gameObject.SetActive(false);
            return view;
        }

        private static GameObject BuildTraceLayer(Transform canvas, GameDatabase database)
        {
            GameObject layer = EnsureChild(canvas, "踪迹层");
            Stretch(layer.GetComponent<RectTransform>());
            layer.transform.SetSiblingIndex(0);

            GameObject blank = EnsureChild(layer.transform, "空白点击区");
            Stretch(blank.GetComponent<RectTransform>());
            var blankImage = EnsureComponent<Image>(blank);
            blankImage.color = new Color(0f, 0f, 0f, 0f);
            blankImage.raycastTarget = true;
            var blankButton = EnsureComponent<Button>(blank);
            blankButton.targetGraphic = blankImage;
            blankButton.transition = Selectable.Transition.None;
            blank.transform.SetSiblingIndex(0);

            GameObject spotRoot = EnsureChild(layer.transform, "踪迹锚点");
            Stretch(spotRoot.GetComponent<RectTransform>());

            TraceFieldView view = EnsureComponent<TraceFieldView>(layer);
            view.database = database;
            view.spotRoot = spotRoot.GetComponent<RectTransform>();
            view.blankButton = blankButton;
            EditorUtility.SetDirty(view);

            return layer;
        }

        private static Image EnsureBar(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject go = EnsureChild(parent, name);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = EnsureComponent<Image>(go);
            if (image.sprite == null)
            {
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                image.type = Image.Type.Sliced;
            }

            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text EnsureText(
            Transform parent,
            string name,
            string content,
            Vector2 position,
            Vector2 size,
            TextAnchor anchor,
            int fontSize,
            Color color,
            Font font)
        {
            GameObject go = EnsureChild(parent, name);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = EnsureComponent<Text>(go);
            text.text = content;
            text.alignment = anchor;
            text.fontSize = fontSize;
            text.color = color;
            text.raycastTarget = false;
            if (text.font == null)
            {
                text.font = font;
            }

            return text;
        }

        private static Button EnsureButtonAt(Transform root, string path)
        {
            Transform target = root.Find(path);
            if (target == null)
            {
                Debug.LogWarning("[灵卷绘形] 找不到按钮 " + path);
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

        private static Image FindImage(Transform root, string path)
        {
            return root.Find(path)?.GetComponent<Image>();
        }

        private static Text FindText(Transform root, string path)
        {
            return root.Find(path)?.GetComponent<Text>();
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Transform FindCanvas()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            return canvas != null ? canvas.transform : null;
        }
    }
}
