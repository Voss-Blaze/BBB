using UnityEngine;

namespace Lingjuan.Data
{
    /// <summary>全局玩法参数，集中管理写生、提示与辨名规则。</summary>
    [CreateAssetMenu(menuName = "灵卷绘形/游戏配置", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("踪迹探索")]
        [Tooltip("每次进入地图出现的踪迹数量")]
        public int tracesPerEntry = 3;
        [Tooltip("连续点击空白区域多少次后提示无踪迹")]
        public int blankClicksForHint = 3;

        [Header("写生判定")]
        [Tooltip("整次写生允许的失败次数")]
        public int tolerance = 2;
        [Tooltip("最多轮数，实际轮数由异兽难度决定")]
        public int maxRounds = 4;
        [Tooltip("每根弧线的限时（秒）")]
        public float roundTime = 15f;
        [Tooltip("指针初始速度")]
        public float baseSpeed = 1f;
        [Tooltip("每完成一轮的速度增量")]
        public float speedStep = 0.15f;
        [Tooltip("两轮之间的缓冲时间（秒）")]
        public float roundBuffer = 1f;

        [Header("界面提示")]
        [Tooltip("UI 提示框显示时长（秒）")]
        public float toastDuration = 3f;

        [Header("辨名")]
        [Tooltip("辨名选项总数，含正确项")]
        public int quizOptionCount = 3;
    }
}
