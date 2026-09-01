using System;
using System.Collections.Generic;
using DesktopPet.AI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    [Serializable]
    public sealed class LubyWeightedPersonalityEntry
    {
        [HorizontalGroup("row", Width = 0.75f)]
        [HideLabel]
        public LubyPersonalityDefinition personality;

        [HorizontalGroup("row")]
        [LabelText("权重"), LabelWidth(36)]
        [MinValue(0f)]
        public float weight = 1f;
    }

    [Serializable]
    public sealed class LubyWeightedTraitEntry
    {
        [HorizontalGroup("row", Width = 0.75f)]
        [HideLabel]
        public LubyTraitDefinition trait;

        [HorizontalGroup("row")]
        [LabelText("权重"), LabelWidth(36)]
        [MinValue(0f)]
        public float weight = 1f;
    }

    /// <summary>特质行为池条目：与性格组并池参与加权随机。</summary>
    [Serializable]
    public sealed class LubyWeightedBehaviorEntry
    {
        [HorizontalGroup("row", Width = 0.75f)]
        [HideLabel]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/AI")]
        public PetBehaviorDefinition behavior;

        [HorizontalGroup("row")]
        [LabelText("权重"), LabelWidth(36)]
        [MinValue(0f)]
        public float weight = 1f;
    }

    /// <summary>盲盒外形条目：按权重抽取；可勾选绑定专属性格/特质池。</summary>
    [Serializable]
    public sealed class LubyWeightedAppearanceEntry
    {
        [LabelText("外形 Prefab")]
        public GameObject prefab;

        [LabelText("权重")]
        [MinValue(0f)]
        public float weight = 1f;

        [LabelText("绑定性格")]
        [Tooltip("勾选后，抽到此外形时只从下方性格池按权重抽；不勾则用盲盒/全局默认性格池。")]
        public bool bindPersonality;

        [ShowIf(nameof(bindPersonality))]
        [LabelText("绑定性格池")]
        [ListDrawerSettings(ShowFoldout = true)]
        public LubyWeightedPersonalityEntry[] personalityPool;

        [LabelText("绑定特质")]
        [Tooltip("勾选后，抽到此外形时只从下方特质池按权重抽；不勾则用盲盒/全局默认特质池。")]
        public bool bindTrait;

        [ShowIf(nameof(bindTrait))]
        [LabelText("绑定特质池")]
        [ListDrawerSettings(ShowFoldout = true)]
        public LubyWeightedTraitEntry[] traitPool;
    }

    /// <summary>按权重抽取 / 按 ID 查找（数组与 List 共用）。</summary>
    public static class LubyWeightedRoll
    {
        public static LubyPersonalityDefinition PickPersonality(IList<LubyWeightedPersonalityEntry> pool) =>
            Pick(pool, e => e.personality, e => e.weight);

        public static LubyTraitDefinition PickTrait(IList<LubyWeightedTraitEntry> pool) =>
            Pick(pool, e => e.trait, e => e.weight);

        /// <summary>加权抽第二特质：排除第一特质本身及其互斥列表（双向）。</summary>
        public static LubyTraitDefinition PickTraitCompatibleWith(
            IList<LubyWeightedTraitEntry> pool,
            LubyTraitDefinition first) =>
            Pick(
                pool,
                e => e.trait,
                e => e.weight,
                v => first == null || (v != null && !first.ConflictsForDual(v)));

        public static LubyWeightedAppearanceEntry PickAppearance(IList<LubyWeightedAppearanceEntry> pool) =>
            Pick(pool, e => e.prefab != null ? e : null, e => e.weight);

        public static LubyPersonalityDefinition FindPersonality(
            IList<LubyWeightedPersonalityEntry> pool,
            string personalityId) =>
            Find(pool, e => e?.personality, p => p.personalityId, personalityId);

        public static LubyTraitDefinition FindTrait(IList<LubyWeightedTraitEntry> pool, string traitId) =>
            Find(pool, e => e?.trait, t => t.traitId, traitId);

        static TResult Pick<TEntry, TResult>(
            IList<TEntry> pool,
            Func<TEntry, TResult> value,
            Func<TEntry, float> weight)
            where TEntry : class
            where TResult : class =>
            Pick(pool, value, weight, _ => true);

        static TResult Pick<TEntry, TResult>(
            IList<TEntry> pool,
            Func<TEntry, TResult> value,
            Func<TEntry, float> weight,
            Func<TResult, bool> accept)
            where TEntry : class
            where TResult : class
        {
            if (pool == null || pool.Count == 0 || accept == null)
                return null;

            float total = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                TEntry e = pool[i];
                if (e == null)
                    continue;
                TResult v = value(e);
                float w = weight(e);
                if (v == null || w <= 0f || !accept(v))
                    continue;
                total += w;
            }

            if (total <= 0f)
                return null;

            float roll = UnityEngine.Random.Range(0f, total);
            float acc = 0f;
            TResult last = null;
            for (int i = 0; i < pool.Count; i++)
            {
                TEntry e = pool[i];
                if (e == null)
                    continue;
                TResult v = value(e);
                float w = weight(e);
                if (v == null || w <= 0f || !accept(v))
                    continue;
                last = v;
                acc += w;
                if (roll <= acc)
                    return v;
            }

            return last;
        }

        static TResult Find<TEntry, TResult>(
            IList<TEntry> pool,
            Func<TEntry, TResult> value,
            Func<TResult, string> idOf,
            string id)
            where TResult : class
        {
            if (pool == null || string.IsNullOrEmpty(id))
                return null;

            for (int i = 0; i < pool.Count; i++)
            {
                TResult v = value(pool[i]);
                if (v != null && idOf(v) == id)
                    return v;
            }

            return null;
        }
    }
}
