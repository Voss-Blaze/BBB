using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lingjuan.Data
{
    /// <summary>名字库中的一条，供辨名阶段抽取干扰项。</summary>
    [Serializable]
    public class NameEntry
    {
        public string displayName;
        public string pinyin;
        public string category;
        [TextArea(1, 3)] public string oneLiner;
    }

    /// <summary>全局名字库，正确项来自异兽自身，干扰项从这里随机抽取。</summary>
    [CreateAssetMenu(menuName = "灵卷绘形/名字库", fileName = "NamePool")]
    public class NamePool : ScriptableObject
    {
        public List<NameEntry> entries = new List<NameEntry>();

        /// <summary>抽取指定数量的干扰项，自动排除正确名字。</summary>
        public List<NameEntry> GetDistractors(string correctName, int count)
        {
            var result = new List<NameEntry>();
            if (entries == null || count <= 0)
            {
                return result;
            }

            foreach (var entry in entries)
            {
                if (entry != null && entry.displayName != correctName)
                {
                    result.Add(entry);
                }
            }

            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                NameEntry temp = result[i];
                result[i] = result[j];
                result[j] = temp;
            }

            if (result.Count > count)
            {
                result.RemoveRange(count, result.Count - count);
            }

            return result;
        }
    }
}
