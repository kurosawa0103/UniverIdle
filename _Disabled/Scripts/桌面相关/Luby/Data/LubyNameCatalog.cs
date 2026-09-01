using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    public enum LubyNamePattern
    {
        /// <summary>形容词 + 名词，如「软团子」。</summary>
        AdjNoun = 0,
        /// <summary>形容词 + 兔后缀，如「懒小兔」。</summary>
        AdjBunny = 1,
        /// <summary>叠字 + 名词/兔，如「团团兔」。</summary>
        Reduplicate = 2,
        /// <summary>外形色词 + 名词，如「薄荷团子」。</summary>
        ColorNoun = 3,
        /// <summary>预设完整名，如「棉花球」。</summary>
        Preset = 4
    }

    [Serializable]
    public struct LubyWeightedNamePattern
    {
        public LubyNamePattern pattern;
        [Min(1)]
        public int weight;
    }

    [Serializable]
    public struct LubyAppearanceNameBias
    {
        [Tooltip("外形 Prefab 名前缀或全名，如 Luby_Look_Mint")]
        public string appearanceKey;
        [Tooltip("该外形额外可抽的色/质感词")]
        public string[] colorWords;
    }

    [Serializable]
    public struct LubyPersonalityNameBias
    {
        public string personalityId;
        [Tooltip("该性格额外可抽的形容词")]
        public string[] adjectives;
    }

    /// <summary>Luby 随机取名词库与组合权重。运行时只读 Resources 资产，词表不在代码里维护。</summary>
    [CreateAssetMenu(menuName = "桌宠/Luby/取名词库", fileName = "LubyNameCatalog")]
    public sealed class LubyNameCatalog : ScriptableObject
    {
        public const string DefaultResourcePath = "GameData/Luby/DefaultLubyNameCatalog";

        [BoxGroup("组合")]
        [LabelText("模式权重")]
        public LubyWeightedNamePattern[] patterns;

        [BoxGroup("通用词库")]
        [LabelText("形容词")]
        public string[] adjectives;

        [BoxGroup("通用词库")]
        [LabelText("兔系名词")]
        public string[] nouns;

        [BoxGroup("通用词库")]
        [LabelText("兔后缀")]
        public string[] bunnySuffixes;

        [BoxGroup("通用词库")]
        [LabelText("可叠字词根（团团/球球）")]
        public string[] reduplicateRoots;

        [BoxGroup("通用词库")]
        [LabelText("预设完整名")]
        public string[] presets;

        [BoxGroup("偏置")]
        [LabelText("外形 → 色词")]
        public LubyAppearanceNameBias[] appearanceBiases;

        [BoxGroup("偏置")]
        [LabelText("性格 → 形容词")]
        public LubyPersonalityNameBias[] personalityBiases;

        [BoxGroup("规则")]
        [LabelText("叠字模式再接兔后缀的概率")]
        [PropertyRange(0f, 1f)]
        public float reduplicateBunnyChance = 0.5f;

        [BoxGroup("规则")]
        [LabelText("有性格偏置时，抽偏置形容词的概率")]
        [PropertyRange(0f, 1f)]
        public float personalityBiasChance = 0.7f;

        public bool HasUsablePools =>
            patterns != null && patterns.Length > 0
            && adjectives != null && adjectives.Length > 0
            && nouns != null && nouns.Length > 0
            && bunnySuffixes != null && bunnySuffixes.Length > 0;

        public static LubyNameCatalog LoadDefault()
        {
            if (_cached != null)
                return _cached;

            LubyNameCatalog loaded = Resources.Load<LubyNameCatalog>(DefaultResourcePath);
            if (loaded == null)
                Debug.LogError($"[Luby] 缺少取名词库：Resources/{DefaultResourcePath}。请创建 DefaultLubyNameCatalog.asset。");
            else if (!loaded.HasUsablePools)
                Debug.LogError("[Luby] DefaultLubyNameCatalog 词库未配齐（patterns/adjectives/nouns/bunnySuffixes）。");

            _cached = loaded;
            return _cached;
        }

        private static LubyNameCatalog _cached;
    }
}
