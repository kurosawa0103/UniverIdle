using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Adventure
{
    /// <summary>探险区域：独立事件池 + 抽取权重；Luby 每趟先抽区域再抽事件。</summary>
    [CreateAssetMenu(menuName = "桌宠/探险/区域", fileName = "AdventureRegion")]
    public sealed class AdventureRegionDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class IdWeightMul
        {
            [LabelText("ID")]
            public string id;

            [LabelText("权重倍率")]
            [MinValue(0f)]
            public float weightMul = 1.5f;
        }

        [BoxGroup("基础")]
        [LabelText("区域 ID")]
        public string regionId = "adv_region_town";

        [BoxGroup("基础")]
        [LabelText("显示名")]
        public string displayName = "街市";

        [BoxGroup("抽取")]
        [LabelText("基础权重")]
        [MinValue(0.01f)]
        public float weight = 1f;

        [BoxGroup("环境")]
        [LabelText("仅夜间")]
        public bool requireNight;

        [BoxGroup("环境")]
        [LabelText("仅雨天")]
        [Tooltip("rainy / stormy")]
        public bool requireRain;

        [BoxGroup("偏置")]
        [LabelText("性格权重倍率")]
        [ListDrawerSettings(ShowFoldout = true)]
        public IdWeightMul[] personalityWeightMuls;

        [BoxGroup("偏置")]
        [LabelText("特质权重倍率")]
        [ListDrawerSettings(ShowFoldout = true)]
        public IdWeightMul[] traitWeightMuls;

        [BoxGroup("事件")]
        [LabelText("本区事件池")]
        [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "eventId")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Adventure/Events")]
        public List<AdventureEventDefinition> events = new List<AdventureEventDefinition>();

        public float ResolveWeight(string personalityId, bool hasTraitCoin, bool hasTraitSleepy,
            bool hasTraitRain, bool hasTraitFoodie, bool hasTraitNightOwl)
        {
            if (weight <= 0f)
                return 0f;

            float w = weight;
            w *= FindMul(personalityWeightMuls, personalityId);

            if (hasTraitCoin)
                w *= FindMul(traitWeightMuls, "trait_coin_greedy");
            if (hasTraitSleepy)
                w *= FindMul(traitWeightMuls, "trait_sleepy");
            if (hasTraitRain)
                w *= FindMul(traitWeightMuls, "trait_rain_play");
            if (hasTraitFoodie)
                w *= FindMul(traitWeightMuls, "trait_foodie");
            if (hasTraitNightOwl)
                w *= FindMul(traitWeightMuls, "trait_night_owl");

            return w;
        }

        private static float FindMul(IdWeightMul[] list, string id)
        {
            if (list == null || string.IsNullOrEmpty(id))
                return 1f;
            for (int i = 0; i < list.Length; i++)
            {
                IdWeightMul e = list[i];
                if (e == null || string.IsNullOrEmpty(e.id))
                    continue;
                if (string.Equals(e.id, id, StringComparison.Ordinal))
                    return Mathf.Max(0f, e.weightMul);
            }

            return 1f;
        }
    }
}
