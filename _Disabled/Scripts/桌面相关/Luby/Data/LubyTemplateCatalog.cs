using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    [CreateAssetMenu(menuName = "桌宠/Luby/模板目录", fileName = "LubyTemplateCatalog")]
    public sealed class LubyTemplateCatalog : ScriptableObject
    {
        [BoxGroup("目录")]
        [LabelText("显示名")]
        public string displayName = "Luby 目录";

        [BoxGroup("模板")]
        [LabelText("可抽取盲盒档位")]
        [ListDrawerSettings(ShowFoldout = true)]
        public List<LubyTemplateDefinition> templates = new List<LubyTemplateDefinition>();

        [BoxGroup("全局随机池")]
        [LabelText("默认性格池（按权重）")]
        [Tooltip("仅当模板/外形未配性格池时兜底；正式权重写在各模板 personalityPool。")]
        public List<LubyWeightedPersonalityEntry> defaultPersonalityPool =
            new List<LubyWeightedPersonalityEntry>();

        [BoxGroup("全局随机池")]
        [LabelText("默认特质池（按权重）")]
        [Tooltip("仅当模板/外形未配特质池时兜底；正式权重写在各模板 traitPool。")]
        public List<LubyWeightedTraitEntry> defaultTraitPool = new List<LubyWeightedTraitEntry>();

        [BoxGroup("AI 组合")]
        [LabelText("性格×特质组合（稀疏）")]
        [Tooltip("必须同时命中性格+特质才并进专属行为。挂在目录上，勿散写进性格/特质 SO。")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/AI/Combos")]
        [ListDrawerSettings(ShowFoldout = true)]
        public List<LubyAiComboDefinition> aiCombos = new List<LubyAiComboDefinition>();

        public LubyTemplateDefinition FindTemplateById(string templateId)
        {
            if (templates == null || string.IsNullOrEmpty(templateId))
                return null;

            for (int i = 0; i < templates.Count; i++)
            {
                LubyTemplateDefinition t = templates[i];
                if (t != null && t.templateId == templateId)
                    return t;
            }

            return null;
        }

        public static LubyTemplateCatalog LoadDefault()
        {
            return Resources.Load<LubyTemplateCatalog>("GameData/Luby/DefaultLubyCatalog");
        }

        public LubyPersonalityDefinition FindPersonalityById(string personalityId)
        {
            if (string.IsNullOrEmpty(personalityId))
                return null;

            LubyPersonalityDefinition p = LubyWeightedRoll.FindPersonality(defaultPersonalityPool, personalityId);
            if (p != null)
                return p;

            if (templates == null)
                return null;

            for (int i = 0; i < templates.Count; i++)
            {
                LubyTemplateDefinition t = templates[i];
                if (t == null)
                    continue;

                p = LubyWeightedRoll.FindPersonality(t.personalityPool, personalityId);
                if (p != null)
                    return p;

                if (t.appearancePool == null)
                    continue;

                for (int a = 0; a < t.appearancePool.Length; a++)
                {
                    LubyWeightedAppearanceEntry app = t.appearancePool[a];
                    if (app == null || !app.bindPersonality)
                        continue;
                    p = LubyWeightedRoll.FindPersonality(app.personalityPool, personalityId);
                    if (p != null)
                        return p;
                }
            }

            return null;
        }

        public LubyTraitDefinition FindTraitById(string traitId)
        {
            if (string.IsNullOrEmpty(traitId))
                return null;

            LubyTraitDefinition tr = LubyWeightedRoll.FindTrait(defaultTraitPool, traitId);
            if (tr != null)
                return tr;

            if (templates == null)
                return null;

            for (int i = 0; i < templates.Count; i++)
            {
                LubyTemplateDefinition t = templates[i];
                if (t == null)
                    continue;

                tr = LubyWeightedRoll.FindTrait(t.traitPool, traitId);
                if (tr != null)
                    return tr;

                if (t.appearancePool == null)
                    continue;

                for (int a = 0; a < t.appearancePool.Length; a++)
                {
                    LubyWeightedAppearanceEntry app = t.appearancePool[a];
                    if (app == null || !app.bindTrait)
                        continue;
                    tr = LubyWeightedRoll.FindTrait(app.traitPool, traitId);
                    if (tr != null)
                        return tr;
                }
            }

            return null;
        }

        /// <summary>
        /// 盲盒内容：先按权重抽外形，再抽性格/特质（外形可绑定专属池，否则用模板池或全局池）。
        /// 模板 dualTraitChance 命中时可再抽第二个不同且不互斥的特质。
        /// </summary>
        public void RollBoxContents(
            LubyTemplateDefinition template,
            out GameObject appearancePrefab,
            out string appearanceKey,
            out LubyPersonalityDefinition personality,
            out LubyTraitDefinition trait,
            out LubyTraitDefinition trait2)
        {
            appearancePrefab = null;
            appearanceKey = string.Empty;
            personality = null;
            trait = null;
            trait2 = null;

            if (template == null)
                return;

            LubyWeightedAppearanceEntry appearance = LubyWeightedRoll.PickAppearance(template.appearancePool);
            if (appearance?.prefab != null)
            {
                appearancePrefab = appearance.prefab;
                appearanceKey = appearance.prefab.name;
            }

            if (appearance != null && appearance.bindPersonality)
                personality = LubyWeightedRoll.PickPersonality(appearance.personalityPool);
            if (personality == null)
                personality = RollPersonality(template);

            IList<LubyWeightedTraitEntry> traitPoolUsed = null;
            if (appearance != null && appearance.bindTrait)
            {
                trait = LubyWeightedRoll.PickTrait(appearance.traitPool);
                if (trait != null)
                    traitPoolUsed = appearance.traitPool;
            }

            if (trait == null)
            {
                trait = RollTrait(template, out traitPoolUsed);
            }

            if (trait == null || template.dualTraitChance <= 0f)
                return;
            if (UnityEngine.Random.value >= template.dualTraitChance)
                return;

            trait2 = LubyWeightedRoll.PickTraitCompatibleWith(traitPoolUsed, trait);
        }

        /// <summary>图鉴全集：按外形 Prefab 名去重（先出现的模板优先）。</summary>
        public List<LubyAppearanceCodexEntry> CollectUniqueAppearances()
        {
            var list = new List<LubyAppearanceCodexEntry>(32);
            if (templates == null)
                return list;

            var seen = new HashSet<string>();
            for (int i = 0; i < templates.Count; i++)
            {
                LubyTemplateDefinition t = templates[i];
                if (t?.appearancePool == null)
                    continue;

                for (int a = 0; a < t.appearancePool.Length; a++)
                {
                    LubyWeightedAppearanceEntry app = t.appearancePool[a];
                    if (app?.prefab == null)
                        continue;

                    string key = app.prefab.name;
                    if (string.IsNullOrEmpty(key) || !seen.Add(key))
                        continue;

                    list.Add(new LubyAppearanceCodexEntry(
                        key,
                        app.prefab,
                        t.templateId,
                        string.IsNullOrEmpty(t.displayName) ? t.templateId : t.displayName));
                }
            }

            return list;
        }

        private LubyPersonalityDefinition RollPersonality(LubyTemplateDefinition template)
        {
            if (template != null)
            {
                LubyPersonalityDefinition p = LubyWeightedRoll.PickPersonality(template.personalityPool);
                if (p != null)
                    return p;
            }

            return LubyWeightedRoll.PickPersonality(defaultPersonalityPool);
        }

        private LubyTraitDefinition RollTrait(
            LubyTemplateDefinition template,
            out IList<LubyWeightedTraitEntry> poolUsed)
        {
            poolUsed = null;
            if (template != null)
            {
                LubyTraitDefinition t = LubyWeightedRoll.PickTrait(template.traitPool);
                if (t != null)
                {
                    poolUsed = template.traitPool;
                    return t;
                }
            }

            LubyTraitDefinition fromDefault = LubyWeightedRoll.PickTrait(defaultTraitPool);
            if (fromDefault != null)
                poolUsed = defaultTraitPool;
            return fromDefault;
        }
    }
}
