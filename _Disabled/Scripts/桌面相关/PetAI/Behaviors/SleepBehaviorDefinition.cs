using DesktopPet.Decor;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.AI
{
    [CreateAssetMenu(menuName = "桌宠/AI/行为/睡觉", fileName = "SleepBehavior")]
    public sealed class SleepBehaviorDefinition : PetBehaviorDefinition
    {
        [BoxGroup("家具")]
        [LabelText("走近速度")]
        [MinValue(0.1f)]
        public float moveToBedSpeed = 1.0f;

        [BoxGroup("家具")]
        [LabelText("到达停距")]
        [MinValue(0.05f)]
        public float bedStopDistance = 0.28f;

        private void Reset()
        {
            behaviorId = "sleep";
            weight = 0.8f;
            minDuration = 4f;
            maxDuration = 10f;
            cooldown = 6f;
            canBeInterrupted = true;
            interruptPriority = 5;
            animTrigger = "Sleep";
            animSpeedParam = "Speed";
            animSpeedValue = 0f;
            moveToBedSpeed = 1.0f;
            bedStopDistance = 0.28f;
        }

        public override IPetBehaviorRuntime CreateRuntime() => new Runtime(this);

        private sealed class Runtime : FurnitureSpotBehaviorRuntime
        {
            private readonly SleepBehaviorDefinition _def;

            public Runtime(SleepBehaviorDefinition definition) : base(definition)
            {
                _def = definition;
            }

            protected override DecorFurnitureKind FurnitureKind => DecorFurnitureKind.Bed;
            protected override float MoveSpeed => _def.moveToBedSpeed;
            protected override float StopDistance => _def.bedStopDistance;
        }
    }
}
