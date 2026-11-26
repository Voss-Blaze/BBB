using System.Collections.Generic;
using System.Text;
using Lingjuan.Data;
using UnityEditor;
using UnityEngine;

namespace Lingjuan.EditorTools
{
    /// <summary>校验数据资产的数量、引用与图片填写情况。</summary>
    public static class DataValidator
    {
        [MenuItem("灵卷绘形/校验数据")]
        public static void Validate()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameDatabase");
            if (guids.Length == 0)
            {
                Debug.LogError("[灵卷绘形] 未找到 GameDatabase 资产，请先创建 Assets/Data/GameDatabase.asset。");
                return;
            }

            GameDatabase db = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (db == null)
            {
                Debug.LogError("[灵卷绘形] GameDatabase 加载失败。");
                return;
            }

            db.BuildIndex();

            var errors = new List<string>();
            var warnings = new List<string>();
            var beastIds = new HashSet<string>();
            int beginner = 0, intermediate = 0, advanced = 0;

            if (db.regions == null || db.regions.Count != 4)
            {
                errors.Add($"区域数量应为 4，当前 {(db.regions == null ? 0 : db.regions.Count)}。");
            }

            if (db.namePool == null)
            {
                errors.Add("NamePool 未引用。");
            }
            else if (db.namePool.entries == null)
            {
                errors.Add("名字库条目为空。");
            }
            else
            {
                if (db.namePool.entries.Count != 46)
                {
                    warnings.Add($"名字库应为 46 条，当前 {db.namePool.entries.Count}。");
                }

                var nameSet = new HashSet<string>();
                foreach (NameEntry entry in db.namePool.entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.displayName))
                    {
                        errors.Add("名字库存在空条目。");
                        continue;
                    }

                    if (!nameSet.Add(entry.displayName))
                    {
                        errors.Add($"名字库重复：{entry.displayName}。");
                    }
                }
            }

            if (db.config == null)
            {
                errors.Add("GameConfig 未引用。");
            }

            if (db.regions != null)
            {
                foreach (RegionDefinition region in db.regions)
                {
                    if (region == null)
                    {
                        errors.Add("区域列表存在空引用。");
                        continue;
                    }

                    if (region.beasts == null || region.beasts.Count != 2)
                    {
                        errors.Add($"区域 {region.displayName} 应有 2 只异兽，当前 {(region.beasts == null ? 0 : region.beasts.Count)}。");
                    }

                    if (region.levelSprite == null)
                    {
                        warnings.Add($"[区域] {region.displayName}：场景关卡图 levelSprite 未填。");
                    }

                    if (region.scrollSprite == null)
                    {
                        warnings.Add($"[区域] {region.displayName}：画卷透明图 scrollSprite 未填。");
                    }

                    if (region.beasts == null)
                    {
                        continue;
                    }

                    foreach (BeastDefinition beast in region.beasts)
                    {
                        if (beast == null)
                        {
                            errors.Add($"区域 {region.displayName} 存在空异兽引用。");
                            continue;
                        }

                        if (string.IsNullOrEmpty(beast.beastId) || !beastIds.Add(beast.beastId))
                        {
                            errors.Add($"异兽 id 为空或重复：{beast.displayName}（{beast.beastId}）。");
                        }

                        if (beast.regionId != region.regionId)
                        {
                            errors.Add($"异兽 {beast.displayName} 的 regionId（{beast.regionId}）与所属区域（{region.regionId}）不一致。");
                        }

                        switch (beast.tier)
                        {
                            case BeastTier.Beginner: beginner++; break;
                            case BeastTier.Intermediate: intermediate++; break;
                            case BeastTier.Advanced: advanced++; break;
                        }

                        if (beast.fragments == null || beast.fragments.Count != 3)
                        {
                            errors.Add($"异兽 {beast.displayName} 应有 3 张残页，当前 {(beast.fragments == null ? 0 : beast.fragments.Count)}。");
                        }
                        else
                        {
                            for (int i = 0; i < beast.fragments.Count; i++)
                            {
                                FragmentData fragment = beast.fragments[i];
                                if (fragment == null || string.IsNullOrEmpty(fragment.body))
                                {
                                    errors.Add($"异兽 {beast.displayName} 的残页 {i + 1} 正文为空。");
                                }
                            }
                        }

                        if (beast.traces == null || beast.traces.Count != 3)
                        {
                            errors.Add($"异兽 {beast.displayName} 应有 3 条踪迹，当前 {(beast.traces == null ? 0 : beast.traces.Count)}。");
                        }
                        else
                        {
                            var usedFragments = new HashSet<int>();
                            foreach (TraceData trace in beast.traces)
                            {
                                if (trace == null || string.IsNullOrEmpty(trace.prompt))
                                {
                                    errors.Add($"异兽 {beast.displayName} 存在空踪迹提示。");
                                    continue;
                                }

                                if (trace.fragmentIndex < 0 || trace.fragmentIndex > 2 || !usedFragments.Add(trace.fragmentIndex))
                                {
                                    errors.Add($"异兽 {beast.displayName} 的残页索引重复或越界：{trace.fragmentIndex}。");
                                }
                            }
                        }

                        if (beast.nameSprite == null) warnings.Add($"[图鉴/辨名] {beast.displayName}：异兽名图未填。");
                        if (beast.codexSprite == null) warnings.Add($"[图鉴] {beast.displayName}：异兽图片未填。");
                        if (beast.writeBaseSprite == null) warnings.Add($"[写生] {beast.displayName}：底图未填。");
                        if (beast.writeDoneSprite == null) warnings.Add($"[写生] {beast.displayName}：完成图未填。");
                        if (beast.portraitSprite == null) warnings.Add($"[写生/辨名] {beast.displayName}：立绘未填。");
                    }
                }
            }

            if (beginner != 4 || intermediate != 3 || advanced != 1)
            {
                errors.Add($"难度分布应为 初级4 / 中级3 / 高级1，当前 {beginner}/{intermediate}/{advanced}。");
            }

            var report = new StringBuilder();
            report.AppendLine($"[灵卷绘形] 数据校验：区域 4，异兽 {beastIds.Count}，错误 {errors.Count}，图片待填 {warnings.Count}。");
            foreach (string error in errors)
            {
                report.AppendLine("错误：" + error);
            }

            foreach (string warning in warnings)
            {
                report.AppendLine("待填：" + warning);
            }

            if (errors.Count == 0)
            {
                Debug.Log(report.ToString());
            }
            else
            {
                Debug.LogError(report.ToString());
            }
        }
    }
}
