using Lingjuan.UI;
using UnityEditor;
using UnityEngine;

namespace Lingjuan.EditorTools
{
    [CustomEditor(typeof(TraceSpot))]
    [CanEditMultipleObjects]
    public sealed class TraceSpotEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (serializedObject.isEditingMultipleObjects || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorGUILayout.HelpBox("移动和缩放此物体的 Transform，即可在关卡地图上调整踪迹。", MessageType.Info);
            if (GUILayout.Button("在 Scene 中查看所属关卡"))
            {
                ShowMap(((TraceSpot)target).transform.root.gameObject);
            }
        }

        private static void ShowMap(GameObject map)
        {
            SceneVisibilityManager visibility = SceneVisibilityManager.instance;
            foreach (GameObject root in map.scene.GetRootGameObjects())
            {
                if (root != map && root.GetComponentInChildren<TraceSpot>(true) != null)
                {
                    visibility.Hide(root, true);
                }
            }

            visibility.Show(map, true);
            SceneView sceneView = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
            if (map.TryGetComponent(out SpriteRenderer background))
            {
                sceneView.Frame(background.bounds, false);
            }

            sceneView.Focus();
            sceneView.Repaint();
        }
    }
}
