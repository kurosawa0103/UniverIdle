using System;
using DesktopPet.AI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Luby
{
    /// <summary>叠加到性格组里同 behaviorId 的选型权重（相加，不乘）。拖任一同 id 行为资产即可。</summary>
    [Serializable]
    public struct LubyBehaviorWeightAdd
    {
        [LabelText("行为")]
        [Tooltip("拖 Stand/Walk/Sleep 等；按 behaviorId 匹配各组同名行为（如 walk）")]
        [AssetSelector(Paths = "Assets/Resources/GameData/Luby/AI")]
        public PetBehaviorDefinition behavior;

        [LabelText("叠加权重")]
        [Tooltip("加到性格组该行为 weight 上；可为负（压低，如夜猫子 sleep）")]
        public float weight;
    }
}
