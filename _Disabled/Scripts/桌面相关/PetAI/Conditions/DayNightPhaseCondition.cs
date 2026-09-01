using DesktopPet.Environment;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>昼夜阶段门闸：仅当当前阶段在允许列表内时行为可进入（选型 / 维持）。</summary>
    [CreateAssetMenu(menuName = "桌宠/AI/条件/昼夜阶段", fileName = "DayNightPhaseCondition")]
    public sealed class DayNightPhaseCondition : PetBehaviorCondition
    {
        [BoxGroup("允许阶段")]
        [LabelText("白天")]
        public bool allowDay;

        [BoxGroup("允许阶段")]
        [LabelText("黄昏")]
        public bool allowDusk;

        [BoxGroup("允许阶段")]
        [LabelText("夜晚")]
        public bool allowNight = true;

        public override bool Evaluate(PetBehaviorContext context)
        {
            DayNightPhase phase = ResolveCurrentPhase();
            switch (phase)
            {
                case DayNightPhase.Day:
                    return allowDay;
                case DayNightPhase.Dusk:
                    return allowDusk;
                case DayNightPhase.Night:
                    return allowNight;
                default:
                    return false;
            }
        }

        private static DayNightPhase ResolveCurrentPhase()
        {
            EnvironmentManager env = DesktopPetServices.Environment;
            if (env?.DayNight == null)
                return DayNightPhase.Day;

            return env.DayNight.CurrentPhase;
        }
    }
}
