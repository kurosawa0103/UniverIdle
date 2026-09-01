using System.Collections.Generic;
using DesktopPet.AI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    [CreateAssetMenu(menuName = "桌宠/Luby/特质", fileName = "LubyTrait")]
    public sealed class LubyTraitDefinition : ScriptableObject
    {
        [BoxGroup("基础")]
        [LabelText("特质 ID")]
        public string traitId = "trait";

        [BoxGroup("基础")]
        [LabelText("显示名")]
        public string displayName = "特质";

        [BoxGroup("基础")]
        [TextArea(2, 4)]
        public string description;

        [BoxGroup("抽取")]
        [LabelText("互斥特质")]
        [Tooltip("双特质时：与本列表中任一特质不会同时抽中（双向：对方列表含本特质也互斥）。")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/Traits")]
        [ListDrawerSettings(ShowFoldout = true)]
        public List<LubyTraitDefinition> exclusiveTraits = new List<LubyTraitDefinition>();

        [BoxGroup("AI")]
        [LabelText("附加行为（带权重）")]
        [Tooltip("与性格 AI 组并池加权随机；权重写在本列表，不读行为资产上的 weight。")]
        [ListDrawerSettings(ShowFoldout = true)]
        public List<LubyWeightedBehaviorEntry> behaviors = new List<LubyWeightedBehaviorEntry>();

        [BoxGroup("AI")]
        [LabelText("叠加性格组内权重")]
        [Tooltip("拖行为资产：选型权重 = 性格组同 behaviorId 的 weight + 本条 weight（相加）。可为负。")]
        [ListDrawerSettings(ShowFoldout = true, ListElementLabelName = "behavior")]
        public LubyBehaviorWeightAdd[] groupWeightAdds;

        /// <summary>双特质配对是否冲突（含自身 / 同 id / 任一侧互斥列表）。</summary>
        public bool ConflictsForDual(LubyTraitDefinition other)
        {
            if (other == null)
                return true;
            if (ReferenceEquals(this, other))
                return true;
            if (!string.IsNullOrEmpty(traitId) && traitId == other.traitId)
                return true;
            return ListContainsTrait(exclusiveTraits, other) ||
                   ListContainsTrait(other.exclusiveTraits, this);
        }

        private static bool ListContainsTrait(IList<LubyTraitDefinition> list, LubyTraitDefinition target)
        {
            if (list == null || target == null)
                return false;
            for (int i = 0; i < list.Count; i++)
            {
                LubyTraitDefinition t = list[i];
                if (t == null)
                    continue;
                if (ReferenceEquals(t, target))
                    return true;
                if (!string.IsNullOrEmpty(t.traitId) && t.traitId == target.traitId)
                    return true;
            }

            return false;
        }
    }
}
