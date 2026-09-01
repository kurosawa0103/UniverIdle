using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Adventure
{
    /// <summary>单条探险事件：权重、时长、金币区间、环境/性格/特质偏置。</summary>
    [CreateAssetMenu(menuName = "桌宠/探险/事件", fileName = "AdventureEvent")]
    public sealed class AdventureEventDefinition : ScriptableObject
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
        [LabelText("事件 ID")]
        public string eventId = "adv_event";

        [BoxGroup("基础")]
        [LabelText("标题")]
        public string title = "探险";

        [BoxGroup("抽取")]
        [LabelText("基础权重")]
        [MinValue(0.01f)]
        public float weight = 1f;

        [BoxGroup("抽取")]
        [LabelText("离桌时长（秒）")]
        [Tooltip("测试默认 30；可配到 600（10 分钟）。")]
        [MinValue(5f)]
        [MaxValue(600f)]
        public float durationSeconds = 30f;

        [BoxGroup("奖励")]
        [LabelText("金币下限")]
        [MinValue(0)]
        public int goldMin;

        [BoxGroup("奖励")]
        [LabelText("金币上限")]
        [MinValue(0)]
        public int goldMax = 2;

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

        public float ResolveDurationSeconds()
        {
            return Mathf.Clamp(durationSeconds, 5f, 600f);
        }

        public int RollGold(System.Random rng)
        {
            int lo = Mathf.Min(goldMin, goldMax);
            int hi = Mathf.Max(goldMin, goldMax);
            if (hi <= lo)
                return Mathf.Max(0, lo);
            int span = hi - lo + 1;
            int roll = rng != null ? rng.Next(span) : UnityEngine.Random.Range(0, span);
            return lo + roll;
        }

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
