using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// 可配置行为模组基类。DLC 新增行为：继承此类并实现 CreateRuntime。
    /// </summary>
    public abstract class PetBehaviorDefinition : ScriptableObject
    {
        [Title("行为模组", "加权选型 · 时长 · 动画")]
        [BoxGroup("身份")]
        [LabelText("行为 ID")]
        [Tooltip("稳定 ID：选型、RequestBehavior、性格/特质修正必须对上")]
        [Required]
        public string behaviorId = "behavior";

        [BoxGroup("选型")]
        [LabelText("权重")]
        [MinValue(0f)]
        [Tooltip("性格组内选型权重；特质可用 groupWeightAdds 相加。≤0 不会被抽到")]
        public float weight = 1f;

        [BoxGroup("选型")]
        [HorizontalGroup("选型/时长")]
        [LabelText("最短时长")]
        [MinValue(0f)]
        public float minDuration = 2f;

        [HorizontalGroup("选型/时长")]
        [LabelText("最长时长")]
        [MinValue(0f)]
        public float maxDuration = 5f;

        [BoxGroup("选型")]
        [LabelText("冷却（秒）")]
        [MinValue(0f)]
        [Tooltip("结束后多久内不能再被随机到")]
        public float cooldown = 0f;

        [BoxGroup("选型")]
        [LabelText("进入条件")]
        [Tooltip("留空 = 始终可进")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
        public List<PetBehaviorCondition> enterConditions = new List<PetBehaviorCondition>();

        [BoxGroup("选型")]
        [LabelText("维持进入条件")]
        [Tooltip("进行中若条件不再满足则提前结束（如天亮结束睡觉）")]
        public bool maintainEnterConditionsWhileActive;

        [BoxGroup("打断")]
        [LabelText("可被打断")]
        public bool canBeInterrupted = true;

        [BoxGroup("打断")]
        [LabelText("打断优先级")]
        [Tooltip("请求切入时越大越容易打断当前行为")]
        [ShowIf(nameof(canBeInterrupted))]
        public int interruptPriority = 0;

        [BoxGroup("动画")]
        [LabelText("Trigger")]
        public string animTrigger;

        [BoxGroup("动画")]
        [HorizontalGroup("动画/Bool")]
        [LabelText("Bool 名")]
        public string animBool;

        [HorizontalGroup("动画/Bool")]
        [LabelText("Bool 值")]
        [ShowIf("@!string.IsNullOrEmpty(animBool)")]
        public bool animBoolValue = true;

        [BoxGroup("动画")]
        [HorizontalGroup("动画/Speed")]
        [LabelText("Speed 参数")]
        public string animSpeedParam = "Speed";

        [HorizontalGroup("动画/Speed")]
        [LabelText("Speed 值")]
        public float animSpeedValue;

        public virtual bool CanEnter(PetBehaviorContext context)
        {
            if (enterConditions == null || enterConditions.Count == 0)
                return true;

            for (int i = 0; i < enterConditions.Count; i++)
            {
                PetBehaviorCondition condition = enterConditions[i];
                if (condition == null)
                    continue;
                if (!condition.Evaluate(context))
                    return false;
            }

            return true;
        }

        public abstract IPetBehaviorRuntime CreateRuntime();
    }
}
