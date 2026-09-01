using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Adventure
{
    /// <summary>探险事件池目录。</summary>
    [CreateAssetMenu(menuName = "桌宠/探险/事件目录", fileName = "AdventureEventCatalog")]
    public sealed class AdventureEventCatalog : ScriptableObject
    {
        public const string DefaultResourcePath = "GameData/Adventure/DefaultAdventureEventCatalog";

        [LabelText("显示名")]
        public string displayName = "探险事件池";

        [LabelText("区域列表")]
        [Tooltip("每趟先抽区域，再在该区事件池内抽事件。")]
        [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "regionId")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Adventure/Regions")]
        public List<AdventureRegionDefinition> regions = new List<AdventureRegionDefinition>();

        [LabelText("事件列表（回退·勿日常维护）")]
        [Tooltip("正式内容只配 regions。仅当区域列表为空时才从这里抽；默认保持空。")]
        [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "eventId")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Adventure/Events")]
        public List<AdventureEventDefinition> events = new List<AdventureEventDefinition>();

        [LabelText("每日软顶趟数")]
        [MinValue(1)]
        public int dailySoftCapTrips = 6;

        [LabelText("软顶金币下限")]
        [MinValue(0)]
        public int softCapGoldMin;

        [LabelText("软顶金币上限")]
        [MinValue(0)]
        public int softCapGoldMax = 1;

        [LabelText("爱财金币倍率")]
        [MinValue(1f)]
        public float coinGreedyGoldMul = 1.2f;

        public static AdventureEventCatalog LoadDefault()
        {
            return Resources.Load<AdventureEventCatalog>(DefaultResourcePath);
        }

        public AdventureRegionDefinition FindRegionById(string regionId)
        {
            if (regions == null || string.IsNullOrEmpty(regionId))
                return null;
            for (int i = 0; i < regions.Count; i++)
            {
                AdventureRegionDefinition r = regions[i];
                if (r != null && r.regionId == regionId)
                    return r;
            }

            return null;
        }

        public AdventureEventDefinition FindById(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                return null;

            if (regions != null)
            {
                for (int i = 0; i < regions.Count; i++)
                {
                    AdventureRegionDefinition region = regions[i];
                    if (region?.events == null)
                        continue;
                    for (int j = 0; j < region.events.Count; j++)
                    {
                        AdventureEventDefinition e = region.events[j];
                        if (e != null && e.eventId == eventId)
                            return e;
                    }
                }
            }

            if (events == null)
                return null;
            for (int i = 0; i < events.Count; i++)
            {
                AdventureEventDefinition e = events[i];
                if (e != null && e.eventId == eventId)
                    return e;
            }

            return null;
        }
    }
}
