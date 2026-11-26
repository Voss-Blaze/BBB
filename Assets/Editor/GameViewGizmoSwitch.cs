using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Lingjuan.EditorTools
{
    /// <summary>
    /// Game 视图里那条「白框 + 小圆环」是编辑器自己画的 Gizmos 叠层
    /// （世界原点上的 Global Light 2D 图标 + 运行时转场 Canvas 的矩形框），
    /// 属于编辑期辅助显示，打包后的游戏里不会出现。
    /// 这里提供一键关闭 / 恢复，默认在打开工程后自动关闭一次。
    /// </summary>
    [InitializeOnLoad]
    public static class GameViewGizmoSwitch
    {
        private const string PrefKey = "Lingjuan.GameViewGizmoOff";
        private const string SessionKey = "Lingjuan.GameViewGizmoApplied";

        static GameViewGizmoSwitch()
        {
            EditorApplication.delayCall += AutoApplyOnce;
        }

        private static void AutoApplyOnce()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);

            if (!EditorPrefs.GetBool(PrefKey, true))
            {
                return;
            }

            if (SetGameViewGizmos(false))
            {
                Debug.Log("[Gizmo] 已关闭 Game 视图的 Gizmos 叠加层（白色框线即由此产生，属编辑期显示，不影响打包）。" +
                          "如需恢复：菜单 Tools/显示 Game 视图 Gizmos。");
            }
        }

        [MenuItem("Tools/隐藏 Game 视图 Gizmos", false, 900)]
        private static void HideGizmos()
        {
            if (SetGameViewGizmos(false))
            {
                EditorPrefs.SetBool(PrefKey, true);
            }
        }

        [MenuItem("Tools/显示 Game 视图 Gizmos", false, 901)]
        private static void ShowGizmos()
        {
            if (SetGameViewGizmos(true))
            {
                EditorPrefs.SetBool(PrefKey, false);
            }
        }

        /// <summary>通过反射改写 GameView 里名字带 gizmo 的 bool 字段（m_ShowGizmos / m_Gizmos）。</summary>
        private static bool SetGameViewGizmos(bool value)
        {
            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null)
            {
                return false;
            }

            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(gameViewType);
            bool touched = false;

            for (int i = 0; i < windows.Length; i++)
            {
                EditorWindow window = windows[i] as EditorWindow;
                if (window == null)
                {
                    continue;
                }

                FieldInfo[] fields = gameViewType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int f = 0; f < fields.Length; f++)
                {
                    FieldInfo field = fields[f];
                    if (field.FieldType != typeof(bool))
                    {
                        continue;
                    }

                    if (field.Name.IndexOf("gizmo", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    try
                    {
                        field.SetValue(window, value);
                        touched = true;
                    }
                    catch (Exception)
                    {
                        // 字段只读或类型不匹配时跳过，不影响其它字段。
                    }
                }

                window.Repaint();
            }

            if (!touched)
            {
                Debug.LogWarning("[Gizmo] 未找到 GameView 的 Gizmos 开关字段，请手动点击 Game 视图工具栏右侧的 Gizmos 按钮。");
            }

            return touched;
        }
    }
}
