using System.Collections.Generic;
using UnityEngine;

namespace Lingjuan.Data
{
    /// <summary>数据总库，作为运行时唯一入口。</summary>
    [CreateAssetMenu(menuName = "灵卷绘形/数据总库", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public List<RegionDefinition> regions = new List<RegionDefinition>();
        public NamePool namePool;
        public GameConfig config;

        private readonly Dictionary<string, BeastDefinition> _beastMap = new Dictionary<string, BeastDefinition>();
        private readonly Dictionary<string, RegionDefinition> _regionMap = new Dictionary<string, RegionDefinition>();
        private readonly List<BeastDefinition> _allBeasts = new List<BeastDefinition>();

        public IReadOnlyList<RegionDefinition> Regions => regions;
        public IReadOnlyList<BeastDefinition> AllBeasts => _allBeasts;

        private void OnEnable()
        {
            BuildIndex();
        }

        /// <summary>建立 id 索引，重复调用安全。</summary>
        public void BuildIndex()
        {
            _beastMap.Clear();
            _regionMap.Clear();
            _allBeasts.Clear();

            if (regions == null)
            {
                return;
            }

            foreach (var region in regions)
            {
                if (region == null || string.IsNullOrEmpty(region.regionId))
                {
                    continue;
                }

                _regionMap[region.regionId] = region;

                if (region.beasts == null)
                {
                    continue;
                }

                foreach (var beast in region.beasts)
                {
                    if (beast == null || string.IsNullOrEmpty(beast.beastId))
                    {
                        continue;
                    }

                    _beastMap[beast.beastId] = beast;
                    _allBeasts.Add(beast);
                }
            }
        }

        public BeastDefinition GetBeast(string beastId)
        {
            if (string.IsNullOrEmpty(beastId))
            {
                return null;
            }

            _beastMap.TryGetValue(beastId, out BeastDefinition beast);
            return beast;
        }

        public RegionDefinition GetRegion(string regionId)
        {
            if (string.IsNullOrEmpty(regionId))
            {
                return null;
            }

            _regionMap.TryGetValue(regionId, out RegionDefinition region);
            return region;
        }
    }
}
