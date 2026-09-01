using System.Collections.Generic;
using DesktopPet.AI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>
    /// 稀疏组合：必须同时命中指定性格 + 指定特质，才把专属行为并进选型池。
    /// 不换 AI 组；不是全矩阵。
    /// </summary>
    [CreateAssetMenu(menuName = "桌宠/Luby/AI 组合", fileName = "LubyAiCombo")]
    public sealed class LubyAiComboDefinition : ScriptableObject
    {
        [BoxGroup("基础")]
        [LabelText("组合 ID")]
        public string comboId = "combo";

        [BoxGroup("基础")]
        [LabelText("显示名")]
        public string displayName = "组合";

        [BoxGroup("条件")]
        [LabelText("性格（必填）")]
        [Required]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/Personalities")]
        public LubyPersonalityDefinition personality;

        [BoxGroup("条件")]
        [LabelText("特质（必填）")]
        [Required]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/Traits")]
        public LubyTraitDefinition trait;

        [BoxGroup("AI")]
        [LabelText("组合专属行为（带权重）")]
        [Tooltip("性格+特质都命中才并池；权重写本列表。")]
        [ListDrawerSettings(ShowFoldout = true)]
        public List<LubyWeightedBehaviorEntry> behaviors = new List<LubyWeightedBehaviorEntry>();

        public bool IsConfigured =>
            personality != null &&
            !string.IsNullOrEmpty(personality.personalityId) &&
            trait != null &&
            !string.IsNullOrEmpty(trait.traitId);

        public bool Matches(string personalityId, IList<LubyTraitDefinition> traits)
        {
            if (!IsConfigured || string.IsNullOrEmpty(personalityId))
                return false;
            if (!string.Equals(personality.personalityId, personalityId, System.StringComparison.Ordinal))
                return false;
            return HasTrait(traits, trait.traitId);
        }

        private static bool HasTrait(IList<LubyTraitDefinition> traits, string traitId)
        {
            if (traits == null || string.IsNullOrEmpty(traitId))
                return false;
            for (int i = 0; i < traits.Count; i++)
            {
                LubyTraitDefinition t = traits[i];
                if (t != null && t.traitId == traitId)
                    return true;
            }

            return false;
        }
    }
}
