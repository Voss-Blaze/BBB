using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 临时排查工具：找出「画面中心那条白色细线 / 白框」到底是哪个对象。
/// 用法：进入 Play 状态 → 菜单 Tools/定位画面中心的白框 → 看 Console。
/// 定位完可以直接删掉这个文件。
/// </summary>
public static class WhiteFrameProbe
{
    private static readonly List<Hit> Hits = new List<Hit>();

    private class Hit
    {
        public string path;
        public string extra;
        public Rect rect;
        public bool containsCenter;
        public bool whiteThin;
    }

    [MenuItem("Tools/定位画面中心的白框")]
    public static void Probe()
    {
        Hits.Clear();
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        // 1. 所有 UI 图形
        foreach (Canvas canvas in Object.FindObjectsOfType<Canvas>(true))
        {
            foreach (Graphic g in canvas.GetComponentsInChildren<Graphic>(true))
            {
                if (g == null || !g.isActiveAndEnabled)
                {
                    continue;
                }

                Rect rect = GetScreenRect(g.rectTransform);
                if (rect.width <= 0f && rect.height <= 0f)
                {
                    continue;
                }

                Add(g.gameObject, rect, center,
                    string.Format("color={0} sprite={1} type={2}",
                        ToStr(g.color),
                        g is Image img && img.sprite != null ? img.sprite.name : (g is Image ? "<无Sprite>" : "-"),
                        g is Image im2 ? im2.type.ToString() : "-"));
            }
        }

        // 2. 世界空间精灵（背景一类，光照下才会变白）
        foreach (SpriteRenderer sr in Object.FindObjectsOfType<SpriteRenderer>(true))
        {
            if (sr == null || !sr.enabled)
            {
                continue;
            }

            Bounds b = sr.bounds;
            if (Camera.main == null)
            {
                continue;
            }

            Vector3 p0 = Camera.main.WorldToScreenPoint(new Vector3(b.min.x, b.min.y, 0f));
            Vector3 p1 = Camera.main.WorldToScreenPoint(new Vector3(b.max.x, b.max.y, 0f));
            Add(sr.gameObject, Rect.MinMaxRect(p0.x, p0.y, p1.x, p1.y), center,
                string.Format("SpriteRenderer sprite={0} color={1} size={2}",
                    sr.sprite != null ? sr.sprite.name : "<无>", ToStr(sr.color), sr.size));
        }

        // 3. 输出
        var sb = new StringBuilder();
        sb.AppendLine(string.Format("[Probe] 屏幕 {0}x{1}，中心 ({2},{3})。命中 {4} 个对象。",
            Screen.width, Screen.height, center.x, center.y, Hits.Count));

        Hits.Sort((a, b) =>
        {
            if (a.containsCenter != b.containsCenter) return a.containsCenter ? -1 : 1;
            if (a.whiteThin != b.whiteThin) return a.whiteThin ? -1 : 1;
            return (a.rect.width * a.rect.height).CompareTo(b.rect.width * b.rect.height);
        });

        for (int i = 0; i < Hits.Count; i++)
        {
            Hit h = Hits[i];
            sb.AppendLine(string.Format(
                "{0}. {1}{2}\n    rect=({3:F0},{4:F0}) {5:F0}x{6:F0}  {7}",
                i + 1,
                h.containsCenter ? "★ " : "  ",
                h.path,
                h.rect.x, h.rect.y, h.rect.width, h.rect.height,
                h.extra));
        }

        Debug.Log(sb.ToString());
        Debug.LogWarning("[Probe] 上面标 ★ 的是覆盖画面中心的对象，细而白的排在最前面——白色框框基本就在这几行里。");
    }

    private static void Add(GameObject go, Rect rect, Vector2 center, string extra)
    {
        bool contains = rect.Contains(center);
        bool whiteThin = rect.width > 0f && rect.height > 0f &&
                         (rect.width / rect.height > 8f || rect.height / rect.width > 8f);

        // 只保留：覆盖中心的、或者细长条的、或者特别大的
        if (!contains && !whiteThin && rect.width * rect.height < Screen.width * (float)Screen.height * 0.5f)
        {
            return;
        }

        Hits.Add(new Hit
        {
            path = GetPath(go.transform),
            extra = extra,
            rect = rect,
            containsCenter = contains,
            whiteThin = whiteThin
        });
    }

    private static Rect GetScreenRect(RectTransform rt)
    {
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
        return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y),
            Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        Transform p = t.parent;
        while (p != null)
        {
            path = p.name + "/" + path;
            p = p.parent;
        }

        return path;
    }

    private static string ToStr(Color c)
    {
        return string.Format("({0:F2},{1:F2},{2:F2},{3:F2})", c.r, c.g, c.b, c.a);
    }
}
