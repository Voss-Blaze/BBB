using Lingjuan.UI;
using UnityEditor;
using UnityEngine;

namespace Lingjuan.EditorTools
{
    internal static class CodexPortraitSizeEditor
    {
        private const string PortraitMenu = "CONTEXT/RectTransform/同步此立绘尺寸到共享图鉴";
        private const string PanelMenu = "CONTEXT/CodexPanelView/同步全部立绘尺寸到共享图鉴";
        private static readonly string[] SizeProperties =
        {
            "m_SizeDelta.x", "m_SizeDelta.y",
            "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z"
        };

        [MenuItem(PortraitMenu, true)]
        private static bool CanApplyPortrait(MenuCommand command)
        {
            var rect = (RectTransform)command.context;
            CodexPanelView panel = rect.GetComponentInParent<CodexPanelView>(true);
            if (panel == null || !PrefabUtility.IsPartOfPrefabInstance(rect) || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return false;
            }

            SerializedProperty entries = new SerializedObject(panel).FindProperty("beastPortraits");
            for (int i = 0; i < entries.arraySize; i++)
            {
                if (GetPortrait(entries, i) == rect)
                {
                    return true;
                }
            }

            return false;
        }

        [MenuItem(PortraitMenu)]
        private static void ApplyPortrait(MenuCommand command)
        {
            ApplySize((RectTransform)command.context);
        }

        [MenuItem(PanelMenu, true)]
        private static bool CanApplyPanel(MenuCommand command)
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   PrefabUtility.IsPartOfPrefabInstance(command.context);
        }

        [MenuItem(PanelMenu)]
        private static void ApplyPanel(MenuCommand command)
        {
            var panel = (CodexPanelView)command.context;
            SerializedProperty entries = new SerializedObject(panel).FindProperty("beastPortraits");
            for (int i = 0; i < entries.arraySize; i++)
            {
                RectTransform rect = GetPortrait(entries, i);
                if (rect != null && PrefabUtility.IsPartOfPrefabInstance(rect))
                {
                    ApplySize(rect);
                }
            }
        }

        private static RectTransform GetPortrait(SerializedProperty entries, int index)
        {
            var image = entries.GetArrayElementAtIndex(index).FindPropertyRelative("image").objectReferenceValue
                as UnityEngine.UI.Image;
            return image != null ? image.rectTransform : null;
        }

        private static void ApplySize(RectTransform rect)
        {
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(rect);
            var serializedRect = new SerializedObject(rect);
            foreach (string name in SizeProperties)
            {
                serializedRect.Update();
                SerializedProperty property = serializedRect.FindProperty(name);
                if (property != null && property.prefabOverride)
                {
                    PrefabUtility.ApplyPropertyOverride(property, path, InteractionMode.UserAction);
                }
            }
        }
    }
}
