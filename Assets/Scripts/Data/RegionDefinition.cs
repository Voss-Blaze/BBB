using System.Collections.Generic;
using UnityEngine;

namespace Lingjuan.Data
{
    /// <summary>一个画卷区域（山卷/海内卷/海外卷/大荒卷），固定包含两只异兽。</summary>
    [CreateAssetMenu(menuName = "灵卷绘形/区域数据", fileName = "Region_")]
    public class RegionDefinition : ScriptableObject
    {
        [Header("基础信息")]
        public string regionId;
        public string displayName;
        public int order;

        [Header("界面图片")]
        public Sprite levelSprite;
        public Sprite scrollSprite;

        [Header("区域异兽（固定 2 只）")]
        public List<BeastDefinition> beasts = new List<BeastDefinition>();

        [Header("踪迹锚点（屏幕归一化坐标 0~1）")]
        public Vector2[] traceSlots;
    }
}
