using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lingjuan.Data
{
    /// <summary>异兽难度层级，对应策划案的初级/中级/高级。</summary>
    public enum BeastTier
    {
        Beginner = 0,
        Intermediate = 1,
        Advanced = 2
    }

    /// <summary>三种踪迹类型：外形、环境、山海遗证。</summary>
    public enum TraceType
    {
        Shape = 0,
        Environment = 1,
        Evidence = 2
    }

    /// <summary>单张残页的数据，图鉴与残页界面共用。</summary>
    [Serializable]
    public class FragmentData
    {
        public string title;
        [TextArea(3, 8)] public string body;
        public Sprite paperSprite;
    }

    /// <summary>一条踪迹，固定对应一张残页。</summary>
    [Serializable]
    public class TraceData
    {
        public TraceType type;
        [TextArea(2, 4)] public string prompt;
        [Range(0, 2)] public int fragmentIndex;
        public Sprite spotSprite;
    }

    /// <summary>一只异兽的全部静态数据，图片槽位留空由美术填写。</summary>
    [CreateAssetMenu(menuName = "灵卷绘形/异兽数据", fileName = "Beast_")]
    public class BeastDefinition : ScriptableObject
    {
        [Header("基础信息")]
        public string beastId;
        public string displayName;
        public string pinyin;
        public string regionId;
        public int orderInRegion;
        public BeastTier tier;

        [Header("图鉴界面")]
        public Sprite nameSprite;
        public Sprite codexSprite;
        public List<FragmentData> fragments = new List<FragmentData>();

        [Header("写生界面")]
        public Sprite writeBackdropSprite;
        public Sprite writeBaseSprite;
        public Sprite writeDoneSprite;
        public Sprite writeFrameSprite;
        public Sprite writeRingLarge;
        public Sprite writeRingMedium;
        public Sprite writeRingSmall;
        public Sprite portraitSprite;
        [Tooltip("写生根数，0 表示按难度自动：初/中 3 根，高 4 根")]
        public int writeRounds;

        [Header("踪迹与残页")]
        public List<TraceData> traces = new List<TraceData>();
    }
}
